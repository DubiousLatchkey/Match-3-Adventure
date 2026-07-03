using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public sealed class GearStopContent {
    public string[] nextStops;
}

public static class GearStopContentLoader {
    private const string ResourceFolder = "Data/GearStops";
    private static Dictionary<string, GearStopContent> cachedById;

    public static GearStopContent Load(string id) {
        if (string.IsNullOrWhiteSpace(id)) {
            return null;
        }

        Dictionary<string, GearStopContent> stops = LoadAllById();
        stops.TryGetValue(id, out GearStopContent stop);
        return stop;
    }

    public static Dictionary<string, GearStopContent> LoadAllById() {
        if (cachedById != null) {
            return new Dictionary<string, GearStopContent>(cachedById);
        }

        cachedById = new Dictionary<string, GearStopContent>();
        TextAsset[] assets = Resources.LoadAll<TextAsset>(ResourceFolder);
        foreach (TextAsset asset in assets) {
            GearStopContent stop = JsonUtility.FromJson<GearStopContent>(asset.text);
            if (stop == null) {
                Debug.LogWarning("Could not parse gear stop resource: " + asset.name);
                continue;
            }

            if (stop.nextStops == null) {
                stop.nextStops = new string[0];
            }

            cachedById[asset.name] = stop;
        }

        return new Dictionary<string, GearStopContent>(cachedById);
    }

    public static void ClearCache() {
        cachedById = null;
    }
}
