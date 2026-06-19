using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class ThingController : MonoBehaviour, IEndDragHandler, IDragHandler
{
    Vector3 target;
    float speed;
    public float baseSpeed = 10f;
    bool moving;
    AudioSource audio;
    public Light2D light;
    bool isIncreasingLightIntensity;
    float accelerationFactor;
    public float accelerationConstant = 1f;
    bool toAccelerate = false;
    bool isWildcard;
    Image image;

    void Awake() {
        EventSystem eventSystem = GetComponent<EventSystem>();
        if (eventSystem != null) {
            Destroy(eventSystem);
        }

        BaseInputModule inputModule = GetComponent<BaseInputModule>();
        if (inputModule != null) {
            Destroy(inputModule);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        target = gameObject.transform.position;
        audio = GetComponent<AudioSource>();
        image = GetComponent<Image>();
        speed = baseSpeed;
        //moving = false;
    }

    // Update is called once per frame
    void Update()
    {

        if (target != gameObject.transform.position) {
            //Debug.Log("Hey I'm driving here!");
            moving = true;
        }
        else {
            speed = baseSpeed / 2;
            moving = false;
            accelerationFactor = 0;
            toAccelerate = false;
        }
        if (toAccelerate)
        {
            accelerationFactor += Time.deltaTime * accelerationConstant;
        }
        float step = speed * Time.deltaTime + accelerationFactor; // calculate distance to move
        //float step = Vector3.Distance(transform.position, target) / 10;
        transform.position = Vector3.MoveTowards(transform.position, target, step);

        if (isWildcard) {
            image.color = Color.HSVToRGB(Mathf.Repeat(Time.time * 0.25f, 1f), 0.65f, 1f);
        }

    }

    void FixedUpdate() {
        if (light.isActiveAndEnabled) {
            float interval = Time.deltaTime * UnityEngine.Random.Range(0.8f, 1.2f);
            if (!isIncreasingLightIntensity) {
                interval *= -1;
            }
            light.intensity += interval;
            light.pointLightOuterRadius = (light.intensity / 5.0f) + 0.2f;
            if ((light.intensity > 5 && isIncreasingLightIntensity) || (light.intensity < 3 && !isIncreasingLightIntensity)) {
                isIncreasingLightIntensity = !isIncreasingLightIntensity;
            }
        }

    }

    public void setSpeed(float newSpeed) {
        speed = newSpeed;
    }

    public void accelerateForNextMove() {
        toAccelerate = true;
    }

    //Takes in type values to assign what piece to use based on type value 
    public void assignPiece(int type) {
        isWildcard = type == ThingTypes.Wildcard;
        image = image != null ? image : GetComponent<Image>();

        switch (type) {
            case (ThingTypes.Red):
                //gameObject.GetComponent<Image>().color = new Color(255, 0, 0);
                gameObject.GetComponent<Image>().color = Color.white;
                gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>("redOrb");
                break;
            case (ThingTypes.Blue):
                //gameObject.GetComponent<Image>().color = new Color(255, 255, 255);
                gameObject.GetComponent<Image>().color = Color.white;
                gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>("blueOrb");
                break;
            case (ThingTypes.Yellow):
                //gameObject.GetComponent<Image>().color = new Color(255, 255, 0);
                gameObject.GetComponent<Image>().color = Color.white;
                gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>("yellowOrb");
                break;
            case (ThingTypes.Damage):
                //gameObject.GetComponent<Image>().color = new Color(255, 0, 255);
                gameObject.GetComponent<Image>().color = Color.white;
                gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>("directDamage");
                break;
            case (ThingTypes.Health):
                //gameObject.GetComponent<Image>().color = new Color(0, 255, 255);
                gameObject.GetComponent<Image>().color = Color.white;
                gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>("heart");
                break;
            case (ThingTypes.Empty):
                //gameObject.GetComponent<Image>().color = new Color(0, 0, 0);
                break;
            case (ThingTypes.Multiplier):
                //gameObject.GetComponent<Image>().color = new Color(0, 255, 0);
                gameObject.GetComponent<Image>().color = Color.white;
                gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>("damageMultiplier");
                break;
            case (ThingTypes.Null):
                gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>("nullTile");
                gameObject.GetComponent<Image>().color = Color.white;
                break;
            case (ThingTypes.Brick):
                gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>("brick");
                gameObject.GetComponent<Image>().color = Color.white;
                break;
            case (ThingTypes.Wildcard):
                gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>("yellowOrb");
                gameObject.GetComponent<Image>().color = Color.HSVToRGB(Mathf.Repeat(Time.time * 0.25f, 1f), 0.65f, 1f);
                break;
            case (ThingTypes.RainbowMana):
                gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>("rainbowMana");
                gameObject.GetComponent<Image>().color = Color.white;
                break;
        }

    }
    
    public void OnEndDrag(PointerEventData eventData) {
        Debug.Log("Oh this is such a drag");

        if(!GridController.isTurn || !gameObject.transform.parent.gameObject.GetComponent<GridController>().isBoardSettledForInput()){
            Debug.Log("Not your turn");
            return;
        }
        //Debug.Log(new Vector2(transform.position.x, transform.position.y).magnitude);
        //Debug.Log((new Vector2(transform.position.x, transform.position.y) - eventData.pressPosition).magnitude);
        if ((eventData.position- eventData.pressPosition).magnitude < 25) {
            return;
        }
        if (GridController.isCasting) {
            return;
        }

        int index = gameObject.transform.parent.gameObject.GetComponent<GridController>().getIndex(gameObject);
        if (!ThingTypes.IsMovable(gameObject.transform.parent.gameObject.GetComponent<GridController>().GetThing(index).getType())) {
            return;
        }

        audio.Play();

        //Get dragged direction
        Vector3 dragDirectionVector = (eventData.position - eventData.pressPosition).normalized;
        float xMagnitude = Math.Abs(dragDirectionVector.x);
        float yMagnitude = Math.Abs(dragDirectionVector.y);
        int swapIndex = 0;

        //Debug.Log((eventData.position - eventData.pressPosition).magnitude);



        if (xMagnitude > yMagnitude) {
            if (dragDirectionVector.x > 0) {
                swapIndex = index + 1;
            }
            else {
                swapIndex = index - 1;
            }
        }
        else {
            if (dragDirectionVector.y > 0) {
                swapIndex = index + GridController.BOARDLENGTH;
            }
            else {
                swapIndex = index - GridController.BOARDLENGTH;
            }
        }

        List<Move> moves = new List<Move>();

        for (int i = 0; i < GridController.BOARDLENGTH * GridController.BOARDLENGTH; i++) {
            moves.AddRange(gameObject.GetComponentInParent<GridController>().movesAtPoint(i));
        }

        //Get other object and swap
        if (swapIndex >= 0 && swapIndex < GridController.BOARDLENGTH * GridController.BOARDLENGTH) {
            if (!ThingTypes.IsMovable(gameObject.transform.parent.gameObject.GetComponent<GridController>().GetThing(swapIndex).getType())) {
                return;
            }
            //Check valid move or not
            foreach (Move i in moves) {
                if ((i.originLocation == index && i.swapLocation == swapIndex) || (i.originLocation == swapIndex && i.swapLocation == index)) {

                    //Make move
                    if (!GridController.madeMove) { GridController.madeMove = true; }
                    moving = true;
                    GameObject.Find("Grid").GetComponent<GridController>().turnOffSpells();
                    speed = baseSpeed / 4f;
                    gameObject.transform.parent.gameObject.GetComponent<GridController>().GetThing(swapIndex).getObject().GetComponent<ThingController>().setSpeed(baseSpeed / 4f);
                    gameObject.transform.parent.gameObject.GetComponent<GridController>().selectPiece(index, Color.white);
                    gameObject.transform.parent.gameObject.GetComponent<GridController>().selectPiece(swapIndex, Color.white);
                    gameObject.transform.parent.gameObject.GetComponent<GridController>().swap(index, swapIndex);

                    return;
                }
            }
            Debug.Log("Illegal Move");
            /*
            if ((index % GridController.BOARDLENGTH != 0 || swapIndex != index - 1) && (index % GridController.BOARDLENGTH != GridController.BOARDLENGTH - 1 || swapIndex != index + 1)) {
                gameObject.transform.parent.gameObject.GetComponent<GridController>().swap(index, swapIndex);
                //gameObject.transform.parent.gameObject.GetComponent<GridController>().GetThing(swapIndex).getObject().GetComponent<ThingController>().speed = ;
            }
            */
            
        }

        //gameObject.transform.parent.gameObject.GetComponent<GridController>().checkForMatches();


    }

    public void OnDrag(PointerEventData eventData) {
        //Do nothing
    }

    void OnMouseOver() {
        
    }

    void OnMouseExit() {
    }

    private void OnMouseDown() {
        if (GridController.isCasting) {
            GridController.selectedPiece = this;
            gameObject.transform.parent.gameObject.GetComponent<GridController>().actionBasedOnPiece(this);
        }
    }



    public void setTarget(Vector3 t) {
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
    
}

public enum type {
red,
blue,
yellow,
damage,
health,
multiplier,
nullTile,
brick,
wildcard,
rainbowMana
}
