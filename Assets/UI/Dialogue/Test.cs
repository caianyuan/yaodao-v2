using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    // Start is called before the first frame update

    // [SerializeField] private AdvanceText _text;
    // [SerializeField] private string content;
    [SerializeField] private Widget _widget;
    void Start()
    {
        // _text.ShowTextByTyping(content);
        _widget.Fade(1, 2, null);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
