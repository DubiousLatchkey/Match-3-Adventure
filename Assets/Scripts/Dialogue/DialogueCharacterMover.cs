using UnityEngine;

public sealed class DialogueCharacterMover : MonoBehaviour {
    private RectTransform rectTransform;
    private Vector2 targetPosition;
    private float speed = 10f;
    private float baseSpeed = 0.5f;
    private float time;
    private bool moving;

    private void Awake() {
        rectTransform = GetComponent<RectTransform>();
        targetPosition = rectTransform.anchoredPosition;
    }

    private void Update() {
        if (rectTransform.anchoredPosition != targetPosition) {
            moving = true;
            time += Time.deltaTime * baseSpeed * speed;
            rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, targetPosition, time);
        }
        else {
            moving = false;
            time = 0;
        }
    }

    public void SetPosition(Vector2 position) {
        targetPosition = position;
        rectTransform.anchoredPosition = position;
        time = 0;
    }

    public void SetTarget(Vector2 position) {
        targetPosition = position;
        time = 0;
    }

    public void SetTarget(Vector2 position, float movementSpeed) {
        speed = movementSpeed;
        SetTarget(position);
    }

    public Vector2 GetTarget() {
        return targetPosition;
    }

    public bool IsMoving() {
        return moving;
    }

    public void SetSpeed(float movementSpeed) {
        speed = movementSpeed;
    }
}
