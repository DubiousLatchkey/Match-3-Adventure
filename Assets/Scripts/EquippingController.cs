using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EquippingController : MonoBehaviour {

    List<Button> availableSpells;
    List<slot> equippedSpells;
    GameObject availableSpellAnchor;
    GameObject equippedSpellAnchor;

    Button selected;

    public GameObject availableWeaponsList;
    List<GameObject> availableWeapons;
    List<slot> equippedWeapons;

    // Start is called before the first frame update
    void Start()
    {
        //Fetch available spells
        availableSpells = new List<Button>();
        equippedSpells = new List<slot>();
        equippedWeapons = new List<slot>();
        availableWeapons = new List<GameObject>();
        equippedSpells.Add(new slot(GameObject.Find("slot1")));
        equippedSpells.Add(new slot(GameObject.Find("slot2")));
        equippedSpells.Add(new slot(GameObject.Find("slot3")));
        equippedSpells.Add(new slot(GameObject.Find("slot4")));
        equippedSpells.Add(new slot(GameObject.Find("slot5")));
        equippedWeapons.Add(new slot(GameObject.Find("equippedWeaponSlot")));
        availableSpellAnchor = GameObject.Find("availableSpellsList");
        equippedSpellAnchor = GameObject.Find("equippedSpellsList");

        SpellContainer spells = SpellContainer.Load(System.IO.Path.Combine(Application.persistentDataPath, "spells.xml"));
        //int spellCount = 0;
        //Load in available spells
        for (int i = 0; i < spells.spells.Length; i++) {
            int spellStatus = PlayerPrefs.GetInt(spells.spells[i].Name, 0);
            if (spellStatus != 0) {
                availableSpells.Add(Instantiate(Resources.Load<Button>("SpellButton"), availableSpellAnchor.transform));
                //Debug.Log(spells.spells[i].Name + " " + availableSpells.Count);
                availableSpells[availableSpells.Count - 1].GetComponent<SpellButtonHandler>().setSpell(spells.spells[i]);
                availableSpells[availableSpells.Count - 1].GetComponent<DragToSpotBehavior>().isSpell = true;
                //spellCount++;
            }
        }
        //availableSpellAnchor.GetComponent<RectTransform>(). = 100 * spellCount;

        //Position spells into either their box or in the list
        for (int i = 0; i < availableSpells.Count; i++) {
            int spellStatus = PlayerPrefs.GetInt(availableSpells[i].GetComponentInChildren<Text>().text, 0);
            availableSpells[i].transform.localScale = new Vector3(0.75f, 0.75f);

            if(spellStatus > 0  && spellStatus < 6){
                availableSpells[i].GetComponent<DragToSpotBehavior>().myBox = equippedSpells[spellStatus - 1];
                availableSpells[i].transform.SetParent(equippedSpells[spellStatus - 1].getHolding().transform);
                availableSpells[i].transform.localPosition = new Vector3(0, 0);
                equippedSpells[spellStatus - 1].isFull = true;
                equippedSpells[spellStatus - 1].setHoldName(availableSpells[i].GetComponentInChildren<Text>().text);

                //equippedSpells[spellStatus - 1].setHolding(availableSpells[i].gameObject);
            }
            availableSpells[i].GetComponent<SpellButtonHandler>().enabled = false;
            availableSpells[i].GetComponent<TooltipHandler>().enabled = true;
            availableSpells[i].GetComponent<TooltipHandler>().setPosition(new Vector3(0, 75));
            availableSpells[i].GetComponent<DragToSpotBehavior>().enabled = true;

        }

        foreach (slot i in equippedSpells) {
            i.getHolding().GetComponent<BoxCollider2D>().size = i.getHolding().GetComponent<Image>().sprite.bounds.size;
        }
        

        WeaponContainer weapons = WeaponContainer.Load(System.IO.Path.Combine(Application.persistentDataPath, "weapons.xml"));

        //Load in weapon buttons in to available weapons
        for (int i = 0; i < weapons.Weapons.Length; i++) {
            int weaponStatus = PlayerPrefs.GetInt(weapons.Weapons[i].Name, 0);
            if (weaponStatus != 0) {
                availableWeapons.Add(Instantiate(Resources.Load<GameObject>("WeaponButton"), availableWeaponsList.transform));
                availableWeapons[availableWeapons.Count - 1].GetComponent<WeaponButtonHandler>().setWeapon(weapons.Weapons[i]);
                availableWeapons[availableWeapons.Count - 1].GetComponent<DragToSpotBehavior>().enabled = true;
                availableWeapons[availableWeapons.Count - 1].GetComponent<DragToSpotBehavior>().isSpell = false;
                //availableWeapons[availableWeapons.Count - 1].transform.localScale = new Vector3(0.5f, 0.5f);
            }
        }

        for (int i = 0; i < availableWeapons.Count; i++) {
            int weaponStatus = PlayerPrefs.GetInt(availableWeapons[i].GetComponent<WeaponButtonHandler>().getWeapon().Name, 0); // 2 for equipped, 1 for have, 0 for not owned
            if (weaponStatus == 2) {
                //equip
                availableWeapons[i].transform.SetParent(GameObject.Find("equippedWeaponSlot").transform);
                availableWeapons[i].transform.localPosition = new Vector3(0, 0);
                equippedWeapons[0].setHoldName(availableWeapons[i].GetComponent<WeaponButtonHandler>().getWeapon().Name);
                //availableWeapons[i].GetComponent<DragToSpotBehavior>().homePosition = availableWeapons[i].transform.position;
                break;
            }

        }
        
        //Skip scene if no spells and weapons
        if (availableWeapons.Count < 1 && availableSpells.Count < 1) {
            moveToCombat();
        }

    }

        // Update is called once per frame
        void Update()
    {
        
    }

    public slot whichBoxAmIIn(Vector3 position, bool isSpell) {
        float min = 9999;
        List<slot> relevantSlots;
        if (isSpell) { relevantSlots = equippedSpells; }
        else { relevantSlots = equippedWeapons; }
        slot closestBox = null;

        foreach (slot i in relevantSlots) {
            //Debug.Log(i.getHolding().transform.position);
            if (!i.isFull && Vector3.Distance(i.getHolding().transform.position, position) < min) {
                min = Vector3.Distance(i.getHolding().transform.position, position);
                closestBox = i;
            }
        }

        if (min > 175) {
            return null;
        }
        else {
            return closestBox;
        }
    }

    public void moveToCombat() {
        //Sets all spells to 6 for available
        foreach (Button i in availableSpells) {
            PlayerPrefs.SetInt(i.GetComponentInChildren<Text>().text, 6);
            //Debug.Log(i.GetComponentInChildren<Text>().text);
        }
        //Sets equipped spells to proper order
        int counter = 1;
        foreach (slot i in equippedSpells) {
            if (i.getHoldName() != "") {
                PlayerPrefs.SetInt(i.getHoldName(), counter);
                counter++; //Counter makes it so empty spots don't get counted
                //Debug.Log(i.getHoldName());
            }
        }

        //Set equipped weapon and others to unequipped
        foreach (GameObject i in availableWeapons) {
            PlayerPrefs.SetInt(i.GetComponent<WeaponButtonHandler>().getWeapon().Name, 1);
        }
        if (equippedWeapons[0].getHoldName() != "") {
            PlayerPrefs.SetInt(equippedWeapons[0].getHoldName(), 2);
        }

        SceneManager.LoadScene("CombatScene", LoadSceneMode.Single);

    }

}

public class slot {
    GameObject holding;
    public bool isFull;
    string thingBeingHeld;
    bool isSpell;

    public slot(GameObject o) {
        holding = o;
        isFull = false;
    }

    public void setHolding(GameObject holdMe) {
        holding = holdMe;
    }
    public GameObject getHolding() {
        return holding;
    }

    public void setHoldName(string s) {
        thingBeingHeld = s;
    }
    public string getHoldName() {
        return thingBeingHeld;
    }

}
