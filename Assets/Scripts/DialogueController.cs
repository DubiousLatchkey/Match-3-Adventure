using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class DialogueController : MonoBehaviour
{
    public static string dialogueToLoad = "3_007_iredellConfrontation";
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
    public RectTransform characterStage;

    private bool wasCutOff = false;
    private List<string> lines;
    private List<DialogueScriptLine> scriptLines;
    private int linePosition = 0;
    private IDictionary<string, GameObject> characters;
    private IDictionary<string, DialogueActor> dialogueActors;

    private sealed class DialogueActor {
        public DialogueActor(string assetKey, string displayName, GameObject instance) {
            AssetKey = assetKey;
            DisplayName = displayName;
            Instance = instance;
        }

        public string AssetKey;
        public string DisplayName;
        public GameObject Instance;
    }

    [System.Obsolete("Dialogue positions are now relative to characterStage.")]
    public float baseYAxisOffset = -0.5f;
    [System.Obsolete("Dialogue positions are now relative to characterStage.")]
    public float positionalMultiplier = 50;
    public float characterStageYOffset = 0.5f;
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
    DialogueCommandRunner commandRunner;
    IDictionary<string, int> textLinkedMoveLengths;
    Text lineOfDialogue;
    Text nameText;

    // Start is called before the first frame update
    void Start()
    {

        if (dialogueToLoad == "changeover") {
            Destroy(mainCamera.GetComponent<AudioListener>());
        }
#if UNITY_EDITOR
        if (DebugDialogueRuntime.TryGetEditorDialogueOverride(out string debugDialogueId)) {
            dialogueToLoad = debugDialogueId;
        }
#endif
        isSkipping = false;
        dialogueVariables = new Dictionary<string, string>();
        textLinkedMoveLengths = new Dictionary<string, int>();

        //Get all variables
        dialogueVariables.Add("main", SaveGameService.GetString("name", "Rachel"));
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
        scriptLines = new List<DialogueScriptLine>();
        commandRunner = new DialogueCommandRunner(this);
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

        scriptLines = new DialogueScriptParser().Parse(dialogue, dialogueVariables);
        foreach (DialogueScriptLine line in scriptLines) {
            lines.Add(line.RawText);
            Debug.Log(line.RawText);
        }

        //Load Characters
        characters = new Dictionary<string, GameObject>();
        dialogueActors = new Dictionary<string, DialogueActor>();
        loadCharacters();
        linePosition++;

        //Load first dialogue
        fadeImage = fadeObject.GetComponent<Image>();
        fadeImage.enabled = false;
        displayImage.SetActive(false);
        backgound.GetComponent<Image>().sprite = Resources.Load<Sprite>("Backgrounds/" + SaveGameService.GetString("background", "classroomPlaceholder"));
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
            logText = logText.Replace("Main:", SaveGameService.GetString("name", "Rachel") + ":");
            log.GetComponent<Text>().text = logText;
            log.transform.parent.parent.gameObject.SetActive(true);
            */
            log.transform.parent.parent.gameObject.SetActive(true);


        }
    }

    //Trigger on click of background, advances dialogue
    public void advanceDialogue() {
        if (linePosition >= scriptLines.Count) {
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
                    entry.Value.GetComponent<DialogueCharacterMover>().SetPosition(entry.Value.GetComponent<DialogueCharacterMover>().GetTarget());
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
        DialogueScriptLine scriptLine = scriptLines[linePosition];
        if (scriptLine.Type == DialogueScriptLineType.Command) {
            commandRunner.Execute(scriptLine.Command);

            //Keep going until find next line
            linePosition++;
            advanceDialogue();
            return;
        }
        else {


            //Assign name
            string name = scriptLine.Speaker;
            //Change focus to speaker, could be more efficient, but meh
            foreach (KeyValuePair<string, GameObject> entry in characters) {
                if (entry.Key == name) {
                    //entry.Value.GetComponent<Image>().color = new Color32(255, 255, 225, 255);
                    entry.Value.GetComponent<FadeController>().setFadeIn();
                    entry.Value.transform.SetAsLastSibling();
                }
                else{
                    //entry.Value.GetComponent<Image>().color = new Color32(150, 150, 150, 255);
                    entry.Value.GetComponent<FadeController>().setFadeOut();
                }
            }
            string displayName = GetDisplayName(name);
            nameText.text = displayName;
            if (name == " ") { //For narration
                namePlate.SetActive(false);
            }
            else {
                namePlate.SetActive(true);
            }

            currentLine = scriptLine.Text;
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
        string line = scriptLines[linePosition].Command;

        //Split line into characters and their information
        List<string> characterNames = new List<string>(line.Split(','));

        //Loop through
        foreach (string i in characterNames) {
            List<string> characterInfo = new DialogueCommandTokenizer().Tokenize(i);
            //Load image
            string expression = "Normal";
            string displayName = DefaultDisplayName(characterInfo[0]);
            ParseSceneSetupOptions(characterInfo, ref expression, ref displayName);
            AddDialogueActor(characterInfo[0], displayName, expression);
            //Set none as focused
            characters[characterInfo[0]].GetComponent<Image>().color = new Color32(150, 150, 150, 255);

            //Position image
            //characters[characterInfo[0]].transform.SetParent(GameObject.Find("Characters").transform);
            float xPosition = float.Parse(characterInfo[1]);
            float yPosition = float.Parse(characterInfo[2]);
            SetCharacterStagePosition(characters[characterInfo[0]], xPosition, yPosition);
        }

    }

    public void TranslateCharacter(string character, float x, float y) {
        SetCharacterStagePosition(characters[character], x, y);
    }

    public void SetCharacterExpression(string character, string expression) {
        string assetKey = dialogueActors.ContainsKey(character) ? dialogueActors[character].AssetKey : character;
        characters[character].GetComponent<Image>().sprite = LoadCharacterSprite(assetKey, expression);
    }

    public void SetCharacterDisplayName(string character, string displayName) {
        if (!dialogueActors.ContainsKey(character)) {
            Debug.LogWarning("Cannot set display name for missing dialogue character '" + character + "'.");
            return;
        }

        dialogueActors[character].DisplayName = displayName;
    }

    public void SetTextLinkedMoveLength(string character, int lineCount) {
        textLinkedMoveLengths[character] = lineCount;
    }

    public void ExitCharacter(string character, string direction) {
        FadeController fadeController = characters[character].GetComponent<FadeController>();
        DialogueCharacterMover targetBehavior = EnsureDialogueMover(characters[character]);
        Vector2 currentPosition = GetStageRelativePosition(characters[character]);
        if (direction == "right") {
            targetBehavior.SetTarget(ToStageAnchoredPosition(1.2f, currentPosition.y));
        }
        else if (direction == "left") {
            targetBehavior.SetTarget(ToStageAnchoredPosition(-0.2f, currentPosition.y));
        }
        else if (direction == "down") {
            targetBehavior.SetTarget(ToStageAnchoredPosition(currentPosition.x, -0.35f));
        }

        targetBehavior.SetSpeed(0.05f);
        fadeController.scheduleForDeletion();
        characters.Remove(character);
        dialogueActors.Remove(character);
    }

    public void EnterCharacter(string characterName, float x, float y, string expression, string displayName) {
        string resolvedExpression = string.IsNullOrWhiteSpace(expression) ? "Normal" : expression;
        string resolvedDisplayName = string.IsNullOrWhiteSpace(displayName) ? DefaultDisplayName(characterName) : displayName;
        AddDialogueActor(characterName, resolvedDisplayName, resolvedExpression);
        characters[characterName].GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        characters[characterName].GetComponent<FadeController>().setFadeIn();

        SetCharacterStagePosition(characters[characterName], x, y);
    }

    public void TransitionBackground(string backgroundName) {
        fadeImage.enabled = true;
        nextBackground = Resources.Load<Sprite>("Backgrounds/" + backgroundName.Trim());
        SaveGameService.SetString("background", backgroundName.Trim());
        StartCoroutine(FadeToBlack());
    }

    public void SetBackground(string backgroundName) {
        backgound.GetComponent<Image>().sprite = Resources.Load<Sprite>("Backgrounds/" + backgroundName.Trim());
        SaveGameService.SetString("background", backgroundName.Trim());
    }

    public void PlayMusic(string trackName) {
        music.setTrack(trackName.Trim());
        music.play();
    }

    public void StopMusic() {
        music.stop();
    }

    public void DisplayImage(string resourcePath) {
        displayImage.SetActive(true);
        displayImage.GetComponent<Image>().sprite = Resources.Load<Sprite>(resourcePath.Trim());
    }

    public void StopDisplayImage() {
        displayImage.SetActive(false);
    }

    public void DisplayCG(string resourcePath) {
        CG.GetComponent<Image>().enabled = true;
        displayImage.GetComponent<Image>().sprite = Resources.Load<Sprite>(resourcePath.Trim());
    }

    public void StopDisplayCG() {
        CG.GetComponent<Image>().enabled = false;
    }

    public void SwapCharacters(string firstCharacter, string secondCharacter) {
        DialogueCharacterMover targetBehavior1 = EnsureDialogueMover(characters[firstCharacter.Trim()]);
        DialogueCharacterMover targetBehavior2 = EnsureDialogueMover(characters[secondCharacter.Trim()]);
        Vector2 firstTarget = targetBehavior1.GetTarget();
        Vector2 secondTarget = targetBehavior2.GetTarget();
        targetBehavior1.SetTarget(secondTarget, 1.5f);
        targetBehavior2.SetTarget(firstTarget, 1.5f);
    }

    public void MoveCharacter(string character, float speed, float xMove, float yMove) {
        DialogueCharacterMover targetBehavior = EnsureDialogueMover(characters[character]);
        Vector2 currentPosition = GetStageRelativePosition(targetBehavior.GetTarget());
        targetBehavior.SetTarget(ToStageAnchoredPosition(currentPosition.x + xMove, currentPosition.y + yMove), speed);
    }

    public void MoveCharacterTo(string character, float speed, float xTarget, float yTarget) {
        DialogueCharacterMover targetBehavior = EnsureDialogueMover(characters[character]);
        targetBehavior.SetTarget(ToStageAnchoredPosition(xTarget, yTarget), speed);

    }

    private RectTransform GetCharacterStage() {
        if (characterStage == null) {
            GameObject stage = GameObject.Find("Characters");
            if (stage != null) {
                characterStage = stage.GetComponent<RectTransform>();
            }
        }

        return characterStage;
    }

    private DialogueCharacterMover EnsureDialogueMover(GameObject character) {
        DialogueCharacterMover mover = character.GetComponent<DialogueCharacterMover>();
        if (mover == null) {
            Debug.LogError("Dialogue character '" + character.name + "' needs a DialogueCharacterMover component.");
        }

        return mover;
    }

    private void SetCharacterStagePosition(GameObject character, float x, float y) {
        EnsureDialogueMover(character).SetPosition(ToStageAnchoredPosition(x, y));
    }

    private void AddDialogueActor(string assetKey, string displayName, string expression) {
        GameObject actor = Instantiate(Resources.Load<GameObject>("Character"), GetCharacterStage());
        characters.Add(assetKey, actor);
        dialogueActors[assetKey] = new DialogueActor(assetKey, displayName, actor);
        EnsureDialogueMover(actor);
        actor.GetComponent<Image>().sprite = LoadCharacterSprite(assetKey, expression);
    }

    private static void ParseSceneSetupOptions(List<string> characterInfo, ref string expression, ref string displayName) {
        if (characterInfo.Count <= 3) {
            return;
        }

        if (!TryParseCharacterDisplayOptions(characterInfo, 3, ref expression, ref displayName)) {
            expression = characterInfo[3];
            if (characterInfo.Count > 4) {
                displayName = characterInfo[4];
            }
        }
    }

    private static bool TryParseCharacterDisplayOptions(List<string> arguments, int startIndex, ref string expression, ref string displayName) {
        int index = startIndex;
        while (index < arguments.Count) {
            if (index + 1 >= arguments.Count) {
                return false;
            }

            string key = NormalizeDisplayOptionKey(arguments[index]);
            string value = arguments[index + 1];
            if (key == "expression") {
                expression = value;
            }
            else if (key == "displayname" || key == "name") {
                displayName = value;
            }
            else {
                return false;
            }

            index += 2;
        }

        return true;
    }

    private static string NormalizeDisplayOptionKey(string key) {
        return key.Replace("-", "").Replace("_", "").ToLowerInvariant();
    }

    private Sprite LoadCharacterSprite(string assetKey, string expression) {
        Sprite sprite = Resources.Load<Sprite>("Characters/" + assetKey + expression);
        if (sprite == null) {
            sprite = Resources.Load<Sprite>("Characters/" + assetKey);
        }
        if (sprite == null) {
            sprite = Resources.Load<Sprite>("Characters/" + assetKey + "Normal");
        }

        return sprite;
    }

    private string GetDisplayName(string character) {
        if (character == " ") {
            return character;
        }

        if (character == "main") {
            return SaveGameService.GetString("name", "Rachel");
        }

        if (dialogueActors.ContainsKey(character) && !string.IsNullOrWhiteSpace(dialogueActors[character].DisplayName)) {
            return dialogueActors[character].DisplayName;
        }

        return DefaultDisplayName(character);
    }

    private static string DefaultDisplayName(string character) {
        if (string.IsNullOrEmpty(character)) {
            return character;
        }

        return char.ToUpper(character[0]) + character.Substring(1);
    }

    private Vector2 GetStageRelativePosition(GameObject character) {
        RectTransform rectTransform = character.GetComponent<RectTransform>();
        return GetStageRelativePosition(rectTransform.anchoredPosition);
    }

    private Vector2 GetStageRelativePosition(Vector2 anchoredPosition) {
        Rect stageRect = GetCharacterStage().rect;
        return new Vector2(
            (anchoredPosition.x / stageRect.width) + 0.5f,
            (anchoredPosition.y / stageRect.height) + 0.5f);
    }

    private Vector2 ToStageAnchoredPosition(float x, float y) {
        Rect stageRect = GetCharacterStage().rect;
        return new Vector2(
            (x - 0.5f) * stageRect.width,
            (y + characterStageYOffset - 0.5f) * stageRect.height);
    }
}
