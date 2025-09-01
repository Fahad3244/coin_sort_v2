using UnityEngine;
using System.Collections.Generic;
using System;
using DG.Tweening;

public class StackMonitor : MonoBehaviour
{
    public static StackMonitor Instance { get; private set; }
    
    // Event triggered when a stack becomes empty
    public static event Action<Vector2Int> OnStackEmpty;
    
    // Dictionary to track coins in each grid position
    private Dictionary<Vector2Int, List<Coin>> stackTracker = new Dictionary<Vector2Int, List<Coin>>();
    
    // Reference to level data for grid information
    private LevelData levelData;

    [Header("Nail Removal Effects")]
    [Tooltip("Sound effect for nail removal")]
    public AudioClip nailRemovalSound;
    [Tooltip("Particle effect for nail removal")]
    public GameObject nailRemovalParticles;
    [Tooltip("Choose animation style for nail removal")]
    public NailAnimationStyle nailAnimationStyle = NailAnimationStyle.Realistic;

    public enum NailAnimationStyle
    {
        Realistic,    // Wiggle, extract, fall
        Explosive     // Quick spin and explosive removal
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public void Initialize(LevelData data)
    {
        levelData = data;
        BuildStackTracker();
        
        Debug.Log($"StackMonitor initialized with {stackTracker.Count} stacks");
    }

    /// <summary>
    /// Build the initial stack tracker by finding all coins in the scene
    /// </summary>
    private void BuildStackTracker()
    {
        stackTracker.Clear();
        
        // Find all coins in the scene and organize them by grid position
        Coin[] allCoins = FindObjectsOfType<Coin>();
        
        foreach (var coin in allCoins)
        {
            Vector2Int gridPos = WorldToGridPosition(coin.transform.position);
            
            if (!stackTracker.ContainsKey(gridPos))
            {
                stackTracker[gridPos] = new List<Coin>();
            }
            
            stackTracker[gridPos].Add(coin);
        }
        
        // Debug: Print initial stack counts
        foreach (var kvp in stackTracker)
        {
            Debug.Log($"Stack at {kvp.Key}: {kvp.Value.Count} coins");
        }
    }

    /// <summary>
    /// Register that a coin has been removed from its stack
    /// This will trigger unlock checks for nailed coins
    /// </summary>
    public void RegisterCoinRemoval(Coin coin)
    {
        Vector2Int gridPos = WorldToGridPosition(coin.transform.position);
        
        if (stackTracker.ContainsKey(gridPos))
        {
            stackTracker[gridPos].Remove(coin);
            
            Debug.Log($"Coin removed from stack at {gridPos}. Remaining: {stackTracker[gridPos].Count}");
            
            // Check if stack is now empty
            if (stackTracker[gridPos].Count == 0)
            {
                Debug.Log($"🎯 Stack at {gridPos} is now EMPTY!");
                OnStackEmpty?.Invoke(gridPos);
                
                // Check for nailed coins that can be unlocked from this direction
                CheckNailedCoinsForUnlock(gridPos);
            }
        }
        else
        {
            Debug.LogWarning($"Tried to remove coin from untracked position: {gridPos}");
        }
    }

