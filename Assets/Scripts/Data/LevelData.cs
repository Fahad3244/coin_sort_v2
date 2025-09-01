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

// Directions for nailed coin unlocking
[System.Flags]
public enum UnlockDirection
{
    None = 0,
    Up = 1 << 0,      // 1
    Down = 1 << 1,    // 2
    Left = 1 << 2,    // 4
    Right = 1 << 3    // 8
    // Using flags allows multiple directions: Up | Down = 3
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
    public CoinVariant variant;

    [ConditionalField("variant", CoinVariant.Nailed)]
    public UnlockDirection unlockDirections = UnlockDirection.None;
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
    
    public static bool IsNailed(this CoinData coin)
    {
        return coin.variant == CoinVariant.Nailed;
    }
    
    public static bool CanUnlockFrom(this CoinData coin, UnlockDirection direction)
    {
        if (!coin.IsNailed()) return false;
        
        // Check using flags - supports multiple directions
        return (coin.unlockDirections & direction) != 0;
    }
    
    public static Vector2Int GetDirectionVector(this UnlockDirection direction)
    {
        return direction switch
        {
            UnlockDirection.Up => Vector2Int.up,
            UnlockDirection.Down => Vector2Int.down,
            UnlockDirection.Left => Vector2Int.left,
            UnlockDirection.Right => Vector2Int.right,
            _ => Vector2Int.zero
        };
    }
}

// Custom attribute for conditional fields (you'll need a custom property drawer for this)
public class ConditionalFieldAttribute : PropertyAttribute
{
    public string fieldName;
    public object value;
    
    public ConditionalFieldAttribute(string fieldName, object value)
    {
        this.fieldName = fieldName;
        this.value = value;
    }
}

// Example usage in game logic
public class NailedCoinManager : MonoBehaviour
{
    public bool CanUnlockNailedCoin(CoinData nailedCoin, Vector2Int coinPosition, LevelData levelData)
    {
        if (!nailedCoin.IsNailed()) return true; // Not nailed, can always unlock
        
        // Check each possible unlock direction
        var directions = System.Enum.GetValues(typeof(UnlockDirection)) as UnlockDirection[];
        
        foreach (var direction in directions)
        {
            if (direction == UnlockDirection.None) continue;
            
            if (nailedCoin.CanUnlockFrom(direction))
            {
                Vector2Int checkPosition = coinPosition + direction.GetDirectionVector();
                
                // Check if the adjacent cell in this direction is empty or has no blocking coins
                if (IsPositionClearForUnlock(checkPosition, levelData))
                {
                    return true; // Found at least one valid unlock direction
                }
            }
        }
        
        return false; // No valid unlock directions available
    }
    
    private bool IsPositionClearForUnlock(Vector2Int position, LevelData levelData)
    {
        // Check if position is out of bounds
        if (position.x < 0 || position.x >= levelData.gridWidth || 
            position.y < 0 || position.y >= levelData.gridHeight)
            return true; // Out of bounds counts as "clear"
        
        // Check if position is disabled
        if (levelData.disabledCells.Contains(position))
            return true; // Disabled cells count as "clear"
        
        // Check if position has any coins
        var cellData = levelData.cellCoins.Find(cell => cell.gridPosition == position);
        return cellData == null || cellData.coins.Count == 0;
    }
}

public class SlotData
{
    public SlotType type;
    public float value;
    public Color color;
}