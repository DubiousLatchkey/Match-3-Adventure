using System.IO;
using System.Xml.Serialization;

public class Spell {

    [XmlAttribute("name")]
    public string Name;

    [XmlArray("costs")]
    [XmlArrayItem("cost")]
    public int[] Costs = new int[6];

    [XmlArray("effects")]
    [XmlArrayItem("effect")]
    public int[] Effects = new int[3];

    [XmlAttribute("spellType")]
    public spellType Type;

    [XmlAttribute("tooltip")]
    public string Tooltip;

    [XmlAttribute("parameters")]
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

    public Spell(string spellText)
    {
        string[] spellTextArray = spellText.Split(',');
        Name = spellTextArray[0];
        string[] costStringArray = spellTextArray[1].Split(' ');
        Costs = new int[] {int.Parse(costStringArray[0]), int.Parse(costStringArray[1]), int.Parse(costStringArray[2]) };
        Parameters = spellTextArray[2];
        Tooltip = spellTextArray[3];
        Type = spellType.Damage;
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

[XmlRoot("SpellCollection")]
public class SpellContainer {
    [XmlArray("Spells")]
    [XmlArrayItem("Spell")]
    public Spell[] spells;

    public SpellContainer() {
        spells = new Spell[57];
    }

    public SpellContainer(int count) {
        spells = new Spell[count];
    }

    public void Save(string path) {
        var serializer = new XmlSerializer(typeof(SpellContainer));
        using (var stream = new FileStream(path, FileMode.Create)) {
            serializer.Serialize(stream, this);
        }
    }

    public static SpellContainer Load(string path) {
        SpellContainer resourceContainer = SpellContentLoader.LoadContainer();
        if (resourceContainer.spells.Length > 0) {
            return resourceContainer;
        }

        var serializer = new XmlSerializer(typeof(SpellContainer));
        using (var stream = new FileStream(path, FileMode.Open)) {
            return serializer.Deserialize(stream) as SpellContainer;
        }
    }

    //Loads the xml directly from the given string. Useful in combination with www.text.
    public static SpellContainer LoadFromText(string text) {
        var serializer = new XmlSerializer(typeof(SpellContainer));
        return serializer.Deserialize(new StringReader(text)) as SpellContainer;
    }

}
