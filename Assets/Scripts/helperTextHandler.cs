using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class helperTextHandler : MonoBehaviour
{
    MoveToTargetBehavior moveToTargetBehavior;
    float fadeOutSpeed = 0.5f;

    // Start is called before the first frame update
    void Start()
    {
        moveToTargetBehavior = gameObject.GetComponent<MoveToTargetBehavior>();
        moveToTargetBehavior.setTarget(gameObject.transform.position + new Vector3(0, 1.2f));
        moveToTargetBehavior.setInitialDistance(1.2f);
        moveToTargetBehavior.setSpeed(0.0005f);
    }

    // Update is called once per frame
    void Update()
    {
        Color color = gameObject.GetComponent<Text>().color;
        if (color.a <= 0) {
            Destroy(gameObject);
        }
        else {
            color.a -= Time.deltaTime * fadeOutSpeed;
            gameObject.GetComponent<Text>().color = color;
        }
    }

    public void setText(string text) {
        gameObject.GetComponent<Text>().text = text;
    }

    public void setPosition(Vector3 vector) {
        Vector3 yOffset = new Vector3(0, Random.Range(-0.25f, 0.25f));
        gameObject.transform.position = vector + yOffset;
        moveToTargetBehavior = gameObject.GetComponent<MoveToTargetBehavior>();
        moveToTargetBehavior.setTarget(gameObject.transform.position + new Vector3(0, 100));
        moveToTargetBehavior.setInitialDistance(100);
        moveToTargetBehavior.setSpeed(0.0005f);
    }

    public void setTextColor(Color color) {
        gameObject.GetComponent<Text>().color = color;
    }

    public void setTextSize(int size) {
        gameObject.GetComponent<Text>().fontSize = size;
    }
}
