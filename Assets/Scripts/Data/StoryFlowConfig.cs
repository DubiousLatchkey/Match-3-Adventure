using UnityEngine;

[System.Serializable]
public sealed class StoryFlowData {
    public string newGameStart = "opening";
    public string defaultResume = "revelation";
}

public static class StoryFlowConfig {
    private const string ResourcePath = "Data/story_flow";
    private static StoryFlowData cached;

    public static StoryFlowData Load() {
        if (cached != null) {
            return cached;
        }

        TextAsset text = Resources.Load<TextAsset>(ResourcePath);
        if (text == null) {
            Debug.LogWarning("Missing story flow config at Resources/" + ResourcePath + ". Using defaults.");
            cached = new StoryFlowData();
            return cached;
        }

        cached = JsonUtility.FromJson<StoryFlowData>(text.text);
        if (cached == null) {
            cached = new StoryFlowData();
        }

        return cached;
    }

    public static string NewGameStart {
        get {
            string value = Load().newGameStart;
            return string.IsNullOrWhiteSpace(value) ? "opening" : value;
        }
    }

    public static string DefaultResume {
        get {
            string value = Load().defaultResume;
            return string.IsNullOrWhiteSpace(value) ? "revelation" : value;
        }
    }
}
