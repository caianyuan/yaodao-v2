using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueBox : MonoBehaviour
{
    // Start is called before the first frame update
    //[Header("Component")] [SerializeField] private Image _background;
    [SerializeField] private Widget _widget;
    //[SerializeField] private TextMeshProUGUI _speaker;
    //[SerializeField] private AdvanceText _content;
    //[SerializeField] private Widget _nextCursorWidget;
    //[SerializeField] private Animator _nextCursorAnimator;
    //private static readonly int _click = Animator.StringToHash("click");

    //[Header("Configs")] [SerializeField] private Sprite[] _backgroundStyle;

    // private bool _interactable;
    // private bool _printFinished;
    // private bool _canQuickShow;
    // private bool _autoNext;
    //
    // private bool CanQuickShow => !_printFinished && _canQuickShow;
    //
    // private bool CanNext => _printFinished;
    //
    // public Action<bool> OnNext; //bool参数代表下一句话是否强制直接显示


    // void Start()
    // {
    //     
    // }
    //
    // // Update is called once per frame
    // void Update()
    // {
    //     
    // }

    //打开 、关闭对话框
    public void Open()
    {
        // if (!gameObject.activeSelf)
        // {
        //     gameObject.SetActive(true);
        //     //_widget.Fade(1, 0.2f, null);
        //     //_speaker.SetText("");
        // }
        //gameObject.SetActive(true);
        _widget.Fade(1, 0.6f, null);
    }

    public void Close()
    {
        _widget.Fade(0, 0.4f, null);
        //gameObject.SetActive(false);
    }
}