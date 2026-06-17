using UnityEngine;

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
