using UnityEngine;
using TMPro;

public class Coin : MonoBehaviour
{
    [Header("Coin Info")]
    public CoinType type;
    public float value;
    public Color color;

    [Header("Renderer")]
    public Renderer coinRenderer;
    public TrailRenderer coinTrailRenderer;

    [Header("Text Display")]
    public TextMeshPro valueText;

    /// <summary>
    /// Initialize the coin with type, value, and color
    /// </summary>
    public void SetupCoin(CoinType newType, float newValue, Color newColor)
    {
        type = newType;
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
    }
}
