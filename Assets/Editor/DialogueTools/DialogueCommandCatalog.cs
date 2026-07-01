#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;

public static class DialogueCommandCatalog {
    private static readonly List<DialogueCommandSpec> Specs = new List<DialogueCommandSpec> {
        new DialogueCommandSpec("translate", "Character",
            new DialogueArgumentSpec("character", DialogueArgumentType.Character),
            new DialogueArgumentSpec("x", DialogueArgumentType.Number),
            new DialogueArgumentSpec("y", DialogueArgumentType.Number)),
        new DialogueCommandSpec("setExpression", "Character",
            new DialogueArgumentSpec("character", DialogueArgumentType.Character),
            new DialogueArgumentSpec("expression", DialogueArgumentType.ResourceName)),
        new DialogueCommandSpec("move", "Character",
            new DialogueArgumentSpec("character", DialogueArgumentType.Character),
            new DialogueArgumentSpec("speed", DialogueArgumentType.Number),
            new DialogueArgumentSpec("x", DialogueArgumentType.Number),
            new DialogueArgumentSpec("y", DialogueArgumentType.Number),
            new DialogueArgumentSpec("linked lines", DialogueArgumentType.Integer)),
        new DialogueCommandSpec("moveTo", "Character",
            new DialogueArgumentSpec("character", DialogueArgumentType.Character),
            new DialogueArgumentSpec("speed", DialogueArgumentType.Number),
            new DialogueArgumentSpec("x", DialogueArgumentType.Number),
            new DialogueArgumentSpec("y", DialogueArgumentType.Number),
            new DialogueArgumentSpec("linked lines", DialogueArgumentType.Integer)),
        new DialogueCommandSpec("exit", "Character",
            new DialogueArgumentSpec("character", DialogueArgumentType.Character),
            new DialogueArgumentSpec("direction", DialogueArgumentType.Direction)),
        new DialogueCommandSpec("enter", "Character",
            new DialogueArgumentSpec("character sprite", DialogueArgumentType.ResourceName),
            new DialogueArgumentSpec("x", DialogueArgumentType.Number),
            new DialogueArgumentSpec("y", DialogueArgumentType.Number),
            new DialogueArgumentSpec("mode", DialogueArgumentType.Text, optional: true),
            new DialogueArgumentSpec("alias", DialogueArgumentType.Character, optional: true)),
        new DialogueCommandSpec("combat", "Flow",
            new DialogueArgumentSpec("combat id", DialogueArgumentType.ResourceName)),
        new DialogueCommandSpec("combatDirect", "Flow",
            new DialogueArgumentSpec("combat id", DialogueArgumentType.ResourceName)),
        new DialogueCommandSpec("resumeCombat", "Flow"),
        new DialogueCommandSpec("transition", "Scene",
            new DialogueArgumentSpec("background", DialogueArgumentType.ResourceName)),
        new DialogueCommandSpec("setBackground", "Scene",
            new DialogueArgumentSpec("background", DialogueArgumentType.ResourceName)),
        new DialogueCommandSpec("setPrefValue", "Progression",
            new DialogueArgumentSpec("key", DialogueArgumentType.Text, consumeRemaining: true),
            new DialogueArgumentSpec("value", DialogueArgumentType.Integer)),
        new DialogueCommandSpec("setFlag", "Progression",
            new DialogueArgumentSpec("key", DialogueArgumentType.Text, consumeRemaining: true),
            new DialogueArgumentSpec("value", DialogueArgumentType.Text)),
        new DialogueCommandSpec("playMusic", "Audio",
            new DialogueArgumentSpec("track", DialogueArgumentType.Text, consumeRemaining: true)),
        new DialogueCommandSpec("stopMusic", "Audio"),
        new DialogueCommandSpec("playSound", "Audio",
            new DialogueArgumentSpec("sound", DialogueArgumentType.Text, consumeRemaining: true)),
        new DialogueCommandSpec("display", "Visual",
            new DialogueArgumentSpec("image resource", DialogueArgumentType.Text, consumeRemaining: true)),
        new DialogueCommandSpec("stopDisplay", "Visual"),
        new DialogueCommandSpec("displayCG", "Visual",
            new DialogueArgumentSpec("image resource", DialogueArgumentType.Text, consumeRemaining: true)),
        new DialogueCommandSpec("stopDisplayCG", "Visual"),
        new DialogueCommandSpec("swap", "Character",
            new DialogueArgumentSpec("first character", DialogueArgumentType.Character),
            new DialogueArgumentSpec("second character", DialogueArgumentType.Character)),
    };

    private static readonly Dictionary<string, DialogueCommandSpec> SpecsByName = Specs.ToDictionary(spec => spec.Name);

    public static IReadOnlyList<DialogueCommandSpec> All => Specs;

    public static bool TryGet(string name, out DialogueCommandSpec spec) {
        return SpecsByName.TryGetValue(name, out spec);
    }
}
#endif
