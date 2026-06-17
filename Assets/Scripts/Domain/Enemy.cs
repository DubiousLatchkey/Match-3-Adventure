using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Linq;

public class Enemy {

    [XmlAttribute("name")]
    public string Name;

    [XmlAttribute("failRate")]
    public float failRate;

    [XmlAttribute("maxRedMana")]
    public int maxRedMana;
    [XmlAttribute("maxBlueMana")]
    public int maxBlueMana;
    [XmlAttribute("maxYellowMana")]
    public int maxYellowMana;

    [XmlArray("priorities")]
    [XmlArrayItem("priority")]
    public int[] Priorities = new int[3];


    [XmlArray("spells")]
    [XmlArrayItem("spell")]
    public Spell[] Spells;

    /*
    [XmlAttribute("spellType")]
    public spellType Type;
    */

    [XmlAttribute("hp")]
    public int Health;

    public Enemy(string n, int[] p, Spell[] s, int h, int r, int b, int y, float fail) {
        Name = n;
        Priorities = p;
        Health = h;
        Spells = s;
        maxRedMana = r;
        maxBlueMana = b;
        maxYellowMana = y;
        failRate = fail;

    }

    public Enemy(string enemyText) {
        string[] enemyTextArray = enemyText.Split(',');
        Name = enemyTextArray[0];
        string[] priorties = enemyTextArray[1].Trim().Split(' ');
        Priorities = new int[] { int.Parse(priorties[0]), int.Parse(priorties[1]), int.Parse(priorties[2]) };
        Health = int.Parse(enemyTextArray[2]);
        string[] maxes = enemyTextArray[3].Trim().Split(' ');
        maxRedMana = int.Parse(maxes[0]);
        maxBlueMana = int.Parse(maxes[1]);
        maxYellowMana = int.Parse(maxes[2]);
        failRate = float.Parse(enemyTextArray[4]);

        Dictionary<string, Spell> spellsDictionary = SpellSerializer.loadSpellsIntoDictionary();
        List<Spell> spells = new List<Spell>();
        for (int i = 5; i < enemyTextArray.Length; i++) {
            spells.Add(spellsDictionary[enemyTextArray[i].Trim()]);
        }
        Spells = spells.ToArray();

    }


    public Enemy() {
        Name = "Default";
        Priorities = new int[] { 0, 0, 0, };
        Health = 100;
        Spells = new Spell[0];

    }

}

[XmlRoot("EnemyContainer")]
public class EnemyContainer {

    [XmlArray("Enemies")]
    [XmlArrayItem("Enemy")]
    public Enemy[] enemies;

    public EnemyContainer() {
        enemies = new Enemy[15];
    }

    public void Save(string path) {
        var serializer = new XmlSerializer(typeof(EnemyContainer));
        using (var stream = new FileStream(path, FileMode.Create)) {
            serializer.Serialize(stream, this);
        }
    }

    public static EnemyContainer Load(string path) {
        var serializer = new XmlSerializer(typeof(EnemyContainer));
        using (var stream = new FileStream(path, FileMode.Open)) {
            return serializer.Deserialize(stream) as EnemyContainer;
        }
    }

    //Loads the xml directly from the given string. Useful in combination with www.text.
    public static EnemyContainer LoadFromText(string text) {
        var serializer = new XmlSerializer(typeof(EnemyContainer));
        return serializer.Deserialize(new StringReader(text)) as EnemyContainer;
    }
}

public class Action {
    int priority = 0;

    public int getPriority() {
        return priority;
    }
    public void setPriority(int priority) {
        this.priority = priority;
    }
}

public class spellAction : Action {
    Spell spell;
    public spellAction(Spell spell) {
        this.spell = spell;
    }
    public Spell getSpell() {
        return spell;
    }
}

public class Move : Action{

    public int originLocation;
    public int swapLocation;
    public int matchLength;
    public int type;

    public Move(int origin, int swap, int length, int t) {
        originLocation = origin;
        swapLocation = swap;
        matchLength = length;
        type = t;
    }

}
