import { parseEnterArgs, tokenizeArgs } from './scriptBlocks';

export type BeatType = 'dialogue' | 'combat' | 'gearStop';

export type StoryTransition = {
  id: string;
  label: string;
  targetType: BeatType;
  target: string;
};

export type StoryBeat = {
  id: string;
  name: string;
  title: string;
  hasAuthoredTitle?: boolean;
  type: BeatType;
  relativePath: string;
  text: string;
  transition: string | null;
  transitionType: BeatType | null;
  transitionLineIndex: number | null;
  transitions: StoryTransition[];
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
  dialogues: string[];
  combats: string[];
  gearStops: string[];
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

export function stripExtension(name: string) {
  return name.replace(/\.(txt|json)$/i, '');
}

export function stripTxt(name: string) {
  return name.replace(/\.txt$/i, '');
}

export function fileBaseName(relativePath: string) {
  return stripExtension(relativePath.split('/').pop() ?? relativePath);
}

function displayTitleFromName(name: string) {
  return name
    .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
    .replace(/[_-]+/g, ' ')
    .replace(/\s+/g, ' ')
    .trim()
    .replace(/^./, (value) => value.toUpperCase());
}

export function targetBeatId(targetType: BeatType | null, targetName: string | null) {
  if (!targetType || !targetName) return null;
  return `${targetType}:${stripExtension(targetName)}`;
}

export function transitionTargetId(transition: StoryTransition) {
  return targetBeatId(transition.targetType, transition.target);
}

export function parseDialogue(_name: string, relativePath: string, text: string): StoryBeat {
  const lines = text.split(lineBreak);
  const name = fileBaseName(relativePath);
  let title = displayTitleFromName(name);
  let hasAuthoredTitle = false;
  let transition: string | null = null;
  let transitionType: BeatType | null = null;
  let transitionLineIndex: number | null = null;

  for (let i = 0; i < lines.length; i += 1) {
    const trimmed = lines[i].trim();
    const titleMatch = trimmed.match(/^\|title(?:\s+(.+))?$/i);
    if (titleMatch) {
      const args = tokenizeArgs(titleMatch[1] ?? '');
      const authoredTitle = args.join(' ').trim();
      if (authoredTitle) {
        title = authoredTitle;
        hasAuthoredTitle = true;
      }
    }
  }

  for (let i = lines.length - 1; i >= 0; i -= 1) {
    const trimmed = lines[i].trim();
    if (!trimmed) continue;

    const combatMatch = trimmed.match(/^\|combat(?:Direct)?\s+(.+)$/i);
    const gearStopMatch = trimmed.match(/^\|gearStop\s+(.+)$/i);
    const dialogueMatch = trimmed.match(/^\|dialogue\s+(.+)$/i);
    if (combatMatch || gearStopMatch || dialogueMatch) {
      transition = (combatMatch?.[1] ?? gearStopMatch?.[1] ?? dialogueMatch?.[1] ?? '').trim();
      transitionType = combatMatch ? 'combat' : gearStopMatch ? 'gearStop' : 'dialogue';
      transitionLineIndex = i;
    }
    break;
  }

  return {
    id: `dialogue:${name}`,
    name,
    title,
    hasAuthoredTitle,
    type: 'dialogue',
    relativePath,
    text,
    transition,
    transitionType,
    transitionLineIndex,
    transitions: transition && transitionType ? [{ id: 'reserved', label: '', targetType: transitionType, target: transition }] : [],
    issues: [],
  };
}

export function parseCombat(_name: string, relativePath: string, text: string): StoryBeat {
  const lines = text.split(lineBreak);
  const name = fileBaseName(relativePath);
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
    id: `combat:${name}`,
    name,
    title: displayTitleFromName(name),
    type: 'combat',
    relativePath,
    text,
    transition,
    transitionType: transition ? 'dialogue' : null,
    transitionLineIndex,
    transitions: transition ? [{ id: 'reserved', label: '', targetType: 'dialogue', target: transition }] : [],
    issues: [],
  };
}

