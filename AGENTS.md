# Project Notes

This is a Unity match-3 adventure project originally created with Unity 2019.4.15f1 and migrated toward Unity 6.3 LTS.

## Current Target

- Unity target: 6000.3.17f1, the latest Unity 6.3 LTS patch checked during the migration on 2026-06-16.
- Main output: Android APK at `Builds/Android/Match3Adventure.apk`.
- Build method: `AndroidBuild.BuildApk` in `Assets/Editor/AndroidBuild.cs`.

## Project Shape

- Gameplay scripts live under `Assets/Scripts`, with combat-specific code in `Assets/Scripts/Combat Scripts`.
- Shared gameplay/domain types now live under `Assets/Scripts/Domain`:
  - `Spell`, `Weapon`, `Enemy`, `Combat`, `StatusEffect`, `Thing`, `Move`, and related containers/interfaces.
- Refactor support code lives under:
  - `Assets/Scripts/Combat/Runtime` for `MatchBoard`, `SpellEffectRunner`, and `CombatSceneRefs`.
  - `Assets/Scripts/Dialogue` for dialogue parsing/command-running helpers.
- Build scenes are configured in `ProjectSettings/EditorBuildSettings.asset`:
  - `Assets/Scenes/MainMenu.unity`
  - `Assets/Scenes/CombatScene.unity`
  - `Assets/Scenes/DialogueScene.unity`
  - `Assets/Scenes/GearUpScene.unity`
- Dialogue and combat data are plain text resources under `Assets/Resources/Dialogues` and `Assets/Resources/Combats`.
- The project uses URP assets at `Assets/UniversalRenderPipelineAsset.asset` and `Assets/UniversalRenderPipelineAsset_Renderer.asset`.

## Migration Notes

- `com.unity.render-pipelines.lightweight` was replaced with `com.unity.render-pipelines.universal` `17.3.0`. During migration, a corrupted local SRP Core package cache caused URP/ShaderGraph compile errors; clear `Library/PackageCache/com.unity.render-pipelines.*` and `Library/PackageCache/com.unity.shadergraph*` if those package-internal symbols go missing again.
- Old unused service packages were removed from `Packages/manifest.json` to avoid pulling 2019-era Ads, Analytics, IAP, Collab, and Quick Search preview packages into Unity 6.
- 2D light namespaces were updated from `UnityEngine.Experimental.Rendering.Universal` to `UnityEngine.Rendering.Universal`.
- Obsolete `FindObjectsOfType` calls in combat controllers were updated to `FindObjectsByType`.
- `GridController.assignType` expects 0-100 weights in its probability table and scales `Random.value` accordingly.

## Refactor Notes

- `GridController.movesAtPoint` now delegates to pure `MatchBoard` move-search logic so board searches can be tested without scene objects.
- `GridController.castSpell` delegates spell parameter iteration to `SpellEffectRunner`; command cases still live in `GridController.ExecuteSpellCommand` as a transitional extraction point.
- `DialogueController` now parses loaded text through `DialogueScriptParser` and dispatches commands through `DialogueCommandRunner`; command behavior still delegates to `DialogueController.performAction`.
- `CombatSceneRefs` is a scene-specific binder for combat references. Add it to one object in `CombatScene` and wire its serialized fields when possible. It still has name-based fallback binding for compatibility.
- Player/enemy health, mana, and multiplier updates are being migrated into shared `CombatantState`, `CombatantView`, and `CombatantRuntime` classes under `Assets/Scripts/Combat/Runtime`. `GridController` and `EnemyController` still expose the old `PlayerController` API as wrappers during the transition.
- Authored spell/weapon data now prefers JSON TextAssets at `Assets/Resources/Data/spells.json` and `Assets/Resources/Data/weapons.json`. `SpellContainer.Load(...)`, `WeaponContainer.Load(...)`, and `SpellSerializer.loadSpells(...)` route through JSON loaders first, with old XML/text paths kept as compatibility fallbacks.
- Player progression now routes through `SaveGameService` and persists to `Application.persistentDataPath/savegame.json`. Remaining `PlayerPrefs` access should stay inside `SaveGameService` as migration fallback only.
- Spell testbed workflow: use Unity menu `Dev/Play Spell Test Dummy` or select a `DebugCombatProfile` and choose `Dev/Play Selected Debug Profile`. The default profile asset is created at `Assets/Debug/Profiles/SpellTestDummy.asset`; edit equipped spells/player stats/dummy enemy stats there.

## Caution

- Do not overwrite existing uncommitted story/dialogue/resource changes unless the user explicitly asks. At migration time, several dialogue and combat text resources already had local edits.
- Unity editor installation may require administrator elevation on this machine. If Unity 6.3 LTS is not installed, install Android Build Support, SDK/NDK tools, and OpenJDK with the editor before running the APK build.
