using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.UI;

public class GridController : MonoBehaviour, PlayerController{

    public static bool isTurn;
    public static bool madeMove;
    public static ThingController selectedPiece;
    public static bool transitioning;
    public static bool isCasting;
    public static int turnNumber;
    public static bool extraTurn;
    public static string combatToLoad = "TestCombat";

    private static float[] probabilities = { 21, 21, 21, 15, 15, 7 };

    Spell currentSpell;
    List<Thing> grid;
    public const int BOARDLENGTH = 8;
    public const float maxMultiplier = 2f;
    public IDictionary<int, Color> matchingColors;
    Dictionary<int, int> typeCounts;
    SortedSet<int> toDelete;
    SortedSet<int> toDestroy;
    AudioSource audio;
    public AudioSource jingle;
    public List<AudioSource> typeJingles;
    List<Move> moves;
    string actionAndParameters;

    int redMana = 0;
    int blueMana = 0;
    int yellowMana = 0;
    int health;
    int totalHealth;
    int maxRedMana;
    int maxBlueMana;
    int maxYellowMana;
    public float damageMultiplier = 1;

    Text redManaTextBox;
    Text blueManaTextBox;
    Text yellowManaTextBox;
    Text hp;
    Text multiplier;
    Text name;

    BarHandler healthBar;
    BarHandler redBar;
    BarHandler blueBar;
    BarHandler yellowBar;
    BarHandler multiplierBar;
    Image background;
    Image enemyBackground;

    List<Button> equippedSpells;
    List<StatusEffect> statusEffects;
    Etcetera extraStatusEffects;

    public Weapon weapon;
    public musicController music;
    public Image portrait;
    public Image skydrop;
    public GameObject weaponObject;
    public GameObject paperback;
    public GameObject enemyPaperback;
    public GameObject backdrop;
    public GameObject targetingIndicator;
    public TextAsset spellTexts;

    // Start is called before the first frame update
    void Start() {
        //Initialization
        isCasting = false;
        transitioning = false;
        currentSpell = null;
        turnNumber = 0;
        toDelete = new SortedSet<int>();
        toDestroy = new SortedSet<int>();
        madeMove = false;
        isTurn = true;
        grid = new List<Thing>();
        audio = GetComponent<AudioSource>();
        statusEffects = new List<StatusEffect>();
        extraStatusEffects = new Etcetera(null, false);
        moves = new List<Move>();
        extraTurn = false;
        skydrop.sprite = Resources.Load<Sprite>("Backgrounds/" + PlayerPrefs.GetString("background", "bg"));
        spellTexts = Resources.Load<TextAsset>("spells");
        typeCounts = new Dictionary<int, int>{
            { 0, 0 },
            { 1, 0 },
            { 2, 0 },
            { 3, 0 },
            { 4, 0 },
            { 5, 0 }
        };

        matchingColors = new Dictionary<int, Color>();
        matchingColors.Add(0, Color.red);
        matchingColors.Add(1, Color.cyan);
        matchingColors.Add(2, Color.yellow);
        matchingColors.Add(3, Color.black);
        matchingColors.Add(4, Color.magenta);
        matchingColors.Add(5, Color.gray);

        foreach (Text i in FindObjectsOfType<Text>()) {

            switch (i.name) {
                case ("redMana"):
                    redManaTextBox = i;
                    break;
                case ("blueMana"):
                    blueManaTextBox = i;
                    break;
                case ("yellowMana"):
                    yellowManaTextBox = i;
                    break;
                case ("hp"):
                    hp = i;
                    health = PlayerPrefs.GetInt("hp", 50);
                    totalHealth = health;
                    hp.text = health.ToString() + " out of " + totalHealth.ToString();
                    break;
                case ("multiplier"):
                    multiplier = i;
                    break;
                case ("name"):
                    name = i;
                    break;
                default:
                    break;
            }

        }

        foreach (Image i in FindObjectsOfType<Image>()) {
            switch (i.name) {
                case ("healthBarFilled"):
                    healthBar = i.GetComponent<BarHandler>();
                    break;
                case ("background"):
                    background = i;
                    break;
                case ("enemyBackground"):
                    enemyBackground = i;
                    enemyBackground.color = new Color(enemyBackground.color.r, enemyBackground.color.g, enemyBackground.color.b, 0f);
                    break;
                case ("redBarFilled"):
                    redBar = i.GetComponent<BarHandler>();
                    maxRedMana = PlayerPrefs.GetInt("maxRedMana", 10);
                    break;
                case ("blueBarFilled"):
                    blueBar = i.GetComponent<BarHandler>();
                    maxBlueMana = PlayerPrefs.GetInt("maxBlueMana", 10);
                    break;
                case ("yellowBarFilled"):
                    yellowBar = i.GetComponent<BarHandler>();
                    maxYellowMana = PlayerPrefs.GetInt("maxYellowMana", 10);
                    break;
                case ("multiplierBarFilled"):
                    multiplierBar = i.GetComponent<BarHandler>();
                    break;
            }
        }
        redBar.setInitialPercentageFilled(0);
        blueBar.setInitialPercentageFilled(0);
        yellowBar.setInitialPercentageFilled(0);
        multiplierBar.setInitialPercentageFilled((damageMultiplier - 1) / (maxMultiplier - 1f));

        setRedMana(0);
        setBlueMana(0);
        setYellowMana(0);

        name.text = PlayerPrefs.GetString("name", "Rachel");

        targetingIndicator.SetActive(false);

        //Make board
        int yIndex = 0;
        for (int i = 0; i < BOARDLENGTH * BOARDLENGTH; i++) {

            //int randType = (int)UnityEngine.Random.Range(0.0f, 6.0f - 0.0001f);
            int randType = assignType(UnityEngine.Random.value);

            if (i % BOARDLENGTH == 0 && i != 0) { yIndex++; }

            //Initialization and Instatiation of new board piece
            GameObject newThing = Instantiate(Resources.Load<GameObject>("Thing"));
            grid.Add(new Thing(ref newThing, randType));
            newThing.transform.SetParent(GameObject.Find("Grid").transform);
            Transform parent = newThing.GetComponentInParent<ThingController>().gameObject.transform;
            newThing.transform.localPosition = new Vector3((i - yIndex * BOARDLENGTH), (yIndex), 1);
            newThing.GetComponent<ThingController>().assignPiece(randType);

        }

        //checkForMatches();

        //Getting and assigning spells
        equippedSpells = new List<Button>();
        SpellContainer spells = SpellContainer.Load(Path.Combine(Application.persistentDataPath, "spells.xml"));
        equippedSpells = new List<Button>(new Button[5]);
        //Add spells from spells in text form (compatibility between new an old systems)
        List<Spell> spellsList = new List<Spell>(spells.spells);
        string[] spellsFromText = spellTexts.text.Split('\n');
        foreach (string spellText in spellsFromText){
            spellsList.Add(new Spell(spellText) );
        }

        for (int i = 0; i < spellsList.Count; i++) {
            //Debug.Log(PlayerPrefs.GetInt(spells.spells[i].Name, 0));
            int spellStatus = PlayerPrefs.GetInt(spellsList[i].Name, 0);
            if (spellStatus > 0 && spellStatus < 6) { //1 -5 for order of spells, 6 for available but not equipped
                //equippedSpells.Add(Instantiate(Resources.Load<Button>("SpellButton"), GameObject.Find("paperback").transform));
                equippedSpells[spellStatus - 1] = (Instantiate(Resources.Load<Button>("SpellButton"), GameObject.Find("paperback").transform));
                equippedSpells[spellStatus - 1].GetComponent<SpellButtonHandler>().setSpell(spellsList[i]);

                //equippedSpells[spellStatus - 1].transform.localScale = new Vector3(0.02f, 0.02f);
                //equippedSpells[spellStatus - 1].transform.localPosition = new Vector3(0, -(spellStatus - 1), 0);

                equippedSpells[spellStatus - 1].gameObject.tag = "player";
            }
        }

        //Removes all the empty buttons
        equippedSpells.RemoveAll((b) => {
            if (b) { return false; }
            return true;
        });

        //Sets the sibling order correctly so the tooltips display over one another
        for (int i = 0; i < equippedSpells.Count; i++) {
            equippedSpells[i].transform.SetAsLastSibling();
            equippedSpells[i].transform.localPosition = new Vector3(0, -i);
        }

        //Get weapon
        weapon = new Weapon();
        WeaponContainer weapons = WeaponContainer.Load(Path.Combine(Application.persistentDataPath, "weapons.xml"));
        foreach (Weapon i in weapons.Weapons) {
            if (PlayerPrefs.GetInt(i.Name, 0) == 2) { //2 for equipped, 1 for own but not equipped, 0 for not owned
                weapon = i;
                break;
            }
        }
        if (weapon.Type != WeaponType.none) {
            weaponObject.SetActive(true);
            weaponObject.GetComponent<WeaponButtonHandler>().setWeapon(weapon);
        }
        else {
            weaponObject.GetComponent<Image>().sprite = Resources.Load<Sprite>("Button");
            weaponObject.SetActive(false);
        }

        music.setTrack("Early in the Morning");
        music.play();

        gameObject.GetComponent<tutorialHandler>().enabled = true;
    }

    //Converts numbers from 0-1 into piece types
    public int assignType(float randomValue) {
        float runningProbability = 0;
        for (int i = 0; i < probabilities.Length; i++) {
            runningProbability += probabilities[i];
            if (randomValue < runningProbability) {
                return i;
            }
        }
        return 5;
    }

    public void hide() {
        foreach (Thing i in grid) {
            i.getObject().SetActive(false);
        }
        //paperback.SetActive(false);
        //enemyPaperback.SetActive(false);
        //backdrop.SetActive(false);
    }

    public void show() {
        foreach (Thing i in grid) {
            i.getObject().SetActive(true);
        }
        //paperback.SetActive(true);
        //enemyPaperback.SetActive(true);
        //backdrop.SetActive(true);
    }

    public bool isSpellsEquipped() {
        return !(equippedSpells.Count == 0);
    }

