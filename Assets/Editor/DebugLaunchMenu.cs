#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DebugLaunchMenu {
    private const string DefaultProfilePath = "Assets/Debug/Profiles/SpellTestDummy.asset";
    private const string DefaultDialogueProfilePath = "Assets/Debug/DialogueProfiles/RelativeMovementTest.asset";
    private const string CombatScenePath = "Assets/Scenes/CombatScene.unity";
    private const string DialogueScenePath = "Assets/Scenes/DialogueScene.unity";

    [MenuItem("Dev/Play Spell Test Dummy")]
    public static void PlaySpellTestDummy() {
        DebugCombatProfile profile = LoadOrCreateDefaultProfile();
        PlayProfile(profile);
    }

    [MenuItem("Dev/Play Selected Debug Profile")]
    public static void PlaySelectedProfile() {
        DebugCombatProfile profile = Selection.activeObject as DebugCombatProfile;
        if (profile == null) {
            Debug.LogWarning("Select a DebugCombatProfile asset first.");
            return;
        }
        PlayProfile(profile);
    }

    [MenuItem("Dev/Select Spell Test Dummy Profile")]
    public static void SelectSpellTestDummyProfile() {
        Selection.activeObject = LoadOrCreateDefaultProfile();
        EditorGUIUtility.PingObject(Selection.activeObject);
    }

    [MenuItem("Dev/Play Dialogue Test")]
    public static void PlayDialogueTest() {
        PlayDialogueProfile(LoadOrCreateDefaultDialogueProfile());
    }

    private static void PlayProfile(DebugCombatProfile profile) {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) {
            return;
        }

        ApplyProfileToSave(profile);
        DebugCombatRuntime.ApplyProfile(profile);
        GridController.combatToLoad = profile.combatId;
        EditorSceneManager.OpenScene(CombatScenePath);
        EditorApplication.isPlaying = true;
    }

    private static DebugCombatProfile LoadOrCreateDefaultProfile() {
        DebugCombatProfile profile = AssetDatabase.LoadAssetAtPath<DebugCombatProfile>(DefaultProfilePath);
        if (profile != null) {
            return profile;
        }

        System.IO.Directory.CreateDirectory("Assets/Debug/Profiles");
        profile = ScriptableObject.CreateInstance<DebugCombatProfile>();
        AssetDatabase.CreateAsset(profile, DefaultProfilePath);
        AssetDatabase.SaveAssets();
        return profile;
    }

    private static void PlayDialogueProfile(DebugDialogueProfile profile) {
        if (profile == null || string.IsNullOrWhiteSpace(profile.dialogueId)) {
            Debug.LogWarning("Dialogue profile needs a dialogue id.");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) {
            return;
        }

        DebugDialogueRuntime.SetEditorDialogueOverride(profile.dialogueId.Trim());
        EditorSceneManager.OpenScene(DialogueScenePath);
        EditorApplication.isPlaying = true;
    }

    private static DebugDialogueProfile LoadOrCreateDefaultDialogueProfile() {
        DebugDialogueProfile profile = AssetDatabase.LoadAssetAtPath<DebugDialogueProfile>(DefaultDialogueProfilePath);
        if (profile != null) {
            return profile;
        }

        System.IO.Directory.CreateDirectory("Assets/Debug/DialogueProfiles");
        profile = ScriptableObject.CreateInstance<DebugDialogueProfile>();
        profile.dialogueId = "relativeMovementTest";
        AssetDatabase.CreateAsset(profile, DefaultDialogueProfilePath);
        AssetDatabase.SaveAssets();
        return profile;
    }

    private static void ApplyProfileToSave(DebugCombatProfile profile) {
        List<Spell> spells = SpellContentLoader.LoadSpells();
        List<Weapon> weapons = WeaponContentLoader.LoadWeapons();
        SaveGameService.NewGame(spells, weapons);
        SaveGameService.SetInt("hp", profile.playerHp, saveImmediately: false);
        SaveGameService.SetInt("maxRedMana", profile.maxRedMana, saveImmediately: false);
        SaveGameService.SetInt("maxBlueMana", profile.maxBlueMana, saveImmediately: false);
        SaveGameService.SetInt("maxYellowMana", profile.maxYellowMana, saveImmediately: false);

        for (int i = 0; i < profile.equippedSpellNames.Length; i++) {
            string spellName = profile.equippedSpellNames[i];
            if (!string.IsNullOrWhiteSpace(spellName)) {
                SaveGameService.SetInt(spellName, i + 1, saveImmediately: false);
            }
        }

        if (!string.IsNullOrWhiteSpace(profile.equippedWeaponName)) {
            SaveGameService.SetInt(profile.equippedWeaponName, 2, saveImmediately: false);
        }

        SaveGameService.Save();
    }
}
#endif
