using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Standings : MonoBehaviour
{
    public GameObject resultsPanel;
    public GameObject racerStandingPrefab;

    public GameObject topThreePanel;
    public GameObject bannerPrefab;

    

    public void PopulateResults(List<RacerData> racerDataList)
    {

        List<GameObject> racers = FindObjectOfType<RaceManager>().racers;

        // Clear the list
        racerDataList.Clear();

        foreach (GameObject racer in racers)
        {
            RacerComponent racerComponent = racer.GetComponent<RacerComponent>();
            if (racerComponent != null)
            {
                
                racerDataList.Add(racerComponent.GetRacerData());
            }
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
            Image racerSprite = racerStanding.transform.Find("RacerSprite").GetComponent<Image>();

            if (racerPosition == null || racerName == null || racerSprite == null)
            {
                Debug.LogError("One or more UI elements are missing from the racerStandingPrefab.");
                continue;
            }

            racerPosition.text = $"{i + 1}";
            racerName.text = racerData.racerName;

            
            if (racerData.racerSprite != null)
            {
                racerSprite.sprite = racerData.racerSprite;
            }
            else
            {
                Debug.LogError($"Sprite for {racerData.racerName} is null.");
            }
        }
    }

    public void PopulateTopThreeBanners(List<RacerData> racerDataList)
    {
        
        foreach (Transform child in topThreePanel.transform)
        {
            Destroy(child.gameObject);
        }

    
        for (int i = 0; i < Mathf.Min(3, racerDataList.Count); i++)
        {
            RacerData racerData = racerDataList[i];
            GameObject banner = Instantiate(bannerPrefab, topThreePanel.transform);

            Image bannerImage = banner.GetComponent<Image>();
            if (bannerImage != null)
            {
                if (racerData.racerBanner != null)
                {
                    bannerImage.sprite = racerData.racerBanner; 
                }
                else
                {
                    Debug.LogWarning($"Banner sprite for {racerData.racerName} is null. Assigning a default sprite.");
                }
            }
            else
            {
                Debug.LogError("Banner prefab does not have an Image component.");
            }
        }
    }
}