export function parseGearStop(_name: string, relativePath: string, text: string): StoryBeat {
  const fileName = fileBaseName(relativePath);
  let data: any = {};
  try {
    data = JSON.parse(text || '{}');
  } catch {
    data = {};
  }

  const name = fileName;
  const transitions: StoryTransition[] = Array.isArray(data.nextStops)
    ? data.nextStops.map((entry: any) => ({
      id: String(typeof entry === 'string' ? entry : entry?.target || entry?.id || ''),
      label: '',
      targetType: 'dialogue',
      target: String(typeof entry === 'string' ? entry : entry?.target || entry?.id || ''),
    }))
    : [];

  return {
    id: `gearStop:${name}`,
    name,
    title: displayTitleFromName(name),
    type: 'gearStop',
    relativePath,
    text: serializeGearStop(transitions),
    transition: null,
    transitionType: null,
    transitionLineIndex: null,
    transitions,
    issues: [],
  };
}

export function serializeGearStop(nextStops: StoryTransition[]) {
  return `${JSON.stringify({
    nextStops: nextStops.map((transition) => transition.target).filter(Boolean),
  }, null, 2)}\n`;
}

export function setTransitionLine(
  beat: StoryBeat,
  targetName: string | null,
  targetType: BeatType | null,
  directCombat = true,
) {
  const lines = beat.text.split(lineBreak);
  const transitionLine =
    beat.type === 'dialogue'
      ? targetName && targetType
        ? dialogueTransitionLine(targetName, targetType, directCombat)
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

  if (beat.type !== 'dialogue') {
    return beat.text;
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

function dialogueTransitionLine(targetName: string, targetType: BeatType, directCombat: boolean) {
  if (targetType === 'gearStop') return `|gearStop ${targetName}`;
  if (targetType === 'dialogue') return `|dialogue ${targetName}`;
  return `|${directCombat ? 'combatDirect' : 'combat'} ${targetName}`;
}

export function replaceGearStopTransition(beat: StoryBeat, transition: StoryTransition) {
  const nextStops = beat.transitions.filter((candidate) => candidate.target !== transition.target);
  const index = nextStops.findIndex((candidate) => candidate.id === transition.id);
  if (index >= 0) nextStops[index] = transition;
  else nextStops.push(transition);
  return gearStopBeatWithTransitions(beat, nextStops);
}

export function removeGearStopTransition(beat: StoryBeat, transitionId: string) {
  return gearStopBeatWithTransitions(beat, beat.transitions.filter((transition) => transition.id !== transitionId));
}

export function gearStopBeatWithTransitions(beat: StoryBeat, transitions: StoryTransition[]) {
  const normalized = transitions
    .filter((transition) => transition.target)
    .map((transition) => ({
      id: transition.target,
      label: '',
      targetType: 'dialogue' as BeatType,
      target: transition.target,
    }));
  const text = serializeGearStop(normalized);
  return { ...beat, text, transitions: normalized };
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
  const roots = new Set(rootDialogueNames.filter(Boolean).map((name) => `dialogue:${stripExtension(name)}`));
  const referenced = new Set<string>();

  return beats.map((beat) => {
    const issues: string[] = [];

    for (const transition of beat.transitions) {
      if (!transition.target) continue;
      const target = transitionTargetId(transition);
      if (!target) continue;
      referenced.add(target);
      if (!ids.has(target)) {
        issues.push(`Missing ${transition.targetType} target: ${transition.target}`);
      }
    }

    if (beat.type === 'combat' && beat.transitions.length === 0) {
      issues.push('Combat has no reserved final dialogue transition.');
    }

    if (beat.type === 'gearStop' && beat.transitions.length === 0) {
      issues.push('Gear stop has no next stops.');
    }

    return { ...beat, issues };
  }).map((beat) => {
    if (beat.type === 'dialogue' && !referenced.has(beat.id) && !roots.has(beat.id)) {
      return { ...beat, issues: [...beat.issues, 'Unconnected dialogue'] };
    }
    return beat;
  });
}
