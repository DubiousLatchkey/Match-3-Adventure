import { memo, useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  Background,
  Controls,
  MiniMap,
  ReactFlow,
  Handle,
  Position,
  useReactFlow,
  useEdgesState,
  useNodesState,
} from '@xyflow/react';
import type { Connection, Edge, Node, NodeProps, Viewport } from '@xyflow/react';
import {
  AlertTriangle,
  BookOpen,
  ChevronLeft,
  ChevronRight,
  Eye,
  FolderOpen,
  GitFork,
  GripVertical,
  Plus,
  RefreshCw,
  Save,
  Search,
  Swords,
  Trash2,
  X,
} from 'lucide-react';
import { createBeat, deleteBeat, loadProject, pickUnityProject, renameBeatFile, saveBeat, saveStoryFlow, saveVisualData } from './fileSystem';
import type { LoadedProject } from './fileSystem';
import { setTransitionLine, targetBeatId, validateBeats } from './storyModel';
import type { AssetCatalog, BeatType, StoryBeat, VisualData } from './storyModel';
import {
  activeCharactersAt,
  buildEnterArgs,
  commandLabel,
  expressionOptions,
  isLegacyEnterArgs,
  newBlock,
  parseBlocks,
  parseEnterArgs,
  serializeBlocks,
} from './scriptBlocks';
import type { SceneSetupCharacter, ScriptBlock } from './scriptBlocks';

const commandSpecs = [
  { name: 'enter', label: 'Enter', template: '|enter {character} {x} {y}', category: 'characters' },
  { name: 'translate', label: 'Place', template: '|translate {character} {x} {y}', category: 'characters' },
  { name: 'move', label: 'Move', template: '|move {character} 8 {x} {y} 1', category: 'characters' },
  { name: 'moveTo', label: 'Move To', template: '|moveTo {character} 8 {x} {y} 1', category: 'characters' },
  { name: 'exit', label: 'Exit', template: '|exit {character} right', category: 'characters' },
  { name: 'setExpression', label: 'Expression', template: '|setExpression {character} Normal', category: 'characters' },
  { name: 'setDisplayName', label: 'Display Name', template: '|setDisplayName {character} "Display Name"', category: 'characters' },
  { name: 'swap', label: 'Swap', template: '|swap {character} {character}', category: 'characters' },
  { name: 'setBackground', label: 'Background', template: '|setBackground {background}', category: 'backgrounds' },
  { name: 'transition', label: 'Transition', template: '|transition {background}', category: 'backgrounds' },
  { name: 'playMusic', label: 'Music', template: '|playMusic {music}', category: 'music' },
  { name: 'playSound', label: 'Sound', template: '|playSound {sound}', category: 'sounds' },
  { name: 'display', label: 'Display', template: '|display {display}', category: 'displays' },
  { name: 'stopDisplay', label: 'Hide Display', template: '|stopDisplay', category: 'displays' },
  { name: 'setPrefValue', label: 'Flag', template: '|setPrefValue "Flag Name" 1', category: null },
  { name: 'setFlag', label: 'Save Flag', template: '|setFlag "Flag Name" 1', category: null },
] as const;

const stagePresets = [
  { label: '2 left', x: '0.35' },
  { label: '2 right', x: '0.65' },
  { label: '3 left', x: '0.25' },
  { label: '3 center', x: '0.5' },
  { label: '3 right', x: '0.75' },
  { label: '4 far left', x: '0.2' },
  { label: '4 left', x: '0.4' },
  { label: '4 right', x: '0.6' },
  { label: '4 far right', x: '0.8' },
  { label: '5 far left', x: '0.16' },
  { label: '5 left', x: '0.33' },
  { label: '5 center', x: '0.5' },
  { label: '5 right', x: '0.67' },
  { label: '5 far right', x: '0.84' },
  { label: 'off left', x: '-0.2' },
  { label: 'off right', x: '1.2' },
];

type StoryNodeData = {
  beat: StoryBeat;
  onDelete: (beat: StoryBeat) => void;
};

const StoryBeatNode = memo(function StoryBeatNode({ data }: NodeProps<Node<StoryNodeData>>) {
  const { beat, onDelete } = data;

  return (
    <div className={`story-node ${beat.type}`}>
      <Handle className="story-node-target-handle" type="target" position={Position.Top} />
      <div className="node-title">
        {beat.type === 'dialogue' ? <BookOpen size={15} /> : <Swords size={15} />}
        <span>{beat.name}</span>
        <button
          type="button"
          className="node-delete-button nodrag nopan"
          aria-label={`Delete ${beat.name}`}
          title="Delete beat"
          onClick={(event) => {
            event.stopPropagation();
            onDelete(beat);
          }}
        >
          <Trash2 size={13} />
        </button>
      </div>
      {(beat.transition || beat.type === 'combat') && (
        <div className={`node-subtitle ${!beat.transition ? 'node-subtitle--warn' : ''}`}>
          {beat.transition ? `-> ${beat.transition}` : 'no transition'}
        </div>
      )}
      {beat.issues.length > 0 && (
        <div className="node-warning">{beat.issues.length} issue{beat.issues.length === 1 ? '' : 's'}</div>
      )}
      <Handle className="story-node-source-handle" type="source" position={Position.Bottom} />
    </div>
  );
});

function nodePosition(index: number, beat: StoryBeat, visualData: VisualData) {
  const saved = visualData.nodes[beat.id];
  if (saved) return saved;
  const column = beat.type === 'dialogue' ? 0 : 1;
  return {
    x: 80 + column * 430 + Math.floor(index / 16) * 900,
    y: 80 + (index % 16) * 120,
  };
}

function makeNodes(beats: StoryBeat[], visualData: VisualData, onDelete: (beat: StoryBeat) => void): Node[] {
  return beats.map((beat, index) => ({
    id: beat.id,
    type: 'storyBeat',
    position: nodePosition(index, beat, visualData),
    data: {
      beat,
      onDelete,
    },
  }));
}

function makeEdges(beats: StoryBeat[]): Edge[] {
  const ids = new Set(beats.map((beat) => beat.id));
  return beats.flatMap((beat) => {
    const target = targetBeatId(beat.type, beat.transition);
    if (!target || !ids.has(target)) return [];
    return [{
      id: `${beat.id}->${target}`,
      source: beat.id,
      target,
      animated: beat.type === 'dialogue',
      label: beat.transition ?? undefined,
    }];
  });
}

function replaceBeat(beats: StoryBeat[], next: StoryBeat, rootDialogueNames: string[] = []) {
  return validateBeats(beats.map((beat) => (beat.id === next.id ? next : beat)), rootDialogueNames);
}

function targetOptions(beats: StoryBeat[], type: BeatType) {
  const targetType = type === 'dialogue' ? 'combat' : 'dialogue';
  return beats.filter((beat) => beat.type === targetType).sort((a, b) => a.name.localeCompare(b.name));
}

function lastSpeakerBefore(blocks: ScriptBlock[], index: number): string | null {
  for (let i = index - 1; i >= 0; i--) {
    const b = blocks[i];
    if (b.kind === 'dialogue' && b.speaker.trim()) return b.speaker.trim();
  }
  return null;
}

function preferredCharacterAt(blocks: ScriptBlock[], index: number, assets: AssetCatalog) {
  const lastSpeaker = lastSpeakerBefore(blocks, index);
  if (lastSpeaker) return lastSpeaker;
  const active = activeCharactersAt(blocks, index);
  return active[0] ?? assets.characters[0] ?? 'main';
}

function cloneBlock(block: ScriptBlock): ScriptBlock {
  const id = crypto.randomUUID();
  if (block.kind === 'sceneSetup') {
    return {
      ...block,
      id,
      characters: block.characters.map((entry) => ({ ...entry, id: crypto.randomUUID() })),
    };
  }
  return { ...block, id };
}

function editableTextForBeat(beat: StoryBeat) {
  const lines = beat.text.split(/\r?\n/);
  if (beat.type === 'combat') {
    return lines.filter((line, index) => (
      index !== beat.transitionLineIndex &&
      !line.trim().match(/^dialogue\s*:/i)
    )).join('\n');
  }
  if (beat.transitionLineIndex === null) return beat.text;
  return lines.filter((_, index) => index !== beat.transitionLineIndex).join('\n');
}

function composeBeatText(beat: StoryBeat, blocks: ScriptBlock[], directCombat: boolean) {
  const body = serializeBlocks(blocks);
  const bodyBeat = { ...beat, text: body, transitionLineIndex: null };
  return setTransitionLine(bodyBeat, beat.transition, directCombat);
}

