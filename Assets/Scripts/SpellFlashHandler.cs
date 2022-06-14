using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

public class SpellFlashHandler : MonoBehaviour
{
    public Light2D light;
    bool lightingUp = true;
    public float speed = 1f;
    public ParticleSystem particleSystem;
    public AudioSource sound;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (lightingUp && light.intensity < 4.5) {
            light.intensity += Time.deltaTime * 2 * speed;
        }
        else if (lightingUp && light.intensity > 4.5) {
            lightingUp = false;
        }
        else if (!lightingUp && light.intensity > 0) {
            light.intensity -= Time.deltaTime * 3.5f * speed;
        }
        else {
            Destroy(gameObject);
        }
    }

    //Sets how fast the flash's light fades in and out  - Maybe I should add setting particle lifetime here too
    public void setSpeed(float speed) {
        this.speed = speed;
    }

    //Sets color of light and particle system for flash
    public void setColor(Color color) {
        Gradient gradient = new Gradient();
        gradient.SetKeys(new GradientColorKey[] { new GradientColorKey(color, 0.0f), new GradientColorKey(Color.clear, 1.0f) }, new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f)});
        var col = particleSystem.colorOverLifetime;
        col.color = gradient;
        light.color = color;
    }
}
