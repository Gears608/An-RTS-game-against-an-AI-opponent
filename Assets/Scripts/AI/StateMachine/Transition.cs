public class Transition
{
    public Condition condition;
    public State targetState;

    public Transition(Condition condition, State targetState)
    {
        this.condition = condition;
        this.targetState = targetState;
    }
    public bool IsTriggered()
    {
        return condition.Test();
    }
}