function initialEditableBlocks(beat: StoryBeat) {
  const parsed = parseBlocks(editableTextForBeat(beat)).filter((block) => block.kind !== 'blank');
  if (beat.type === 'dialogue' && parsed[0]?.kind !== 'sceneSetup') {
    return [newBlock('sceneSetup', 'dialogue'), ...parsed];
  }
  return parsed;
}

type StagePosition = { x: number; y: number };

function parseStagePosition(args: string[], xIndex: number, yIndex: number, fallback?: StagePosition) {
  const x = Number.parseFloat(args[xIndex] ?? '');
  const y = Number.parseFloat(args[yIndex] ?? '');
  const nextX = Number.isFinite(x) ? x : fallback?.x;
  const nextY = Number.isFinite(y) ? y : fallback?.y;
  if (nextX === undefined || nextY === undefined) return null;
  return { x: nextX, y: nextY };
}

function enteredCharacterKey(args: string[]) {
  return parseEnterArgs(args).assetKey.trim();
}

type DialogueCharacterState = StagePosition & {
  displayName: string;
  expression: string;
};

type ScenePreviewCharacter = DialogueCharacterState & {
  key: string;
};

type ScenePreviewState = {
  background: string | null;
  characters: ScenePreviewCharacter[];
  speaker: string;
  text: string;
};

function applySceneSetup(characters: Map<string, DialogueCharacterState>, entries: SceneSetupCharacter[]) {
  characters.clear();
  entries.forEach((entry) => {
    const x = Number.parseFloat(entry.x);
    const y = Number.parseFloat(entry.y);
    if (entry.character.trim() && Number.isFinite(x) && Number.isFinite(y)) {
      characters.set(entry.character.trim(), {
        x,
        y,
        displayName: entry.displayName,
        expression: entry.expression || 'Normal',
      });
    }
  });
}

function applyPreviewCommand(characters: Map<string, DialogueCharacterState>, block: Extract<ScriptBlock, { kind: 'command' }>) {
  if (block.command === 'enter') {
    const character = enteredCharacterKey(block.args);
    if (!character) return;
    const enter = parseEnterArgs(block.args);
    const current = characters.get(character);
    const position = parseStagePosition(block.args, 1, 2, current);
    if (position) {
      characters.set(character, {
        ...position,
        displayName: enter.displayName,
        expression: enter.expression || 'Normal',
      });
    }
    return;
  }

  const character = block.args[0]?.trim();
  if (!character) return;

  if (block.command === 'setDisplayName') {
    const current = characters.get(character);
    if (current) characters.set(character, { ...current, displayName: block.args.slice(1).join(' ') });
    return;
  }

  if (block.command === 'setExpression') {
    const current = characters.get(character);
    if (current) characters.set(character, { ...current, expression: block.args[1] || 'Normal' });
    return;
  }

  if (block.command === 'translate') {
    const current = characters.get(character);
    const position = parseStagePosition(block.args, 1, 2, current);
    if (position) characters.set(character, { ...(current ?? defaultCharacterState()), ...position });
    return;
  }

  if (block.command === 'moveTo') {
    const current = characters.get(character);
    const position = parseStagePosition(block.args, 2, 3, current);
    if (position) characters.set(character, { ...(current ?? defaultCharacterState()), ...position });
    return;
  }

  if (block.command === 'move') {
    const dx = Number.parseFloat(block.args[2] ?? '');
    const dy = Number.parseFloat(block.args[3] ?? '');
    const current = characters.get(character);
    if (current && Number.isFinite(dx) && Number.isFinite(dy)) {
      characters.set(character, { ...current, x: current.x + dx, y: current.y + dy });
    }
    return;
  }

  if (block.command === 'exit') {
    characters.delete(character);
    return;
  }

  if (block.command === 'swap') {
    const other = block.args[1]?.trim();
    if (!other) return;
    const firstPosition = characters.get(character);
    const secondPosition = characters.get(other);
    if (firstPosition && secondPosition) {
      characters.set(character, { ...firstPosition, x: secondPosition.x, y: secondPosition.y });
      characters.set(other, { ...secondPosition, x: firstPosition.x, y: firstPosition.y });
    }
  }
}

function scenePreviewAt(blocks: ScriptBlock[], index: number): ScenePreviewState {
  const characters = new Map<string, DialogueCharacterState>();
  let background: string | null = null;
  let speaker = '';
  let text = '';

  for (let i = 0; i <= index; i += 1) {
    const block = blocks[i];
    if (!block) continue;
    if (block.kind === 'sceneSetup') applySceneSetup(characters, block.characters);
    if (block.kind === 'command') {
      if (['setBackground', 'transition'].includes(block.command)) {
        const nextBackground = block.args[0]?.trim();
        if (nextBackground) background = nextBackground;
      }
      applyPreviewCommand(characters, block);
    }
    if (block.kind === 'dialogue') {
      speaker = block.speaker.trim();
      text = block.text;
    }
  }

  return {
    background,
    speaker,
    text,
    characters: Array.from(characters.entries()).map(([key, state]) => ({ key, ...state })),
  };
}

function finalDialogueState(beat: StoryBeat) {
  const characters = new Map<string, DialogueCharacterState>();

  for (const block of parseBlocks(editableTextForBeat(beat))) {
    if (block.kind === 'sceneSetup') {
      applySceneSetup(characters, block.characters);
    }

    if (block.kind !== 'command') continue;
    applyPreviewCommand(characters, block);
  }

  return characters;
}

function defaultCharacterState(): DialogueCharacterState {
  return { x: 0.5, y: 0, displayName: '', expression: 'Normal' };
}

function finalSceneSetupFromDialogue(beat: StoryBeat): SceneSetupCharacter[] {
  return Array.from(finalDialogueState(beat).entries())
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([character, state]) => ({
      id: crypto.randomUUID(),
      character,
      x: Number(state.x.toFixed(3)).toString(),
      y: Number(state.y.toFixed(3)).toString(),
      displayName: state.displayName,
      expression: state.expression,
    }));
}

function finalBackgroundFromDialogue(beat: StoryBeat) {
  let background: string | null = null;

  for (const block of parseBlocks(editableTextForBeat(beat))) {
    if (block.kind !== 'command') continue;
    if (['setBackground', 'transition'].includes(block.command)) {
      const nextBackground = block.args[0]?.trim();
      if (nextBackground) background = nextBackground;
    }
  }

  return background;
}

function upstreamDialogueFor(beat: StoryBeat, beats: StoryBeat[]) {
  if (beat.type !== 'dialogue') return null;
  const incoming = beats.filter((candidate) => targetBeatId(candidate.type, candidate.transition) === beat.id);
  const incomingCombat = incoming.find((candidate) => candidate.type === 'combat');
  if (incomingCombat) {
    return beats.find((candidate) => (
      candidate.type === 'dialogue' &&
      targetBeatId(candidate.type, candidate.transition) === incomingCombat.id
    )) ?? null;
  }

  return incoming.find((candidate) => candidate.type === 'dialogue') ?? null;
}

type EditorProps = {
  beat: StoryBeat;
  beats: StoryBeat[];
  assets: AssetCatalog;
  onClose: () => void;
  onChange: (beat: StoryBeat) => void;
  onSave: (beat: StoryBeat) => Promise<void>;
  onDelete: (beat: StoryBeat) => Promise<void>;
  onRename: (beat: StoryBeat, newName: string) => Promise<void>;
  onAddPlaceholder: (category: keyof AssetCatalog, value: string) => void;
};

