# Project Notes

This is a Unity match-3 adventure project targeting Unity 6.3 LTS.

## Build Target

- Unity version: `6000.3.17f1`.
- Main output: Android APK at `Builds/Android/Match3Adventure.apk`.
- Build method: `AndroidBuild.BuildApk` in `Assets/Editor/AndroidBuild.cs`.
- Android builds require Android Build Support, SDK/NDK tools, and OpenJDK for the installed Unity editor.

## Project Layout

- Gameplay scripts live under `Assets/Scripts`.
- Legacy combat controllers live under `Assets/Scripts/Combat Scripts`.
- Shared gameplay/domain types live under `Assets/Scripts/Domain`.
- Combat runtime helpers live under `Assets/Scripts/Combat/Runtime`.
- Dialogue parsing and command helpers live under `Assets/Scripts/Dialogue`.
- Dialogue and combat scripts are plain text resources under `Assets/Resources/Dialogues` and `Assets/Resources/Combats`.
- Authored spell, weapon, and enemy data live in JSON TextAssets under `Assets/Resources/Data`.
- Enemy entries in `Assets/Resources/Data/enemies.json` may set `portraitKey` to share one character portrait across multiple enemy loadouts. If omitted, runtime portrait fallback uses the enemy `name`; combat-row portrait overrides still take precedence.
- Story flow entry points live in `Assets/Resources/Data/story_flow.json`; `StoryFlowConfig` reads this file for new-game start and default resume dialogue IDs.
- URP assets live at `Assets/UniversalRenderPipelineAsset.asset` and `Assets/UniversalRenderPipelineAsset_Renderer.asset`.

## Scenes

Build scenes are configured in `ProjectSettings/EditorBuildSettings.asset`:

- `Assets/Scenes/MainMenu.unity`
- `Assets/Scenes/CombatScene.unity`
- `Assets/Scenes/DialogueScene.unity`
- `Assets/Scenes/GearUpScene.unity`

## Runtime Notes

- `GridController.movesAtPoint` delegates board move-search logic to `MatchBoard`.
- `GridController.castSpell` delegates spell parameter iteration to `SpellEffectRunner`; individual command cases still live in `GridController.ExecuteSpellCommand`.
- Targeted spell board effects should prefer reusable parameterized actions: `scoreSquare`/`destroySquare`, `targetShape <shape> <score|destroy|count>`, `randomPieces <count> <score|destroy>`, and `rotateSquare <clockwise|counterclockwise>`. Use `score` when resources should be awarded and `destroy` when pieces should only be removed/replaced.
- `GridController.assignType` expects 0-100 weights in its probability table and scales `Random.value` accordingly. Generated board types are mapped explicitly, so non-contiguous or retired tile IDs can remain supported.
- Board tile IDs are centralized in `ThingTypes`: mana `0-2`, damage `3`, legacy health `4`, legacy multiplier `5`, null `6`, brick `7`, wildcard `8`, rainbow mana `9`, and empty `-1`.
- Null tiles match and clear but do not score resources. Bricks cannot move or match. Wildcards can participate in red/blue/yellow/rainbow mana matches but do not start matches by themselves or score their own resource. Rainbow mana matches only with rainbow mana and wildcards, then gives one mana of each color per scored tile.
- `DialogueController` parses loaded text through `DialogueScriptParser` and dispatches commands through `DialogueCommandRunner`.
- Dialogue commands are tokenized by `DialogueCommandTokenizer`, so quoted arguments can contain spaces, e.g. `|playMusic "Flesh & Bones"` or `|setPrefValue "Amplifying Staff" 1`.
- Dialogue command behavior is registered in `DialogueCommandRegistry`; keep command execution out of `DialogueController` and expose only scene-facing controller methods when handlers need to touch dialogue UI or characters.
- Dialogue `enter` separates the character resource key from display name and expression. Prefer named options after the required character/x/y fields, e.g. `|enter mercenaryAtherian 7 -1 expression Normal displayname "Raider"`. Legacy positional optional args still parse as expression then display name, and the story-flow PWA marks/migrates old `enter` syntax on save.
- Dialogue scene setup entries support the same display options after character/x/y, e.g. `|mercenaryAtherian 0.7 0 expression Normal displayname "Raider"`, and the PWA carries these through Setup From Previous.
- Dialogue display names can be changed after entry with `|setDisplayName <character> "Display Name"`. Speaker highlighting and character commands use the character resource key, while the nameplate/log use the display name.
- Dialogue progression commands are split by storage target: `setFlag <key> <value>` writes to `SaveGameService`/`savegame.json` as an int when the value parses as an integer and otherwise as a string, while legacy `setPrefValue <key> <int>` writes directly to Unity `PlayerPrefs`.
- Dialogue character positioning uses `DialogueController.characterStage` and `DialogueCharacterMover`; numeric x/y command inputs are relative stage coordinates where `0,0` is bottom-left and `1,1` is top-right before applying `characterStageYOffset` (default `0.5`). Values outside `0-1` are allowed for offstage movement.
- Directional `exit <character> <left|right|down>` remains intentionally generic because characters fade while exiting.
- `CombatSceneRefs` binds combat scene references through explicit serialized fields in `CombatScene`; it does not auto-fill missing references by object name.
- Player and enemy state is shared through `CombatantState`, `CombatantView`, and `CombatantRuntime`, while older controller APIs remain as wrappers.
- Player progression routes through `SaveGameService` and persists to `Application.persistentDataPath/savegame.json`. Keep any `PlayerPrefs` migration fallback contained inside `SaveGameService`.

