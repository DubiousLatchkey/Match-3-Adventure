using System.Collections.Generic;

public sealed class DialogueScriptParser {
    public List<DialogueScriptLine> Parse(string dialogueText, IDictionary<string, string> variables) {
        if (variables != null) {
            foreach (string key in variables.Keys) {
                dialogueText = dialogueText.Replace("@" + key + "@", variables[key]);
            }
        }

        List<DialogueScriptLine> lines = new List<DialogueScriptLine>();
        foreach (string rawLine in dialogueText.Split('\n')) {
            string line = rawLine.TrimEnd('\r');
            if (line.Length == 0) {
                continue;
            }

            if (line[0] == '|') {
                lines.Add(new DialogueScriptLine(
                    DialogueScriptLineType.Command,
                    line,
                    "",
                    "",
                    line.Substring(1)));
                continue;
            }

            int separator = line.IndexOf(':');
            if (separator < 0) {
                lines.Add(new DialogueScriptLine(DialogueScriptLineType.Dialogue, line, " ", line, ""));
                continue;
            }

            lines.Add(new DialogueScriptLine(
                DialogueScriptLineType.Dialogue,
                line,
                line.Substring(0, separator),
                line.Substring(separator + 1),
                ""));
        }

        return lines;
    }
}
