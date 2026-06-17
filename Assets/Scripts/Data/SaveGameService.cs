using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public sealed class SaveGame {
    public bool savedGameExists;
    public string playerName = "Rachel";
    public string background = "bg";
    public int hp = 100;
    public int maxRedMana = 10;
    public int maxBlueMana = 10;
    public int maxYellowMana = 10;
    public List<SaveIntEntry> ints = new List<SaveIntEntry>();
    public List<SaveStringEntry> strings = new List<SaveStringEntry>();
}

[System.Serializable]
public sealed class SaveIntEntry {
    public string key;
    public int value;
}

[System.Serializable]
public sealed class SaveStringEntry {
    public string key;
    public string value;
}

public static class SaveGameService {
    private const string FileName = "savegame.json";
    private static SaveGame cached;

    public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    public static SaveGame Load() {
        if (cached != null) {
            return cached;
        }

        if (File.Exists(SavePath)) {
            cached = JsonUtility.FromJson<SaveGame>(File.ReadAllText(SavePath));
        }

        if (cached == null) {
            cached = CreateFromPlayerPrefs();
            Save();
        }

        return cached;
    }

    public static void Save() {
        if (cached == null) {
            cached = CreateFromPlayerPrefs();
        }
        File.WriteAllText(SavePath, JsonUtility.ToJson(cached, true));
    }

    public static SaveGame NewGame(IEnumerable<Spell> spells, IEnumerable<Weapon> weapons) {
        cached = new SaveGame {
            savedGameExists = true,
            playerName = "Rachel",
            background = "bg",
            hp = 100,
            maxRedMana = 10,
            maxBlueMana = 10,
            maxYellowMana = 10
        };

        foreach (Spell spell in spells) {
            SetInt(spell.Name, 0, saveImmediately: false);
        }
        foreach (Weapon weapon in weapons) {
            SetInt(weapon.Name, 0, saveImmediately: false);
        }
        Save();
        return cached;
    }

    public static bool SavedGameExists() {
        return Load().savedGameExists;
    }

    public static int GetInt(string key, int defaultValue = 0) {
        SaveGame save = Load();
        switch (key) {
            case "savedGameExists":
                return save.savedGameExists ? 1 : 0;
            case "hp":
                return save.hp;
            case "maxRedMana":
                return save.maxRedMana;
            case "maxBlueMana":
                return save.maxBlueMana;
            case "maxYellowMana":
                return save.maxYellowMana;
        }

        foreach (SaveIntEntry entry in save.ints) {
            if (entry.key == key) {
                return entry.value;
            }
        }

        return PlayerPrefs.GetInt(key, defaultValue);
    }

    public static void SetInt(string key, int value, bool saveImmediately = true) {
        SaveGame save = Load();
        switch (key) {
            case "savedGameExists":
                save.savedGameExists = value == 1;
                break;
            case "hp":
                save.hp = value;
                break;
            case "maxRedMana":
                save.maxRedMana = value;
                break;
            case "maxBlueMana":
                save.maxBlueMana = value;
                break;
            case "maxYellowMana":
                save.maxYellowMana = value;
                break;
            default:
                SaveIntEntry entry = save.ints.Find(x => x.key == key);
                if (entry == null) {
                    entry = new SaveIntEntry { key = key };
                    save.ints.Add(entry);
                }
                entry.value = value;
                break;
        }

        if (saveImmediately) {
            Save();
        }
    }

    public static string GetString(string key, string defaultValue = "") {
        SaveGame save = Load();
        switch (key) {
            case "name":
                return save.playerName;
            case "background":
                return save.background;
        }

        foreach (SaveStringEntry entry in save.strings) {
            if (entry.key == key) {
                return entry.value;
            }
        }

        return PlayerPrefs.GetString(key, defaultValue);
    }

    public static void SetString(string key, string value, bool saveImmediately = true) {
        SaveGame save = Load();
        switch (key) {
            case "name":
                save.playerName = value;
                break;
            case "background":
                save.background = value;
                break;
            default:
                SaveStringEntry entry = save.strings.Find(x => x.key == key);
                if (entry == null) {
                    entry = new SaveStringEntry { key = key };
                    save.strings.Add(entry);
                }
                entry.value = value;
                break;
        }

        if (saveImmediately) {
            Save();
        }
    }

    private static SaveGame CreateFromPlayerPrefs() {
        return new SaveGame {
            savedGameExists = PlayerPrefs.GetInt("savedGameExists", 0) == 1,
            playerName = PlayerPrefs.GetString("name", "Rachel"),
            background = PlayerPrefs.GetString("background", "bg"),
            hp = PlayerPrefs.GetInt("hp", 100),
            maxRedMana = PlayerPrefs.GetInt("maxRedMana", 10),
            maxBlueMana = PlayerPrefs.GetInt("maxBlueMana", 10),
            maxYellowMana = PlayerPrefs.GetInt("maxYellowMana", 10)
        };
    }
}
