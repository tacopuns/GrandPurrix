using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaceInitializer : MonoBehaviour
{
    public List<Transform> startingPositions;
    private Dictionary<GameObject, int> previousRaceResults;
    private RaceManager raceManager;

    //public SavePlayerData savePlayerData;

    void Awake()
    {
        raceManager = FindObjectOfType<RaceManager>();  
    }

    public void InitializeStartingPositions()
    {
        
        List<GameObject> racers = raceManager.racers;

        PersistenceManager.Instance.LoadRacerPositions(racers);

       
        raceManager.racers.Sort((r1, r2) => 
        {
            RacerComponent data1 = r1.GetComponent<RacerComponent>();
            RacerComponent data2 = r2.GetComponent<RacerComponent>();
            return data1.previousRacePosition.CompareTo(data2.previousRacePosition);
        });


        for (int i = 0; i < racers.Count; i++)
        {
            Transform startPos = startingPositions[i];
            GameObject racer = racers[i];

            racer.transform.position = startPos.position;
            racer.transform.rotation = startPos.rotation;

            
            Debug.Log($"Racer {racer.name} is assigned to position {i}");
        }

        
    }

    public void SaveRaceResults(List<GameObject> raceResults)
    {

        List<GameObject> racers = raceManager.racers;

        
        racers.Sort(raceManager.CompareRacers);

        for (int i = 0; i < racers.Count; i++)
        {
            GameObject racer = racers[i];
            RacerComponent data = racer.GetComponent<RacerComponent>();

            if (data != null)
            {
                data.previousRacePosition = i; //+ 1;
                
                Debug.Log($"Racer {racer.name} finished in position {i}");
            }
        }


        PersistenceManager.Instance.SaveRacerPositions(racers);
    }

    public void ResetRaceData()
    {
        List<GameObject> racers = raceManager.racers;

         foreach (GameObject racer in raceManager.racers)
        {
            RacerComponent data = racer.GetComponent<RacerComponent>();
            if (data != null)
            {
                data.previousRacePosition = data.defaultRacePosition;

                
            }
        }
        
        //savePlayerData.SaveRacerPositions(racers);

        PersistenceManager.Instance.SaveRacerPositions(racers);
    }

    private int CompareRacersByPreviousResult(GameObject racer1, GameObject racer2)
    {        
        RacerComponent data1 = racer1.GetComponent<RacerComponent>();
        RacerComponent data2 = racer2.GetComponent<RacerComponent>();

        if (data1 != null && data2 != null)
        {
            return data1.previousRacePosition.CompareTo(data2.previousRacePosition);
        }

        return 0;
    }
}
