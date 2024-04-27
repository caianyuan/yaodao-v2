using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialogueData
{
    public string Speaker;
    [Multiline] public string Content;
    public bool AutoNext;
    public bool NeedTyping;
    public bool CanQuickShow;
}

[CreateAssetMenu(fileName = "Node_",menuName = "Event/Message/Show Dialogue")]
public class EN_ShowDialogue : EventNodeBase
{
    public DialogueData[] datas;
    private int _index; //当前已经显示到了第几个对话

    public override void Execute()
    {
        base.Execute();
        _index = 0;
        UIManager.OpenDialogueBox();
    }

    private void ShowNextDialogue(bool forceDisplayDirectly)
    {
        if (_index < datas.Length)
        {
            DialogueData data = new DialogueData()
            {
                Speaker = datas[_index].Speaker,
                Content = datas[_index].Content,
                CanQuickShow = datas[_index].CanQuickShow,
                AutoNext = datas[_index].AutoNext,
                NeedTyping = !forceDisplayDirectly && datas[_index].NeedTyping,
            };
            UIManager.PrintDialogue(data);
            _index++;
        }
        else
        {
            state = NodeState.Finished;
            OnFinished(true);
        }
    }
}
