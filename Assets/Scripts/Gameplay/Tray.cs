using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[System.Serializable]
public struct MergeRule
{
    public CoinType inputType;
    public int requiredCount;
    public CoinType outputType;
}

public class Tray : MonoBehaviour
{
    public GridAndCoinGenerator gridAndCoinGenerator;
    private List<Coin> coins = new List<Coin>();
    public IReadOnlyList<Coin> Coins => coins; // Read-only access

    [Header("Animation Settings")]
    public float moveDuration = 0.5f;
    public float jumpPower = 1f;
    
    [Header("Merge Animation Settings")]
    [Tooltip("Y offset for coins during merge animation to prevent overlapping")]
    public float mergeYOffset = 2f;
    
    [Header("Stacking Behavior")]
    [Tooltip("If true, coins of same type will stack together. If false, coins will stack in order added.")]
    public bool groupSameTypes = true;

    [Header("Tray Capacity")]
    [Tooltip("Maximum number of coins the tray can hold")]
    public int maxCapacity = 10;
    [Tooltip("Reference to LevelManager for auto-matching")]
    public LevelManager levelManager;

    private HorizontalLayout3D layout; // cache layout reference
    
    // Track coins that are currently animating to the tray
    private List<PendingCoin> pendingCoins = new List<PendingCoin>();
    
    // Helper struct to track pending coins
    [System.Serializable]
    private struct PendingCoin
    {
        public Coin coin;
        public CoinType type;
        public int insertIndex;
        
        public PendingCoin(Coin coin, CoinType type, int insertIndex)
        {
            this.coin = coin;
            this.type = type;
            this.insertIndex = insertIndex;
        }
    }

    public List<MergeRule> mergeRules = new List<MergeRule>()
    {
        new MergeRule { inputType = CoinType.Half, requiredCount = 2, outputType = CoinType.One },
        new MergeRule { inputType = CoinType.One, requiredCount = 5, outputType = CoinType.Five },
        new MergeRule { inputType = CoinType.Five, requiredCount = 2, outputType = CoinType.Ten },
        new MergeRule { inputType = CoinType.Ten, requiredCount = 5, outputType = CoinType.Fifty },
        new MergeRule { inputType = CoinType.Fifty, requiredCount = 2, outputType = CoinType.OneHundred },
        new MergeRule { inputType = CoinType.OneHundred, requiredCount = 5, outputType = CoinType.FiveHundred },
        new MergeRule { inputType = CoinType.FiveHundred, requiredCount = 2, outputType = CoinType.OneThousand },
    };

    private void Awake()
    {
        layout = GetComponentInChildren<HorizontalLayout3D>();
        if (layout != null)
        {
            layout.groupSameTypes = groupSameTypes;
        }
    }

    private void OnValidate()
    {
        // Update layout grouping behavior when changed in inspector
        if (layout != null)
        {
            layout.groupSameTypes = groupSameTypes;
        }
    }

    /// <summary>
    /// Animate coin into tray, then parent it in correct slot.
    /// </summary>
    public void AddCoin(Coin coin)
    {
        if (coin == null) return;

        coin.GetComponent<BoxCollider>().enabled = false;

        if (layout == null) layout = GetComponentInChildren<HorizontalLayout3D>();
        if (layout == null)
        {
            Debug.LogWarning("Tray: HorizontalLayout3D not found.");
            return;
        }

        // Sync the grouping behavior
        layout.groupSameTypes = groupSameTypes;

        // 1) compute insertion index considering both existing coins and pending coins
        int insertIndex = GetInsertIndexForCoin(coin.type);

        // 2) Add to pending coins list BEFORE shifting
        pendingCoins.Add(new PendingCoin(coin, coin.type, insertIndex));

        // 3) Immediately shift existing coins to make space and rebuild layout
        ShiftCoinsForInsertion(insertIndex);

        // 4) compute exact world position where the layout would place the new child (after shifting)
        Vector3 targetWorldPos = GetWorldPositionForInsertIndex(insertIndex);

        // 5) detach coin (if it has a parent) so tween works in world space
        coin.transform.SetParent(null, true);

        // 6) Add a small stagger for rapid tapping to prevent position conflicts
        float staggerDelay = 0f;
        if (pendingCoins.Count > 1)
        {
            // Small delay based on how many pending coins of the same type we have
            int sameTypeCount = 0;
            foreach (var pending in pendingCoins)
            {
                if (pending.type == coin.type && pending.coin != coin)
                    sameTypeCount++;
            }
            staggerDelay = sameTypeCount * 0.03f; // 30ms stagger per same-type coin
        }

        // 7) tween directly to that world position with optional stagger
        var tween = coin.transform.DOJump(targetWorldPos, jumpPower, 1, moveDuration)
            .SetEase(Ease.OutQuad);

        if (staggerDelay > 0)
        {
            tween.SetDelay(staggerDelay);
        }

        tween.OnComplete(() =>
        {
            // Remove from pending coins
            pendingCoins.RemoveAll(p => p.coin == coin);

            // Update layout's pending count
            layout.SetPendingCoinsCount(pendingCoins.Count);

            // 8) now parent into the layout but preserve world position so it doesn't snap
            coin.transform.SetParent(layout.transform, true);

            // Set sibling index to the computed insertion index
            int safeIndex = Mathf.Clamp(insertIndex, 0, layout.transform.childCount - 1);
            coin.transform.SetSiblingIndex(safeIndex);

            // 9) track it and run merges
            if (!coins.Contains(coin)) coins.Add(coin);

            // Rebuild layout to finalize positions
            layout.ForceRebuild();
            CheckForMerges();

            // Notify LevelManager about the new coin for auto-matching
            if (levelManager != null)
            {
                levelManager.OnCoinAddedToTray(coin);
            }
        });
        
        // Check if tray is full (including pending coins)
        if (coins.Count + pendingCoins.Count >= maxCapacity)
        {
            Debug.Log(HasAvailableMerge());
            if (!HasAvailableMerge())
            {
                Debug.Log("Tray is full and no merges available! Triggering level fail.");
                UiManager.instance.OnLevelFail();
                return;
            }
        }
    }

