using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class IncomingUI : MonoBehaviour
{
    public RectTransform panel; // Reference to the panel's RectTransform component
    public float slideDuration = 1f; // Duration of the slide animation
    public float notificationDuration = 2f; // Duration of the notification display
    public float fadeDuration = 1f; // Duration of the fade animation
    public Ease easingType = Ease.OutQuad; // Type of easing for the animation

    private Vector3 originalPosition; // Original position of the panel

    public CanvasGroup canvasGroup;
    
    void Start()
    {
        // Store the original position of the panel
        originalPosition = panel.localPosition;

        // Hide the panel initially
        HideIncoming();

        // Get or add CanvasGroup component
        canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = panel.gameObject.AddComponent<CanvasGroup>();
    }

    public void ShowIncoming()
    {
        canvasGroup.alpha = 1f;
        // Animate the panel's position to slide in from the right
        panel.DOLocalMoveX(originalPosition.x, slideDuration).SetEase(easingType).OnComplete(() =>
        {
            // After the slide animation is complete, start the fade out animation
            FadeIncoming();
        });
    }

    private void FadeIncoming()
    {
        // Wait for the specified notification duration before fading out
        DOVirtual.DelayedCall(notificationDuration, () =>
        {
            // Fade out the panel
            canvasGroup.DOFade(0f, fadeDuration).OnComplete(() =>
            {
                // Hide the panel after the fade animation is complete
                HideIncoming();
            });
        });
    }

    private void HideIncoming()
    {
        // Move the panel outside the screen to hide it
        panel.localPosition = new Vector3(Screen.width + panel.rect.width, originalPosition.y, originalPosition.z);
        // Reset the panel's alpha
        canvasGroup.alpha = 0f;
    }
    
}
