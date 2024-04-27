using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class AdvanceButtonA : AdvanceButton
{
    Widget _frontRing;
    Animator _animator;

    protected override void Awake()
    {
        base.Awake();
        _frontRing = transform.Find("Front Ring White").GetComponent<Widget>();
        _animator = GetComponent<Animator>();
    }

    public override void OnSelect(BaseEventData eventData)
    {
        base.OnSelect(eventData);
        _frontRing.Fade(1, 0.1f, null);
    }

    public override void OnDeselect(BaseEventData eventData)
    {
        base.OnDeselect(eventData);
        _frontRing.Fade(0, 0.25f, null);
    }

    private static readonly int Click = Animator.StringToHash("Click");
    protected override void OnClickEvent()
    {
        base.OnClickEvent();
        _animator.SetTrigger(Click);
        Debug.Log("鼠标发生了点击");

    }
}
