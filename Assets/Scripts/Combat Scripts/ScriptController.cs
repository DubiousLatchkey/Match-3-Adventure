using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScriptController : MonoBehaviour
{
    public static GridController gridController;
    public static EnemyController enemyController;
    public static resultsScreenHandler resultsScreenHandler;


    // Start is called before the first frame update
    void Start()
    {
        gridController = GameObject.Find("Grid").GetComponent<GridController>();
        enemyController = GameObject.Find("Grid").GetComponent<EnemyController>();
        resultsScreenHandler = GameObject.Find("resultsScreen").GetComponent<resultsScreenHandler>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static void pauseGame() {
        gridController.disableGrid();
        enemyController.enabled = false;
        resultsScreenHandler.gameObject.SetActive(true);
    }

    public static void unPauseGame() {
        gridController.enabled = true;
        gridController.enableGrid();
        enemyController.enabled = true;
        resultsScreenHandler.gameObject.SetActive(false);

    }

    public static void hideCombat() {
        gridController.hide();
    }

    public static void showCombat() {
        gridController.show();
    }
}
