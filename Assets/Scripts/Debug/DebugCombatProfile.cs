using UnityEngine;

[CreateAssetMenu(menuName = "Match3/Debug Combat Profile")]
public sealed class DebugCombatProfile : ScriptableObject {
    public string combatId = "debugSpellTestDummy";
    public int playerHp = 999;
    public int maxRedMana = 99;
    public int maxBlueMana = 99;
    public int maxYellowMana = 99;
    public string equippedWeaponName = "";
    public string[] equippedSpellNames = new string[] {
        "Fireball",
        "Coin Flip",
        "Field Disruption"
    };

    [Header("Dummy Enemy")]
    public string enemyName = "Spell Test Dummy";
    public int enemyHp = 999;
    public int enemyMaxRedMana = 0;
    public int enemyMaxBlueMana = 0;
    public int enemyMaxYellowMana = 0;
    public Vector3Int enemyPriorities = new Vector3Int(0, 0, 0);
    [Range(0f, 1f)] public float enemyFailRate = 1f;
    public string[] enemySpellNames = new string[0];
    public bool disableEnemyActions = true;
}
