using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragToSpotBehavior : MonoBehaviour, IEndDragHandler, IDragHandler, IBeginDragHandler {

    //public Vector3 homePosition;
    EquippingController equippingController;
    public slot myBox;
    TooltipHandler tooltipHandler;
    public bool isSpell;

    // Start is called before the first frame update
    void Start()
    {
        //homePosition = gameObject.transform.position;
        equippingController = gameObject.GetComponentInParent<EquippingController>();

        try {
            tooltipHandler = gameObject.GetComponent<TooltipHandler>();
        }
        catch (System.Exception e) {
            Debug.Log("This object has no tooltip");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnEndDrag(PointerEventData eventData) {

        myBox = equippingController.whichBoxAmIIn(eventData.position, isSpell);
        //If Found a box
        if (myBox != null) {
            gameObject.transform.SetParent(myBox.getHolding().transform);
            gameObject.transform.localPosition = new Vector3(0, 0);
            myBox.isFull = true;
            if (isSpell) {
                Debug.Log(gameObject.transform.position);
                myBox.setHoldName(gameObject.GetComponentInChildren<Text>().text);
                tooltipHandler.setPosition(new Vector3(0, 75));
            }
            else {
                myBox.setHoldName(gameObject.GetComponent<WeaponButtonHandler>().getWeapon().Name);
                tooltipHandler.setPosition(Vector3.zero);
            }
        }
        //Otherwise, if too far away
        else {
            //Reset to original position
            if (isSpell) {
                gameObject.transform.SetParent(GameObject.Find("availableSpellsList").transform);
            }
            else {
                gameObject.transform.SetParent(GameObject.Find("availableWeaponsList").transform);
            }
            gameObject.transform.localPosition = new Vector3(0, 0);

        }

        if (tooltipHandler) {
            tooltipHandler.enabled = true;
        }

    }

    public void OnDrag(PointerEventData eventData) {
        gameObject.transform.position = eventData.position;

    }

    public void OnBeginDrag(PointerEventData eventData) {

        //Leave current box if not in a list
        if (((isSpell && gameObject.transform.parent != GameObject.Find("availableSpellsList").transform)
            || (!isSpell && gameObject.transform.parent != GameObject.Find("availableWeaponsList").transform))
            && myBox != null) {
            myBox.isFull = false;
            myBox.setHoldName("");
        }
        //Otherwise, if not in a box, leave the layout
        else {
            gameObject.transform.SetParent(gameObject.transform.parent.parent.parent);
        }

        //Disable tooltip in movement
        if (tooltipHandler) {
            tooltipHandler.enabled = false;
        }
    }
}
