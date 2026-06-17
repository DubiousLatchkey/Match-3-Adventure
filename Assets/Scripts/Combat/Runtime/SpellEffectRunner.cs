using System.Collections.Generic;

public sealed class SpellEffectRunner {
    private readonly System.Action<string> executeCommand;

    public SpellEffectRunner(System.Action<string> executeCommand) {
        this.executeCommand = executeCommand;
    }

    public void Execute(Spell spell) {
        foreach (string parameter in ParseParameters(spell)) {
            executeCommand(parameter);
        }
    }

    public static List<string> ParseParameters(Spell spell) {
        List<string> parameters = new List<string>();
        foreach (string parameter in spell.Parameters.Split('\n', '+')) {
            string trimmed = parameter.Trim();
            if (trimmed.Length > 0) {
                parameters.Add(trimmed);
            }
        }
        return parameters;
    }
}
