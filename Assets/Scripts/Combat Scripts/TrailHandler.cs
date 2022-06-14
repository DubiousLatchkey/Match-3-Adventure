using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrailHandler : MonoBehaviour
{
    public float speed = 10.0f;
    Vector3 target;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (gameObject.transform.position == target) {
            gameObject.GetComponent<ParticleSystem>().Clear();
            Destroy(gameObject);
        }

        float step = speed * Time.deltaTime; 
        transform.position = Vector3.MoveTowards(transform.position, target, step);

    }

    public void setTarget(Vector3 t) {
        target = t;
    }

}
