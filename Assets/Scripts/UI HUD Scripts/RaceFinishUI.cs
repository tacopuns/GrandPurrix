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
        //qualifiedText.transform.DOScale(Vector3.one * 2f, .7f).SetEase(Ease.OutBack);
        Sequence sequence = DOTween.Sequence();

        sequence.Append(qualifiedText.transform.DOScale(Vector3.one * 2f, 0.7f).SetEase(Ease.OutBack))
                .AppendInterval(2f) // Wait for 2 seconds before showing the button
                .AppendCallback(() => toResultsBtn.gameObject.SetActive(true)) // Activate the button
                .Append(toResultsBtn.GetComponent<CanvasGroup>().DOFade(1f, 0.5f)); // Fade in the button

        sequence.Play();
        
        
    }

    public void ShowResults()
    {
        //endpanel set active and transition in
        endPanel.gameObject.SetActive(true); // Ensure the panel is active

        // Move the panel from off-screen (assuming the screen width is 1920 for this example)
        endPanel.anchoredPosition = new Vector2(0, 1080); // Set the initial position off-screen to the left

        // Animate the panel to slide in from the left
        endPanel.DOAnchorPos(Vector2.zero, .6f).SetEase(Ease.OutQuad);
    }

    
}
