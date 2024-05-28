using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FadeController : MonoBehaviour
{
    bool fadingIn = true; //If false, will fade out
    Image image;
    Color fadeIn;
    Color fadeOut;
    float fadeSpeed = 0.01f;
    const float fadeOutValue = 150;
    bool scheduledForDeletion = false;
    float time = 0;

    // Start is called before the first frame update
    void Start()
    {
        image = gameObject.GetComponent<Image>();
        fadeIn = new Color32(255, 255, 255, 255);
        fadeOut = new Color32((int)fadeOutValue, (int)fadeOutValue, (int)fadeOutValue, 255);
    }

    // Update is called once per frame
    void Update()
    {
        if (scheduledForDeletion) {
            if (image.color.a <= 0) {
                //Debug.Log("Deleting Object");
                Destroy(gameObject);
            }
            time += Time.deltaTime * fadeSpeed * 2;
            Color endColor = new Color(image.color.r, image.color.g, image.color.b, 0);
            image.color = Color.Lerp(image.color, endColor, time);
            //image.color = color;
        }
        else if (fadingIn && !compareColorValues(image.color, fadeIn)) {
            image.color = getFadeColor();
            if (image.color.r > 255) {
                image.color = fadeIn;
            }
            //Debug.Log(image.color.r);
        }
        else if (!fadingIn && !compareColorValues(image.color, fadeOut)) {
            image.color = getFadeColor();
            if (image.color.r < fadeOutValue) {
                image.color = fadeOut;
            }
            //Debug.Log(image.color.r);
        }
    }

    private Color32 getFadeColor() {
        Color fadeColor = image.color;
        float increment = fadeSpeed;
        if (!fadingIn) {
            increment *= -1;
        }
        fadeColor.r += increment;
        fadeColor.g += increment;
        fadeColor.b += increment;
        fadeColor.a += increment;

        return fadeColor;
    }

    private bool compareColorValues(Color a, Color b) {
        if (a.r == b.r && a.g == b.g && a.b == b.b) {
            return true;
        }
        else {
            return false;
        }
    }

    public void setFadeIn() {
        fadingIn = true;
    }

    public void setFadeOut() {
        fadingIn = false;
    }

    public bool getFadingIn() {
        return fadingIn;
    }

    public void setFadeSpeed(float speed) {
        fadeSpeed = speed;
    }

    public float getFadeSpeed() {
        return fadeSpeed;
    }

    public void scheduleForDeletion() {
        scheduledForDeletion = true;
    }

}
