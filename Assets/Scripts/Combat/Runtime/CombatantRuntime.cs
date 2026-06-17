using System;

public sealed class CombatantRuntime {
    private readonly bool includeMaxManaInText;

    public CombatantRuntime(CombatantState state, CombatantView view, bool includeMaxManaInText) {
        State = state;
        View = view;
        this.includeMaxManaInText = includeMaxManaInText;
    }

    public CombatantState State { get; }
    public CombatantView View { get; private set; }

    public void SetView(CombatantView view) {
        View = view;
    }

    public void SetIdentity(string name, int maxHealth, int maxRedMana, int maxBlueMana, int maxYellowMana) {
        State.Name = name;
        State.MaxHealth = maxHealth;
        State.Health = maxHealth;
        State.MaxRedMana = maxRedMana;
        State.MaxBlueMana = maxBlueMana;
        State.MaxYellowMana = maxYellowMana;
        State.RedMana = 0;
        State.BlueMana = 0;
        State.YellowMana = 0;
        State.DamageMultiplier = 1f;
        View?.SyncName(State);
        View?.SyncInitialHealth(State);
        SyncAllMana(initial: true);
        View?.SyncInitialMultiplier(State);
    }

    public void SetHealth(int health, bool initial = false) {
        State.Health = Clamp(health, 0, State.MaxHealth);
        if (initial) {
            View?.SyncInitialHealth(State);
        }
        else {
            View?.SyncHealth(State);
        }
    }

    public void SetMana(int id, int value, bool initial = false) {
        State.SetMana(id, value);
        if (initial) {
            View?.SyncInitialMana(State, id, includeMaxManaInText);
        }
        else {
            View?.SyncMana(State, id, includeMaxManaInText);
        }
    }

    public int GetMana(int id) {
        return State.GetMana(id);
    }

    public void SetMultiplier(float amount, bool initial = false) {
        State.DamageMultiplier = amount > GridController.maxMultiplier ? GridController.maxMultiplier : amount;
        if (State.DamageMultiplier < 1f) {
            State.DamageMultiplier = 1f;
        }

        if (initial) {
            View?.SyncInitialMultiplier(State);
        }
        else {
            View?.SyncMultiplier(State);
        }
    }

    public void DecrementMultiplier(float amount) {
        if (State.DamageMultiplier > 1f) {
            State.DamageMultiplier -= amount;
            State.DamageMultiplier = (float)Math.Round(State.DamageMultiplier, 1);
        }
        if (State.DamageMultiplier < 1f) {
            State.DamageMultiplier = 1f;
        }
        View?.SyncMultiplier(State);
    }

    public void SyncAllMana(bool initial = false) {
        SetMana((int)colorType.red, State.RedMana, initial);
        SetMana((int)colorType.blue, State.BlueMana, initial);
        SetMana((int)colorType.yellow, State.YellowMana, initial);
    }

    private int Clamp(int value, int min, int max) {
        if (value < min) {
            return min;
        }
        if (value > max) {
            return max;
        }
        return value;
    }
}
