using UnityEngine.SceneManagement;

public static class GearStopRuntime {
    public static string GearStopToLoad { get; private set; } = "";

    public static bool HasActiveGearStop {
        get { return !string.IsNullOrWhiteSpace(GearStopToLoad); }
    }

    public static void LoadGearStop(string gearStopId) {
        GearStopToLoad = gearStopId;
        SceneManager.LoadScene("GearUpScene", LoadSceneMode.Single);
    }

    public static void LoadDialogue(string dialogueId) {
        GearStopToLoad = "";
        DialogueController.dialogueToLoad = dialogueId;
        SceneManager.LoadScene("DialogueScene", LoadSceneMode.Single);
    }

    public static void LoadCombat(string combatId) {
        GearStopToLoad = "";
        GridController.combatToLoad = combatId;
        SceneManager.LoadScene("CombatScene", LoadSceneMode.Single);
    }
}