    private bool HasAvailableMerge()
    {
        foreach (var rule in mergeRules)
        {
            int count = 0;

            // count from coins already in tray
            foreach (var c in coins)
            {
                if (c.type == rule.inputType)
                    count++;
            }

            // count from coins still pending
            foreach (var p in pendingCoins)
            {
                if (p.type == rule.inputType)
                    count++;
            }

            if (count >= rule.requiredCount)
            {
                return true; // merge is possible
            }
        }
        return false;
    }



    /// <summary>
    /// Get insertion index for a coin type, considering both existing coins and pending coins
    /// </summary>
    private int GetInsertIndexForCoin(CoinType type)
    {
        if (!groupSameTypes)
        {
            // Simple stacking: add after existing coins and pending coins
            return layout.GetValidChildCount() + pendingCoins.Count;
        }

        // Grouping behavior: find the last position of the same type
        int insertIndex = 0;
        int validIndex = 0;
        bool foundMatchingType = false;

        // First, check existing coins in the layout
        for (int i = 0; i < layout.transform.childCount; i++)
        {
            Transform child = layout.transform.GetChild(i);
            if (layout.ignoreInactive && !child.gameObject.activeSelf) continue;

            var coinComp = child.GetComponent<Coin>();
            if (coinComp != null && coinComp.type == type)
            {
                insertIndex = validIndex + 1;
                foundMatchingType = true;
            }

            validIndex++;
        }

        // Now check pending coins and adjust insertion index
        // We need to count how many pending coins of the same type will be inserted before us
        int pendingCoinsOfSameTypeBefore = 0;

        foreach (var pending in pendingCoins)
        {
            if (pending.type == type)
            {
                // Count pending coins of same type
                pendingCoinsOfSameTypeBefore++;
            }
        }

        if (foundMatchingType)
        {
            // Insert after the last existing coin of same type, plus any pending coins of same type
            insertIndex += pendingCoinsOfSameTypeBefore;
        }
        else
        {
            // No matching type found in existing coins
            // Insert at the end, after all existing coins and all pending coins
            insertIndex = validIndex + pendingCoins.Count;
        }

        return insertIndex;
    }

    /// <summary>
    /// Shift existing coins to make space for insertion at the specified index
    /// </summary>
    private void ShiftCoinsForInsertion(int insertIndex)
    {
        // Tell the layout about pending coins so it can calculate positions correctly
        layout.SetPendingCoinsCount(pendingCoins.Count);
        
        // Force rebuild the layout - this will recalculate all positions including space for pending coins
        layout.ForceRebuild();
    }

    /// <summary>
    /// Get world position for a specific insert index
    /// </summary>
    private Vector3 GetWorldPositionForInsertIndex(int insertIndex)
    {
        // Layout now knows about pending coins and calculates positions accordingly
        return layout.GetWorldPositionForInsertIndex(insertIndex);
    }

    private void CheckForMerges()
    {
        foreach (var rule in mergeRules)
        {
            // Count coins of this type
            var matches = new List<Coin>();
            foreach (var c in coins)
            {
                if (c.type == rule.inputType)
                    matches.Add(c);
            }

            if (matches.Count >= rule.requiredCount)
            {
                // Get the coins to merge (first required count)
                var coinsToMerge = matches.GetRange(0, rule.requiredCount);

                // Start merge animation
                StartMergeAnimation(coinsToMerge, rule.outputType);
                return; // important to break - only one merge at a time
            }
        }
    }

