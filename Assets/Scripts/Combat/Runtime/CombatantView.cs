using UnityEngine;
using UnityEngine.UI;

public sealed class CombatantView : MonoBehaviour {
    [SerializeField] private Text nameText;
    [SerializeField] private Text hpText;
    [SerializeField] private Text redManaText;
    [SerializeField] private Text blueManaText;
    [SerializeField] private Text yellowManaText;
    [SerializeField] private Text multiplierText;
    [SerializeField] private BarHandler healthBar;
    [SerializeField] private BarHandler redBar;
    [SerializeField] private BarHandler blueBar;
    [SerializeField] private BarHandler yellowBar;
    [SerializeField] private BarHandler multiplierBar;
    [SerializeField] private Image portrait;
    [SerializeField] private Transform spellParent;
    [SerializeField] private Transform statusParent;

    public Image Portrait => portrait;
    public Transform SpellParent => spellParent;
    public Transform StatusParent => statusParent;

    public void SetFallbacks(
        Text nameText,
        Text hpText,
        Text redManaText,
        Text blueManaText,
        Text yellowManaText,
        Text multiplierText,
        BarHandler healthBar,
        BarHandler redBar,
        BarHandler blueBar,
        BarHandler yellowBar,
        BarHandler multiplierBar,
        Image portrait = null,
        Transform spellParent = null,
        Transform statusParent = null) {
        this.nameText = this.nameText != null ? this.nameText : nameText;
        this.hpText = this.hpText != null ? this.hpText : hpText;
        this.redManaText = this.redManaText != null ? this.redManaText : redManaText;
        this.blueManaText = this.blueManaText != null ? this.blueManaText : blueManaText;
        this.yellowManaText = this.yellowManaText != null ? this.yellowManaText : yellowManaText;
        this.multiplierText = this.multiplierText != null ? this.multiplierText : multiplierText;
        this.healthBar = this.healthBar != null ? this.healthBar : healthBar;
        this.redBar = this.redBar != null ? this.redBar : redBar;
        this.blueBar = this.blueBar != null ? this.blueBar : blueBar;
        this.yellowBar = this.yellowBar != null ? this.yellowBar : yellowBar;
        this.multiplierBar = this.multiplierBar != null ? this.multiplierBar : multiplierBar;
        this.portrait = this.portrait != null ? this.portrait : portrait;
        this.spellParent = this.spellParent != null ? this.spellParent : spellParent;
        this.statusParent = this.statusParent != null ? this.statusParent : statusParent;
    }

    public void SyncName(CombatantState state) {
        if (nameText != null) {
            nameText.text = state.Name;
        }
    }

    public void SyncHealth(CombatantState state) {
        if (hpText != null) {
            hpText.text = state.Health.ToString() + " out of " + state.MaxHealth.ToString();
        }
        if (healthBar != null) {
            healthBar.setPercentageFilled(state.Health / (state.MaxHealth + 0f));
        }
    }

    public void SyncInitialHealth(CombatantState state) {
        if (hpText != null) {
            hpText.text = state.Health.ToString() + " out of " + state.MaxHealth.ToString();
        }
        if (healthBar != null) {
            healthBar.setInitialPercentageFilled(state.Health / (state.MaxHealth + 0f));
        }
    }

    public void SyncMana(CombatantState state, int id, bool includeMax) {
        Text targetText = null;
        BarHandler targetBar = null;
        switch (id) {
            case (int)colorType.red:
                targetText = redManaText;
                targetBar = redBar;
                break;
            case (int)colorType.blue:
                targetText = blueManaText;
                targetBar = blueBar;
                break;
            case (int)colorType.yellow:
                targetText = yellowManaText;
                targetBar = yellowBar;
                break;
        }

        if (targetText != null) {
            targetText.text = includeMax
                ? state.GetMana(id).ToString() + "/" + state.GetMaxMana(id).ToString()
                : state.GetMana(id).ToString();
        }
        if (targetBar != null) {
            targetBar.setPercentageFilled(state.GetMana(id) / (state.GetMaxMana(id) + 0f));
        }
    }

    public void SyncInitialMana(CombatantState state, int id, bool includeMax) {
        Text targetText = null;
        BarHandler targetBar = null;
        switch (id) {
            case (int)colorType.red:
                targetText = redManaText;
                targetBar = redBar;
                break;
            case (int)colorType.blue:
                targetText = blueManaText;
                targetBar = blueBar;
                break;
            case (int)colorType.yellow:
                targetText = yellowManaText;
                targetBar = yellowBar;
                break;
        }

        if (targetText != null) {
            targetText.text = includeMax
                ? state.GetMana(id).ToString() + "/" + state.GetMaxMana(id).ToString()
                : state.GetMana(id).ToString();
        }
        if (targetBar != null) {
            targetBar.setInitialPercentageFilled(state.GetMana(id) / (state.GetMaxMana(id) + 0f));
        }
    }

    public void SyncMultiplier(CombatantState state) {
        if (multiplierText != null) {
            multiplierText.text = state.DamageMultiplier.ToString() + "x";
        }
        if (multiplierBar != null) {
            multiplierBar.setPercentageFilled((state.DamageMultiplier - 1f) / (GridController.maxMultiplier - 1f));
        }
    }

    public void SyncInitialMultiplier(CombatantState state) {
        if (multiplierText != null) {
            multiplierText.text = state.DamageMultiplier.ToString() + "x";
        }
        if (multiplierBar != null) {
            multiplierBar.setInitialPercentageFilled((state.DamageMultiplier - 1f) / (GridController.maxMultiplier - 1f));
        }
    }
}
