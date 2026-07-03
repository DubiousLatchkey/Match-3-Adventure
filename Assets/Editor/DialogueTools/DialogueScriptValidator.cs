#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class DialogueScriptValidator {
    private static readonly Regex VariableRegex = new Regex("@[^@\\s]+@", RegexOptions.Compiled);

    [MenuItem("Tools/Dialogue/Validate All Dialogue")]
    public static void ValidateAllDialogueMenu() {
        List<DialogueValidationIssue> issues = ValidateAllDialogue();
        LogIssues(issues);
    }

    public static List<DialogueValidationIssue> ValidateAllDialogue() {
        List<DialogueValidationIssue> issues = new List<DialogueValidationIssue>();
        foreach (string guid in AssetDatabase.FindAssets("t:TextAsset", new[] { "Assets/Resources/Dialogues" })) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (asset != null) {
                issues.AddRange(Validate(asset, path));
            }
        }

        return issues;
    }

    public static List<DialogueValidationIssue> Validate(TextAsset asset) {
        string path = AssetDatabase.GetAssetPath(asset);
        return Validate(asset, path);
    }

    public static List<DialogueValidationIssue> Validate(TextAsset asset, string assetPath) {
        return ValidateText(asset != null ? asset.text : "", assetPath);
    }

    public static List<DialogueValidationIssue> ValidateText(string text, string assetPath) {
        List<DialogueValidationIssue> issues = new List<DialogueValidationIssue>();
        DialogueCommandTokenizer tokenizer = new DialogueCommandTokenizer();
        HashSet<string> characters = new HashSet<string>();
        bool sawContent = false;
        bool sawInitialSetup = false;

        string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        for (int i = 0; i < lines.Length; i++) {
            int lineNumber = i + 1;
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) {
                continue;
            }

            sawContent = true;
            foreach (Match match in VariableRegex.Matches(line)) {
                Add(issues, DialogueValidationSeverity.Warning, assetPath, lineNumber, "Unresolved variable token " + match.Value + " may need test values.");
            }

            if (line.StartsWith("|")) {
                string commandText = line.Substring(1).Trim();
                if (!sawInitialSetup) {
                    ValidateInitialSetup(commandText, assetPath, lineNumber, issues, characters);
                    sawInitialSetup = true;
                    continue;
                }

                ValidateCommand(commandText, assetPath, lineNumber, issues, characters, tokenizer);
                continue;
            }

            int separator = line.IndexOf(':');
            if (separator < 0) {
                Add(issues, DialogueValidationSeverity.Warning, assetPath, lineNumber, "Dialogue line has no speaker separator ':'. It will display as narration-like text.");
                continue;
            }

            string speaker = line.Substring(0, separator).Trim();
            if (speaker.Length > 0 && speaker != "main" && speaker != "???" && !characters.Contains(speaker)) {
                Add(issues, DialogueValidationSeverity.Warning, assetPath, lineNumber, "Speaker '" + speaker + "' is not currently known from setup or enter commands.");
            }
        }

        if (!sawContent) {
            Add(issues, DialogueValidationSeverity.Warning, assetPath, 1, "Dialogue file is empty.");
        }

        return issues;
    }

    public static void LogIssues(IReadOnlyList<DialogueValidationIssue> issues) {
        if (issues.Count == 0) {
            Debug.Log("Dialogue validation passed.");
            return;
        }

        foreach (DialogueValidationIssue issue in issues) {
            string message = issue.AssetPath + ":" + issue.LineNumber + " " + issue.Message;
            if (issue.Severity == DialogueValidationSeverity.Error) {
                Debug.LogError(message);
            }
            else {
                Debug.LogWarning(message);
            }
        }
    }

    private static void ValidateInitialSetup(string commandText, string assetPath, int lineNumber, List<DialogueValidationIssue> issues, HashSet<string> characters) {
        if (string.IsNullOrWhiteSpace(commandText)) {
            Add(issues, DialogueValidationSeverity.Error, assetPath, lineNumber, "Initial character setup is empty.");
            return;
        }

        foreach (string rawCharacter in commandText.Split(',')) {
            string[] parts = rawCharacter.Trim().Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3) {
                Add(issues, DialogueValidationSeverity.Error, assetPath, lineNumber, "Initial character entry '" + rawCharacter.Trim() + "' should be '<name> <x> <y>'.");
                continue;
            }

            characters.Add(parts[0]);
            ValidateNumber(parts[1], assetPath, lineNumber, "Initial character x position", issues);
            ValidateNumber(parts[2], assetPath, lineNumber, "Initial character y position", issues);
            ValidateResource<Sprite>("Assets/Resources/Characters/" + parts[0] + "Normal", assetPath, lineNumber, "Character sprite", issues);
        }
    }

    private static void ValidateCommand(string commandText, string assetPath, int lineNumber, List<DialogueValidationIssue> issues, HashSet<string> characters, DialogueCommandTokenizer tokenizer) {
        List<string> tokens = tokenizer.Tokenize(commandText);
        if (tokens.Count == 0) {
            Add(issues, DialogueValidationSeverity.Error, assetPath, lineNumber, "Command line is empty.");
            return;
        }

        string commandName = tokens[0];
        if (!DialogueCommandCatalog.TryGet(commandName, out DialogueCommandSpec spec)) {
            Add(issues, DialogueValidationSeverity.Error, assetPath, lineNumber, "Unknown dialogue command '" + commandName + "'.");
            return;
        }

        ValidateArgumentCount(spec, tokens, assetPath, lineNumber, issues);
        ValidateArgumentValues(spec, tokens, assetPath, lineNumber, issues, characters);
        TrackCharacterChanges(commandName, tokens, characters);
    }

    private static void ValidateArgumentCount(DialogueCommandSpec spec, List<string> tokens, string assetPath, int lineNumber, List<DialogueValidationIssue> issues) {
        int required = spec.Arguments.Count(argument => !argument.Optional);
        int provided = tokens.Count - 1;
        bool consumesRemaining = spec.Arguments.Any(argument => argument.ConsumeRemaining);

        if (provided < required) {
            Add(issues, DialogueValidationSeverity.Error, assetPath, lineNumber, spec.Name + " expects at least " + required + " argument(s), got " + provided + ".");
        }

        if (!consumesRemaining && provided > spec.Arguments.Count) {
            Add(issues, DialogueValidationSeverity.Warning, assetPath, lineNumber, spec.Name + " received extra argument(s).");
        }
    }

    private static void ValidateArgumentValues(DialogueCommandSpec spec, List<string> tokens, string assetPath, int lineNumber, List<DialogueValidationIssue> issues, HashSet<string> characters) {
        for (int i = 0; i < spec.Arguments.Count; i++) {
            int tokenIndex = i + 1;
            if (tokenIndex >= tokens.Count) {
                break;
            }

            DialogueArgumentSpec argument = spec.Arguments[i];
            string value = tokens[tokenIndex];
            if (argument.ConsumeRemaining && spec.Name == "setPrefValue") {
                value = tokens[tokens.Count - 1];
                ValidateInteger(value, assetPath, lineNumber, argument.Name, issues);
                break;
            }
            if (argument.ConsumeRemaining && spec.Name == "setFlag") {
                break;
            }

            switch (argument.Type) {
                case DialogueArgumentType.Integer:
                    ValidateInteger(value, assetPath, lineNumber, argument.Name, issues);
                    break;
                case DialogueArgumentType.Number:
                    ValidateNumber(value, assetPath, lineNumber, argument.Name, issues);
                    break;
                case DialogueArgumentType.Character:
                    if (!characters.Contains(value) && value != "main") {
                        Add(issues, DialogueValidationSeverity.Warning, assetPath, lineNumber, argument.Name + " '" + value + "' is not currently known.");
                    }
                    break;
                case DialogueArgumentType.Direction:
                    if (value != "left" && value != "right" && value != "down") {
                        Add(issues, DialogueValidationSeverity.Error, assetPath, lineNumber, "Direction should be left, right, or down.");
                    }
                    break;
            }
        }

        ValidateReferencedResources(spec.Name, tokens, assetPath, lineNumber, issues);
    }

    private static void ValidateReferencedResources(string commandName, List<string> tokens, string assetPath, int lineNumber, List<DialogueValidationIssue> issues) {
        if ((commandName == "transition" || commandName == "setBackground") && tokens.Count > 1) {
            ValidateResource<Sprite>("Assets/Resources/Backgrounds/" + tokens[1], assetPath, lineNumber, "Background", issues);
        }
        else if (commandName == "setExpression" && tokens.Count > 2) {
            ValidateResource<Sprite>("Assets/Resources/Characters/" + tokens[1] + tokens[2], assetPath, lineNumber, "Expression sprite", issues);
        }
        else if (commandName == "enter" && tokens.Count > 3) {
            string suffix = tokens.Count < 5 ? "Normal" : "";
            ValidateResource<Sprite>("Assets/Resources/Characters/" + tokens[1] + suffix, assetPath, lineNumber, "Enter sprite", issues);
        }
        else if ((commandName == "combat" || commandName == "combatDirect") && tokens.Count > 1) {
            ValidateResource<TextAsset>("Assets/Resources/Combats/" + tokens[1], assetPath, lineNumber, "Combat script", issues);
        }
        else if (commandName == "dialogue" && tokens.Count > 1) {
            ValidateResource<TextAsset>("Assets/Resources/Dialogues/" + tokens[1], assetPath, lineNumber, "Dialogue script", issues);
        }
        else if (commandName == "gearStop" && tokens.Count > 1) {
            ValidateResource<TextAsset>("Assets/Resources/Data/GearStops/" + tokens[1], assetPath, lineNumber, "Gear stop", issues);
        }
    }

    private static void TrackCharacterChanges(string commandName, List<string> tokens, HashSet<string> characters) {
        if (commandName == "enter" && tokens.Count > 3) {
            string alias = tokens.Count > 5 ? tokens[5] : tokens[1];
            characters.Add(alias);
        }
        else if (commandName == "exit" && tokens.Count > 1) {
            characters.Remove(tokens[1]);
        }
    }

    private static void ValidateInteger(string value, string assetPath, int lineNumber, string label, List<DialogueValidationIssue> issues) {
        if (!int.TryParse(value, out int _)) {
            Add(issues, DialogueValidationSeverity.Error, assetPath, lineNumber, label + " should be an integer.");
        }
    }

    private static void ValidateNumber(string value, string assetPath, int lineNumber, string label, List<DialogueValidationIssue> issues) {
        if (!float.TryParse(value, out float _)) {
            Add(issues, DialogueValidationSeverity.Error, assetPath, lineNumber, label + " should be a number.");
        }
    }

    private static void ValidateResource<T>(string resourcePathWithoutExtension, string assetPath, int lineNumber, string label, List<DialogueValidationIssue> issues) where T : Object {
        string[] guids = AssetDatabase.FindAssets(Path.GetFileName(resourcePathWithoutExtension), new[] { Path.GetDirectoryName(resourcePathWithoutExtension).Replace('\\', '/') });
        bool found = guids.Any(guid => {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            return Path.ChangeExtension(path, null).Replace('\\', '/') == resourcePathWithoutExtension;
        });

        if (!found) {
            Add(issues, DialogueValidationSeverity.Warning, assetPath, lineNumber, label + " resource not found: " + resourcePathWithoutExtension);
        }
    }

    private static void Add(List<DialogueValidationIssue> issues, DialogueValidationSeverity severity, string assetPath, int lineNumber, string message) {
        issues.Add(new DialogueValidationIssue(severity, assetPath, lineNumber, message));
    }
}
#endif
