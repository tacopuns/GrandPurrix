using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class IncomingUI : MonoBehaviour
{
    public RectTransform panel; 
    public float slideDuration = 1f; 
    public float notificationDuration = 2f; 
    public float fadeDuration = 1f; 
    public Ease easingType = Ease.OutQuad; 

    private Vector3 originalPosition; 

    public CanvasGroup canvasGroup;
    private PaparazziCarCon paparazziController;
    
    void Start()
    {
        paparazziController = FindObjectOfType<PaparazziCarCon>();
        
        originalPosition = panel.localPosition;

        
        HideIncoming();

        
        canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = panel.gameObject.AddComponent<CanvasGroup>();
    }

    void Update()
    {
        
        if (paparazziController.currentState == PaparazziCarCon.PaparazziState.FollowingPlayer)
        {
            
            ShowIncoming();
            
        }
    }

    public void ShowIncoming()
    {
        canvasGroup.alpha = 1f;
        
        panel.DOLocalMoveX(originalPosition.x, slideDuration).SetEase(easingType).OnComplete(() =>
        {
            
            FadeIncoming();
        });
    }

    private void FadeIncoming()
    {
        
        DOVirtual.DelayedCall(notificationDuration, () =>
        {
            
            canvasGroup.DOFade(0f, fadeDuration).OnComplete(() =>
            {
                
                HideIncoming();
            });
        });
    }

    private void HideIncoming()
    {
        
        panel.localPosition = new Vector3(Screen.width + panel.rect.width, originalPosition.y, originalPosition.z);
        
        canvasGroup.alpha = 0f;
    }
    
}
