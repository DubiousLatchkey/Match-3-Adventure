public class Enemy {

    public string Name;

    public float failRate;

    public int maxRedMana;
    public int maxBlueMana;
    public int maxYellowMana;

    public int[] Priorities = new int[3];

    public Spell[] Spells;

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
