using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PersistenceManager : MonoBehaviour
{
    public static PersistenceManager Instance;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);

        //SavePlayerData.Instance.LoadGame();
    }


    public void SaveGame()
    {
        SavePlayerData.Instance.SaveGame();
    }

    public void LoadGame()
    {
        SavePlayerData.Instance.LoadGame();
    }

    public void SaveRacerPositions(List<GameObject> racers)
    {
        SavePlayerData.Instance.SaveRacerPositions(racers);
    }

    public void LoadRacerPositions(List<GameObject> racers)
    {
        SavePlayerData.Instance.LoadRacerPositions(racers);
    }

    //call methods from here when switching scenes instead of calling them directly
}
