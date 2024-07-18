using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

public class RaceFinishUI : MonoBehaviour
{
    public TextMeshProUGUI qualifiedText;
    public TextMeshProUGUI gameoverText;

    public Button toResultsBtn;
    
    public RectTransform endPanel;

    private RaceCompletion raceCompletion;

    private RaceManager raceManager;
    public Standings standingsManager;


    void Awake()
    {
        raceManager = FindObjectOfType<RaceManager>();
    }
    
    void Start()
    {
        raceCompletion = FindObjectOfType<RaceCompletion>();
        qualifiedText.gameObject.SetActive(false);
        gameoverText.gameObject.SetActive(false);
    }

    
    void Update()
    {
        if (raceCompletion != null && raceCompletion.finishRace)
        {
            ShowQualifiedText();
        }
    }

    
    private void ShowQualifiedText()
    {
        qualifiedText.gameObject.SetActive(true);
        //qualifiedText.transform.DOScale(Vector3.one * 2f, .7f).SetEase(Ease.OutBack);
        Sequence sequence = DOTween.Sequence();

        sequence.Append(qualifiedText.transform.DOScale(Vector3.one * 2f, 0.7f).SetEase(Ease.OutBack))
                .AppendInterval(2f) 
                .AppendCallback(() => toResultsBtn.gameObject.SetActive(true)) 
                .Append(toResultsBtn.GetComponent<CanvasGroup>().DOFade(1f, 0.5f)); 

        sequence.Play();
        
        
    }

    public void ShowResults()
    {
        
        endPanel.gameObject.SetActive(true); 

        
        endPanel.anchoredPosition = new Vector2(0, 1080);

        
        endPanel.DOAnchorPos(Vector2.zero, .6f).SetEase(Ease.OutQuad);
    }

    public void ShowStandings()
    {
        List<GameObject> racers = raceManager.racers;

        
        racers.Sort(raceManager.CompareRacers);

        List<RacerData> racerDataList = new List<RacerData>();

        foreach (GameObject racer in racers)
        {
            RacerComponent data = racer.GetComponent<RacerComponent>();
            if (data != null)
            {
                racerDataList.Add(new RacerData
                {
                    racerName = data.racerName,
                    previousRacePosition = data.previousRacePosition,
                    defaultRacePosition = data.defaultRacePosition
                });
            }
        }

        standingsManager.PopulateResults(racerDataList);
    }
    
}
