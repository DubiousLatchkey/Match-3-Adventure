using System.Collections.Generic;
using System.Text;

public sealed class DialogueCommandTokenizer {
    public List<string> Tokenize(string command) {
        List<string> tokens = new List<string>();
        StringBuilder token = new StringBuilder();
        bool inQuotes = false;
        bool escaping = false;

        foreach (char c in command) {
            if (escaping) {
                token.Append(c);
                escaping = false;
                continue;
            }

            if (c == '\\' && inQuotes) {
                escaping = true;
                continue;
            }

            if (c == '"') {
                inQuotes = !inQuotes;
                continue;
            }

            if (!inQuotes && char.IsWhiteSpace(c)) {
                AddToken(tokens, token);
                continue;
            }

            token.Append(c);
        }

        AddToken(tokens, token);
        return tokens;
    }

    private static void AddToken(List<string> tokens, StringBuilder token) {
        if (token.Length == 0) {
            return;
        }

        tokens.Add(token.ToString());
        token.Length = 0;
    }
}
