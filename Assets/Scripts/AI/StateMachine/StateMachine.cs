using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateMachine : MonoBehaviour
{
    public string state;
    State currentState;

    private void Start()
    {
        currentState = GetInitialState();
        if(currentState != null)
        {
            currentState.EnterState();
        }
    }

    private void Update()
    {
        //check if a transition has been triggered
        foreach(Transition transition in currentState.transitions)
        {
            //if a transition has been triggered
            if (transition.IsTriggered())
            {
                //exit current state
                currentState.ExitState();
                //set new state
                currentState = transition.targetState;
                //enter new state
                currentState.EnterState();
            }
        }

        if(currentState != null)
        {
            currentState.UpdateLogic();
            state = currentState.stateName;
        }
    }

    private void FixedUpdate()
    {
        if(currentState != null)
        {
            currentState.UpdatePhysics();
        }
    }

    public virtual State GetInitialState()
    {
        return null;
    }
}
