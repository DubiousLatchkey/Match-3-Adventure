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
        if(SaveGameService.GetInt("savedGameExists", 0) == 1) {
            //PLACEHOLDER: To be replaced with resuming to specific location
            //SceneManager.LoadScene("CombatScene", LoadSceneMode.Single);
            DialogueController.dialogueToLoad = StoryFlowConfig.DefaultResume;
            SceneManager.LoadScene("DialogueScene", LoadSceneMode.Single);
        }
        //TODO: What to when there is no saved game
    }

    public void newGame() {
        SaveGameService.NewGame(SpellContentLoader.LoadSpells(), WeaponContentLoader.LoadWeapons());

        //Intro
        DialogueController.dialogueToLoad = StoryFlowConfig.NewGameStart;
        SceneManager.LoadScene("DialogueScene", LoadSceneMode.Single);

    }

    public void quit() {
        Application.Quit();
    }
}
