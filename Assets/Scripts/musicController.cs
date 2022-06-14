using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class musicController : MonoBehaviour
{
    public AudioSource music;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void play() {
        if (music.clip) {
            music.Play();
        }
    }

    public void stop() {
        music.Stop();
    }

    public void setTrack(string trackName) {
        music.clip = Resources.Load<AudioClip>("music/" +trackName);
    }

}
