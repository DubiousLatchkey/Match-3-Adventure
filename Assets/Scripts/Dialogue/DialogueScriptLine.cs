public enum DialogueScriptLineType {
    Dialogue,
    Command
}

public sealed class DialogueScriptLine {
    public DialogueScriptLine(DialogueScriptLineType type, string rawText, string speaker, string text, string command) {
        Type = type;
        RawText = rawText;
        Speaker = speaker;
        Text = text;
        Command = command;
    }

    public DialogueScriptLineType Type { get; }
    public string RawText { get; }
    public string Speaker { get; }
    public string Text { get; }
    public string Command { get; }
}
