using System.Collections.Generic;
[System.Serializable]
public class PlayerData
{
    public int level;
    public int rating;
    public List<Paparazzi> paparazziList;
    public List<RacerData> racerPositions;

    public PlayerData()
    {
        paparazziList = new List<Paparazzi>();
        racerPositions = new List<RacerData>();
    }
}
