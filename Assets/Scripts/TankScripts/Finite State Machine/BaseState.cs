using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Defines the structure of the state classes for all states to derive from. (What functions they have).
public abstract class BaseState 
{
    public abstract Type StateEnter();
    public abstract Type StateUpdate();
    public abstract Type StateExit();
}
