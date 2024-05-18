using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EnemyController : MonoBehaviour, PlayerController
{
    public static bool madeMove;

    public const int rounds = 1;
    public int currentRound = 0;
    public float delayTime = 0.4f;

    Enemy enemy;
    List<Button> equippedSpells;
    List<int> priorities;
    List<Action> availableActions;

    int redMana;
    int blueMana;
    int yellowMana;
    int currentHealth;
    public float damageMultiplier = 1;
    bool doingTurn;
    public Weapon weapon;
    GameObject weaponObject;
    int averageRedManaCost;
    int averageBlueManaCost;
    int averageYellowManaCost;

    Text redManaTextBox;
    Text blueManaTextBox;
    Text yellowManaTextBox;
    Text hp;
    Text multiplier;
    Text name;
    public Combat combat;

    BarHandler healthBar;
    BarHandler redBar;
    BarHandler blueBar;
    BarHandler yellowBar;
    BarHandler multiplierBar;

    public Image portrait;

    List<StatusEffect> statusEffects;
    Etcetera extraStatusEffects;

    // Start is called before the first frame update
    void Start()
    {
        SpellContainer spells = SpellContainer.Load(Path.Combine(Application.persistentDataPath, "spells.xml"));

        //Make enemy data
        EnemyContainer enemies = new EnemyContainer();
        enemies.enemies[0] = new Enemy("Wizard Adept", new int[] {5, 2, 10 }, new Spell[] { spells.spells[0], spells.spells[2]}, 100, 10, 10, 10, 0.5f);
        enemies.enemies[1] = new Enemy("Wizard Superior", new int[] { 6, 2, 7 }, new Spell[] { spells.spells[0], spells.spells[1], spells.spells[2] }, 150, 12, 12, 12, 0.2f);
        enemies.enemies[2] = new Enemy("Deranged Hermit", new int[] { 10, 0, 10 }, new Spell[] { spells.spells[1], spells.spells[2] }, 150, 8, 8, 8, 0.7f);
        enemies.enemies[3] = new Enemy("Arcane Elemental", new int[] { 10, 5, 5 }, new Spell[] { spells.spells[0]}, 50, 8, 5, 5, 0.7f);
        enemies.enemies[4] = new Enemy("Raider", new int[] { 5, 5, 5 }, new Spell[] {  }, 50, 0, 0, 0, 0.5f);
        enemies.enemies[5] = new Enemy("Exchange Thief", new int[] { 3, 10, 7 }, new Spell[] { spells.spells[12], spells.spells[20] }, 80, 5, 8, 5, 0.7f);
        enemies.enemies[6] = new Enemy("Servant of Necromancy", new int[] { 3, 10, 3 }, new Spell[] { spells.spells[2], spells.spells[12] }, 80, 8, 10, 8, 0.5f);
        enemies.enemies[7] = new Enemy("Ephran, Exchange Head", new int[] { 10, 3, 8 }, new Spell[] { spells.spells[30], spells.spells[31] }, 100, 10, 10, 10, 0.3f);
        enemies.enemies[8] = new Enemy("City Guard", new int[] { 7, 10, 3 }, new Spell[] { spells.spells[15] }, 80, 9, 9, 9, 0.4f);
        enemies.enemies[9] = new Enemy("Skeleton", new int[] { 10, 3, 7 }, new Spell[] { spells.spells[6] }, 80, 10, 10, 10, 0.5f);
        enemies.enemies[10] = new Enemy("Atheria's Guard", new int[] { 10, 3, 7 }, new Spell[] { spells.spells[12], spells.spells[10] }, 80, 10, 10, 10, 0.25f);
        enemies.enemies[11] = new Enemy("Atheria, Necromancer General", new int[] { 10, 3, 7 }, new Spell[] { spells.spells[10], spells.spells[8], spells.spells[1] }, 150, 15, 15, 15, 0.05f);
        enemies.enemies[12] = new Enemy("Roxanne, Atheria's Right Hand", new int[] { 10, 3, 7 }, new Spell[] { spells.spells[10] }, 100, 10, 10, 10, 0.1f);
        enemies.enemies[13] = new Enemy("Atherian Mage", new int[] { 10, 3, 7 }, new Spell[] { spells.spells[0] }, 100, 10, 10, 10, 0.2f);
        enemies.enemies[14] = new Enemy("Earlygame Enemy", new int[] { 5, 10, 5 }, new Spell[] { spells.spells[55] }, 30, 15, 15, 15, 0.4f);

        enemies.Save(Path.Combine(Application.persistentDataPath, "enemies.xml"));

        combat = new Combat(GridController.combatToLoad);
        if (!combat.getIsSolo()) {
            enemy = EnemyContainer.Load(Path.Combine(Application.persistentDataPath, "enemies.xml")).enemies[combat.getEnemy(0)];
        }
        else {
            List<string> soloArguments = new List<string>(combat.getSoloArguments().Split(' '));
            enemy = new Enemy(string.Join(" ", soloArguments.GetRange(2, soloArguments.Count - 2)).Trim(), new int[] { 0, 0, 0 }, new Spell[0], int.Parse(soloArguments[1]), 0, 0, 0, 0);
        }
        //Initialize
        doingTurn = false;
        equippedSpells = new List<Button>();
        madeMove = false;
        redMana = 0;
        blueMana = 0;
        yellowMana = 0;
        currentHealth = enemy.Health;
        availableActions = new List<Action>();

        statusEffects = new List<StatusEffect>();
        extraStatusEffects = new Etcetera(null, false);
        foreach (Text i in GameObject.FindObjectsOfType<Text>()) {
            switch (i.name) {
                case ("enemyRedMana"):
                    redManaTextBox = i;
                    //Debug.Log("Found red");
                    break;
                case ("enemyBlueMana"):
                    //Debug.Log("Found blue");
                    blueManaTextBox = i;
                    break;
                case ("enemyYellowMana"):
                    yellowManaTextBox = i;
                    //Debug.Log("Found yellow");
                    break;
                case ("enemyHp"):
                    hp = i;
                    hp.text = enemy.Health.ToString() + " out of " + enemy.Health.ToString();
                    break;
                case ("enemyMultiplier"):
                    multiplier = i;
                    break;
                case ("enemyName"):
                    name = i;
                    name.text = enemy.Name + " (" + (currentRound + 1) + "/" + combat.getTotalRounds().ToString() + ")";
                    break;
                default:
                    break;
            }
        }

        foreach (Image i in FindObjectsOfType<Image>()) {
            switch (i.name) {
                case ("enemyHealthBarFilled"):
                    healthBar = i.GetComponent<BarHandler>();
                    break;
                case ("enemyRedBarFilled"):
                    redBar = i.GetComponent<BarHandler>();
                    break;
                case ("enemyBlueBarFilled"):
                    blueBar = i.GetComponent<BarHandler>();
                    break;
                case ("enemyYellowBarFilled"):
                    yellowBar = i.GetComponent<BarHandler>();
                    break;
                case ("enemyMultiplierBarFilled"):
                    multiplierBar = i.GetComponent<BarHandler>();
                    break;

            }
        }
        redBar.setInitialPercentageFilled(0);
        blueBar.setInitialPercentageFilled(0);
        yellowBar.setInitialPercentageFilled(0);
        multiplierBar.setInitialPercentageFilled((damageMultiplier - 1) / (GridController.maxMultiplier - 1f));

        setRedMana(0);
        setBlueMana(0);
        setYellowMana(0);

        if (combat.getPortrait(combat.getRound()) != "") {
            portrait.sprite = Resources.Load<Sprite>("Characters/" + combat.getPortrait(combat.getRound()));
        }
        else {
            portrait.sprite = Resources.Load<Sprite>("Characters/" + enemy.Name + "Normal");
        }


        //SpellContainer spells = SpellContainer.Load(Path.Combine(Application.persistentDataPath, "spells.xml"));

        /*
        for (int i = 0; i < spells.spells.Length; i++) {
            //Debug.Log(i.Effects[0] + " " + i.Costs[0]);
            if (enemy.Spells[i]) {
                equippedSpells.Add(Instantiate(Resources.Load<Button>("SpellButton"), GameObject.Find("Canvas").transform));
                equippedSpells[equippedSpells.Count - 1].GetComponent<SpellButtonHandler>().setSpell(spells.spells[i]);
            }
        }
        */
        for (int i = 0; i < enemy.Spells.Length; i++) {
            //equippedSpells[i].transform.SetParent(GameObject.Find("Canvas").transform);
            equippedSpells.Add(Instantiate(Resources.Load<Button>("SpellButton"), GameObject.Find("enemyPaperback").transform));
            equippedSpells[equippedSpells.Count - 1].GetComponent<SpellButtonHandler>().setSpell(enemy.Spells[i]);
            //equippedSpells[i].transform.localScale = new Vector3(0.02f, 0.02f);
            equippedSpells[i].transform.localPosition = new Vector3(0, -i, 0);
            equippedSpells[i].gameObject.tag = "enemy";
            equippedSpells[i].interactable = false;
        }

        int spellCount = 0; 
        foreach(Button i in equippedSpells) {
            averageRedManaCost += i.gameObject.GetComponent<SpellButtonHandler>().getSpell().Costs[0];
            averageBlueManaCost += i.gameObject.GetComponent<SpellButtonHandler>().getSpell().Costs[1];
            averageYellowManaCost += i.gameObject.GetComponent<SpellButtonHandler>().getSpell().Costs[2];
            spellCount++;
        }
        if (spellCount == 0) { spellCount++; }
        averageRedManaCost /= spellCount;
        averageBlueManaCost /= spellCount;
        averageYellowManaCost /= spellCount;

        //Get weapon
        weapon = new Weapon();
        WeaponContainer weapons = WeaponContainer.Load(Path.Combine(Application.persistentDataPath, "weapons.xml"));
        if (combat.getWeapon(0) != -1) {
            weapon = weapons.Weapons[combat.getWeapon(0)];
        }
        weaponObject = GameObject.Find("enemyWeapon");
        if (weapon.Type != WeaponType.none) {
            weaponObject.SetActive(true);
            weaponObject.GetComponent<WeaponButtonHandler>().setWeapon(weapon);
        }
        else {
            weaponObject.GetComponent<Image>().sprite = Resources.Load<Sprite>("Button");
            weaponObject.SetActive(false);
        }

    }

    public void handleStatusEffects() {
        /*
        foreach (EffectEveryTurn i in effectsEveryTurn) {
            i.performEffect();
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

    public void addStatusEffect(StatusEffect effect) {
        statusEffects.Add(effect);
        statusEffects.Sort((a, b) => a.getTurns().CompareTo(b.getTurns()));
        displayStatusEffects();
    }

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
            statusEffects[i].getIndicator().transform.SetParent(GameObject.Find("enemyPortrait").transform, false);
            //statusEffects[i].getIndicator().transform.localScale = Vector3.one;
            statusEffects[i].getIndicator().transform.localPosition = new Vector3(90, 45f - i * 30f);

        }
        //Put the rest in etcetera
        if (statusEffects.Count > 3) {
            extraStatusEffects = new Etcetera(Instantiate(Resources.Load<GameObject>("StatusEffectIndicator"), GameObject.Find("enemyPortrait").transform), false);
            extraStatusEffects.getIndicator().GetComponent<Image>().sprite = Resources.Load<Sprite>("dotdotdot");

            extraStatusEffects.getIndicator().transform.localPosition = new Vector3(90, -45f);

            for (int i = 3; i < statusEffects.Count; i++) {
                extraStatusEffects.enqueue(statusEffects[i]);
            }
            extraStatusEffects.updateIndicator();
        }
    }

    public List<StatusEffect> GetStatusEffects() {
        return statusEffects;
    }

    /*
    public void score(int type) {
        switch (type) {
            case (0):
                if (redMana + 1 <= enemy.maxRedMana) {
                    redMana += 1;
                    redManaTextBox.text = redMana.ToString();
                    redBar.setPercentageFilled(redMana / (enemy.maxRedMana + 0f));
                }
                break;
            case (1):
                if (blueMana + 1 <= enemy.maxBlueMana) {
                    blueMana += 1;
                    blueBar.setPercentageFilled(blueMana / (enemy.maxBlueMana + 0f));
                    blueManaTextBox.text = blueMana.ToString();
                }
                break;
            case (2):
                if (yellowMana + 1 <= enemy.maxYellowMana) {
                    yellowMana += 1;
                    yellowBar.setPercentageFilled(yellowMana / (enemy.maxYellowMana + 0f));
                    yellowManaTextBox.text = yellowMana.ToString();
                }
                break;
            case (3):
                gameObject.GetComponent<GridController>().dealDamage(2);
                break;
            case (4):
                dealDamage(-2);
                break;
            case (5):
                if (damageMultiplier + 0.5f <= GridController.maxMultiplier) {
                    damageMultiplier += 0.5f;
                }
                else {
                    damageMultiplier = GridController.maxMultiplier;
                }
                multiplier.text = damageMultiplier.ToString() + "x";
                multiplierBar.setPercentageFilled((damageMultiplier - 1) / (GridController.maxMultiplier - 1f));
                break;
        }
    }
    */
    public void scoreByAmount(Dictionary<int, int> typeCounts) {
        if (typeCounts[0] != 0) {
            int real = (int)weapon.manaEffect(0, redMana + typeCounts[0]);
            string amount = real > 0 ? "+" + (real - redMana) : real.ToString();
            setRedMana(real);
            GridController.makeManaSplashText(amount, GridController.isTurn, 0);
            gameObject.GetComponent<GridController>().playManaSound(0);
        }
        if (typeCounts[1] != 0) {
            int real = (int)weapon.manaEffect(1, blueMana + typeCounts[1]);
            string amount = real > 0 ? "+" + (real - blueMana) : real.ToString();
            setBlueMana(real);
            GridController.makeManaSplashText(amount, GridController.isTurn, 1);
            gameObject.GetComponent<GridController>().playManaSound(1);
        }
        if (typeCounts[2] != 0) {
            int real = (int)weapon.manaEffect(2, yellowMana + typeCounts[2]);
            string amount = real > 0 ? "+" + (real - yellowMana) : real.ToString();
            setYellowMana(real);
            GridController.makeManaSplashText(amount, GridController.isTurn, 2);
            gameObject.GetComponent<GridController>().playManaSound(2);
        }
        if (typeCounts[3] != 0) {
            int real = gameObject.GetComponent<GridController>().applyModifiersToDamageDone(typeCounts[3], DamageType.matchDamage);
            gameObject.GetComponent<GridController>().dealDamage(real);
            GridController.makeSplashText(real + " Damage", !GridController.isTurn, Color.white);
            gameObject.GetComponent<GridController>().playManaSound(3);
        }
        if (typeCounts[4] != 0) {
            int real = applyModifiersToDamageDone(-typeCounts[4], DamageType.matchDamage);
            dealDamage(real);
            GridController.makeSplashText("Healed for " +Mathf.Abs(real), GridController.isTurn, Color.white);
            gameObject.GetComponent<GridController>().playManaSound(4);
        }
        if (typeCounts[5] != 0 && damageMultiplier + weapon.manaEffect(5, 0.2f * typeCounts[5]) <= GridController.maxMultiplier) {
            float real = weapon.manaEffect(5, 0.2f * typeCounts[5]);
            damageMultiplier += real;
            GridController.makeSplashText("Multiplier +" + real, GridController.isTurn, Color.white);
            gameObject.GetComponent<GridController>().playManaSound(5);
        }
        else if(damageMultiplier + weapon.manaEffect(5, 0.2f * typeCounts[5]) > GridController.maxMultiplier) {
            GridController.makeSplashText("Multiplier +" + (GridController.maxMultiplier - damageMultiplier), GridController.isTurn, Color.white);
            damageMultiplier = GridController.maxMultiplier;
            gameObject.GetComponent<GridController>().playManaSound(5);
        }
        multiplier.text = damageMultiplier.ToString() + "x";
        multiplierBar.setPercentageFilled((damageMultiplier - 1) / (GridController.maxMultiplier - 1f));
    }

    // Update is called once per frame
    void Update()
    {
        if (!GridController.transitioning && !GridController.isTurn && !madeMove && gameObject.GetComponent<GridController>().isValidMoveTime()) {
            if (!doingTurn) {
                doingTurn = true;
                //Pass turn immediately if is a solo fight
                if (combat.getIsSolo()) {
                    madeMove = true;
                    doingTurn = false;
                    return;
                }
                //Otherwise do turn
                StartCoroutine(doAIDelay());
            }
        }
    }

    IEnumerator doAIDelay() {
        yield return new WaitForSecondsRealtime(delayTime);
        delayTime = 0.4f;
        doTurn();
        doingTurn = false;
    }

    private void doTurn() {
        //Get swaps
        List<Move> moves = new List<Move>();

        for (int i = 0; i < GridController.BOARDLENGTH * GridController.BOARDLENGTH; i++) {
            moves.AddRange(gameObject.GetComponent<GridController>().movesAtPoint(i));
        }

        //Evaluate Spells
        List<Spell> castableSpells = new List<Spell>();
        foreach (Button i in equippedSpells) {
            if (checkCosts(i.GetComponent<SpellButtonHandler>().getSpell().Costs)) {
                castableSpells.Add(i.GetComponent<SpellButtonHandler>().getSpell());
            }
        }

        //Make actions list
        availableActions.AddRange(moves);
        /*string moveText = "";
        foreach (Move i in availableActions) {
            moveText += "[" + i.originLocation + "," + i.swapLocation + "] ";
        }
        Debug.Log(moveText);*/
        foreach (Spell i in castableSpells) {
            availableActions.Add(new spellAction(i));
        }

        //Evaluate actions
        foreach (Action i in availableActions) {
            evaluateAction(i);
        }

        //Random Check
        if (Random.Range(0f, 1f) > enemy.failRate) {
            //Make good move
            availableActions.Sort((a, b) => b.getPriority().CompareTo(a.getPriority()));
            int numberOfMovesWithHighestPriority = 0;
            foreach (Action i in availableActions) {
                if (i.getPriority() == availableActions[0].getPriority()) {
                    numberOfMovesWithHighestPriority++;
                }
            }
            Debug.Log("Making good move");
            executeAction(availableActions[(int)Random.Range(0f, numberOfMovesWithHighestPriority - 0.0001f)]);

        }
        else {
            //Make random move
            Debug.Log("Making random move");
            executeAction(availableActions[(int)Random.Range(0f, availableActions.Count - 0.0001f)]);
        }

        /*
        //Cast spell with highest CMC
        if (castableSpells.Count > 0) {
            castableSpells.Sort((a, b) => (b.getConvertedManaCost().CompareTo(a.getConvertedManaCost())));
            cast(castableSpells[0]);
            //gameObject.GetComponent<GridController>().checkForMatches();
            return;
        }

        //Sort moves by priority if passes random check
        if (UnityEngine.Random.Range(0f, 1f) > enemy.failRate) {
            Debug.Log("Making good move");
            setPriorities();
            //foreach (int i in priorities) { Debug.Log(i); }
            moves.Sort((a, b) => priorities[b.type].CompareTo(priorities[a.type]));
        }
        else {
            //If failed random check
            Debug.Log("Making non-optimal move");
            int randomMove = (int)UnityEngine.Random.Range(0f, moves.Count - 0.0001f);
            gameObject.GetComponent<GridController>().GetThing(moves[randomMove].originLocation).getObject().GetComponent<ThingController>().speed = 1.5f;
            gameObject.GetComponent<GridController>().GetThing(moves[randomMove].swapLocation).getObject().GetComponent<ThingController>().speed = 1.5f;
            gameObject.GetComponent<GridController>().swap(moves[randomMove].originLocation, moves[randomMove].swapLocation);
            gameObject.GetComponent<GridController>().selectPiece(moves[randomMove].originLocation);
            gameObject.GetComponent<GridController>().selectPiece(moves[randomMove].swapLocation);
            Debug.Log(moves[randomMove].originLocation + " " + moves[randomMove].swapLocation);
            madeMove = true;
            return;
        }

        //Make good move
        foreach (Move i in moves) {
            if (i.matchLength == 5) {
                gameObject.GetComponent<GridController>().GetThing(i.originLocation).getObject().GetComponent<ThingController>().speed = 1.5f;
                gameObject.GetComponent<GridController>().GetThing(i.swapLocation).getObject().GetComponent<ThingController>().speed = 1.5f;
                gameObject.GetComponent<GridController>().swap(i.originLocation, i.swapLocation);
                gameObject.GetComponent<GridController>().selectPiece(i.originLocation);
                gameObject.GetComponent<GridController>().selectPiece(i.swapLocation);
                Debug.Log("Five:" + i.originLocation + " " + i.swapLocation);
                madeMove = true;
                return;
            }
            else if (i.matchLength == 4) {
                gameObject.GetComponent<GridController>().GetThing(i.originLocation).getObject().GetComponent<ThingController>().speed = 1.5f;
                gameObject.GetComponent<GridController>().GetThing(i.swapLocation).getObject().GetComponent<ThingController>().speed = 1.5f;
                gameObject.GetComponent<GridController>().swap(i.originLocation, i.swapLocation);
                gameObject.GetComponent<GridController>().selectPiece(i.originLocation);
                gameObject.GetComponent<GridController>().selectPiece(i.swapLocation);
                Debug.Log("Four: " + i.originLocation + " " + i.swapLocation);
                madeMove = true;
                return;
            }

        }

        if (moves.Count == 0) {
            madeMove = true;
            return;
        }

        gameObject.GetComponent<GridController>().GetThing(moves[0].originLocation).getObject().GetComponent<ThingController>().speed = 2f;
        gameObject.GetComponent<GridController>().GetThing(moves[0].swapLocation).getObject().GetComponent<ThingController>().speed = 2f;
        gameObject.GetComponent<GridController>().swap(moves[0].originLocation, moves[0].swapLocation);
        gameObject.GetComponent<GridController>().selectPiece(moves[0].originLocation);
        gameObject.GetComponent<GridController>().selectPiece(moves[0].swapLocation);
        Debug.Log(moves[0].originLocation + " " + moves[0].swapLocation);


        //Pass turn
        madeMove = true;
        */
    }

    private void evaluateAction(Action action) {
        int runningPrioritySum = 0;
        if (action is Move a) {
            //Evaluate Move

            //4+ in a rows get priority
            if (a.matchLength > 3) {
                runningPrioritySum += 2;
            }

            //Different evaluations based on type
            switch (a.type) {
                case (int)colorType.red:
                    if (averageRedManaCost > redMana) { runningPrioritySum++; }
                    else if (redMana == enemy.maxRedMana) { runningPrioritySum--; }
                    break;
                case (int)colorType.blue:
                    if (averageBlueManaCost > blueMana) { runningPrioritySum++; }
                    else if (blueMana == enemy.maxBlueMana) { runningPrioritySum--; }
                    break;
                case (int)colorType.yellow:
                    if (averageYellowManaCost > yellowMana) { runningPrioritySum++; }
                    else if (yellowMana == enemy.maxYellowMana) { runningPrioritySum--; }
                    break;
                case (int)colorType.damage:
                    //Make the killing blow
                    if(2 * a.matchLength >= gameObject.GetComponent<GridController>().getHealth()) { runningPrioritySum += 3; }
                    runningPrioritySum += 2;
                    break;
                case (int)colorType.health:
                    if (currentHealth < 0.2 * enemy.Health) { runningPrioritySum++; }
                    break;
                case (int)colorType.multiplier:
                    if (damageMultiplier >= 1.9) { runningPrioritySum -= 2; }
                    runningPrioritySum++;
                    break;
            }

        }
        else if (action is spellAction s) {
            //Evaluate spellAction
            switch (s.getSpell().Type) {
                case (spellType.Damage):
                    runningPrioritySum += 3;
                    break;
                case (spellType.Healing):
                    if (currentHealth < 0.8 * enemy.Health) {
                        runningPrioritySum += 2;
                    }
                    
                    break;
                default:
                    runningPrioritySum += 2;
                    break;
            }
        }

        action.setPriority(runningPrioritySum);
    }

    private void executeAction(Action action) {
        Debug.Log("Making move with priority: " + action.getPriority());
        if (action is spellAction spellAction) {
            cast(spellAction.getSpell());
            delayTime = 0.6f;
        }
        else {
            Move move = (Move)action;
            gameObject.GetComponent<GridController>().GetThing(move.originLocation).getObject().GetComponent<ThingController>().setSpeed(2f);
            gameObject.GetComponent<GridController>().GetThing(move.swapLocation).getObject().GetComponent<ThingController>().setSpeed(2f);
            gameObject.GetComponent<GridController>().swap(move.originLocation, move.swapLocation);
            gameObject.GetComponent<GridController>().selectPiece(move.originLocation);
            gameObject.GetComponent<GridController>().selectPiece(move.swapLocation);
            Debug.Log(move.originLocation + " " + move.swapLocation);
            madeMove = true;
            delayTime = 0.4f;
        }
        availableActions.Clear();

    }

    //All done damage goes through here
    public int applyModifiersToDamageDone(int amount, DamageType damageType) {
        GridController gridController = gameObject.GetComponent<GridController>();

        if (damageType == DamageType.spellDamage) {
            //Apply weapon effects
            amount = gridController.weapon.spellDamageEffect() + amount;
            //May have defensive weapons later

            //Apply status effects
            //Player effects
            foreach (StatusEffect i in gridController.GetStatusEffects()) {
                if (i is DamageIncreaseEffect) {
                    amount = ((DamageIncreaseEffect)i).modifyDamage(amount);
                }
                else if (i is SpellDamageIncreaseEffect) {
                    amount = ((SpellDamageIncreaseEffect)i).modifyDamage(amount);
                }
            }
            //Enemy effects
            foreach (StatusEffect i in statusEffects) {
                if (i is DamageReductionEffect) {
                    amount = ((DamageReductionEffect)i).modifyDamage(amount);
                }
            }
            return (int)(amount * gridController.damageMultiplier);
        }
        else if (damageType == DamageType.matchDamage) {
            //May apply nonlinear formula to damage
            //Weapon effects
            amount = (int)gridController.weapon.manaEffect(3, 2 * amount);
            //May have defensive weapons later

            //Apply status effects
            //Player effects
            foreach (StatusEffect i in gridController.GetStatusEffects()) {
                if (i is DamageIncreaseEffect) {
                    amount = ((DamageIncreaseEffect)i).modifyDamage(amount);
                }
            }
            //Enemy effects
            foreach (StatusEffect i in statusEffects) {
                if (i is DamageReductionEffect) {
                    amount = ((DamageReductionEffect)i).modifyDamage(amount);
                }
            }
            return (int)(amount * gridController.damageMultiplier);
        }
        else if (damageType == DamageType.statusEffectDamage) {
            //Weapon effects

            //May have defensive weapons later

            //Apply status effects (Will have more later)
            //Player effects
            //Enemy effects

            return amount;
        }
        return amount;
    }

    public void dealDamage(int amount) {
        if (currentHealth - amount <= enemy.Health) {
            currentHealth -= amount;
            if (currentHealth <= 0) {
                hp.text = "0" + " out of " + enemy.Health.ToString();
                healthBar.setPercentageFilled(0);
                currentHealth = 0;

                //win
                Debug.Log("You win");

                currentRound++;
                combat.advanceRound();
                int next = combat.getEnemy(combat.getRound());

                //loadEnemy();
                if (next == -1) {
                    //Send to new scene
                    DialogueController.dialogueToLoad = combat.getNextDialogue();
                    resultsScreenHandler.setWin();
                    resultsScreenHandler.setDone();
                    ScriptController.pauseGame();
                }
                else {
                    //StartCoroutine(resultsBackground.GetComponent<resultsScreenHandler>().fadeOut());
                    //Send control to script controller
                    resultsScreenHandler.setNext();
                    ScriptController.pauseGame();

                }
                

                
                
            }
        }
        else {
            currentHealth = enemy.Health;
        }
        hp.text = currentHealth.ToString() + " out of " + enemy.Health.ToString();
        healthBar.setPercentageFilled(currentHealth / (enemy.Health + 0f));
    }

    public int processDamage(int preProcessedDamage) {
        //Do status effect damage processing
        foreach (StatusEffect i in statusEffects) {
            if (i is DamageReductionEffect) {
                preProcessedDamage = ((DamageReductionEffect)i).modifyDamage(preProcessedDamage);
            }
        }
        return preProcessedDamage;
    }

    public GameObject getPortrait() {
        return GameObject.Find("enemyPortraitImage");
    }

    public void loadEnemy() {
        //Load next enemy
        enemy = EnemyContainer.Load(Path.Combine(Application.persistentDataPath, "enemies.xml")).enemies[combat.getEnemy(currentRound)];

        madeMove = false;
        redMana = 0;
        blueMana = 0;
        yellowMana = 0;
        currentHealth = enemy.Health;
        damageMultiplier = 1;

        healthBar.setInitialPercentageFilled(currentHealth / (enemy.Health + 0f));
        redBar.setInitialPercentageFilled(redMana / (enemy.maxRedMana + 0f));
        blueBar.setInitialPercentageFilled(blueMana / (enemy.maxBlueMana + 0f));
        yellowBar.setInitialPercentageFilled(yellowMana / (enemy.maxYellowMana + 0f));
        multiplierBar.setInitialPercentageFilled((damageMultiplier - 1) / (GridController.maxMultiplier - 1f));
        redManaTextBox.text = redMana.ToString() + "/" + enemy.maxRedMana.ToString();
        blueManaTextBox.text = blueMana.ToString() + "/" + enemy.maxBlueMana.ToString();
        yellowManaTextBox.text = yellowMana.ToString() + "/" + enemy.maxYellowMana.ToString();
        multiplier.text = damageMultiplier.ToString() + "x";
        name.text = enemy.Name + " (" + (currentRound + 1) + "/" + combat.getTotalRounds().ToString() + ")";
        hp.text = currentHealth.ToString() + " out of " + enemy.Health.ToString();

        if (combat.getPortrait(combat.getRound()) != "") {
            portrait.sprite = Resources.Load<Sprite>("Characters/" + combat.getPortrait(combat.getRound()));
        }
        else {
            portrait.sprite = Resources.Load<Sprite>("Characters/" + enemy.Name + "Normal");
        }

        SpellContainer spells = SpellContainer.Load(Path.Combine(Application.persistentDataPath, "spells.xml"));
        foreach (Button i in equippedSpells) {
            Destroy(i.gameObject);
        }
        equippedSpells.Clear();
        /*
        for (int i = 0; i < spells.spells.Length; i++) {
            //Debug.Log(i.Effects[0] + " " + i.Costs[0]);
            if (enemy.Spells[i]) {
                equippedSpells.Add(Instantiate(Resources.Load<Button>("SpellButton"), GameObject.Find("Canvas").transform));
                equippedSpells[equippedSpells.Count - 1].GetComponent<SpellButtonHandler>().setSpell(spells.spells[i]);
            }
        }
        */
        for (int i = 0; i < enemy.Spells.Length; i++) {
            //equippedSpells[i].transform.SetParent(GameObject.Find("Canvas").transform);
            equippedSpells.Add(Instantiate(Resources.Load<Button>("SpellButton"), GameObject.Find("enemyPaperback").transform));
            equippedSpells[equippedSpells.Count - 1].GetComponent<SpellButtonHandler>().setSpell(enemy.Spells[i]);
            //equippedSpells[i].transform.localScale = new Vector3(0.02f, 0.02f);
            equippedSpells[i].transform.localPosition = new Vector3(0, -i, 0);
            equippedSpells[i].gameObject.tag = "enemy";
            equippedSpells[i].interactable = false;
        }

        checkAllCosts();

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
        if (combat.getWeapon(currentRound) != -1) {
            weapon = weapons.Weapons[combat.getWeapon(currentRound)];
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

    public void checkAllCosts() {
        foreach (Button i in equippedSpells) {
            bool canCast = checkCosts(i.GetComponent<SpellButtonHandler>().getSpell().Costs);
            if (i.GetComponent<Image>().sprite.name == "Button" && canCast) {
                i.GetComponent<Image>().sprite = Resources.Load<Sprite>("castableButton");
                //if (isTurn) { i.interactable = true; }
            }
            else if (i.GetComponent<Image>().sprite.name == "castableButton" && !canCast) {
                i.GetComponent<Image>().sprite = Resources.Load<Sprite>("Button");
                //i.interactable = false;
            }
            //if (!isTurn) {
            //    i.interactable = false; 
            //}
        }
    }

    public void cast(Spell spell) {
        /*
        setRedMana(redMana - spell.Costs[0]);
        setBlueMana(blueMana - spell.Costs[1]);
        setYellowMana(yellowMana - spell.Costs[2]);

        GridController.makeSplashText("Casting " + spell.Name, false);
        */
        weapon.castEffect(false);

        gameObject.GetComponent<GridController>().castSpell(spell, this, gameObject.GetComponent<GridController>());
    }

    public void setPriorities() {
        priorities = new List<int>();
        //Get totals in spells
        int totalRedCosts = 0;
        int totalBlueCosts = 0;
        int totalYellowCosts = 0;
        foreach (Button i in equippedSpells) {
            totalRedCosts += i.GetComponent<SpellButtonHandler>().getSpell().Costs[0];
            totalBlueCosts += i.GetComponent<SpellButtonHandler>().getSpell().Costs[1];
            totalYellowCosts += i.GetComponent<SpellButtonHandler>().getSpell().Costs[2];

        }
        priorities.Add(totalRedCosts - redMana);
        priorities.Add(totalBlueCosts - blueMana);
        priorities.Add(totalYellowCosts - yellowMana);
        priorities.Add(enemy.Priorities[0]);
        priorities.Add(enemy.Priorities[1]);
        if (damageMultiplier < GridController.maxMultiplier) {
            priorities.Add(enemy.Priorities[2]);
        }
        else {
            priorities.Add(-10);
        }
    }

    public void decrementMultiplier() {
        if (damageMultiplier > 1f) {
            damageMultiplier -= 0.1f;
            damageMultiplier = (float)System.Math.Round(damageMultiplier, 1);
        }
        else {
            damageMultiplier = 1;
        }
        multiplierBar.setPercentageFilled((damageMultiplier - 1) / (GridController.maxMultiplier - 1.0f));
        multiplier.text = damageMultiplier.ToString() + "x";
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

    public int getRedMana() {
        return redMana;
    }
    public int getBlueMana() {
        return blueMana;
    }
    public int getYellowMana() {
        return yellowMana;
    }
    public void setYellowMana(int mana) {
        //Debug.Log(mana + " " +enemy.maxYellowMana + " " +(mana > enemy.maxYellowMana));

        yellowMana = mana > enemy.maxYellowMana ? enemy.maxYellowMana : mana;
        yellowMana = yellowMana >= 0 ? yellowMana : 0;
        yellowBar.setPercentageFilled(yellowMana / (enemy.maxYellowMana + 0f));
        yellowManaTextBox.text = yellowMana.ToString() + "/" + enemy.maxYellowMana.ToString();
    }
    public void setBlueMana(int mana) {
        blueMana = mana > enemy.maxBlueMana ? enemy.maxBlueMana : mana;
        blueMana = blueMana >= 0 ? blueMana : 0;
        blueBar.setPercentageFilled(blueMana / (enemy.maxBlueMana + 0f));
        blueManaTextBox.text = blueMana.ToString() + "/" + enemy.maxBlueMana.ToString();
    }
    public void setRedMana(int mana) {
        redMana = mana > enemy.maxRedMana ? enemy.maxRedMana : mana;
        redMana = redMana >= 0 ? redMana : 0;
        redBar.setPercentageFilled(redMana / (enemy.maxRedMana + 0f));
        redManaTextBox.text = redMana.ToString() + "/" + enemy.maxRedMana.ToString();
    }

    public void setMultiplier(float amount) {
        amount = amount > GridController.maxMultiplier ? GridController.maxMultiplier : amount;
        if (amount >= 1) {
            damageMultiplier = amount;
            multiplier.text = damageMultiplier.ToString() + "x";
            multiplierBar.setPercentageFilled((damageMultiplier - 1) / (GridController.maxMultiplier - 1f));
        }
    }
}

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
