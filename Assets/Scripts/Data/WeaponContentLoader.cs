using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public sealed class WeaponContentFile {
    public WeaponContentEntry[] weapons;
}

[System.Serializable]
public sealed class WeaponContentEntry {
    public string id;
    public int index;
    public string name;
    public int[] effects;
    public string type;
    public string tooltip;
}

public static class WeaponContentLoader {
    private static List<Weapon> cachedWeapons;

    public static List<Weapon> LoadWeapons() {
        if (cachedWeapons != null) {
            return new List<Weapon>(cachedWeapons);
        }

        TextAsset text = Resources.Load<TextAsset>("Data/weapons");
        if (text == null) {
            cachedWeapons = new List<Weapon>();
            return new List<Weapon>(cachedWeapons);
        }

        WeaponContentFile file = JsonUtility.FromJson<WeaponContentFile>(text.text);
        cachedWeapons = new List<Weapon>();
        if (file != null && file.weapons != null) {
            foreach (WeaponContentEntry entry in file.weapons) {
                WeaponType type = WeaponType.none;
                System.Enum.TryParse(entry.type, out type);
                cachedWeapons.Add(new Weapon(entry.name, entry.effects, type, entry.tooltip));
            }
        }

        return new List<Weapon>(cachedWeapons);
    }

    public static WeaponContainer LoadContainer() {
        List<Weapon> weapons = LoadWeapons();
        WeaponContainer container = new WeaponContainer(weapons.Count);
        for (int i = 0; i < weapons.Count; i++) {
            container.Weapons[i] = weapons[i];
        }
        return container;
    }
}
