using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Standings : MonoBehaviour
{
    public GameObject resultsPanel;
    public GameObject racerStandingPrefab;

    public void PopulateResults(List<RacerData> racerDataList)
    {

        foreach (Transform child in resultsPanel.transform)
        {
            Destroy(child.gameObject);
        }

        
        racerDataList.Sort((r1, r2) => r1.previousRacePosition.CompareTo(r2.previousRacePosition));

        
        for (int i = 0; i < racerDataList.Count; i++)
        {
            RacerData racerData = racerDataList[i];
            GameObject racerStanding = Instantiate(racerStandingPrefab, resultsPanel.transform);

            if (racerStanding == null)
        {
            Debug.LogError("Failed to instantiate racerStandingPrefab.");
            continue;
        }

            TextMeshProUGUI racerPosition = racerStanding.transform.Find("RacerPosition").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI racerName = racerStanding.transform.Find("RacerName").GetComponent<TextMeshProUGUI>();
            //Image racerSprite = racerStanding.transform.Find("RacerSprite").GetComponent<Image>();

             if (racerPosition == null || racerName == null)
            {
                Debug.LogError("One or more UI elements are missing from the racerStandingPrefab.");
                continue;
            }

            racerPosition.text = $"{i + 1}";
            racerName.text = racerData.racerName;
            // racerImage.sprite = GetRacerImage(racerData.racerName); // Assuming you have a method to get the racer's image

            
        }
    }

    private Sprite GetRacerImage(string racerName)
    {
        // Implement this method to return the appropriate image for the racer
        // For example, you might have a dictionary mapping racer names to sprites
        return null;
    }
}
