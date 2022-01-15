using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BTSequence : BTBaseNode
{
    //A list of the action nodes in the sequence. (Children nodes of the sequence).
    protected List<BTBaseNode> btNodes = new List<BTBaseNode>();

    //Sets the nodes into the sequence.
    public BTSequence(List<BTBaseNode> btNodes)
    {
        this.btNodes = btNodes;
    }

    //If any action node fails, the sequence node fails. 
    public override BTNodeStates Evaluate()
    {
        bool failed = false;
        foreach (BTBaseNode btNode in btNodes)
        {
            if (failed == true)
            {
                break;
            }

            switch (btNode.Evaluate())
            {
                case BTNodeStates.FAILURE:
                    btNodeState = BTNodeStates.FAILURE;
                    failed = true;
                    return btNodeState;
                case BTNodeStates.SUCCESS:
                    btNodeState = BTNodeStates.SUCCESS;
                    continue;
                default:
                    btNodeState = BTNodeStates.FAILURE;
                    return btNodeState;
            }
        }
        return btNodeState;
    }
}