using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

public class LoseUI : MonoBehaviour
{
    public TextMeshProUGUI loseAmountText;
    public Button retryButton;

    void Start()
    {
        if (loseAmountText != null && UiManager.instance != null && UiManager.instance.coinCounter != null)
        {
            float lostAmount = UiManager.instance.coinCounter.totalCoin;
            loseAmountText.text = "You lost $" + lostAmount.ToString("F2");
        }

        AnimateLoseText();

        retryButton.onClick.AddListener(OnRetry);

        CurrencyManager.instance.CutCash(UiManager.instance.coinCounter.totalCoin);
    }

    private void AnimateLoseText()
    {
        // Reset scale
        loseAmountText.transform.localScale = Vector3.zero;

        // Sequence of animations
        Sequence seq = DOTween.Sequence();

        // Pop in bounce
        seq.Append(loseAmountText.transform.DOScale(Vector3.one, 0.6f).SetEase(Ease.OutBack));

        // Floating loop
        seq.AppendCallback(() =>
        {
            loseAmountText.transform
                .DOMoveY(loseAmountText.transform.position.y + 15f, 1f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);

            loseAmountText.transform
                .DORotate(new Vector3(0, 0, 5f), 1f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        });
    }

    private void OnRetry()
    {
        // reload current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
