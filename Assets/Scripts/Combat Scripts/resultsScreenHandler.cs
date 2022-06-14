using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class resultsScreenHandler : MonoBehaviour
{

    public static bool isDone = false;
    public static bool hasWon = false;
    public static bool hasLost = false;
    public static Text resultsText;

    // Start is called before the first frame update
    void Start()
    {
        resultsText = GetComponentInChildren<Text>();
        gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static void setWin() {
        hasWon = true;
        resultsText.text = "You Win!";
    }

    public static void setLoss() {
        hasLost = true;
        resultsText.text = "You Lose!";
    }

    public static void setDone() {
        isDone = true;
        resultsText.text = "You Win!";
    }

    public static void setNext() {
        hasWon = true;
        resultsText.text = "Next Enemy";
    }

    public void handlePress() {
        Debug.Log("Clicked results screen");
        //PLACEHOLDER: Will be 2 ifs, one to go to next piece of content, one to go to main menu/reset to last save

        if (hasWon && isDone) {
            SceneManager.LoadScene("DialogueScene", LoadSceneMode.Single);
            hasWon = false;
            isDone = false;
        }

        if (hasLost) {
            isDone = false;
            hasLost = false;
            hasWon = false;

            EnemyController enemyController = GameObject.Find("Grid").GetComponent<EnemyController>();

            if (enemyController.combat.getCompanion() == "none") {
                ScriptController.unPauseGame();
                enemyController.currentRound = 0;
                enemyController.loadEnemy();
                GameObject.Find("Grid").GetComponent<GridController>().resetPlayer();
                GridController.isTurn = true;
                GridController.madeMove = false;
            }
            else {
                DialogueController.writeDialogueVariable("companion", enemyController.combat.getCompanion());
                DialogueController.writeDialogueVariable("companionChangeoverDialogue", DialogueController.getCompanionDialogue(enemyController.combat.getCompanion()));
                DialogueController.dialogueToLoad = "changeover";
                GameObject.Find("Grid").GetComponent<GridController>().loadCompanion(enemyController.combat.getCompanion());
                SceneManager.LoadScene("DialogueScene", LoadSceneMode.Additive);
                GridController.isTurn = true;
                GridController.madeMove = false;
                enemyController.combat.removeCompanion();
                ScriptController.hideCombat();
                gameObject.SetActive(false);
                
            }

        }
        if (hasWon) {
            hasWon = false;
            ScriptController.unPauseGame();
            GridController.isTurn = true;
            GridController.madeMove = false;
            GameObject.Find("Grid").GetComponent<EnemyController>().loadEnemy();
            GameObject.Find("Grid").GetComponent<GridController>().inBetweenRoundEffects();
        }
        
    }


}
