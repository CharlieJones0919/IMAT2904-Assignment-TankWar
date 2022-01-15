using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class SmartTank : AITank
{
    public Dictionary<GameObject, float> targets = new Dictionary<GameObject, float>();         //Stores found enemy tanks from AITank.
    public Dictionary<GameObject, float> consumables = new Dictionary<GameObject, float>();     //Stores found consumables from AITank.
    public Dictionary<GameObject, float> bases = new Dictionary<GameObject, float>();           //Stores found enemy bases from AITank.

    public GameObject enemyTank;    //First enemy tank object from the list.
    public GameObject consumable;   //First consumable object from the list.
    public GameObject enemyBase;    //First enemy base object from the list.

    float t;                        //Time passed while wandering. 
    float wanderTime = 30.0f;       //Time to spend wandering before generating a new random point to follow.

    //////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////// RULE BASED SYSTEM DATA  ///////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// Data used for the rule based system's fact and rule conditions.
    public Dictionary<string, bool> stats = new Dictionary<string, bool>();     //Stores the tank's "facts" which are defined in the start function.
    public Rules rules = new Rules();                                           //Rules created from comparing facts to result in a state change.
    private float healthcheck;                                                  //Stores the value of the tanks health from the previous update to see if it's decreasing to deterimine if the tank is being attacked.

    private float howLongToNotBeAttacked = 5.0f;                                //How much time must pass from the last loss of health, for the stat "beingAttacked" to be set back to false.
    private float howLongToBeStillFor = 10.0f;                                  //How much time can pass while attacking before the tank swaps into the EvadeState.
    public float attacktimer;                                                   //How much time has passed since the tank was last attacked.
    public float stillTooLongTimer;                                             //How long the tank has been attacking for since the last swap to EvadeState.
    public float shootingRange;                                                 //Distance at which the tank will attempt to fire from.
    public float tooFarRange;                                                   //How far is deemed too far to attempt to travel to with low fuel.
    public float tooCloseRange;                                                 //How far is deemed too close to the tank to fire from.

    /////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////// BEHAVIOURAL TREE ACTION NODES ////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// Action nodes which are children to sequence nodes which must be successful in a sequence for the sequence node to succeed.

    ////////// STAT CHECKS //////////
    /// Checking facts from the RBS to determine if the result of the sequence should proceed.
    public BTAction checkSearching;
    public BTAction checkAttacking;
    public BTAction checkEvading;
    public BTAction checkRetreating;

    public BTAction checkHealthLowTrue;
    public BTAction checkHealthLowFalse;
    public BTAction checkAmmoLowTrue;
    public BTAction checkAmmoLowFalse;
    public BTAction checkFuelLowTrue;
    public BTAction checkFuelLowFalse;

    public BTAction checkHealthOrAmmoLow;
    public BTAction checkFineHealthAndAmmo;
    public BTAction checkResourcesAllFineTrue;
    public BTAction checkResourcesAllFineFalse;

    public BTAction checkEnemyFound;
    public BTAction checkBaseFound;
    public BTAction checkConsumableFound;
    public BTAction checkNoTargetFound;

    public BTAction checkBeingAttackedTrue;
    public BTAction checkBeingAttackedFalse;
    public BTAction checkWithinShootingRangeTrue;
    public BTAction checkWithinShootingRangeFalse;
    public BTAction checkNotTooClose;
    public BTAction checkTargetIsFarTrue;
    public BTAction checkTargetIsFarFalse;
    public BTAction checkBeenStillTooLongTrue;
    public BTAction checkBeenStillTooLongFalse;

    ////////// ACTIONABLE //////////
    /// Resultant child nodes of sequences carried through if the nodes prior were evaluated as successful.
    public BTAction fireAtTank;
    public BTAction fireAtBase;

    public BTAction travelToEnemy;
    public BTAction travelToBase;
    public BTAction travelToConsumable;

    public BTAction travelToEvadePoint;
    public BTAction travelToRetreatPoint;
    public BTAction travelToRandomPoint;

    public BTAction swapToSearch;
    public BTAction swapToAttack;
    public BTAction swapToEvade;
    public BTAction swapToRetreat;

    /////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////// BEHAVIOURAL TREE SEQUENCES ///////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Lists of BTActions which can be evaluated as successful.
    /// </summary>
    
    /////////////// Sequences Used by SearchState ///////////////
    /// Sequences which are only evaluated in SearchState.
    public BTSequence s_PursueEnemy;            //Swaps into AttackState if an enemy is found and the tank has sufficient health and ammo.
    public BTSequence s_PursueFarEnemy;         //Swaps into AttackState if an enemy is found while fuel is low but it isn't too far in distance.
    public BTSequence s_PursueBase;             //Swaps into AttackState if a base is found and the tank has sufficient health and ammo.
    public BTSequence s_PursueConsumable;       //Moves towards consumable if one is found and fuel isn't low.
    public BTSequence s_PursueFarConsumable;    //Moves towards consumable if one is found and fuel is low, but the consumable isn't too far away.

    public BTSequence s_WanderRandomly;         //Tank goes to random points on the map.
    public BTSequence s_SwapToRetreatState;     //If health or ammo are low and the tank is being attacked, swap into RetreatState.

    /////////////// Sequences Used by AttackState ///////////////
    /// Sequences which are only evaluated in AttackState.
    public BTSequence a_PursueEnemy;            //Move towards an enemy if one is found and they're further in distance than shootingRange.
    public BTSequence a_PursueFarEnemy;         //Move towards an enemy if one is found and they're further in distance than shootingRange, and fuel is low but the enemy tank isn't far.
    public BTSequence a_PursueBase;             //Move towards a base if one is found and the tank isn't currently being attacked.

    public BTSequence a_AttackEnemy;            //Fire at the found enemy tank if within shooting range and the tank has ammo and health.
    public BTSequence a_AttackBase;             //Fire at the found base if it's within shooting range and the tank has ammo and fuel plus isn't being attacked.

    public BTSequence a_SwapToEvadeState;       //Tank has been still for too long while attacking so swaps into EvadeState.
    public BTSequence a_SwapToRetreatState;     //Tank is low on health or ammo so retreats.
    public BTSequence a_SwapToSearchStateNoTargets; //The tank no longer has a target so swaps back into SearchState.
    public BTSequence a_SwapToSearchState;      //The tank is low on health or ammo so swaps back into SearchState.
    public BTSequence a_SwapToSearchStateLowFuel;

    /////////////// Sequences Used by EvadeState ///////////////
    /// Sequences which are only evaluated in EvadeState.
    public BTSequence e_Evade;                  //Move away from the enemy slightly then go back into AttackState.
    public BTSequence e_SwapToAttackState;      //Swap back to AttackState after evading if health and ammo are still fine. 
    public BTSequence e_SwapToRetreatState;     //Health or ammo are low while the tank is being attacked so swap into RetreatState.
    public BTSequence e_SwapToSearchState;      //Health or ammo are low but the tank isn't being attacked so swap into SearchState.

    /////////////// Sequences Used by RetreatState ///////////////
    /// Sequences which are only evaluated in RetreatState.
    public BTSequence r_Retreat;                //Go to a random point which is far from the enemy tank.
    public BTSequence r_RetreatLowFuel;         //Go to a random point which is far from the enemy tank with low fuel.
    public BTSequence r_SwapToSearchState;      //Tank is no longer being attacked so swap back into SearchState.

    /*******************************************************************************************************************************************     
    ************************************************************   INITIALISE FSM   ************************************************************
    /*******************************************************************************************************************************************/
    private void InitializeStateMachine()
    {
        Dictionary<Type, BaseState> states = new Dictionary<Type, BaseState>(); //A list to store all the states in the finite state machine with SearchState as the starting state.

        //Adding all the states to the list of states.
        states.Add(typeof(SearchState), new SearchState(this));
        states.Add(typeof(AttackState), new AttackState(this));
        states.Add(typeof(EvadeState), new EvadeState(this));
        states.Add(typeof(RetreatState), new RetreatState(this));

        //Setting the states to the finite state machine.
        GetComponent<StateMachine>().SetStates(states);
    }

    /*******************************************************************************************************************************************     
    ************************************************************   AWAKE FUNCTION   ************************************************************
    /*******************************************************************************************************************************************/
    private void Awake()
    {
        InitializeStateMachine();   //Calling for the FSM to be initialised before runtime.
    }

    /*******************************************************************************************************************************************     
    ************************************************************   START FUNCTION   ************************************************************
    /*******************************************************************************************************************************************/
    public override void AITankStart()
    {
        ///////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////// RBS STATS AND RULES  ///////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////////////////
        ///These are set to true on entry to their respective states and set to false on exit. They allow for the rules to determine which state is currently active so they can determine which state to swap to when needed.
        stats.Add("searching", true);
        stats.Add("attacking", false);
        stats.Add("evading", false);
        stats.Add("retreating", false);

        ///These are set to true in sequences when a state needs to be swapped to.
        stats.Add("swapToSearchState", false);
        stats.Add("swapToAttackState", false);
        stats.Add("swapToEvadeState", false);
        stats.Add("swapToRetreatState", false);

        //Rules for Swapping from SearchState
        rules.AddRule(new Rule("searching", "swapToAttackState", typeof(AttackState), Rule.Predicate.And));     //If currently in SearchState and the stat "swapToAttackState" is set to true, swap into AttackState.
        rules.AddRule(new Rule("searching", "swapToRetreatState", typeof(RetreatState), Rule.Predicate.And));

        //Rules for Swapping from AttackState
        rules.AddRule(new Rule("attacking", "swapToSearchState", typeof(SearchState), Rule.Predicate.And));
        rules.AddRule(new Rule("attacking", "swapToEvadeState", typeof(EvadeState), Rule.Predicate.And));
        rules.AddRule(new Rule("attacking", "swapToRetreatState", typeof(RetreatState), Rule.Predicate.And));

        //Rules for Swapping from EvadeState
        rules.AddRule(new Rule("evading", "swapToAttackState", typeof(AttackState), Rule.Predicate.And));
        rules.AddRule(new Rule("evading", "swapToRetreatState", typeof(RetreatState), Rule.Predicate.And));
        rules.AddRule(new Rule("evading", "swapToSearchState", typeof(SearchState), Rule.Predicate.And));

        //Rule for Swapping from RetreatState
        rules.AddRule(new Rule("retreating", "swapToSearchState", typeof(SearchState), Rule.Predicate.And));

        //Initialising variables on start.
        healthcheck = GetHealth;
        attacktimer = howLongToNotBeAttacked;
        stillTooLongTimer = howLongToBeStillFor;
        shootingRange = 25.0f;
        tooFarRange = 50.0f;
        tooCloseRange = 10.0f;

        //Initialising stat facts on start to false.
        stats.Add("healthLow", false);
        stats.Add("ammoLow", false);
        stats.Add("fuelLow", false);
        stats.Add("fuelCritical", false);
        stats.Add("healthOrAmmoLow", false);
        stats.Add("fineHealthandAmmo", true);
        stats.Add("resourcesAllFine", true);

        stats.Add("enemyFound", false);
        stats.Add("consumableFound", false);
        stats.Add("baseFound", false);
        stats.Add("noTargetFound", false);

        stats.Add("beingAttacked", false);
        stats.Add("withinShootingRange", false);
        stats.Add("notTooClose", false);
        stats.Add("targetIsFar", false);
        stats.Add("beenStillTooLong", false);

        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////// BEHAVIOURAL TREE ACTION NODES ////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///Setting functions to the actions.
        
        ////////// STAT CHECKS //////////
        checkSearching = new BTAction(CheckSearching);
        checkAttacking = new BTAction(CheckAttacking);
        checkEvading = new BTAction(CheckEvading);
        checkRetreating = new BTAction(CheckRetreating);

        checkHealthLowTrue = new BTAction(CheckHealthLowTrue);
        checkHealthLowFalse = new BTAction(CheckHealthLowFalse);
        checkAmmoLowTrue = new BTAction(CheckAmmoLowTrue);
        checkAmmoLowFalse = new BTAction(CheckAmmoLowFalse);
        checkFuelLowTrue = new BTAction(CheckFuelLowTrue);
        checkFuelLowFalse = new BTAction(CheckFuelLowFalse);

        checkHealthOrAmmoLow = new BTAction(CheckHealthOrAmmoLow);
        checkFineHealthAndAmmo = new BTAction(CheckFineHealthAndAmmo);
        checkResourcesAllFineTrue = new BTAction(CheckResourcesAllFineTrue);
        checkResourcesAllFineFalse = new BTAction(CheckResourcesAllFineFalse);

        checkEnemyFound = new BTAction(CheckEnemyFound);
        checkBaseFound = new BTAction(CheckBaseFound);
        checkConsumableFound = new BTAction(CheckConsumableFound);
        checkNoTargetFound = new BTAction(CheckNoTargetFound);

        checkBeingAttackedTrue = new BTAction(CheckBeingAttackedTrue);
        checkBeingAttackedFalse = new BTAction(CheckBeingAttackedFalse);
        checkWithinShootingRangeTrue = new BTAction(CheckWithinShootingRangeTrue);
        checkWithinShootingRangeFalse = new BTAction(CheckWithinShootingRangeFalse);
        checkNotTooClose = new BTAction(CheckNotTooClose);
        checkTargetIsFarTrue = new BTAction(CheckTargetIsFarTrue);
        checkTargetIsFarFalse = new BTAction(CheckTargetIsFarFalse);
        checkBeenStillTooLongTrue = new BTAction(CheckBeenStillTooLongTrue);
        checkBeenStillTooLongFalse = new BTAction(CheckBeenStillTooLongFalse);

        ////////// ACTIONABLE //////////
        fireAtTank = new BTAction(FireAtTank);
        fireAtBase = new BTAction(FireAtBase);

        travelToEnemy = new BTAction(TravelToEnemy);
        travelToBase = new BTAction(TravelToBase);
        travelToConsumable = new BTAction(TravelToConsumable);

        travelToEvadePoint = new BTAction(TravelToEvadePoint);
        travelToRetreatPoint = new BTAction(TravelToRetreatPoint);
        travelToRandomPoint = new BTAction(TravelToRandomPoint);

        swapToSearch = new BTAction(SwapToSearch);
        swapToAttack = new BTAction(SwapToAttack);
        swapToEvade = new BTAction(SwapToEvade);
        swapToRetreat = new BTAction(SwapToRetreat);

        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////// BEHAVIOURAL TREE SEQUENCES ///////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///Defining the order/sequence of actions for each sequence.
        
        /////////////// Sequences Used by SearchState ///////////////
        s_PursueEnemy = new BTSequence(new List<BTBaseNode> { checkSearching, checkEnemyFound, checkResourcesAllFineTrue, swapToAttack });
        s_PursueFarEnemy = new BTSequence(new List<BTBaseNode> { checkSearching, checkEnemyFound, checkFineHealthAndAmmo, checkFuelLowTrue, checkTargetIsFarFalse, swapToAttack });
        s_PursueBase = new BTSequence(new List<BTBaseNode> { checkSearching, checkBaseFound, checkBeingAttackedFalse, checkFuelLowFalse, swapToAttack });
        s_PursueConsumable = new BTSequence(new List<BTBaseNode> { checkSearching, checkConsumableFound, checkBeingAttackedFalse, checkFuelLowFalse, travelToConsumable});
        s_PursueFarConsumable = new BTSequence(new List<BTBaseNode> { checkSearching, checkConsumableFound, checkBeingAttackedFalse, checkFuelLowTrue, checkTargetIsFarFalse, travelToConsumable });

        s_SwapToRetreatState = new BTSequence(new List<BTBaseNode> { checkSearching, checkBeingAttackedTrue, checkHealthOrAmmoLow, swapToRetreat });
        s_WanderRandomly = new BTSequence(new List<BTBaseNode> { checkSearching, checkBeingAttackedFalse, travelToRandomPoint });

        /////////////// Sequences Used by AttackState ///////////////
        a_PursueEnemy = new BTSequence(new List<BTBaseNode> { checkAttacking, checkEnemyFound, checkWithinShootingRangeFalse, checkResourcesAllFineTrue, travelToEnemy });
        a_PursueFarEnemy = new BTSequence(new List<BTBaseNode> { checkAttacking, checkEnemyFound, checkWithinShootingRangeFalse, checkFineHealthAndAmmo, checkFuelLowTrue, checkTargetIsFarFalse, travelToEnemy });
        a_PursueBase = new BTSequence(new List<BTBaseNode> { checkAttacking, checkBaseFound, checkWithinShootingRangeFalse, checkBeingAttackedFalse, checkAmmoLowFalse, checkFuelLowFalse, travelToBase });

        a_AttackEnemy = new BTSequence(new List<BTBaseNode> { checkAttacking, checkEnemyFound, checkWithinShootingRangeTrue, checkNotTooClose, checkFineHealthAndAmmo, checkBeenStillTooLongFalse, fireAtTank });
        a_AttackBase = new BTSequence(new List<BTBaseNode> { checkAttacking, checkBaseFound, checkBeingAttackedFalse, checkWithinShootingRangeTrue, checkAmmoLowFalse, fireAtBase });

        a_SwapToEvadeState = new BTSequence(new List<BTBaseNode> { checkAttacking, checkBeingAttackedTrue, checkBeenStillTooLongTrue, swapToEvade });
        a_SwapToRetreatState = new BTSequence(new List<BTBaseNode> { checkAttacking, checkBeingAttackedTrue, checkHealthOrAmmoLow, swapToRetreat });
        a_SwapToSearchStateNoTargets = new BTSequence(new List<BTBaseNode> { checkAttacking, checkNoTargetFound, swapToSearch });
        a_SwapToSearchState = new BTSequence(new List<BTBaseNode> { checkAttacking, checkHealthOrAmmoLow, swapToSearch });
        a_SwapToSearchStateLowFuel = new BTSequence(new List<BTBaseNode> { checkAttacking, checkFuelLowTrue, swapToSearch });

        /////////////// Sequences Used by EvadeState ///////////////
        e_Evade = new BTSequence(new List<BTBaseNode> { checkEvading, checkBeenStillTooLongTrue, travelToEvadePoint });
        e_SwapToAttackState = new BTSequence(new List<BTBaseNode> { checkEvading, checkBeenStillTooLongFalse, checkFineHealthAndAmmo, swapToAttack });
        e_SwapToRetreatState = new BTSequence(new List<BTBaseNode> { checkEvading, checkBeingAttackedTrue, checkHealthOrAmmoLow, swapToRetreat });
        e_SwapToSearchState = new BTSequence(new List<BTBaseNode> { checkEvading, checkBeingAttackedFalse, checkHealthOrAmmoLow, swapToSearch });

        /////////////// Sequences Used by RetreatState ///////////////
        r_Retreat = new BTSequence(new List<BTBaseNode> { checkRetreating, checkBeingAttackedTrue, checkFuelLowFalse, travelToRetreatPoint });
        //Originally we wanted to initialize game objects for travelling points to travel to which would have allowed for the checking of distance between. 
        r_RetreatLowFuel = new BTSequence(new List<BTBaseNode> { checkRetreating, checkBeingAttackedTrue, checkFuelLowTrue, checkTargetIsFarFalse, travelToRetreatPoint });
        r_SwapToSearchState = new BTSequence(new List<BTBaseNode> { checkRetreating, checkBeingAttackedFalse, swapToSearch });
    }

    /********************************************************************************************************************************************     
    ************************************************************   UPDATE FUNCTION   ************************************************************
    /********************************************************************************************************************************************/
    public override void AITankUpdate()
    {
        //Get the targets found from the sensor view.
        targets = GetTargetsFound; //Enemy Tanks
        consumables = GetConsumablesFound;
        bases = GetBasesFound;

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////// FACT CHECKS ////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///Checking the conditions for the stats list's facts to be true or false.

        if (targets.Count > 0)  //If an enemy tank has been found set the stats accordingly.
        {
            enemyTank = targets.First().Key;
            if (enemyTank != null)
            {
                stats["baseFound"] = false;
                stats["consumableFound"] = false;

                stats["enemyFound"] = true;
                stats["noTargetFound"] = false;

                if (Vector3.Distance(transform.position, enemyTank.transform.position) < shootingRange)
                {
                    stats["withinShootingRange"] = true;

                    if (Vector3.Distance(transform.position, enemyTank.transform.position) < tooCloseRange)
                    {
                        stats["notTooClose"] = false;
                    }
                    else
                    {
                        stats["notTooClose"] = true;
                    }
                }
                else
                {
                    stats["withinShootingRange"] = false;

                    if (Vector3.Distance(transform.position, enemyTank.transform.position) >= tooFarRange)
                    {
                        stats["targetIsFar"] = true;
                    }
                    else
                    {
                        stats["targetIsFar"] = false;
                    }
                }
            }
        }
        else if (consumables.Count > 0) //If a consumable has been found set the stats accordingly.
        {
            consumable = consumables.First().Key;
            if (consumable != null)
            {
                stats["enemyFound"] = false;
                stats["baseFound"] = false;

                stats["consumableFound"] = true;
                stats["noTargetFound"] = false;
            }
        }
        else if (bases.Count > 0) //If a base has been found set the stats accordingly.
        {
            enemyBase = bases.First().Key;
            if (enemyBase != null)
            {
                stats["enemyFound"] = false;
                stats["consumableFound"] = false;

                stats["baseFound"] = true;
                stats["noTargetFound"] = false;

                if (Vector3.Distance(transform.position, enemyBase.transform.position) <= shootingRange)
                {
                    stats["withinShootingRange"] = true;
                }
                else
                {
                    stats["withinShootingRange"] = false;

                    if (Vector3.Distance(transform.position, enemyBase.transform.position) >= tooFarRange)
                    {
                        stats["targetIsFar"] = true;
                    }
                    else
                    {
                        stats["targetIsFar"] = false;
                    }
                }

                enemyBase = null;
            }
            else
            {
                stats["baseFound"] = false;
                stats["noTargetFound"] = true;
            }
        }

        //If no objects were found, set the stats accordingly.
        if (targets.Count <= 0)
        {
            stats["enemyFound"] = false;
            stats["noTargetFound"] = true;
        }
        else if (consumables.Count <= 0)
        {
            stats["consumableFound"] = false;
            stats["noTargetFound"] = true;
        }
        else if (bases.Count <= 0)
        {
            stats["baseFound"] = false;
            stats["noTargetFound"] = true;
        }

        if (healthcheck > GetHealth) //Check if tank has been hit again since last update.
        {
            stats["beingAttacked"] = true;
            attacktimer = howLongToNotBeAttacked;
        }
        healthcheck = GetHealth;

        if (stats["beingAttacked"] == true) 
        {
            attacktimer -= Time.deltaTime;
            if (attacktimer <= 0)
            {
                stats["beingAttacked"] = false;     //If enough time passes after being hit, update beingAttacked status.
            }

            stillTooLongTimer -= Time.deltaTime;
            if (stillTooLongTimer <= 0)
            {
                stats["beenStillTooLong"] = true;   //If enough time passes since the last swap to EvadeState, set beenStillTooLong status as true.
            }
        }
        else    //If not being attacked (anymore), set stats accordingly.
        {
            attacktimer = howLongToNotBeAttacked;
            stillTooLongTimer = howLongToBeStillFor;
            stats["beingAttacked"] = false;
        }

        if (GetHealth < 20.0f) //Check if health is low.
        {
            stats["healthLow"] = true;
            stats["fineHealthandAmmo"] = false;
            stats["healthOrAmmoLow"] = true;
            stats["resourcesAllFine"] = false;
        }
        else
        {
            stats["healthLow"] = false;
        }

        if (GetAmmo < 1) //Check if out of ammo.
        {
            stats["ammoLow"] = true;
            stats["fineHealthandAmmo"] = false;
            stats["healthOrAmmoLow"] = true;
            stats["resourcesAllFine"] = false;
        }
        else
        {
            stats["ammoLow"] = false;
        }

        if (GetFuel < 25) //Check if fuel is low.
        {
            stats["fuelLow"] = true;
            stats["resourcesAllFine"] = false;
            if (GetFuel < 5)    //Check if fuel is critical.
            {
                stats["fuelCritical"] = true;
            }
            else
            {
                stats["fuelCritical"] = false;
            }
        }
        else
        {
            stats["fuelLow"] = false;
        }

        if ((stats["healthLow"] == false) && (stats["ammoLow"] == false)) //Check extra stats.
        {
            stats["healthOrAmmoLow"] = false;
            stats["fineHealthandAmmo"] = true;

            if (stats["fuelLow"] == false)
            {
                stats["resourcesAllFine"] = true;
            }
        }
    }

    /******************************************************************************************************************************************     
    ************************************************************   FIRE FUNCTION   ************************************************************
    /******************************************************************************************************************************************/
    void Fire(GameObject target)
    {
        FireAtPointInWorld(target);
    }

    /*******************************************************************************************************************************************     
    ************************************************************   EVADE FUNCTION   ************************************************************
    /*******************************************************************************************************************************************/
    public void GoToEvadePoint()
    {
        stats["beenStillTooLong"] = false;
        stillTooLongTimer = howLongToBeStillFor;

        FollowPathToRandomPoint(1f); //Move away from enemy.

        //If tank has gone outside of shooting range from the enemy, move back to them.
        if (enemyTank != null) 
        {
            if (Vector3.Distance(transform.position, enemyTank.transform.position) > shootingRange)
            {
                FollowPathToPointInWorld(enemyTank, 1f);
            }
        }
    }

    /*********************************************************************************************************************************************     
    ************************************************************   RETREAT FUNCTION   ************************************************************
    /*********************************************************************************************************************************************/
    public void GoToRetreatPoint()
    {
        //If the tank isn't currently further away from where the enemy tank was found last or is still being attacked, generate a new random point to travel to.
        if (enemyTank != null) 
        {
            if ((Vector3.Distance(transform.position, enemyTank.transform.position) < tooFarRange) || (stats["beingAttacked"] == true))
            {
                FindAnotherRandomPoint();
                t = 0;
            }
        }

        FollowPathToRandomPoint(1f); //Go to generated random point.
    }

    /********************************************************************************************************************************************     
    ************************************************************   WANDER FUNCTION   ************************************************************
    /********************************************************************************************************************************************/
    public void GoToRandomPoint() 
    {
        //Go to random point, and if time has passed, generate a new random path to go to.
        FollowPathToRandomPoint(1f); 

        t += Time.deltaTime;
        if (t > wanderTime)
        {
            FindAnotherRandomPoint();
            t = 0;
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////   BT ACTION DEFINITIONS   ///////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///Define the functions called for each BT action.

    public BTNodeStates CheckSearching()
    {
        if (stats["searching"] == true)
        {
            return BTNodeStates.SUCCESS;
        }
        return BTNodeStates.FAILURE;
    }

    public BTNodeStates CheckAttacking()
    {
        if (stats["attacking"] == true)
        {
            return BTNodeStates.SUCCESS;
        }
        else
        {
            return BTNodeStates.FAILURE;
        }
    }

    public BTNodeStates CheckEvading()
    {
        if (stats["evading"] == true)
        {
            return BTNodeStates.SUCCESS;
        }
        else
        {
            return BTNodeStates.FAILURE;
        }
    }

    public BTNodeStates CheckRetreating()
    {
        if (stats["retreating"] == true)
        {
            return BTNodeStates.SUCCESS;
        }
        else
        {
            return BTNodeStates.FAILURE;
        }
    }

    public BTNodeStates CheckHealthLowTrue()
    {
        if (stats["healthLow"] == true)
        {
            return BTNodeStates.SUCCESS;
        }
        else
        {
            return BTNodeStates.FAILURE;
        }
    }

    public BTNodeStates CheckHealthLowFalse()
    {
        if (stats["healthLow"] == false)
        {
            return BTNodeStates.SUCCESS;
        }
        else
        {
            return BTNodeStates.FAILURE;
        }
    }

    public BTNodeStates CheckAmmoLowTrue()
    {
        if (stats["ammoLow"] == true)
        {
            return BTNodeStates.SUCCESS;
        }
        else
        {
            return BTNodeStates.FAILURE;
        }
    }

    public BTNodeStates CheckAmmoLowFalse()
    {
        if (stats["ammoLow"] == false)
        {
            return BTNodeStates.SUCCESS;
        }
        else
        {
            return BTNodeStates.FAILURE;
        }
    }

    public BTNodeStates CheckFuelLowTrue()
    {
        if (stats["fuelLow"] == true)
        {
            return BTNodeStates.SUCCESS;
        }
        else
        {
            return BTNodeStates.FAILURE;
        }
    }

    public BTNodeStates CheckFuelLowFalse()
    {
        if (stats["fuelLow"] == false)
        {
            return BTNodeStates.SUCCESS;
        }
        else
        {
            return BTNodeStates.FAILURE;
        }
    }

    public BTNodeStates CheckHealthOrAmmoLow()
    {
        if (stats["healthOrAmmoLow"] == true)
        {
            return BTNodeStates.SUCCESS;
        }
        else
        {
            return BTNodeStates.FAILURE;
        }
    }

    public BTNodeStates CheckFineHealthAndAmmo()
    {
        if (stats["fineHealthandAmmo"] == true)
        {
            return BTNodeStates.SUCCESS;
        }
        else
        {
            return BTNodeStates.FAILURE;
        }
    }

    public BTNodeStates CheckResourcesAllFineTrue()
    {
        if (stats["resourcesAllFine"] == true)
        {
            return BTNodeStates.SUCCESS;
        }
        else
        {
            return BTNodeStates.FAILURE;
        }
    }

    public BTNodeStates CheckResourcesAllFineFalse()
    {
        if (stats["resourcesAllFine"] == false)
        {
            return BTNodeStates.SUCCESS;
        }
        else
        {
            return BTNodeStates.FAILURE;
        }
    }

    public BTNodeStates CheckEnemyFound()
    {
        if (stats["enemyFound"] == true)
        {
            return BTNodeStates.SUCCESS;
        }
        else
        {
            return BTNodeStates.FAILURE;
        }
    }

    public BTNodeStates CheckBaseFound()
    {
        if (stats["baseFound"] == true)
        {
            return BTNodeStates.SUCCESS;
        }
        else
        {
            return BTNodeStates.FAILURE;
        }
    }

    public BTNodeStates CheckConsumableFound()
    {
        if (stats["consumableFound"] == true)
        {
            return BTNodeStates.SUCCESS;
        }
        else
        {
            return BTNodeStates.FAILURE;
        }
    }

    public BTNodeStates CheckNoTargetFound()
    {
        if (stats["noTargetFound"] == true)
        {
            return BTNodeStates.SUCCESS;
        }
        else
        {
            return BTNodeStates.FAILURE;
        }
    }

    public BTNodeStates CheckBeingAttackedTrue()
    {
        if (stats["beingAttacked"] == true)
        {
            return BTNodeStates.SUCCESS;
        }
        else
        {
            return BTNodeStates.FAILURE;
        }
    }

    public BTNodeStates CheckBeingAttackedFalse()
    {
        if (stats["beingAttacked"] == false)
        {
            return BTNodeStates.SUCCESS;
        }
        else
        {
            return BTNodeStates.FAILURE;
        }
    }

    public BTNodeStates CheckWithinShootingRangeTrue()
    {
        if (stats["withinShootingRange"] == true)
        {
            return BTNodeStates.SUCCESS;
        }
        else
        {
            return BTNodeStates.FAILURE;
        }
    }

    public BTNodeStates CheckWithinShootingRangeFalse()
    {
        if (stats["withinShootingRange"] == false)
        {
            return BTNodeStates.SUCCESS;
        }
        else
        {
            return BTNodeStates.FAILURE;
        }
    }

    public BTNodeStates CheckNotTooClose()
    {
        if (stats["notTooClose"] == true)
        {
            return BTNodeStates.SUCCESS;
        }
        else
        {
            return BTNodeStates.FAILURE;
        }
    }

    public BTNodeStates CheckTargetIsFarTrue()
    {
        if (stats["targetIsFar"] == true)
        {
            return BTNodeStates.SUCCESS;
        }
        else
        {
            return BTNodeStates.FAILURE;
        }
    }

    public BTNodeStates CheckTargetIsFarFalse()
    {
        if (stats["targetIsFar"] == false)
        {
            return BTNodeStates.SUCCESS;
        }
        else
        {
            return BTNodeStates.FAILURE;
        }
    }

    public BTNodeStates CheckBeenStillTooLongTrue()
    {
        if (stats["beenStillTooLong"] == true)
        {
            return BTNodeStates.SUCCESS;
        }
        else
        {
            return BTNodeStates.FAILURE;
        }
    }

    public BTNodeStates CheckBeenStillTooLongFalse()
    {
        if (stats["beenStillTooLong"] == false)
        {
            return BTNodeStates.SUCCESS;
        }
        else
        {
            return BTNodeStates.FAILURE;
        }
    }

    public BTNodeStates FireAtTank()
    {
        //If there is an enemy to shoot at, do so.
        if (targets.Count > 0 && targets.First().Key != null)
        {
            enemyTank = targets.First().Key;
            if (enemyTank != null)
            {
                Fire(enemyTank);
            }
            return BTNodeStates.SUCCESS;
        }
        else
        {
            return BTNodeStates.FAILURE;
        }
    }

    public BTNodeStates FireAtBase()
    {
        //If there is a base to shoot at, do so.
        if (bases.Count > 0 && bases.First().Key != null)
        {
            enemyBase = bases.First().Key;
            if (enemyBase != null)
            {
                Fire(enemyBase);
            }
            return BTNodeStates.SUCCESS;
        }
        else
        {
            return BTNodeStates.FAILURE;
        }
    }

    public BTNodeStates TravelToEnemy()
    {
        //If the enemy tank is further than in shooting range, move towards it.
        if (targets.Count > 0 && targets.First().Key != null)
        {
            enemyTank = targets.First().Key;
            if (enemyTank != null)
            {
                if (Vector3.Distance(transform.position, enemyTank.transform.position) > shootingRange)
                {
                    FollowPathToPointInWorld(enemyTank, 1f);
                } //It would have been ideal to create a point to go still within shooting range if the tank is too  close.
            }
            return BTNodeStates.SUCCESS;
        }
        else
        {
            return BTNodeStates.FAILURE;
        }
    }

    public BTNodeStates TravelToBase()
    {
        //If the base is further than in shooting range, move towards it.
        if (bases.Count > 0 && bases.First().Key != null)
        {
            enemyBase = bases.First().Key;
            if (enemyBase != null)
            {
                if (Vector3.Distance(transform.position, enemyBase.transform.position) > shootingRange)
                {
                    FollowPathToPointInWorld(enemyBase, 1f);
                }
            }
            return BTNodeStates.SUCCESS;
        }
        else
        {
            return BTNodeStates.FAILURE;
        }
    }

    public BTNodeStates TravelToConsumable()
    {
        //Move towards the consumable.
        if (consumables.Count > 0 && consumables.First().Key != null)
        {
            consumable = consumables.First().Key;
            if (consumable != null)
            {
                FollowPathToPointInWorld(consumable, 1f);
            }
            return BTNodeStates.SUCCESS;
        }
        else
        {
            return BTNodeStates.FAILURE;
        }
    }

    public BTNodeStates TravelToEvadePoint()
    {
        GoToEvadePoint();
        return BTNodeStates.SUCCESS;
    }

    public BTNodeStates TravelToRetreatPoint()
    {
        GoToRetreatPoint();
        return BTNodeStates.SUCCESS;
    }

    public BTNodeStates TravelToRandomPoint()
    {
        GoToRandomPoint();
        return BTNodeStates.SUCCESS;
    }

    public BTNodeStates SwapToAttack()
    {
        stats["swapToAttackState"] = true;
        return BTNodeStates.SUCCESS;
    }

    public BTNodeStates SwapToSearch()
    {
        stats["swapToSearchState"] = true;
        return BTNodeStates.SUCCESS;
    }

    public BTNodeStates SwapToEvade()
    {
        stats["swapToEvadeState"] = true;
        return BTNodeStates.SUCCESS;
    }

    public BTNodeStates SwapToRetreat()
    {
        stats["swapToRetreatState"] = true;
        return BTNodeStates.SUCCESS;
    }

    /***********************************************************************************************************************************************     
    ************************************************************   COLLISION FUNCTION   ************************************************************
    /***********************************************************************************************************************************************/
    public override void AIOnCollisionEnter(Collision collision)
    {
    }
}