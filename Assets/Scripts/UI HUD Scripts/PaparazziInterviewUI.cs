using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PaparazziInterviewUI : MonoBehaviour
{
    public RectTransform panel;
    public float slideDuration = .2f;
    public Ease easingType = Ease.OutQuad;

    private Vector3 originalPosition;
    private Vector3 hiddenPosition;
    private Vector3 shownPosition;

    private PaparazziCarCon paparazziController;

    void Start()
    {
        
        hiddenPosition = new Vector3(panel.localPosition.x, -695, panel.localPosition.z);
        shownPosition = panel.localPosition;

        
        HidePanel();

        
        paparazziController = FindObjectOfType<PaparazziCarCon>();
    }

    void Update()
    {
    
        if (paparazziController.currentState == PaparazziCarCon.PaparazziState.Interviewing)
        {
            ShowPanel();
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
}


