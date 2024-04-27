using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public enum NodeState 
{
    //节点状态
    Waiting,
    Executing,
    Finished
}
public class EventNodeBase : ScriptableObject
{
    protected Action<bool> OnFinished; //bool参数代表节点执行是否成功  Action为回调
    [HideInInspector]public NodeState state;

    public virtual void Init(Action<bool> OnFinishedEvent)
    {
        OnFinished = OnFinishedEvent;
        state = NodeState.Waiting;

    }

    public virtual void Execute()
    {
        if (state != NodeState.Waiting)
            return;
        state = NodeState.Executing;
    }
}
