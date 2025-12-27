using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelButton : MonoBehaviour
{
    [SerializeField]
    private Button Button;

    [SerializeField]
    private TextMeshProUGUI Text;

    public void Initialize(int index)
    {
        Button.onClick.AddListener(() =>
        {
            LevelManager.Instance.SetGroupAlpha(0f);
            LevelManager.Instance.LoadLevel(index);
        });
        Text.text = $"Level {(index + 1)}";
    }
}