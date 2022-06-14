using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmokePuffHandler : MonoBehaviour
{

    ParticleSystem SmokePuffGenerator;

    // Start is called before the first frame update
    void Start()
    {
        SmokePuffGenerator = gameObject.GetComponent<ParticleSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        //When particles gone, destroy object
        if (!SmokePuffGenerator.IsAlive()) {
            Destroy(gameObject);
        }
    }
}
