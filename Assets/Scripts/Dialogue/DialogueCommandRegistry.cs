using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class DialogueCommandRegistry {
    private readonly Dictionary<string, Action<DialogueCommandContext>> commands =
        new Dictionary<string, Action<DialogueCommandContext>>();

    public DialogueCommandRegistry() {
        RegisterDefaults();
    }

    public bool TryExecute(DialogueCommandContext context) {
        if (context.Arguments.Count == 0) {
            return false;
        }

        string commandName = context.Arguments[0];
        if (!commands.TryGetValue(commandName, out Action<DialogueCommandContext> command)) {
            Debug.LogWarning("Unknown dialogue command: " + commandName);
            return false;
        }

        command(context);
        return true;
    }

    public void Register(string commandName, Action<DialogueCommandContext> command) {
        commands[commandName] = command;
    }

    private void RegisterDefaults() {
        Register("translate", Translate);
        Register("setExpression", SetExpression);
        Register("setDisplayName", SetDisplayName);
        Register("move", Move);
        Register("moveTo", MoveTo);
        Register("exit", Exit);
        Register("enter", Enter);
        Register("combat", Combat);
        Register("combatDirect", CombatDirect);
        Register("resumeCombat", ResumeCombat);
        Register("transition", Transition);
        Register("setBackground", SetBackground);
        Register("setPrefValue", SetPrefValue);
        Register("setFlag", SetFlag);
        Register("playMusic", PlayMusic);
        Register("stopMusic", StopMusic);
        Register("playSound", PlaySound);
        Register("display", Display);
        Register("stopDisplay", StopDisplay);
        Register("displayCG", DisplayCG);
        Register("stopDisplayCG", StopDisplayCG);
        Register("swap", Swap);
    }

    private static void Translate(DialogueCommandContext context) {
        string character = context.RequireString(1, "translate");
        float x = context.RequireFloat(2, "translate");
        float y = context.RequireFloat(3, "translate");
        context.Controller.TranslateCharacter(character, x, y);
    }

    private static void SetExpression(DialogueCommandContext context) {
        string character = context.RequireString(1, "setExpression");
        string expression = context.RequireString(2, "setExpression");
        context.Controller.SetCharacterExpression(character, expression);
    }

    private static void SetDisplayName(DialogueCommandContext context) {
        string character = context.RequireString(1, "setDisplayName");
        string displayName = context.JoinArguments(2);
        context.Controller.SetCharacterDisplayName(character, displayName);
    }

    private static void Move(DialogueCommandContext context) {
        string character = context.RequireString(1, "move");
        float speed = context.RequireFloat(2, "move");
        float x = context.RequireFloat(3, "move");
        float y = context.RequireFloat(4, "move");
        int linkedLines = context.RequireInt(5, "move");
        context.Controller.MoveCharacter(character, speed, x, y);
        context.Controller.SetTextLinkedMoveLength(character, linkedLines);
    }

    private static void MoveTo(DialogueCommandContext context) {
        string character = context.RequireString(1, "moveTo");
        float speed = context.RequireFloat(2, "moveTo");
        float x = context.RequireFloat(3, "moveTo");
        float y = context.RequireFloat(4, "moveTo");
        int linkedLines = context.RequireInt(5, "moveTo");
        context.Controller.MoveCharacterTo(character, speed, x, y);
        context.Controller.SetTextLinkedMoveLength(character, linkedLines);
    }

    private static void Exit(DialogueCommandContext context) {
        string character = context.RequireString(1, "exit");
        string direction = context.RequireString(2, "exit");
        context.Controller.ExitCharacter(character, direction);
    }

    private static void Enter(DialogueCommandContext context) {
        string character = context.RequireString(1, "enter");
        float x = context.RequireFloat(2, "enter");
        float y = context.RequireFloat(3, "enter");
        EnterOptions options = ParseEnterOptions(context, character);
        context.Controller.EnterCharacter(character, x, y, options.Expression, options.DisplayName);
    }

    private sealed class EnterOptions {
        public string Expression = "Normal";
        public string DisplayName = "";
    }

    private static EnterOptions ParseEnterOptions(DialogueCommandContext context, string character) {
        EnterOptions options = new EnterOptions();

        if (context.Arguments.Count <= 4) {
            return options;
        }

        if (TryParseEnterKeyValueOptions(context, options)) {
            return options;
        }

        options.Expression = context.Arguments[4];
        if (context.Arguments.Count > 5) {
            options.DisplayName = context.Arguments[5];
        }

        return options;
    }

    private static bool TryParseEnterKeyValueOptions(DialogueCommandContext context, EnterOptions options) {
        int index = 4;
        while (index < context.Arguments.Count) {
            string key = context.Arguments[index];
            if (!IsEnterOptionKey(key) || index + 1 >= context.Arguments.Count) {
                return false;
            }

            string value = context.Arguments[index + 1];
            switch (NormalizeEnterOptionKey(key)) {
                case "displayname":
                case "name":
                    options.DisplayName = value;
                    break;
                case "expression":
                    options.Expression = value;
                    break;
            }

            index += 2;
        }

        return true;
    }

    private static bool IsEnterOptionKey(string key) {
        string normalized = NormalizeEnterOptionKey(key);
        return normalized == "displayname" || normalized == "name" || normalized == "expression";
    }

    private static string NormalizeEnterOptionKey(string key) {
        return key.Replace("-", "").Replace("_", "").ToLowerInvariant();
    }

    private static void Combat(DialogueCommandContext context) {
        GridController.combatToLoad = context.RequireString(1, "combat");
        SceneManager.LoadScene("GearUpScene", LoadSceneMode.Single);
    }

    private static void CombatDirect(DialogueCommandContext context) {
        GridController.combatToLoad = context.RequireString(1, "combatDirect");
        SceneManager.LoadScene("CombatScene", LoadSceneMode.Single);
    }

    private static void ResumeCombat(DialogueCommandContext context) {
        ScriptController.unPauseGame();
        ScriptController.showCombat();
        SceneManager.UnloadSceneAsync("DialogueScene");
    }

    private static void Transition(DialogueCommandContext context) {
        context.Controller.TransitionBackground(context.RequireString(1, "transition"));
    }

    private static void SetBackground(DialogueCommandContext context) {
        context.Controller.SetBackground(context.RequireString(1, "setBackground"));
    }

    private static void SetPrefValue(DialogueCommandContext context) {
        if (context.Arguments.Count < 3) {
            throw new ArgumentException("setPrefValue requires a key and integer value");
        }

        string key = context.JoinArguments(1, context.Arguments.Count - 2);
        int value = context.RequireInt(context.Arguments.Count - 1, "setPrefValue");
        PlayerPrefs.SetInt(key, value);
        PlayerPrefs.Save();
    }

    private static void SetFlag(DialogueCommandContext context) {
        if (context.Arguments.Count < 3) {
            throw new ArgumentException("setFlag requires a key and value");
        }

        string key = context.JoinArguments(1, context.Arguments.Count - 2);
        string value = context.RequireString(context.Arguments.Count - 1, "setFlag");
        int intValue;
        if (int.TryParse(value, out intValue)) {
            SaveGameService.SetInt(key, intValue);
        }
        else {
            SaveGameService.SetString(key, value);
        }
    }

    private static void PlayMusic(DialogueCommandContext context) {
        context.Controller.PlayMusic(context.JoinArguments(1));
    }

    private static void StopMusic(DialogueCommandContext context) {
        context.Controller.StopMusic();
    }

    private static void PlaySound(DialogueCommandContext context) {
        GameObject soundEffect = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("sound effect"));
        soundEffect.GetComponent<SoundEffectHandler>().play(context.JoinArguments(1));
    }

    private static void Display(DialogueCommandContext context) {
        context.Controller.DisplayImage(context.JoinArguments(1));
    }

    private static void StopDisplay(DialogueCommandContext context) {
        context.Controller.StopDisplayImage();
    }

    private static void DisplayCG(DialogueCommandContext context) {
        context.Controller.DisplayCG(context.JoinArguments(1));
    }

    private static void StopDisplayCG(DialogueCommandContext context) {
        context.Controller.StopDisplayCG();
    }

    private static void Swap(DialogueCommandContext context) {
        string first = context.RequireString(1, "swap");
        string second = context.RequireString(2, "swap");
        context.Controller.SwapCharacters(first, second);
    }
}
