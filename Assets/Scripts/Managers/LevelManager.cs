using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class LevelManager : MonoBehaviour
{
    public GridAndCoinGenerator gridAndCoinGenerator;
    
    [Header("Level Data")]
    public List<LevelData> levels = new List<LevelData>();
    public LevelData currentLevel;
    private const string LEVEL_KEY = "ActiveLevel"; // Fixed typo from "CASH_KEY"
    private int activeCoins;

    [Header("Slot References")]
    public Slot[] slots; // length can be 3 (max slots)
    
    [Header("Tray Reference")]
    public Tray tray;   // reference to tray

    [Header("Stack Monitor Integration")]
    public StackMonitor stackMonitor; // Reference to StackMonitor for approach 1

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
        // Fixed bounds checking
        int levelIndex = (debugLevelIndex >= 0) ? debugLevelIndex : GetCurrentLevelIndex();
        
        // Ensure level index is within bounds
        if (levelIndex >= levels.Count || levelIndex < 0)
        {
            Debug.LogWarning($"Level index {levelIndex} out of bounds. Using level 0.");
            levelIndex = 0;
        }

        if (levels.Count == 0)
        {
            Debug.LogError("No levels assigned to LevelManager!");
            return;
        }

        currentLevel = levels[levelIndex];
        
        // Show/hide tray based on level
        if (tray != null)
        {
            tray.gameObject.SetActive(levelIndex != 0);
        }
    }

    private void Start()
    {
        SetupSlots();
        
        // Initialize StackMonitor after a small delay to ensure all coins are generated
        if (stackMonitor != null && currentLevel != null)
        {
            DOVirtual.DelayedCall(0.1f, () => {
                stackMonitor.Initialize(currentLevel);
                Debug.Log("StackMonitor initialized from LevelManager");
            });
        }
    }

    public float GetBonusCoins()
    {
        return currentLevel != null ? currentLevel.bounusCoinCount : 0f;
    }

    /// <summary>
    /// Initialize slots with type, value, and color from LevelData
    /// </summary>
    private void SetupSlots()
    {
        if (currentLevel == null)
        {
            Debug.LogError("CurrentLevel is null in SetupSlots!");
            return;
        }

        if (gridAndCoinGenerator == null)
        {
            Debug.LogError("GridAndCoinGenerator reference is null!");
            return;
        }

        for (int i = 0; i < currentLevel.slotTypes.Count && i < slots.Length; i++)
        {
            if (slots[i] == null) 
            {
                Debug.LogWarning($"Slot {i} is null!");
                continue;
            }

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
        if (currentLevel == null) return false;
        
        for (int i = 0; i < currentLevel.slotTypes.Count && i < slots.Length; i++)
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
        if (currentLevel == null) return null;
        
        for (int i = 0; i < currentLevel.slotTypes.Count && i < slots.Length; i++)
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
            if (tray != null)
            {
                tray.RemoveCoin(coin);
            }

            // Move coin to slot with animation
            MoveCoinToSlot(coin, matchingSlot.transform);
        }
    }

    /// <summary>
    /// Move coin to slot with animation (similar to InputManager logic)
    /// </summary>
    private void MoveCoinToSlot(Coin coin, Transform slotTransform)
    {
        // Register coin removal with StackMonitor BEFORE moving
        if (stackMonitor != null && StackMonitor.Instance != null)
        {
            StackMonitor.Instance.RegisterCoinRemoval(coin);
        }

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
            // Null check for UiManager
            if (UiManager.instance != null)
            {
                Vector3 screenPos = Camera.main.WorldToScreenPoint(slotTransform.position);
                UiManager.instance.UpdateCoinCount(screenPos, coin.value);
            }

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
        Debug.Log($"Coin registered. Active coins: {activeCoins}");
    }

    public void UnregisterCoin()
    {
        activeCoins--;
        Debug.Log($"Coin unregistered. Active coins: {activeCoins}");
        
        if (activeCoins <= 0)
        {
            OnBoardCleared();
        }
    }

    private void OnBoardCleared()
    {
        Debug.Log("🎉 Board Cleared!");
        if (UiManager.instance != null)
        {
            UiManager.instance.OnLevelWin();
        }
        else
        {
            Debug.LogWarning("UiManager.instance is null in OnBoardCleared!");
        }
    }

    public void SetCurrentLevelIndex(int index)
    {
        PlayerPrefs.SetInt(LEVEL_KEY, index);
        PlayerPrefs.Save();
        Debug.Log($"Level index set to: {index}");
    }

    public int GetCurrentLevelIndex() // Fixed typo in method name
    {
        return PlayerPrefs.GetInt(LEVEL_KEY, 0);
    }
    
    public void LoadNextLevel()
    {
        int nextIndex = GetCurrentLevelIndex() + 1;
        if (nextIndex >= levels.Count)
        {
            nextIndex = 0; // loop back to first level or clamp
            Debug.Log("Reached end of levels, looping back to level 0");
        }

        SetCurrentLevelIndex(nextIndex);
        Debug.Log($"Loading next level: {nextIndex}");
        
        // Reload the scene or reinitialize
        // UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    // Event subscription for StackMonitor (optional)
    private void OnEnable()
    {
        StackMonitor.OnStackEmpty += OnStackBecameEmpty;
    }

    private void OnDisable()
    {
        StackMonitor.OnStackEmpty -= OnStackBecameEmpty;
    }

    private void OnStackBecameEmpty(Vector2Int gridPosition)
    {
        Debug.Log($"🎊 LevelManager: Stack at {gridPosition} became empty!");
        // Add any game-specific logic here
    }

    // Debug methods
    [ContextMenu("Debug Active Coins")]
    private void DebugActiveCoins()
    {
        Debug.Log($"Active coins: {activeCoins}");
    }

    [ContextMenu("Force Board Clear")]
    private void ForceboardClear()
    {
        activeCoins = 0;
        OnBoardCleared();
    }

    [ContextMenu("Debug Stack Monitor")]
    public void DebugStackMonitor()
    {
        if (stackMonitor != null)
        {
            stackMonitor.DebugPrintAllStacks();
        }
        else
        {
            Debug.LogError("StackMonitor reference is null!");
        }
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