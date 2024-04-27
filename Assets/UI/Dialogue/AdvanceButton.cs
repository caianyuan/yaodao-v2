using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AdvanceButton : Button
{
    protected override void Awake()
    {
        base.Awake();
        onClick.AddListener(OnClickEvent);
    }
    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
        Select();
    }

    protected virtual void OnClickEvent()
    {
        
    }
    
}