function BeatEditor({ beat, beats, assets, onClose, onChange, onSave, onDelete, onRename, onAddPlaceholder }: EditorProps) {
  const [draft, setDraft] = useState(beat);
  const [blocks, setBlocks] = useState<ScriptBlock[]>(() => initialEditableBlocks(beat));
  const [saving, setSaving] = useState(false);
  const [isDirty, setIsDirty] = useState(false);
  const [placeholder, setPlaceholder] = useState('');
  const [renameValue, setRenameValue] = useState(beat.name);
  const [placeholderCategory, setPlaceholderCategory] = useState<keyof Pick<AssetCatalog, 'characters' | 'backgrounds' | 'music' | 'sounds' | 'displays'>>('characters');
  const [directCombat, setDirectCombat] = useState(() =>
    beat.type === 'dialogue' && /\|combatDirect\s+/i.test(beat.text)
  );
  const [dragIndex, setDragIndex] = useState<number | null>(null);
  const [pendingFocus, setPendingFocus] = useState<{ id: string; field: 'speaker' | 'text' } | null>(null);
  const [stickyPreviewIndex, setStickyPreviewIndex] = useState<number | null>(null);
  const options = targetOptions(beats, draft.type);

  const updateText = (text: string) => {
    const reparsed = { ...draft, text, transition: null, transitionLineIndex: null };
    const candidate = { ...reparsed, ...parseTransition(draft.type, text) };
    const validated = validateBeats(beats.map((beat) => beat.id === candidate.id ? candidate : beat));
    const parsed = validated.find((beat) => beat.id === candidate.id) ?? candidate;
    setDraft(parsed);
    setIsDirty(true);
    onChange(parsed);
  };

  const updateBlocks = (nextBlocks: ScriptBlock[]) => {
    setBlocks(nextBlocks);
    updateText(composeBeatText(draft, nextBlocks, directCombat));
  };

  const patchBlock = (index: number, next: ScriptBlock) => {
    updateBlocks(blocks.map((block, i) => (i === index ? next : block)));
  };

  const insertBlock = (index: number, kind: ScriptBlock['kind']) => {
    const block = newBlock(kind, draft.type);
    const preferredCharacter = preferredCharacterAt(blocks, index, assets);
    if (kind === 'dialogue' && block.kind === 'dialogue') {
      const active = activeCharactersAt(blocks, index);
      const last = lastSpeakerBefore(blocks, index);
      block.speaker = active.find((c) => c !== last) ?? active[0] ?? block.speaker;
    }
    if (kind === 'command' && block.kind === 'command') {
      block.args = defaultCommandArgs(block.command, assets, preferredCharacter);
    }
    updateBlocks([...blocks.slice(0, index), block, ...blocks.slice(index)]);
    return block.id;
  };

  const duplicateBlockAbove = (index: number) => {
    if (index <= 0) return;
    updateBlocks([...blocks.slice(0, index), cloneBlock(blocks[index - 1]), ...blocks.slice(index)]);
  };

  const insertDialogueFromEnter = (index: number) => {
    const focusField = activeCharactersAt(blocks, index).length > 2 ? 'speaker' : 'text';
    const id = insertBlock(index, 'dialogue');
    setPendingFocus({ id, field: focusField });
  };

  const moveBlock = (from: number, to: number) => {
    const target = Math.max(0, Math.min(blocks.length - 1, to));
    if (from === target) return;
    const next = [...blocks];
    const [moved] = next.splice(from, 1);
    next.splice(target, 0, moved);
    updateBlocks(next);
  };

  const deleteBlockAt = (index: number) => {
    updateBlocks(blocks.filter((_, i) => i !== index));
  };

  const setupFromPreviousDialogue = () => {
    const previousDialogue = upstreamDialogueFor(draft, beats);
    if (!previousDialogue) {
      window.alert('No previous dialogue found in this chain.');
      return;
    }

    const setupCharacters = finalSceneSetupFromDialogue(previousDialogue);
    if (setupCharacters.length === 0) {
      window.alert(`No active characters found at the end of ${previousDialogue.name}.`);
      return;
    }

    const setupBlock: ScriptBlock = {
      id: crypto.randomUUID(),
      kind: 'sceneSetup',
      characters: setupCharacters,
      raw: '',
    };
    const previousBackground = finalBackgroundFromDialogue(previousDialogue);
    const rest = blocks[0]?.kind === 'sceneSetup' ? blocks.slice(1) : blocks;
    if (!previousBackground) {
      updateBlocks([setupBlock, ...rest]);
      return;
    }

    const backgroundBlock: ScriptBlock = {
      id: crypto.randomUUID(),
      kind: 'command',
      command: 'setBackground',
      args: [previousBackground],
      raw: '',
    };
    if (rest[0]?.kind === 'command' && rest[0].command === 'setBackground') {
      updateBlocks([setupBlock, { ...rest[0], args: [previousBackground] }, ...rest.slice(1)]);
      return;
    }

    updateBlocks([setupBlock, backgroundBlock, ...rest]);
  };

  const commitTransition = (targetName: string, dc = directCombat) => {
    const text = setTransitionLine({ ...draft, text: serializeBlocks(blocks), transitionLineIndex: null }, targetName || null, dc);
    updateText(text);
  };

  const toggleDirectCombat = () => {
    const next = !directCombat;
    setDirectCombat(next);
    if (draft.transition) commitTransition(draft.transition, next);
  };

  const save = useCallback(async () => {
    setSaving(true);
    const migratedText = composeBeatText(draft, blocks, directCombat);
    const migratedBeat = {
      ...draft,
      text: migratedText,
      transitionLineIndex: parseTransition(draft.type, migratedText).transitionLineIndex,
    };
    setDraft(migratedBeat);
    await onSave(migratedBeat);
    setIsDirty(false);
    setSaving(false);
  }, [blocks, directCombat, draft, onSave]);

  const saveRef = useRef(save);
  saveRef.current = save;

  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key === 's') {
        e.preventDefault();
        saveRef.current();
      }
    };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, []);

  useEffect(() => {
    if (!pendingFocus) return;
    requestAnimationFrame(() => {
      const selector = `[data-block-id="${pendingFocus.id}"][data-focus-field="${pendingFocus.field}"]`;
      const element = document.querySelector<HTMLInputElement | HTMLTextAreaElement>(selector);
      element?.focus();
      if (element instanceof HTMLTextAreaElement || element instanceof HTMLInputElement) {
        element.setSelectionRange(element.value.length, element.value.length);
      }
      setPendingFocus(null);
    });
  }, [blocks, pendingFocus]);

  const handleClose = () => {
    if (isDirty && !window.confirm('Close without saving?')) return;
    onClose();
  };

  return (
    <div className="editor-screen">
      <header className="editor-header">
        <div>
          <div className="eyebrow">{draft.type}</div>
          <h1>{isDirty ? `• ${draft.name}` : draft.name}</h1>
          <p>{draft.relativePath}</p>
        </div>
        <div className="editor-actions">
          <input
            className="rename-input"
            value={renameValue}
            onChange={(event) => setRenameValue(event.target.value)}
            aria-label="Beat name"
          />
          <button type="button" onClick={() => onRename(draft, renameValue)} disabled={!renameValue.trim() || renameValue.trim() === draft.name}>
            Rename
          </button>
          {draft.type === 'dialogue' && (
            <button type="button" onClick={setupFromPreviousDialogue}>
              Setup From Previous
            </button>
          )}
          <button type="button" onClick={save} disabled={saving}>
            <Save size={17} />{saving ? 'Saving…' : 'Save'}
          </button>
          <button type="button" onClick={() => onDelete(draft)}><Trash2 size={17} />Delete</button>
          <button type="button" onClick={handleClose}><X size={18} /></button>
        </div>
      </header>

      <main className="editor-layout">
        <section className="script-pane">
          <div className="block-list">
            <InsertRow beatType={draft.type} allowSceneSetup onInsert={(kind) => insertBlock(0, kind)} />
            {blocks.map((block, index) => (
              <div key={block.id} className="block-with-insert">
                <ScriptBlockEditor
                  block={block}
                  index={index}
                  beatType={draft.type}
                  activeCharacters={activeCharactersAt(blocks, index)}
                  assets={assets}
                  onChange={(next) => patchBlock(index, next)}
                  onDelete={() => deleteBlockAt(index)}
                  onEnterKey={block.kind === 'dialogue' ? () => insertDialogueFromEnter(index + 1) : undefined}
                  onDragStart={() => setDragIndex(index)}
                  onDrop={() => {
                    if (dragIndex !== null) moveBlock(dragIndex, index);
                    setDragIndex(null);
                  }}
                  onDragEnd={() => setDragIndex(null)}
                  previewState={draft.type === 'dialogue' ? scenePreviewAt(blocks, index) : null}
                  previewSticky={stickyPreviewIndex === index}
                  onTogglePreview={() => setStickyPreviewIndex(stickyPreviewIndex === index ? null : index)}
                  onClosePreview={() => setStickyPreviewIndex(null)}
                />
                <InsertRow
                  beatType={draft.type}
                  canDuplicate={index >= 0}
                  onDuplicateAbove={() => duplicateBlockAbove(index + 1)}
                  onInsert={(kind) => insertBlock(index + 1, kind)}
                />
              </div>
            ))}
          </div>
        </section>
      </main>

      <footer className="editor-footer">
        <div className="footer-section">
          <span className="footer-label">Transition</span>
          <select value={draft.transition ?? ''} onChange={(e) => commitTransition(e.target.value)}>
            <option value="">None</option>
            {options.map((o) => <option key={o.id} value={o.name}>{o.name}</option>)}
          </select>
          {draft.type === 'dialogue' && (
            <label className="direct-combat-toggle">
              <input type="checkbox" checked={directCombat} onChange={toggleDirectCombat} />
              Direct
            </label>
          )}
          {draft.issues.map((issue) => (
            <span className="footer-issue" key={issue}><AlertTriangle size={14} />{issue}</span>
          ))}
        </div>
        <div className="footer-divider" />
        <div className="footer-section">
          <span className="footer-label">Placeholder</span>
          <select value={placeholderCategory} onChange={(e) => setPlaceholderCategory(e.target.value as typeof placeholderCategory)}>
            <option value="characters">Character</option>
            <option value="backgrounds">Background</option>
            <option value="music">Music</option>
            <option value="sounds">Sound</option>
            <option value="displays">Display</option>
          </select>
          <input value={placeholder} onChange={(e) => setPlaceholder(e.target.value)} placeholder="value" />
          <button type="button" onClick={() => {
            if (!placeholder.trim()) return;
            onAddPlaceholder(placeholderCategory, placeholder.trim());
            setPlaceholder('');
          }}>Add</button>
        </div>
      </footer>
    </div>
  );
}

