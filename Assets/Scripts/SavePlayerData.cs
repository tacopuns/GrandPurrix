using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class SavePlayerData : MonoBehaviour
{
    public PlayerData playerData;
    string saveFilePath;
    public FavorSystem favorSystem;

    void Awake()
    {
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
        LoadGame();

        Debug.Log(Application.persistentDataPath);
    }

    private void InitializePlayerData()
    {
        playerData = new PlayerData
        {
            level = 0,
            rating = 0
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

            // Re-initialize player data after deletion
            InitializePlayerData();

            // Optionally save the reset state immediately
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
}

