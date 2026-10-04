using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Managers & Entities")]
    public GridManager gridManager;
    public PlayerController player;
    public LevelDifficultyConfig config;

    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI countdownText; // Centered countdown display

    [Header("Effects Prefabs")]
    public GameObject floatingScorePrefab;

    public int CurrentScrollRow { get; private set; } = 0;
    public bool IsGameOver { get; private set; } = false;
    public bool IsPausedAtStart { get; private set; } = true;

    private int currentLevel = 1;
    private int highestRow = 1;
    private int score = 0;
    private float scrollTimer = 0f;

    private HashSet<Vector2Int> activeBombs = new HashSet<Vector2Int>();

    private void Start()
    {
        UpdateUI();
        StartLevel();
    }

    private void StartLevel()
    {
        CurrentScrollRow = 0;
        highestRow = 1;
        scrollTimer = 0f;
        activeBombs.Clear();

        gridManager.transform.position = Vector3.zero;
        gridManager.GenerateBoard(config);
        player.Initialize(gridManager, this, config);
        UpdateUI();

        // Start countdown routine (3... 2... 1... GO!)
        StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        IsPausedAtStart = true;

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);

            // 3
            countdownText.text = "3";
            countdownText.color = new Color(1f, 0.95f, 0.2f, 1f); // Vibrant Yellow
            yield return StartCoroutine(PulseTextEffect());

            // 2
            countdownText.text = "2";
            countdownText.color = new Color(1f, 0.65f, 0.1f, 1f); // Orange
            yield return StartCoroutine(PulseTextEffect());

            // 1
            countdownText.text = "1";
            countdownText.color = new Color(0.2f, 0.9f, 1f, 1f); // Cyan
            yield return StartCoroutine(PulseTextEffect());

            // GO!
            countdownText.text = "GO!";
            countdownText.color = new Color(0.2f, 1f, 0.4f, 1f); // Neon Green
            StartCoroutine(PulseTextEffect());

            yield return new WaitForSeconds(0.4f);
            countdownText.gameObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(3.0f);
        }

        IsPausedAtStart = false;
    }

    private IEnumerator PulseTextEffect()
    {
        if (countdownText == null) yield break;

        float duration = 0.85f;
        float elapsed = 0f;
        Vector3 startScale = Vector3.one * 1.5f;
        Vector3 endScale = Vector3.one * 1.0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Punchy scale down
            countdownText.transform.localScale = Vector3.Lerp(startScale, endScale, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        countdownText.transform.localScale = Vector3.one;
    }

    private void Update()
    {
        // Prevent scrolling while game over or during countdown
        if (IsGameOver || IsPausedAtStart) return;

        scrollTimer += Time.deltaTime;
        if (scrollTimer >= config.scrollInterval)
        {
            scrollTimer = 0f;
            CurrentScrollRow++;

            gridManager.transform.position = new Vector3(0, -CurrentScrollRow, 0);
            player.UpdateWorldPosition();

            if (player.gridPos.y < CurrentScrollRow)
            {
                TriggerGameOver(false);
            }
        }
    }

    public void OnPlayerMoved(Vector2Int pos)
    {
        if (IsGameOver) return;

        if (pos.y > highestRow)
        {
            score += (pos.y - highestRow) * 10;
            highestRow = pos.y;
            UpdateUI();
        }

        if (gridManager.visualCells[pos.x, pos.y] != null)
        {
            gridManager.visualCells[pos.x, pos.y].Reveal();
        }

        TileType steppedType = gridManager.grid[pos.x, pos.y];

        switch (steppedType)
        {
            case TileType.FinishLine:
                score += 1000;
                SpawnFloatingText(player.transform.position, "+1000", Color.magenta);
                UpdateUI();
                AdvanceToNextLevel();
                break;

            case TileType.BonusPoint:
                StartCoroutine(DelayedBonusRoutine(pos, 150));
                break;

            case TileType.JumpPad:
                StartCoroutine(DelayedJumpPadRoutine(pos, 3));
                break;

            case TileType.Bomb:
                StartCoroutine(DelayedBombRoutine(pos));
                break;
        }
    }

    private IEnumerator DelayedBonusRoutine(Vector2Int pos, int points)
    {
        Vector3 spawnPos = player.transform.position + new Vector3(0, 0.75f, 0);
        SpawnFloatingText(spawnPos, $"+{points}", new Color(1f, 0.85f, 0.1f));

        score += points;
        UpdateUI();

        yield return new WaitForSeconds(0.3f);

        GridCell cell = gridManager.visualCells[pos.x, pos.y];
        if (cell != null)
        {
            cell.FadeOutAndConsume(gridManager, pos.x, pos.y, 0.7f);
        }
        else
        {
            gridManager.ClearTile(pos.x, pos.y);
        }
    }

    private IEnumerator DelayedJumpPadRoutine(Vector2Int pos, int requestedTilesUp)
    {
        yield return new WaitForSeconds(0.2f);

        int tilesLeaped = player.ApplyJumpPad(requestedTilesUp);
        int earnedPoints = tilesLeaped * 25;
        score += earnedPoints;
        UpdateUI();

        Vector3 spawnPos = player.transform.position + new Vector3(0, 0.75f, 0);
        SpawnFloatingText(spawnPos, $"+{earnedPoints}", new Color(0.1f, 1f, 0.5f));

        GridCell cell = gridManager.visualCells[pos.x, pos.y];
        if (cell != null)
        {
            cell.FadeOutAndConsume(gridManager, pos.x, pos.y, 0.8f);
        }
        else
        {
            gridManager.ClearTile(pos.x, pos.y);
        }
    }

    private IEnumerator DelayedBombRoutine(Vector2Int pos)
    {
        yield return new WaitForSeconds(1.0f);
        gridManager.ClearTile(pos.x, pos.y);
        TriggerBomb(pos, 0.5f);
    }

    public void TriggerBomb(Vector2Int origin, float fuseDuration)
    {
        if (activeBombs.Contains(origin)) return;
        activeBombs.Add(origin);
        StartCoroutine(BombSequenceRoutine(origin, fuseDuration));
    }

    private IEnumerator BombSequenceRoutine(Vector2Int origin, float fuseDuration)
    {
        List<GridCell> blastCells = new List<GridCell>();

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                int bx = origin.x + dx;
                int by = origin.y + dy;

                if (bx >= 0 && bx < GridManager.WIDTH && by >= 0 && by < gridManager.totalHeight)
                {
                    GridCell cell = gridManager.visualCells[bx, by];
                    if (cell != null)
                    {
                        cell.SetDangerWarning(true);
                        blastCells.Add(cell);
                    }
                }
            }
        }

        yield return new WaitForSeconds(fuseDuration);

        foreach (var cell in blastCells)
        {
            if (cell != null) cell.PlayExplosionVisual();
        }

        List<Vector2Int> chainedBombs = new List<Vector2Int>();
        int wallsDestroyed = 0;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                int bx = origin.x + dx;
                int by = origin.y + dy;

                if (bx >= 0 && bx < GridManager.WIDTH && by >= 0 && by < gridManager.totalHeight)
                {
                    if (gridManager.grid[bx, by] == TileType.Wall)
                    {
                        gridManager.ClearTile(bx, by);
                        wallsDestroyed++;

                        float screenY = by - CurrentScrollRow;
                        Vector3 wallWorldPos = new Vector3(bx, screenY + 0.4f, -2f);
                        SpawnFloatingText(wallWorldPos, "+25", new Color(1f, 0.65f, 0.1f));
                    }

                    if (gridManager.grid[bx, by] == TileType.Bomb)
                    {
                        gridManager.ClearTile(bx, by);
                        chainedBombs.Add(new Vector2Int(bx, by));
                    }

                    if (player.gridPos.x == bx && player.gridPos.y == by)
                    {
                        player.ApplyStun(1.5f);
                    }
                }
            }
        }

        if (wallsDestroyed > 0)
        {
            score += wallsDestroyed * 25;
            UpdateUI();
        }

        activeBombs.Remove(origin);

        foreach (var chainedBomb in chainedBombs)
        {
            TriggerBomb(chainedBomb, 0.25f);
        }
    }

    private void SpawnFloatingText(Vector3 worldPos, string message, Color color)
    {
        if (floatingScorePrefab == null) return;

        GameObject go = Instantiate(floatingScorePrefab, worldPos, Quaternion.identity);
        FloatingText ft = go.GetComponent<FloatingText>();
        if (ft != null)
        {
            ft.Setup(message, color);
        }
    }

    private void UpdateUI()
    {
        if (scoreText != null) scoreText.text = $"SCORE: {score:N0}";
        if (levelText != null) levelText.text = $"LEVEL: {currentLevel}";
    }

    private void AdvanceToNextLevel()
    {
        StartCoroutine(TransitionToNextLevelRoutine());
    }

    private IEnumerator TransitionToNextLevelRoutine()
    {
        currentLevel++;
        Debug.Log($"<color=green>Advancing to Level {currentLevel}!</color>");

        config.scrollInterval = Mathf.Max(0.5f, config.scrollInterval - 0.1f);
        config.obstacleDensity = Mathf.Min(0.38f, config.obstacleDensity + 0.02f);

        gridManager.ClearAllVisualCells();

        yield return new WaitForEndOfFrame();

        StartLevel();
    }

    private void TriggerGameOver(bool won)
    {
        IsGameOver = true;
        if (won)
            Debug.Log($"<color=green>WIN! Final Score: {score}</color>");
        else
            Debug.Log($"<color=red>GAME OVER! Final Score: {score}</color>");
    }
}