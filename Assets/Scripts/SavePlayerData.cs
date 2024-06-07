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
        playerData = new PlayerData(); 
        
        saveFilePath = Application.persistentDataPath + "/PlayerData.json";
        
        favorSystem.Initialize(playerData);
    }
    
    void Start()
    {
        
        playerData.level = 0;
        playerData.rating = 0;
        playerData.favor = 5;

        
        Debug.Log(Application.persistentDataPath);
    }

    public void SaveGame()
    {
        string saveGameData = JsonUtility.ToJson(playerData);
        File.WriteAllText(saveFilePath, saveGameData);

        Debug.Log("Save file created at: " + saveFilePath);
    }

    public void LoadGame()
    {
        if(File.Exists(saveFilePath))
        {
            string loadGameData = File.ReadAllText(saveFilePath);
            playerData = JsonUtility.FromJson<PlayerData>(loadGameData);

            Debug.Log("Load game complete!");

            favorSystem.Initialize(playerData);
        }
        else
        {
            Debug.Log("There is no save file to load!");
        }
    }

    public void DeleteSaveFile()
    {
        if(File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);

            Debug.Log("Save file DELETED!"); 
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
