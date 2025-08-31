using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class LevelManager : MonoBehaviour
{
    public GridAndCoinGenerator gridAndCoinGenerator;
    [Header("Level Data")]
    public List<LevelData> levels = new List<LevelData>();
    public LevelData currentLevel;
    private const string CASH_KEY = "ActiveLevel";
    private int activeCoins;

    [Header("Slot References")]
    public Slot[] slots; // length can be 3 (max slots)
    [Header("Tray Reference")]
    public Tray tray;   // reference to tray

    [Header("Auto-Match Settings")]
    [Tooltip("Enable automatic matching of tray coins to slots")]
    public bool enableAutoMatch = true;
    [Tooltip("Delay before checking for auto-match after coin is added to tray")]
    public float autoMatchDelay = 0.2f;
    [Tooltip("Duration for auto-match movement animation")]
    public float autoMatchMoveDuration = 0.8f;
    [Tooltip("Jump power for auto-match movement")]
    public float autoMatchJumpPower = 1f;
    [Tooltip("Ease type for auto-match movement")]
    public Ease autoMatchEase = Ease.InOutQuad;

    [Header("Debug Settings")]
    [Tooltip("Set to -1 for normal play. If >= 0, that level index will always load.")]
    public int debugLevelIndex = -1;

    void Awake()
    {
        LoadLevel();
    }

    void LoadLevel()
    {
        int levelIndex = (debugLevelIndex >= 0) ? debugLevelIndex : GetCurrentLevelIndext();

        currentLevel = levels[levelIndex];
        SetupSlots();

        if (levelIndex == 0) (tray.gameObject).SetActive(false);
        else (tray.gameObject).SetActive(true);
    }

    private void Start()
    {
        SetupSlots();
    }

    public float GetBonusCoins()
    {
        return currentLevel.bounusCoinCount;
    }

    /// <summary>
    /// Initialize slots with type, value, and color from LevelData
    /// </summary>
    private void SetupSlots()
    {
        for (int i = 0; i < currentLevel.slotTypes.Count; i++)
        {
            if (slots[i] == null) continue;

            SlotType slotType = currentLevel.slotTypes[i];

            slots[i].SetupSlot(
                slotType,
                gridAndCoinGenerator.GetCoinValue((CoinType)slotType),
                gridAndCoinGenerator.GetCoinColor((CoinType)slotType)
            );

            slots[i].gameObject.SetActive(true); // make sure slot is visible
        }

        // Disable any unused slots
        for (int i = currentLevel.slotTypes.Count; i < slots.Length; i++)
        {
            if (slots[i] != null)
                slots[i].gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Check if a coin matches any slot
    /// </summary>
    public bool MatchesSlot(CoinType coinType)
    {
        for (int i = 0; i < currentLevel.slotTypes.Count; i++)
        {
            if (slots[i] != null && slots[i].type == (SlotType)coinType)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Get the first matching slot for a coin type
    /// </summary>
    public Slot GetMatchingSlot(CoinType coinType)
    {
        for (int i = 0; i < currentLevel.slotTypes.Count; i++)
        {
            if (slots[i] != null && slots[i].type == (SlotType)coinType)
                return slots[i];
        }
        return null;
    }

    /// <summary>
    /// Called when a new coin is added to tray (call this from Tray script)
    /// </summary>
    public void OnCoinAddedToTray(Coin coin)
    {
        if (!enableAutoMatch || coin == null) return;

        // Check after a short delay to allow tray animations to settle
        DOVirtual.DelayedCall(autoMatchDelay, () =>
        {
            CheckAndMoveToSlot(coin);
        });
    }

    /// <summary>
    /// Check if coin matches a slot and move it automatically
    /// </summary>
    private void CheckAndMoveToSlot(Coin coin)
    {
        if (coin == null) return;

        Slot matchingSlot = GetMatchingSlot(coin.type);
        if (matchingSlot != null)
        {
            // Remove coin from tray before moving
            tray.RemoveCoin(coin);

            // Move coin to slot with animation
            MoveCoinToSlot(coin, matchingSlot.transform);
        }
    }

    /// <summary>
    /// Move coin to slot with animation (similar to InputManager logic)
    /// </summary>
    private void MoveCoinToSlot(Coin coin, Transform slotTransform)
    {
        // Optional: stop any previous tweens
        coin.transform.DOKill();
        coin.transform.DORotate(new Vector3(90, 0, 0), autoMatchMoveDuration).SetEase(Ease.InCubic);

        // Use DOJump for jump effect
        coin.transform.DOJump(
            slotTransform.position,      // end position
            autoMatchJumpPower,          // jump height
            1,                           // number of jumps
            autoMatchMoveDuration        // duration
        ).SetEase(autoMatchEase).OnComplete(() =>
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(slotTransform.position);
            UiManager.instance.UpdateCoinCount(screenPos, coin.value);
            slotTransform.DOPunchScale(Vector3.one * 0.3f, 0.2f, 1, 0.5f).SetEase(Ease.OutBack);
            Destroy(coin.gameObject); // remove coin after reaching slot
            UnregisterCoin();
            Slot slot = slotTransform.GetComponent<Slot>();
            slot?.PlayEffect();
        });
    }

    public void RegisterCoin()
    {
        activeCoins++;
    }

    public void UnregisterCoin()
    {
        activeCoins--;
        if (activeCoins <= 0)
        {
            OnBoardCleared();
        }
    }

    private void OnBoardCleared()
    {
        Debug.Log("Board Cleared!");
        UiManager.instance.OnLevelWin();
    }

    public void SetCurrentLevelIndex(int index)
    {
        PlayerPrefs.SetInt(CASH_KEY, index);
        PlayerPrefs.Save();
    }

    public int GetCurrentLevelIndext()
    {
        return PlayerPrefs.GetInt(CASH_KEY, 0);
    }
    
    public void LoadNextLevel()
    {
        int nextIndex = GetCurrentLevelIndext() + 1;
        if (nextIndex >= levels.Count)
            nextIndex = 0; // loop back to first level or clamp

        SetCurrentLevelIndex(nextIndex);
    }
}

// -----------------------------
// CoinType Enum
// -----------------------------
public enum CoinType
{
    Half,     // 0.5
    One,
    Five,
    Ten,
    Fifty,
    OneHundred,
    FiveHundred,
    OneThousand
}
public enum SlotType
{
    Half,     // 0.5
    One,
    Five,
    Ten,
    Fifty,
    OneHundred,
    FiveHundred,
    OneThousand
}
