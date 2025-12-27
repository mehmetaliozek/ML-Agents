using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [SerializeField]
    private CanvasGroup LevelGroup;

    [SerializeField]
    private GameObject LevelsParent;

    [SerializeField]
    private LevelButton LevelButtonPrefab;

    private Level[] levels;

    private void Awake()
    {
        Instance = this;
        levels = LevelsParent.GetComponentsInChildren<Level>(true);
        for (int i = 0; i < levels.Length; i++)
        {
            LevelButton levelButton = Instantiate(LevelButtonPrefab, transform);

            levelButton.Initialize(i);
        }
    }

    public void SetGroupAlpha(float alpha)
    {
        LevelGroup.alpha = alpha;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetGroupAlpha(LevelGroup.alpha == 1 ? 0 : 1);
        }
    }

    public void LoadLevel(int index)
    {
        for (int i = 0; i < levels.Length; i++)
        {
            levels[i].gameObject.SetActive(i == index);
        }
    }
}