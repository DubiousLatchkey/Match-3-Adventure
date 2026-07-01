import type { AssetCatalog, BeatType } from './storyModel';

export type ScriptBlock =
  | { id: string; kind: 'blank'; raw: string }
  | { id: string; kind: 'sceneSetup'; characters: SceneSetupCharacter[]; raw: string }
  | { id: string; kind: 'dialogue'; speaker: string; text: string; raw: string }
  | { id: string; kind: 'command'; command: string; args: string[]; raw: string }
  | { id: string; kind: 'setting'; key: string; value: string; raw: string }
  | { id: string; kind: 'text'; text: string; raw: string };

const commandRegex = /^\|(\S+)(?:\s+(.+))?$/;
const knownCommands = new Set([
  'translate', 'setExpression', 'setDisplayName', 'move', 'moveTo', 'exit', 'enter', 'combat', 'combatDirect',
  'resumeCombat', 'transition', 'setBackground', 'setPrefValue', 'setFlag', 'playMusic', 'stopMusic',
  'playSound', 'display', 'stopDisplay', 'displayCG', 'stopDisplayCG', 'swap',
]);

export type SceneSetupCharacter = {
  id: string;
  character: string;
  x: string;
  y: string;
  displayName: string;
  expression: string;
};

export type EnterCommandOptions = {
  assetKey: string;
  x: string;
  y: string;
  expression: string;
  displayName: string;
  legacy: boolean;
};

export function tokenizeArgs(input: string) {
  const args: string[] = [];
  const regex = /"([^"]*)"|(\S+)/g;
  let match: RegExpExecArray | null;
  while ((match = regex.exec(input)) !== null) {
    args.push(match[1] ?? match[2]);
  }
  return args;
}

function quoteArg(arg: string) {
  return /\s/.test(arg) ? `"${arg.replaceAll('"', '\\"')}"` : arg;
}

export function parseBlocks(text: string): ScriptBlock[] {
  return text.split(/\r?\n/).map((line, index) => {
    const id = `${index}-${crypto.randomUUID()}`;
    if (line.trim() === '') return { id, kind: 'blank', raw: line };

    const command = line.trim().match(commandRegex);
    if (command) {
      if (index === 0 && !knownCommands.has(command[1])) {
        return {
          id,
          kind: 'sceneSetup',
          characters: parseSceneSetup(line),
          raw: line,
        };
      }

      return {
        id,
        kind: 'command',
        command: command[1],
        args: tokenizeArgs(command[2] ?? ''),
        raw: line,
      };
    }

    const setting = line.match(/^([^:|\s][^:]*?)\s*:\s*(.*)$/);
    if (setting && ['enemy', 'dialogue', 'weapons', 'solo', 'companion', 'tutorial'].includes(setting[1].trim())) {
      return { id, kind: 'setting', key: setting[1].trim(), value: setting[2], raw: line };
    }

    const dialogue = line.match(/^([^:|]*?)\s*:\s*(.*)$/);
    if (dialogue) {
      return { id, kind: 'dialogue', speaker: dialogue[1].trim(), text: dialogue[2], raw: line };
    }

    return { id, kind: 'text', text: line, raw: line };
  });
}

export function serializeBlocks(blocks: ScriptBlock[]) {
  return blocks.map((block) => {
    if (block.kind === 'blank') return '';
    if (block.kind === 'sceneSetup') return serializeSceneSetup(block.characters);
    if (block.kind === 'dialogue') return `${block.speaker}: ${block.text}`;
    if (block.kind === 'command') {
      const args = block.command === 'enter' ? serializeEnterArgs(block.args) : block.args;
      return `|${block.command}${args.length ? ` ${args.map(quoteArg).join(' ')}` : ''}`;
    }
    if (block.kind === 'setting') return `${block.key}: ${block.value}`;
    return block.text;
  }).join('\n');
}

export function parseEnterArgs(args: string[]): EnterCommandOptions {
  const assetKey = args[0] ?? '';
  const options: EnterCommandOptions = {
    assetKey,
    x: args[1] ?? '',
    y: args[2] ?? '',
    expression: 'Normal',
    displayName: '',
    legacy: false,
  };
  const optional = args.slice(3);
  if (optional.length === 0) return options;

  if (tryApplyEnterKeyValueOptions(optional, options)) {
    return options;
  }

  options.legacy = true;
  options.expression = optional[0] || 'Normal';
  if (optional[1]) options.displayName = characterLabel(optional[1]);
  return options;
}

export function buildEnterArgs(options: Partial<EnterCommandOptions>) {
  const assetKey = options.assetKey ?? '';
  const args = [assetKey, options.x ?? '', options.y ?? ''];
  const expression = options.expression?.trim() ?? '';
  const displayName = options.displayName?.trim() ?? '';

  if (expression && expression !== 'Normal') args.push('expression', expression);
  if (displayName) args.push('displayname', displayName);
  return args;
}

export function serializeEnterArgs(args: string[]) {
  const options = parseEnterArgs(args);
  return buildEnterArgs(options);
}

