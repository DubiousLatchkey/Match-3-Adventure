using UnityEngine;

public static class DebugDialogueRuntime {
    private const string EditorDialogueOverrideKey = "Match3.DebugDialogueOverride";

    public static void SetEditorDialogueOverride(string dialogueId) {
        PlayerPrefs.SetString(EditorDialogueOverrideKey, dialogueId);
        PlayerPrefs.Save();
    }

    public static bool TryGetEditorDialogueOverride(out string dialogueId) {
        dialogueId = PlayerPrefs.GetString(EditorDialogueOverrideKey, "");
        return !string.IsNullOrWhiteSpace(dialogueId);
    }
}