    public void resetPlayer() {

        maxRedMana = PlayerPrefs.GetInt("maxRedMana", 10);
        maxBlueMana = PlayerPrefs.GetInt("maxBlueMana", 10);
        maxYellowMana = PlayerPrefs.GetInt("maxYellowMana", 10);
        totalHealth = PlayerPrefs.GetInt("hp", 100);
        name.text = PlayerPrefs.GetString("name", "Rachel");

        health = totalHealth;
        redMana = 0;
        blueMana = 0;
        yellowMana = 0;
        damageMultiplier = 1;

        multiplier.text = damageMultiplier.ToString() + "x";
        redManaTextBox.text = redMana.ToString();
        blueManaTextBox.text = blueMana.ToString();
        yellowManaTextBox.text = yellowMana.ToString();
        hp.text = health.ToString() + " out of " + totalHealth.ToString();

        healthBar.setInitialPercentageFilled(health / (totalHealth + 0f));
        redBar.setInitialPercentageFilled(redMana / (maxRedMana + 0f));
        blueBar.setInitialPercentageFilled(blueMana / (maxBlueMana + 0f));
        yellowBar.setInitialPercentageFilled(yellowMana / (maxYellowMana + 0f));
        multiplierBar.setInitialPercentageFilled((damageMultiplier - 1) / (maxMultiplier - 1f));

        foreach (StatusEffect i in statusEffects) {
            Destroy(i.indicator);
        }
        statusEffects.Clear();
        if (extraStatusEffects.getIndicator() != null) {
            Destroy(extraStatusEffects.getIndicator());
            extraStatusEffects.clearIndicator();
            extraStatusEffects.clear();
        }

        foreach (Button i in equippedSpells) {
            Destroy(i.gameObject);
        }
        equippedSpells.Clear();

        //Getting and assigning spells
        equippedSpells = new List<Button>();
        SpellContainer spells = SpellContainer.Load(Path.Combine(Application.persistentDataPath, "spells.xml"));
        equippedSpells = new List<Button>(new Button[5]);

        for (int i = 0; i < spells.spells.Length; i++) {
            //Debug.Log(PlayerPrefs.GetInt(spells.spells[i].Name, 0));
            int spellStatus = PlayerPrefs.GetInt(spells.spells[i].Name, 0);
            if (spellStatus > 0 && spellStatus < 6) { //1 -5 for order of spells, 6 for available but not equipped
                //equippedSpells.Add(Instantiate(Resources.Load<Button>("SpellButton"), GameObject.Find("paperback").transform));
                equippedSpells[spellStatus - 1] = (Instantiate(Resources.Load<Button>("SpellButton"), GameObject.Find("paperback").transform));
                equippedSpells[spellStatus - 1].GetComponent<SpellButtonHandler>().setSpell(spells.spells[i]);

                //equippedSpells[spellStatus - 1].transform.localScale = new Vector3(0.02f, 0.02f);
                //equippedSpells[spellStatus - 1].transform.localPosition = new Vector3(0, -(spellStatus - 1), 0);

                equippedSpells[spellStatus - 1].gameObject.tag = "player";
            }
        }

        //Removes all the empty buttons
        equippedSpells.RemoveAll((b) => {
            if (b) { return false; }
            return true;
        });

        //Sets the sibling order correctly so the tooltips display over one another
        for (int i = 0; i < equippedSpells.Count; i++) {
            equippedSpells[i].transform.SetAsLastSibling();
            equippedSpells[i].transform.localPosition = new Vector3(0, -i);
        }

        //Get weapon
        weapon = new Weapon();
        WeaponContainer weapons = WeaponContainer.Load(Path.Combine(Application.persistentDataPath, "weapons.xml"));
        foreach (Weapon i in weapons.Weapons) {
            if (PlayerPrefs.GetInt(i.Name, 0) == 2) { //2 for equipped, 1 for own but not equipped, 0 for not owned
                weapon = i;
                break;
            }
        }
        if (weapon.Type != WeaponType.none) {
            weaponObject.SetActive(true);
            weaponObject.GetComponent<WeaponButtonHandler>().setWeapon(weapon);
        }
        else {
            weaponObject.GetComponent<Image>().sprite = Resources.Load<Sprite>("Button");
            weaponObject.SetActive(false);
        }

    }

    public void inBetweenRoundEffects() {
        //Current feature is player gets 50% health back, continues fight with previous traits
        if (health > 0.5 * totalHealth) {
            health = totalHealth;
        }
        else {
            health += (int)(0.5 * totalHealth);
        }
        healthBar.setPercentageFilled(health / (totalHealth + 0f));
        hp.text = health.ToString() + " out of " + totalHealth.ToString();
        madeMove = false;
        isTurn = true;
        turnOnSpells();
    }

    public void loadCompanion(string companionName) {
        Companion companion = DialogueController.GetCompanion(companionName);
        totalHealth = companion.hp;
        maxRedMana = companion.maxRedMana;
        maxBlueMana = companion.maxBlueMana;
        maxYellowMana = companion.maxYellowMana;
        name.text = char.ToUpper(companion.name[0]) + companion.name.Substring(1);

        //Debug.Log(companion.spells[0] + " " +companion.spells[1]);

        portrait.sprite = Resources.Load<Sprite>("characters/" + companion.name + "Normal");

        health = totalHealth;
        setRedMana(0);
        setBlueMana(0);
        setYellowMana(0);
        healthBar.setInitialPercentageFilled(1);
        hp.text = companion.hp.ToString() + " out of " + companion.hp.ToString();
        damageMultiplier = 1;
        multiplier.text = damageMultiplier.ToString() + "x";
        multiplierBar.setInitialPercentageFilled(0);

        SpellContainer spells = SpellContainer.Load(Path.Combine(Application.persistentDataPath, "spells.xml"));
        foreach (Button i in equippedSpells) {
            Destroy(i.gameObject);
        }
        equippedSpells.Clear();

        for (int i = 0; i < companion.spells.Length; i++) {
            //equippedSpells[i].transform.SetParent(GameObject.Find("Canvas").transform);
            equippedSpells.Add(Instantiate(Resources.Load<Button>("SpellButton"), GameObject.Find("paperback").transform));
            equippedSpells[equippedSpells.Count - 1].GetComponent<SpellButtonHandler>().setSpell(spells.spells[companion.spells[i]]);
            //equippedSpells[i].transform.localScale = new Vector3(0.02f, 0.02f);
            equippedSpells[i].transform.localPosition = new Vector3(0, -i, 0);
            equippedSpells[i].gameObject.tag = "player";
            equippedSpells[i].interactable = false;
        }

        foreach (StatusEffect i in statusEffects) {
            Destroy(i.indicator);
        }
        statusEffects.Clear();

        if (extraStatusEffects.getIndicator() != null) {
            Destroy(extraStatusEffects.getIndicator());
            extraStatusEffects.clearIndicator();
            extraStatusEffects.clear();
        }

        weapon = new Weapon();
        WeaponContainer weapons = WeaponContainer.Load(Path.Combine(Application.persistentDataPath, "weapons.xml"));
        if (companion.weapon != -1) {
            weapon = weapons.Weapons[companion.weapon];
        }

        if (weapon.Type != WeaponType.none) {
            weaponObject.SetActive(true);
            weaponObject.GetComponent<WeaponButtonHandler>().setWeapon(weapon);
        }
        else {
            weaponObject.GetComponent<Image>().sprite = Resources.Load<Sprite>("Button");
            weaponObject.SetActive(false);
        }

        turnOnSpells();
    }

    public bool checkCosts(int[] costs) {
        if (redMana < costs[0]) {
            return false;
        }
        else if (blueMana < costs[1]) {
            return false;
        }
        else if (yellowMana < costs[2]) {
            return false;
        }
        return true;
    }

    public List<StatusEffect> GetStatusEffects() {
        return statusEffects;
    }

    public bool getExtraTurn() {
        return extraTurn;
    }

    public int getHealth() {
        return health;
    }

    //All take damage goes through here
    public int applyModifiersToDamageDone(int amount, DamageType damageType) {
        EnemyController enemyController = gameObject.GetComponent<EnemyController>();

        if (damageType == DamageType.spellDamage) {
            //Apply weapon effects
            amount = enemyController.weapon.spellDamageEffect() + amount;
            //May have defensive weapons later

            //Apply status effects
            //Enemy effects
            foreach (StatusEffect i in enemyController.GetStatusEffects()) {
                if (i is DamageIncreaseEffect) {
                    amount = ((DamageIncreaseEffect)i).modifyDamage(amount);
                }
                else if (i is SpellDamageIncreaseEffect) {
                    amount = ((SpellDamageIncreaseEffect)i).modifyDamage(amount);
                }
            }
            //Player effects
            foreach (StatusEffect i in statusEffects) {
                if (i is DamageReductionEffect) {
                    amount = ((DamageReductionEffect)i).modifyDamage(amount);
                }
              
            }
            return (int)(amount * enemyController.damageMultiplier);
        }
        else if (damageType == DamageType.matchDamage) {
            //May apply nonlinear formula to damage
            //Weapon effects
            amount = (int)enemyController.weapon.manaEffect(3, 2 * amount);
            //May have defensive weapons later

            //Apply status effects
            //Enemy effects
            foreach (StatusEffect i in enemyController.GetStatusEffects()) {
                if (i is DamageIncreaseEffect) {
                    amount = ((DamageIncreaseEffect)i).modifyDamage(amount);
                }
            }
            //Player effects
            foreach (StatusEffect i in statusEffects) {
                if (i is DamageReductionEffect) {
                    amount = ((DamageReductionEffect)i).modifyDamage(amount);
                }
            }
            return (int)(amount * enemyController.damageMultiplier);
        }
        else if (damageType == DamageType.statusEffectDamage) {
            //Weapon effects

            //May have defensive weapons later

            //Apply status effects (Will have more later)
            //Enemy effects
            //Player effects
            return amount;
        }
        return amount;
    }

    public void dealDamage(int damage) {
        if (health - damage <= totalHealth) {
            health -= damage;
            if (health <= 0) {
                health = 0;
                hp.text = health.ToString() + " out of " + totalHealth.ToString();
                healthBar.setPercentageFilled(health / (totalHealth + 0f));

                //lose
                Debug.Log("You lose");

                resultsScreenHandler.setLoss();
                ScriptController.pauseGame();
            }
        }
        else {
            health = totalHealth;
        }
        hp.text = health.ToString() + " out of " + totalHealth.ToString();
        healthBar.setPercentageFilled(health / (totalHealth + 0f));
    }

    public GameObject getPortrait() {
        return GameObject.Find("portraitImage");
    }

    public int processStatusDamage(int preProcessedDamage) {
        //Do status effect damage processing
        foreach (StatusEffect i in statusEffects) {
            if (i is DamageReductionEffect) {
                preProcessedDamage = ((DamageReductionEffect)i).modifyDamage(preProcessedDamage);
            }
        }
        return preProcessedDamage;
    }

    public void setMana(int id, int mana) {
        switch (id) {
            case (0):
                setRedMana(mana);
                break;
            case (1):
                setBlueMana(mana);
                break;
            case (2):
                setYellowMana(mana);
                break;
        }
    }

    public int getMana(int id) {
        switch (id) {
            case (0):
                return getRedMana();
            case (1):
                return getBlueMana();
            case (2):
                return getYellowMana();
        }
        return 0;
    }

