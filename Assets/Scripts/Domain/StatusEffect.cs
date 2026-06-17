using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
