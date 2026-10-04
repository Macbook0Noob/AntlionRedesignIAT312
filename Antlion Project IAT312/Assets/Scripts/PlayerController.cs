using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("State")]
    public Vector2Int gridPos = new Vector2Int(4, 1);
    public bool isStunned = false;
    public bool isMoving = false;
    public bool isJumping = false;

    [Header("Movement Speed Settings")]
    [Tooltip("Base time in seconds to slide across a normal grid tile.")]
    public float baseMoveDuration = 0.3f;

    [Tooltip("Multiplier applied to movement duration when wading through river/water tiles.")]
    public float waterSlowMultiplier = 2.5f;

    private GridManager gridManager;
    private GameManager gameManager;
    private LevelDifficultyConfig config;
    private SpriteRenderer spriteRenderer;
    private Color originalPlayerColor = Color.yellow;

    public void Initialize(GridManager gm, GameManager gmgr, LevelDifficultyConfig cfg)
    {
        gridManager = gm;
        gameManager = gmgr;
        config = cfg;
        isStunned = false;
        isMoving = false;
        isJumping = false;

        // Centered horizontally on row 1 (safe grace zone)
        gridPos = new Vector2Int(GridManager.WIDTH / 2, 1);

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalPlayerColor = spriteRenderer.color;
        }

        if (config != null)
        {
            baseMoveDuration = config.inputCooldown;
        }

        UpdateWorldPosition();
    }

    private void Update()
    {
        // Locked if stunned, sliding, jumping, game over, OR during start countdown
        if (isStunned || isMoving || isJumping || gameManager == null || gameManager.IsGameOver || gameManager.IsPausedAtStart) 
            return;

        var kb = Keyboard.current;
        if (kb == null) return;

        Vector2Int dir = Vector2Int.zero;

        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) dir = Vector2Int.up;
        else if (kb.sKey.isPressed || kb.downArrowKey.isPressed) dir = Vector2Int.down;
        else if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) dir = Vector2Int.left;
        else if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) dir = Vector2Int.right;

        if (dir != Vector2Int.zero)
        {
            TryMove(dir);
        }
    }

    private void TryMove(Vector2Int dir)
    {
        Vector2Int target = gridPos + dir;

        // 1. Horizontal bounds: 0 to WIDTH - 1
        if (target.x < 0 || target.x >= GridManager.WIDTH) return;

        // 2. Prevent walking below the bottom of the screen
        if (target.y < gameManager.CurrentScrollRow) return;

        // 3. Screen ceiling (row 9 relative to view)
        int targetScreenY = target.y - gameManager.CurrentScrollRow;
        if (targetScreenY > 9) return;

        // 4. Impassable obstacles (Walls)
        if (!gridManager.IsTraversable(target.x, target.y)) return;

        // Water terrain penalty calculation
        bool inWater = gridManager.IsWater(gridPos.x, gridPos.y) || gridManager.IsWater(target.x, target.y);
        float stepDuration = inWater ? (baseMoveDuration * waterSlowMultiplier) : baseMoveDuration;

        StartCoroutine(StepSlideRoutine(target, stepDuration, inWater));
    }

    private IEnumerator StepSlideRoutine(Vector2Int target, float duration, bool wadingInWater)
    {
        isMoving = true;
        Vector2Int startPos = gridPos;
        gridPos = target;

        // Water resistance visual tint
        if (spriteRenderer != null && wadingInWater)
        {
            spriteRenderer.color = Color.Lerp(originalPlayerColor, new Color(0.2f, 0.7f, 1f), 0.5f);
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float currentX = Mathf.Lerp(startPos.x, target.x, t);
            float currentGridY = Mathf.Lerp(startPos.y, target.y, t);

            float screenY = currentGridY - gameManager.CurrentScrollRow;
            transform.position = new Vector3(currentX, screenY, -1f);

            yield return null;
        }

        // Restore color when not in water
        if (spriteRenderer != null && !gridManager.IsWater(gridPos.x, gridPos.y))
        {
            spriteRenderer.color = originalPlayerColor;
        }

        isMoving = false;
        UpdateWorldPosition();
        gameManager.OnPlayerMoved(gridPos);
    }

    /// <summary>
    /// Jump pad launches player up, bypassing the screen ceiling.
    /// Returns the actual number of tiles leaped forward for scoring.
    /// </summary>
    public int ApplyJumpPad(int tilesUp)
    {
        StopAllCoroutines();
        isMoving = false;

        int startY = gridPos.y;
        int targetY = Mathf.Min(gridPos.y + tilesUp, gridManager.totalHeight - 1);

        while (!gridManager.IsTraversable(gridPos.x, targetY) && targetY < gridManager.totalHeight - 1)
        {
            targetY++;
        }

        int actualTilesTraveled = targetY - startY;

        StartCoroutine(JumpRoutine(targetY, 1.0f));

        return actualTilesTraveled;
    }

    private IEnumerator JumpRoutine(int destinationY, float duration)
    {
        isJumping = true;
        float startGridY = gridPos.y;
        float elapsed = 0f;

        Vector3 originalScale = transform.localScale;
        Vector3 peakScale = originalScale * 1.35f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            float currentGridY = Mathf.Lerp(startGridY, destinationY, smoothT);

            float screenY = currentGridY - gameManager.CurrentScrollRow;
            transform.position = new Vector3(gridPos.x, screenY, -1.5f);

            float jumpArc = Mathf.Sin(smoothT * Mathf.PI);
            transform.localScale = Vector3.Lerp(originalScale, peakScale, jumpArc);

            yield return null;
        }

        transform.localScale = originalScale;
        gridPos.y = destinationY;
        isJumping = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = gridManager.IsWater(gridPos.x, gridPos.y) 
                ? Color.Lerp(originalPlayerColor, new Color(0.2f, 0.7f, 1f), 0.5f) 
                : originalPlayerColor;
        }

        UpdateWorldPosition();
        gameManager.OnPlayerMoved(gridPos);
    }

    public void ApplyStun(float duration)
    {
        StartCoroutine(StunRoutine(duration));
    }

    private IEnumerator StunRoutine(float duration)
    {
        isStunned = true;
        if (spriteRenderer != null) spriteRenderer.color = Color.gray;

        yield return new WaitForSeconds(duration);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = gridManager.IsWater(gridPos.x, gridPos.y) 
                ? Color.Lerp(originalPlayerColor, new Color(0.2f, 0.7f, 1f), 0.5f) 
                : originalPlayerColor;
        }
        isStunned = false;
    }

    public void UpdateWorldPosition()
    {
        if (gameManager == null || isMoving || isJumping) return;
        float screenY = gridPos.y - gameManager.CurrentScrollRow;
        transform.position = new Vector3(gridPos.x, screenY, -1f);
    }
}