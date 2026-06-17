using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public sealed class EnemyContentFile {
    public EnemyContentEntry[] enemies;
}

[System.Serializable]
public sealed class EnemyContentEntry {
    public string id;
    public string key;
    public string name;
    public int[] priorities;
    public int hp;
    public int maxRedMana;
    public int maxBlueMana;
    public int maxYellowMana;
    public float failRate;
    public string[] spells;
}

public static class EnemyContentLoader {
    private static List<Enemy> cachedEnemies;
    private static Dictionary<string, Enemy> cachedByKey;

    public static List<Enemy> LoadEnemies() {
        if (cachedEnemies != null) {
            return new List<Enemy>(cachedEnemies);
        }

        TextAsset text = Resources.Load<TextAsset>("Data/enemies");
        if (text == null) {
            Debug.LogError("Missing enemy data resource at Assets/Resources/Data/enemies.json");
            cachedEnemies = new List<Enemy>();
            cachedByKey = new Dictionary<string, Enemy>();
            return new List<Enemy>(cachedEnemies);
        }

        EnemyContentFile file = JsonUtility.FromJson<EnemyContentFile>(text.text);
        Dictionary<string, Spell> spellsByName = SpellContentLoader.LoadSpellsByName();
        cachedEnemies = new List<Enemy>();
        cachedByKey = new Dictionary<string, Enemy>();

        if (file != null && file.enemies != null) {
            foreach (EnemyContentEntry entry in file.enemies) {
                Enemy enemy = CreateEnemy(entry, spellsByName);
                cachedEnemies.Add(enemy);

                if (!string.IsNullOrWhiteSpace(entry.key)) {
                    cachedByKey[entry.key] = enemy;
                }
                cachedByKey[enemy.Name] = enemy;
            }
        }

        return new List<Enemy>(cachedEnemies);
    }

    public static Dictionary<string, Enemy> LoadEnemiesByKey() {
        if (cachedByKey == null) {
            LoadEnemies();
        }

        return new Dictionary<string, Enemy>(cachedByKey);
    }

    private static Enemy CreateEnemy(EnemyContentEntry entry, Dictionary<string, Spell> spellsByName) {
        List<Spell> spells = new List<Spell>();
        if (entry.spells != null) {
            foreach (string spellName in entry.spells) {
                if (spellsByName.ContainsKey(spellName)) {
                    spells.Add(spellsByName[spellName]);
                }
                else {
                    Debug.LogWarning("Enemy '" + entry.name + "' references missing spell '" + spellName + "'.");
                }
            }
        }

        return new Enemy(entry.name, entry.priorities, spells.ToArray(), entry.hp,
            entry.maxRedMana, entry.maxBlueMana, entry.maxYellowMana, entry.failRate);
    }
}