    public void setYellowMana(int mana) {
        mana = mana > maxYellowMana ? maxYellowMana : mana;
        yellowMana = mana >= 0 ? mana : 0;
        yellowBar.setPercentageFilled(yellowMana / (maxYellowMana + 0f));
        yellowManaTextBox.text = yellowMana.ToString() + "/" + maxYellowMana.ToString();
    }
    public void setBlueMana(int mana) {
        mana = mana > maxBlueMana ? maxBlueMana : mana;
        blueMana = mana >= 0 ? mana : 0;
        blueBar.setPercentageFilled(blueMana / (maxBlueMana + 0f));
        blueManaTextBox.text = blueMana.ToString() + "/" + maxBlueMana.ToString();
    }
    public void setRedMana(int mana) {
        if (mana > maxRedMana) {
            redMana = maxRedMana;
        }
        else if (mana < 0) {
            redMana = 0;
        }
        else {
            redMana = mana;
        }
        redBar.setPercentageFilled(redMana / (maxRedMana + 0f));
        redManaTextBox.text = redMana.ToString() + "/" + maxRedMana.ToString();
    }

    public void setMultiplier(float amount) {
        amount = amount > maxMultiplier ? maxMultiplier : amount;
        if (amount >= 1) {
            damageMultiplier = amount;
            multiplier.text = damageMultiplier.ToString() + "x";
            multiplierBar.setPercentageFilled((damageMultiplier - 1) / (maxMultiplier - 1f));
        }
    }
    public int getRedMana() {
        return redMana;
    }
    public int getBlueMana() {
        return blueMana;
    }
    public int getYellowMana() {
        return yellowMana;
    }

    //A player casts a spell assuming has ability to pay
    public void castSpell(Spell spell, PlayerController caster, PlayerController target) {
        //Pay costs
        if (isTurn) {
            //Do cast effect if player is casting
            weapon.castEffect(true);
        }
        caster.setMana((int)colorType.red, caster.getMana((int)colorType.red) - spell.Costs[(int)colorType.red]);
        caster.setMana((int)colorType.blue, caster.getMana((int)colorType.blue) - spell.Costs[(int)colorType.blue]);
        caster.setMana((int)colorType.yellow, caster.getMana((int)colorType.yellow) - spell.Costs[(int)colorType.yellow]);
        makeSplashText("Casting " + spell.Name, isTurn);

        List<string> spellParameters = new List<string>(spell.Parameters.Split( '\n', '+' ));

        foreach (string parameter in spellParameters) {
            List<string> actionAndParameters = new List<string>(parameter.Split(' '));
            switch (actionAndParameters[0]) {
                case "dealDamage":
                    //Spells with this parameter deal parameter 1 of damage to the enemy
                    target.dealDamage(target.applyModifiersToDamageDone(int.Parse(actionAndParameters[1]), DamageType.spellDamage));
                    GameObject flash = Instantiate(Resources.Load<GameObject>("spellFlash"), target.getPortrait().transform);
                    flash.GetComponent<SpellFlashHandler>().sound.clip = Resources.Load<AudioClip>("Fire Ignition");
                    flash.GetComponent<SpellFlashHandler>().sound.Play();
                    break;
                case "dealDamageEqualTo":
                    //Spells with this parameter deal damage as an output of function parameter 1
                    Type thisType = this.GetType();
                    MethodInfo getDamage = thisType.GetMethod(actionAndParameters[1]);
                    int damage = (int)getDamage.Invoke(this, null);
                    target.dealDamage(target.applyModifiersToDamageDone(damage, DamageType.spellDamage));
                    flash = Instantiate(Resources.Load<GameObject>("spellFlash"), target.getPortrait().transform);
                    flash.GetComponent<SpellFlashHandler>().sound.clip = Resources.Load<AudioClip>("Fire Ignition");
                    flash.GetComponent<SpellFlashHandler>().sound.Play();
                    break;
                case "setMana":
                    //Spells with this parameter set the mana of type parameter 1 to amount parameter 2
                    caster.setMana(int.Parse(actionAndParameters[1]), int.Parse(actionAndParameters[2]));
                    break;
                case "setMultiplier":
                    //Spells with this parameter set the multiplier to parameter 1
                    caster.setMultiplier(float.Parse(actionAndParameters[1]));
                    break;
                case "heal":
                    //Spells with this parameter heal self for parameter 1
                    caster.dealDamage(applyModifiersToDamageDone(-int.Parse(actionAndParameters[1]), DamageType.spellDamage));
                    break;
                case "destroySetLine":
                    //Spells with this parameter score all lines between paramter 1 and parameter 2
                    for (int i = int.Parse(actionAndParameters[1]) * BOARDLENGTH; i < int.Parse(actionAndParameters[2]) * BOARDLENGTH; i++) {
                        //score(grid[i].getType());
                        toDelete.Add(i);
                    }
                    break;
                case "destroySquare":
                    //Spells with this parameter score a square of size parameter 1
                    beginCasting();
                    selectedPiece = null;
                    currentSpell = spell;
                    this.actionAndParameters = parameter;
                    turnOffSpells();
                    break;
                case ("shiftTarget"):
                    //Spells with this parameter shift a target piece to type parameter 1
                    beginCasting();
                    selectedPiece = null;
                    currentSpell = spell;
                    this.actionAndParameters = parameter;
                    turnOffSpells();
                    break;
                case "randomShift":
                    //Spells with this parameter change parameter 1 random pieces into type parameter 2 (if parameter 2 is -1, score them, and if parameter 2 is 7, the piece is random)
                    List<int> randomNumbers = new List<int>();
                    while (randomNumbers.Count < int.Parse(actionAndParameters[1])) {
                        int randomNumber = UnityEngine.Random.Range(0, grid.Count);
                        if (!randomNumbers.Contains(randomNumber)) {
                            randomNumbers.Add(randomNumber);
                        }
                    }
                    foreach (int i in randomNumbers) {
                        if (actionAndParameters[2] == "2") {
                            toDelete.Add(i);
                        }
                        else if (actionAndParameters[2] == "7") {
                            int randType = UnityEngine.Random.Range(0, 5);
                            grid[i].setType(randType);
                            grid[i].setIsSuperPiece(false);
                            grid[i].getObject().GetComponent<ThingController>().assignPiece(randType);
                        }
                        else {
                            int newType = int.Parse(actionAndParameters[2]);
                            grid[i].setType(newType);
                            grid[i].setIsSuperPiece(false);
                            grid[i].getObject().GetComponent<ThingController>().assignPiece(newType);
                        }
                        flash = Instantiate(Resources.Load<GameObject>("spellFlash"), grid[i].getObject().transform);
                        flash.transform.localScale.Set(0.5f, 0.5f, 0.5f);
                    }
                    break;
                case "grantExtraTurn":
                    //Spells with this parameter guarantee an extra move
                    extraTurn = true;
                    break;
                case "destroyAllPiecesOfType":
                    //Spells with this parameter destroy all pieces with type parameter 1
                    for (int i = 0; i < BOARDLENGTH * BOARDLENGTH; i++) {
                        if (grid[i].getType() == int.Parse(actionAndParameters[1])) {
                            toDestroy.Add(i);
                        }
                    }
                    break;
                case "grantStatusEffect":
                    //Spells with this parameter add a status effect of name parameter 1 lasting parameter 3 turns with potency parameter 2.  They also create a visual effect on the player of parameter 4
                    GameObject indicator = Instantiate(Resources.Load<GameObject>("StatusEffectIndicator"));
                    Type statusEffectType = Type.GetType(actionAndParameters[1]);
                    StatusEffect status = (StatusEffect)Activator.CreateInstance(statusEffectType, indicator, isTurn, int.Parse(actionAndParameters[3]), int.Parse(actionAndParameters[2]) );
                    status.updateIndicator();
                    string effect = "buffSpellFlash";
                    if (actionAndParameters.Count > 4) { effect = actionAndParameters[4]; }
                    Instantiate(Resources.Load<GameObject>(effect), caster.getPortrait().transform);
                    caster.addStatusEffect(status);
                    break;
                case "inflictStatusEffect":
                    //Spells with this parameter add a status effect of name parameter 1 lasting parameter 3 turns with intensity parameter 2.  They also create a visual effect on the player of parameter 4
                    indicator = Instantiate(Resources.Load<GameObject>("StatusEffectIndicator"));
                    statusEffectType = Type.GetType(actionAndParameters[1]);
                    status = (StatusEffect)Activator.CreateInstance(statusEffectType, indicator, !isTurn, int.Parse(actionAndParameters[3]), int.Parse(actionAndParameters[2]));
                    status.updateIndicator();
                    effect = "spellFlash";
                    if (actionAndParameters.Count > 4) { effect = actionAndParameters[4]; }
                    Instantiate(Resources.Load<GameObject>(effect), target.getPortrait().transform);
                    target.addStatusEffect(status);
                    break;
                case "addMana":
                    //Spells with this parameter add mana of type parameter 1 of amount parameter 2
                    caster.setMana(int.Parse(actionAndParameters[1]), int.Parse(actionAndParameters[2]) + caster.getMana(int.Parse(actionAndParameters[1])));
                    Instantiate(Resources.Load<GameObject>("buffSpellFlash"), GameObject.Find("portraitImage").transform);
                    break;
                case "endTurn":
                    //Spells with this parameter end the turn upon use
                    gameObject.GetComponent<EnemyController>().delayTime = 1.5f;
                    madeMove = true;
                    turnOffSpells();
                    break;
                case "stealMana":
                    //Spells with this parameter steal mana of type parameter 1 in amount parameter 2
                    if (actionAndParameters[1] == "all") {
                        int redToSteal = target.getMana((int)colorType.red) >= int.Parse(actionAndParameters[2]) ? int.Parse(actionAndParameters[2]) : target.getMana((int)colorType.red);
                        int blueToSteal = target.getMana((int)colorType.blue) >= int.Parse(actionAndParameters[2]) ? int.Parse(actionAndParameters[2]) : target.getMana((int)colorType.blue);
                        int yellowToSteal = target.getMana((int)colorType.yellow) >= int.Parse(actionAndParameters[2]) ? int.Parse(actionAndParameters[2]) : target.getMana((int)colorType.yellow);
                        //Debug.Log(redToSteal+ " " +blueToSteal+ " "+ yellowToSteal);

                        target.setMana((int)colorType.red, target.getMana((int)colorType.red) - redToSteal);
                        target.setMana((int)colorType.blue, target.getMana((int)colorType.blue) - blueToSteal);
                        target.setMana((int)colorType.yellow, target.getMana((int)colorType.yellow) - yellowToSteal);

                        //Debug.Log(redToSteal + redMana + " " + blueToSteal + blueMana + " " + yellowToSteal +yellowMana);
                        caster.setMana((int)colorType.red, redToSteal + caster.getMana((int)colorType.red));
                        caster.setMana((int)colorType.blue, blueToSteal + caster.getMana((int)colorType.blue));
                        caster.setMana((int)colorType.yellow, yellowToSteal + caster.getMana((int)colorType.yellow));

                    }
                    break;
                case "shiftColor":
                    //Spells with this parameter shift the color of all pieces of type parameter 1 into pieces of type parameter 2
                    for (int i = 0; i < BOARDLENGTH * BOARDLENGTH; i++) {
                        if (grid[i].getType() == int.Parse(actionAndParameters[1])) {
                            if (int.Parse(actionAndParameters[2]) == -1) {
                                toDelete.Add(i);
                            }
                            else {
                                grid[i].setType(int.Parse(actionAndParameters[2]));
                                grid[i].setIsSuperPiece(false);
                                grid[i].getObject().GetComponent<ThingController>().assignPiece(int.Parse(actionAndParameters[2]));
                            }
                        }
                    }
                    break;
                case "randomDestruction":
                    //Spells with this parameter give all pieces a probabilaty of being destroyed equal to parameter 1 divided by 10
                    //Debug.Log("casting random destruction");
                    float probability = float.Parse(actionAndParameters[1]) / 10;
                    Debug.Log(probability);
                    for (int i = 0; i < BOARDLENGTH * BOARDLENGTH; i++){
                        if (UnityEngine.Random.Range(0f, 1f) < probability) { 
                            toDestroy.Add(i);
                        }
                    }
                    break;
                default:
                    Debug.Log("Hmm, bad parameter");
                    break;
            }
        }
    }
    private int countHeartPiecesTimes2() {
        int sum = 0;
        foreach (Thing i in grid) {
            if (i.getType() == (int)type.health) {
                sum++;
            }
        }
        return 2 * sum;
    }
    private int tenthOfHealth() {
        return health % 10;
    }
    private int countHeartPieces() {
        int sum = 0;
        foreach (Thing i in grid) {
            if (i.getType() == (int)type.health) {
                sum++;
            }
        }
        return sum;
    }
    private int countRedManaPieces() {
        int sum = 0;
        foreach (Thing i in grid) {
            if (i.getType() == (int)type.red) {
                sum++;
            }
        }
        return sum;
    }
    private int countBlueManaPieces() {
        int sum = 0;
        foreach (Thing i in grid) {
            if (i.getType() == (int)type.blue) {
                sum++;
            }
        }
        return sum;
    }
    private int countYellowManaPieces() {
        int sum = 0;
        foreach (Thing i in grid) {
            if (i.getType() == (int)type.yellow) {
                sum++;
            }
        }
        return sum;
    }
    private int countDamagePieces() {
        int sum = 0;
        foreach (Thing i in grid) {
            if (i.getType() == (int)type.damage) {
                sum++;
            }
        }
        return sum;
    }
    private int countMultiplierPieces() {
        int sum = 0;
        foreach (Thing i in grid) {
            if (i.getType() == (int)type.multiplier) {
                sum++;
            }
        }
        return sum;
    }

