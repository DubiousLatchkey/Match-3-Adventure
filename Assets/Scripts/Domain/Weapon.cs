using System.IO;
using System.Xml.Serialization;
using UnityEngine;

public enum WeaponType {
    eot, //Effects: [0=damage - 1-3 = red-blue-green mana - 4=healing, amount, TBD]
    damage,
    mana, //Effects: [Mana type, bonus amount, TBD]
    status,
    cast, //Effects: [0=damage, amount, TBD]
    spellDamage, //Effects[Amount of Extra Damage, TBD, TBD]
    none
}

public class Weapon {

    [XmlAttribute("name")]
    public string Name;

    [XmlArray("effects")]
    [XmlArrayItem("effect")]
    public int[] Effects = new int[3];

    [XmlAttribute("weaponType")]
    public WeaponType Type;

    [XmlAttribute("tooltip")]
    public string Tooltip;

    public Weapon(string n, int[] e, WeaponType t, string tt) {
        Name = n;
        Effects = e;
        Type = t;
        Tooltip = tt;
    }
    public Weapon() {
        Name = "Default";
        Effects = new int[] { -1, -1, -1 };
        Type = WeaponType.none;
        Tooltip = "";
    }

    public void eotEffect(bool ownedByPlayer) {
        if (Type == WeaponType.eot) {
            GridController gridController = GameObject.Find("Grid").GetComponent<GridController>();
            EnemyController enemyController = GameObject.Find("Grid").GetComponent<EnemyController>();

            if (Effects[0] == 0) { //Do damage each turn
                if (ownedByPlayer) {
                    GameObject.Find("Grid").GetComponent<EnemyController>().dealDamage(Effects[1]);
                }
                else {
                    GameObject.Find("Grid").GetComponent<GridController>().dealDamage(Effects[1]);
                }
            }
            else if (Effects[0] == 1) {
                if (ownedByPlayer) {
                    gridController.setRedMana(Effects[1] + gridController.getRedMana());
                }
                else {
                    enemyController.setRedMana(Effects[1] + enemyController.getRedMana());
                }
            }
            else if (Effects[0] == 2) {
                if (ownedByPlayer) {
                    gridController.setBlueMana(Effects[1] + gridController.getBlueMana());
                }
                else {
                    enemyController.setBlueMana(Effects[1] + enemyController.getBlueMana());
                }
            }
            else if (Effects[0] == 3) {
                if (ownedByPlayer) {
                    gridController.setYellowMana(Effects[1] + gridController.getYellowMana());
                }
                else {
                    enemyController.setYellowMana(Effects[1] + enemyController.getYellowMana());
                }
            }
            else if (Effects[0] == 4) { //Heal user
                if (ownedByPlayer) {
                    GameObject.Find("Grid").GetComponent<GridController>().dealDamage(-Effects[1]);
                }
                else {
                    GameObject.Find("Grid").GetComponent<EnemyController>().dealDamage(-Effects[1]);
                }
            }
        }
    }

    public float manaEffect(int type, float initialAmount) { //Changes mana scored of type
        if (Type == WeaponType.mana) {
            if (type == Effects[0]) {
                return initialAmount + Effects[1];
            }
            else {
                return initialAmount;
            }
        }
        return initialAmount;
    }

    public void castEffect(bool ownedByPlayer) {
        if (Type == WeaponType.cast) { //Deal x damage
            if (ownedByPlayer) {
                GameObject.Find("Grid").GetComponent<EnemyController>().dealDamage(Effects[1]);
            }
            else {
                GameObject.Find("Grid").GetComponent<GridController>().dealDamage(Effects[1]);
            }
        }
    }

    public int spellDamageEffect() {
        if (Type == WeaponType.spellDamage) {
            return Effects[0];
        }
        return 0;
    }

}

[XmlRoot("WeaponCollection")]
public class WeaponContainer {
    [XmlArray("Weapons")]
    [XmlArrayItem("Weapon")]
    public Weapon[] Weapons;

    public WeaponContainer() {
        Weapons = new Weapon[8];
    }

    public WeaponContainer(int count) {
        Weapons = new Weapon[count];
    }

    public void Save(string path) {
        var serializer = new XmlSerializer(typeof(WeaponContainer));
        using (var stream = new FileStream(path, FileMode.Create)) {
            serializer.Serialize(stream, this);
        }
    }

    public static WeaponContainer Load(string path) {
        WeaponContainer resourceContainer = WeaponContentLoader.LoadContainer();
        if (resourceContainer.Weapons.Length > 0) {
            return resourceContainer;
        }

        var serializer = new XmlSerializer(typeof(WeaponContainer));
        using (var stream = new FileStream(path, FileMode.Open)) {
            return serializer.Deserialize(stream) as WeaponContainer;
        }
    }

    //Loads the xml directly from the given string. Useful in combination with www.text.
    public static WeaponContainer LoadFromText(string text) {
        var serializer = new XmlSerializer(typeof(WeaponContainer));
        return serializer.Deserialize(new StringReader(text)) as WeaponContainer;
    }

}
