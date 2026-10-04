using System.Collections;
using UnityEngine;

public class GridCell : MonoBehaviour
{
    [Header("Visual Components")]
    public SpriteRenderer cellBackground;
    public SpriteRenderer iconRenderer;

    [Header("Icon Scaling")]
    [Range(0.2f, 0.95f)]
    public float iconScaleRatio = 0.72f;

    public TileType assignedType = TileType.Empty;
    public bool isRevealed = false;

    // Palette Colors
    private readonly Color emptyTileColor = new Color(0.12f, 0.12f, 0.15f, 1f);
    private readonly Color iceWallColor   = new Color(0.0f, 0.85f, 1.0f, 1f);
    private readonly Color riverColor     = new Color(0.05f, 0.35f, 0.85f, 1f);
    private readonly Color bombColor      = new Color(0.95f, 0.1f, 0.15f, 1f);
    private readonly Color jumpPadColor   = new Color(0.0f, 0.95f, 0.45f, 1f);
    private readonly Color bonusColor     = new Color(1.0f, 0.78f, 0.05f, 1f);
    private readonly Color finishColor    = new Color(0.85f, 0.05f, 0.95f, 1f);

    private Color originalBgColor;
    private TileSpriteData spriteData;
    private Coroutine dangerCoroutine;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (cellBackground == null) cellBackground = GetComponent<SpriteRenderer>();
        originalBgColor = emptyTileColor;
    }

    public void Setup(TileType type, TileSpriteData data)
    {
        // Stop any running fades or alerts from previous state
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (dangerCoroutine != null)
        {
            StopCoroutine(dangerCoroutine);
            dangerCoroutine = null;
        }

        transform.localScale = Vector3.one;

        // Reset icon alpha and visibility
        if (iconRenderer != null)
        {
            Color ic = iconRenderer.color;
            ic.a = 1f;
            iconRenderer.color = ic;
            iconRenderer.enabled = false;
        }

        // Reset background
        if (cellBackground != null)
        {
            cellBackground.color = emptyTileColor;
        }

        assignedType = type;
        spriteData = data;
        isRevealed = false;

        UpdateVisual();
    }

    public void Reveal()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
        isRevealed = true;
        UpdateVisual();
    }

    public void FadeOutToHidden(float fadeDuration = 1.0f)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeRoutine(fadeDuration, false));
    }

    public void FadeOutAndConsume(GridManager gm, int x, int y, float fadeDuration = 0.8f)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeRoutine(fadeDuration, true, gm, x, y));
    }

    private IEnumerator FadeRoutine(float duration, bool clearTileAfter, GridManager gm = null, int x = 0, int y = 0)
    {
        float elapsed = 0f;
        Color initialBg = cellBackground != null ? cellBackground.color : emptyTileColor;
        Color initialIcon = iconRenderer != null ? iconRenderer.color : Color.white;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (cellBackground != null)
            {
                cellBackground.color = Color.Lerp(initialBg, emptyTileColor, t);
            }

            if (iconRenderer != null)
            {
                Color c = initialIcon;
                c.a = Mathf.Lerp(1f, 0f, t);
                iconRenderer.color = c;
            }

            yield return null;
        }

        isRevealed = false;
        if (clearTileAfter && gm != null)
        {
            gm.ClearTile(x, y);
        }
        else
        {
            UpdateVisual();
        }

        fadeCoroutine = null;
    }

    public void SetDangerWarning(bool active)
    {
        if (active)
        {
            if (dangerCoroutine == null) dangerCoroutine = StartCoroutine(DangerFlashRoutine());
        }
        else
        {
            if (dangerCoroutine != null)
            {
                StopCoroutine(dangerCoroutine);
                dangerCoroutine = null;
            }
            UpdateVisual();
        }
    }

    private IEnumerator DangerFlashRoutine()
    {
        Color flashYellow = new Color(1f, 0.9f, 0.2f, 1f);
        while (true)
        {
            if (cellBackground != null) cellBackground.color = bombColor;
            yield return new WaitForSeconds(0.1f);
            if (cellBackground != null) cellBackground.color = flashYellow;
            yield return new WaitForSeconds(0.1f);
        }
    }

    public void PlayExplosionVisual()
    {
        StartCoroutine(ExplosionAnimationRoutine());
    }

    private IEnumerator ExplosionAnimationRoutine()
    {
        SetDangerWarning(false);
        if (cellBackground != null) cellBackground.color = new Color(1f, 0.5f, 0.0f, 1f);

        Vector3 originalScale = transform.localScale;
        Vector3 expandedScale = originalScale * 1.4f;

        float elapsed = 0f;
        float duration = 0.25f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(expandedScale, originalScale, t);
            yield return null;
        }

        transform.localScale = originalScale;
        UpdateVisual();
    }

    public void UpdateVisual()
    {
        if (iconRenderer == null) return;

        Color ic = iconRenderer.color;
        ic.a = 1f;
        iconRenderer.color = ic;

        bool isSpecialHidden = (assignedType == TileType.Bomb || 
                                assignedType == TileType.JumpPad || 
                                assignedType == TileType.BonusPoint);

        if ((isSpecialHidden && !isRevealed) || assignedType == TileType.Empty)
        {
            iconRenderer.enabled = false;
            if (cellBackground != null) cellBackground.color = emptyTileColor;
            return;
        }

        iconRenderer.enabled = true;
        Sprite targetSprite = null;
        Color bgColor = emptyTileColor;

        switch (assignedType)
        {
            case TileType.Wall:
                targetSprite = spriteData != null ? spriteData.wallSprite : null;
                bgColor = iceWallColor;
                break;
            case TileType.River:
                targetSprite = spriteData != null ? spriteData.riverSprite : null;
                bgColor = riverColor;
                break;
            case TileType.Bomb:
                targetSprite = spriteData != null ? spriteData.bombSprite : null;
                bgColor = bombColor;
                break;
            case TileType.JumpPad:
                targetSprite = spriteData != null ? spriteData.jumpPadSprite : null;
                bgColor = jumpPadColor;
                break;
            case TileType.BonusPoint:
                targetSprite = spriteData != null ? spriteData.bonusSprite : null;
                bgColor = bonusColor;
                break;
            case TileType.FinishLine:
                targetSprite = spriteData != null ? spriteData.finishLineSprite : null;
                bgColor = finishColor;
                break;
        }

        if (cellBackground != null) cellBackground.color = bgColor;

        if (targetSprite != null)
        {
            iconRenderer.sprite = targetSprite;
            FitSpriteToCell(iconRenderer, targetSprite);
        }
    }

    private void FitSpriteToCell(SpriteRenderer sr, Sprite sprite)
    {
        if (sprite == null) return;
        Vector2 spriteSize = sprite.bounds.size;
        if (spriteSize.x == 0 || spriteSize.y == 0) return;

        float maxDimension = Mathf.Max(spriteSize.x, spriteSize.y);
        float targetScale = iconScaleRatio / maxDimension;
        sr.transform.localScale = new Vector3(targetScale, targetScale, 1f);
    }
}