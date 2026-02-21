using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class FavorSystem : MonoBehaviour
{
    private PlayerData playerData;
    //private SavePlayerData savePlayerData;
    private Paparazzi currentPaparazzi;

    public void Setup(PlayerData data)
    {
        playerData = data;
        //savePlayerData = FindObjectOfType<SavePlayerData>();
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

    private void SaveFavorPoints()
    {
        PersistenceManager.Instance.SaveGame();
    }


    public void ApplyAnswerValue(int value)
    {
        if (currentPaparazzi == null)
        {
            Debug.LogWarning("No current paparazzi set!");
            return;
        }

        currentPaparazzi.favorPoints += value;

        Debug.Log($"Favor for {currentPaparazzi.name} is now {currentPaparazzi.favorPoints}");

        SaveFavorPoints();
    }

    public void ApplyAnswer(AnswerOption answer)
    {
        currentPaparazzi.favorPoints += answer.value;
        Debug.Log($"Favor for {currentPaparazzi.name} is now {currentPaparazzi.favorPoints}");

        SaveFavorPoints();
    
    }


}


