import { parseEnterArgs, tokenizeArgs } from './scriptBlocks';

export type BeatType = 'dialogue' | 'combat';

export type StoryBeat = {
  id: string;
  name: string;
  type: BeatType;
  relativePath: string;
  text: string;
  transition: string | null;
  transitionLineIndex: number | null;
  issues: string[];
};

export type VisualNode = {
  x: number;
  y: number;
};

export type VisualData = {
  version: 1;
  nodes: Record<string, VisualNode>;
  placeholders: {
    characters: string[];
    backgrounds: string[];
    music: string[];
    sounds: string[];
    displays: string[];
  };
};

export type AssetCatalog = {
  characters: string[];
  characterExpressions: Record<string, string[]>;
  backgrounds: string[];
  music: string[];
  sounds: string[];
  displays: string[];
  enemies: string[];
  weapons: string[];
};

export const visualDataPath = ['story-flow.visuals.json'];

export const emptyVisualData = (): VisualData => ({
  version: 1,
  nodes: {},
  placeholders: {
    characters: [],
    backgrounds: [],
    music: [],
    sounds: [],
    displays: [],
  },
});

const lineBreak = /\r?\n/;

export function stripTxt(name: string) {
  return name.replace(/\.txt$/i, '');
}

export function fileBaseName(relativePath: string) {
  return stripTxt(relativePath.split('/').pop() ?? relativePath);
}

export function parseDialogue(_name: string, relativePath: string, text: string): StoryBeat {
  const lines = text.split(lineBreak);
  let transition: string | null = null;
  let transitionLineIndex: number | null = null;

  for (let i = lines.length - 1; i >= 0; i -= 1) {
    const trimmed = lines[i].trim();
    if (!trimmed) continue;

    const match = trimmed.match(/^\|combat(?:Direct)?\s+(.+)$/i);
    if (match) {
      transition = match[1].trim();
      transitionLineIndex = i;
    }
    break;
  }

  return {
    id: `dialogue:${fileBaseName(relativePath)}`,
    name: fileBaseName(relativePath),
    type: 'dialogue',
    relativePath,
    text,
    transition,
    transitionLineIndex,
    issues: [],
  };
}

export function parseCombat(_name: string, relativePath: string, text: string): StoryBeat {
  const lines = text.split(lineBreak);
  let transition: string | null = null;
  let transitionLineIndex: number | null = null;

  for (let i = lines.length - 1; i >= 0; i -= 1) {
    const trimmed = lines[i].trim();
    if (!trimmed) continue;

    const match = trimmed.match(/^dialogue\s*:\s*(.*)$/i);
    if (match) {
      transition = match[1].trim();
      transitionLineIndex = i;
      break;
    }
  }

  return {
    id: `combat:${fileBaseName(relativePath)}`,
    name: fileBaseName(relativePath),
    type: 'combat',
    relativePath,
    text,
    transition,
    transitionLineIndex,
    issues: [],
  };
}

export function targetBeatId(sourceType: BeatType, targetName: string | null) {
  if (!targetName) return null;
  return sourceType === 'dialogue'
    ? `combat:${stripTxt(targetName)}`
    : `dialogue:${stripTxt(targetName)}`;
}

export function setTransitionLine(beat: StoryBeat, targetName: string | null, directCombat: boolean) {
  const lines = beat.text.split(lineBreak);
  const transitionLine =
    beat.type === 'dialogue'
      ? targetName
        ? `|${directCombat ? 'combatDirect' : 'combat'} ${targetName}`
        : ''
      : targetName
        ? `dialogue: ${targetName}`
        : '';

  if (beat.type === 'combat') {
    const bodyLines = lines.filter((line, index) => (
      index !== beat.transitionLineIndex &&
      !line.trim().match(/^dialogue\s*:/i)
    ));
    while (bodyLines.length > 0 && bodyLines[bodyLines.length - 1].trim() === '') {
      bodyLines.pop();
    }
    if (transitionLine) bodyLines.push(transitionLine);
    return bodyLines.join('\n');
  }

  if (beat.transitionLineIndex !== null) {
    if (transitionLine) {
      lines[beat.transitionLineIndex] = transitionLine;
    } else {
      lines.splice(beat.transitionLineIndex, 1);
    }
  } else if (transitionLine) {
    if (lines.length > 0 && lines[lines.length - 1].trim() !== '') {
      lines.push(transitionLine);
    } else {
      lines[lines.length - 1] = transitionLine;
    }
  }

  return lines.join('\n');
}

export function collectUsedDialogueTokens(text: string) {
  const characters = new Set<string>();
  const backgrounds = new Set<string>();
  const music = new Set<string>();
  const sounds = new Set<string>();
  const displays = new Set<string>();

  text.split(lineBreak).forEach((line) => {
    const trimmed = line.trim();
    const speaker = trimmed.match(/^([^:\s|]+)\s*:/);
    if (speaker) characters.add(speaker[1]);

    const command = trimmed.match(/^\|(\S+)(?:\s+(.+))?$/);
    if (!command) return;

    const name = command[1];
    const rest = command[2] ?? '';
    const args = tokenizeArgs(rest);
    if (args.length === 0) return;

    if (name === 'enter') {
      const enter = parseEnterArgs(args);
      if (enter.assetKey) characters.add(enter.assetKey);
    }
    if (['translate', 'move', 'moveTo', 'exit', 'swap', 'setExpression', 'setDisplayName'].includes(name)) {
      characters.add(args[0]);
    }
    if (['setBackground', 'transition'].includes(name)) backgrounds.add(args.join(' '));
    if (name === 'playMusic') music.add(args.join(' '));
    if (name === 'playSound') sounds.add(args.join(' '));
    if (['display', 'displayCG'].includes(name)) displays.add(args.join(' '));
  });

  return { characters, backgrounds, music, sounds, displays };
}

export function validateBeats(beats: StoryBeat[], rootDialogueNames: string[] = []) {
  const ids = new Set(beats.map((beat) => beat.id));
  const roots = new Set(rootDialogueNames.filter(Boolean).map((name) => `dialogue:${stripTxt(name)}`));
  const referenced = new Set<string>();

  return beats.map((beat) => {
    const issues: string[] = [];
    const target = targetBeatId(beat.type, beat.transition);

    if (target) {
      referenced.add(target);
      if (!ids.has(target)) {
        issues.push(`Missing transition target: ${beat.transition}`);
      }
    }

    if (!target && beat.type === 'combat') {
      issues.push('Combat has no reserved final dialogue transition.');
    }

    return { ...beat, issues };
  }).map((beat) => {
    if (beat.type === 'dialogue' && !referenced.has(beat.id) && !roots.has(beat.id)) {
      return { ...beat, issues: [...beat.issues, 'Unconnected dialogue'] };
    }
    return beat;
  });
}
