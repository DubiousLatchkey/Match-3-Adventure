using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class BarHandler : MonoBehaviour
{
    public float X;
    public float Y;
    public bool vertical;

    private Vector2 sizeDelta;

    void Start()
    {
        sizeDelta = gameObject.GetComponent<RectTransform>().sizeDelta;
    }

    // Update is called once per frame
    void Update()
    {
        if (gameObject.GetComponent<RectTransform>().sizeDelta != sizeDelta) {
            //Move towards size
            gameObject.GetComponent<RectTransform>().sizeDelta = Vector2.MoveTowards(gameObject.GetComponent<RectTransform>().sizeDelta, sizeDelta, Time.deltaTime * 100);
        }
    }

    //Sets percentage with movement
    public void setPercentageFilled(float percentage) {
        //Debug.Log(percentage * X);
        if (!vertical && percentage >= 0) {
            sizeDelta = new Vector2(percentage * X, Y);
        }
        else if (vertical && percentage >= 0) {
            sizeDelta = new Vector2(X, percentage * Y);

        }
    }

    //Sets initial percentage, no movement
    public void setInitialPercentageFilled(float percentage) {
        if (!vertical && percentage >= 0) {
            gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(percentage * X, Y);
            sizeDelta = gameObject.GetComponent<RectTransform>().sizeDelta;
        }
        else if (vertical && percentage >= 0) {
            gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(X, percentage * Y);
            sizeDelta = gameObject.GetComponent<RectTransform>().sizeDelta;

        }
    }

}
