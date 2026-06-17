using UnityEngine;

public sealed class CombatSceneRefs : MonoBehaviour {
    public static CombatSceneRefs Instance { get; private set; }

    [SerializeField] private GridController gridController;
    [SerializeField] private EnemyController enemyController;
    [SerializeField] private resultsScreenHandler resultsScreen;
    [SerializeField] private Transform canvas;
    [SerializeField] private Transform playerPortrait;
    [SerializeField] private Transform enemyPortrait;
    [SerializeField] private Transform playerPortraitImage;
    [SerializeField] private Transform enemyPortraitImage;
    [SerializeField] private Transform playerSpellParent;
    [SerializeField] private Transform enemySpellParent;
    [SerializeField] private CombatantView playerCombatantView;
    [SerializeField] private CombatantView enemyCombatantView;

    public GridController Grid => gridController;
    public EnemyController Enemy => enemyController;
    public resultsScreenHandler ResultsScreen => resultsScreen;
    public Transform Canvas => canvas;
    public Transform PlayerPortrait => playerPortrait;
    public Transform EnemyPortrait => enemyPortrait;
    public Transform PlayerPortraitImage => playerPortraitImage;
    public Transform EnemyPortraitImage => enemyPortraitImage;
    public Transform PlayerSpellParent => playerSpellParent;
    public Transform EnemySpellParent => enemySpellParent;
    public CombatantView PlayerCombatantView => playerCombatantView;
    public CombatantView EnemyCombatantView => enemyCombatantView;

    private void Awake() {
        Instance = this;
        AutoBindMissingReferences();
    }

    private void OnValidate() {
        AutoBindMissingReferences();
    }

    private void AutoBindMissingReferences() {
        if (gridController == null) {
            GameObject grid = GameObject.Find("Grid");
            if (grid != null) {
                gridController = grid.GetComponent<GridController>();
                enemyController = grid.GetComponent<EnemyController>();
            }
        }

        if (resultsScreen == null) {
            GameObject results = GameObject.Find("resultsScreen");
            if (results != null) {
                resultsScreen = results.GetComponent<resultsScreenHandler>();
            }
        }

        if (canvas == null) {
            GameObject canvasObject = GameObject.Find("Canvas");
            if (canvasObject != null) {
                canvas = canvasObject.transform;
            }
        }

        playerPortrait = playerPortrait != null ? playerPortrait : FindTransform("portrait");
        enemyPortrait = enemyPortrait != null ? enemyPortrait : FindTransform("enemyPortrait");
        playerPortraitImage = playerPortraitImage != null ? playerPortraitImage : FindTransform("portraitImage");
        enemyPortraitImage = enemyPortraitImage != null ? enemyPortraitImage : FindTransform("enemyPortraitImage");
        playerSpellParent = playerSpellParent != null ? playerSpellParent : FindTransform("paperback");
        enemySpellParent = enemySpellParent != null ? enemySpellParent : FindTransform("enemyPaperback");

        if (playerCombatantView == null && playerPortrait != null) {
            playerCombatantView = playerPortrait.GetComponent<CombatantView>();
        }
        if (enemyCombatantView == null && enemyPortrait != null) {
            enemyCombatantView = enemyPortrait.GetComponent<CombatantView>();
        }
    }

    private Transform FindTransform(string objectName) {
        GameObject found = GameObject.Find(objectName);
        return found != null ? found.transform : null;
    }
}
