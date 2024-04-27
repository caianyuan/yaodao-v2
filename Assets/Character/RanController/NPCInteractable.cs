using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCInteractable : MonoBehaviour
{
    [SerializeField] private DialogueBox _dialogueBox;
    
    
    public void Interact()
    {
        //Debug.Log("找到RanHongXia了");
        _dialogueBox.Open();
        //_text.ShowTextByTyping(content);
    }
    
}
