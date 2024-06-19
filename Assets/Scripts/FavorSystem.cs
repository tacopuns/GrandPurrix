using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FavorSystem : MonoBehaviour
{
    private PlayerData playerData;
    private SavePlayerData savePlayerData;
    private Paparazzi currentPaparazzi;

    public void Setup(PlayerData data)
    {
        playerData = data;
        savePlayerData = FindObjectOfType<SavePlayerData>();
    }

    public void SetCurrentPaparazzi(string paparazziName)
    {
        currentPaparazzi = playerData.paparazziList.Find(p => p.name == paparazziName);
        if (currentPaparazzi == null)
        {
            currentPaparazzi = new Paparazzi(paparazziName);
            playerData.paparazziList.Add(currentPaparazzi);
        }
    }

    public void AnswerA()
    {
        if (currentPaparazzi != null)
        {
            currentPaparazzi.favorPoints += 1;
            Debug.Log("FP for " + currentPaparazzi.name + " = " + currentPaparazzi.favorPoints);
            SaveFavorPoints();
        }
    }

    public void AnswerB()
    {
        if (currentPaparazzi != null)
        {
            currentPaparazzi.favorPoints -= 1;
            Debug.Log("FP for " + currentPaparazzi.name + " = " + currentPaparazzi.favorPoints);
            SaveFavorPoints();
        }
    }

    private void SaveFavorPoints()
    {
        if (savePlayerData != null)
        {
            savePlayerData.SavePlayerDataToFile();
        }
    }
}


