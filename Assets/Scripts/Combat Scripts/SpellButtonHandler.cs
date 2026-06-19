using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpellButtonHandler : MonoBehaviour
{

    Spell spell;
    bool pressed = false;
    bool tooltipOpen = false;
    float pressedTime;

    GameObject tooltip;
    
    // Start is called before the first frame update
    void Start()
    {
        foreach (Image i in gameObject.GetComponentsInChildren<Image>()) {
            if (i.name == "tooltip") {
                tooltip = i.gameObject;
            }
        }
        
        tooltip.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (pressed) {
            if (Time.time - pressedTime > 0.3f && tooltipOpen == false) {
                //Open tooltip
                Debug.Log("Opening tooltip");
                tooltipOpen = true;
                tooltip.SetActive(true);
            }
        }
    }

    public void cast() {
        CombatSceneRefs refs = CombatSceneRefs.Instance;
        GridController gridController = refs != null ? refs.Grid : GameObject.Find("Grid").GetComponent<GridController>();
        EnemyController enemyController = refs != null ? refs.Enemy : GameObject.Find("Grid").GetComponent<EnemyController>();
        if (gameObject.tag == "player" && !gridController.isBoardSettledForInput()) {
            return;
        }

        Debug.Log("Casting: " + spell.Name);
        //Check Costs and cast spell

        if (gameObject.tag == "player" && gridController.checkCosts(spell.Costs)) {
            gridController.castSpell(spell, gridController, enemyController);
        }
        else if (gameObject.tag == "enemy" && enemyController.checkCosts(spell.Costs)) {
            gridController.castSpell(spell, enemyController, gridController);
        }
    }

    public void setSpell(Spell s) {
        spell = s;
        gameObject.GetComponentInChildren<Text>().text = spell.Name;
        foreach (Text i in gameObject.GetComponentsInChildren<Text>()) {
            switch (i.name) {
                case ("redCostText"):
                    i.text = spell.Costs[0].ToString();
                    if (spell.Costs[0] == 0) {
                        i.enabled = false;
                        i.GetComponentInChildren<Image>().enabled = false;
                    }
                    break;
                case ("blueCostText"):
                    i.text = spell.Costs[1].ToString();
                    if (spell.Costs[1] == 0) {
                        i.enabled = false;
                        i.GetComponentInChildren<Image>().enabled = false;
                    }
                    break;
                case ("yellowCostText"):
                    i.text = spell.Costs[2].ToString();
                    if (spell.Costs[2] == 0) {
                        i.enabled = false;
                        i.GetComponentInChildren<Image>().enabled = false;
                    }
                    break;
                case ("tooltipText"):
                    i.text = spell.Tooltip;
                    break;
            }
        }

    }

    public void buttonDown() {
        pressed = true;
        pressedTime = Time.time;
    }

    public void buttonUp() {
        pressed = false;
        if (tooltipOpen) {
            tooltipOpen = false;
            //Close tooltip
            tooltip.SetActive(false);
        }
    }

    public Spell getSpell() {
        return spell;
    }
}
