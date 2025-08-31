using UnityEngine;
using System.Collections.Generic;

// Define coin variants as an enum - easily extensible
public enum CoinVariant
{
    Normal,
    Mystery,
    Nailed,
    Locked,
    keyCoin
    // Add more variants here as needed
}

[CreateAssetMenu(menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Insert Slots")]
    public List<SlotType> slotTypes = new List<SlotType>();

    [Header("Grid Settings")]
    public int gridWidth = 5;
    public int gridHeight = 5;
    public Vector2 cellSpacing = new Vector2(1.5f, 1.5f);

    [Header("Disabled Cells (make random shaped grid)")]
    public List<Vector2Int> disabledCells = new List<Vector2Int>();

    [Header("Coin Layout")]
    public List<CellCoins> cellCoins = new List<CellCoins>();
    
    [Header("Coin Count")]
    public float bounusCoinCount = 0; // extra coins to add to total

    [Header("Grid Offset")]
    public Vector3 gridOffset = Vector3.zero;
}

[System.Serializable]
public class CellCoins
{
    public Vector2Int gridPosition;
    public List<CoinData> coins;
}

[System.Serializable]
public class CoinData
{
    public CoinType type;
    public CoinVariant variant; // No need to explicitly set default
}

// Extension methods for easy checking
public static class CoinDataExtensions
{
    public static bool IsMystery(this CoinData coin)
    {
        return coin.variant == CoinVariant.Mystery;
    }
    
    public static bool IsSpecial(this CoinData coin)
    {
        return coin.variant != CoinVariant.Normal;
    }
    
}

public class SlotData
{
    public SlotType type;
    public float value;
    public Color color;
}