function InsertRow({
  beatType,
  allowSceneSetup = false,
  canDuplicate = false,
  onDuplicateAbove,
  onInsert,
}: {
  beatType: BeatType;
  allowSceneSetup?: boolean;
  canDuplicate?: boolean;
  onDuplicateAbove?: () => void;
  onInsert: (kind: ScriptBlock['kind']) => void;
}) {
  return (
    <div className="insert-row">
      <div className="insert-row-line" />
      <div className="insert-row-buttons">
        {canDuplicate && <button type="button" onClick={onDuplicateAbove}><Plus size={11} />Duplicate Above</button>}
        {beatType === 'dialogue' && allowSceneSetup && <button type="button" onClick={() => onInsert('sceneSetup')}><Plus size={11} />Setup</button>}
        {beatType === 'dialogue' ? (
          <>
            <button type="button" onClick={() => onInsert('dialogue')}><Plus size={11} />Dialogue</button>
            <button type="button" onClick={() => onInsert('command')}><Plus size={11} />Command</button>
          </>
        ) : (
          <button type="button" onClick={() => onInsert('setting')}><Plus size={11} />Combat Setting</button>
        )}
      </div>
    </div>
  );
}

function defaultCommandArgs(command: string, assets: AssetCatalog, preferredCharacter?: string) {
  const character = preferredCharacter?.trim() || assets.characters[0] || 'main';
  const background = assets.backgrounds[0] ?? 'backgroundName';
  if (command === 'enter' || command === 'translate') return [character, '0.5', '0'];
  if (command === 'move' || command === 'moveTo') return [character, '8', '0.5', '0', '1'];
  if (command === 'exit') return [character, 'right'];
  if (command === 'setExpression') return [character, 'Normal'];
  if (command === 'setDisplayName') return [character, characterLabel(character)];
  if (command === 'swap') return [character, assets.characters[1] ?? 'otherCharacter'];
  if (command === 'setBackground' || command === 'transition') return [background];
  if (command === 'playMusic') return [assets.music[0] ?? 'Track Name'];
  if (command === 'playSound') return [assets.sounds[0] ?? 'soundName'];
  if (command === 'display' || command === 'displayCG') return [assets.displays[0] ?? 'resourcePath'];
  if (command === 'combat' || command === 'combatDirect') return ['combatName'];
  if (command === 'setPrefValue' || command === 'setFlag') return ['Flag Name', '1'];
  return [];
}

function characterLabel(character: string) {
  if (!character) return '';
  return `${character[0].toUpperCase()}${character.slice(1)}`;
}

function InputWithOptions({ value, options, onChange, placeholder }: { value: string; options: string[]; onChange: (value: string) => void; placeholder?: string }) {
  const listId = `list-${placeholder ?? 'value'}-${options.length}`;
  return (
    <>
      <input value={value} list={listId} placeholder={placeholder} onChange={(event) => onChange(event.target.value)} />
      <datalist id={listId}>
        {options.map((option) => <option key={option} value={option} />)}
      </datalist>
    </>
  );
}

function AutoGrowTextarea({ value, onChange, placeholder, className, onEnterKey, blockId, focusField }: {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  className?: string;
  onEnterKey?: () => void;
  blockId?: string;
  focusField?: string;
}) {
  const resize = (target: HTMLTextAreaElement) => {
    target.style.height = '32px';
    if (target.scrollHeight > 34) {
      target.style.height = `${target.scrollHeight}px`;
    }
  };

  return (
    <textarea
      className={className}
      data-block-id={blockId}
      data-focus-field={focusField}
      value={value}
      rows={1}
      placeholder={placeholder}
      onKeyDown={(e) => {
        if (onEnterKey && e.key === 'Enter' && !e.shiftKey) {
          e.preventDefault();
          onEnterKey();
        }
      }}
      onInput={(event) => { resize(event.currentTarget); }}
      onChange={(event) => onChange(event.target.value)}
      ref={(element) => {
        if (!element) return;
        resize(element);
      }}
    />
  );
}

function ScriptBlockEditor({
  block,
  index,
  beatType,
  activeCharacters,
  assets,
  onChange,
  onDelete,
  onEnterKey,
  onDragStart,
  onDrop,
  onDragEnd,
  previewState,
  previewSticky,
  onTogglePreview,
  onClosePreview,
}: {
  block: ScriptBlock;
  index: number;
  beatType: BeatType;
  activeCharacters: string[];
  assets: AssetCatalog;
  onChange: (block: ScriptBlock) => void;
  onDelete: () => void;
  onEnterKey?: () => void;
  onDragStart: () => void;
  onDrop: () => void;
  onDragEnd: () => void;
  previewState: ScenePreviewState | null;
  previewSticky: boolean;
  onTogglePreview: () => void;
  onClosePreview: () => void;
}) {
  const characterOptions = Array.from(new Set([...activeCharacters, ...assets.characters])).sort();
  const isNarration = block.kind === 'dialogue' && !block.speaker.trim();
  const previewButtonRef = useRef<HTMLButtonElement | null>(null);
  const [previewPlacement, setPreviewPlacement] = useState<'below' | 'above'>('below');
  const marker = block.kind === 'command' && block.command === 'enter' && isLegacyEnterArgs(block.args)
    ? 'Legacy enter syntax. This will save as named options.'
    : '';
  const updateArg = (argIndex: number, value: string) => {
    if (block.kind !== 'command') return;
    const args = [...block.args];
    args[argIndex] = value;
    onChange({ ...block, args });
  };
  const updatePreviewPlacement = () => {
    const rect = previewButtonRef.current?.getBoundingClientRect();
    if (!rect) return;
    const previewHeight = 350;
    const spaceBelow = window.innerHeight - rect.bottom;
    const spaceAbove = rect.top;
    setPreviewPlacement(spaceBelow < previewHeight && spaceAbove > spaceBelow ? 'above' : 'below');
  };

  return (
    <article
      className={`script-block ${block.kind}${isNarration ? ' narration' : ''}${previewSticky ? ' preview-sticky' : ''}`}
      onDragOver={(event) => event.preventDefault()}
      onDrop={(event) => { event.preventDefault(); onDrop(); }}
    >
      <div className="block-toolbar">
        <button
          type="button"
          className="drag-handle"
          draggable
          aria-label="Drag to reorder"
          onDragStart={(event) => {
            event.dataTransfer.effectAllowed = 'move';
            onDragStart();
          }}
          onDragEnd={onDragEnd}
        >
          <GripVertical size={15} />
        </button>
        <span className="block-index">{index + 1}</span>
        {marker && <span className="block-marker">{marker}</span>}
      </div>

      {block.kind === 'dialogue' && (
        <div className="dialogue-block-grid">
          <SpeakerField blockId={block.id} value={block.speaker} options={characterOptions} onChange={(speaker) => onChange({ ...block, speaker })} />
          <AutoGrowTextarea blockId={block.id} focusField="text" value={block.text} onChange={(text) => onChange({ ...block, text })} placeholder="Dialogue text" onEnterKey={onEnterKey} />
        </div>
      )}

      {block.kind === 'sceneSetup' && (
        <SceneSetupEditor
          characters={block.characters}
          assets={assets}
          onChange={(characters) => onChange({ ...block, characters })}
        />
      )}

      {block.kind === 'command' && (
        <div className="command-block-grid">
          <select value={block.command} onChange={(event) => onChange({
            ...block,
            command: event.target.value,
            args: defaultCommandArgs(event.target.value, assets, block.args[0] || characterOptions[0]),
          })}>
            {commandSpecs.map((command) => <option key={command.name} value={command.name}>{commandLabel(command.name)}</option>)}
            <option value={block.command}>{commandLabel(block.command)}</option>
          </select>
          <CommandArgs
            block={block}
            assets={assets}
            characterOptions={characterOptions}
            updateArg={updateArg}
            replaceArgs={(args) => onChange({ ...block, args })}
          />
        </div>
      )}

      {block.kind === 'setting' && (
        <SettingBlockEditor
          block={block}
          beatType={beatType}
          assets={assets}
          onChange={(next) => onChange(next)}
        />
      )}

      {(block.kind === 'text' || block.kind === 'blank') && (
        <AutoGrowTextarea className="text-block-input" value={block.kind === 'text' ? block.text : ''} onChange={(text) => onChange({ id: block.id, kind: 'text', text, raw: text })} placeholder="Narration or raw text" />
      )}

      <button type="button" className="block-delete" onClick={onDelete} aria-label="Delete block">
        <Trash2 size={13} />
      </button>

      {previewState && (
        <div className="preview-anchor">
          <button
            ref={previewButtonRef}
            type="button"
            className="block-preview-button"
            onMouseEnter={updatePreviewPlacement}
            onFocus={updatePreviewPlacement}
            onClick={() => {
              updatePreviewPlacement();
              onTogglePreview();
            }}
            aria-pressed={previewSticky}
            aria-label="Preview scene"
            title="Preview scene"
          >
            <Eye size={14} />
          </button>
          <ScenePreviewPopout state={previewState} sticky={previewSticky} placement={previewPlacement} onClose={onClosePreview} />
        </div>
      )}
    </article>
  );
}

