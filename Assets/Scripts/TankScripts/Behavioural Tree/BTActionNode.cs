public class BTAction : BTBaseNode
{
    //Stores the function sigature for the action.
    public delegate BTNodeStates ActionNodeFunction();

    //Stores the action node function for evaluation.
    private ActionNodeFunction btAction;

    //The function is passed in and stored upon creating the action node.
    public BTAction(ActionNodeFunction btAction)
    {
        this.btAction = btAction;
    }

    // Evaluates if the action node has failed or not.
    public override BTNodeStates Evaluate()
    {
        switch (btAction())
        {
            case BTNodeStates.SUCCESS:
                btNodeState = BTNodeStates.SUCCESS;
                return btNodeState;
            case BTNodeStates.FAILURE:
                btNodeState = BTNodeStates.FAILURE;
                return btNodeState;
            default:
                btNodeState = BTNodeStates.FAILURE;
                return btNodeState;
        }
    }
}