using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System;

public class CoinCounter : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI counterText;       // drag TMP text from Canvas
    public RectTransform uiTarget;            // drag coin icon
    public RectTransform uiCanvas;            // main canvas
    public GameObject uiCoinPrefab;           // small coin Image prefab

    public float totalCoin = 0;

    public void ResetCounter()
    {
        totalCoin = 0;
        UpdateText();
    }

    void OnEnable()
    {
        UpdateCashUi();

        if (CurrencyManager.instance != null)
            CurrencyManager.instance.OnCashChanged += OnCashChanged;
    }

    void OnDisable()
    {
        if (CurrencyManager.instance != null)
            CurrencyManager.instance.OnCashChanged -= OnCashChanged;
    }

    private void OnCashChanged(float newCash)
    {
        UpdateCashUi();
    }

    private void UpdateCashUi()
    {
        if (counterText != null)
            counterText.text = MoneyUtils.ToShortString(CurrencyManager.instance.GetTotalCash());

        // 🔹 optional juice effect
        if (counterText != null)
            counterText.transform.DOPunchScale(Vector3.one * 0.15f, 0.25f, 6, 0.8f);
    }


    public void AddCoins(float cents)
    {
        totalCoin += cents;
        // UpdateText();

        // // feedback punch
        // if (counterText != null)
        //     counterText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5, 1);
    }

    private void UpdateText()
    {
        if (counterText != null)
            counterText.text = MoneyUtils.ToShortString(totalCoin);
    }

    // 🔹 Spawns a flying UI coin
    public void SpawnFlyingUICoin(Vector3 screenStartPos, float cents)
    {
        GameObject coinObj = Instantiate(uiCoinPrefab, uiCanvas);
        RectTransform coinRect = coinObj.GetComponent<RectTransform>();

        // place at screen start
        coinRect.position = screenStartPos;

        // animate to counter
        coinRect.DOMove(uiTarget.position, 0.6f)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                AddCoins(cents);
                CurrencyManager.instance.AddCash(cents);
                Destroy(coinObj);
            });

        coinRect.DOScale(0.5f, 0.6f);
    }


    public void SpawnFlyingUpUICoin(Vector3 screenStartPos, float cents)
    {
        AddCoins(cents);
        GameObject coinObj = Instantiate(uiCoinPrefab, uiCanvas);
        RectTransform coinRect = coinObj.GetComponent<RectTransform>();

        // start position
        coinRect.position = screenStartPos;

        // add CanvasGroup for fading (if not already on prefab)
        CanvasGroup cg = coinObj.AddComponent<CanvasGroup>();
        cg.alpha = 1f;

        // move slightly upward
        Vector3 upPos = screenStartPos + new Vector3(0f, 300f, 0f);

        // animate upwards
        coinRect.DOMove(upPos, 1f).SetEase(Ease.OutQuad);

        // fade out
        cg.DOFade(0f, 1f).OnComplete(() =>
        {
            Destroy(coinObj);
        });
    }
}
