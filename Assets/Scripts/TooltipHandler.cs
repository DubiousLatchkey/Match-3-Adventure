using UnityEngine;
using UnityEngine.UI;

public class TooltipHandler : MonoBehaviour
{
    GameObject tooltip;
    Text tooltipText;
    string tempTextStorage;
    Vector3 tempPositionStorage;

    bool pressed = false;
    bool tooltipOpen = false;
    float pressedTime;

    // Start is called before the first frame update
    void Start()
    {
        foreach (Image i in gameObject.GetComponentsInChildren<Image>()) {
            if (i.gameObject.name == "tooltip") {
                tooltip = i.gameObject;
            }
        }
        foreach (Text i in gameObject.GetComponentsInChildren<Text>()) {
            if (i.gameObject.name == "tooltipText") {
                tooltipText = i;
            }
        }
        if (tempTextStorage != null) {
            tooltipText.text = tempTextStorage;
            tempTextStorage = null;
        }
        //tempTextStorage = tooltipText.text;
        //Debug.Log(tooltip.ToString() + tooltipText.text);
        if (tempPositionStorage != null) {
            tooltip.transform.localPosition = tempPositionStorage;
        }
        tooltip.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (pressed) {
            if (Time.time - pressedTime > 0.3f && tooltipOpen == false) {
                //Open tooltip
                Debug.Log("Opening tooltip");
                if (tempTextStorage != null) {
                    tooltipText.text = tempTextStorage;
                    tempTextStorage = null;
                }
                if (tempPositionStorage != null) {
                    //Debug.Log(tempPositionStorage);
                    tooltip.transform.localPosition = tempPositionStorage;
                    //tempPositionStorage = Vector3.zero; //Bad practice to leave this out, increases runtime overhead, but fixes misplacing bug
                }
                tooltipOpen = true;
                tooltip.SetActive(true);
                tooltip.transform.SetParent(GameObject.Find("Canvas").transform);
            }
        }
        
    }

    public void buttonDown() {
        pressed = true;
        pressedTime = Time.time;
    }

    public void buttonUp() {
        pressed = false;
        if (tooltipOpen) {
            tooltipOpen = false;
            //Close tooltip
            tooltip.SetActive(false);
            tooltip.transform.SetParent(gameObject.transform);
        }
    }

    public void setPosition(Vector3 position) {
        tempPositionStorage = position;
    }

    public void setText(string text) {
        tempTextStorage = text;
    }

}
