using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class State
{
    public string stateName;
    public List<Transition> transitions;
    protected StateMachine stateMachine;

    public State(string stateName, StateMachine stateMachine)
    {
        this.stateName = stateName;
        this.stateMachine = stateMachine;
        transitions = new List<Transition>();
    }

    public virtual void EnterState() { }

    public virtual void UpdateLogic() { }

    public virtual void UpdatePhysics() { }

    public virtual void ExitState() { }
}
    
