using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;
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
    PlayerController currentSpellCaster;
    PlayerController currentSpellTarget;
    List<string> pendingSpellParameters;
    int pendingSpellParameterIndex;
    int lastSpellDestroyedCount;
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
    List<int> selectedSpellTargets;
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
    CombatantRuntime combatant;

    private void Awake() {
        EnsureRuntimeCollections();
    }

    private void EnsureRuntimeCollections() {
        if (statusEffects == null) {
            statusEffects = new List<StatusEffect>();
        }
        if (extraStatusEffects == null) {
            extraStatusEffects = new Etcetera(null, false);
        }
        if (moves == null) {
            moves = new List<Move>();
        }
        if (toDelete == null) {
            toDelete = new SortedSet<int>();
        }
        if (toDestroy == null) {
            toDestroy = new SortedSet<int>();
        }
        if (selectedSpellTargets == null) {
            selectedSpellTargets = new List<int>();
        }
        if (weapon == null) {
            weapon = new Weapon();
        }
        if (combatant == null) {
            combatant = new CombatantRuntime(new CombatantState(), GetCombatantView(), includeMaxManaInText: true);
        }
        else if (combatant.View == null) {
            combatant.SetView(GetCombatantView());
        }
    }

    private CombatantView GetCombatantView() {
        CombatSceneRefs refs = CombatSceneRefs.Instance;
        CombatantView view = refs != null ? refs.PlayerCombatantView : null;
        if (view == null) {
            Transform holder = transform.Find("PlayerCombatantView");
            if (holder == null) {
                holder = new GameObject("PlayerCombatantView").transform;
                holder.SetParent(transform, false);
            }
            view = holder.GetComponent<CombatantView>();
            if (view == null) {
                view = holder.gameObject.AddComponent<CombatantView>();
            }
        }
        return view;
    }

    private void SetCombatantFallbacks() {
        CombatantView view = combatant.View;
        if (view == null) {
            return;
        }

        Transform spellParent = paperback != null ? paperback.transform : null;
        Transform statusParent = portrait != null ? portrait.transform : null;
        view.SetFallbacks(name, hp, redManaTextBox, blueManaTextBox, yellowManaTextBox, multiplier,
            healthBar, redBar, blueBar, yellowBar, multiplierBar, portrait, spellParent, statusParent);
    }

    private void SyncFieldsFromCombatant() {
        CombatantState state = combatant.State;
        health = state.Health;
        totalHealth = state.MaxHealth;
        redMana = state.RedMana;
        blueMana = state.BlueMana;
        yellowMana = state.YellowMana;
        maxRedMana = state.MaxRedMana;
        maxBlueMana = state.MaxBlueMana;
        maxYellowMana = state.MaxYellowMana;
        damageMultiplier = state.DamageMultiplier;
        weapon = state.Weapon;
    }

    // Start is called before the first frame update
    void Start() {
        //Initialization
        EnsureRuntimeCollections();
        isCasting = false;
        transitioning = false;
        currentSpell = null;
        currentSpellCaster = null;
        currentSpellTarget = null;
        pendingSpellParameters = null;
        pendingSpellParameterIndex = 0;
        lastSpellDestroyedCount = 0;
        turnNumber = 0;
        toDelete = new SortedSet<int>();
        toDestroy = new SortedSet<int>();
        madeMove = false;
        isTurn = true;
        grid = new List<Thing>();
        audio = GetComponent<AudioSource>();
        statusEffects = new List<StatusEffect>();
        selectedSpellTargets = new List<int>();
        extraStatusEffects = new Etcetera(null, false);
        moves = new List<Move>();
        extraTurn = false;
        skydrop.sprite = Resources.Load<Sprite>("Backgrounds/" + SaveGameService.GetString("background", "bg"));
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

        foreach (Text i in FindObjectsByType<Text>(FindObjectsSortMode.None)) {

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
                    health = SaveGameService.GetInt("hp", 50);
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

        foreach (Image i in FindObjectsByType<Image>(FindObjectsSortMode.None)) {
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
                    maxRedMana = SaveGameService.GetInt("maxRedMana", 10);
                    break;
                case ("blueBarFilled"):
                    blueBar = i.GetComponent<BarHandler>();
                    maxBlueMana = SaveGameService.GetInt("maxBlueMana", 10);
                    break;
                case ("yellowBarFilled"):
                    yellowBar = i.GetComponent<BarHandler>();
                    maxYellowMana = SaveGameService.GetInt("maxYellowMana", 10);
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

        SetCombatantFallbacks();
        combatant.SetIdentity(SaveGameService.GetString("name", "Rachel"), health, maxRedMana, maxBlueMana, maxYellowMana);
        combatant.State.Weapon = weapon;
        SyncFieldsFromCombatant();

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
            newThing.GetComponent<Image>().color = Color.white;
            newThing.GetComponent<ThingController>().light.gameObject.SetActive(false);
            newThing.GetComponent<ThingController>().assignPiece(randType);

        }

        //checkForMatches();

        //Getting and assigning spells
        equippedSpells = new List<Button>();
        List<Spell> spellsList = SpellContentLoader.LoadSpells();
        equippedSpells = new List<Button>(new Button[5]);

        for (int i = 0; i < spellsList.Count; i++) {
            int spellStatus = SaveGameService.GetInt(spellsList[i].Name, 0);
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
        List<Weapon> weapons = WeaponContentLoader.LoadWeapons();
        foreach (Weapon i in weapons) {
            if (SaveGameService.GetInt(i.Name, 0) == 2) { //2 for equipped, 1 for own but not equipped, 0 for not owned
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
        randomValue *= 100f;
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

        maxRedMana = SaveGameService.GetInt("maxRedMana", 10);
        maxBlueMana = SaveGameService.GetInt("maxBlueMana", 10);
        maxYellowMana = SaveGameService.GetInt("maxYellowMana", 10);
        totalHealth = SaveGameService.GetInt("hp", 100);
        name.text = SaveGameService.GetString("name", "Rachel");

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
        combatant.SetIdentity(SaveGameService.GetString("name", "Rachel"), totalHealth, maxRedMana, maxBlueMana, maxYellowMana);
        combatant.State.Weapon = weapon;
        SyncFieldsFromCombatant();

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
        List<Spell> spells = SpellContentLoader.LoadSpells();
        equippedSpells = new List<Button>(new Button[5]);

        for (int i = 0; i < spells.Count; i++) {
            int spellStatus = SaveGameService.GetInt(spells[i].Name, 0);
            if (spellStatus > 0 && spellStatus < 6) { //1 -5 for order of spells, 6 for available but not equipped
                //equippedSpells.Add(Instantiate(Resources.Load<Button>("SpellButton"), GameObject.Find("paperback").transform));
                equippedSpells[spellStatus - 1] = (Instantiate(Resources.Load<Button>("SpellButton"), GameObject.Find("paperback").transform));
                equippedSpells[spellStatus - 1].GetComponent<SpellButtonHandler>().setSpell(spells[i]);

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
        List<Weapon> weapons = WeaponContentLoader.LoadWeapons();
        foreach (Weapon i in weapons) {
            if (SaveGameService.GetInt(i.Name, 0) == 2) { //2 for equipped, 1 for own but not equipped, 0 for not owned
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
        combatant.State.Weapon = weapon;
        SyncFieldsFromCombatant();

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
        combatant.SetHealth(health);
        SyncFieldsFromCombatant();
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
        combatant.SetIdentity(char.ToUpper(companion.name[0]) + companion.name.Substring(1), totalHealth, maxRedMana, maxBlueMana, maxYellowMana);
        SyncFieldsFromCombatant();

        List<Spell> spells = SpellContentLoader.LoadSpells();
        foreach (Button i in equippedSpells) {
            Destroy(i.gameObject);
        }
        equippedSpells.Clear();

        for (int i = 0; i < companion.spells.Length; i++) {
            //equippedSpells[i].transform.SetParent(GameObject.Find("Canvas").transform);
            equippedSpells.Add(Instantiate(Resources.Load<Button>("SpellButton"), GameObject.Find("paperback").transform));
            equippedSpells[equippedSpells.Count - 1].GetComponent<SpellButtonHandler>().setSpell(spells[companion.spells[i]]);
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
        List<Weapon> weapons = WeaponContentLoader.LoadWeapons();
        if (companion.weapon != -1) {
            weapon = weapons[companion.weapon];
        }

        if (weapon.Type != WeaponType.none) {
            weaponObject.SetActive(true);
            weaponObject.GetComponent<WeaponButtonHandler>().setWeapon(weapon);
        }
        else {
            weaponObject.GetComponent<Image>().sprite = Resources.Load<Sprite>("Button");
            weaponObject.SetActive(false);
        }
        combatant.State.Weapon = weapon;
        SyncFieldsFromCombatant();

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
        EnsureRuntimeCollections();
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
        EnsureRuntimeCollections();
        EnemyController enemyController = gameObject.GetComponent<EnemyController>();
        Weapon enemyWeapon = enemyController.weapon ?? new Weapon();

        if (damageType == DamageType.spellDamage) {
            //Apply weapon effects
            amount = enemyWeapon.spellDamageEffect() + amount;
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
            amount = (int)enemyWeapon.manaEffect(3, 2 * amount);
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
        EnsureRuntimeCollections();
        if (combatant.State.Health - damage <= combatant.State.MaxHealth) {
            combatant.SetHealth(combatant.State.Health - damage);
            SyncFieldsFromCombatant();
            if (health <= 0) {
                combatant.SetHealth(0);
                SyncFieldsFromCombatant();

                //lose
                Debug.Log("You lose");

                resultsScreenHandler.setLoss();
                ScriptController.pauseGame();
            }
        }
        else {
            combatant.SetHealth(combatant.State.MaxHealth);
            SyncFieldsFromCombatant();
        }
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
        EnsureRuntimeCollections();
        combatant.SetMana(id, mana);
        SyncFieldsFromCombatant();
    }

    public int getMana(int id) {
        EnsureRuntimeCollections();
        return combatant.GetMana(id);
    }

    public void setYellowMana(int mana) {
        setMana((int)colorType.yellow, mana);
    }
    public void setBlueMana(int mana) {
        setMana((int)colorType.blue, mana);
    }
    public void setRedMana(int mana) {
        setMana((int)colorType.red, mana);
    }

    public void setMultiplier(float amount) {
        EnsureRuntimeCollections();
        combatant.SetMultiplier(amount);
        SyncFieldsFromCombatant();
    }
    public int getRedMana() {
        EnsureRuntimeCollections();
        return combatant.GetMana((int)colorType.red);
    }
    public int getBlueMana() {
        EnsureRuntimeCollections();
        return combatant.GetMana((int)colorType.blue);
    }
    public int getYellowMana() {
        EnsureRuntimeCollections();
        return combatant.GetMana((int)colorType.yellow);
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

        ExecuteSpellParameters(spell, caster, target, SpellEffectRunner.ParseParameters(spell), 0);
    }

    private void ExecuteSpellParameters(Spell spell, PlayerController caster, PlayerController target, List<string> parameters, int startIndex) {
        for (int i = startIndex; i < parameters.Count; i++) {
            if (ExecuteSpellCommand(parameters[i], spell, caster, target)) {
                pendingSpellParameters = parameters;
                pendingSpellParameterIndex = i + 1;
                return;
            }
        }

        pendingSpellParameters = null;
        pendingSpellParameterIndex = 0;
    }

    public bool ExecuteSpellCommand(string parameter, Spell spell, PlayerController caster, PlayerController target) {
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
                    beginCasting(parameter, spell, caster, target);
                    return true;
                case ("shiftTarget"):
                    //Spells with this parameter shift a target piece to type parameter 1
                    beginCasting(parameter, spell, caster, target);
                    return true;
                case ("swapTargets"):
                    //Spells with this parameter use two selected board pieces together.
                    beginCasting(parameter, spell, caster, target);
                    return true;
                case ("destroyTargetLShape"):
                    beginCasting(parameter, spell, caster, target);
                    return true;
                case ("destroyContiguousChunk"):
                    beginCasting(parameter, spell, caster, target);
                    return true;
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
        return false;
    }
    public int countHeartPiecesTimes2() {
        int sum = 0;
        foreach (Thing i in grid) {
            if (i.getType() == (int)type.health) {
                sum++;
            }
        }
        return 2 * sum;
    }
    public int tenthOfHealth() {
        return health % 10;
    }
    public int countHeartPieces() {
        int sum = 0;
        foreach (Thing i in grid) {
            if (i.getType() == (int)type.health) {
                sum++;
            }
        }
        return sum;
    }
    public int countRedManaPieces() {
        int sum = 0;
        foreach (Thing i in grid) {
            if (i.getType() == (int)type.red) {
                sum++;
            }
        }
        return sum;
    }
    public int countBlueManaPieces() {
        int sum = 0;
        foreach (Thing i in grid) {
            if (i.getType() == (int)type.blue) {
                sum++;
            }
        }
        return sum;
    }
    public int countYellowManaPieces() {
        int sum = 0;
        foreach (Thing i in grid) {
            if (i.getType() == (int)type.yellow) {
                sum++;
            }
        }
        return sum;
    }
    public int countDamagePieces() {
        int sum = 0;
        foreach (Thing i in grid) {
            if (i.getType() == (int)type.damage) {
                sum++;
            }
        }
        return sum;
    }
    public int countMultiplierPieces() {
        int sum = 0;
        foreach (Thing i in grid) {
            if (i.getType() == (int)type.multiplier) {
                sum++;
            }
        }
        return sum;
    }
    public int countUniqueCornerTypesTimes5() {
        HashSet<int> cornerTypes = new HashSet<int>();
        cornerTypes.Add(grid[0].getType());
        cornerTypes.Add(grid[BOARDLENGTH - 1].getType());
        cornerTypes.Add(grid[BOARDLENGTH * (BOARDLENGTH - 1)].getType());
        cornerTypes.Add(grid[BOARDLENGTH * BOARDLENGTH - 1].getType());
        return cornerTypes.Count * 5;
    }
    public int getLastSpellDestroyedCountTimes5() {
        return lastSpellDestroyedCount * 5;
    }

    private void beginCasting(string actionAndParameters, Spell spell, PlayerController caster, PlayerController target) {
        this.actionAndParameters = actionAndParameters;
        currentSpell = spell;
        currentSpellCaster = caster;
        currentSpellTarget = target;
        selectedSpellTargets.Clear();
        selectedPiece = null;
        isCasting = true;
        targetingIndicator.SetActive(true);
        turnOffSpells();
        makeSplashText("Select a target (Esc/right click to cancel)", isTurn);
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
            if (parameters[0] == "swapTargets") {
                handleSwapTargets(thingController);
                return;
            }

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

                        lastSpellDestroyedCount = positions.Count;
                        toDelete.UnionWith(positions);
                        CompleteTargetedSpellCommand();
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

                    CompleteTargetedSpellCommand();
                    break;
                case ("destroyTargetLShape"):
                    index = getIndex(thingController.gameObject);
                    positions = FindLShapeContaining(index);
                    if (positions.Count == 0) {
                        KeepCastingAfterInvalidTarget("No L shape there. Esc/right click to cancel.");
                        return;
                    }

                    lastSpellDestroyedCount = positions.Count;
                    toDelete.UnionWith(positions);
                    CompleteTargetedSpellCommand();
                    break;
                case ("destroyContiguousChunk"):
                    index = getIndex(thingController.gameObject);
                    positions = FindContiguousChunk(index);
                    if (positions.Count == 0) {
                        KeepCastingAfterInvalidTarget("No valid chunk there. Esc/right click to cancel.");
                        return;
                    }

                    lastSpellDestroyedCount = positions.Count;
                    toDelete.UnionWith(positions);
                    CompleteTargetedSpellCommand();
                    break;
            }
        }
    }

    private void handleSwapTargets(ThingController thingController) {
        int index = getIndex(thingController.gameObject);
        if (index < 0) {
            return;
        }

        if (selectedSpellTargets.Count == 0) {
            selectedSpellTargets.Add(index);
            selectedPiece = thingController;
            selectPiece(index, Color.cyan);
            makeSplashText("Select a second piece", isTurn);
            return;
        }

        int firstIndex = selectedSpellTargets[0];
        if (firstIndex == index) {
            makeSplashText("Select a different piece", isTurn);
            return;
        }

        selectedSpellTargets.Clear();
        actionAndParameters = "";
        targetingIndicator.SetActive(false);

        selectPiece(index, Color.cyan);
        swap(firstIndex, index);

        CompleteTargetedSpellCommand();
    }

    private void CompleteTargetedSpellCommand() {
        Spell spell = currentSpell;
        PlayerController caster = currentSpellCaster;
        PlayerController target = currentSpellTarget;
        List<string> parameters = pendingSpellParameters;
        int nextIndex = pendingSpellParameterIndex;

        ClearCurrentSpellCast();

        if (parameters != null && spell != null && caster != null && target != null) {
            ExecuteSpellParameters(spell, caster, target, parameters, nextIndex);
        }

        if (!isCasting && isTurn && !madeMove) {
            turnOnSpells();
        }
    }

    private void KeepCastingAfterInvalidTarget(string message) {
        isCasting = true;
        targetingIndicator.SetActive(true);
        makeSplashText(message, isTurn);
    }

    private void CancelCurrentSpellCast() {
        if (currentSpell != null && currentSpellCaster != null) {
            for (int i = 0; i < currentSpell.Costs.Length; i++) {
                currentSpellCaster.setMana(i, currentSpellCaster.getMana(i) + currentSpell.Costs[i]);
            }
        }

        ClearCurrentSpellCast();
        turnOnSpells();
        makeSplashText("Spell cancelled", isTurn);
    }

    private void ClearCurrentSpellCast() {
        actionAndParameters = "";
        selectedSpellTargets.Clear();
        selectedPiece = null;
        currentSpell = null;
        currentSpellCaster = null;
        currentSpellTarget = null;
        pendingSpellParameters = null;
        pendingSpellParameterIndex = 0;
        isCasting = false;
        targetingIndicator.SetActive(false);
    }

    private List<int> FindLShapeContaining(int targetIndex) {
        List<int> result = new List<int>();
        if (targetIndex < 0 || targetIndex >= grid.Count) {
            return result;
        }

        int[] firstOffsets = new int[] { BOARDLENGTH, 1, -BOARDLENGTH, -1 };
        int[] secondOffsets = new int[] { 1, -BOARDLENGTH, -1, BOARDLENGTH };

        for (int elbow = 0; elbow < grid.Count; elbow++) {
            int type = grid[elbow].getType();
            if (type < 0) {
                continue;
            }

            for (int i = 0; i < firstOffsets.Length; i++) {
                int first = elbow + firstOffsets[i];
                int second = elbow + secondOffsets[i];
                if (!IsNeighbor(elbow, first, firstOffsets[i]) || !IsNeighbor(elbow, second, secondOffsets[i])) {
                    continue;
                }

                if (grid[first].getType() == type && grid[second].getType() == type) {
                    result = new List<int> { elbow, first, second };
                    if (result.Contains(targetIndex)) {
                        return result;
                    }
                }
            }
        }

        return new List<int>();
    }

    private List<int> FindContiguousChunk(int targetIndex) {
        List<int> chunk = new List<int>();
        if (targetIndex < 0 || targetIndex >= grid.Count) {
            return chunk;
        }

        int type = grid[targetIndex].getType();
        if (type < 0) {
            return chunk;
        }

        Queue<int> frontier = new Queue<int>();
        HashSet<int> visited = new HashSet<int>();
        frontier.Enqueue(targetIndex);
        visited.Add(targetIndex);

        while (frontier.Count > 0) {
            int current = frontier.Dequeue();
            chunk.Add(current);

            foreach (int offset in new int[] { BOARDLENGTH, -BOARDLENGTH, 1, -1 }) {
                int neighbor = current + offset;
                if (visited.Contains(neighbor) || !IsNeighbor(current, neighbor, offset)) {
                    continue;
                }

                if (grid[neighbor].getType() == type) {
                    visited.Add(neighbor);
                    frontier.Enqueue(neighbor);
                }
            }
        }

        return chunk;
    }

    private bool IsNeighbor(int origin, int candidate, int offset) {
        if (candidate < 0 || candidate >= BOARDLENGTH * BOARDLENGTH) {
            return false;
        }

        if (offset == 1) {
            return origin % BOARDLENGTH != BOARDLENGTH - 1;
        }
        if (offset == -1) {
            return origin % BOARDLENGTH != 0;
        }

        return true;
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
                        grid[j].getObject().GetComponent<ThingController>().setSpeed(5f);
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

            grid[i].getObject().GetComponent<ThingController>().setSpeed(40f);
            
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
                setMultiplier(damageMultiplier + real);
                makeSplashText("Multiplier +" + real, isTurn, Color.white);
                playManaSound(5);
            }
            else if(damageMultiplier + weapon.manaEffect(5, 0.2f * typeCounts[5]) > maxMultiplier) {
                makeSplashText("Multiplier +" + (maxMultiplier - damageMultiplier), isTurn, Color.white);
                setMultiplier(maxMultiplier);
                playManaSound(5);
            }
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
        return CreateMatchBoard().FindMovesAtPoint(i);
    }

    private MatchBoard CreateMatchBoard() {
        List<int> types = new List<int>(grid.Count);
        foreach (Thing thing in grid) {
            types.Add(thing.getType());
        }
        return new MatchBoard(types, BOARDLENGTH);
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
        if (isCasting && (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))) {
            CancelCurrentSpellCast();
            return;
        }

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
