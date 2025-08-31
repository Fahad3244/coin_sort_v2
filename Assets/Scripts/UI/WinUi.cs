using System.Collections;
using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class WinUi : MonoBehaviour
{
    private UiManager uiManager;
    private CoinCounter coinCounter;

    [Header("UI References")]
    public TextMeshProUGUI celibrationText;
    public TextMeshProUGUI moneyCountText;
    public TextMeshProUGUI bonusCountText;
    public string celibrationPhrases = "Great Job!|Well Done!|Awesome!|Fantastic!|You're a Star!|Keep it Up!|Superb!";

    [Header("Coin Animation Settings")]
    public GameObject coinPrefabUI;       // prefab for coin
    public RectTransform targetUI;        // coin target in HUD
    [SerializeField] private RectTransform spawnArea; // assign a UI panel/rect for spawn area
    [SerializeField] private float spawnRadius = 50f;   // how close coins spawn together
    public int coinsToSpawn = 10;         // number of coins animated
    public float animationDuration = 0.8f;
    public float spawnInterval = 0.1f;
    public AudioClip coinSound;           // optional sound effect
    public AudioSource audioSource;       // assign in inspector

    private float moneyToAdd = 0f;
    private float displayedMoney = 0f;

    void Start()
    {
        uiManager = UiManager.instance;
        if (uiManager != null)
            coinCounter = uiManager.coinCounter;

        if (coinCounter != null)
        {
            moneyToAdd = uiManager.levelManager.GetBonusCoins();
            displayedMoney = moneyToAdd;

            moneyCountText.text = "Congratulations!\nYou earned $" + displayedMoney.ToString("F2");
            bonusCountText.text = "Bonus: $" + uiManager.levelManager.GetBonusCoins().ToString("F2");
            celibrationText.text = celibrationPhrases.Split('|')[Random.Range(0, celibrationPhrases.Split('|').Length)];
        }
    }



    public void OnNextButton()
    {
        StartCoroutine(PlayCoinCollectAnimation());
    }

private IEnumerator PlayCoinCollectAnimation()
{
    float perCoinValue = moneyToAdd / coinsToSpawn;
    GameObject[] coinObjects = new GameObject[coinsToSpawn];

    Vector3 centerPos = spawnArea.position; // center of spawn area in world space

    // 🔹 Step 1: Spawn all coins in close cluster
    for (int i = 0; i < coinsToSpawn; i++)
    {
        GameObject coinObj = Instantiate(coinPrefabUI, this.transform);
        RectTransform coinRect = coinObj.GetComponent<RectTransform>();
        coinObjects[i] = coinObj;

        // random offset inside a circle (tight cluster)
        Vector2 offset = Random.insideUnitCircle * spawnRadius;
        coinRect.position = centerPos + (Vector3)offset;

        // start tiny, scale pop-in
        coinRect.localScale = Vector3.zero;
        coinRect.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);

        coinObj.SetActive(true);
    }

    // 🔹 Step 2: Animate coins one by one to target
    for (int i = 0; i < coinsToSpawn; i++)
    {
        GameObject coinObj = coinObjects[i];
        RectTransform coinRect = coinObj.GetComponent<RectTransform>();
        bool isLast = (i == coinsToSpawn - 1);

        coinRect.DOMove(targetUI.position, animationDuration)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                Destroy(coinObj);

                CurrencyManager.instance.AddCash(perCoinValue);
                displayedMoney += perCoinValue;

                targetUI.DOPunchScale(Vector3.one * 0.2f, 0.2f);

                if (coinSound != null && audioSource != null)
                    audioSource.PlayOneShot(coinSound);

                if (isLast)
                    OnCoinsAnimationFinished();
            });

        yield return new WaitForSeconds(spawnInterval); // delay between launches
    }
}



    private void OnCoinsAnimationFinished()
    {
        uiManager.levelManager.LoadNextLevel();
        SceneManager.LoadScene(0); // Load the first scene (index 0)
    }

    private IEnumerator UpdateMoneyText(float start, float end, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float current = Mathf.Lerp(start, end, elapsed / duration);
            moneyCountText.text = "Congratulations!\nYou earned $" + current.ToString("F2");
            yield return null;
        }

        // snap to final value
        moneyCountText.text = "Congratulations!\nYou earned $" + end.ToString("F2");
    }
}
