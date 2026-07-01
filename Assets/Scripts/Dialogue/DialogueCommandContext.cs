using System.Collections.Generic;
using System.Text;

public sealed class DialogueCommandContext {
    public DialogueCommandContext(DialogueController controller, IReadOnlyList<string> arguments) {
        Controller = controller;
        Arguments = arguments;
    }

    public DialogueController Controller { get; }
    public IReadOnlyList<string> Arguments { get; }

    public string RequireString(int index, string commandName) {
        if (index >= Arguments.Count) {
            throw new System.ArgumentException(commandName + " missing argument " + index);
        }

        return Arguments[index];
    }

    public float RequireFloat(int index, string commandName) {
        string value = RequireString(index, commandName);
        if (!float.TryParse(value, out float parsed)) {
            throw new System.ArgumentException(commandName + " argument " + index + " must be a number: " + value);
        }

        return parsed;
    }

    public int RequireInt(int index, string commandName) {
        string value = RequireString(index, commandName);
        if (!int.TryParse(value, out int parsed)) {
            throw new System.ArgumentException(commandName + " argument " + index + " must be an integer: " + value);
        }

        return parsed;
    }

    public string JoinArguments(int startIndex) {
        return JoinArguments(startIndex, Arguments.Count - startIndex);
    }

    public string JoinArguments(int startIndex, int count) {
        if (startIndex >= Arguments.Count || count <= 0) {
            return "";
        }

        StringBuilder result = new StringBuilder();
        int endIndex = System.Math.Min(Arguments.Count, startIndex + count);
        for (int i = startIndex; i < endIndex; i++) {
            if (result.Length > 0) {
                result.Append(" ");
            }

            result.Append(Arguments[i]);
        }

        return result.ToString().Trim();
    }
}
