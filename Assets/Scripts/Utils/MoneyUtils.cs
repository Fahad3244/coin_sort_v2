using UnityEngine;

public static class MoneyUtils
{
    /// <summary>
    /// Format a float value into a short string (e.g., 123_400f -> "123.4k").
    /// Handles decimals properly (e.g., 0.5 -> "0.5", 1.5 -> "1.5").
    /// </summary>
    public static string ToShortString(float value)
    {
        string suffix = "";

        if (Mathf.Abs(value) >= 1_000_000_000f) 
        { 
            value /= 1_000_000_000f; 
            suffix = "b"; 
        }
        else if (Mathf.Abs(value) >= 1_000_000f) 
        { 
            value /= 1_000_000f; 
            suffix = "m"; 
        }
        else if (Mathf.Abs(value) >= 1_000f) 
        { 
            value /= 1_000f; 
            suffix = "k"; 
        }

        // Always keep up to 2 decimals, but drop trailing zeros
        string num = value.ToString("0.##");  
        return num + suffix;
    }

    /// <summary>
    /// Example: show "$12.34" from float cents.
    /// </summary>
    public static string ToCurrency(float cents)
    {
        return $"${(cents / 100f):0.##}";
    }
}
