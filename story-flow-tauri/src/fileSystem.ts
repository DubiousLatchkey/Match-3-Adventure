import { invoke } from '@tauri-apps/api/core';
import { open } from '@tauri-apps/plugin-dialog';
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
  parseGearStop,
  validateBeats,
  visualDataPath,
} from './storyModel';

type ProjectRoot = string;

type NativeFileEntry = {
  name: string;
  relative_path: string;
};

type ListedFile = {
  name: string;
  relativePath: string;
  projectPath: string;
};

export type LoadedProject = {
  root: ProjectRoot;
  beats: StoryBeat[];
  visualData: VisualData;
  storyFlow: StoryFlowData;
  assets: AssetCatalog;
};

export type StoryFlowData = {
  newGameStart: string;
  defaultResume: string;
};

function joinPath(parts: string[]) {
  return parts.filter(Boolean).join('/');
}

function fileNameFromPath(path: string) {
  return path.split('/').pop() ?? path;
}

async function readFile(root: ProjectRoot, relativePath: string) {
  return invoke<string>('read_text', {
    rootPath: root,
    relativePath,
  });
}

async function writeFile(root: ProjectRoot, relativePath: string, text: string) {
  await invoke('write_text', {
    rootPath: root,
    relativePath,
    text,
  });
}

async function removeFile(root: ProjectRoot, relativePath: string) {
  await invoke('remove_file', {
    rootPath: root,
    relativePath,
  });
}

async function listFiles(root: ProjectRoot, path: string[], prefix = ''): Promise<ListedFile[]> {
  const relativeDir = joinPath(path);
  const files = await invoke<NativeFileEntry[]>('list_files', {
    rootPath: root,
    relativeDir,
  });

  return files.map((file) => ({
    name: file.name,
    relativePath: `${prefix}${file.relative_path}`,
    projectPath: joinPath([relativeDir, file.relative_path]),
  }));
}

async function listResourceNames(root: ProjectRoot, path: string[]) {
  try {
    const files = await listFiles(root, path);
    return files
      .filter((file) => !file.name.endsWith('.meta'))
      .map((file) => file.relativePath.replace(/\.[^/.]+$/, '').replaceAll('\\', '/'))
      .sort((a, b) => a.localeCompare(b));
  } catch {
    return [];
  }
}

async function listFirstExistingResourceNames(root: ProjectRoot, paths: string[][]) {
  for (const path of paths) {
    const names = await listResourceNames(root, path);
    if (names.length > 0) return names;
  }
  return [];
}