    private void beginCasting(string actionAndParameters="") {
        this.actionAndParameters = actionAndParameters;
        isCasting = true;
        targetingIndicator.SetActive(true);
    }

    public void addStatusEffect(StatusEffect effect) {
        statusEffects.Add(effect);
        statusEffects.Sort((a,b) => a.getTurns().CompareTo(b.getTurns()));
        displayStatusEffects();
    }

    //Displays status effect for enemy?
    private void displayStatusEffects() {
        if (extraStatusEffects.getIndicator()) {
            Destroy(extraStatusEffects.getIndicator());
            extraStatusEffects.clearIndicator();
        }
        int numberOfStatusEffectsToDisplay = statusEffects.Count;
        //Display first 3
        if (statusEffects.Count > 3) {
            numberOfStatusEffectsToDisplay = 3;
        }
        for (int i = 0; i < numberOfStatusEffectsToDisplay; i++) {
            statusEffects[i].getIndicator().transform.SetParent(GameObject.Find("portrait").transform, false);
            statusEffects[i].getIndicator().transform.localPosition = new Vector3(-90, 45f - i * 30f);
            
        }
        //Put the rest in etcetera
        if (statusEffects.Count > 3) {
            extraStatusEffects = new Etcetera(Instantiate(Resources.Load<GameObject>("StatusEffectIndicator")), false);
            extraStatusEffects.getIndicator().GetComponent<Image>().sprite = Resources.Load<Sprite>("dotdotdot");

            extraStatusEffects.getIndicator().transform.SetParent((GameObject.Find("portrait").transform));
            extraStatusEffects.getIndicator().transform.localPosition = new Vector3(-90, -45f);
            
            for (int i = 3; i < statusEffects.Count; i++) {
                extraStatusEffects.enqueue(statusEffects[i]);
            }
            extraStatusEffects.updateIndicator();
        }
    }


    public void actionBasedOnPiece(ThingController thingController) {
        if (currentSpell != null) {
            List<string> parameters = new List<string>(actionAndParameters.Split(' '));
            actionAndParameters = "";
            targetingIndicator.SetActive(false);
            turnOnSpells();
            
            switch (parameters[0]) {
                case ("destroyLines"):
                    //Destroy selected lines
                    /*
                    if (currentSpell.Effects[0] == 0) {
                        for (int i = getIndex * BOARDLENGTH; i < currentSpell.Effects[2] * BOARDLENGTH; i++) {
                            //score(grid[i].getType());
                            toDelete.Add(i);
                        }
                        currentSpell = null;
                    }
                    */
                    break;
                case ("destroySquare"):
                    int index = getIndex(thingController.gameObject);
                    List<int> positions = new List<int>();

                    if (currentSpell.Effects[0] == 0) {
                        for (int i = -int.Parse(parameters[1]); i <= int.Parse(parameters[1]); i++) {
                            if (index + i * BOARDLENGTH < 0 || index + i * BOARDLENGTH > BOARDLENGTH * BOARDLENGTH) {
                                continue;
                            }
                            for (int j = -int.Parse(parameters[1]); j <= int.Parse(parameters[1]); j++) {
                                if (index + i * BOARDLENGTH + j >= 0 && index + i * BOARDLENGTH + j < BOARDLENGTH * BOARDLENGTH) {
                                    if (index + i * BOARDLENGTH + j >= ((index + i * BOARDLENGTH) / BOARDLENGTH) * BOARDLENGTH && index + i * BOARDLENGTH + j < ((index + (i + 1) * BOARDLENGTH) / BOARDLENGTH) * BOARDLENGTH) {
                                        positions.Add(index + i * BOARDLENGTH + j);
                                        //Debug.Log(index + i * BOARDLENGTH + j);
                                    }
                                }
                                if (index + j % 8 == 7) {
                                    //Debug.Log("Reached edge");
                                    break;
                                }
                            }
                        }

                        //Pass turn
                        madeMove = true;

                        currentSpell = null;
                        selectedPiece = null;

                        toDelete.UnionWith(positions);
                    }
                    break;
                case ("shiftTarget"):
                    index = getIndex(thingController.gameObject);
                    grid[index].setType(int.Parse(parameters[1]));
                    thingController.assignPiece(int.Parse(parameters[1]));

                    GameObject destroyEffect = Instantiate(Resources.Load<GameObject>("spellFlash"));
                    destroyEffect.transform.position = grid[index].getObject().transform.position;
                    destroyEffect.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    destroyEffect.GetComponent<SpellFlashHandler>().speed = 2;

                    //Pass turn
                    madeMove = true;
                    currentSpell = null;
                    selectedPiece = null;
                    break;
            }
        }
        isCasting = false;
    }

    public void turnOnSpells() {
        foreach( StatusEffect i in statusEffects) {
            if (i is Seal) {
                return;
            }
        }
        foreach (Button i in equippedSpells) {
            i.interactable = true;
        }
    }
    public void turnOffSpells() {
        foreach (Button i in equippedSpells) {
            i.interactable = false;
        }
    }

    public int getIndex(GameObject piece) {
        for(int i = 0; i < grid.Count; i++) {
            try {
                if (((Thing)grid[i]).getObject() == piece) {
                    return i;
                }
            }
            catch (Exception e) {
                Debug.Log(e.Message);
            }
        }
        return -1;
    }

    public Thing GetThing(int index) {
        return grid[index];
    }

    public void swap(int index1, int index2) {
        Thing other = GetThing(index1);
        Thing me = GetThing(index2);

        grid[index1].getObject().GetComponent<ThingController>().setMoving(true);
        grid[index2].getObject().GetComponent<ThingController>().setMoving(true);

        //Animation
        Vector3 tempTarget = me.getObject().GetComponent<ThingController>().getTarget();
        me.getObject().GetComponent<ThingController>().setTarget(other.getObject().GetComponent<ThingController>().getTarget());
        other.getObject().GetComponent<ThingController>().setTarget(tempTarget);

        swapHelper(ref me, ref other);


        //checkForMatches();
    }

    public void swapHelper(ref Thing one, ref Thing two) {
        Thing temp = new Thing(ref one.getObject(), one.getType());
        temp.setIsSuperPiece(one.getIsSuperPiece());
        //one = two;
        //two = one;
        one.setObject(ref two.getObject());
        one.setType(two.getType());
        one.setIsSuperPiece(two.getIsSuperPiece());
        two.setObject(ref temp.getObject());
        two.setType(temp.getType());
        two.setIsSuperPiece(temp.getIsSuperPiece());
    }