function ScenePreviewPopout({ state, sticky, placement, onClose }: { state: ScenePreviewState; sticky: boolean; placement: 'below' | 'above'; onClose: () => void }) {
  const speaker = state.speaker
    ? state.characters.find((character) => character.key === state.speaker)?.displayName || characterLabel(state.speaker)
    : '';

  return (
    <aside className={`scene-preview-popout is-${placement}${sticky ? ' is-sticky' : ''}`}>
      <button type="button" className="scene-preview-close" onClick={onClose} aria-label="Close preview">
        <X size={13} />
      </button>
      <div className="scene-preview-stage" aria-label="Scene preview">
        <div className="scene-preview-background">
          <div className="scene-preview-sky" />
          <div className="scene-preview-mountains" />
          <div className="scene-preview-ground" />
          {state.background && <span className="scene-preview-background-label">{state.background}</span>}
        </div>
        <div className="scene-preview-characters">
          {state.characters.map((character) => (
            <div
              key={character.key}
              className={`scene-preview-character${state.speaker === character.key ? ' is-speaking' : ''}`}
              style={{
                left: `${character.x * 100}%`,
                bottom: `calc(${character.y * 100}% - 70px)`,
              }}
            >
              <span>{character.displayName || characterLabel(character.key)}</span>
              {character.expression && character.expression !== 'Normal' && <em>{character.expression}</em>}
              <div className="scene-preview-portrait">
                <div className="scene-preview-person-head" />
                <div className="scene-preview-person-body" />
              </div>
            </div>
          ))}
        </div>
        <div className="scene-preview-log">Log</div>
        <div className="scene-preview-skip">Skip</div>
        <div className="scene-preview-dialogue">
          {speaker && <div className="scene-preview-name">{speaker}</div>}
          <p>{state.text || '...'}</p>
        </div>
      </div>
    </aside>
  );
}

function SettingBlockEditor({
  block,
  beatType,
  assets,
  onChange,
}: {
  block: Extract<ScriptBlock, { kind: 'setting' }>;
  beatType: BeatType;
  assets: AssetCatalog;
  onChange: (block: Extract<ScriptBlock, { kind: 'setting' }>) => void;
}) {
  const keyOptions = beatType === 'combat'
    ? ['enemy', 'weapons', 'solo', 'companion', 'tutorial']
    : ['note'];

  const valueOptions =
    block.key === 'enemy'
      ? assets.enemies
      : block.key === 'weapons'
        ? assets.weapons
        : block.key === 'companion'
          ? assets.characters
          : [];

  return (
    <div className="setting-block-grid">
      <InputWithOptions value={block.key} options={keyOptions} onChange={(key) => onChange({ ...block, key })} />
      <InputWithOptions value={block.value} options={valueOptions} placeholder={settingPlaceholder(block.key)} onChange={(value) => onChange({ ...block, value })} />
    </div>
  );
}

function settingPlaceholder(key: string) {
  if (key === 'enemy') return 'enemy or enemy, enemy';
  if (key === 'weapons') return 'weapon or weapon, weapon';
  if (key === 'solo') return 'solo arguments';
  if (key === 'companion') return 'character';
  if (key === 'tutorial') return 'tutorial id';
  return 'value';
}

function SceneSetupEditor({
  characters,
  assets,
  onChange,
}: {
  characters: SceneSetupCharacter[];
  assets: AssetCatalog;
  onChange: (characters: SceneSetupCharacter[]) => void;
}) {
  const patch = (id: string, next: Partial<SceneSetupCharacter>) => {
    onChange(characters.map((entry) => entry.id === id ? { ...entry, ...next } : entry));
  };
  const remove = (id: string) => {
    onChange(characters.filter((entry) => entry.id !== id));
  };
  const add = () => {
    onChange([...characters, { id: crypto.randomUUID(), character: assets.characters[0] ?? 'main', x: '0.5', y: '0', displayName: '', expression: 'Normal' }]);
  };

  return (
    <div className="scene-setup-block">
      <div className="scene-setup-title">Scene Setup</div>
      {characters.map((entry) => (
        <div className="scene-setup-row" key={entry.id}>
          <label className="setup-field setup-character-field">
            <span>Character</span>
            <InputWithOptions value={entry.character} options={assets.characters} placeholder="character" onChange={(character) => patch(entry.id, { character })} />
          </label>
          <PositionFields x={entry.x} y={entry.y} onXChange={(x) => patch(entry.id, { x })} onYChange={(y) => patch(entry.id, { y })} />
          <label className="setup-field setup-expression-field">
            <span>Expression</span>
            <InputWithOptions value={entry.expression} options={expressionOptions(assets, entry.character)} placeholder="expression" onChange={(expression) => patch(entry.id, { expression })} />
          </label>
          <label className="setup-field setup-display-field">
            <span>Display</span>
            <input value={entry.displayName} placeholder="display name" onChange={(event) => patch(entry.id, { displayName: event.target.value })} />
          </label>
          <button type="button" className="setup-remove-button" onClick={() => remove(entry.id)} aria-label={`Remove ${entry.character || 'character'}`}>
            <Trash2 size={14} />
          </button>
        </div>
      ))}
      <button type="button" className="scene-setup-add" onClick={add}><Plus size={13} />Character</button>
    </div>
  );
}

