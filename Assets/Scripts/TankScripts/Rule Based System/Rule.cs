using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

//Class creates objects of the "Rule" which takes 2 facts, compares them with a specified comparison, and returns a resultant state to swap to if the rule's conditions are made true.
public class Rule
{
    //The 2 facts given for comparison.
    public string atecedentA;  
    public string atecedentB;

    public Type consequentState;    //The state which will be returned if the rule's conditions are evaluated as true.
    public Predicate compare;       //The comparison to be done on the 2 facts.
    public enum Predicate           //Kinds of comparison which can be done on the facts.
    { And, Or, nAnd }

    public Rule(string atecedentA, string atecedentB, Type consequentState, Predicate compare)  //Allows for the required data to be set from SmartTank.
    {
        this.atecedentA = atecedentA;
        this.atecedentB = atecedentB;
        this.consequentState = consequentState;
        this.compare = compare;
    }

    public Type CheckRule(Dictionary<string, bool> stats)   //Function to check the rule's conditions.
    {
        bool atecedentABool = stats[atecedentA];
        bool atecedentBBool = stats[atecedentB];

        //Defines how the specified comparison types are evaluated.
        switch (compare)
        {
            case Predicate.And:
                if (atecedentABool && atecedentBBool)
                {
                    return consequentState;
                }
                else
                {
                    return null;
                }

            case Predicate.Or:

                if (atecedentABool || atecedentBBool)
                {
                    return consequentState;
                }
                else
                {
                    return null;
                }


            case Predicate.nAnd:

                if (!atecedentABool && !atecedentBBool)
                {
                    return consequentState;
                }
                else
                {
                    return null;
                }

            default:

                return null;
        }
    }
}


