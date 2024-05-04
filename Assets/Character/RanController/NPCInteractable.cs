using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCInteractable : MonoBehaviour
{
    [SerializeField] private DialogueBox _dialogueBox;
    [SerializeField] private AdvanceButtonA _buttonA;
    [SerializeField] private AdvanceButtonA _buttonB;
    
    
    public void Interact()
    {
        //Debug.Log("找到RanHongXia了");
        _dialogueBox.Open();
        //_buttonA.
        //_text.ShowTextByTyping(content);
        //_buttonA.gameObject.SetActive(true);
        _buttonA.Open();
        _buttonB.Open();
        
    }
    
}
