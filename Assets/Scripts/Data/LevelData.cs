using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Insert Slots")]
    public List<SlotType> slotTypes = new List<SlotType>(); // Slot types array

    [Header("Grid Settings")]
    public int gridWidth = 5;
    public int gridHeight = 5;
    public Vector2 cellSpacing = new Vector2(1.5f, 1.5f); // spacing between cells

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
    public Vector2Int gridPosition; // which cell
    public List<CoinData> coins;    // list of coins in that cell
}

[System.Serializable]
public class CoinData
{
    public CoinType type; // value will be assigned automatically
}

// For clarity: Slot requirements stored in LevelManager, not in asset
public class SlotData
{
    public SlotType type;
    public float value;
    public Color color;
}
