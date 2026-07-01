#if UNITY_EDITOR
using System.Collections.Generic;

public enum DialogueArgumentType {
    Text,
    Integer,
    Number,
    Character,
    Direction,
    ResourceName
}

public sealed class DialogueCommandSpec {
    public DialogueCommandSpec(string name, string category, params DialogueArgumentSpec[] arguments) {
        Name = name;
        Category = category;
        Arguments = arguments;
    }

    public string Name { get; }
    public string Category { get; }
    public IReadOnlyList<DialogueArgumentSpec> Arguments { get; }
}

public sealed class DialogueArgumentSpec {
    public DialogueArgumentSpec(string name, DialogueArgumentType type, bool consumeRemaining = false, bool optional = false) {
        Name = name;
        Type = type;
        ConsumeRemaining = consumeRemaining;
        Optional = optional;
    }

    public string Name { get; }
    public DialogueArgumentType Type { get; }
    public bool ConsumeRemaining { get; }
    public bool Optional { get; }
}
#endif
