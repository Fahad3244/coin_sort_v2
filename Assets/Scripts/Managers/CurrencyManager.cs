using UnityEngine;
using System; // for Action

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager instance;

    private const string CASH_KEY = "TotalCash";
    private float totalCash;

    // 🔑 Event for when cash changes
    public event Action<float> OnCashChanged;

    void Awake()
    {
        // Singleton setup
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // persist between scenes
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadCash();

        // Fire initial event so listeners know starting value
        OnCashChanged?.Invoke(totalCash);
    }

    /// <summary>
    /// Add cash to the total and save
    /// </summary>
    public void AddCash(float amount)
    {
        totalCash += amount;
        SaveCash();

        OnCashChanged?.Invoke(totalCash); // 🔥 notify subscribers
    }

    /// <summary>
    /// Subtract cash if enough available
    /// </summary>
    public bool CutCash(float amount)
    {
        if (totalCash >= amount)
        {
            totalCash -= amount;
            SaveCash();

            OnCashChanged?.Invoke(totalCash); // 🔥 notify subscribers
            return true;
        }
        else
        {
            Debug.LogWarning("Not enough cash!");
            return false;
        }
    }

    /// <summary>
    /// Get total cash
    /// </summary>
    public float GetTotalCash()
    {
        return totalCash;
    }

    private void SaveCash()
    {
        PlayerPrefs.SetFloat(CASH_KEY, totalCash);
        PlayerPrefs.Save();
    }

    private void LoadCash()
    {
        totalCash = PlayerPrefs.GetFloat(CASH_KEY, 0f); // default 0
    }

    public void ResetCash()
    {
        totalCash = 0f;
        SaveCash();

        OnCashChanged?.Invoke(totalCash); // 🔥 notify subscribers
    }
}
