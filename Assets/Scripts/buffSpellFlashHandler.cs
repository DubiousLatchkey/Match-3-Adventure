using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class buffSpellFlashHandler : MonoBehaviour
{
    public ParticleSystem particleSystem;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!particleSystem.IsAlive()) {
            Destroy(gameObject);
        }
    }
}
