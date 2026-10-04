using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SonarSystem : MonoBehaviour
{
    [Header("Dependencies")]
    public PlayerController player;
    public GridManager gridManager;
    public LevelDifficultyConfig config;

    [Header("UI & Visual Prefabs")]
    public Slider sonarCooldownSlider;
    public GameObject sonarRingPrefab;

    private float nextSonarTime = 0f;

    private void Start()
    {
        if (sonarCooldownSlider != null) sonarCooldownSlider.value = 1f;
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (sonarCooldownSlider != null)
        {
            if (Time.time >= nextSonarTime)
            {
                sonarCooldownSlider.value = 1f;
            }
            else
            {
                float timeRemaining = nextSonarTime - Time.time;
                float progress = 1f - (timeRemaining / config.sonarCooldown);
                sonarCooldownSlider.value = Mathf.Clamp01(progress);
            }
        }

        if (kb.spaceKey.wasPressedThisFrame && Time.time >= nextSonarTime)
        {
            nextSonarTime = Time.time + config.sonarCooldown;
            StartCoroutine(PerformWaveSonar());
        }
    }

    private IEnumerator PerformWaveSonar()
    {
        Vector2Int center = player.gridPos;
        float maxRadius = config.sonarRadius;
        float waveDuration = 0.55f; // Fast, punchy expansion

        // Spawn visual expanding ring at player's current screen position
        if (sonarRingPrefab != null)
        {
            GameObject ring = Instantiate(sonarRingPrefab, player.transform.position, Quaternion.identity);
            SonarPulseRing pulse = ring.GetComponent<SonarPulseRing>();
            if (pulse != null)
            {
                pulse.AnimatePulse(maxRadius, waveDuration);
            }
        }

        List<GridCell> revealedTiles = new List<GridCell>();
        HashSet<Vector2Int> touched = new HashSet<Vector2Int>();

        float elapsed = 0f;
        while (elapsed < waveDuration)
        {
            elapsed += Time.deltaTime;
            float currentWaveRadius = (elapsed / waveDuration) * maxRadius;

            int intRadius = Mathf.CeilToInt(currentWaveRadius);

            for (int x = center.x - intRadius; x <= center.x + intRadius; x++)
            {
                for (int y = center.y - intRadius; y <= center.y + intRadius; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (touched.Contains(pos)) continue;

                    // Circular distance check
                    float dist = Vector2.Distance(center, pos);
                    if (dist <= currentWaveRadius)
                    {
                        touched.Add(pos);

                        if (x >= 0 && x < GridManager.WIDTH && y >= 0 && y < gridManager.totalHeight)
                        {
                            GridCell cell = gridManager.visualCells[x, y];
                            if (cell != null && !cell.isRevealed)
                            {
                                TileType t = gridManager.grid[x, y];
                                if (t == TileType.Bomb || t == TileType.JumpPad || t == TileType.BonusPoint)
                                {
                                    cell.Reveal();
                                    revealedTiles.Add(cell);
                                }
                            }
                        }
                    }
                }
            }

            yield return null;
        }

        // Keep illuminated for a moment after wave finishes
        yield return new WaitForSeconds(config.sonarRevealDuration);

        // Smoothly fade every sonar-revealed tile out of existence
        foreach (var cell in revealedTiles)
        {
            if (cell != null)
            {
                cell.FadeOutToHidden(0.75f);
            }
        }
    }
}