    private void StartMergeAnimation(List<Coin> coinsToMerge, CoinType outputType)
    {
        if (coinsToMerge == null || coinsToMerge.Count == 0) return;

        // Calculate middle position of all merging coins
        Vector3 centerPosition = Vector3.zero;
        foreach (var coin in coinsToMerge)
        {
            centerPosition += coin.transform.position;
        }
        centerPosition /= coinsToMerge.Count;
        
        // Add Y offset for merge animation
        Vector3 mergePosition = centerPosition + Vector3.up * mergeYOffset;

        // Create the merged coin at the elevated merge position but make it invisible initially
        Coin mergedCoin = SpawnCoin(outputType);
        levelManager?.RegisterCoin();
        float mergedValue = gridAndCoinGenerator.GetCoinValue(outputType);
        Color mergedColor = gridAndCoinGenerator.GetCoinColor(outputType);
        mergedCoin.SetupCoin(outputType, mergedValue, mergedColor);
        
        // Position the merged coin at elevated merge position and make it invisible
        mergedCoin.transform.position = mergePosition;
        mergedCoin.transform.localScale = Vector3.zero; // Start invisible

        // Remove coins from tracking list
        foreach (var coin in coinsToMerge)
        {
            coins.Remove(coin);
        }

        // Animate all coins to the elevated merge position
        int completedAnimations = 0;
        float animDuration = moveDuration * 0.7f; // Slightly faster than normal movement

        foreach (var coin in coinsToMerge)
        {
            // Move to elevated merge position with a slight jump
            coin.transform.DOJump(mergePosition, jumpPower * 0.5f, 1, animDuration)
                .SetEase(Ease.InOutQuad)
                .OnComplete(() =>
                {
                    completedAnimations++;
                    
                    // When all coins have reached the merge position
                    if (completedAnimations == coinsToMerge.Count)
                    {
                        // Destroy the old coins
                        foreach (var oldCoin in coinsToMerge)
                        {
                            Destroy(oldCoin.gameObject);
                            levelManager?.UnregisterCoin();
                        }

                        // Show the merged coin with a pop animation
                        mergedCoin.transform.DOScale(Vector3.one, 0.3f)
                            .SetEase(Ease.OutBack)
                            .OnComplete(() =>
                            {
                                // Add the merged coin to the tray normally (no jump animation)
                                if (levelManager.MatchesSlot(mergedCoin.type))
                                {
                                    // If it matches a slot, auto-move it there
                                    levelManager.OnCoinAddedToTray(mergedCoin);
                                }
                                else
                                {
                                    AddCoinDirectly(mergedCoin);
                                }

                                // Notify LevelManager about the merged coin for auto-matching
                                if (levelManager != null)
                                {
                                    levelManager.OnCoinAddedToTray(mergedCoin);
                                }

                                // Check for more merges after a short delay
                                DOVirtual.DelayedCall(0.1f, () => CheckForMerges());
                            });
                    }
                });

            //Optional: Add a slight scale down animation while moving
            coin.transform.DOScale(Vector3.one * 0.8f, animDuration * 0.5f)
                .SetLoops(2, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
    }

    /// <summary>
    /// Add coin directly without jump animation (used for merged coins)
    /// </summary>
    private void AddCoinDirectly(Coin coin)
    {
        if (coin == null) return;
        if (layout == null) layout = GetComponentInChildren<HorizontalLayout3D>();
        if (layout == null)
        {
            Debug.LogWarning("Tray: HorizontalLayout3D not found.");
            return;
        }

        // Sync the grouping behavior
        layout.groupSameTypes = groupSameTypes;

        // 1) compute insertion index
        int insertIndex;
        if (groupSameTypes)
        {
            insertIndex = layout.GetInsertIndexForType(coin.type);
        }
        else
        {
            insertIndex = layout.GetValidChildCount();
        }

        // 2) Parent the coin to the layout
        coin.transform.SetParent(layout.transform, true);

        // 3) Set sibling index
        int safeIndex = Mathf.Clamp(insertIndex, 0, layout.transform.childCount - 1);
        coin.transform.SetSiblingIndex(safeIndex);

        // 4) Track it and rebuild layout
        if (!coins.Contains(coin)) coins.Add(coin);

        layout.ForceRebuild();
    }

    private Coin SpawnCoin(CoinType type)
    {
        GameObject prefab = gridAndCoinGenerator.coinPrefab;
        GameObject go = Instantiate(prefab);
        return go.GetComponent<Coin>();
    }

    /// <summary>
    /// Remove coin from tray when moved to slot.
    /// </summary>
    public void RemoveCoin(Coin coin)
    {
        if (coin == null) return;
        if (coins.Contains(coin))
        {
            coins.Remove(coin);
        }
        
        // Also remove from pending coins if it's there
        pendingCoins.RemoveAll(p => p.coin == coin);
        
        // Update layout's pending count
        if (layout != null)
        {
            layout.SetPendingCoinsCount(pendingCoins.Count);
        }
    }

    public void ClearTray()
    {
        coins.Clear();
        pendingCoins.Clear();
        
        // Reset layout's pending count
        if (layout != null)
        {
            layout.SetPendingCoinsCount(0);
        }
    }

    /// <summary>
    /// Check if tray is full (including pending coins)
    /// </summary>
    public bool IsFull()
    {
        return coins.Count + pendingCoins.Count >= maxCapacity;
    }

    /// <summary>
    /// Get current coin count (including pending coins)
    /// </summary>
    public int GetCoinCount()
    {
        return coins.Count + pendingCoins.Count;
    }

    /// <summary>
    /// Get remaining capacity (considering pending coins)
    /// </summary>
    public int GetRemainingCapacity()
    {
        return maxCapacity - (coins.Count + pendingCoins.Count);
    }
}