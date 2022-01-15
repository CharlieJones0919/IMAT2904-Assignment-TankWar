using System;
using UnityEngine;

class AttackState : BaseState
{
    private SmartTank tank; //Stores a reference to SmartTank script.

    public AttackState(SmartTank tank) //Gets SmartTank script.
    {
        this.tank = tank;
    }

    public override Type StateEnter()
    {
        Debug.Log("AttackState Entered");

        tank.stats["attacking"] = true;
        tank.stats["swapToAttackState"] = false;
 
        return null;
    }

    public override Type StateExit()
    {
        Debug.Log("AttackState Exited");

        tank.stats["attacking"] = false;
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
        if (tank.a_PursueEnemy.Evaluate() == BTNodeStates.SUCCESS)
        {
            Debug.Log("Pursue Enemy Sequence");
            return null;
        }
        else if (tank.a_AttackEnemy.Evaluate() == BTNodeStates.SUCCESS)
        {
            Debug.Log("Attack Enemy Sequence");
            return null;
        }
        else if (tank.a_PursueBase.Evaluate() == BTNodeStates.SUCCESS)
        {
            Debug.Log("Pursue Base Sequence");
            return null;
        }
        else if (tank.a_PursueFarEnemy.Evaluate() == BTNodeStates.SUCCESS)
        {
            Debug.Log("Pursue Far Enemy Sequence");
            return null;
        }
        else if (tank.a_AttackBase.Evaluate() == BTNodeStates.SUCCESS)
        {
            Debug.Log("Attack Base Sequence");
            return null;
        }
        else if (tank.a_SwapToEvadeState.Evaluate() == BTNodeStates.SUCCESS)
        {
            Debug.Log("Swap to EvadeState Sequence");
            return null;
        }
        else if (tank.a_SwapToRetreatState.Evaluate() == BTNodeStates.SUCCESS)
        {
            Debug.Log("Swap to RetreatState Sequence");
            return null;
        }
        else if (tank.a_SwapToSearchStateNoTargets.Evaluate() == BTNodeStates.SUCCESS)
        {
            Debug.Log("Swap to SearchState Sequence");
            return null;
        }
        else if (tank.a_SwapToSearchState.Evaluate() == BTNodeStates.SUCCESS)
        {
            Debug.Log("Swap to SearchState Sequence");
            return null;
        }
        else if (tank.a_SwapToSearchStateLowFuel.Evaluate() == BTNodeStates.SUCCESS)
        {
            Debug.Log("Swap to SearchState for LowFuel Sequence");
            return null;
        }

        return null;
    }
}