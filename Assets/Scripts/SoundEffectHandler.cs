using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundEffectHandler : MonoBehaviour
{
    public AudioSource soundEffect;
    bool started = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (started && !soundEffect.isPlaying) {
            Destroy(gameObject);
        }
    }

    public void play(string sound) {
        soundEffect.clip = Resources.Load<AudioClip>(sound);
        soundEffect.Play();
        started = true;
    }
}
