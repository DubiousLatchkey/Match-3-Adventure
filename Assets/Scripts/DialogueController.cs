using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DialogueController : MonoBehaviour
{
    public static string dialogueToLoad = "sampleDialogue";
    public static bool isSkipping = false;
    public GameObject fadeObject;
    public GameObject backgound;
    public GameObject namePlate;
    public GameObject log;
    public musicController music;
    public GameObject displayImage;
    public GameObject CG;
    public Button skipButton;
    public GameObject mainCamera;

    private bool wasCutOff = false;
    private List<string> lines;
    private int linePosition = 0;
    private IDictionary<string, GameObject> characters;

    public float baseYAxisOffset = -0.5f;
    public float positionalMultiplier = 50;
    public float baseSpeed;
    public int maxCharacters = 250;
    public static IDictionary<string, string> dialogueVariables;

    private string currentLine;
    private bool isTyping = false;
    private int textStartIndex = 0;
    private bool keepLineFlag = false;
    private bool isFading = false;
    private bool isDoingSkipDelay = false;

    private Sprite nextBackground;
    private Image fadeImage;

    IEnumerator textDisplay;
    IDictionary<string, int> textLinkedMoveLengths;
    Text lineOfDialogue;
    Text nameText;

    // Start is called before the first frame update
    void Start()
    {

        if (dialogueToLoad == "changeover") {
            Destroy(mainCamera.GetComponent<AudioListener>());
        }
        isSkipping = false;
        dialogueVariables = new Dictionary<string, string>();
        textLinkedMoveLengths = new Dictionary<string, int>();

        //Get all variables
        dialogueVariables.Add("main", PlayerPrefs.GetString("name", "Rachel"));
        if (File.Exists(Path.Combine(Application.persistentDataPath, "dialogueVariables.txt"))) {
            string varText = File.ReadAllText(Path.Combine(Application.persistentDataPath, "dialogueVariables.txt"));
            foreach (string i in varText.Split('\n')) {
                string[] variable = i.Split('|');
                if (variable.Length == 2) {
                    dialogueVariables.Add(variable[0], variable[1]);
                }
            }
        }

        lines = new List<string>();
        foreach (Text i in gameObject.GetComponentsInChildren<Text>()) {
            switch (i.name) {
                case ("DialogueText"):
                    lineOfDialogue = i;
                    break;
                case ("NameText"):
                    nameText = i;
                    break;
            }
        }

        //Load appropriate scene
        TextAsset dialogueAsset = Resources.Load<TextAsset>("Dialogues/"+ dialogueToLoad);
        string dialogue = dialogueAsset.text;

        foreach (string i in dialogueVariables.Keys) {
            dialogue = dialogue.Replace("@" + i + "@", dialogueVariables[i]);
        }

        lines = new List<string>(dialogue.Split('\n'));
        foreach (string line in lines) {
            Debug.Log(line);
        }

        //Load Characters
        characters = new Dictionary<string, GameObject>();
        loadCharacters();
        linePosition++;

        //Load first dialogue
        fadeImage = fadeObject.GetComponent<Image>();
        fadeImage.enabled = false;
        displayImage.SetActive(false);
        backgound.GetComponent<Image>().sprite = Resources.Load<Sprite>("Backgrounds/" + PlayerPrefs.GetString("background", "classroomPlaceholder"));
        advanceDialogue();

    }

    public static void writeDialogueVariable(string variableName, string variableValue) {
        if (File.Exists(Path.Combine(Application.persistentDataPath, "dialogueVariables.txt"))) {
            Debug.Log("Dialogue Variables file exists");
            string varText = File.ReadAllText(Path.Combine(Application.persistentDataPath, "dialogueVariables.txt"));
            Debug.Log(varText);

            string[] variables = varText.Split('\n');
            List<string[]> variableValuePairs = new List<string[]>();
            bool nameExists = false;
            foreach (string i in variables) {
                variableValuePairs.Add(i.Split('|'));
                if (variableValuePairs[variableValuePairs.Count - 1][0] == variableName) {
                    variableValuePairs[variableValuePairs.Count - 1][1] = variableValue;
                    nameExists = true;
                }
            }
            if (!nameExists) {
                variableValuePairs.Add(new string[] { variableName, variableValue });
            }
            List<string> variablesInCSVLines = new List<string>();
            foreach (string[] i in variableValuePairs) {
                variablesInCSVLines.Add(string.Join("|", i));
            }
            string newVariables = string.Join("\n", variablesInCSVLines);
            File.WriteAllText(Path.Combine(Application.persistentDataPath, "dialogueVariables.txt"), newVariables);
        }
        else {
            Debug.Log("Making Dialogue variables file");
            var sr = File.CreateText(Path.Combine(Application.persistentDataPath, "dialogueVariables.txt"));
            sr.WriteLine(variableName + "|" + variableValue);
            sr.Close();
        }
        
    }

    public static string getCompanionDialogue(string companionName) {
        string[] companionsData = Resources.Load<TextAsset>("companions").text.Split('\n');
        foreach (string i in companionsData) {
            string[] data = i.Split('|');
            if (data[0] == companionName) {
                return data[1];
            }
        }
        return "";
    }

    public static Companion GetCompanion(string companionName) {
        string[] companionsData = Resources.Load<TextAsset>("companions").text.Split('\n');
        foreach (string i in companionsData) {
            string[] data = i.Split('|');
            if (data[0] == companionName) {
                int[] spells = System.Array.ConvertAll(data[7].Split(','), s => int.Parse(s));
                Debug.Log(spells[0] + " " +spells[1]);
                return new Companion(data[0], data[1], int.Parse(data[2]), int.Parse(data[3]), int.Parse(data[4]), int.Parse(data[5]), int.Parse(data[6]), spells);
            }
        }
        return default(Companion) ;
    }

    // Update is called once per frame
    void Update()
    {
        if (isSkipping && !isDoingSkipDelay) {
            StartCoroutine(doSkipDelay());
        }
    }

    IEnumerator doSkipDelay() {
        isDoingSkipDelay = true;
        yield return new WaitForSecondsRealtime(0.1f);
        advanceDialogue();
        isDoingSkipDelay = false;
    }

    //Trigger on click of log button, toggles visibility of log
    public void toggleLog() {
        if (log.activeInHierarchy) {
            log.transform.parent.parent.gameObject.SetActive(false);
        }
        else {
            //Debug.Log(linePosition);
            /*
            string logText = "";
            for (int i = 0; i < linePosition; i++) {
                if (!lines[i].StartsWith("|")) {
                    string[] split = lines[i].Split(':');
                    string niceLooking;
                    if (split[0] == " ") {
                        niceLooking = lines[i].Replace(split[0] + ":", "");
                        logText += niceLooking + "\n\n";
                    }
                    else {
                        niceLooking = lines[i].Replace(split[0] + ":", split[0] + ": ");
                        logText += char.ToUpper(lines[i][0]) + niceLooking.Substring(1) + "\n\n";
                    }
                }
            }
            logText = logText.Replace("Main:", PlayerPrefs.GetString("name", "Rachel") + ":");
            log.GetComponent<Text>().text = logText;
            log.transform.parent.parent.gameObject.SetActive(true);
            */
            log.transform.parent.parent.gameObject.SetActive(true);


        }
    }

    //Trigger on click of background, advances dialogue
    public void advanceDialogue() {
        if (linePosition >= lines.Count) {
            //Shouldn't be reachable
            Debug.Log("This isn't Kansas anymore");
            return;
        }
        //If is during a fade to black, next actions should be performed during the blackness, not the fade
        if(isFading) {
            return;
        }

        foreach (KeyValuePair<string, GameObject> entry in characters) {
            if (textLinkedMoveLengths.Keys.Contains(entry.Key)) {
                if(textLinkedMoveLengths[entry.Key] == 0) {
                    //If reached end of location, snap to position
                    entry.Value.transform.position = entry.Value.GetComponent<MoveToTargetBehavior>().getTarget();
                    textLinkedMoveLengths.Remove(entry.Key);
                }
            }
        }

        //Check if currently typing out text, then if so, skip to end
        if (isTyping) {
            //StopCoroutine(textDisplay);
            StopAllCoroutines();
            Color color = fadeObject.GetComponent<Image>().color;
            color.a = 0;
            fadeObject.GetComponent<Image>().color = color;
            fadeImage.enabled = false;
            lineOfDialogue.text = currentLine.Trim();
            isTyping = false;
            if (!keepLineFlag) {
                linePosition++;
                foreach (KeyValuePair<string, GameObject> entry in characters) {
                    if (textLinkedMoveLengths.Keys.Contains(entry.Key)) {
                        textLinkedMoveLengths[entry.Key] -= 1;
                    }
                }
            }
            return;
        }
        textDisplay = displayText();

        //Parse commands, else display dialogue
        if (lines[linePosition][0] == '|') {
            performAction(lines[linePosition].Substring(1, lines[linePosition].Length - 1));

            //Keep going until find next line
            linePosition++;
            advanceDialogue();
            return;
        }
        else {


            //Assign name
            string name = lines[linePosition].Split(':')[0];
            //Change focus to speaker, could be more efficient, but meh
            foreach (KeyValuePair<string, GameObject> entry in characters) {
                if (entry.Key == name) {
                    //entry.Value.GetComponent<Image>().color = new Color32(255, 255, 225, 255);
                    entry.Value.GetComponent<FadeController>().setFadeIn();
                }
                else{
                    //entry.Value.GetComponent<Image>().color = new Color32(150, 150, 150, 255);
                    entry.Value.GetComponent<FadeController>().setFadeOut();
                }
            }
            if (name == "main") { name = PlayerPrefs.GetString("name", "Rachel"); }
         
            nameText.text = char.ToUpper(name[0]) + name.Substring(1);
            if (name == " ") { //For narration
                namePlate.SetActive(false);
            }
            else {
                namePlate.SetActive(true);
            }

            currentLine = lines[linePosition].Split(':')[1];
            currentLine = currentLine.Substring(textStartIndex);

            //Add text to log
            addTextToLog(name, currentLine);

            //Cut line down to size if overflowing
            if (currentLine.Length > maxCharacters) {
                wasCutOff = true;
                for (int i = maxCharacters - 1; i >= 0; i--) {
                    if (".!?".Contains(currentLine[i].ToString())) {
                        currentLine = currentLine.Substring(0, i + 2);
                        textStartIndex += i + 2;
                        keepLineFlag = true;
                        break;
                    }
                }
            }
            else {
                wasCutOff = false;
            }
            if (!wasCutOff) {
                textStartIndex = 0;
                keepLineFlag = false;
            }
        }
              
        
        //Display next line, line only reached if it wasn't a command
        StartCoroutine(textDisplay);

    }

    private void addTextToLog(string name, string currentLine) {
        //Add to log
        GameObject logText = Instantiate(Resources.Load<GameObject>("logText"), log.transform);
        Text logTextText = logText.GetComponentInChildren<Text>();
        logTextText.text = name == " " ? currentLine : nameText.text + ": " + currentLine;
        logText.GetComponent<RectTransform>().sizeDelta = new Vector2(850f, logTextText.preferredHeight + 50f);
        logText.transform.SetAsLastSibling();
    }

    //Does the text crawl
    IEnumerator displayText() {
        isTyping = true;
        for (int i = 0; i <= currentLine.Length; i++) {
            lineOfDialogue.text = currentLine.Substring(0, i).Trim();
            yield return new WaitForEndOfFrame();
        }
        if (!keepLineFlag) {
            linePosition++;
        }
        isTyping = false;
        foreach (KeyValuePair<string, GameObject> entry in characters) {
            if (textLinkedMoveLengths.Keys.Contains(entry.Key)) {
                textLinkedMoveLengths[entry.Key] -= 1;
            }
        }
    }

    IEnumerator FadeToBlack() {
        isFading = true;
        for (float i = 0; i <= 1; i += 0.05f) {
            //Debug.Log("Fading to black " +i);
            Color color = fadeImage.color;
            color.a = i;
            fadeImage.color = color;
            yield return new WaitForSeconds(0.05f);
        }
        if (nextBackground != null) {
            backgound.GetComponent<Image>().sprite = nextBackground;
            nextBackground = null;
        }
        isFading = false;
        advanceDialogue();
        StartCoroutine(FadeBackIn());
    }

    IEnumerator FadeBackIn() {
        //Debug.Log("Fading back in");
        for (float i = 1; i > 0; i -= 0.05f) {
            Color color = fadeObject.GetComponent<Image>().color;
            color.a = i;
            fadeImage.color = color;
            yield return new WaitForSeconds(0.05f);
        }

        fadeImage.enabled = false;
    }


    //Initializes characters into scene
    public void loadCharacters() {
        string line = lines[linePosition];
        line = line.Substring(1, line.Length - 1);

        //Split line into characters and their information
        List<string> characterNames = new List<string>(line.Split(','));

        //Loop through
        foreach (string i in characterNames) {
            List<string> characterInfo = new List<string>(i.Split(' '));
            //Load image
            characters.Add(characterInfo[0], Instantiate(Resources.Load<GameObject>("Character"), GameObject.Find("Characters").transform));
            characters[characterInfo[0]].GetComponent<Image>().sprite = Resources.Load<Sprite>("Characters/" + characterInfo[0] + "Normal");
            //Set none as focused
            characters[characterInfo[0]].GetComponent<Image>().color = new Color32(150, 150, 150, 255);

            //Position image
            //characters[characterInfo[0]].transform.SetParent(GameObject.Find("Characters").transform);
            characters[characterInfo[0]].transform.localPosition = new Vector3(0, 0);
            float xPosition = float.Parse(characterInfo[1]) * positionalMultiplier;
            float yPosition = (float.Parse(characterInfo[2]) + baseYAxisOffset) * positionalMultiplier;
            characters[characterInfo[0]].transform.localPosition = (new Vector3(xPosition, yPosition));
        }

    }

    //String that corresponds to an action in the dialogue
    public void performAction(string action) {
        List<string> actionAndParameters = new List<string>(action.Split(' '));
        switch (actionAndParameters[0]) {
            //Add spaces to actions without parameters for proper parsing
            case ("translate"):
                //Teleports to coords of parameters 2 and 3
                float xMove = float.Parse(actionAndParameters[2]) * positionalMultiplier;
                float yMove = (float.Parse(actionAndParameters[3]) + baseYAxisOffset) * positionalMultiplier;
                characters[actionAndParameters[1]].transform.localPosition = (new Vector3(xMove, yMove));
                characters[actionAndParameters[1]].GetComponent<MoveToTargetBehavior>().setTarget(characters[actionAndParameters[1]].transform.position);
                //Debug.Log(characters[actionAndParameters[1]].transform.localPosition);
                //Debug.Log(characters[actionAndParameters[1]].transform.position);
                break;
            case ("setExpression"):
                //Sets parameter 1's expression to parameter 2
                string filename = "Characters/" + actionAndParameters[1] + actionAndParameters[2];
                filename = filename.Trim();
                characters[actionAndParameters[1]].GetComponent<Image>().sprite = Resources.Load<Sprite>(filename);
                break;
            case ("move"):
                //Moves parameter 1 by parameters 3 and 4 at speed parameter 2 over parameter 5 dialogue lines
                move(actionAndParameters[1], float.Parse(actionAndParameters[2]), float.Parse(actionAndParameters[3]), float.Parse(actionAndParameters[4]));
                textLinkedMoveLengths.Add(actionAndParameters[1], int.Parse(actionAndParameters[5]));
                break;
            case ("moveTo"):
                //Moves parameter 1 to coordinates parameters 3 and 4 at speed parameter 2
                moveTo(actionAndParameters[1], float.Parse(actionAndParameters[2]), float.Parse(actionAndParameters[3]), float.Parse(actionAndParameters[4]));
                textLinkedMoveLengths.Add(actionAndParameters[1], int.Parse(actionAndParameters[5]));
                break;
            case ("exit"):
                //Fade and move out character, followed by deletion
                FadeController fadeController = characters[actionAndParameters[1]].GetComponent<FadeController>();
                MoveToTargetBehavior targetBehavior = characters[actionAndParameters[1]].GetComponent<MoveToTargetBehavior>();
                if (actionAndParameters[2] == "right") {
                    targetBehavior.setTarget(new Vector3(1000, 0) + characters[actionAndParameters[1]].transform.localPosition);
                }
                else if (actionAndParameters[2] == "left") {
                    targetBehavior.setTarget(new Vector3(-1000, 0) + characters[actionAndParameters[1]].transform.localPosition);
                }
                else if (actionAndParameters[2] == "down") {
                    targetBehavior.setTarget(new Vector3(0, -1000) + characters[actionAndParameters[1]].transform.localPosition);
                }
                targetBehavior.setInitialDistance(Vector3.Distance(characters[actionAndParameters[1]].transform.localPosition, targetBehavior.getTarget()));
                targetBehavior.setSpeed(5);
                fadeController.scheduleForDeletion();
                characters.Remove(actionAndParameters[1]);
                break;
            case ("enter"):
                //Loads in parameter 1 at position parameters 2 and 3
                //If parameter 4 is "generic", load generic character instead
                //Generics can also have an alias at parameter 5.  Blank means the alias is the file name of the image
                //Add person at position
                string additional = "";
                string alias;
                if (actionAndParameters.Count < 5) { additional = "Normal"; }
                
                string characterName = actionAndParameters[1].Trim();

                if (actionAndParameters.Count > 5) {
                    alias = actionAndParameters[5].Trim();
                }
                else {
                    alias = characterName;
                }

                characters.Add(alias, Instantiate(Resources.Load<GameObject>("Character"), GameObject.Find("Characters").transform));
                characters[alias].GetComponent<Image>().sprite = Resources.Load<Sprite>("Characters/"+characterName + additional);

                characters[alias].GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                fadeController = characters[alias].GetComponent<FadeController>();
                fadeController.setFadeIn();

                //characters[characterName].transform.SetParent(GameObject.Find("Characters").transform);

                //Set position
                characters[alias].transform.localPosition = new Vector3(0, 0);
                float xPosition = float.Parse(actionAndParameters[2]) * positionalMultiplier;
                float yPosition = (float.Parse(actionAndParameters[3]) + baseYAxisOffset) * positionalMultiplier;
                characters[alias].transform.localPosition = (new Vector3(xPosition, yPosition));

                break;
            case ("combat"):
                //Loads combat with name parameter 1
                GridController.combatToLoad = actionAndParameters[1];

                SceneManager.LoadScene("GearUpScene", LoadSceneMode.Single);
                break;
            case ("combatDirect"):
                //Loads combat with name parameter 1 without going to gear up scene
                GridController.combatToLoad = actionAndParameters[1];

                SceneManager.LoadScene("CombatScene", LoadSceneMode.Single);
                break;
            case ("resumeCombat"):
                //Closes dialogue and goes to already loaded combat for changeovers
                ScriptController.unPauseGame();
                ScriptController.showCombat();

                SceneManager.UnloadSceneAsync("DialogueScene");
                break;
            case ("transition"):
                //Change background to parameter 1 with fade to black
                //Fade to black, change backgrounds, and go back
                fadeImage.enabled = true;
                nextBackground = Resources.Load<Sprite>("Backgrounds/" + actionAndParameters[1].Trim());
                PlayerPrefs.SetString("background", actionAndParameters[1].Trim());
                StartCoroutine(FadeToBlack());
                break;
            case ("setBackground"):
                //Set background with no transition to parameter 1
                backgound.GetComponent<Image>().sprite = Resources.Load<Sprite>("Backgrounds/" + actionAndParameters[1].Trim());
                PlayerPrefs.SetString("background", actionAndParameters[1].Trim());
                break;
            case ("setPrefValue"):
                //Set player prefs key (Combination of all parameters except first and last) to last parameter
                string key = string.Join(" ", actionAndParameters.GetRange(1, actionAndParameters.Count - 2));
                //Debug.Log(key);
                PlayerPrefs.SetInt(key, int.Parse(actionAndParameters[actionAndParameters.Count - 1]));
                break;
            case ("playMusic"):
                //Play music with name made from all non-zero parameters
                //Use only for music, not sound
                //Debug.Log(string.Join(" ", actionAndParameters.GetRange(1, actionAndParameters.Count - 1)));
                music.setTrack(string.Join(" ", actionAndParameters.GetRange(1, actionAndParameters.Count - 1)).Trim());
                music.play();
                break;
            case ("stopMusic"):
                //No parameters, just stops music
                music.stop();
                break;
            case ("playSound"):
                //Play sound with name made from all non-zero parameters
                //Use only for sound effects, not music
                GameObject soundEffect = Instantiate(Resources.Load<GameObject>("sound effect"));
                soundEffect.GetComponent<SoundEffectHandler>().play(string.Join(" ", actionAndParameters.GetRange(1, actionAndParameters.Count - 1)).Trim());
                break;
            case ("display"):
                //Set display image to nonzero parameters
                displayImage.SetActive(true);
                displayImage.GetComponent<Image>().sprite = Resources.Load<Sprite>(string.Join(" ", actionAndParameters.GetRange(1, actionAndParameters.Count - 1)).Trim());
                break;
            case ("stopDisplay"):
                //No parameters, just turns off image display
                displayImage.SetActive(false);
                break;
            case ("displayCG"):
                //Sets CG image to nonzero parameters
                CG.GetComponent<Image>().enabled = true;
                displayImage.GetComponent<Image>().sprite = Resources.Load<Sprite>(string.Join(" ", actionAndParameters.GetRange(1, actionAndParameters.Count - 1)).Trim());
                break;
            case ("stopDisplayCG"):
                //No parameters, just turns off CG display
                CG.GetComponent<Image>().enabled = false;
                break;
            case ("swap"):
                //swap position of character with name parameter 1 with character with name parameter 2 (for when too many people are on screen)
                MoveToTargetBehavior targetBehavior1 = characters[actionAndParameters[1].Trim()].GetComponent<MoveToTargetBehavior>();
                MoveToTargetBehavior targetBehavior2 = characters[actionAndParameters[2].Trim()].GetComponent<MoveToTargetBehavior>();
                targetBehavior1.setTarget(targetBehavior2.gameObject.transform.position, 1.5f);
                targetBehavior2.setTarget(targetBehavior1.gameObject.transform.position, 1.5f);
                break;
        }
    }

    //Move character in relative units
    public void move(string character, float speed, float xMove, float yMove) {
        xMove = xMove * positionalMultiplier;
        yMove = (yMove + baseYAxisOffset) * positionalMultiplier;
        //speed = speed * baseSpeed;
        MoveToTargetBehavior targetBehavior = characters[character].GetComponent<MoveToTargetBehavior>();
        //Debug.Log(new Vector3(xMove, yMove) + " " + characters[actionAndParameters[1]].transform.localPosition +" " + characters[actionAndParameters[1]].transform.position);
        targetBehavior.setTarget((new Vector3(xMove, yMove, -characters[character].transform.localPosition.z) + characters[character].transform.localPosition) / 100, speed);
    }

    //Move character to target position
    public void moveTo(string character, float speed, float xTarget, float yTarget) {
        MoveToTargetBehavior targetBehavior = characters[character].GetComponent<MoveToTargetBehavior>();
        xTarget = xTarget * positionalMultiplier;
        yTarget = (yTarget + baseYAxisOffset) * positionalMultiplier;
        //speed = speed * baseSpeed;
        targetBehavior.setTarget((new Vector3(xTarget, yTarget, -characters[character].transform.localPosition.z)) / 100, speed);

    }
}
