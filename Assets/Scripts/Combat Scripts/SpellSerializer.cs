using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

public class SpellSerializer : MonoBehaviour
{

    // Start is called before the first frame update
    void Start()
    {
        SpellContainer spells = new SpellContainer();

        spells.spells[0] = new Spell("Fireball", new int[]{ 7, 0, 0 }, "dealDamage 10", "Deals 10 damage\nDoesn't end turn"); 
        spells.spells[1] = new Spell("Aether", new int[] { 0, 6, 0 }, "dealDamage 5\nheal 5", "Deals 5 damage and heals 5\nDoesn't end turn"); 
        spells.spells[2] = new Spell("Rebase", new int[] { 0, 0, 7 }, "destroySetLine 0 1\nendTurn ", "Scores bottom row\nEnds turn");
        spells.spells[3] = new Spell("Ethereal Vivisection", new int[] { 1, 1, 6 }, "destroySquare 1\nendTurn ", "Destroys 3x3 grid around target space\nEnds turn");
        spells.spells[4] = new Spell("Toxic Deluge", new int[] { 0, 1, 0 }, "inflictStatusEffect Poison 3 5 spellFlash\nendTurn ", "Poisons opponent, dealing 3 damage per turn for 5 turns\nEnds turn");
        spells.spells[5] = new Spell("Clean Slate", new int[] { 0, 0, 10 }, "destroyAllPiecesOfType 0\ndestroyAllPiecesOfType 1\ndestroyAllPiecesOfType 2\ndestroyAllPiecesOfType 3\ndestroyAllPiecesOfType 4\n destroyAllPiecesOfType 5\nendTurn ", "Reshuffles entire board\nEnds turn");
        spells.spells[6] = new Spell("Swords to Plowshares", new int[] { 7, 0, 5 }, "shiftColor 3 -1\nendTurn ", "Scores all sword pieces\nEnds turn");
        spells.spells[7] = new Spell("Balance", new int[] { 0, 6, 5 }, "balance \nendTurn", "Adds up players' mana of each type and redistributes equally\nEnds turn");
        spells.spells[8] = new Spell("Restore", new int[] { 0, 6, 0 }, "heal 10\nendTurn ", "Heals 10 health\nEnds turn");
        spells.spells[9] = new Spell("Mana Drain", new int[] { 0, 5, 2 }, "stealMana all 3\nendTurn", "Steals 3 mana of each color\nEnds turn");
        spells.spells[10] = new Spell("Barrier", new int[] { 1, 5, 2 }, "grantStatusEffect Shield 2 6\nendTurn ", "Prevents 2 of each instance of damage for the next 6 turns\nEnds turn");
        spells.spells[11] = new Spell("Cantrip", new int[] { 0, 1, 0 }, "dealDamage 0", "Does 0 Damage\nDoesn't end turn");
        spells.spells[12] = new Spell("Enflame", new int[] { 4, 0, 0 }, "dealDamage 6\nendTurn", "Deals 6 damage\nEnds turn");
        spells.spells[13] = new Spell("Fiery Transfusion", new int[] { 0, 4, 4 }, "addMana 0 5", "Generates 5 red mana\nDoesn't end turn");
        spells.spells[14] = new Spell("Precision Strike", new int[] { 4, 5, 2 }, "shiftTarget 3\nendTurn", "Changes target piece into a sword\nEnds turn");
        spells.spells[15] = new Spell("Invigorate", new int[] { 3, 6, 0 }, "grantStatusEffect DamageBuff 2 6\nendTurn ", "Grants 2 extra damage per attack for 6 turns\nEnds turn");
        spells.spells[16] = new Spell("Explosive Outburst", new int[] { 3, 7, 0 }, "dealDamageEqualTo getRedMana\nsetMana 0 0\nendTurn ", "Deals damage equal to red mana and drains red mana to zero\nEnds turn");
        spells.spells[17] = new Spell("Play with Fire", new int[] { 5, 2, 5 }, "randomShift 7 3\nendTurn", "Turns 7 random pieces into swords\nEnds turn");
        spells.spells[18] = new Spell("Shared Torment", new int[] { 5, 3, 0 }, "dealDamage 7\nheal -7\nendTurn", "Deals 7 damage to each player\nEnds turn");
        spells.spells[19] = new Spell("Life and Death", new int[] { 2, 7, 3 }, "dealDamageEqualTo countHeartPiecesTimes2\ndestroyAllPiecesOfType 4\nendTurn", "Destroys all heart pieces and deals 2 damage for each piece destroyed\nEnds turn");
        spells.spells[20] = new Spell("Draining Chill", new int[] { 2, 5, 0 }, "dealDamage 3\nheal 5\nendTurn", "Deals 3 damage and heals 5\nEnds turn");
        spells.spells[21] = new Spell("Burst of Life", new int[] { 5, 8, 0 }, "dealDamageEqualTo tenthOfHealth\nendTurn", "Does 1 damage for every 10 points of your health\nEnds turn");
        spells.spells[22] = new Spell("Nightingale's Boon", new int[] { 2, 6, 1 }, "grantStatusEffect HealingBuff 2 6\nendTurn", "Grants 2 additional health per healing for 6 turns\nEnds turn");
        spells.spells[23] = new Spell("Breath of Vitality", new int[] { 0, 8, 6 }, "shiftColor 4 -1\nendTurn", "Scores all heart pieces\nEnds turn");
        spells.spells[24] = new Spell("Cleansing wave", new int[] { 0, 5, 0 }, "cleanse player\ncleanse enemy", "Removes all status effects from both players\nEnds turn");
        spells.spells[25] = new Spell("Pillar of Destruction", new int[] { 0, 1, 7 }, "destroyColumn random", "Score a random column\nEnds turn");
        spells.spells[26] = new Spell("Bounty of Mana", new int[] { 2, 8, 6 }, "grantStatusEffect ScoreBuff 2 5", "Grants additional 2 to all piece scoring for 5 turns\nEnds turn");
        spells.spells[27] = new Spell("Timeshift", new int[] { 0, 3, 4 }, "grantExtraTurn ", "Grants an extra turn (Does not stack with 4 and 5 in a rows)\nDoesn't end turn");
        spells.spells[28] = new Spell("Flurry Warp", new int[] { 3, 3, 6 }, "randomShift 9 -1\nendTurn ", "Score 9 random pieces\nEnds turn");
        spells.spells[29] = new Spell("Warp", new int[] { 0, 4, 5 }, "destroySquare 0", "Score target piece\nDoesn't end turn");
        spells.spells[30] = new Spell("Distortion Strike", new int[] { 0, 4, 4 }, "dealDamage 4\nrandomShift 1 -1", "Deals 4 damage and scores a random piece \nEnds turn");
        spells.spells[31] = new Spell("Icy Transfusion", new int[] { 4, 0, 4 }, "addMana 1 5", "Generates 5 blue mana\nDoesn't end turn");
        spells.spells[32] = new Spell("Slow", new int[] { 0, 6, 4 }, "inflictStatusEffect AttackDebuff 2 6\nendTurn", "Inflicts -2 damage per foe's attack for 6 turns\nEnds turn");
        spells.spells[33] = new Spell("Icicle Spear", new int[] { 0, 8, 5 }, "dealDamageEqualTo countBlueManaPieces\nendTurn", "Deals 1 damage for each blue mana piece on the board\nEnds turn");
        spells.spells[34] = new Spell("Blueshift", new int[] { 3, 6, 5 }, "shiftColor 0 1", "Turns all red mana pieces into blue mana pieces\nEnds turn");
        spells.spells[35] = new Spell("Doubling Boon", new int[] { 0, 7, 2 }, "grantStatusEffect doubleDamage 2 1", "Grants double damage for 1 turn\nDoesn't end turn");
        spells.spells[36] = new Spell("Power Surge", new int[] { 3, 5, 3 }, "setMultiplier 2", "Maxes out damage multiplier\nDoesn't end turn");
        spells.spells[37] = new Spell("Lock", new int[] { 3, 1, 6 }, "inflictStatusEffect AttackDebuff 6 3\nendTurn", "Inflicts -6 damage per foe's attack for 3 turns\nEnds turn");
        spells.spells[38] = new Spell("Seal", new int[] { 3, 2, 5 }, "inflictStatusEffect Seal 1 4\nendTurn", "Inflicts inability to cast spells for 4 turns\nEnds turn");
        spells.spells[39] = new Spell("Persistent Flame", new int[] { 4, 1, 5 }, "inflictStatusEffect Burn 1 7\nendTurn", "Inflicts 1 damage on foe per turn\nEnds turn");
        spells.spells[40] = new Spell("Staggering Blow", new int[] { 5, 0, 5 }, "dealDamage 5\ninflictStatusEffect Seal 1 2\nendTurn", "Deals 5 damage and inflicts inability to cast spells for 2 turns\nEnds turn");
        spells.spells[41] = new Spell("Lightning Storm", new  int[] { 5, 0, 7 }, "shiftColor 2 -1\nendTurn", "Scores all yellow mana pieces\nEnds turn");
        spells.spells[42] = new Spell("Ionize", new int[] { 6, 3, 5 }, "shiftColor 1 2\nendTurn", "Turns all blue mana pieces into yellow mana pieces\nEnds turn");
        spells.spells[43] = new Spell("Stonewall", new int[] { 3, 1, 7 }, "grantStatusEffect Shield 7 3\nendTurn", "Grants -7 damage from foes attacks for 3 turns\nnEnds turn");
        spells.spells[44] = new Spell("Discharge", new int[] { 3, 0, 4 }, "dealDamage 10\ngrantStatusEffect attackDebuff 6 3\nendTurn", "Deals 10 damage and inflicts -6 damage per self's attack for 3 turns\nEnds turn");
        spells.spells[45] = new Spell("Cross Blitz", new int[] { 8, 0, 6 }, "dealDamageEqualTo countMultiplierPiecesTimes2\ndestroyAllPiecesOfType 5\nendTurn", "Destroys all multiplier pieces and deals 2 damage for each piece destroyed\nEnds turn");
        spells.spells[46] = new Spell("Mass polymorph", new int[] { 3, 0, 7 }, "randomShift 10 7\nendTurn", "Shifts ten random pieces into another type\nEnds turn");
        spells.spells[47] = new Spell("Melt", new int[] { 5, 0, 3 }, "dealDamageEqualTo countIfFewerThan8Blues\nendTurn", "Deals 5 damage.  Deals 8 damage instead if there are fewer than 8 blue mana pieces on the board\nEnds turn");
        spells.spells[48] = new Spell("The Four Winds", new int[] { 6, 0, 4 }, "dealDamageEqualTo fiveIfCornersAreDifferent\nscore 0\nscore 7\nscore 63\nscore 56\nendTurn", "Scores the corner pieces and deals 5 damage if those pieces were all different\nEnds turn");
        spells.spells[49] = new Spell("Set Ablaze", new int[] { 3, 3, 6 }, "randomShift 6 1\nendTurn", "Shifts 6 random pieces into red mana pieces\nEnds turn");
        spells.spells[50] = new Spell("Enrage", new int[] { 8, 2, 2 }, "grantStatusEffect damageBuffBasedOnMana 0 5\nendTurn", "Grants extra damage equal to a quarter of red mana for 5 turns\nEnds turn");
        spells.spells[51] = new Spell("Stun", new int[] { 6, 1, 6 }, "dealDamage 3\ninflictStatusEffect NegativeScoring 1 5\nendTurn", "Deals 3 damage and inflicts -1 to all foe's scoring for 5 turns\nEnds turn");
        spells.spells[52] = new Spell("Field Attenuation", new int[] { 3, 1, 6 }, "shiftColor 3 mana\nshiftColor 4 mana\nshiftcolor 5 mana\nendTurn", "shifts non-mana pieces into mana pieces\nEnds turn");
        spells.spells[53] = new Spell("Eruption", new int[] { 6, 2, 4 }, "dealDamageEqualTo countRedManaPieces\nendTurn", "Deals 1 damage for each red mana piece on the board\nEnds turn");
        spells.spells[54] = new Spell("Omni Charm", new int[] { 3, 4, 4 }, "dealDamage 3\nheal 3\nrandomShift 1 -1\nrandomShift 1 7", "Deals 3 Damage, Heals 3, scores a random piece, and shifts one random piece into another type\nDoesn't end turn");
        spells.spells[55] = new Spell("Blue Elemental Blast", new int[] { 0, 10, 0 }, "dealDamage 15\nendTurn", "Deals 15 Damage\nEnds Turn");
        spells.spells[56] = new Spell("Amplify", new int[] { 1, 5, 2 }, "grantStatusEffect SpellDamageIncreaseEffect 5 8", "Adds 5 Damage to all spell damage dealt for the next 6 turns\nDoesn't end Turn ");


        spells.Save(Path.Combine(Application.persistentDataPath, "spells.xml"));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

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

    public void Save(string path) {
        var serializer = new XmlSerializer(typeof(SpellContainer));
        using (var stream = new FileStream(path, FileMode.Create)) {
            serializer.Serialize(stream, this);
        }
    }

    public static SpellContainer Load(string path) {
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
