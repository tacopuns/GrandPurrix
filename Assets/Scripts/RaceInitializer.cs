using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaceInitializer : MonoBehaviour
{
    public List<Transform> startingPositions;
    private Dictionary<GameObject, int> previousRaceResults;
    private RaceManager raceManager;

    public SavePlayerData savePlayerData;

    void Awake()
    {
        //previousRaceResults = new Dictionary<GameObject, int>();
        raceManager = FindObjectOfType<RaceManager>(); // Find the RaceManager in the scene
        //ResetRaceData(); // Initialize with default positions

        //InitializeStartingPositions();

        savePlayerData = FindObjectOfType<SavePlayerData>();

        
    }

    public void InitializeStartingPositions()
    {
        
        
        // Get the list of racers from RaceManager
        List<GameObject> racers = raceManager.racers;
        
        savePlayerData.LoadRacerPositions(raceManager.racers);

        // Sort racers based on previous race results
        //racers.Sort(CompareRacersByPreviousResult);

        // Sort racers based on previous race positions
        raceManager.racers.Sort((r1, r2) => 
        {
            RacerComponent data1 = r1.GetComponent<RacerComponent>();
            RacerComponent data2 = r2.GetComponent<RacerComponent>();
            return data1.previousRacePosition.CompareTo(data2.previousRacePosition);
        });

        // Assign racers to starting positions
        /*for (int i = 0; i < racers.Count; i++)
        {
            racers[i].transform.position = startingPositions[i].position;
        }*/

        for (int i = 0; i < racers.Count; i++)
        {
            Transform startPos = startingPositions[i];
            GameObject racer = racers[i];

            racer.transform.position = startPos.position;
            racer.transform.rotation = startPos.rotation;

            // Debug log to show which racer is assigned to which position
            Debug.Log($"Racer {racer.name} is assigned to position {i}");
        }

        
    }

    public void SaveRaceResults(List<GameObject> raceResults)
    {
        /*for (int i = 0; i < raceResults.Count; i++)
        {
            if (previousRaceResults.ContainsKey(raceResults[i]))
            {
                previousRaceResults[raceResults[i]] = i + 1;
            }
            else
            {
                previousRaceResults.Add(raceResults[i], i + 1);
            }
        }*/

        List<GameObject> racers = raceManager.racers;

        // Sort racers by current race position (first, second, etc.)
        racers.Sort(raceManager.CompareRacers);

        for (int i = 0; i < racers.Count; i++)
        {
            GameObject racer = racers[i];
            RacerComponent data = racer.GetComponent<RacerComponent>();

            if (data != null)
            {
                data.previousRacePosition = i; //+ 1; // Save the finishing position
                // Debug log to show which position was saved for which racer
                Debug.Log($"Racer {racer.name} finished in position {i}");
            }
        }

        savePlayerData.SaveRacerPositions(raceManager.racers);
    }

    public void ResetRaceData()
    {
        /*previousRaceResults.Clear();

        // Get the list of racers from RaceManager
        List<GameObject> racers = raceManager.racers;

        // Set default positions, e.g., player last
        for (int i = 0; i < racers.Count; i++)
        {
            if (racers[i].tag == "Player")
            {
                previousRaceResults[racers[i]] = racers.Count; // Player starts last
            }
            else
            {
                previousRaceResults[racers[i]] = i; // AI racers get the first positions
            }
        }*/

        List<GameObject> racers = raceManager.racers;

         foreach (GameObject racer in raceManager.racers)
        {
            RacerComponent data = racer.GetComponent<RacerComponent>();
            if (data != null)
            {
                data.previousRacePosition = data.defaultRacePosition;

                
            }
        }
        
        savePlayerData.SaveRacerPositions(racers);
    }

    private int CompareRacersByPreviousResult(GameObject racer1, GameObject racer2)
    {
        /*int result1 = previousRaceResults.ContainsKey(racer1) ? previousRaceResults[racer1] : raceManager.racers.Count;
        int result2 = previousRaceResults.ContainsKey(racer2) ? previousRaceResults[racer2] : raceManager.racers.Count;
        return result1.CompareTo(result2);*/

        // Assume that each racer has a unique identifier and race result data stored
        RacerComponent data1 = racer1.GetComponent<RacerComponent>();
        RacerComponent data2 = racer2.GetComponent<RacerComponent>();

        if (data1 != null && data2 != null)
        {
            return data1.previousRacePosition.CompareTo(data2.previousRacePosition);
        }

        return 0; // If no data is found, consider them equal
    }
}
