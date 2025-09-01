using UnityEngine;
using DG.Tweening;

public class InputManager : MonoBehaviour
{
    [Header("References")]
    public LevelManager levelManager;

    [Header("DOTween Settings")]
    public float moveDuration = 0.8f;
    public float jumpPower = 1f;
    public int jumpNum = 1;
    public Ease moveEase = Ease.InOutQuad;

    private void Start()
    {
        // Subscribe to stack empty events for additional feedback
        StackMonitor.OnStackEmpty += OnStackBecameEmpty;
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        StackMonitor.OnStackEmpty -= OnStackBecameEmpty;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Coin coin = hit.collider.GetComponentInParent<Coin>();
                if (coin != null)
                {
                    TryMoveCoin(coin);
                    UiManager.instance.DisableTutorial();
                }
            }
        }
    }

    private void TryMoveCoin(Coin coin)
    {
        // Check if this coin is nailed and cannot be moved
        if (coin.variant == CoinVariant.Nailed)
        {
            Debug.Log("❌ Cannot move nailed coin! It needs to be unlocked first.");
            
            // Optional: Add some visual feedback for trying to move a nailed coin
            coin.transform.DOPunchRotation(Vector3.forward * 10f, 0.3f, 1, 0.5f);
            
            return; // Early exit for nailed coins
        }
        if (coin.variant == CoinVariant.Locked)
        {
            Debug.Log("❌ Cannot move nailed coin! It needs to be unlocked first.");
            
            // Optional: Add some visual feedback for trying to move a nailed coin
            coin.transform.DOPunchRotation(Vector3.forward * 10f, 0.3f, 1, 0.5f);
            
            return; // Early exit for nailed coins
        }

        bool matched = false;

        coin.CheckAndEnableCoinBelow();

        // Loop through all active slots
        for (int i = 0; i < levelManager.currentLevel.slotTypes.Count; i++)
        {
            if (levelManager.slots[i] == null) continue;

            if (coin.variant == CoinVariant.Mystery)
            {
                coin.RevealMystery();
            }

            if (coin.type == (CoinType)levelManager.slots[i].type)
            {
                MoveCoinToSlot(coin, levelManager.slots[i].transform);
                matched = true;
                break;
            }
        }

        if (!matched)
        {
            MoveCoinToTray(coin);
        }
        
        coin.CheckForSpecialBehaviorOnCoinMove();
    }

    private void MoveCoinToSlot(Coin coin, Transform slotTransform)
    {
        // 🎯 CRITICAL: Register coin removal BEFORE starting the movement
        if (StackMonitor.Instance != null)
        {
            StackMonitor.Instance.RegisterCoinRemoval(coin);
        }
        else
        {
            Debug.LogWarning("StackMonitor.Instance is null! Nailed coins won't be unlocked.");
        }

        coin.transform.DOKill();
        coin.transform.DORotate(new Vector3(90, 0, 0), moveDuration).SetEase(Ease.InCubic);

        coin.transform.DOJump(
            slotTransform.position,
            jumpPower,
            jumpNum,
            moveDuration
        ).SetEase(moveEase).OnComplete(() =>
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(slotTransform.position);
            UiManager.instance.UpdateCoinCount(screenPos, coin.value);
            slotTransform.DOPunchScale(Vector3.one * 0.3f, 0.2f, 1, 0.5f).SetEase(Ease.OutBack);
            Destroy(coin.gameObject);
            levelManager.UnregisterCoin();
            Slot slot = slotTransform.GetComponent<Slot>();
            slot?.PlayEffect();
        });
    }

    private void MoveCoinToTray(Coin coin)
    {
        // 🎯 CRITICAL: Register coin removal BEFORE moving to tray
        if (StackMonitor.Instance != null)
        {
            StackMonitor.Instance.RegisterCoinRemoval(coin);
        }
        else
        {
            Debug.LogWarning("StackMonitor.Instance is null! Nailed coins won't be unlocked.");
        }

        levelManager.tray.AddCoin(coin);
    }

    /// <summary>
    /// Called when a stack becomes empty (optional additional feedback)
    /// </summary>
    private void OnStackBecameEmpty(Vector2Int gridPosition)
    {
        Debug.Log($"🎉 Stack at grid position {gridPosition} became empty! Checking for unlocks...");
        
        // Optional: Add visual effects, sound, or UI feedback when a stack becomes empty
        // For example, you could spawn a particle effect at the empty position
        
        // You could also trigger other game mechanics here, like:
        // - Award bonus points for clearing a stack
        // - Update UI to show progress
        // - Play a special sound effect
    }

    /// <summary>
    /// Debug method to manually trigger stack monitoring debug
    /// </summary>
    [ContextMenu("Debug Stack Monitor")]
    private void DebugStackMonitor()
    {
        if (StackMonitor.Instance != null)
        {
            StackMonitor.Instance.DebugPrintAllStacks();
        }
        else
        {
            Debug.LogError("StackMonitor.Instance is null!");
        }
    }
}