using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class SavePlayerData : MonoBehaviour
{
    public static SavePlayerData Instance;
    
    public PlayerData playerData;
    string saveFilePath;
    public FavorSystem favorSystem;


    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);


        saveFilePath = Application.persistentDataPath + "/PlayerData.json";

        favorSystem = FindObjectOfType<FavorSystem>();
        if (favorSystem != null)
        {
            favorSystem.Setup(playerData);
        }
    }

    void Start()
    {
        //InitializePlayerData();
        //LoadGame();

        Debug.Log(Application.persistentDataPath);
    }

    private void InitializePlayerData()
    {
        playerData = new PlayerData
        {
            level = 0,
            rating = 0,
            paparazziList = new List<Paparazzi>(),
            racerPositions = new List<RacerData>()
        };
    }

    public void SaveGame()
    {
        string saveGameData = JsonUtility.ToJson(playerData);
        File.WriteAllText(saveFilePath, saveGameData);
        Debug.Log("Save file created at: " + saveFilePath);
    }

    public void LoadGame()
    {
        if (File.Exists(saveFilePath))
        {
            string loadGameData = File.ReadAllText(saveFilePath);
            playerData = JsonUtility.FromJson<PlayerData>(loadGameData);
            Debug.Log("Load game complete!");

            if (favorSystem != null)
            {
                favorSystem.Setup(playerData);
            }

            if (playerData.racerPositions == null)
            {
                playerData.racerPositions = new List<RacerData>();
            }
        }
        else
        {
            Debug.Log("There is no save file to load!");
            //InitializePlayerData();
        }
    }

    public void DeleteSaveFile()
    {
        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
            Debug.Log("Save file DELETED!");

            
            InitializePlayerData();

            
            //SaveGame();
        }
        else
        {
            Debug.Log("There is nothing to delete!");
        }
    }

    public void SavePlayerDataToFile()
    {
        SaveGame();
    }

    public void SaveRacerPositions(List<GameObject> racers)
    {
        playerData.racerPositions.Clear();

        foreach (GameObject racer in racers)
        {
            RacerComponent data = racer.GetComponent<RacerComponent>();
            if (data != null)
            {
                RacerData racerData = new RacerData
                {
                    racerName = data.racerName,
                    previousRacePosition = data.previousRacePosition,
                    defaultRacePosition = data.defaultRacePosition
                };
                playerData.racerPositions.Add(racerData);
            }
        }
        SaveGame();
    }

    public void LoadRacerPositions(List<GameObject> racers)
    {
        Debug.Log("Loading racer positions...");

        if (playerData == null)
        {
            Debug.LogError("LoadedPlayerData is null!");
            return;
        }

        if (playerData.racerPositions == null)
        {
            Debug.LogError("loadedPlayerData.racerPositions is null!");
            return;
        }

        foreach (GameObject racer in racers)
        {
            RacerComponent data = racer.GetComponent<RacerComponent>();
            if (data != null)
            {
                //Debug.Log($"Attempting to load position for racer: {data.racerName}");
                RacerData savedData = playerData.racerPositions.Find(p => p.racerName == data.racerName);
                if (savedData != null)
                {
                    data.previousRacePosition = savedData.previousRacePosition;
                    data.defaultRacePosition = savedData.defaultRacePosition;
                    //Debug.Log($"Loaded position: {data.previousRacePosition} for racer: {data.racerName}");
                }
                else
                {
                    data.previousRacePosition = data.defaultRacePosition;
                    //Debug.Log($"No saved position found for racer: {data.racerName}. Using default position: {data.defaultRacePosition}");
                }
            }
            else
            {
                Debug.LogWarning($"No RacerComponent found on {racer.name}");
            }
        }
    }
    


}