function CommandArgs({
  block,
  assets,
  characterOptions,
  updateArg,
  replaceArgs,
}: {
  block: Extract<ScriptBlock, { kind: 'command' }>;
  assets: AssetCatalog;
  characterOptions: string[];
  updateArg: (index: number, value: string) => void;
  replaceArgs: (args: string[]) => void;
}) {
  const arg = (index: number) => block.args[index] ?? '';
  if (block.command === 'enter') {
    const enter = parseEnterArgs(block.args);
    const updateEnter = (next: Partial<typeof enter>) => replaceArgs(buildEnterArgs({ ...enter, ...next }));
    const expressions = expressionOptions(assets, enter.assetKey);
    return (
      <>
        <InputWithOptions value={enter.assetKey} options={assets.characters} placeholder="character" onChange={(assetKey) => updateEnter({ assetKey })} />
        <PositionFields compact x={enter.x} y={enter.y} onXChange={(x) => updateEnter({ x })} onYChange={(y) => updateEnter({ y })} />
        <input value={enter.displayName} placeholder="display name" onChange={(event) => updateEnter({ displayName: event.target.value })} />
        <InputWithOptions value={enter.expression} options={expressions} placeholder="expression" onChange={(expression) => updateEnter({ expression })} />
        <div className="quick-chip-row">
          {expressions.map((expression) => (
            <button key={expression} type="button" onClick={() => updateEnter({ expression })}>{expression}</button>
          ))}
        </div>
      </>
    );
  }
  if (block.command === 'translate') {
    return (
      <>
        <InputWithOptions value={arg(0)} options={characterOptions} placeholder="character" onChange={(value) => updateArg(0, value)} />
        <PositionFields compact x={arg(1)} y={arg(2)} onXChange={(value) => updateArg(1, value)} onYChange={(value) => updateArg(2, value)} />
      </>
    );
  }
  if (['move', 'moveTo'].includes(block.command)) {
    return (
      <>
        <InputWithOptions value={arg(0)} options={characterOptions} placeholder="character" onChange={(value) => updateArg(0, value)} />
        <input value={arg(1)} placeholder="speed" onChange={(event) => updateArg(1, event.target.value)} />
        <PositionFields compact x={arg(2)} y={arg(3)} onXChange={(value) => updateArg(2, value)} onYChange={(value) => updateArg(3, value)} />
        <input value={arg(4)} placeholder="linked lines" onChange={(event) => updateArg(4, event.target.value)} />
      </>
    );
  }
  if (block.command === 'exit') {
    return (
      <>
        <InputWithOptions value={arg(0)} options={characterOptions} placeholder="character" onChange={(value) => updateArg(0, value)} />
        <InputWithOptions value={arg(1)} options={['left', 'right', 'down']} placeholder="direction" onChange={(value) => updateArg(1, value)} />
      </>
    );
  }
  if (block.command === 'setExpression') {
    const expressions = expressionOptions(assets, arg(0));
    return (
      <>
        <InputWithOptions value={arg(0)} options={characterOptions} placeholder="character" onChange={(value) => updateArg(0, value)} />
        <InputWithOptions value={arg(1)} options={expressions} placeholder="expression" onChange={(value) => updateArg(1, value)} />
        <div className="quick-chip-row">
          {expressions.map((expression) => (
            <button key={expression} type="button" onClick={() => updateArg(1, expression)}>{expression}</button>
          ))}
        </div>
      </>
    );
  }
  if (block.command === 'setDisplayName') {
    return (
      <>
        <InputWithOptions value={arg(0)} options={characterOptions} placeholder="character" onChange={(value) => updateArg(0, value)} />
        <input value={arg(1)} placeholder="display name" onChange={(event) => updateArg(1, event.target.value)} />
      </>
    );
  }
  if (block.command === 'swap') {
    return (
      <>
        <InputWithOptions value={arg(0)} options={characterOptions} placeholder="first" onChange={(value) => updateArg(0, value)} />
        <InputWithOptions value={arg(1)} options={characterOptions} placeholder="second" onChange={(value) => updateArg(1, value)} />
      </>
    );
  }
  const assetOptions = ['setBackground', 'transition'].includes(block.command)
    ? assets.backgrounds
    : block.command === 'playMusic'
      ? assets.music
      : block.command === 'playSound'
        ? assets.sounds
        : ['display', 'displayCG'].includes(block.command)
          ? assets.displays
          : [];
  if (assetOptions.length > 0) {
    return <InputWithOptions value={arg(0)} options={assetOptions} placeholder="asset" onChange={(value) => updateArg(0, value)} />;
  }
  if (block.command === 'setPrefValue' || block.command === 'setFlag') {
    return (
      <>
        <input value={block.args.slice(0, -1).join(' ')} placeholder="key" onChange={(event) => replaceArgs([event.target.value, arg(block.args.length - 1) || '1'])} />
        <input value={arg(block.args.length - 1)} placeholder="value" onChange={(event) => replaceArgs([...block.args.slice(0, -1), event.target.value])} />
      </>
    );
  }
  return <input value={block.args.join(' ')} placeholder="custom arguments" onChange={(event) => replaceArgs(event.target.value.split(/\s+/).filter(Boolean))} />;
}

function PositionFields({ x, y, compact = false, onXChange, onYChange }: { x: string; y: string; compact?: boolean; onXChange: (x: string) => void; onYChange: (y: string) => void }) {
  const selectedPreset = stagePresets.find((preset) => preset.x === x)?.label ?? '';
  if (compact) {
    return (
      <div className="position-control position-control--compact">
        <label className="compact-coordinate-field">
          <span>X</span>
          <input value={x} placeholder="0-1" onChange={(event) => onXChange(event.target.value)} />
        </label>
        <label className="compact-coordinate-field">
          <span>Y</span>
          <input value={y} placeholder="0-1" onChange={(event) => onYChange(event.target.value)} />
        </label>
        <select
          aria-label="Position preset"
          value={selectedPreset}
          onChange={(event) => {
            const preset = stagePresets.find((candidate) => candidate.label === event.target.value);
            if (preset) onXChange(preset.x);
          }}
        >
          <option value="">Custom</option>
          {stagePresets.map((preset) => (
            <option key={preset.label} value={preset.label}>{preset.label}</option>
          ))}
        </select>
      </div>
    );
  }

  return (
    <div className="position-control">
      <label className="setup-field setup-coordinate-field">
        <span>X</span>
        <input value={x} placeholder="x 0-1" onChange={(event) => onXChange(event.target.value)} />
      </label>
      <label className="setup-field setup-coordinate-field">
        <span>Y</span>
        <input value={y} placeholder="y 0-1" onChange={(event) => onYChange(event.target.value)} />
      </label>
      <label className="setup-field setup-preset-field">
        <span>Preset</span>
        <select
          value={selectedPreset}
          onChange={(event) => {
            const preset = stagePresets.find((candidate) => candidate.label === event.target.value);
            if (preset) onXChange(preset.x);
          }}
        >
          <option value="">Custom</option>
          {stagePresets.map((preset) => (
            <option key={preset.label} value={preset.label}>{preset.label}</option>
          ))}
        </select>
      </label>
    </div>
  );
}

function SpeakerField({ blockId, value, options, onChange }: { blockId: string; value: string; options: string[]; onChange: (v: string) => void }) {
  const listId = `speaker-${options.length}`;
  const isNarration = !value.trim();
  return (
    <div className={`speaker-field${isNarration ? ' is-narration' : ''}`}>
      <input data-block-id={blockId} data-focus-field="speaker" value={value} list={listId} placeholder="Narration" onChange={(e) => onChange(e.target.value)} />
      <datalist id={listId}>
        <option value="">Narration</option>
        {options.map((o) => <option key={o} value={o} />)}
      </datalist>
    </div>
  );
}

function parseTransition(type: BeatType, text: string) {
  const lines = text.split(/\r?\n/);
  for (let i = lines.length - 1; i >= 0; i -= 1) {
    const trimmed = lines[i].trim();
    if (!trimmed) continue;
    const match = type === 'dialogue'
      ? trimmed.match(/^\|combat(?:Direct)?\s+(.+)$/i)
      : trimmed.match(/^dialogue\s*:\s*(.*)$/i);
    return {
      transition: match ? match[1].trim() : null,
      transitionLineIndex: match ? i : null,
    };
  }
  return { transition: null, transitionLineIndex: null };
}

