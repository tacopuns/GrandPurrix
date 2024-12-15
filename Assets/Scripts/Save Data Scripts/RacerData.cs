using UnityEngine;
[System.Serializable]
public class RacerData
{
    public string racerName;
    public int previousRacePosition = 0; // Position from the previous race
    public int defaultRacePosition = 0;  // Default position for the racer

    public Sprite racerSprite; // Profile icon
    public Sprite racerBanner; // Top Segment banner
}
