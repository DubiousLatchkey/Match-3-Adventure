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
- `GridController.assignType` expects 0-100 weights in its probability table and scales `Random.value` accordingly.
- `DialogueController` parses loaded text through `DialogueScriptParser` and dispatches commands through `DialogueCommandRunner`; command behavior still delegates to `DialogueController.performAction`.
- `CombatSceneRefs` binds combat scene references. Prefer wiring serialized fields in `CombatScene`; name-based fallback binding remains for compatibility.
- Player and enemy state is shared through `CombatantState`, `CombatantView`, and `CombatantRuntime`, while older controller APIs remain as wrappers.
- Player progression routes through `SaveGameService` and persists to `Application.persistentDataPath/savegame.json`. Keep any `PlayerPrefs` migration fallback contained inside `SaveGameService`.

## Workflows

- Spell testbed: use Unity menu `Dev/Play Spell Test Dummy`.
- Debug profiles: select a `DebugCombatProfile` and choose `Dev/Play Selected Debug Profile`.
- Default spell test profile: `Assets/Debug/Profiles/SpellTestDummy.asset`.

## Caution

- Do not overwrite uncommitted story, dialogue, scene, or resource changes unless explicitly asked.
- Do not commit generated Unity folders such as `Library`, `Temp`, `Obj`, `Logs`, or build output unless the user explicitly asks.
- If URP or ShaderGraph package-internal symbols go missing, clear `Library/PackageCache/com.unity.render-pipelines.*` and `Library/PackageCache/com.unity.shadergraph*`, then let Unity restore packages.
