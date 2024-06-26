using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;

public class PaparazziInterviewUI : MonoBehaviour
{
    public RectTransform panel;
    public float slideDuration = .2f;
    public Ease easingType = Ease.OutQuad;

    private Vector3 originalPosition;
    private Vector3 hiddenPosition;
    private Vector3 shownPosition;

    public PaparazziCarCon paparazziController;

    private Button[] answerButtons;

    public Slider timerSlider;
    


    void Start()
    {
        
        hiddenPosition = new Vector3(panel.localPosition.x, -742, panel.localPosition.z);
        shownPosition = panel.localPosition;

        
        HidePanel();

        
        //paparazziController = FindObjectOfType<PaparazziCarCon>();


        answerButtons = panel.GetComponentsInChildren<Button>();
        foreach (Button button in answerButtons)
        {
            button.onClick.AddListener(() => OnAnswerSelected(button));
        }

    }

    void Update()
    {
    
        if (paparazziController.currentState == PaparazziCarCon.PaparazziState.Interviewing)
        {
            ShowPanel();
            UpdateTimerSlider();
        }
        else
        {
            HidePanel();
        }
    }

    public void ShowPanel()
    {
        panel.DOLocalMoveY(shownPosition.y, slideDuration).SetEase(easingType);
    }

    public void HidePanel()
    {
        panel.DOLocalMoveY(hiddenPosition.y, slideDuration).SetEase(easingType);
    }

    private void OnAnswerSelected(Button selectedButton)
    {
        string selectedAnswer = selectedButton.GetComponentInChildren<TextMeshProUGUI>().text;
        //Debug.Log("Player selected answer: " + selectedAnswer);

        // End the interview and hide the panel
        paparazziController.EndInterview();
        HidePanel();
    }

    private void UpdateTimerSlider()
    {
        float timeRemainingRatio = paparazziController.interviewTimer / paparazziController.interviewDuration;
        timerSlider.value = timeRemainingRatio;
    }
}


