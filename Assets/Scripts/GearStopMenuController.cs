using UnityEngine;
using UnityEngine.UI;

public sealed class GearStopMenuController : MonoBehaviour {
    [Header("Content")]
    public string fallbackGearStopId = "";

    [Header("Scene References")]
    public Transform optionListParent;
    public Button optionButtonPrefab;
    public EquippingController equippingController;
    public bool clearExistingOptions = true;

    private void Start() {
        string gearStopId = GearStopRuntime.HasActiveGearStop ? GearStopRuntime.GearStopToLoad : fallbackGearStopId;
        if (string.IsNullOrWhiteSpace(gearStopId)) {
            Debug.Log("GearStopMenuController has no active gear stop to render.");
            return;
        }

        GearStopContent stop = GearStopContentLoader.Load(gearStopId);
        if (stop == null) {
            Debug.LogWarning("Missing gear stop: " + gearStopId);
            return;
        }

        Render(stop);
    }

    private void Render(GearStopContent stop) {
        if (optionListParent == null || optionButtonPrefab == null) {
            Debug.LogWarning("GearStopMenuController needs optionListParent and optionButtonPrefab assigned.");
            return;
        }

        if (clearExistingOptions) {
            for (int i = optionListParent.childCount - 1; i >= 0; i--) {
                Destroy(optionListParent.GetChild(i).gameObject);
            }
        }

        foreach (string dialogueId in stop.nextStops) {
            if (string.IsNullOrWhiteSpace(dialogueId)) {
                continue;
            }

            Button button = Instantiate(optionButtonPrefab, optionListParent);
            Text label = button.GetComponentInChildren<Text>();
            if (label != null) {
                label.text = DialogueTitleLoader.GetTitle(dialogueId);
            }

            string captured = dialogueId;
            button.onClick.AddListener(() => FollowTransition(captured));
        }
    }

    private void FollowTransition(string dialogueId) {
        if (equippingController != null) {
            equippingController.SaveEquipment();
        }

        GearStopRuntime.LoadDialogue(dialogueId);
    }
}