    public void checkForMatches() {

        //Find Matches
        //toDelete = new SortedSet<int>();
        for (int i = 0; i < BOARDLENGTH * BOARDLENGTH; i++) {
            checkForMatchesHelper(i, GetThing(i).getType());
        }
        //Check for 4+ in a rows (and checks if isn't first turn)
        if ((madeMove || EnemyController.madeMove) && !extraTurn && areFourOrMoreInARows()) {
            Debug.Log("4+ in a row");
            extraTurn = true;
            jingle.Play();

        }

        //Do superPiece handling
        handleSuperPieces(new List<int>());

        typeCounts = new Dictionary<int, int>{
            { 0, 0 },
            { 1, 0 },
            { 2, 0 },
            { 3, 0 },
            { 4, 0 },
            { 5, 0 }
        };
        //Calculate gained amounts
        if (madeMove || EnemyController.madeMove) {
            foreach (int i in toDelete) {
                int type = grid[i].getType();
                if (type == 3 && grid[i].getIsSuperPiece()) { typeCounts[type] += 2; } //Extra 2 points for super sword
                typeCounts[type]++;
            }
        }
        scoreByAmount(typeCounts);
        
        //Destroy Matches
        foreach (int i in toDelete) {

            //Make explosion for superpieces
            if (grid[i].getIsSuperPiece()) {
                GameObject sound = Instantiate(Resources.Load<GameObject>("sound effect"));
                sound.GetComponent<SoundEffectHandler>().play("explosion");
            }

            //Make particle trail
            generateParticles(i, grid[i].getType());

            //Make a smoke cloud
            /*
            GameObject smokePuff = Instantiate(Resources.Load<GameObject>("SmokePuff"));
            smokePuff.transform.position = grid[i].getObject().transform.position;
            smokePuff.transform.Translate(new Vector3(0, 0, -2));
            ParticleSystem.MainModule puffGenerator = smokePuff.GetComponent<ParticleSystem>().main;
            puffGenerator.startColor = matchingColors[grid[i].getType()];
            */

            // Leave behind a colored square to show what piece type was destroyed
            GameObject matchIdentifier = Instantiate(Resources.Load<GameObject>("MatchIdentifier"));
            matchIdentifier.GetComponent<Image>().color = matchingColors[grid[i].getType()];
            matchIdentifier.transform.SetParent(gameObject.transform);
            matchIdentifier.transform.position = grid[i].getObject().transform.position;

            grid[i].setType(-1);
            grid[i].setIsSuperPiece(false);
            grid[i].getObject().GetComponent<ThingController>().light.gameObject.SetActive(false);
            grid[i].getObject().GetComponent<Image>().color = Color.white;
            grid[i].getObject().GetComponent<ThingController>().assignPiece(-1);
            
        }

        //Check castability of spells
        if (isTurn) {
            foreach (Button i in equippedSpells) {
                bool canCast = checkCosts(i.GetComponent<SpellButtonHandler>().getSpell().Costs);
                if (i.GetComponent<Image>().sprite.name == "Button" && canCast) {
                    i.GetComponent<Image>().sprite = Resources.Load<Sprite>("castableButton");
                }
                else if (i.GetComponent<Image>().sprite.name == "castableButton" && !canCast) {
                    i.GetComponent<Image>().sprite = Resources.Load<Sprite>("Button");
                }
            }
        }
        else {
            gameObject.GetComponent<EnemyController>().checkAllCosts();
        }

        //Check if there are still moves, otherwise refresh board
        if (toDelete.Count == 0 && toDestroy.Count == 0) {
            List<Move> moves = new List<Move>();
            for (int i = 0; i < BOARDLENGTH * BOARDLENGTH; i++) {
                moves.AddRange(movesAtPoint(i));
            }
            if (moves.Count == 0) {
                for (int i = 0; i < BOARDLENGTH * BOARDLENGTH; i++) {
                    toDestroy.Add(i);
                }
            }
        }

        //Passing turns
        if (toDelete.Count == 0 && isTurn && madeMove) {
            passTurn();
            return;
        }
        else if (toDelete.Count == 0 && !isTurn && EnemyController.madeMove) {
            passTurn();
            return;
        }

        //Fall
        for (int i = 0; i < BOARDLENGTH * BOARDLENGTH; i++) {
            if (((Thing)grid[i]).getType() == -1) {
                audio.Play();
                //Debug.Log("Autumn");
                for (int j = i + BOARDLENGTH; j < BOARDLENGTH * BOARDLENGTH; j += BOARDLENGTH) {
                    
                    if (grid[j].getType() != -1) {
                        grid[j].getObject().GetComponent<ThingController>().accelerateForNextMove();
                        grid[j].getObject().GetComponent<ThingController>().setSpeed(8f);
                        swap(i, j);
                        //Debug.Log(i + " " + j);
                        break;
                    }
                    
                }
            }
        }

        //Replace
        toDelete.Clear();
        for (int i = 0; i < grid.Count; i++) {
            if (grid[i].getType() == -1) {
                toDelete.Add(i);
            }
        }
        toDelete.UnionWith(toDestroy);

        foreach (int i in toDelete) {
            //Delete objects and send them away
            float angle = UnityEngine.Random.Range(0f, 360f);
            float distance = UnityEngine.Random.Range(10f, 20f);
            grid[i].getObject().transform.Rotate(new Vector3(0f, 0f, angle));
            grid[i].getObject().transform.Translate(new Vector3(distance, 0f, distance));
            grid[i].getObject().transform.rotation = Quaternion.identity;

            int randType = (int)UnityEngine.Random.Range(0.0f, 6.0f);
            grid[i].setType(randType);
            if (randType == 3) { //Make super sword pieces, may do this for more later
                if (UnityEngine.Random.value < 0.1) {
                    grid[i].setIsSuperPiece(true);
                    grid[i].getObject().GetComponent<ThingController>().light.gameObject.SetActive(true);
                    grid[i].getObject().GetComponent<Image>().color = new Color(0.5f, 0.2f, 0.25f);
                }
            }

            grid[i].getObject().GetComponent<ThingController>().assignPiece(randType);

            grid[i].getObject().GetComponent<ThingController>().setSpeed(50f);
            
        }

        toDelete.Clear();
        toDestroy.Clear();
    }

    //Adds super piece explosion to toDelete, and runs again if the new list contains more super pieces
    private void handleSuperPieces(List<int> superPieces) {
        List<int> positions = new List<int>();
        foreach (int i in toDelete) {
            if (grid[i].getIsSuperPiece()) {
                superPieces.Add(i);
                //Add pieces around
                //Debug.Log("Adding surrounding pieces to be destroyed");
                for (int j = -1; j <= 1; j++) {
                    if (i + j * BOARDLENGTH < 0 || i + j * BOARDLENGTH >= BOARDLENGTH * BOARDLENGTH) {
                        continue;
                    }
                    for (int k = -1; k <= 1; k++) {
                        if (i + j * BOARDLENGTH + k >= 0 && i + j * BOARDLENGTH + k < BOARDLENGTH * BOARDLENGTH) {
                            if (i + j * BOARDLENGTH + k >= ((i + j * BOARDLENGTH) / BOARDLENGTH) * BOARDLENGTH && i + j * BOARDLENGTH + k < ((i + (j + 1) * BOARDLENGTH) / BOARDLENGTH) * BOARDLENGTH) {
                                positions.Add(i + j * BOARDLENGTH + k);
                                //Debug.Log(i + j * BOARDLENGTH + k);
                            }
                        }
                        if (i + k % BOARDLENGTH == 7) {
                            //Debug.Log("Reached edge");
                            break;
                        }
                    }
                }
            }
        }
        toDelete.UnionWith(positions);

        foreach (int i in positions) {
            if (grid[i].getIsSuperPiece() && !superPieces.Contains(i)) {
                handleSuperPieces(superPieces);  //May run over the same place multiple times, but sorted set doesn't care, is inefficient, but whatever
            }
        }
    }

    private void checkForMatchesHelper(int index, int type) {
        if (type == -1) {
            Debug.Log("Not real");
            return; }
        look(index, type, 1);
        look(index, type, BOARDLENGTH);
    }

    private bool areFourOrMoreInARows() {
        foreach (int i in toDelete) {
            int matchLength = 0;
            int matchColor = grid[i].getType();
            //Count to the left
            int counter = -1;
            while (i + counter >= getLowerHorizontalBound(i) && grid[i + counter].getType() == matchColor) {
                matchLength++;
                counter--;
            }
            //Count to the right
            counter = 1;
            while (i + counter < getUpperHorizontalBound(i) && grid[i + counter].getType() == matchColor) {
                matchLength++;
                counter++;
            }
            //Check for 4+
            if (matchLength >= 3) {
                makeSpashText("Extra turn", grid[i].getObject().transform.position, Color.white, 24);
                return true;
            }

            //Count up
            matchLength = 0;
            counter = BOARDLENGTH;
            while (i + counter < BOARDLENGTH * BOARDLENGTH && grid[i + counter].getType() == matchColor) {
                matchLength++;
                counter += BOARDLENGTH;
            }
            //Count down
            counter = -BOARDLENGTH;
            while (i + counter >= 0 && grid[i + counter].getType() == matchColor) {
                matchLength++;
                counter -= BOARDLENGTH;
            }
            //Check for 4+ in this direction
            if (matchLength >= 3) {
                makeSpashText("Extra turn", grid[i].getObject().transform.position, Color.white, 24);
                return true;
            }
        }
        return false;
    }

    private void look(int index, int type, int increment) {
        HashSet<int> potentialDeletion = new HashSet<int>();
        //potentialDeletion.Add(index);

        //Look on positive side and then negative
        int extention = 0;
        while ((index + extention) % BOARDLENGTH < BOARDLENGTH - 1 && index + extention < BOARDLENGTH * BOARDLENGTH) {
            if (GetThing(index + extention).getType() == type) {
                potentialDeletion.Add(index + extention);
                extention += increment;
            }
            else {
                break;
            }
        }
        extention = 0;
        while ((index + extention) % BOARDLENGTH > 0 && index + extention > 0) {
            if (GetThing(index + extention).getType() == type) {
                potentialDeletion.Add(index + extention);
                extention -= increment;
            }
            else {
                break;
            }
        }
        if (potentialDeletion.Count >= 3) {
            toDelete.UnionWith(potentialDeletion);

        }

    }
    public void passTurn() {
        turnNumber++;
        if (isTurn) {
            Debug.Log("Passed turn to enemy");
            if (extraTurn) {
                extraTurn = false;
                madeMove = false;
                turnOnSpells();
                return;
            }
            StartCoroutine(fadeOutBackground());
            isTurn = false;
            EnemyController.madeMove = false;
            if (damageMultiplier > 1f) {
                damageMultiplier -= 0.1f;
                damageMultiplier = (float)Math.Round(damageMultiplier, 1);
            }
            else {
                damageMultiplier = 1;
            }
            multiplierBar.setPercentageFilled((damageMultiplier - 1) / (maxMultiplier - 1f));
            multiplier.text = damageMultiplier.ToString() + "x";
            turnOffSpells();

            weapon.eotEffect(true); //Do EOT player weapon effects
        }
        else {
            Debug.Log("Passed turn to player");
            if (extraTurn) {
                extraTurn = false;
                EnemyController.madeMove = false;
                return;
            }
            EnemyController enemyController = gameObject.GetComponent<EnemyController>();
            handleStatusEffects();
            enemyController.handleStatusEffects();
            StartCoroutine(fadeInBackground());
            isTurn = true;
            madeMove = false;
            enemyController.decrementMultiplier();
            turnOnSpells();

            if (enemyController.weapon.Type != WeaponType.none) { enemyController.weapon.eotEffect(false); }
        }
    }
    public void handleStatusEffects() {

        /*
        foreach (EffectEveryTurn i in effectsEveryTurn) {
            i.performEffect();
            if (effectsEveryTurn.Count == 0) {
                break;
            }
        }
        */

        foreach (StatusEffect i in statusEffects) {

            if (i is EffectEveryTurn) {
                ((EffectEveryTurn)i).performEffect();
            }
            i.decrementTurns();
            i.updateIndicator();

            if (i.getTurns() <= 0) {
                Destroy(i.getIndicator());
            }
        }
        
        statusEffects.RemoveAll(item => item.getTurns() <= 0);
        displayStatusEffects();
    }