async function listCharacterAssets(root: ProjectRoot) {
  const files = (await listResourceNames(root, ['Assets', 'Resources', 'Characters']))
    .map((file) => fileNameFromPath(file));
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

async function readVisualData(root: ProjectRoot): Promise<VisualData> {
  try {
    return { ...emptyVisualData(), ...JSON.parse(await readFile(root, visualDataPath[0])) };
  } catch {
    return emptyVisualData();
  }
}

async function readStoryFlow(root: ProjectRoot): Promise<StoryFlowData> {
  try {
    return { newGameStart: 'opening', defaultResume: 'revelation', ...JSON.parse(await readFile(root, 'Assets/Resources/Data/story_flow.json')) };
  } catch {
    return { newGameStart: 'opening', defaultResume: 'revelation' };
  }
}

export async function saveStoryFlow(root: ProjectRoot, storyFlow: StoryFlowData) {
  await writeFile(root, 'Assets/Resources/Data/story_flow.json', `${JSON.stringify(storyFlow, null, 2)}\n`);
}

export async function saveVisualData(root: ProjectRoot, visualData: VisualData) {
  await writeFile(root, visualDataPath[0], `${JSON.stringify(visualData, null, 2)}\n`);
}

export async function saveBeat(root: ProjectRoot, beat: StoryBeat) {
  await writeFile(root, beat.relativePath, beat.text);
}

export async function renameBeatFile(root: ProjectRoot, beat: StoryBeat, newName: string) {
  const cleanName = newName.trim().replace(/[\\/:*?"<>|]+/g, '').replace(/\s+/g, '');
  if (!cleanName) throw new Error('A file name is required.');

  const path = beat.relativePath.split('/');
  const oldFileName = path.pop();
  if (!oldFileName) throw new Error(`Invalid path: ${beat.relativePath}`);

  let oldText = await readFile(root, beat.relativePath);
  const extension = oldFileName.endsWith('.json') ? '.json' : '.txt';
  if (beat.type === 'gearStop' && !oldText.trim()) oldText = `${JSON.stringify({ nextStops: [] }, null, 2)}\n`;
  const newFileName = `${cleanName}${extension}`;
  const newPath = joinPath([...path, newFileName]);
  await writeFile(root, newPath, oldText);

  try {
    const oldMetaPath = joinPath([...path, `${oldFileName}.meta`]);
    const metaText = await readFile(root, oldMetaPath);
    await writeFile(root, joinPath([...path, `${newFileName}.meta`]), metaText);
    await removeFile(root, oldMetaPath);
  } catch {
    // Newly-created files may not have Unity meta files yet.
  }

  await removeFile(root, beat.relativePath);
  return cleanName;
}

export async function createBeat(root: ProjectRoot, type: StoryBeat['type'], name: string) {
  const cleanName = name.trim().replace(/[\\/:*?"<>|]+/g, '').replace(/\s+/g, '');
  if (!cleanName) throw new Error('A file name is required.');
  const path = type === 'dialogue'
    ? ['Assets', 'Resources', 'Dialogues']
    : type === 'combat'
      ? ['Assets', 'Resources', 'Combats']
      : ['Assets', 'Resources', 'Data', 'GearStops'];
  const fileName = `${cleanName}${type === 'gearStop' ? '.json' : '.txt'}`;
  const starter = type === 'dialogue'
    ? '|main 0.5 0\n|title "New Scene"\nmain: \n'
    : type === 'combat'
      ? 'enemy: Enemy Name\ndialogue: \n'
      : `${JSON.stringify({ nextStops: [] }, null, 2)}\n`;
  await writeFile(root, joinPath([...path, fileName]), starter);
  return cleanName;
}

export async function deleteBeat(root: ProjectRoot, beat: StoryBeat) {
  await removeFile(root, beat.relativePath);
  try {
    await removeFile(root, `${beat.relativePath}.meta`);
  } catch {
    // Unity meta files may not exist for newly-created files.
  }
}

export async function loadProject(root: ProjectRoot): Promise<LoadedProject> {
  const storyFlow = await readStoryFlow(root);
  const dialogueFiles = (await listFiles(root, ['Assets', 'Resources', 'Dialogues'], 'Assets/Resources/Dialogues/')).filter((file) => file.name.endsWith('.txt'));
  const combatFiles = (await listFiles(root, ['Assets', 'Resources', 'Combats'], 'Assets/Resources/Combats/')).filter((file) => file.name.endsWith('.txt'));
  const gearStopFiles = (await listFiles(root, ['Assets', 'Resources', 'Data', 'GearStops'], 'Assets/Resources/Data/GearStops/')
    .catch(() => [])).filter((file) => file.name.endsWith('.json'));

  const beats: StoryBeat[] = [];
  for (const file of dialogueFiles) {
    beats.push(parseDialogue(file.name, file.relativePath, await readFile(root, file.projectPath)));
  }
  for (const file of combatFiles) {
    beats.push(parseCombat(file.name, file.relativePath, await readFile(root, file.projectPath)));
  }
  for (const file of gearStopFiles) {
    beats.push(parseGearStop(file.name, file.relativePath, await readFile(root, file.projectPath)));
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
    dialogues: beats.filter((beat) => beat.type === 'dialogue').map((beat) => beat.name).sort(),
    combats: beats.filter((beat) => beat.type === 'combat').map((beat) => beat.name).sort(),
    gearStops: beats.filter((beat) => beat.type === 'gearStop').map((beat) => beat.name).sort(),
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
  const selected = await open({
    directory: true,
    multiple: false,
    title: 'Select the Match-3 Adventure Unity project folder',
  });

  if (!selected || Array.isArray(selected)) {
    throw new Error('No Unity project folder was selected.');
  }

  return selected;
}
