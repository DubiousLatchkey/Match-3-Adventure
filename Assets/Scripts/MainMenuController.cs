using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    List<MoveToTargetBehavior> objectsToMove;

    // Start is called before the first frame update
    void Start()
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;

        objectsToMove = new List<MoveToTargetBehavior>();
        foreach(MoveToTargetBehavior i in gameObject.GetComponentsInChildren<MoveToTargetBehavior>()){
            objectsToMove.Add(i);
            i.setTarget(i.gameObject.transform.position);
            float angle = UnityEngine.Random.Range(0f, 360f);
            float distance = UnityEngine.Random.Range(500f, 1500f);
            //i.setTarget(i.transform.position);
            i.gameObject.transform.Rotate(new Vector3(0f, 0f, angle));
            i.gameObject.transform.Translate(new Vector3(distance, 0f, distance));
            i.gameObject.transform.rotation = Quaternion.identity;
            i.setInitialDistance(Vector3.Distance(i.getTarget(), i.transform.position));
            i.setSpeed(2f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void resumeGame() {
        //TODO: Start game without making save data
        if(PlayerPrefs.GetInt("savedGameExists", 0) == 1) {
            //PLACEHOLDER: To be replaced with resuming to specific location
            //SceneManager.LoadScene("CombatScene", LoadSceneMode.Single);
            DialogueController.dialogueToLoad = "revelation";
            SceneManager.LoadScene("DialogueScene", LoadSceneMode.Single);
        }
        //TODO: What to when there is no saved game
    }

    public void newGame() {
        //TODO: Start game and make save data
        //Placeholder spells
        PlayerPrefs.SetInt("Enflame", 0);
        PlayerPrefs.SetInt("Fireball", 0);
        PlayerPrefs.SetInt("Aether", 0);
        PlayerPrefs.SetInt("Rebase", 0);
        PlayerPrefs.SetInt("Ethereal Vivisection", 0);
        PlayerPrefs.SetInt("Toxic Deluge", 0);
        PlayerPrefs.SetInt("Clean Slate", 0);
        PlayerPrefs.SetInt("Swords to Plowshares", 0);
        PlayerPrefs.SetInt("Balance", 0);
        PlayerPrefs.SetInt("Restore", 0);
        PlayerPrefs.SetInt("Mana Drain", 0);
        PlayerPrefs.SetInt("Kinetic Barrier", 0);
        PlayerPrefs.SetInt("Cantrip", 0);
        PlayerPrefs.SetInt("Fiery Transfusion", 0);
        PlayerPrefs.SetInt("Precision Strike", 0);
        PlayerPrefs.SetInt("Invigorate", 0);
        PlayerPrefs.SetInt("Explosive Outburst", 0);

        PlayerPrefs.SetInt("Warrior's Staff", 0);
        PlayerPrefs.SetInt("Mage Rapier", 0);
        PlayerPrefs.SetInt("Spellslinger's Device", 0);
        PlayerPrefs.SetInt("Scarlet Staff", 0);
        PlayerPrefs.SetInt("Crimson Staff", 0);
        PlayerPrefs.SetInt("Amplifying Staff", 0);


        //Base values
        PlayerPrefs.SetInt("hp", 100);
        PlayerPrefs.SetInt("maxRedMana", 10);
        PlayerPrefs.SetInt("maxBlueMana", 10);
        PlayerPrefs.SetInt("maxYellowMana", 10);
        PlayerPrefs.SetInt("savedGameExists", 1);

        //Intro
        DialogueController.dialogueToLoad = "opening";
        SceneManager.LoadScene("DialogueScene", LoadSceneMode.Single);

    }

    public void quit() {
        Application.Quit();
    }
}
