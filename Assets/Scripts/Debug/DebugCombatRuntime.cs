using System.Collections.Generic;

public static class DebugCombatRuntime {
    public static bool HasActiveProfile { get; private set; }
    public static bool DisableEnemyActions { get; private set; }
    private static Enemy enemyOverride;

    public static void ApplyProfile(DebugCombatProfile profile) {
        HasActiveProfile = profile != null;
        DisableEnemyActions = profile != null && profile.disableEnemyActions;
        enemyOverride = profile != null ? BuildEnemy(profile) : null;
    }

    public static void Clear() {
        HasActiveProfile = false;
        DisableEnemyActions = false;
        enemyOverride = null;
    }

    public static bool TryGetEnemyOverride(out Enemy enemy) {
        enemy = enemyOverride;
        return enemy != null;
    }

    private static Enemy BuildEnemy(DebugCombatProfile profile) {
        Dictionary<string, Spell> spellsByName = SpellContentLoader.LoadSpellsByName();
        List<Spell> spells = new List<Spell>();
        foreach (string spellName in profile.enemySpellNames) {
            if (spellsByName.ContainsKey(spellName)) {
                spells.Add(spellsByName[spellName]);
            }
        }

        return new Enemy(
            profile.enemyName,
            new int[] { profile.enemyPriorities.x, profile.enemyPriorities.y, profile.enemyPriorities.z },
            spells.ToArray(),
            profile.enemyHp,
            profile.enemyMaxRedMana,
            profile.enemyMaxBlueMana,
            profile.enemyMaxYellowMana,
            profile.enemyFailRate);
    }
}
