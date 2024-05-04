using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class AdvanceButtonA : AdvanceButton
{
    Widget _widgetButton;
    Widget _frontRing;
    Animator _animator;
    private Widget _canvas;
    // [SerializeField] private ButtonType _buttonType;
    //
    // public enum ButtonType
    // {
    //     option1,
    //     option2,
    // }
    protected override void Awake()
    {
        base.Awake();
        _frontRing = transform.Find("Front Ring White").GetComponent<Widget>();
        _widgetButton = transform.GetComponent<Widget>();
        _animator = GetComponent<Animator>();
        _canvas = transform.parent.parent.GetComponent<Widget>();
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
        _canvas.Fade(0, 0.4f, null);
        //Debug.Log("鼠标发生了点击,关闭对话框");

    }

    protected void buttonActive()
    {
        this.IsActive();
    }

    public void Open()
    {
        _widgetButton.Fade(1.0f, 0.6f, null);
    }

    public void Close()
    {
        _widgetButton.Fade(0, 0.4f, null);
    }

    public void OptionSelect(string option)
    {
        Debug.Log(option);
    }
}
