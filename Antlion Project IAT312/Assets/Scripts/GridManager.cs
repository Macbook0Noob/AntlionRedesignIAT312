using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public const int WIDTH = 9; 

    [System.NonSerialized] public TileType[,] grid;
    [System.NonSerialized] public GridCell[,] visualCells;
    public int totalHeight;

    public GameObject gridCellPrefab;
    public TileSpriteData spriteData;

    public void GenerateBoard(LevelDifficultyConfig config)
    {
        totalHeight = config.finishLineDistance;
        grid = new TileType[WIDTH, totalHeight];
        visualCells = new GridCell[WIDTH, totalHeight];

        // 4 safe bottom rows (0, 1, 2, 3) where no obstacles or special tiles can spawn
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < WIDTH; x++)
            {
                grid[x, y] = TileType.Empty;
            }
        }

        // Finish line row at top
        for (int x = 0; x < WIDTH; x++)
        {
            grid[x, totalHeight - 1] = TileType.FinishLine;
        }

        // --- PASS 1: Base Obstacles (Row 4 onward) ---
        for (int y = 4; y < totalHeight - 1; y++)
        {
            for (int x = 0; x < WIDTH; x++)
            {
                if (Random.value < config.obstacleDensity)
                {
                    grid[x, y] = (Random.value > 0.4f) ? TileType.Wall : TileType.River;
                }
                else
                {
                    grid[x, y] = TileType.Empty;
                }
            }
        }

        // --- PASS 2: Unique Tiles (Row 4 onward) ---
        for (int y = 4; y < totalHeight - 1; y++)
        {
            for (int x = 0; x < WIDTH; x++)
            {
                if (grid[x, y] != TileType.Empty) continue;
                if (HasAdjacentSpecialTile(x, y)) continue;

                // Priority: bomb beneath an ice wall
                bool isBelowIceWall = (y < totalHeight - 2 && grid[x, y + 1] == TileType.Wall);

                if (isBelowIceWall && Random.value < 0.70f)
                {
                    grid[x, y] = TileType.Bomb;
                    continue;
                }

                if (Random.value < config.specialTileDensity)
                {
                    float specialRoll = Random.value;
                    if (specialRoll < 0.20f)
                    {
                        grid[x, y] = TileType.Bomb;
                    }
                    else if (specialRoll < 0.60f)
                    {
                        grid[x, y] = TileType.JumpPad;
                    }
                    else
                    {
                        grid[x, y] = TileType.BonusPoint;
                    }
                }
            }
        }

        EnsurePathExists();
        SpawnVisuals();
    }

    private bool HasAdjacentSpecialTile(int originX, int originY)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = originX + dx;
                int ny = originY + dy;

                if (nx >= 0 && nx < WIDTH && ny >= 0 && ny < totalHeight)
                {
                    TileType neighbor = grid[nx, ny];
                    if (neighbor == TileType.Bomb || 
                        neighbor == TileType.JumpPad || 
                        neighbor == TileType.BonusPoint)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public bool IsTraversable(int x, int y)
    {
        if (x < 0 || x >= WIDTH || y < 0 || y >= totalHeight) return false;
        return grid[x, y] != TileType.Wall;
    }

    public bool IsWater(int x, int y)
    {
        if (x < 0 || x >= WIDTH || y < 0 || y >= totalHeight) return false;
        return grid[x, y] == TileType.River;
    }

    public void ClearTile(int x, int y)
    {
        if (x >= 0 && x < WIDTH && y >= 0 && y < totalHeight)
        {
            grid[x, y] = TileType.Empty;
            if (visualCells != null && visualCells[x, y] != null)
            {
                visualCells[x, y].Setup(TileType.Empty, spriteData);
            }
        }
    }

    public void ClearAllVisualCells()
    {
        if (visualCells != null)
        {
            for (int y = 0; y < totalHeight; y++)
            {
                for (int x = 0; x < WIDTH; x++)
                {
                    if (visualCells[x, y] != null)
                    {
                        Destroy(visualCells[x, y].gameObject);
                    }
                }
            }
            visualCells = null;
        }

        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }

    private void EnsurePathExists()
    {
        int currentX = WIDTH / 2;
        for (int y = 4; y < totalHeight; y++)
        {
            if (grid[currentX, y] == TileType.Wall)
            {
                grid[currentX, y] = TileType.Empty;
            }
            currentX = Mathf.Clamp(currentX + Random.Range(-1, 2), 0, WIDTH - 1);
        }
    }

    private void SpawnVisuals()
    {
        for (int y = 0; y < totalHeight; y++)
        {
            for (int x = 0; x < WIDTH; x++)
            {
                GameObject obj = Instantiate(gridCellPrefab, new Vector3(x, y, 0), Quaternion.identity, transform);
                GridCell cell = obj.GetComponent<GridCell>();
                cell.Setup(grid[x, y], spriteData);
                visualCells[x, y] = cell;
            }
        }
    }
}