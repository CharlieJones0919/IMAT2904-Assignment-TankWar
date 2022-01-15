using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BTBaseNode
{
    //The current state of the node.
    protected BTNodeStates btNodeState;

    //Return the node's success state.
    public BTNodeStates BTNodeState
    {
        get { return btNodeState; }
    }

    //Evavluate the state's set of conditions.
    public abstract BTNodeStates Evaluate();
}
