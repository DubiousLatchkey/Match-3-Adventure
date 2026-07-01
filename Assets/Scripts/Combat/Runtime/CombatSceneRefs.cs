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
    }
}