export function isLegacyEnterArgs(args: string[]) {
  return parseEnterArgs(args).legacy;
}

function tryApplyEnterKeyValueOptions(optional: string[], options: EnterCommandOptions) {
  if (optional.length % 2 !== 0) return false;

  for (let i = 0; i < optional.length; i += 2) {
    const key = normalizeEnterOptionKey(optional[i]);
    const value = optional[i + 1];
    if (!['displayname', 'expression'].includes(key)) return false;

    if (key === 'displayname') options.displayName = value;
    if (key === 'expression') options.expression = value;
  }

  return true;
}

function normalizeEnterOptionKey(key: string) {
  const normalized = key.replace(/[-_]/g, '').toLowerCase();
  return normalized === 'name' ? 'displayname' : normalized;
}

export function parseSceneSetup(line: string): SceneSetupCharacter[] {
  return line.replace(/^\|/, '').split(',').map((entry) => {
    const parts = tokenizeArgs(entry.trim());
    const options = parseCharacterDisplayOptions(parts);
    return {
      id: crypto.randomUUID(),
      character: parts[0] ?? '',
      x: parts[1] ?? '0.5',
      y: parts[2] ?? '0',
      displayName: options.displayName,
      expression: options.expression,
    };
  }).filter((entry) => entry.character.trim());
}

export function serializeSceneSetup(characters: SceneSetupCharacter[]) {
  return `|${characters.map((entry) => {
    const args = [entry.character, entry.x, entry.y];
    if (entry.expression && entry.expression !== 'Normal') args.push('expression', entry.expression);
    if (entry.displayName) args.push('displayname', entry.displayName);
    return args.map(quoteArg).join(' ');
  }).join(',')}`;
}

function parseCharacterDisplayOptions(parts: string[]) {
  const options = { expression: 'Normal', displayName: '' };
  const optional = parts.slice(3);
  if (optional.length === 0) return options;
  if (optional.length % 2 !== 0) {
    options.expression = optional[0] || 'Normal';
    if (optional[1]) options.displayName = characterLabel(optional[1]);
    return options;
  }

  for (let i = 0; i < optional.length; i += 2) {
    const key = normalizeEnterOptionKey(optional[i]);
    const value = optional[i + 1];
    if (key === 'expression') options.expression = value;
    if (key === 'displayname') options.displayName = value;
  }

  return options;
}

export function activeCharactersAt(blocks: ScriptBlock[], index: number) {
  const active = new Set<string>();
  for (let i = 0; i < index; i += 1) {
    const block = blocks[i];
    if (block.kind === 'sceneSetup') {
      block.characters.forEach((entry) => {
        if (entry.character.trim()) active.add(entry.character.trim());
      });
    }
    if (block.kind !== 'command') continue;
    if (block.command === 'enter') {
      const entered = parseEnterArgs(block.args).assetKey;
      if (entered) active.add(entered);
    }
    if (['translate', 'move', 'moveTo', 'setExpression', 'setDisplayName'].includes(block.command) && block.args[0]) {
      active.add(block.args[0]);
    }
    if (block.command === 'exit' && block.args[0]) active.delete(block.args[0]);
  }
  return Array.from(active).sort((a, b) => a.localeCompare(b));
}

export function newBlock(kind: ScriptBlock['kind'], beatType: BeatType): ScriptBlock {
  const id = crypto.randomUUID();
  if (kind === 'sceneSetup') return { id, kind, characters: [{ id: crypto.randomUUID(), character: 'main', x: '0.5', y: '0', displayName: '', expression: 'Normal' }], raw: '' };
  if (kind === 'dialogue') return { id, kind, speaker: 'main', text: '', raw: '' };
  if (kind === 'command') return { id, kind, command: beatType === 'dialogue' ? 'translate' : 'enemy', args: [], raw: '' };
  if (kind === 'setting') return { id, kind, key: beatType === 'combat' ? 'enemy' : 'note', value: '', raw: '' };
  if (kind === 'blank') return { id, kind, raw: '' };
  return { id, kind: 'text', text: '', raw: '' };
}

function characterLabel(character: string) {
  if (!character) return '';
  return `${character[0].toUpperCase()}${character.slice(1)}`;
}

export function commandLabel(command: string) {
  const labels: Record<string, string> = {
    enter: 'Enter',
    translate: 'Place',
    move: 'Move',
    moveTo: 'Move To',
    exit: 'Exit',
    setExpression: 'Expression',
    setDisplayName: 'Display Name',
    swap: 'Swap',
    setBackground: 'Background',
    transition: 'Transition',
    playMusic: 'Music',
    playSound: 'Sound',
    display: 'Display',
    displayCG: 'Display CG',
    stopDisplay: 'Hide Display',
    stopDisplayCG: 'Hide CG',
    combat: 'Combat',
    combatDirect: 'Direct Combat',
    resumeCombat: 'Resume Combat',
    setPrefValue: 'Flag',
    setFlag: 'Save Flag',
  };
  return labels[command] ?? command;
}

export function expressionOptions(assets: AssetCatalog, character: string) {
  return assets.characterExpressions[character] ?? [];
}
