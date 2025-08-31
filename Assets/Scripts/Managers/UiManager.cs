using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Unity.VisualScripting;

public class UiManager : MonoBehaviour
{
    public static UiManager instance;
    public LevelManager levelManager;
    [Header("UI References")]
    public CoinCounter coinCounter;
    public GameObject tutorialPanel;
    public GameObject winPanel;
    public GameObject losePanel;
    private bool tutorialDisabled = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (levelManager.GetCurrentLevelIndext() == 0)
        {
            tutorialPanel.SetActive(true);
        }
    }

    public void UpdateCoinCount(Vector3 screenStartPos, float newCount)
    {
        if (coinCounter != null)
        {
            coinCounter.SpawnFlyingUICoin(screenStartPos, newCount);
        }
    }

    public void DisableTutorial()
    {
        if (tutorialDisabled) return;

        tutorialPanel.SetActive(false);
        tutorialDisabled = true;
    }

    public void OnLevelWin()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }
    }
    public void OnLevelFail()
    {
        if (losePanel != null)
        {
            losePanel.SetActive(true);
        }
    }
}
