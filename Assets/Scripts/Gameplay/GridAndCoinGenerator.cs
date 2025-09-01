using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GridAndCoinGenerator : MonoBehaviour
{
    public LevelManager levelManager;
    [Header("Level Data")]
    private LevelData levelData;

    [Header("Prefabs")]
    public GameObject cellPrefab;
    public GameObject coinPrefab;   // 7 prefabs in order of CoinType enum

    [Header("Parent Transforms")]
    public Transform cellsParent;
    public Transform coinsParent;

    [Header("Coin Placement")]
    public float firstCoinOffsetY = 0.5f;   // lift first coin above cell
    public float stackOffsetY = 0.3f;       // distance between stacked coins

    [Header("Coin Colors (match CoinType order)")]
    public Color[] coinColors; // Define colors in inspector for each CoinType

    // quick lookup for coin spawning
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
    }

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

                //int coinIndex = Mathf.Clamp((int)coin.type, 0, coinPrefabs.Length - 1);
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

    // Optional helper to get color outside
    public Color GetCoinColor(CoinType type)
    {
        int index = Mathf.Clamp((int)type, 0, coinColors.Length - 1);
        return coinColors != null && coinColors.Length > 0 ? coinColors[index] : Color.white;
    }
}
