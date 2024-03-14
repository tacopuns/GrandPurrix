using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PaparazziInterviewUI : MonoBehaviour
{
    public RectTransform panel; // Reference to the panel's RectTransform component
    public float slideDuration = .2f; // Duration of the slide animation
    public Ease easingType = Ease.OutQuad; // Type of easing for the animation

    private Vector3 originalPosition; //Vector3 (818.049561,276,0) // Original position of the panel

    private Vector3 hiddenPosition; // Position of the panel when it's hidden
    private Vector3 shownPosition;

    void Start()
    {
        // Store the hidden and shown positions of the panel
        hiddenPosition = new Vector3(panel.localPosition.x, -695, panel.localPosition.z);
        shownPosition = panel.localPosition;

        // Move the panel initially to hide it
        HidePanel();
    }

    public void ShowPanel()
    {
        // Animate the panel's position to slide up from the bottom
        panel.DOLocalMoveY(shownPosition.y, slideDuration).SetEase(easingType);
    }

    public void HidePanel()
    {
        // Move the panel below the screen to hide it
        panel.localPosition = hiddenPosition;
    }

}
