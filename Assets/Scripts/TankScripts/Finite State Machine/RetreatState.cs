using System;
using UnityEngine;

class RetreatState : BaseState
{
    private SmartTank tank; //Stores a reference to SmartTank script.

    public RetreatState(SmartTank tank) //Gets SmartTank script.
    {
        this.tank = tank;
    }

    public override Type StateEnter()
    {
        Debug.Log("RetreatState Entered");

        tank.stats["retreating"] = true;
        tank.stats["swapToRetreatState"] = false;

        return null;
    }

    public override Type StateExit()
    {
        Debug.Log("RetreatState Exited");

        tank.stats["retreating"] = false;
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
        if (tank.r_Retreat.Evaluate() == BTNodeStates.SUCCESS)
        {
            Debug.Log("Retreat Sequence");
            return null;
        }
        else if (tank.r_RetreatLowFuel.Evaluate() == BTNodeStates.SUCCESS)
        {
            Debug.Log("Retreat for LowFuel Sequence");
            return null;
        }
        else if (tank.r_SwapToSearchState.Evaluate() == BTNodeStates.SUCCESS)
        {
            Debug.Log("Swap to SearchState Sequence");
            return null;
        }

        return null;
    }
}
