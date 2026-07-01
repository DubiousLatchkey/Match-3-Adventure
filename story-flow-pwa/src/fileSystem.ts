import type {
  AssetCatalog,
  StoryBeat,
  VisualData,
} from './storyModel';
import {
  collectUsedDialogueTokens,
  emptyVisualData,
  parseCombat,
  parseDialogue,
  validateBeats,
  visualDataPath,
} from './storyModel';

type DirectoryHandle = any;
type FileHandle = any;

export type LoadedProject = {
  root: DirectoryHandle;
  beats: StoryBeat[];
  visualData: VisualData;
  storyFlow: StoryFlowData;
  assets: AssetCatalog;
};

export type StoryFlowData = {
  newGameStart: string;
  defaultResume: string;
};

const textDecoder = new TextDecoder();

async function getDirectory(root: DirectoryHandle, path: string[], create = false) {
  let current = root;
  for (const part of path) {
    current = await current.getDirectoryHandle(part, { create });
  }
  return current;
}

async function tryGetDirectory(root: DirectoryHandle, path: string[]) {
  try {
    return await getDirectory(root, path);
  } catch {
    return null;
  }
}

async function readFile(handle: FileHandle) {
  const file = await handle.getFile();
  return textDecoder.decode(await file.arrayBuffer());
}

async function writeFile(handle: FileHandle, text: string) {
  const writable = await handle.createWritable();
  await writable.write(text);
  await writable.close();
}

async function listFiles(dir: DirectoryHandle, prefix = ''): Promise<Array<{ name: string; relativePath: string; handle: FileHandle }>> {
  const files: Array<{ name: string; relativePath: string; handle: FileHandle }> = [];
  for await (const [name, handle] of dir.entries()) {
    if (handle.kind === 'file') {
      files.push({ name, relativePath: `${prefix}${name}`, handle });
    } else if (handle.kind === 'directory') {
      files.push(...(await listFiles(handle, `${prefix}${name}/`)));
    }
  }
  return files;
}

async function listResourceNames(root: DirectoryHandle, path: string[]) {
  const dir = await tryGetDirectory(root, path);
  if (!dir) return [];

  const files = await listFiles(dir);
  return files
    .filter((file) => !file.name.endsWith('.meta'))
    .map((file) => file.relativePath.replace(/\.[^/.]+$/, '').replaceAll('\\', '/'))
    .sort((a, b) => a.localeCompare(b));
}

async function listFirstExistingResourceNames(root: DirectoryHandle, paths: string[][]) {
  for (const path of paths) {
    const names = await listResourceNames(root, path);
    if (names.length > 0) return names;
  }
  return [];
}

async function listCharacterAssets(root: DirectoryHandle) {
  const dir = await tryGetDirectory(root, ['Assets', 'Resources', 'Characters']);
  if (!dir) return { characters: [], characterExpressions: {} };

  const files = (await listFiles(dir))
    .filter((file) => !file.name.endsWith('.meta'))
    .map((file) => file.name.replace(/\.[^/.]+$/, ''));
  const characterExpressions: Record<string, string[]> = {};

  for (const file of files) {
    const match = file.match(/^(.+?)(Normal|Angry|Happy|Pained|Pout|Question\d*|Serious|Shocked|Sad|Smile|Surprised|Concerned)(?:\(\d+\))?$/i);
    if (!match) continue;
    const character = match[1];
    const expression = match[2];
    characterExpressions[character] = Array.from(new Set([...(characterExpressions[character] ?? []), expression])).sort();
  }

  const characters = Object.entries(characterExpressions)
    .filter(([, expressions]) => expressions.some((expression) => expression.toLowerCase() === 'normal'))
    .map(([character]) => character)
    .sort((a, b) => a.localeCompare(b));

  return { characters, characterExpressions };
}

async function readVisualData(root: DirectoryHandle): Promise<VisualData> {
  try {
    const handle = await root.getFileHandle(visualDataPath[0]);
    return { ...emptyVisualData(), ...JSON.parse(await readFile(handle)) };
  } catch {
    return emptyVisualData();
  }
}

async function readStoryFlow(root: DirectoryHandle): Promise<StoryFlowData> {
  try {
    const dir = await getDirectory(root, ['Assets', 'Resources', 'Data']);
    const handle = await dir.getFileHandle('story_flow.json');
    return { newGameStart: 'opening', defaultResume: 'revelation', ...JSON.parse(await readFile(handle)) };
  } catch {
    return { newGameStart: 'opening', defaultResume: 'revelation' };
  }
}

export async function saveStoryFlow(root: DirectoryHandle, storyFlow: StoryFlowData) {
  const dir = await getDirectory(root, ['Assets', 'Resources', 'Data'], true);
  const handle = await dir.getFileHandle('story_flow.json', { create: true });
  await writeFile(handle, `${JSON.stringify(storyFlow, null, 2)}\n`);
}

export async function saveVisualData(root: DirectoryHandle, visualData: VisualData) {
  const handle = await root.getFileHandle(visualDataPath[0], { create: true });
  await writeFile(handle, `${JSON.stringify(visualData, null, 2)}\n`);
}

export async function saveBeat(root: DirectoryHandle, beat: StoryBeat) {
  const path = beat.relativePath.split('/');
  const fileName = path.pop();
  if (!fileName) throw new Error(`Invalid path: ${beat.relativePath}`);

  const dir = await getDirectory(root, path);
  const handle = await dir.getFileHandle(fileName);
  await writeFile(handle, beat.text);
}

