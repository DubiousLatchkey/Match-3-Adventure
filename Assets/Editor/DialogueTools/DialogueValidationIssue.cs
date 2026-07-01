#if UNITY_EDITOR
public enum DialogueValidationSeverity {
    Warning,
    Error
}

public sealed class DialogueValidationIssue {
    public DialogueValidationIssue(DialogueValidationSeverity severity, string assetPath, int lineNumber, string message) {
        Severity = severity;
        AssetPath = assetPath;
        LineNumber = lineNumber;
        Message = message;
    }

    public DialogueValidationSeverity Severity { get; }
    public string AssetPath { get; }
    public int LineNumber { get; }
    public string Message { get; }
}
#endif
