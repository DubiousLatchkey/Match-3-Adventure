using UnityEngine;

public static class DialogueTitleLoader {
    private static readonly DialogueCommandTokenizer Tokenizer = new DialogueCommandTokenizer();

    public static string GetTitle(string dialogueId) {
        if (string.IsNullOrWhiteSpace(dialogueId)) {
            return "";
        }

        TextAsset asset = Resources.Load<TextAsset>("Dialogues/" + dialogueId);
        if (asset == null) {
            return dialogueId;
        }

        foreach (string rawLine in asset.text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')) {
            string line = rawLine.Trim();
            if (line.Length == 0) {
                continue;
            }

            if (!line.StartsWith("|title")) {
                continue;
            }

            System.Collections.Generic.List<string> tokens = Tokenizer.Tokenize(line.Substring(1));
            if (tokens.Count > 1) {
                return string.Join(" ", tokens.GetRange(1, tokens.Count - 1));
            }
        }

        return dialogueId;
    }
}
