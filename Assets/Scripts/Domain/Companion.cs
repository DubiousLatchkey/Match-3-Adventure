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
