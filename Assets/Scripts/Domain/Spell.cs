public class Spell {

    public string Name;

    public int[] Costs = new int[6];

    public int[] Effects = new int[3];

    public spellType Type;

    public string Tooltip;

    public string Parameters;

    public Spell(string n, int[] c, int[] e, spellType t, string tt) {
        Name = n;
        Costs = c;
        Effects = e;
        Type = t;
        Tooltip = tt;
        Parameters = "";
    }
    public Spell(string n, int[] c, int[] e, spellType t, string parameters, string tt) {
        Name = n;
        Costs = c;
        Effects = e;
        Type = t;
        Tooltip = tt;
        Parameters = parameters;
    }
    public Spell(string name, int[] costs, string parameters, string tooltip) {
        Name = name;
        Costs = costs;
        Effects = new int[] { 0, 0, 0 };
        Type = spellType.Damage;
        Parameters = parameters;
        Tooltip = tooltip;
    }

    public Spell() {
        Name = "Default";
        Costs = new int[] { 0, 0, 0 };
        Effects = new int[] { 0, 0, 0 };
        Type = spellType.Damage;
        Tooltip = "";
    }

    public int getConvertedManaCost() {
        int sum = 0;
        foreach (int i in Costs) {
            sum += i;
        }
        return sum;
    }


}

public enum spellType {
    Damage, //Effects: [damage, healing, (0: doesn't end turn, 1: ends turn)]
    Healing,  //Effects: [heal amount, TBD, (0: doesn't end turn, 1: ends turn)]
    DestroyLines, //Effects: [starting lines, TBD, number of lines]
    DestroySquare, //Effects: [0 = targeted, 1 = fixed, size of square, TBD]
    ColorShift,  //Effects: [starting type, ending type(0 is score), TBD]
    RandomShift, 
    PersistentDamage, //Effects: [damager per turn, number of turns, TBD]
    SpawnType,
    SpawnRateChange,
    Reshuffle, //Effects: [0 = full area, 1 = limited area, TBD, TBD]
    Communism, //Effects: [Resources affected (0:mana, 1:health, 2, both), TBD, TBD]
    ManaTheft, //Effects:[(Resources affected (0:all, 1:red, 2:blue, 3:yellow), Amount to steal, (0: doesn't end turn, 1: ends turn)]
    Shield, //Effects [0=percentage 1=amount, amount prevented, how many turns it lasts]
    DamageAndStatus,
    TargetShift, //Effects [Resulting type, TBD, TBD]
    Ritual, //Effects [target mana type, mana generated, TBD]
    DamageBuff,//Effects [Amount of damage increased, number of turns, TBD]
    DamageWithEffects,
    DestroyAndDamage,
    Buff,
    RemoveBuff,
    Destroy,
    ExtraTurn,
}

