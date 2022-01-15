using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StateMachine : MonoBehaviour
{
    private Dictionary<Type, BaseState> states; //List of all the FSM's states.
    public BaseState currentState; //Stores the current active state.

    public BaseState CurrentState //Gets the current state.
    {
        get
        {
            return currentState;
        }
        private set
        {
            currentState = value;
        }
    }

    public void SetStates(Dictionary<Type, BaseState> states) //Allows for states to be set into the list of states.
    {
        this.states = states;
    }

    void Update() //Allows for current state to be swapped into the next state in the list.
    {
        if(CurrentState == null)
        {
            CurrentState = states.Values.First();
        }
        else
        {
            var nextState = CurrentState.StateUpdate();

            if(nextState != null && nextState != CurrentState.GetType())
            {
                SwitchToState(nextState);
            }
        }
    }

    void SwitchToState(Type nextState) //Upon exiting the current state, swap into the next one in the list.
    {
        CurrentState.StateExit();
        CurrentState = states[nextState];
        CurrentState.StateEnter();
    }
}