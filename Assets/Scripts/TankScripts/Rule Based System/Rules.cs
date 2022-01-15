using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Allows for the creation of new rules which are added to a subsequent list of rules for ease of checking.
public class Rules
    {
    public void AddRule(Rule rule)
    {
        GetRules.Add(rule);
    }
    public List<Rule> GetRules { get; } = new List<Rule>();
}

