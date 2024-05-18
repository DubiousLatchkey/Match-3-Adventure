using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FadeOutHandler : MonoBehaviour
{
    public float fadeDuration = 2f;
    Image image;
    float time = 0;
    Color endColor;
    // Start is called before the first frame update
    void Start()
    {
        image = gameObject.GetComponent<Image>();
        endColor = new Color(image.color.r, image.color.g, image.color.b, 0);
    }

    // Update is called once per frame
    void Update()
    {
        
        if (time > fadeDuration)
        {
            Destroy(gameObject);
        }

        time += Time.deltaTime;
        image.color = Color.Lerp(image.color, endColor, time / fadeDuration);

    }

}
