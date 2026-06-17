using System.Collections.Generic;

public sealed class CombatantState {
    public string Name = "";
    public int Health;
    public int MaxHealth;
    public int RedMana;
    public int BlueMana;
    public int YellowMana;
    public int MaxRedMana;
    public int MaxBlueMana;
    public int MaxYellowMana;
    public float DamageMultiplier = 1f;
    public Weapon Weapon = new Weapon();
    public readonly List<StatusEffect> StatusEffects = new List<StatusEffect>();

    public int GetMana(int id) {
        switch (id) {
            case (int)colorType.red:
                return RedMana;
            case (int)colorType.blue:
                return BlueMana;
            case (int)colorType.yellow:
                return YellowMana;
            default:
                return 0;
        }
    }

    public int GetMaxMana(int id) {
        switch (id) {
            case (int)colorType.red:
                return MaxRedMana;
            case (int)colorType.blue:
                return MaxBlueMana;
            case (int)colorType.yellow:
                return MaxYellowMana;
            default:
                return 0;
        }
    }

    public void SetMana(int id, int value) {
        int max = GetMaxMana(id);
        int clamped = value > max ? max : value;
        clamped = clamped >= 0 ? clamped : 0;

        switch (id) {
            case (int)colorType.red:
                RedMana = clamped;
                break;
            case (int)colorType.blue:
                BlueMana = clamped;
                break;
            case (int)colorType.yellow:
                YellowMana = clamped;
                break;
        }
    }
}
