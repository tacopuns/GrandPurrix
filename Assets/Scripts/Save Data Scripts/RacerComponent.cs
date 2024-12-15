using UnityEngine;

public class RacerComponent : MonoBehaviour
{
    public string racerName;
    public int previousRacePosition = 0;
    public int defaultRacePosition = 0;

    public Sprite racerSprite;
    public Sprite racerBanner;



    public RacerData GetRacerData()
    {
        return new RacerData
        {
            racerName = this.racerName,
            previousRacePosition = this.previousRacePosition,
            defaultRacePosition = this.defaultRacePosition,
            racerSprite = this.racerSprite,
            racerBanner = this.racerBanner
        };
    }
}
