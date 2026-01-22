using UnityEngine;

public class StateMachine 
{
    public PlayerStates.State previousState;
    public PlayerStates.State currentState;

    public BaseAbility[] abilitiesArr;

    public void ChangeState(PlayerStates.State newState)
    {
        foreach (BaseAbility ability in abilitiesArr)
        {
            if (ability.thisAbilityState == newState)
            {
                if (!ability.isPermitted)
                {
                    return;
                }
            }
        }

        foreach (BaseAbility ability in abilitiesArr)
        {
            if (ability.thisAbilityState == currentState)
            {
                ability.ExitAbility();
                previousState = currentState;
            }
        }

        foreach (BaseAbility ability in abilitiesArr)
        {
            if (ability.thisAbilityState == newState)
            {
                if (ability.isPermitted)
                {
                    currentState = newState;
                    ability.EnterAbility();
                }
                Debug.Log("State changed to: " + newState);
                break;
            }
        }
    }

    public void ForceChange(PlayerStates.State newState)
    {
        previousState = currentState;
        currentState = newState;
    }
}
