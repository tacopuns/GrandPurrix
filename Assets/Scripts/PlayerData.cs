using System.Collections.Generic;
[System.Serializable]
public class PlayerData
{
    public int level;
    public int rating;
    public List<Paparazzi> paparazziList;

    public PlayerData()
    {
        paparazziList = new List<Paparazzi>();
    }
}
