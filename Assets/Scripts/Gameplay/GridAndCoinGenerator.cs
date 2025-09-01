// Integration with GridAndCoinGenerator
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class GridAndCoinGenerator : MonoBehaviour
{
    public LevelManager levelManager;
    
    [Header("Level Data")]
    private LevelData levelData;

    [Header("Prefabs")]
    public GameObject cellPrefab;
    public GameObject coinPrefab;

    [Header("Parent Transforms")]
    public Transform cellsParent;
    public Transform coinsParent;

    [Header("Coin Placement")]
    public float firstCoinOffsetY = 0.5f;
    public float stackOffsetY = 0.3f;

    [Header("Coin Colors")]
    public Color[] coinColors;

    [Header("Stack Monitor")]
    public StackMonitor stackMonitor; // 🎯 Reference to StackMonitor

    private Dictionary<Vector2Int, Transform> cellLookup = new Dictionary<Vector2Int, Transform>();

    private void Start()
    {
        levelData = levelManager.currentLevel;
        if (levelData == null)
        {
            Debug.LogError("LevelData not assigned!");
            return;
        }

        GenerateGrid();
        GenerateCoins();
        
        // 🎯 Initialize stack monitor AFTER generating coins
        InitializeStackMonitor();
    }

    private void InitializeStackMonitor()
    {
        if (stackMonitor != null)
        {
            Debug.Log("Initializing StackMonitor...");
            stackMonitor.Initialize(levelData);
            
            // Optional: Do some initial debugging
            stackMonitor.DebugPrintAllStacks();
            stackMonitor.ForceCheckAllNailedCoins();
        }
        else
        {
            Debug.LogError("StackMonitor reference not assigned in GridAndCoinGenerator!");
        }
    }

    // Your existing GenerateGrid method stays the same
    private void GenerateGrid()
    {
        cellLookup.Clear();

        for (int x = 0; x < levelData.gridWidth; x++)
        {
            for (int z = 0; z < levelData.gridHeight; z++)
            {
                Vector2Int currentPos = new Vector2Int(x, z);

                if (levelData.disabledCells.Contains(currentPos))
                    continue;

                Vector3 spawnPos = new Vector3(
                    x * levelData.cellSpacing.x,
                    0f,
                    z * levelData.cellSpacing.y
                ) + levelData.gridOffset;

                GameObject cell = Instantiate(cellPrefab, spawnPos, Quaternion.identity, cellsParent);
                cell.name = $"Cell_{x}_{z}";

                cellLookup[currentPos] = cell.transform;
            }
        }
    }

    // Your existing GenerateCoins method stays the same
    private void GenerateCoins()
    {
        foreach (var cellCoin in levelData.cellCoins)
        {
            if (cellCoin == null || cellCoin.coins == null || cellCoin.coins.Count == 0)
                continue;

            if (!cellLookup.ContainsKey(cellCoin.gridPosition))
                continue;

            Transform cellTransform = cellLookup[cellCoin.gridPosition];
            float currentYOffset = firstCoinOffsetY;

            foreach (var coin in cellCoin.coins)
            {
                if (coin == null) continue;

                GameObject prefab = coinPrefab;
                Vector3 spawnPos = cellTransform.position + new Vector3(0f, currentYOffset, 0f);

                GameObject coinObj = Instantiate(prefab, spawnPos, Quaternion.identity, coinsParent);

                Coin coinComponent = coinObj.GetComponent<Coin>();
                if (coinComponent != null)
                {
                    coinComponent.SetupCoin(
                        coin.type,
                        coin.variant,
                        GetCoinValue(coin.type),
                        GetCoinColor(coin.type),
                        coin.unlockDirections
                    );
                }

                currentYOffset += stackOffsetY;
                levelManager.RegisterCoin();
            }
        }
    }

    public float GetCoinValue(CoinType type)
    {
        switch (type)
        {
            case CoinType.Half: return 0.5f;
            case CoinType.One: return 1f;
            case CoinType.Five: return 5f;
            case CoinType.Ten: return 10f;
            case CoinType.Fifty: return 50f;
            case CoinType.OneHundred: return 100f;
            case CoinType.FiveHundred: return 500f;
            case CoinType.OneThousand: return 1000f;
            default: return 0f;
        }
    }

    public Color GetCoinColor(CoinType type)
    {
        int index = Mathf.Clamp((int)type, 0, coinColors.Length - 1);
        return coinColors != null && coinColors.Length > 0 ? coinColors[index] : Color.white;
    }
}