    IEnumerator fadeOutBackground() {
        transitioning = true;
        for (float ft = 1f; ft >= 0; ft -= 0.1f) {
            Color c = background.color;
            c.a = ft;
            background.color = c;

            c = enemyBackground.color;
            c.a = 1 - ft;
            enemyBackground.color = c;

            yield return new WaitForSeconds(0.05f);
        }

        transitioning = false;
    }
    IEnumerator fadeInBackground() {
        transitioning = true;
        for (float ft = 0f; ft <= 1; ft += 0.1f) {
            Color c = background.color;
            c.a = ft;
            background.color = c;

            c = enemyBackground.color;
            c.a = 1 - ft;
            enemyBackground.color = c;

            yield return new WaitForSeconds(0.05f);
        }
        transitioning = false;
    }

    /*
    public void score(int type) {

        //Add to numbers
        if (isTurn) {
            //Check if first turn
            if (!madeMove) {
                return;
            }
            switch (type) {
                case (0):
                    if (redMana + 1 <= maxRedMana) {
                        redMana += 1;
                        redManaTextBox.text = redMana.ToString();
                        redBar.setPercentageFilled(redMana / (maxRedMana + 0f));
                    }
                    break;
                case (1):
                    if (blueMana + 1 <= maxBlueMana) {
                        blueMana += 1;
                        blueManaTextBox.text = blueMana.ToString();
                        blueBar.setPercentageFilled(blueMana / (maxBlueMana + 0f));
                    }
                    break;
                case (2):
                    if (yellowMana + 1 <= maxYellowMana) {
                        yellowMana += 1;
                        yellowManaTextBox.text = yellowMana.ToString();
                        yellowBar.setPercentageFilled(yellowMana / (maxYellowMana + 0f));
                    }
                    break;
                case (3):
                    gameObject.GetComponent<EnemyController>().dealDamage(2);
                    break;
                case (4):
                    dealDamage(-2);
                    break;
                case (5):
                    if (damageMultiplier + 0.5f <= maxMultiplier) {
                        damageMultiplier += 0.5f;
                    }
                    else {
                        damageMultiplier = maxMultiplier;
                    }
                    multiplier.text = damageMultiplier.ToString() + "x";
                    multiplierBar.setPercentageFilled((damageMultiplier - 1) / (maxMultiplier - 1f));
                    break;
            }
        }
        else {
            gameObject.GetComponent<EnemyController>().score(type);

        }
    }
    */
    public void scoreByAmount(Dictionary<int, int> typeCounts) {
        if (isTurn) {
            //Check if first turn
            if (!madeMove) {
                return;
            }
            if (typeCounts[0] != 0) {
                int real = (int)weapon.manaEffect(0, redMana + typeCounts[0]);
                string amount = real > 0 ? "+" + (real - redMana) : real.ToString();
                setRedMana(real);
                makeManaSplashText(amount, isTurn, 0);
                playManaSound(0);
            }
            if (typeCounts[1] != 0) {
                int real = (int)weapon.manaEffect(1, blueMana + typeCounts[1]);
                string amount = real > 0 ? "+" + (real - blueMana) : real.ToString();
                setBlueMana(real);
                makeManaSplashText(amount, isTurn, 1);
                playManaSound(1);
            }
            if (typeCounts[2] != 0) {
                int real = (int)weapon.manaEffect(2, yellowMana + typeCounts[2]);
                string amount = real > 0 ? "+" + (real - yellowMana) : real.ToString();
                setYellowMana(real);
                makeManaSplashText(amount, isTurn, 2);
                playManaSound(2);
            }
            if (typeCounts[3] != 0) {
                int real = gameObject.GetComponent<EnemyController>().applyModifiersToDamageDone(typeCounts[3], DamageType.matchDamage);
                gameObject.GetComponent<EnemyController>().dealDamage(real);
                makeSplashText(real + " Damage", !isTurn, Color.white);
                playManaSound(3);
            }
            if (typeCounts[4] != 0) {
                int real = applyModifiersToDamageDone(-typeCounts[4], DamageType.matchDamage);
                dealDamage(real);
                makeSplashText("Healed for " + Mathf.Abs(real), isTurn, Color.white);
                playManaSound(4);
            }
            if (typeCounts[5] != 0 && damageMultiplier + weapon.manaEffect(5, 0.2f * typeCounts[5]) <= maxMultiplier) {
                float real = weapon.manaEffect(5, 0.2f * typeCounts[5]);
                damageMultiplier += real;
                makeSplashText("Multiplier +" + real, isTurn, Color.white);
                playManaSound(5);
            }
            else if(damageMultiplier + weapon.manaEffect(5, 0.2f * typeCounts[5]) > maxMultiplier) {
                makeSplashText("Multiplier +" + (maxMultiplier - damageMultiplier), isTurn, Color.white);
                damageMultiplier = maxMultiplier;
                playManaSound(5);
            }
            multiplier.text = damageMultiplier.ToString() + "x";
            multiplierBar.setPercentageFilled((damageMultiplier - 1) / (maxMultiplier - 1f));
        }
        else {
            gameObject.GetComponent<EnemyController>().scoreByAmount(typeCounts);
        }
    }

    public int getLastScored(int type) {
        return typeCounts[type];
    }

    public void playManaSound(int type) {
        if (typeJingles[type] != null) {
            typeJingles[type].Play();
        }
    }

    public void generateParticles(int index, int type) {
        //If it's the first turn
        if (isTurn && !madeMove) {
            return;
        }
        GameObject trail = Instantiate(Resources.Load<GameObject>("Trail"));
        trail.transform.position = grid[index].getObject().transform.position;
        ParticleSystem.MainModule trailMain = trail.GetComponent<ParticleSystem>().main;

        //Making target location
        Vector3 target;

        if (type >= 0) { trailMain.startColor = matchingColors[type]; }

        //Setting particle color
        switch (type) {
            case (0):
                target = new Vector3(-6f, 2f, 0);
                break;
            case (1):
                target = new Vector3(-6f, 2f, 0);
                break;
            case (2):
                target = new Vector3(-6f, 2f, 0);
                break;
            case (3):
                target = new Vector3(6f, 2f, 0);
                break;
            case (4):
                target = new Vector3(-6f, 2f, 0);
                break;
            case (5):
                target = new Vector3(-3f, -4f, 0);
                break;
            default:
                target = new Vector3(-6f, 2f, 0);
                break;
        }
        //Enemy locations are player locations mirrored over x
        if (!isTurn) {
            target.x *= -1;
        }
        trail.GetComponent<TrailHandler>().setTarget(target);
    }

    public bool isValidMoveTime() {
        foreach (Thing i in grid) {
            if (i.getObject().GetComponent<ThingController>().getMoving()) {
                return false;
            }
        }
        return true;
    }

    public List<Move> movesAtPoint(int i) {
        List<Move> moves = new List<Move>();
        int type = grid[i].getType();

        List<int> pieces = new List<int>();
        //Look Left Vertical
        if (i % BOARDLENGTH != 0) {
            
            //Look up left
            pieces.AddRange(movesAtPointHelperVertical (i, BOARDLENGTH, BOARDLENGTH - 1, type));
            //Look down left
            pieces.AddRange(movesAtPointHelperVertical(i, -BOARDLENGTH, -BOARDLENGTH - 1, type));
            if (pieces.Count >= 2) {
                moves.Add(new Move(i, i - 1, pieces.Count + 1, type));
            }
            pieces.Clear();
        }
        //Look Right Vertical
        if (i % BOARDLENGTH != 7) {
            //Look up right
            pieces.AddRange(movesAtPointHelperVertical(i, BOARDLENGTH, BOARDLENGTH + 1, type));
            //look down right
            pieces.AddRange(movesAtPointHelperVertical(i, -BOARDLENGTH, -BOARDLENGTH + 1, type));
            if (pieces.Count >= 2) {
                moves.Add(new Move(i, i + 1, pieces.Count + 1, type));
            }
            pieces.Clear();
        }

        //Look Top Horizontal
        if (i < BOARDLENGTH * BOARDLENGTH - BOARDLENGTH) {
            int displacement = 7;
            //look up left
            while (i + displacement >= (i / BOARDLENGTH + 1) * (BOARDLENGTH) && grid[i + displacement].getType() == type) {
                pieces.Add(i + displacement);
                displacement -= 1;
            }
            //look up right
            displacement = 9;
            while (i + displacement < (i / BOARDLENGTH + 2) * (BOARDLENGTH) && grid[i + displacement].getType() == type ) {
                pieces.Add(i + displacement);
                displacement += 1;
            }
            if (pieces.Count >= 2) {
                moves.Add(new Move(i, i + BOARDLENGTH, pieces.Count + 1, type));
            }
            pieces.Clear();
        }
        //Look Bottom Horizontal
        if (i >= BOARDLENGTH) {
            int displacement = -9;
            //look down left
            while (i + displacement >= (i / BOARDLENGTH - 1) * BOARDLENGTH && grid[i + displacement].getType() == type) {
                pieces.Add(i + displacement);
                displacement -= 1;
            }
            //look down right
            displacement = -7;
            while (i + displacement < (i / BOARDLENGTH) * BOARDLENGTH && grid[i + displacement].getType() == type) {
                pieces.Add(i + displacement);
                displacement += 1;
            }
            if (pieces.Count >= 2) {
                moves.Add(new Move(i, i - BOARDLENGTH, pieces.Count + 1, type));
            }
            pieces.Clear();
        }

        //Look up
        if (getLowerHorizontalBound(i) < BOARDLENGTH * BOARDLENGTH - 3 * BOARDLENGTH) {
            int displacement = BOARDLENGTH * 2;
            while (i + displacement < BOARDLENGTH * BOARDLENGTH && grid[i + displacement].getType() == type) {
                pieces.Add(i + displacement);
                displacement += BOARDLENGTH;
            }
            if (pieces.Count >= 2) {
                moves.Add(new Move(i, i + BOARDLENGTH, pieces.Count + 1, type));
            }
            pieces.Clear();
        }
        //Look down
        if (getLowerHorizontalBound(i) >= 3 * BOARDLENGTH) {
            int displacement = -BOARDLENGTH * 2;
            while (i + displacement >= 0 && grid[i + displacement].getType() == type) {
                pieces.Add(i + displacement);
                displacement -=BOARDLENGTH;
            }
            if (pieces.Count >= 2) {
                moves.Add(new Move(i, i - BOARDLENGTH, pieces.Count + 1, type));
            }
            pieces.Clear();
        }
        //Look right
        if (getUpperHorizontalBound(i) >= i + 3) {
            int displacement = 2;
            while (i + displacement < getUpperHorizontalBound(i) && grid[i + displacement].getType() == type) {
                pieces.Add(i + displacement);
                displacement += 1;
            }
            if (pieces.Count >= 2) {
                moves.Add(new Move(i, i + 1, pieces.Count + 1, type));
            }
            pieces.Clear();
        }
        //Look left
        if (getLowerHorizontalBound(i) <= i - 3) {
            int displacement = -2;
            while (i + displacement >= getLowerHorizontalBound(i) && grid[i + displacement].getType() == type) {
                pieces.Add(i + displacement);
                displacement -= 1;
            }
            if (pieces.Count >= 2) {
                moves.Add(new Move(i, i - 1, pieces.Count + 1, type));
            }
            pieces.Clear();
        }

        return moves;
    }
    //Returns the position of the first grid element in the row
    private int getLowerHorizontalBound(int position) {
        return (position / BOARDLENGTH) * BOARDLENGTH;
    }
    //Returns the position of the first grid element in the next row
    private int getUpperHorizontalBound(int position) {
        return ((position / BOARDLENGTH) + 1) * BOARDLENGTH;
    }

