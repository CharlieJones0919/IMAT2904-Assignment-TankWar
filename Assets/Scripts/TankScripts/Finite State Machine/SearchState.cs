using System;
using UnityEngine;

class SearchState : BaseState
{
    private SmartTank tank; //Stores a reference to SmartTank script.

    public SearchState(SmartTank tank) //Gets SmartTank script.
    {
        this.tank = tank;
    }

    public override Type StateEnter()
    {
        Debug.Log("SearchState Entered");

        tank.stats["searching"] = true;
        tank.stats["swapToSearchState"] = false;
        return null;
    }

    public override Type StateExit()
    {
        Debug.Log("SearchState Exited");

        tank.stats["searching"] = false;
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
        if (tank.s_PursueEnemy.Evaluate() == BTNodeStates.SUCCESS)
        {
            Debug.Log("Found and Pursuing Enemy Sequence");
            return null;
        }
        else if (tank.s_PursueFarEnemy.Evaluate() == BTNodeStates.SUCCESS)
        {
            Debug.Log("Found and Pursuing Far Enemy Sequence");
            return null;
        }
        else if (tank.s_PursueConsumable.Evaluate() == BTNodeStates.SUCCESS)
        {
            Debug.Log("Found and Pursuing Consumable Sequence");
            return null;
        }
        else if (tank.s_PursueFarConsumable.Evaluate() == BTNodeStates.SUCCESS)
        {
            Debug.Log("Found and Pursuing Far Consumable Sequence");
            return null;
        }
        else if (tank.s_PursueBase.Evaluate() == BTNodeStates.SUCCESS)
        {
            Debug.Log("Found and Pursuing Base Sequence");
            return null;
        }
        else if (tank.s_WanderRandomly.Evaluate() == BTNodeStates.SUCCESS)
        {
            Debug.Log("Wander Sequence");
            return null;
        }
        else if (tank.s_SwapToRetreatState.Evaluate() == BTNodeStates.SUCCESS)
        {
            Debug.Log("Swap to RetreatState Sequence");
            return null;
        }

        return null;
    }
}