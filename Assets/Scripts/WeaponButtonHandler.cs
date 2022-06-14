using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponButtonHandler : MonoBehaviour
{
    public TooltipHandler tooltipHandler;
    public Image weaponImage;
    Weapon weapon;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void setWeapon(Weapon weapon) {
        this.weapon = weapon;
        tooltipHandler.setText(weapon.Name + ": " +weapon.Tooltip);
        weaponImage.sprite = Resources.Load<Sprite>("Weapons/" +weapon.Name);
    }

    public Weapon getWeapon() {
        return weapon;
    }
}
