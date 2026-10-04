using UnityEngine;

[CreateAssetMenu(fileName = "DifficultyConfig", menuName = "GridGame/DifficultyConfig")]
public class LevelDifficultyConfig : ScriptableObject
{
    [Header("Game Flow")]
    public float scrollInterval = 1.2f;     // Seconds per row shift down
    public int finishLineDistance = 50;     // Total rows to finish line
    public float inputCooldown = 0.5f;      

    [Header("Obstacles & Pickups")]
    [Range(0.1f, 0.45f)]
    public float obstacleDensity = 0.4f;
    [Range(0.05f, 0.2f)]
    public float specialTileDensity = 0.15f;

    [Header("Sonar Ability")]
    public float sonarCooldown = 5.0f;
    public int sonarRadius = 5;
    public float sonarRevealDuration = 1.0f;
}