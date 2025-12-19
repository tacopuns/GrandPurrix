using System.Collections.Generic;
using UnityEngine;

public class RaceManager : MonoBehaviour
{
    public static RaceManager Instance;
    private CheckpointManager checkpointManager;
    public List<GameObject> racers;

    public RaceInitializer raceInitializer;
    //public GameManager gameManager;

    private void Start()
    {
        checkpointManager = CheckpointManager.Instance;
        //raceInitializer.InitializeStartingPositions();
    }

    private void Update()
    {
        UpdateRacerPositions();
    }

    private void UpdateRacerPositions()
    {
        racers.Sort(CompareRacers);
    }

    public int CompareRacers(GameObject racer1, GameObject racer2)
    {
        // Calculate virtual checkpoint indices for racers
        int virtualCheckpointIndex1 = CalculateVirtualCheckpointIndex(racer1);
        int virtualCheckpointIndex2 = CalculateVirtualCheckpointIndex(racer2);

        // Compare virtual checkpoint indices
        int checkpointComparison = virtualCheckpointIndex1.CompareTo(virtualCheckpointIndex2);

        // If the virtual checkpoint indices are equal, compare the distances to the next checkpoints
        if (checkpointComparison == 0)
        {
            float distanceToNextCheckpoint1 = checkpointManager.DistanceToNextCheckpoint(racer1);
            float distanceToNextCheckpoint2 = checkpointManager.DistanceToNextCheckpoint(racer2);
            
            // Compare distances to the next checkpoints
            int distanceComparison = distanceToNextCheckpoint1.CompareTo(distanceToNextCheckpoint2);
            
            
            // If the distances are equal, prioritize the racer with the higher lap count
            if (distanceComparison == 0)
            {
                int lapComparison = checkpointManager.GetLapCount(racer1).CompareTo(checkpointManager.GetLapCount(racer2));
                return -lapComparison; // Higher lap count means higher position
            }
            
            return distanceComparison; // Smaller distance means higher position
        }

        // Return the result of the comparison based on virtual checkpoint indices
        return -checkpointComparison; // Higher virtual checkpoint index means higher position
    }

    private int CalculateVirtualCheckpointIndex(GameObject racer)
    {
        int lapCount = checkpointManager.GetLapCount(racer);
        int checkpointIndex = checkpointManager.GetLastPassedCheckpointIndex(racer);
        int totalCheckpoints = checkpointManager.checkpoints.Count;

        // Calculate virtual checkpoint index based on lap count and physical checkpoint index
        // Adjust for circular nature of checkpoints
        int virtualCheckpointIndex = lapCount * totalCheckpoints + checkpointIndex;

        return virtualCheckpointIndex;
    }

    public void FinishRace()
    {
        // Assuming the racers list is sorted by their final positions
        raceInitializer.SaveRaceResults(racers);
    }
}