    private List<int> movesAtPointHelperVertical(int i, int increment, int displacement, int type) {
        List<int> pieces = new List<int>();
        while (i + displacement < BOARDLENGTH * BOARDLENGTH && i + displacement >= 0 && grid[i + displacement].getType() == type) {
            pieces.Add(i + displacement);
            displacement += increment;
        }
        return pieces;
    }

    public void selectPiece(int index, Color color = default(Color)) {
        GameObject selector = Instantiate(Resources.Load<GameObject>("Selector"));
        selector.transform.parent = grid[index].getObject().transform;
        selector.transform.localPosition = new Vector3(0f, 0f, -1f);
        if (color != default(Color)) {
            selector.GetComponent<SpriteRenderer>().color = color;
        }
    }

    public SortedSet<int> getToDelete() {
        return toDelete;
    }

    public void disableGrid() {
        foreach (Thing i in grid) {
            i.getObject().GetComponent<ThingController>().enabled = false;
        }
        foreach (Button i in equippedSpells) {
            i.enabled = false;
        }
        this.enabled = false;
    }

    public void enableGrid() {
        foreach (Thing i in grid) {
            i.getObject().GetComponent<ThingController>().enabled = true;
        }
        foreach (Button i in equippedSpells) {
            i.enabled = true;
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (isValidMoveTime()) {
            checkForMatches();
        }
    }

    public static void makeSplashText(string text, bool player) { //true for player, false for enemy
        GameObject splashText = Instantiate(Resources.Load<GameObject>("helperText"), GameObject.Find("Canvas").transform);
        splashText.GetComponent<helperTextHandler>().setText(text);
        if (player) {
            splashText.GetComponent<helperTextHandler>().setPosition(GameObject.Find("portrait").transform.position);
        }
        else {
            splashText.GetComponent<helperTextHandler>().setPosition(GameObject.Find("enemyPortrait").transform.position);
        }
    }

    public static void makeSplashText(string text, bool player, Color color = default(Color)) { //true for player, false for enemy
        GameObject splashText = Instantiate(Resources.Load<GameObject>("helperText"), GameObject.Find("Canvas").transform);
        splashText.GetComponent<helperTextHandler>().setText(text);
        if (color != default(Color)) {
            splashText.GetComponent<helperTextHandler>().setTextColor(color);
        }
        if (player) {
            splashText.GetComponent<helperTextHandler>().setPosition(GameObject.Find("portrait").transform.position);
        }
        else {
            splashText.GetComponent<helperTextHandler>().setPosition(GameObject.Find("enemyPortrait").transform.position);
        }
    }

    public static void makeSpashText(string text, Vector3 position, Color color = default(Color), int size = 18) {
        GameObject splashText = Instantiate(Resources.Load<GameObject>("helperText"), GameObject.Find("Canvas").transform);
        helperTextHandler helperText = splashText.GetComponent<helperTextHandler>();
        helperText.setText(text);
        helperText.setPosition(position);
        helperText.setTextSize(size);
        if (color != default(Color)) {
            helperText.setTextColor(color);
        }
    }

    public static void makeManaSplashText(string amount, bool player, int color) {
        GameObject splashText = Instantiate(Resources.Load<GameObject>("helperText"), GameObject.Find("Canvas").transform);
        splashText.GetComponent<helperTextHandler>().setText(amount);
        Vector3 manaPosition = new Vector3(color - 1, -1);
        if (player) {   
            splashText.GetComponent<helperTextHandler>().setPosition(GameObject.Find("portrait").transform.position + manaPosition);
        }
        else {
            splashText.GetComponent<helperTextHandler>().setPosition(GameObject.Find("enemyPortrait").transform.position + manaPosition);
        }


    }

}

public class Thing {
    private GameObject boardPiece;
    private int type;
    private bool isSuperPiece;

    public Thing(ref GameObject bp, int t) {
        boardPiece = bp;
        type = t;
        isSuperPiece = false;
    }

    public ref GameObject getObject() {
        return ref boardPiece;
    }

    public int getType() {
        return type;
    }

    public void setObject(ref GameObject bp) {
        boardPiece = bp;
    }

    public void setType(int t) {
        type = t;
    }

    public void setIsSuperPiece(bool pieceStatus) {
        isSuperPiece = pieceStatus;
    }

    public bool getIsSuperPiece() {
        return isSuperPiece;
    }

}

public enum DamageType {
    spellDamage,
    matchDamage,
    statusEffectDamage
}

public enum colorType { 
    red,
    blue,
    yellow,
    damage,
    health,
    multiplier
}

//Interface for dealing with players, AI or human
public interface PlayerController {
    int applyModifiersToDamageDone(int amount, DamageType damageType);
    void dealDamage(int damage);
    GameObject getPortrait();
    void setMana(int id, int mana);
    void setMultiplier(float amount);
    int getMana(int id);
    void addStatusEffect(StatusEffect effect);
}

public class Combat {
    List<int> enemies;
    List<int> weapons;
    List<string> portraits;
    List<string> tutorials;
    string nextDialogue;
    int round;
    bool isSolo = false;
    string soloArguments = "";
    string companion = "none";

    public Combat(string combatFile) {
        TextAsset combat = Resources.Load<TextAsset>("Combats/" + combatFile);
        List<string> lines = new List<string>(combat.text.Split('\n'));
        enemies = new List<int>();
        weapons = new List<int>();
        portraits = new List<string>();
        tutorials = new List<string>();

        foreach (string line in lines) {
            string[] lineParts = line.Split(':');
            if (lineParts[0] == "enemy") {
                string[] enemyParts = lineParts[1].Split(' ');
                enemies.Add(int.Parse(enemyParts[0]));
                weapons.Add(int.Parse(enemyParts[1]));
                if (enemyParts.Length > 2) { portraits.Add(enemyParts[2].Trim()); }
                else { portraits.Add(""); }
            }
            else if (lineParts[0] == "dialogue") {
                nextDialogue = lineParts[1].Trim();
            }
            else if (lineParts[0] == "solo") {
                isSolo = true;
                soloArguments = lineParts[1];
            }
            else if (lineParts[0] == "companion") {
                companion = lineParts[1].Trim();
            }
            else if (lineParts[0] == "tutorial") {
                tutorials.Add(lineParts[1].Trim());
            }
        }
        round = 0;
    }

    public int getEnemy(int round) {
        if (round >= enemies.Count) {
            return -1;
        }
        else {
            return enemies[round];
        }
    }
    public int getWeapon(int round) {
        if (round >= enemies.Count) {
            return -1;
        }
        else {
            return weapons[round];
        }
    }

    public string getPortrait(int round) {
        if (round >= enemies.Count) {
            return "";
        }
        else {
            return portraits[round];
        }
    }

    public void advanceRound() {
        round++;
    }

    public int getRound() {
        return round;
    }

    public bool getIsSolo() {
        return isSolo;
    }

    public string getSoloArguments() {
        return soloArguments;
    }

    public string getNextDialogue() {
        return nextDialogue;
    }

    public int getTotalRounds() {
        if (!isSolo) {
            return enemies.Count;
        }
        else {
            return 1;
        }
    }

    public string getCompanion() {
        return companion;
    }

    public void removeCompanion() {
        companion = "none";
    }

    public List<string> getTutorials() {
        return tutorials;
    }

}

public class Companion{
    public string name;
    public string companionChangeoverDialogue;
    public int hp;
    public int maxRedMana;
    public int maxBlueMana;
    public int maxYellowMana;
    public int weapon;
    public int[] spells;

    public Companion(string name, string companionChangeoverDialogue, int hp, int maxRedMana, int maxBlueMana, int maxYellowMana, int weapon, int[] spells) {
        this.name = name;
        this.companionChangeoverDialogue = companionChangeoverDialogue;
        this.hp = hp;
        this.maxRedMana = maxRedMana;
        this.maxBlueMana = maxBlueMana;
        this.maxYellowMana = maxYellowMana;
        this.weapon = weapon;
        this.spells = spells;
    }
}

public class StatusEffect : TemporaryEffect {

    public GameObject indicator;
    public bool affectedParty; //false is enemy, true for player
    public TooltipHandler tooltipHandler;
    int turns;


    public StatusEffect(GameObject i, bool ap, int t) {
        indicator = i;
        affectedParty = ap;
        turns = t;
        tooltipHandler = i.GetComponent<TooltipHandler>();
        if (affectedParty) {
            tooltipHandler.setPosition(new Vector3(100, 0f));
        }
        else {
            tooltipHandler.setPosition(new Vector3(-100, 0f));
        }
        updateIndicator();
    }

    public void decrementTurns() {
        setTurns(getTurns() - 1);
    }

    public bool getAffectedParty() {
        return affectedParty;
    }

    public GameObject getIndicator() {
        return indicator;
    }

    public int getTurns() {
        return turns;
    }

    public void setTurns(int turns) {
        this.turns = turns;
    }

    public virtual string updateIndicator() {
        return "";
    }
}

//Defines all extra status effects that don't have space to display
public class Etcetera{

