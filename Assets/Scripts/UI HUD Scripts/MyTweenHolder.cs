using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class MyTweenHolder : MonoBehaviour
{
    /*public Tween numberBounce; //player position update, race countdown
    public Tween interviewShow; //interview panel show
    public Tween interviewHide; //interview panel hide
    public Tween incomingShow; //incoming show
    public Tween fadeOut; //fade out (incoming ui)
    public Tween fadeIn; //fade in (to results screen btn)*/

    
    void Awake()
    {
        DOTween.Init(true, true, LogBehaviour.ErrorsOnly).SetCapacity(2000, 200);
        
    }

    void Start()
    {
        //numberBounce = //
    }

   
    void Update()
    {
        
    }
}
