using UnityEngine;

public class Slot : MonoBehaviour
{
    [Header("Slot Info")]
    public SlotType type;
    public float value;
    public Color color;

    [Header("Renderer")]
    public Renderer slotRenderer; // material to update color
    [Header("Effect")]
    public ParticleSystem effectPrefab;

    public void SetupSlot(SlotType newType, float newValue, Color newColor)
    {
        type = newType;
        value = newValue;
        color = newColor;

        // Update material color
        if (slotRenderer != null)
        {
            slotRenderer.material = new Material(slotRenderer.material); // clone to avoid shared material issues
            slotRenderer.material.color = color;
        }
    }

    public void PlayEffect()
    {
        if (effectPrefab != null)
        {
            effectPrefab.Play();
        }
    }
}
