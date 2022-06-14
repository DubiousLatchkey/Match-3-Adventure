using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class tutorialHandler : MonoBehaviour
{

    public GridController gridController;
    public GameObject tutorialScreen;
    public Image tutorialImage;
    public Text tutorialText;
    public Text tutorialTitleText;

    Combat combat;
    List<string> tutorials;
    string currentTutorial;

    // Start is called before the first frame update
    void Start()
    {
        combat = new Combat(GridController.combatToLoad);
        tutorials = combat.getTutorials();
        //Disable tutorial listener if there are no tutorials for this combat
        if(tutorials.Count == 0) {
            enabled = false;
        }


    }

    // Update is called once per frame
    void LateUpdate()
    {
        //If there are no more tutorials, destroy this object
        if(tutorials.Count < 1) {
            enabled = false;
        }
        if (gridController.isValidMoveTime()) {
            //Check conditions for tutorials
            foreach (string tutorialName in tutorials) {
                switch (tutorialName) {
                    case "basics":
                        evaluateTutorialCondition(true, tutorialName);
                        break;
                    case "multipliers":
                        evaluateTutorialCondition(gridController.getLastScored(5) > 0, tutorialName);
                        break;
                    case "fourInARows":
                        evaluateTutorialCondition(gridController.getExtraTurn(), tutorialName);
                        break;
                    case "opponents":
                        evaluateTutorialCondition(true, tutorialName);
                        break;
                    case "castingSpells":
                        evaluateTutorialCondition(gridController.isSpellsEquipped(), tutorialName);
                        break;
                    case "weapons":
                        evaluateTutorialCondition(gridController.weapon.Type != WeaponType.none, tutorialName);
                        break;
                    case "statusEffects":
                        evaluateTutorialCondition(gridController.GetStatusEffects().Count > 0 || gameObject.GetComponent<EnemyController>().GetStatusEffects().Count > 0, tutorialName);
                        break;
                    default:
                        continue;
                }
            }
        }

    }

    //Takes in boolean function, shows tutorial of name if it is true
    public void evaluateTutorialCondition(bool condition, string tutorialName) {
        if (condition) {
            showTutorial(tutorialName);
        }
    }

    //Shows tutorial screen for named tutorial
    public void showTutorial(string tutorialName) {
        //Load in tutorial file
        string text = Resources.Load<TextAsset>("Tutorials/" + tutorialName).text;
        List<string> textArray = new List<string>(text.Split('\n'));

        //Display pieces
        tutorialScreen.SetActive(true);
        //First 2 lines are title and image, others are text 
        tutorialTitleText.text = textArray[0];
        tutorialImage.sprite = Resources.Load<Sprite>("Tutorials/" + textArray[1].Trim());
        tutorialText.text = string.Join("\n", textArray.GetRange(2, textArray.Count - 2));

        currentTutorial = tutorialName;

        ScriptController.hideCombat();
    }

    //Triggered on button click to dismiss tutorial
    public void dismissTutorial() {
        tutorialScreen.SetActive(false);
        tutorials.Remove(currentTutorial);
        ScriptController.showCombat();
    }
}
