using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterDialogueAdvanceHandler : MonoBehaviour
{
    DialogueController dialogueController;

    // Start is called before the first frame update
    void Start()
    {
        dialogueController = GameObject.Find("DialogueBox").GetComponent<DialogueController>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void click() {
        dialogueController.advanceDialogue();
    }
}