export async function renameBeatFile(root: DirectoryHandle, beat: StoryBeat, newName: string) {
  const cleanName = newName.trim().replace(/[\\/:*?"<>|]+/g, '').replace(/\s+/g, '');
  if (!cleanName) throw new Error('A file name is required.');

  const path = beat.relativePath.split('/');
  const oldFileName = path.pop();
  if (!oldFileName) throw new Error(`Invalid path: ${beat.relativePath}`);

  const dir = await getDirectory(root, path);
  const oldHandle = await dir.getFileHandle(oldFileName);
  const oldText = await readFile(oldHandle);
  const newFileName = `${cleanName}.txt`;
  const newHandle = await dir.getFileHandle(newFileName, { create: true });
  await writeFile(newHandle, oldText);

  try {
    const oldMetaHandle = await dir.getFileHandle(`${oldFileName}.meta`);
    const metaText = await readFile(oldMetaHandle);
    const newMetaHandle = await dir.getFileHandle(`${newFileName}.meta`, { create: true });
    await writeFile(newMetaHandle, metaText);
    await dir.removeEntry(`${oldFileName}.meta`);
  } catch {
    // Browser-created files may not have Unity meta files yet.
  }

  await dir.removeEntry(oldFileName);
  return cleanName;
}

export async function createBeat(root: DirectoryHandle, type: 'dialogue' | 'combat', name: string) {
  const cleanName = name.trim().replace(/[\\/:*?"<>|]+/g, '').replace(/\s+/g, '');
  if (!cleanName) throw new Error('A file name is required.');
  const folder = type === 'dialogue' ? 'Dialogues' : 'Combats';
  const dir = await getDirectory(root, ['Assets', 'Resources', folder]);
  const fileName = `${cleanName}.txt`;
  const handle = await dir.getFileHandle(fileName, { create: true });
  const starter = type === 'dialogue'
    ? '|main 0.5 0\nmain: \n'
    : 'enemy: Enemy Name\ndialogue: \n';
  await writeFile(handle, starter);
  return cleanName;
}

export async function deleteBeat(root: DirectoryHandle, beat: StoryBeat) {
  const path = beat.relativePath.split('/');
  const fileName = path.pop();
  if (!fileName) throw new Error(`Invalid path: ${beat.relativePath}`);

  const dir = await getDirectory(root, path);
  await dir.removeEntry(fileName);
  try {
    await dir.removeEntry(`${fileName}.meta`);
  } catch {
    // Unity meta files may not exist for newly-created browser files.
  }
}

export async function loadProject(root: DirectoryHandle): Promise<LoadedProject> {
  const dialogues = await getDirectory(root, ['Assets', 'Resources', 'Dialogues']);
  const combats = await getDirectory(root, ['Assets', 'Resources', 'Combats']);
  const storyFlow = await readStoryFlow(root);
  const dialogueFiles = (await listFiles(dialogues, 'Assets/Resources/Dialogues/')).filter((file) => file.name.endsWith('.txt'));
  const combatFiles = (await listFiles(combats, 'Assets/Resources/Combats/')).filter((file) => file.name.endsWith('.txt'));

  const beats: StoryBeat[] = [];
  for (const file of dialogueFiles) {
    beats.push(parseDialogue(file.name, file.relativePath, await readFile(file.handle)));
  }
  for (const file of combatFiles) {
    beats.push(parseCombat(file.name, file.relativePath, await readFile(file.handle)));
  }

  const used = beats
    .filter((beat) => beat.type === 'dialogue')
    .map((beat) => collectUsedDialogueTokens(beat.text));

  const mergeUsed = (key: keyof ReturnType<typeof collectUsedDialogueTokens>) =>
    Array.from(new Set(used.flatMap((entry) => Array.from(entry[key])))).sort((a, b) => a.localeCompare(b));

  const visualData = await readVisualData(root);
  const characterAssets = await listCharacterAssets(root);
  const assets: AssetCatalog = {
    characters: Array.from(new Set([...characterAssets.characters, ...mergeUsed('characters'), ...visualData.placeholders.characters])).sort(),
    characterExpressions: characterAssets.characterExpressions,
    backgrounds: Array.from(new Set([...(await listResourceNames(root, ['Assets', 'Resources', 'Backgrounds'])), ...mergeUsed('backgrounds'), ...visualData.placeholders.backgrounds])).sort(),
    music: Array.from(new Set([...(await listResourceNames(root, ['Assets', 'Resources', 'Music'])), ...mergeUsed('music'), ...visualData.placeholders.music])).sort(),
    sounds: Array.from(new Set([...(await listFirstExistingResourceNames(root, [
      ['Assets', 'Resources', 'Sounds'],
      ['Assets', 'Resources', 'Audio'],
      ['Assets', 'Resources', 'SFX'],
    ])), ...mergeUsed('sounds'), ...visualData.placeholders.sounds])).sort(),
    displays: Array.from(new Set([...(await listFirstExistingResourceNames(root, [
      ['Assets', 'Resources', 'Displays'],
      ['Assets', 'Resources', 'CGs'],
      ['Assets', 'Resources', 'Tutorials'],
    ])), ...mergeUsed('displays'), ...visualData.placeholders.displays])).sort(),
    enemies: await listResourceNames(root, ['Assets', 'Resources', 'Enemies']),
    weapons: await listResourceNames(root, ['Assets', 'Resources', 'Weapons']),
  };

  return {
    root,
    beats: validateBeats(beats, [storyFlow.newGameStart, storyFlow.defaultResume]),
    visualData,
    storyFlow,
    assets,
  };
}

export async function pickUnityProject() {
  if (!('showDirectoryPicker' in window)) {
    throw new Error('Folder access requires Chrome or Edge with the File System Access API.');
  }

  const root = await (window as any).showDirectoryPicker({ mode: 'readwrite' });
  const permission = await root.requestPermission({ mode: 'readwrite' });
  if (permission !== 'granted') {
    throw new Error('Read/write permission was not granted.');
  }
  return root;
}
