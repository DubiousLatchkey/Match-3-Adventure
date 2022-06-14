using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    private Vector3 basePosition;

    private void Start() {
        basePosition = gameObject.transform.position;
    }

    public void shakeCamera(float duration, float intensity) {
        StartCoroutine(doCameraShake(duration, intensity));
    }

    IEnumerator doCameraShake(float duration, float intensity) {
        float elapsed = 0;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;

            float xOffset = Random.Range(-intensity + basePosition.x, intensity + basePosition.x);
            float yOffset = Random.Range(-intensity + basePosition.y, intensity + basePosition.y);

            //gameObject.transform.position = Vector3.MoveTowards(gameObject.transform.position,new Vector3(xOffset, yOffset), 0.2f);
            gameObject.transform.position = new Vector3(xOffset, yOffset);

            yield return null;
        }
        StartCoroutine(doMoveBack());
    }

    IEnumerator doMoveBack() {
        while (gameObject.transform.position != basePosition) {
            gameObject.transform.position = Vector3.MoveTowards(gameObject.transform.position, basePosition, 1f);
            yield return null;
        }
    }
}
