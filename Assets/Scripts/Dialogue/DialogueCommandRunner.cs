public sealed class DialogueCommandRunner {
    private readonly DialogueController controller;
    private readonly DialogueCommandRegistry registry;
    private readonly DialogueCommandTokenizer tokenizer;

    public DialogueCommandRunner(DialogueController controller) {
        this.controller = controller;
        registry = new DialogueCommandRegistry();
        tokenizer = new DialogueCommandTokenizer();
    }

    public void Execute(string command) {
        registry.TryExecute(new DialogueCommandContext(controller, tokenizer.Tokenize(command)));
    }
}
