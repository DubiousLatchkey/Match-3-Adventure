using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveToTargetBehavior : MonoBehaviour
{
    Vector3 target;
    float speed = 10;
    bool moving;
    float initialDistance;
    float baseSpeed = 0.5f;
    float time = 0;

    // Start is called before the first frame update
    void Start()
    {
        target = gameObject.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (target != gameObject.transform.position) {
            //Debug.Log("Hey I'm driving here!");
            moving = true;
            //float distanceToTarget = Vector3.Distance(transform.position, target);
            //speed *= 5 * distanceToTarget / initialDistance;
            /*
            float distanceToTarget = Vector3.Distance(transform.position, target);
            if (distanceToTarget / initialDistance < 0.01) {
                transform.position = target;
            }

            Vector3 dirNormalized = (target - transform.position).normalized;
            */
            //float step = baseSpeed * speed * Time.fixedDeltaTime;

            //Debug.Log(step);

            //transform.position = Vector3.MoveTowards(transform.position, target, step);
            //transform.position = transform.position + dirNormalized * step;
            time += Time.deltaTime * baseSpeed * speed;

            transform.position = Vector3.Lerp(transform.position, target, time);

        }
        else {
            moving = false;
            time = 0;
        }

    }

    public void setTarget(Vector3 t) {
        target = t;
        initialDistance = Vector3.Distance(gameObject.transform.position, target);
    }

    public void setTarget(Vector3 t, float s) {
        speed = s;
        initialDistance = Vector3.Distance(gameObject.transform.position, t);
        target = t;
    }

    public Vector3 getTarget() {
        return target;
    }

    public bool getMoving() {
        return moving;
    }

    public void setMoving(bool set) {
        moving = set;
    }

    public float getInitalDistance() {
        return initialDistance;
    }

    public void setInitialDistance(float distance) {
        initialDistance = distance;
    }

    public float getSpeed() {
        return speed;
    }

    public void setSpeed(float s) {
        speed = s;
    }

}
