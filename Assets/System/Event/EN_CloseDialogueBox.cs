using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Node_",menuName = "Event/Message/Close Dialogue Box")]
public class EN_CloseDialogueBox : EventNodeBase
{
    public override void Execute()
    {
        base.Execute();
        UIManager.CloseDialogueBox();
        state = NodeState.Finished;
        OnFinished(true);
    }
}
