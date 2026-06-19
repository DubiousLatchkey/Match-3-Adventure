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

public static class ThingTypes {
    public const int Empty = -1;
    public const int Red = 0;
    public const int Blue = 1;
    public const int Yellow = 2;
    public const int Damage = 3;
    public const int Health = 4;
    public const int Multiplier = 5;
    public const int Null = 6;
    public const int Brick = 7;
    public const int Wildcard = 8;
    public const int RainbowMana = 9;

    public static bool IsMana(int type) {
        return type == Red || type == Blue || type == Yellow;
    }

    public static bool IsMovable(int type) {
        return type != Empty && type != Brick;
    }

    public static bool CanMatchAs(int type, int matchType) {
        if (type == Empty || type == Brick) {
            return false;
        }
        if (IsMana(matchType)) {
            return type == matchType || type == Wildcard;
        }
        if (matchType == RainbowMana) {
            return type == RainbowMana || type == Wildcard;
        }
        return type == matchType;
    }

    public static bool CanStartMatch(int type) {
        return type != Empty && type != Brick && type != Wildcard;
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
    multiplier,
    nullTile,
    brick,
    wildcard,
    rainbowMana
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