## Workflows

- Dialogue editor: use Unity menu `Tools/Dialogue/Dialogue Editor`.
- Dialogue validation: use Unity menu `Tools/Dialogue/Validate All Dialogue`; the editor window can also validate and play the selected dialogue.
- Story flow PWA: `story-flow-pwa` is a local React/Vite PWA for graph-based dialogue/combat editing. Run it with `npm run dev -- --host 127.0.0.1` from `story-flow-pwa`, then select the Unity project root in Chrome/Edge. It edits actual `Assets/Resources/Dialogues` and `Assets/Resources/Combats` text files, supports beat create/delete, rewrites reserved final transition lines from completed graph links, edits `Assets/Resources/Data/story_flow.json` entry settings, deletes `.txt.meta` sidecars when deleting beats, and stores layout/placeholders in `story-flow.visuals.json`. Beat nodes expose a bottom handle for dragging new transitions, a top handle for incoming transitions, and a graph-level trash button; connection writes happen only after a valid drop, while deletion clears incoming reserved transition lines before removing the beat file. Combat editing follows `Combat.cs`: body rows are `enemy`, `weapons`, `solo`, `companion`, or `tutorial`, while the last `dialogue:` entry is treated as the reserved transition footer and is serialized back to the final line.
- Story flow PWA scene preview: dialogue story-block rows include a hover/click scene preview popout built from parsed setup/character/background/dialogue commands. Hover previews the current block position, clicking the eye button pins it, and the close button clears the pinned preview.
- Dialogue testbed: edit `Assets/Debug/DialogueProfiles/RelativeMovementTest.asset` and use Unity menu `Dev/Play Dialogue Test`.
- Spell testbed: use Unity menu `Dev/Play Spell Test Dummy`.
- Debug profiles: select a `DebugCombatProfile` and choose `Dev/Play Selected Debug Profile`.
- Default spell test profile: `Assets/Debug/Profiles/SpellTestDummy.asset`.
- New spell test profiles are grouped under `Assets/Debug/Profiles`: `NewSpellDestructionRandom.asset`, `NewSpellShapesRotation.asset`, and `EtherealScoreVariants.asset`.

## Caution

- Do not overwrite uncommitted story, dialogue, scene, or resource changes unless explicitly asked.
- Do not commit generated Unity folders such as `Library`, `Temp`, `Obj`, `Logs`, or build output unless the user explicitly asks.
- If URP or ShaderGraph package-internal symbols go missing, clear `Library/PackageCache/com.unity.render-pipelines.*` and `Library/PackageCache/com.unity.shadergraph*`, then let Unity restore packages.
