using UnityEngine;

[CreateAssetMenu(fileName = "TileSpriteData", menuName = "GridGame/TileSpriteData")]
public class TileSpriteData : ScriptableObject
{
    [Header("Persistent Sprites")]
    public Sprite wallSprite;
    public Sprite riverSprite;
    public Sprite bombSprite;
    public Sprite jumpPadSprite;
    public Sprite bonusSprite;
    public Sprite finishLineSprite;
}