using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Executor_",menuName = "Event/Sequence Executor")]
public class SequenceEventExecutor : ScriptableObject
{
    public Action<bool> OnFinished; //bool参数代表执行器执行是否成功

    private int _index; //代表当前执行到第几个节点
    public EventNodeBase[] nodes; //事件节点数组

    public void Init(Action<bool> OnFinishedEvent)
    {
        _index = 0;

        foreach (EventNodeBase item in nodes)
        {
            item.Init(OnNodeFinished);
        }

        OnFinished = OnFinishedEvent;
    }

    private void OnNodeFinished(bool success)
    {
        if (success)
        {
            ExecuteNextNode();
        }
        else
        {
            OnFinished(false);
        }
    }

    private void ExecuteNextNode()
    {
        if (_index < nodes.Length)
        {
            if (nodes[_index].state == NodeState.Waiting)
            {
                nodes[_index].Execute();
                _index++;
            }
            
        }
        else
        {
            OnFinished(true);
        }
    }

    public void Execute()
    {
        _index = 0;
        ExecuteNextNode();
    }
}
