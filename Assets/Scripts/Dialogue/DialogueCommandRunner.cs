public sealed class DialogueCommandRunner {
    private readonly DialogueController controller;

    public DialogueCommandRunner(DialogueController controller) {
        this.controller = controller;
    }

    public void Execute(string command) {
        controller.performAction(command);
    }
}
