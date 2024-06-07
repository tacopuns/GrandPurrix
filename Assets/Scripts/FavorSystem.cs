using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FavorSystem : MonoBehaviour
{
    private int favorPoints = 0;

    private int startingValue = 5;

    public PlayerData playerData;
    private SavePlayerData savePlayerData;

    public void Initialize(PlayerData data)
    {
        playerData = data;
        favorPoints = playerData.favor;

        savePlayerData = FindObjectOfType<SavePlayerData>();
    }

    public void AnswerA()
    {
        favorPoints = startingValue + 1;
        Debug.Log("FP = " + favorPoints);
        playerData.favor = favorPoints;

        SaveFavorPoints();
    }

    public void AnswerB()
    {
        favorPoints = startingValue - 1;
        Debug.Log("FP = " + favorPoints);
        playerData.favor = favorPoints;

        SaveFavorPoints();
    }

    private void SaveFavorPoints()
    {
        savePlayerData.SavePlayerDataToFile();
        
    }
}