    /// <summary>
    /// Check all adjacent positions for nailed coins that can be unlocked
    /// </summary>
    private void CheckNailedCoinsForUnlock(Vector2Int emptyPosition)
    {
        // Define direction mappings
        // If a position becomes empty, check adjacent positions for nailed coins
        // The unlock direction is OPPOSITE to the relative position
        
        var directionChecks = new[]
        {
            new { offset = Vector2Int.up, unlockDir = UnlockDirection.Down, name = "UP" },
            new { offset = Vector2Int.down, unlockDir = UnlockDirection.Up, name = "DOWN" },
            new { offset = Vector2Int.left, unlockDir = UnlockDirection.Right, name = "LEFT" },
            new { offset = Vector2Int.right, unlockDir = UnlockDirection.Left, name = "RIGHT" }
        };

        foreach (var check in directionChecks)
        {
            Vector2Int checkPosition = emptyPosition + check.offset;
            
            Debug.Log($"Checking {check.name} of empty position {emptyPosition} -> {checkPosition}");
            
            if (stackTracker.ContainsKey(checkPosition))
            {
                var coinsAtPosition = stackTracker[checkPosition];
                
                foreach (var coin in coinsAtPosition)
                {
                    if (coin != null && coin.variant == CoinVariant.Nailed && 
                        (coin.unlockDirections & check.unlockDir) != 0)
                    {
                        Debug.Log($"🔓 Unlocking nailed coin at {checkPosition} from direction {check.unlockDir}");
                        UnlockNailedCoin(coin);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Unlock a nailed coin with dramatic nail removal animation
    /// </summary>
    private void UnlockNailedCoin(Coin nailedCoin)
    {
        // Play sound effect
        PlayNailRemovalSound();
        
        // Spawn particles
        SpawnNailRemovalParticles(nailedCoin.transform.position);
        
        // Enable the coin's collider immediately
        Collider coinCollider = nailedCoin.GetComponent<Collider>();
        if (coinCollider != null)
        {
            coinCollider.enabled = true;
        }
        
        // Change variant to normal
        nailedCoin.variant = CoinVariant.Normal;
        
        // Animate the nail removal with chosen style
        if (nailedCoin.nail != null)
        {
            switch (nailAnimationStyle)
            {
                case NailAnimationStyle.Realistic:
                    AnimateNailRemovalRealistic(nailedCoin.nail, nailedCoin);
                    break;
                case NailAnimationStyle.Explosive:
                    AnimateNailRemovalExplosive(nailedCoin.nail, nailedCoin);
                    break;
            }
        }
        else
        {
            // If no nail object, just do coin animation
            AnimateUnlockedCoin(nailedCoin);
        }
        
        Debug.Log($"✨ Nailed coin at {WorldToGridPosition(nailedCoin.transform.position)} has been unlocked!");
    }

    /// <summary>
    /// Play sound effect for nail removal
    /// </summary>
    private void PlayNailRemovalSound()
    {
        if (nailRemovalSound != null)
        {
            // Create a temporary audio source for the sound
            GameObject audioGO = new GameObject("NailRemovalSound");
            AudioSource audioSource = audioGO.AddComponent<AudioSource>();
            audioSource.clip = nailRemovalSound;
            audioSource.volume = 0.7f;
            audioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f); // Slight pitch variation
            audioSource.Play();
            
            // Destroy after sound finishes
            Destroy(audioGO, nailRemovalSound.length + 0.1f);
        }
    }

    /// <summary>
    /// Spawn particle effects for nail removal
    /// </summary>
    private void SpawnNailRemovalParticles(Vector3 position)
    {
        if (nailRemovalParticles != null)
        {
            GameObject particles = Instantiate(nailRemovalParticles, position + Vector3.up * 0.5f, Quaternion.identity);
            
            // Auto-destroy particles after a few seconds
            Destroy(particles, 3f);
        }
    }

    /// <summary>
    /// Realistic nail removal: wiggle, extract, fall
    /// </summary>
    private void AnimateNailRemovalRealistic(GameObject nail, Coin coin)
    {
        // Store original nail transform
        Vector3 originalPos = nail.transform.position;
        Vector3 originalRotation = nail.transform.eulerAngles;
        
        // Create animation sequence
        Sequence nailSequence = DOTween.Sequence();
        
        // Phase 1: Nail loosening - wiggle effect (like it's being worked loose)
        nailSequence.Append(nail.transform.DOPunchRotation(new Vector3(0, 0, 15f), 0.2f, 3, 0.3f))
                   .Join(nail.transform.DOPunchPosition(Vector3.up * 0.05f, 0.2f, 2, 0.3f));
        
        // Phase 2: Nail extraction - dramatic pull out with rotation
        nailSequence.Append(nail.transform.DORotate(originalRotation + new Vector3(15f, 45f, 30f), 0.4f).SetEase(Ease.OutBack))
                   .Join(nail.transform.DOMoveY(originalPos.y + 2f, 0.4f).SetEase(Ease.OutBack))
                   .Join(nail.transform.DOScale(Vector3.one * 1.3f, 0.3f).SetEase(Ease.OutBack));
        
        // Phase 3: Nail falling with realistic tumbling
        nailSequence.Append(nail.transform.DORotate(originalRotation + new Vector3(270f, 180f, 90f), 1f, RotateMode.FastBeyond360).SetEase(Ease.InQuad))
                   .Join(nail.transform.DOMoveY(originalPos.y - 3f, 1f).SetEase(Ease.InQuad))
                   .Join(nail.transform.DOMove(originalPos + new Vector3(UnityEngine.Random.Range(-1f, 1f), -3f, UnityEngine.Random.Range(-1f, 1f)), 1f).SetEase(Ease.InQuad));
        
        // Phase 4: Nail shrinking as it "disappears"
        nailSequence.Append(nail.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack));
        
        // Phase 5: Cleanup
        nailSequence.OnComplete(() => {
            nail.SetActive(false);
            ResetNailTransform(nail, originalPos, originalRotation);
        });
        
        // Animate the coin during nail removal
        AnimateUnlockedCoin(coin);
    }

    /// <summary>
    /// Explosive nail removal: quick and dramatic
    /// </summary>
    private void AnimateNailRemovalExplosive(GameObject nail, Coin coin)
    {
        Vector3 originalPos = nail.transform.position;
        Vector3 originalRotation = nail.transform.eulerAngles;
        
        Sequence explosiveSequence = DOTween.Sequence();
        
        // Phase 1: Quick buildup
        explosiveSequence.Append(nail.transform.DOScale(Vector3.one * 1.5f, 0.1f).SetEase(Ease.OutBack))
                        .Join(nail.transform.DOPunchRotation(new Vector3(0, 0, 30f), 0.1f, 1, 1f));
        
        // Phase 2: Explosive launch with multiple rotations
        explosiveSequence.Append(nail.transform.DOScale(Vector3.one * 0.8f, 0.1f))
                        .Append(nail.transform.DORotate(new Vector3(0, 0, 720f), 0.6f, RotateMode.FastBeyond360).SetEase(Ease.OutQuad))
                        .Join(nail.transform.DOMoveY(originalPos.y + 4f, 0.3f).SetEase(Ease.OutQuad))
                        .Join(nail.transform.DOMoveY(originalPos.y - 4f, 0.3f).SetEase(Ease.InQuad).SetDelay(0.3f));
        
        // Phase 3: Shrink to nothing
        explosiveSequence.Append(nail.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack));
        
        explosiveSequence.OnComplete(() => {
            nail.SetActive(false);
            ResetNailTransform(nail, originalPos, originalRotation);
        });
        
        AnimateUnlockedCoin(coin);
    }

    /// <summary>
    /// Reset nail transform for potential reuse
    /// </summary>
    private void ResetNailTransform(GameObject nail, Vector3 originalPos, Vector3 originalRotation)
    {
        nail.transform.position = originalPos;
        nail.transform.rotation = Quaternion.Euler(originalRotation);
        nail.transform.localScale = Vector3.one;
    }

    /// <summary>
    /// Animate the coin celebration when unlocked
    /// </summary>
    private void AnimateUnlockedCoin(Coin coin)
    {
        // Coin celebration animation sequence
        Sequence coinSequence = DOTween.Sequence();
        
        // Phase 1: Initial relief shake
        coinSequence.Append(coin.transform.DOPunchRotation(new Vector3(0, 0, 15f), 0.4f, 2, 0.3f))
                   .Join(coin.transform.DOPunchPosition(Vector3.up * 0.15f, 0.4f, 1, 0.3f));
        
        // Phase 2: Freedom bounce with scale
        coinSequence.Append(coin.transform.DOPunchScale(Vector3.one * 0.4f, 0.5f, 1, 0.5f).SetEase(Ease.OutBack))
                   .Join(coin.transform.DOPunchPosition(Vector3.up * 0.25f, 0.5f, 1, 0.3f));
        
        // Phase 3: Subtle glow effect
        if (coin.coinRenderer != null)
        {
            Material coinMaterial = coin.coinRenderer.material;
            Color originalColor = coinMaterial.color;
            
            // Brief golden glow effect
            Color glowColor = originalColor + Color.yellow * 0.3f;
            coinSequence.Insert(0.2f, coinMaterial.DOColor(glowColor, 0.2f).SetEase(Ease.OutFlash)
                               .OnComplete(() => coinMaterial.DOColor(originalColor, 0.4f).SetEase(Ease.InOutQuad)));
        }
        
        // Phase 4: Trail effect enhancement
        if (coin.coinTrailRenderer != null)
        {
            var trail = coin.coinTrailRenderer;
            float originalWidth = trail.widthMultiplier;
            
            coinSequence.Insert(0.3f, DOTween.To(() => trail.widthMultiplier, 
                                                x => trail.widthMultiplier = x, 
                                                originalWidth * 2f, 0.2f)
                                    .OnComplete(() => DOTween.To(() => trail.widthMultiplier,
                                                               x => trail.widthMultiplier = x,
                                                               originalWidth, 0.3f)));
        }
    }

    /// <summary>
    /// Convert world position to grid coordinates
    /// </summary>
    private Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        if (levelData == null) 
        {
            Debug.LogError("LevelData is null in WorldToGridPosition!");
            return Vector2Int.zero;
        }
        
        Vector3 localPos = worldPosition - levelData.gridOffset;
        
        int x = Mathf.RoundToInt(localPos.x / levelData.cellSpacing.x);
        int z = Mathf.RoundToInt(localPos.z / levelData.cellSpacing.y);
        
        return new Vector2Int(x, z);
    }

    /// <summary>
    /// Check if a stack at given position is empty
    /// </summary>
    public bool IsStackEmpty(Vector2Int gridPosition)
    {
        return !stackTracker.ContainsKey(gridPosition) || stackTracker[gridPosition].Count == 0;
    }

    /// <summary>
    /// Get the number of coins in a stack
    /// </summary>
    public int GetStackCount(Vector2Int gridPosition)
    {
        return stackTracker.ContainsKey(gridPosition) ? stackTracker[gridPosition].Count : 0;
    }

    /// <summary>
    /// Debug method to print current stack states
    /// </summary>
    [ContextMenu("Debug Print All Stacks")]
    public void DebugPrintAllStacks()
    {
        Debug.Log("=== CURRENT STACK STATES ===");
        
        if (stackTracker.Count == 0)
        {
            Debug.Log("No stacks tracked!");
            return;
        }
        
        foreach (var kvp in stackTracker)
        {
            var position = kvp.Key;
            var coins = kvp.Value;
            
            string coinInfo = "";
            foreach (var coin in coins)
            {
                if (coin != null)
                {
                    coinInfo += $"{coin.variant}({coin.type}) ";
                }
            }
            
            Debug.Log($"Position {position}: {coins.Count} coins -> {coinInfo}");
        }
        
        Debug.Log("============================");
    }

    /// <summary>
    /// Add a coin to tracking (useful if coins are spawned during gameplay)
    /// </summary>
    public void RegisterNewCoin(Coin coin)
    {
        Vector2Int gridPos = WorldToGridPosition(coin.transform.position);
        
        if (!stackTracker.ContainsKey(gridPos))
        {
            stackTracker[gridPos] = new List<Coin>();
        }
        
        if (!stackTracker[gridPos].Contains(coin))
        {
            stackTracker[gridPos].Add(coin);
            Debug.Log($"New coin registered at {gridPos}. Stack count: {stackTracker[gridPos].Count}");
        }
    }

    /// <summary>
    /// Force check all nailed coins (useful for debugging)
    /// </summary>
    [ContextMenu("Force Check All Nailed Coins")]
    public void ForceCheckAllNailedCoins()
    {
        foreach (var kvp in stackTracker)
        {
            foreach (var coin in kvp.Value)
            {
                if (coin != null && coin.variant == CoinVariant.Nailed)
                {
                    Debug.Log($"Found nailed coin at {kvp.Key} with unlock directions: {coin.unlockDirections}");
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}