using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

public class WeaponSerializer : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        WeaponContainer weapons = new WeaponContainer();
        weapons.Weapons[0] = new Weapon("Warrior's Staff", new int[]{0, 1, 0}, WeaponType.eot, "Does 1 damage per turn");
        weapons.Weapons[1] = new Weapon("Mage Rapier", new int[] { 3, 2, 0 }, WeaponType.mana, "Does 2 extra damage per sword match");
        weapons.Weapons[2] = new Weapon("Spellslinger's Device", new int[] { 0, 2, 0 }, WeaponType.cast, "Does 2 damage on spell cast");
        weapons.Weapons[3] = new Weapon("Scarlet Staff", new int[] { 0, 1, 0 }, WeaponType.mana, "Red mana matches gain an extra 1");
        weapons.Weapons[4] = new Weapon("Crimson Staff", new int[] { 1, 1, 0 }, WeaponType.eot, "Adds 1 red mana per turn");
        weapons.Weapons[5] = new Weapon("Amplifying Staff", new int[] { 2, 0, 0 }, WeaponType.spellDamage, "Spells do 2 more damage (before multiplier)");
        weapons.Weapons[6] = new Weapon("Illis-infused Lance", new int[] { 3, 1, 0 }, WeaponType.mana, "Does 1 extra damage per sword match");
        weapons.Weapons[7] = new Weapon("Staff of Reanimation", new int[] { 4, 2, 0 }, WeaponType.eot, "Heals 2 health per turn");


        weapons.Save(Path.Combine(Application.persistentDataPath, "weapons.xml"));
    }

    // Update is called once per frame
    void Update()
    {
        
    }

}
