using System;
using UnityEngine;

class EvadeState : BaseState
{
    private SmartTank tank; //Stores a reference to SmartTank script.

    public EvadeState(SmartTank tank) //Gets SmartTank script.
    {
        this.tank = tank;
    }

    public override Type StateEnter()
    {
        Debug.Log("EvadeState Entered");

        tank.stats["evading"] = true;
        tank.stats["swapToEvadeState"] = false;
        return null;
    }

    public override Type StateExit()
    {
        Debug.Log("EvadeState Exited");

        tank.stats["evading"] = false;
        return null;
    }

    public override Type StateUpdate()
    {
        //Checking each rule in the rules list to see if a state change should occur.
        foreach (var item in tank.rules.GetRules)
        {
            if (item.CheckRule(tank.stats) != null)
            {
                return item.CheckRule(tank.stats);
            }
        }

        //Checking each of the state's behaviour sequences so if one is evaluated as true the current update ends to find the next sequence to choose.
        if (tank.e_Evade.Evaluate() == BTNodeStates.SUCCESS)
        {
            Debug.Log("Evade Sequence");
            return null;
        }
        else if (tank.e_SwapToAttackState.Evaluate() == BTNodeStates.SUCCESS)
        {
            Debug.Log("Swap to AttackState Sequence");
            return null;
        }
        else if (tank.e_SwapToRetreatState.Evaluate() == BTNodeStates.SUCCESS)
        {
            Debug.Log("Swap to RetreatState Sequence");
            return null;
        }
        else if (tank.e_SwapToSearchState.Evaluate() == BTNodeStates.SUCCESS)
        {
            Debug.Log("Swap to SearchState Sequence");
            return null;
        }

        return null;
    }
}
