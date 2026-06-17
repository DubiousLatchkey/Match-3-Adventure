using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public sealed class SpellContentFile {
    public SpellContentEntry[] spells;
}

[System.Serializable]
public sealed class SpellContentEntry {
    public string id;
    public int index;
    public string name;
    public int[] costs;
    public string parameters;
    public string tooltip;
}

public static class SpellContentLoader {
    private static List<Spell> cachedSpells;
    private static Dictionary<string, Spell> cachedByName;

    public static List<Spell> LoadSpells() {
        if (cachedSpells != null) {
            return new List<Spell>(cachedSpells);
        }

        TextAsset text = Resources.Load<TextAsset>("Data/spells");
        if (text == null) {
            Debug.LogError("Missing spell data resource at Assets/Resources/Data/spells.json");
            cachedSpells = new List<Spell>();
            return new List<Spell>(cachedSpells);
        }

        SpellContentFile file = JsonUtility.FromJson<SpellContentFile>(text.text);
        cachedSpells = new List<Spell>();
        if (file != null && file.spells != null) {
            foreach (SpellContentEntry entry in file.spells) {
                cachedSpells.Add(new Spell(entry.name, entry.costs, entry.parameters, entry.tooltip));
            }
        }

        return new List<Spell>(cachedSpells);
    }

    public static Dictionary<string, Spell> LoadSpellsByName() {
        if (cachedByName != null) {
            return new Dictionary<string, Spell>(cachedByName);
        }

        cachedByName = new Dictionary<string, Spell>();
        foreach (Spell spell in LoadSpells()) {
            cachedByName[spell.Name] = spell;
        }
        return new Dictionary<string, Spell>(cachedByName);
    }
}
