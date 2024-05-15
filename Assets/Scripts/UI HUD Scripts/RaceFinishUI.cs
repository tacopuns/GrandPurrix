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

    public RectTransform endPanel;

    private RaceCompletion raceCompletion;


    void Start()
    {
        raceCompletion = FindObjectOfType<RaceCompletion>();
        qualifiedText.gameObject.SetActive(false);
        gameoverText.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (raceCompletion != null && raceCompletion.finishRace)
        {
            ShowQualifiedText();
        }
    }

    //qualifiedText uses dotween to popout when the finishRace bool from race completion is true
    private void ShowQualifiedText()
    {
        qualifiedText.gameObject.SetActive(true);
        qualifiedText.transform.DOScale(Vector3.one * 1.2f, 0.5f).SetEase(Ease.OutBounce)
            .OnComplete(() => qualifiedText.transform.DOScale(Vector3.one, 0.2f));
        
        // Set raceCompletion.finishRace to false to avoid repeatedly showing the text
        //raceCompletion.finishRace = false;
    }

    
}