    public List<StatusEffect> extraStatusEffects;
    bool affectedParty; //True for player, false for enemy
    GameObject indicator;
    public TooltipHandler tooltipHandler;

    public Etcetera(GameObject i, bool ap) {
        extraStatusEffects = new List<StatusEffect>();
        indicator = i;
        affectedParty = ap;

        if (i) { 
            tooltipHandler = i.GetComponent<TooltipHandler>();
            if (affectedParty) {
                tooltipHandler.setPosition(new Vector3(2.3f, 0f));
            }
            else {
                tooltipHandler.setPosition(new Vector3(-2.3f, 0f));
            }
        }
    }

    public void updateIndicator() {
        string tooltip = "";
        foreach (StatusEffect i in extraStatusEffects) {
            tooltip += i.updateIndicator() + "\n";
        }
        tooltipHandler.setText(tooltip);
    }

    public void enqueue(StatusEffect effect) {
        //Adds a new status effect to this queue
        extraStatusEffects.Add(effect);
        
    }

    public GameObject getIndicator() {
        return indicator;
    }

    public void clearIndicator() {
        indicator = null;
    }


    public void clear() {
        extraStatusEffects.Clear();
    }

}

public class Poison : StatusEffect, EffectEveryTurn {

    int damage;

    public Poison(GameObject i, bool ap, int t, int damage) : base(i, ap, t) {
        this.damage = damage;
        i.GetComponent<Image>().sprite = Resources.Load<Sprite>("StatusEffects/Poison");
    }

    public void performEffect() {
        Debug.Log("Dealing Poison damage");
        if (affectedParty) {
            GameObject.Find("Grid").GetComponent<GridController>().dealDamage(GameObject.Find("Grid").GetComponent<GridController>().applyModifiersToDamageDone(damage, DamageType.statusEffectDamage));
            GridController.makeSplashText(damage + " Poison Damage", true);
        }
        else {
            GameObject.Find("Grid").GetComponent<EnemyController>().dealDamage(GameObject.Find("Grid").GetComponent<EnemyController>().applyModifiersToDamageDone(damage, DamageType.statusEffectDamage));
            GridController.makeSplashText(damage + " Poison Damage", false);
        }
    }

    public override string updateIndicator() {
        string tooltip = "Damage per turn: " + damage + "\nTurns Remaining: " + getTurns();
        tooltipHandler.setText(tooltip);
        return tooltip;
    }

}

public class Seal : StatusEffect, EffectEveryTurn
{
    //potency unused
    public Seal(GameObject i, bool ap, int t, int p) : base(i, ap, t) {
        i.GetComponent<Image>().sprite = Resources.Load<Sprite>("StatusEffects/Seal");
    }

    public void performEffect() {
        //Doesn't have actual effect, turning on spells checks for existence of seal

        if (affectedParty) {
            GameObject.Find("Grid").GetComponent<GridController>().turnOffSpells();
            //GameObject.Find("Grid").GetComponent<GridController>().dealDamage(GameObject.Find("Grid").GetComponent<GridController>().applyModifiersToDamageDone(damage, DamageType.statusEffectDamage));
            //GridController.makeSplashText(damage + " Poison Damage", true);
        }
        else {
            //GameObject.Find("Grid").GetComponent<EnemyController>().dealDamage(GameObject.Find("Grid").GetComponent<EnemyController>().applyModifiersToDamageDone(damage, DamageType.statusEffectDamage));
            //GridController.makeSplashText(damage + " Poison Damage", false);
        }
    }

    public override string updateIndicator() {
        string tooltip = "Prevents spell use\nTurns Remaining: " + getTurns();
        tooltipHandler.setText(tooltip);
        return tooltip;
    }

}

public class Shield : StatusEffect, DamageReductionEffect {

    int prevented;

    public Shield(GameObject i, bool ap, int t, int p) : base(i, ap, t) {
        prevented = p;
        i.GetComponent<Image>().sprite = Resources.Load<Sprite>("StatusEffects/Shield");

    }

    public int modifyDamage(int preProcessedDamage) {
        int processedDamage = preProcessedDamage;
        if (preProcessedDamage >= 0) {
            processedDamage = preProcessedDamage - prevented >= 0 ? preProcessedDamage - prevented : preProcessedDamage;
        }
        return processedDamage;
    }

    public override string updateIndicator() {
        string tooltip = "Prevents " + prevented + " damage taken\nTurns Remaining: " + getTurns();
        tooltipHandler.setText(tooltip);
        return tooltip;
    }
}

public class DamageBuff : StatusEffect, DamageIncreaseEffect {
    int added;

    public DamageBuff(GameObject i, bool ap, int t, int p) : base(i, ap, t) {
        added = p;
        i.GetComponent<Image>().sprite = Resources.Load<Sprite>("StatusEffects/DamageBuff");
    }

    public int modifyDamage(int preProcessedDamage) {
        int processedDamage = preProcessedDamage >= 0 ? preProcessedDamage + added : preProcessedDamage;
        return processedDamage;
    }

    public override string updateIndicator() {
        string tooltip = "Adds " + added + " to damage dealt\nTurns Remaining: " + getTurns();
        tooltipHandler.setText(tooltip);
        return tooltip;
    }
}

public class SpellDamageBuff : StatusEffect, SpellDamageIncreaseEffect
{
    int added;

    public SpellDamageBuff(GameObject i, bool ap, int t, int p) : base(i, ap, t) {
        added = p;
        i.GetComponent<Image>().sprite = Resources.Load<Sprite>("StatusEffects/DamageBuff");
    }

    public int modifyDamage(int preProcessedDamage) {
        int processedDamage = preProcessedDamage >= 0 ? preProcessedDamage + added : preProcessedDamage;
        return processedDamage;
    }

    public override string updateIndicator() {
        string tooltip = "Adds " + added + " to spell damage dealt\nTurns Remaining: " + getTurns();
        tooltipHandler.setText(tooltip);
        return tooltip;
    }
}

public class DoubleDamage : StatusEffect, DamageIncreaseEffect
{
    //potency unused
    public DoubleDamage(GameObject i, bool ap, int t, int p) : base(i, ap, t) {
        i.GetComponent<Image>().sprite = Resources.Load<Sprite>("StatusEffects/DoubleDamage");
    }

    public int modifyDamage(int preProcessedDamage) {
        int processedDamage = preProcessedDamage >= 0 ? preProcessedDamage * 2 : preProcessedDamage;
        return processedDamage;
    }

    public override string updateIndicator() {
        string tooltip = "Doubles damage dealt\nTurns Remaining: " + getTurns();
        tooltipHandler.setText(tooltip);
        return tooltip;
    }
}
public class DamageDebuff : StatusEffect, DamageIncreaseEffect
{
    int subtracted;

    public DamageDebuff(GameObject i, bool ap, int t, int p) : base(i, ap, t) {
        subtracted = p;
        i.GetComponent<Image>().sprite = Resources.Load<Sprite>("StatusEffects/DamageDebuff");
    }

    public int modifyDamage(int preProcessedDamage) {
        int processedDamage = preProcessedDamage >= 0 ? preProcessedDamage - subtracted : preProcessedDamage;
        processedDamage = processedDamage >= 0 ? processedDamage : preProcessedDamage;
        return processedDamage;
    }

    public override string updateIndicator() {
        string tooltip = "Subtracts " + subtracted + " to damage dealt\nTurns Remaining: " + getTurns();
        tooltipHandler.setText(tooltip);
        return tooltip;
    }
}

public class DoublingSeason : StatusEffect, ScoreChangeEffect {
    //potency unused
    public DoublingSeason(GameObject i, bool ap, int t, int p) : base(i, ap, t) {
        i.GetComponent<Image>().sprite = Resources.Load<Sprite>("StatusEffects/DoublingSeason");
    }

    public int modifyScore(int preProcessedScore, int type) {
        if (type < 3 || type == 5) {
            return 2 * preProcessedScore;
        }
        return preProcessedScore;
    }
    public override string updateIndicator() {
        string tooltip = "Doubles mana and multiplier gain\nTurns Remaining: " + getTurns();
        tooltipHandler.setText(tooltip);
        return tooltip;
    }
}

public class ScoreReductionEffect : StatusEffect, ScoreChangeEffect
{
    int subtracted;

    public ScoreReductionEffect(GameObject i, bool ap, int t, int p) : base(i, ap, t) {
        subtracted = p;
        i.GetComponent<Image>().sprite = Resources.Load<Sprite>("StatusEffects/ScoreReduction");
    }

    public int modifyScore(int preProcessedScore, int type) {
        if (type < 3 || type == 5) {
            return preProcessedScore - subtracted >= 0 ? preProcessedScore - subtracted : preProcessedScore;
        }
        return preProcessedScore;
    }
    public override string updateIndicator() {
        string tooltip = "Reduces mana and multiplier gain by "+subtracted +"\nTurns Remaining: " + getTurns();
        tooltipHandler.setText(tooltip);
        return tooltip;
    }
}

public class HealingBuff : StatusEffect, ScoreChangeEffect {
    int added;

    public HealingBuff(GameObject i, bool ap, int t, int p) : base(i, ap, t) {
        added = p;
        i.GetComponent<Image>().sprite = Resources.Load<Sprite>("StatusEffects/HealingBuff");
    }

    public int modifyScore(int preProcessedScore, int type) {
        if (type == 4) {
            return added + preProcessedScore;
        }
        return preProcessedScore;
    }
    public override string updateIndicator() {
        string tooltip = "Adds " + added + " to healing done\nTurns Remaining: " + getTurns();
        tooltipHandler.setText(tooltip);
        return tooltip;
    }
}




//Effects that decay and have effects every turn
public interface EffectEveryTurn : TemporaryEffect {
    void performEffect();
}

//Effects that decrease damage taken
public interface DamageReductionEffect : TemporaryEffect {
    int modifyDamage(int preProcessedDamage);
}

//Effects that increase damage dealt
public interface DamageIncreaseEffect : TemporaryEffect {
    int modifyDamage(int preProcessedDamage);
}

//Effects that increase spell damage dealt
public interface SpellDamageIncreaseEffect : TemporaryEffect
{
    int modifyDamage(int preProcessedDamage);
}

public interface SpellDamageDecreaseEffect : TemporaryEffect
{
    int modifyDamage(int preProcessedDamage);
}

public interface ScoreChangeEffect : TemporaryEffect {
    int modifyScore(int preProcessedScore, int type);
}


//Effects that decay, but don't necessarily have effects that trigger every turn
public interface TemporaryEffect {
    bool getAffectedParty();
    int getTurns();
    void decrementTurns();
    GameObject getIndicator();
}