export function App() {
  const [project, setProject] = useState<LoadedProject | null>(null);
  const [beats, setBeats] = useState<StoryBeat[]>([]);
  const [visualData, setVisualData] = useState<VisualData | null>(null);
  const [editorBeatId, setEditorBeatId] = useState<string | null>(null);
  const [focusedBeatId, setFocusedBeatId] = useState<string | null>(null);
  const [newBeatName, setNewBeatName] = useState('');
  const [newBeatType, setNewBeatType] = useState<BeatType>('dialogue');
  const [status, setStatus] = useState('Choose your Unity project folder to begin.');
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const [sidebarSearch, setSidebarSearch] = useState('');
  const [nodes, setNodes, onNodesChange] = useNodesState<Node>([]);
  const [edges, setEdges, onEdgesChange] = useEdgesState<Edge>([]);
  const viewportRef = useRef<Viewport | null>(null);
  const graphShellRef = useRef<HTMLElement | null>(null);
  const reactFlow = useReactFlow();
  const nodeTypes = useMemo(() => ({ storyBeat: StoryBeatNode }), []);

  const selectedBeat = beats.find((beat) => beat.id === editorBeatId) ?? null;

  const removeBeatRef = useRef<(beat: StoryBeat) => void>(() => {});
  const stableOnDelete = useCallback((beat: StoryBeat) => removeBeatRef.current(beat), []);

  const currentGraphNodes = useCallback(() => {
    const liveNodes = reactFlow.getNodes();
    return liveNodes.length > 0 ? liveNodes : nodes;
  }, [nodes, reactFlow]);

  const visualDataWithLivePositions = useCallback((nextVisualData: VisualData) => {
    const liveNodes = currentGraphNodes();
    if (liveNodes.length === 0) return nextVisualData;

    return {
      ...nextVisualData,
      nodes: {
        ...nextVisualData.nodes,
        ...Object.fromEntries(liveNodes.map((node) => [node.id, { x: node.position.x, y: node.position.y }])),
      },
    };
  }, [currentGraphNodes]);

  const refreshGraph = useCallback((nextBeats: StoryBeat[], nextVisualData: VisualData, preserveLivePositions = false) => {
    const graphVisualData = preserveLivePositions ? visualDataWithLivePositions(nextVisualData) : nextVisualData;
    setVisualData(graphVisualData);
    setNodes(makeNodes(nextBeats, graphVisualData, stableOnDelete));
    setEdges(makeEdges(nextBeats));
  }, [setEdges, setNodes, stableOnDelete, visualDataWithLivePositions]);

  const openProject = async () => {
    try {
      setStatus('Waiting for folder permission…');
      const root = await pickUnityProject();
      const loaded = await loadProject(root);
      setProject(loaded);
      setBeats(loaded.beats);
      setVisualData(loaded.visualData);
      viewportRef.current = null;
      refreshGraph(loaded.beats, loaded.visualData);
      setStatus(`Loaded ${loaded.beats.length} story beats.`);
    } catch (error) {
      setStatus(error instanceof Error ? error.message : String(error));
    }
  };

  const reloadProject = async () => {
    if (!project) return;
    const loaded = await loadProject(project.root);
    setProject(loaded);
    setBeats(loaded.beats);
    setVisualData(loaded.visualData);
    refreshGraph(loaded.beats, loaded.visualData);
    setStatus('Reloaded from disk.');
  };

  const saveVisuals = async (nextNodes?: Node[]) => {
    if (!project || !visualData) return;
    const nodesToSave = nextNodes ?? currentGraphNodes();
    const nextVisualData = {
      ...visualData,
      nodes: Object.fromEntries(nodesToSave.map((node) => [node.id, { x: node.position.x, y: node.position.y }])),
    };
    setNodes(nodesToSave);
    setVisualData(nextVisualData);
    await saveVisualData(project.root, nextVisualData);
    setStatus('Saved graph layout.');
  };

  const currentViewportCenter = () => {
    const rect = graphShellRef.current?.getBoundingClientRect();
    const viewport = viewportRef.current ?? { x: 0, y: 0, zoom: 1 };
    if (!rect) return { x: 80, y: 80 };
    return {
      x: (rect.width / 2 - viewport.x) / viewport.zoom - 120,
      y: (rect.height / 2 - viewport.y) / viewport.zoom - 45,
    };
  };

  const focusBeatOnGraph = (beatId: string, openEditor = false) => {
    const node = nodes.find((entry) => entry.id === beatId);
    setFocusedBeatId(beatId);
    if (node) {
      const zoom = viewportRef.current?.zoom ?? 1;
      reactFlow.setCenter(node.position.x + 120, node.position.y + 45, { zoom, duration: 450 });
    }
    if (openEditor) setEditorBeatId(beatId);
  };

  const onNodeDragStop = async (_event: unknown, node: Node) => {
    if (!project || !visualData) return;
    const nextVisualData = visualDataWithLivePositions(visualData);
    nextVisualData.nodes[node.id] = { x: node.position.x, y: node.position.y };
    setVisualData(nextVisualData);
    await saveVisualData(project.root, nextVisualData);
    setStatus(`Saved position for ${String(node.id).replace(/^[^:]+:/, '')}.`);
  };

  const onConnect = useCallback(async (connection: Connection) => {
    if (!project || !connection.source || !connection.target) return;
    const source = beats.find((beat) => beat.id === connection.source);
    const target = beats.find((beat) => beat.id === connection.target);
    if (!source || !target) return;
    if ((source.type === 'dialogue' && target.type !== 'combat') || (source.type === 'combat' && target.type !== 'dialogue')) {
      setStatus('Links must connect dialogue → combat or combat → dialogue.');
      return;
    }
    const nextText = setTransitionLine(source, target.name, /\|combatDirect\s+/i.test(source.text));
    const nextBeat = { ...source, text: nextText, transition: target.name, transitionLineIndex: parseTransition(source.type, nextText).transitionLineIndex };
    const roots = project ? [project.storyFlow.newGameStart, project.storyFlow.defaultResume] : [];
    const nextBeats = replaceBeat(beats, nextBeat, roots);
    setBeats(nextBeats);
    if (visualData) refreshGraph(nextBeats, visualData, true);
    await saveBeat(project.root, nextBeat);
    setStatus(`Linked ${source.name} → ${target.name}.`);
  }, [beats, project, visualData, refreshGraph]);

  const isValidConnection = useCallback((connection: Connection | Edge) => {
    if (!connection.source || !connection.target || connection.source === connection.target) return false;
    const source = beats.find((beat) => beat.id === connection.source);
    const target = beats.find((beat) => beat.id === connection.target);
    if (!source || !target) return false;
    return (source.type === 'dialogue' && target.type === 'combat') || (source.type === 'combat' && target.type === 'dialogue');
  }, [beats]);

  const updateBeat = (beat: StoryBeat) => {
    const roots = project ? [project.storyFlow.newGameStart, project.storyFlow.defaultResume] : [];
    const nextBeats = replaceBeat(beats, beat, roots);
    setBeats(nextBeats);
  };

  const persistBeat = async (beat: StoryBeat) => {
    if (!project || !visualData) return;
    await saveBeat(project.root, beat);
    const nextBeats = replaceBeat(beats, beat, [project.storyFlow.newGameStart, project.storyFlow.defaultResume]);
    setBeats(nextBeats);
    setStatus(`Saved ${beat.name}.`);
  };

  const closeEditor = () => {
    setEditorBeatId(null);
    if (visualData) refreshGraph(beats, visualData, true);
  };

  const updateStoryFlow = async (key: 'newGameStart' | 'defaultResume', value: string) => {
    if (!project) return;
    const nextStoryFlow = { ...project.storyFlow, [key]: value };
    await saveStoryFlow(project.root, nextStoryFlow);
    setProject({ ...project, storyFlow: nextStoryFlow });
    setStatus(`Set ${key === 'newGameStart' ? 'new game start' : 'default resume'} to ${value}.`);
  };

  const renameBeat = async (beat: StoryBeat, newName: string) => {
    if (!project || !visualData) return;
    const cleanName = await renameBeatFile(project.root, beat, newName);
    const oldId = beat.id;
    const newId = `${beat.type}:${cleanName}`;

    for (const candidate of beats) {
      if (candidate.id === oldId) continue;
      if (targetBeatId(candidate.type, candidate.transition) !== oldId) continue;
      const updatedText = setTransitionLine(candidate, cleanName, /\|combatDirect\s+/i.test(candidate.text));
      const updatedBeat = {
        ...candidate,
        text: updatedText,
        transition: cleanName,
        transitionLineIndex: parseTransition(candidate.type, updatedText).transitionLineIndex,
      };
      await saveBeat(project.root, updatedBeat);
    }

    const nextStoryFlow = { ...project.storyFlow };
    if (beat.type === 'dialogue' && nextStoryFlow.newGameStart === beat.name) {
      nextStoryFlow.newGameStart = cleanName;
    }
    if (beat.type === 'dialogue' && nextStoryFlow.defaultResume === beat.name) {
      nextStoryFlow.defaultResume = cleanName;
    }
    if (nextStoryFlow.newGameStart !== project.storyFlow.newGameStart || nextStoryFlow.defaultResume !== project.storyFlow.defaultResume) {
      await saveStoryFlow(project.root, nextStoryFlow);
    }

    const nodePosition = visualData.nodes[oldId] ?? nodes.find((node) => node.id === oldId)?.position;
    const nextNodes = { ...visualData.nodes };
    delete nextNodes[oldId];
    if (nodePosition) nextNodes[newId] = nodePosition;
    const nextVisualData = { ...visualData, nodes: nextNodes };
    await saveVisualData(project.root, nextVisualData);

    const loaded = await loadProject(project.root);
    setProject({ ...loaded, visualData: nextVisualData, storyFlow: nextStoryFlow });
    setBeats(loaded.beats);
    setVisualData(nextVisualData);
    refreshGraph(loaded.beats, nextVisualData);
    setEditorBeatId(newId);
    setFocusedBeatId(newId);
    setStatus(`Renamed ${beat.name} to ${cleanName}.`);
  };

  const addPlaceholder = async (category: keyof AssetCatalog, value: string) => {
    if (!project || !visualData) return;
    if (!(category in visualData.placeholders)) return;
    const nextVisualData = {
      ...visualData,
      placeholders: {
        ...visualData.placeholders,
        [category]: Array.from(new Set([...(visualData.placeholders as any)[category], value])).sort(),
      },
    };
    await saveVisualData(project.root, nextVisualData);
    const loaded = await loadProject(project.root);
    setProject(loaded);
    setVisualData(loaded.visualData);
    setStatus(`Added placeholder: ${value}.`);
  };

  const addBeat = async () => {
    if (!project || !visualData) return;
    const createdName = await createBeat(project.root, newBeatType, newBeatName);
    const createdId = `${newBeatType}:${createdName}`;
    const createdPosition = currentViewportCenter();
    setNewBeatName('');
    const loaded = await loadProject(project.root);
    const liveVisualData = visualDataWithLivePositions({
      ...loaded.visualData,
      nodes: {
        ...visualData.nodes,
        ...loaded.visualData.nodes,
      },
    });
    const nextVisualData = {
      ...liveVisualData,
      nodes: {
        ...liveVisualData.nodes,
        [createdId]: createdPosition,
      },
    };
    await saveVisualData(project.root, nextVisualData);
    setProject({ ...loaded, visualData: nextVisualData });
    setBeats(loaded.beats);
    setVisualData(nextVisualData);
    refreshGraph(loaded.beats, nextVisualData);
    setFocusedBeatId(createdId);
    reactFlow.setCenter(createdPosition.x + 120, createdPosition.y + 45, { zoom: viewportRef.current?.zoom ?? 1, duration: 350 });
    setStatus(`Created ${newBeatType} "${createdName}" at the current viewport center.`);
  };

  async function removeBeat(beat: StoryBeat) {
    if (!project) return;
    const incoming = beats.filter((candidate) => targetBeatId(candidate.type, candidate.transition) === beat.id);
    const confirmed = window.confirm(
      `Delete ${beat.relativePath} and its Unity meta file?` +
      (incoming.length > 0 ? `\n\nThis will also clear ${incoming.length} incoming transition${incoming.length === 1 ? '' : 's'}.` : '')
    );
    if (!confirmed) return;

    for (const candidate of incoming) {
      const updatedText = setTransitionLine(candidate, null, /\|combatDirect\s+/i.test(candidate.text));
      await saveBeat(project.root, {
        ...candidate,
        text: updatedText,
        transition: null,
        transitionLineIndex: parseTransition(candidate.type, updatedText).transitionLineIndex,
      });
    }

    await deleteBeat(project.root, beat);
    if (visualData) {
      const nextNodes = { ...visualData.nodes };
      delete nextNodes[beat.id];
      const nextVisualData = { ...visualData, nodes: nextNodes };
      await saveVisualData(project.root, nextVisualData);
    }
    setEditorBeatId(null);
    setFocusedBeatId(null);
    await reloadProject();
    setStatus(`Deleted ${beat.name}.`);
  }
  removeBeatRef.current = removeBeat;

  const stats = useMemo(() => {
    const dialogues = beats.filter((beat) => beat.type === 'dialogue').length;
    const combats = beats.length - dialogues;
    const issues = beats.reduce((count, beat) => count + beat.issues.length, 0);
    return { dialogues, combats, issues };
  }, [beats]);

  const filteredBeats = useMemo(() => {
    const q = sidebarSearch.toLowerCase();
    return q ? beats.filter((b) => b.name.toLowerCase().includes(q)) : beats;
  }, [beats, sidebarSearch]);

  if (selectedBeat && project) {
    return (
      <BeatEditor
        beat={selectedBeat}
        beats={beats}
        assets={project.assets}
        onClose={closeEditor}
        onChange={updateBeat}
        onSave={persistBeat}
        onDelete={removeBeat}
        onRename={renameBeat}
        onAddPlaceholder={addPlaceholder}
      />
    );
  }

  return (
    <div className="app-shell">
      <header className="topbar">
        <div>
          <div className="eyebrow">Match-3 Adventure</div>
          <h1>Story Flow</h1>
        </div>
        <div className="topbar-actions">
          <button type="button" onClick={openProject}><FolderOpen size={17} />Open Unity Project</button>
          <button type="button" onClick={reloadProject} disabled={!project}><RefreshCw size={17} />Reload</button>
          <button type="button" onClick={() => saveVisuals()} disabled={!project}><Save size={17} />Save Layout</button>
        </div>
      </header>

      <section className="status-row">
        <span>{status}</span>
        <span>{stats.dialogues} dialogues</span>
        <span>{stats.combats} combats</span>
        {stats.issues > 0 && <span className="status-issues"><AlertTriangle size={14} />{stats.issues} issues</span>}
      </section>

      {project && (
        <section className="crud-row">
          <select value={newBeatType} onChange={(event) => setNewBeatType(event.target.value as BeatType)}>
            <option value="dialogue">Dialogue</option>
            <option value="combat">Combat</option>
          </select>
          <input value={newBeatName} onChange={(event) => setNewBeatName(event.target.value)} placeholder="newFileName" />
          <button type="button" onClick={addBeat} disabled={!newBeatName.trim()}><Plus size={15} />Create Beat</button>
          <label className="story-flow-control">
            New game
            <select value={project.storyFlow.newGameStart} onChange={(event) => updateStoryFlow('newGameStart', event.target.value)}>
              {beats.filter((beat) => beat.type === 'dialogue').map((beat) => (
                <option key={beat.id} value={beat.name}>{beat.name}</option>
              ))}
            </select>
          </label>
          <label className="story-flow-control">
            Resume
            <select value={project.storyFlow.defaultResume} onChange={(event) => updateStoryFlow('defaultResume', event.target.value)}>
              {beats.filter((beat) => beat.type === 'dialogue').map((beat) => (
                <option key={beat.id} value={beat.name}>{beat.name}</option>
              ))}
            </select>
          </label>
          <span>Double-click a node to edit. Drag between handles to set transitions.</span>
        </section>
      )}

      <div className="content-area">
        {beats.length > 0 && (
          <aside className={`beat-sidebar ${sidebarOpen ? 'open' : 'closed'}`}>
            {sidebarOpen && (
              <>
                <div className="sidebar-search">
                  <Search size={14} />
                  <input
                    value={sidebarSearch}
                    onChange={(e) => setSidebarSearch(e.target.value)}
                    placeholder="Search…"
                  />
                </div>
                <div className="sidebar-list">
                  {filteredBeats.map((beat) => (
                    <button
                      key={beat.id}
                      type="button"
                      className={focusedBeatId === beat.id ? 'is-focused' : ''}
                      onClick={() => focusBeatOnGraph(beat.id)}
                      onDoubleClick={() => focusBeatOnGraph(beat.id, true)}
                    >
                      {beat.type === 'dialogue' ? <BookOpen size={14} /> : <Swords size={14} />}
                      <span>{beat.name}</span>
                      {beat.issues.length > 0 && <AlertTriangle size={13} />}
                    </button>
                  ))}
                  {filteredBeats.length === 0 && <p className="sidebar-empty">No matches</p>}
                </div>
              </>
            )}
            <button
              type="button"
              className="sidebar-toggle"
              onClick={() => setSidebarOpen(!sidebarOpen)}
              aria-label={sidebarOpen ? 'Collapse sidebar' : 'Expand sidebar'}
            >
              {sidebarOpen ? <ChevronLeft size={16} /> : <ChevronRight size={16} />}
            </button>
          </aside>
        )}

        <main className="graph-shell" ref={graphShellRef}>
          {beats.length === 0 ? (
            <div className="empty-state">
              <GitFork size={34} />
              <h2>Select the Unity project folder</h2>
              <p>The editor reads and writes dialogue and combat text files under Assets/Resources.</p>
              <button type="button" onClick={openProject}><FolderOpen size={17} />Open Folder</button>
            </div>
          ) : (
            <ReactFlow
              nodes={nodes}
              edges={edges}
              nodeTypes={nodeTypes}
              onNodesChange={onNodesChange}
              onEdgesChange={onEdgesChange}
              onConnect={onConnect}
              isValidConnection={isValidConnection}
              onNodeDragStop={onNodeDragStop}
              onNodeClick={(_event, node) => setFocusedBeatId(node.id)}
              onNodeDoubleClick={(_event, node) => setEditorBeatId(node.id)}
              onMoveEnd={(_e, vp) => { viewportRef.current = vp; }}
              fitView={!viewportRef.current}
              defaultViewport={viewportRef.current ?? { x: 0, y: 0, zoom: 1 }}
            >
              <Background />
              <Controls />
              <MiniMap pannable zoomable />
            </ReactFlow>
          )}
        </main>
      </div>
    </div>
  );
}
