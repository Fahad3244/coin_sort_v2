using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using System;
using DG.Tweening;

public class Coin : MonoBehaviour
{
    [Header("Coin Info")]
    public CoinType type;
    public CoinVariant variant;
    public float value;
    public Color color;
    public Color mysteryColor;
    private float revealedValue = 0;
    private Color revealedColor = Color.white;

    [Header("Renderer")]
    public Renderer coinRenderer;
    public TrailRenderer coinTrailRenderer;

    [Header("Text Display")]
    public TextMeshPro valueText;

    [Header("Collider")]
    private Collider lastDisabledCollider = null;
    [Header("Objects")]
    public GameObject lockObject;
    private Collider coinCollider;
    private GameObject key;
    [Header("Prefabs")]
    public GameObject keyPrefab;

    void OnEnable()
    {
        coinCollider = GetComponent<Collider>();
    }

    /// <summary>
    /// Initialize the coin with type, value, and color
    /// </summary>
    public void SetupCoin(CoinType newType, CoinVariant newVariant, float newValue, Color newColor)
    {
        type = newType;
        variant = newVariant;
        value = newValue;
        color = newColor;

        // Update material color
        if (coinRenderer != null)
        {
            coinRenderer.material = new Material(coinRenderer.material);
            coinRenderer.material.color = color;
        }

        // Update trail color
        if (coinTrailRenderer != null)
        {
            coinTrailRenderer.material = new Material(coinTrailRenderer.material);
            coinTrailRenderer.material.color = color;
        }

        // Update value text
        if (valueText != null)
        {
            valueText.text = value.ToString();
        }

        if (variant == CoinVariant.Mystery && valueText != null)
        {
            revealedValue = value;
            revealedColor = color;
            valueText.text = "?";
            coinRenderer.material = new Material(coinRenderer.material);
            coinRenderer.material.color = mysteryColor;
        }
        else if (variant == CoinVariant.Locked)
        {
            if (lockObject != null)
            {
                lockObject.SetActive(true);
                coinCollider.enabled = false; // Disable collider until unlocked
            }
        }
        else if (variant == CoinVariant.keyCoin)
        {
            if (keyPrefab != null)
            {
                key = Instantiate(keyPrefab);
                key.transform.position = this.transform.localPosition + new Vector3(0, 0, 0);
                key.transform.localRotation = Quaternion.identity;
            }
        }
        else if (variant == CoinVariant.Nailed)
        {
            
        }

        // 🔎 Check for another Coin below this one
        CheckAndDisableCoinBelow();
    }

    public void CheckForSpecialBehaviorOnCoinMove()
    {
        if (variant == CoinVariant.keyCoin)
        {
            MoveKeyToLock();
        }
    }

    private void MoveKeyToLock()
    {
        // Find the nearest locked coin in the scene
        Coin nearestLockedCoin = null;
        float nearestDistance = float.MaxValue;

        foreach (var coin in FindObjectsOfType<Coin>())
        {
            if (coin.variant == CoinVariant.Locked)
            {
                float distance = Vector3.Distance(this.transform.position, coin.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestLockedCoin = coin;
                }
            }
        }

        if (nearestLockedCoin != null)
        {
            // Move the key to the locked coin's position
            key.transform.DOJump(nearestLockedCoin.transform.position, 4f,1,1f).SetEase(Ease.InOutSine).OnComplete(() =>
            {
                // Unlock the locked coin
                nearestLockedCoin.UnlockCoin();

                // Destroy the key coin after unlocking
                Destroy(key.gameObject);
            });
        }
        
    }

    public void LockCoin()
    {
        if (lockObject != null)
        {
            lockObject.SetActive(true);
            coinCollider.enabled = false; // Disable collider until unlocked
        }
    }

    public void UnlockCoin()
    {
        if (lockObject != null)
        {
            lockObject.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() =>
            {
                lockObject.SetActive(false);
                coinCollider.enabled = true; // Enable collider when unlocked
                variant = CoinVariant.Normal; // Change variant to normal after unlocking
            });
        }
    }

    private void CheckAndDisableCoinBelow()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit))
        {
            Coin otherCoin = hit.collider.GetComponent<Coin>();
            if (otherCoin != null && hit.collider != null)
            {
                lastDisabledCollider = hit.collider;
                hit.collider.enabled = false;
            }
        }
    }
    public void CheckAndEnableCoinBelow()
    {
        if(lastDisabledCollider != null)
        { lastDisabledCollider.enabled = true;
        lastDisabledCollider = null;}
    }

    public void RevealMystery()
    {
        if (variant == CoinVariant.Mystery)
        {
            value = revealedValue;
            color = revealedColor;
            variant = CoinVariant.Normal; // Change variant to normal after revealing

            // Update material color
            if (coinRenderer != null)
            {
                coinRenderer.material.color = color;
            }

            // Update trail color
            if (coinTrailRenderer != null)
            {
                coinTrailRenderer.material.color = color;
            }

            // Update value text
            if (valueText != null)
            {
                valueText.text = value.ToString();
            }
        }
    }
}
