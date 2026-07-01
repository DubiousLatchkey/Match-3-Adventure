#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class DialogueEditorWindow : EditorWindow {
    private const string DialogueScenePath = "Assets/Scenes/DialogueScene.unity";
    private const string DialogueFolder = "Assets/Resources/Dialogues";

    private readonly List<DialogueBlock> blocks = new List<DialogueBlock>();
    private readonly DialogueCommandTokenizer tokenizer = new DialogueCommandTokenizer();
    private TextAsset dialogueAsset;
    private Vector2 listScroll;
    private Vector2 editScroll;
    private int selectedIndex = -1;
    private List<DialogueValidationIssue> validationIssues = new List<DialogueValidationIssue>();
    private string newDialogueName = "newDialogue";

    [MenuItem("Tools/Dialogue/Dialogue Editor")]
    public static void Open() {
        GetWindow<DialogueEditorWindow>("Dialogue Editor");
    }

    private void OnGUI() {
        DrawToolbar();
        EditorGUILayout.Space(6);

        using (new EditorGUILayout.HorizontalScope()) {
            DrawBlockList();
            DrawSelectedBlock();
        }
    }

    private void DrawToolbar() {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar)) {
            TextAsset selectedAsset = (TextAsset)EditorGUILayout.ObjectField(dialogueAsset, typeof(TextAsset), false, GUILayout.MinWidth(220));
            if (selectedAsset != dialogueAsset) {
                LoadAsset(selectedAsset);
            }

            if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(60))) {
                LoadAsset(dialogueAsset);
            }

            if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(60))) {
                Save();
            }

            if (GUILayout.Button("Validate", EditorStyles.toolbarButton, GUILayout.Width(75))) {
                ValidateCurrent();
            }

            if (GUILayout.Button("Play", EditorStyles.toolbarButton, GUILayout.Width(55))) {
                PlayCurrent();
            }

            GUILayout.FlexibleSpace();
            newDialogueName = GUILayout.TextField(newDialogueName, EditorStyles.toolbarTextField, GUILayout.Width(140));
            if (GUILayout.Button("Create", EditorStyles.toolbarButton, GUILayout.Width(60))) {
                CreateDialogue();
            }
        }
    }

    private void DrawBlockList() {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(360))) {
            EditorGUILayout.LabelField("Blocks", EditorStyles.boldLabel);
            listScroll = EditorGUILayout.BeginScrollView(listScroll);
            for (int i = 0; i < blocks.Count; i++) {
                DialogueBlock block = blocks[i];
                GUIStyle style = i == selectedIndex ? EditorStyles.helpBox : EditorStyles.miniButton;
                if (GUILayout.Button((i + 1) + ". " + block.GetLabel(), style, GUILayout.MinHeight(28))) {
                    selectedIndex = i;
                }
            }
            EditorGUILayout.EndScrollView();

            using (new EditorGUILayout.HorizontalScope()) {
                if (GUILayout.Button("+ Dialogue")) {
                    InsertBlock(DialogueBlock.Dialogue("main", ""));
                }
                if (GUILayout.Button("+ Command")) {
                    InsertBlock(DialogueBlock.Command("setExpression", new List<string> { "main", "Normal" }));
                }
            }

            using (new EditorGUILayout.HorizontalScope()) {
                GUI.enabled = selectedIndex > 0;
                if (GUILayout.Button("Up")) {
                    MoveSelected(-1);
                }
                GUI.enabled = selectedIndex >= 0 && selectedIndex < blocks.Count - 1;
                if (GUILayout.Button("Down")) {
                    MoveSelected(1);
                }
                GUI.enabled = selectedIndex >= 0;
                if (GUILayout.Button("Delete")) {
                    blocks.RemoveAt(selectedIndex);
                    selectedIndex = Mathf.Clamp(selectedIndex, 0, blocks.Count - 1);
                }
                GUI.enabled = true;
            }
        }
    }

    private void DrawSelectedBlock() {
        using (new EditorGUILayout.VerticalScope()) {
            EditorGUILayout.LabelField("Edit", EditorStyles.boldLabel);
            editScroll = EditorGUILayout.BeginScrollView(editScroll);

            if (selectedIndex < 0 || selectedIndex >= blocks.Count) {
                EditorGUILayout.HelpBox("Select a block to edit.", MessageType.Info);
                DrawValidationPanel();
                EditorGUILayout.EndScrollView();
                return;
            }

            DialogueBlock block = blocks[selectedIndex];
            block.Type = (DialogueBlockType)EditorGUILayout.EnumPopup("Type", block.Type);
            if (block.Type == DialogueBlockType.CharacterSetup) {
                EditorGUILayout.HelpBox("Initial character setup. Format: name x y,name x y", MessageType.Info);
                block.Text = EditorGUILayout.TextField("Characters", block.Text);
            }
            else if (block.Type == DialogueBlockType.Dialogue) {
                block.Speaker = EditorGUILayout.TextField("Speaker", block.Speaker);
                EditorGUILayout.LabelField("Text");
                block.Text = EditorGUILayout.TextArea(block.Text, GUILayout.MinHeight(90));
            }
            else {
                DrawCommandEditor(block);
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Serialized Line", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(block.ToLine(), EditorStyles.textField, GUILayout.Height(22));
            DrawValidationPanel();
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawCommandEditor(DialogueBlock block) {
        string[] commandNames = DialogueCommandCatalog.All.Select(spec => spec.Name).ToArray();
        int currentIndex = Mathf.Max(0, System.Array.IndexOf(commandNames, block.CommandName));
        int newIndex = EditorGUILayout.Popup("Command", currentIndex, commandNames);
        if (newIndex != currentIndex || string.IsNullOrWhiteSpace(block.CommandName)) {
            block.CommandName = commandNames[newIndex];
            block.Arguments = CreateBlankArguments(block.CommandName);
        }

        if (!DialogueCommandCatalog.TryGet(block.CommandName, out DialogueCommandSpec spec)) {
            EditorGUILayout.HelpBox("Unknown command. Edit raw line by switching this block back through text if needed.", MessageType.Warning);
            return;
        }

        EnsureArgumentCount(block, spec);
        for (int i = 0; i < block.Arguments.Count; i++) {
            string label = i < spec.Arguments.Count ? spec.Arguments[i].Name : "extra " + (i + 1);
            block.Arguments[i] = EditorGUILayout.TextField(label, block.Arguments[i]);
        }
    }

    private void DrawValidationPanel() {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
        if (validationIssues.Count == 0) {
            EditorGUILayout.HelpBox("No validation messages for the last run.", MessageType.Info);
            return;
        }

        foreach (DialogueValidationIssue issue in validationIssues.Take(30)) {
            MessageType type = issue.Severity == DialogueValidationSeverity.Error ? MessageType.Error : MessageType.Warning;
            EditorGUILayout.HelpBox("Line " + issue.LineNumber + ": " + issue.Message, type);
        }
    }

    private void LoadAsset(TextAsset asset) {
        dialogueAsset = asset;
        blocks.Clear();
        selectedIndex = -1;
        validationIssues.Clear();

        if (dialogueAsset == null) {
            return;
        }

        foreach (string line in dialogueAsset.text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')) {
            if (string.IsNullOrWhiteSpace(line)) {
                continue;
            }

            blocks.Add(ParseLine(line, blocks.Count == 0));
        }

        if (blocks.Count > 0) {
            selectedIndex = 0;
        }
    }

    private DialogueBlock ParseLine(string line, bool isFirstBlock) {
        if (line.StartsWith("|")) {
            if (isFirstBlock) {
                return DialogueBlock.CharacterSetup(line.Substring(1));
            }

            List<string> tokens = tokenizer.Tokenize(line.Substring(1));
            if (tokens.Count == 0) {
                return DialogueBlock.Command("", new List<string>());
            }

            return DialogueBlock.Command(tokens[0], tokens.Skip(1).ToList());
        }

        int separator = line.IndexOf(':');
        if (separator < 0) {
            return DialogueBlock.Dialogue(" ", line);
        }

        return DialogueBlock.Dialogue(line.Substring(0, separator), line.Substring(separator + 1));
    }

    private void InsertBlock(DialogueBlock block) {
        int insertIndex = selectedIndex >= 0 ? selectedIndex + 1 : blocks.Count;
        blocks.Insert(insertIndex, block);
        selectedIndex = insertIndex;
    }

    private void MoveSelected(int direction) {
        int newIndex = selectedIndex + direction;
        DialogueBlock selected = blocks[selectedIndex];
        blocks.RemoveAt(selectedIndex);
        blocks.Insert(newIndex, selected);
        selectedIndex = newIndex;
    }

    private void Save() {
        if (dialogueAsset == null) {
            Debug.LogWarning("Select or create a dialogue asset before saving.");
            return;
        }

        string path = AssetDatabase.GetAssetPath(dialogueAsset);
        File.WriteAllText(path, string.Join("\n", blocks.Select(block => block.ToLine())) + "\n");
        AssetDatabase.ImportAsset(path);
        dialogueAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        ValidateCurrent();
    }

    private void ValidateCurrent() {
        string path = dialogueAsset != null ? AssetDatabase.GetAssetPath(dialogueAsset) : "Unsaved dialogue";
        validationIssues = DialogueScriptValidator.ValidateText(string.Join("\n", blocks.Select(block => block.ToLine())), path);
        DialogueScriptValidator.LogIssues(validationIssues);
    }

    private void PlayCurrent() {
        Save();
        if (dialogueAsset == null) {
            return;
        }

        string path = AssetDatabase.GetAssetPath(dialogueAsset);
        string dialogueId = GetDialogueResourceId(path);
        if (string.IsNullOrWhiteSpace(dialogueId)) {
            Debug.LogError("Dialogue must be under " + DialogueFolder + " to run in DialogueScene.");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) {
            return;
        }

        EditorSceneManager.OpenScene(DialogueScenePath);
        DialogueController.dialogueToLoad = dialogueId;
        EditorApplication.isPlaying = true;
    }

    private void CreateDialogue() {
        string safeName = string.IsNullOrWhiteSpace(newDialogueName) ? "newDialogue" : newDialogueName.Trim();
        Directory.CreateDirectory(DialogueFolder);
        string path = AssetDatabase.GenerateUniqueAssetPath(DialogueFolder + "/" + safeName + ".txt");
        File.WriteAllText(path, "|main 0 -1\nmain:\n");
        AssetDatabase.ImportAsset(path);
        LoadAsset(AssetDatabase.LoadAssetAtPath<TextAsset>(path));
        Selection.activeObject = dialogueAsset;
    }

    private static string GetDialogueResourceId(string assetPath) {
        string normalized = assetPath.Replace('\\', '/');
        string prefix = DialogueFolder + "/";
        if (!normalized.StartsWith(prefix)) {
            return "";
        }

        return Path.ChangeExtension(normalized.Substring(prefix.Length), null);
    }

    private static List<string> CreateBlankArguments(string commandName) {
        if (!DialogueCommandCatalog.TryGet(commandName, out DialogueCommandSpec spec)) {
            return new List<string>();
        }

        return spec.Arguments.Select(argument => "").ToList();
    }

    private static void EnsureArgumentCount(DialogueBlock block, DialogueCommandSpec spec) {
        while (block.Arguments.Count < spec.Arguments.Count) {
            block.Arguments.Add("");
        }
    }

    private enum DialogueBlockType {
        CharacterSetup,
        Dialogue,
        Command
    }

    private sealed class DialogueBlock {
        public DialogueBlockType Type;
        public string Speaker;
        public string Text;
        public string CommandName;
        public List<string> Arguments;

        public static DialogueBlock CharacterSetup(string text) {
            return new DialogueBlock {
                Type = DialogueBlockType.CharacterSetup,
                Text = text,
                Arguments = new List<string>()
            };
        }

        public static DialogueBlock Dialogue(string speaker, string text) {
            return new DialogueBlock {
                Type = DialogueBlockType.Dialogue,
                Speaker = speaker,
                Text = text,
                Arguments = new List<string>()
            };
        }

        public static DialogueBlock Command(string commandName, List<string> arguments) {
            return new DialogueBlock {
                Type = DialogueBlockType.Command,
                CommandName = commandName,
                Arguments = arguments
            };
        }

        public string GetLabel() {
            if (Type == DialogueBlockType.CharacterSetup) {
                return "|setup " + Truncate(Text, 54);
            }

            if (Type == DialogueBlockType.Dialogue) {
                string labelSpeaker = string.IsNullOrWhiteSpace(Speaker) ? "narration" : Speaker.Trim();
                string preview = string.IsNullOrWhiteSpace(Text) ? "(empty)" : Text.Trim();
                return labelSpeaker + ": " + Truncate(preview, 42);
            }

            return "|" + CommandName + " " + Truncate(string.Join(" ", Arguments), 48);
        }

        public string ToLine() {
            if (Type == DialogueBlockType.CharacterSetup) {
                return "|" + Text;
            }

            if (Type == DialogueBlockType.Dialogue) {
                return Speaker + ":" + Text;
            }

            List<string> parts = new List<string> { CommandName };
            parts.AddRange(Arguments.Where(argument => argument != null).Select(QuoteIfNeeded));
            return "|" + string.Join(" ", parts).TrimEnd();
        }

        private static string QuoteIfNeeded(string value) {
            if (string.IsNullOrEmpty(value)) {
                return "\"\"";
            }

            if (!value.Any(char.IsWhiteSpace) && !value.Contains("\"")) {
                return value;
            }

            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        private static string Truncate(string text, int maxLength) {
            if (text.Length <= maxLength) {
                return text;
            }

            return text.Substring(0, maxLength - 3) + "...";
        }
    }
}
#endif
