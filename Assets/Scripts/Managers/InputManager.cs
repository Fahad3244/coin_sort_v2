using UnityEngine;
using DG.Tweening;

public class InputManager : MonoBehaviour
{
    [Header("References")]
    public LevelManager levelManager;

    [Header("DOTween Settings")]
    public float moveDuration = 0.8f;     // time to reach the slot
    public float jumpPower = 1f;          // height of the jump
    public int jumpNum = 1;               // number of jumps
    public Ease moveEase = Ease.InOutQuad;           // ease of movement (optional, can use Ease.InOutQuad)

    /// <summary>
    /// Called when user clicks/touches a coin
    /// </summary>
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
                    TryMoveCoinToSlot(coin);
                    UiManager.instance.DisableTutorial();
                }
            }
        }
    }

    private void TryMoveCoinToSlot(Coin coin)
    {
        bool matched = false;

        // Loop through all active slots
        for (int i = 0; i < levelManager.currentLevel.slotTypes.Count; i++)
        {
            if (levelManager.slots[i] == null) continue;

            if (coin.type == (CoinType)levelManager.slots[i].type)
            {
                MoveCoinToSlot(coin, levelManager.slots[i].transform);
                matched = true;
                break; // stop after first match
            }
        }

        if (!matched)
        {
            MoveCoinToTray(coin);
        }
    }


    private void MoveCoinToSlot(Coin coin, Transform slotTransform)
    {
        // Optional: stop any previous tweens
        coin.transform.DOKill();

        coin.transform.DORotate(new Vector3(90, 0, 0), moveDuration).SetEase(Ease.InCubic);

        // Use DOJump for jump effect
        coin.transform.DOJump(
            slotTransform.position,      // end position
            jumpPower,    // jump height
            jumpNum,      // number of jumps
            moveDuration  // duration
        ).SetEase(moveEase).OnComplete(() =>
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(slotTransform.position);
            UiManager.instance.UpdateCoinCount(screenPos, coin.value);
            slotTransform.DOPunchScale(Vector3.one * 0.3f, 0.2f, 1, 0.5f).SetEase(Ease.OutBack);
            Destroy(coin.gameObject); // remove coin after reaching slot
            levelManager.UnregisterCoin();
            Slot slot = slotTransform.GetComponent<Slot>();
            slot?.PlayEffect();
        });
    }

    private void MoveCoinToTray(Coin coin)
    {
        levelManager.tray.AddCoin(coin);
    }
}
