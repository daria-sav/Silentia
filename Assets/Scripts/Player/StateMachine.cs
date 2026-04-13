using UnityEngine;

/// <summary>
/// Handles transitions between player states.
///
/// Stores the current and previous state, validates whether a target
/// state has a matching permitted ability, exits the current ability,
/// and enters the new one.
/// </summary>
public class StateMachine 
{
    public PlayerStates.State previousState;
    public PlayerStates.State currentState;

    public BaseAbility[] abilitiesArr;

    public bool ChangeState(PlayerStates.State newState)
    {
        if (abilitiesArr == null || abilitiesArr.Length == 0)
        {
            Debug.LogError("StateMachine: abilitiesArr is null/empty");
            return false;
        }

        // check whether this state exists
        bool hasAny = false;

        // first permitted ability for the target state
        BaseAbility target = null;

        foreach (BaseAbility ability in abilitiesArr)
        {
            if (ability.thisAbilityState != newState) continue;

            hasAny = true;

            if (ability.isPermitted && target == null)
                target = ability;
        }

        if (!hasAny)
        {
            Debug.LogError($"StateMachine: No ability found for state {newState}");
            return false;
        }

        if (target == null)
        {
            Debug.LogWarning($"StateMachine: State {newState} exists but ALL abilities are not permitted.");
            return false;
        }

        // exit current state
        foreach (BaseAbility ability in abilitiesArr)
        {
            if (ability.thisAbilityState == currentState)
                ability.ExitAbility();
        }

        previousState = currentState;
        currentState = newState;

        target.EnterAbility();

        Debug.Log("State changed to: " + newState);
        return true;
    }

    // changes state without calling EnterAbility/ExitAbility
    public void ForceChange(PlayerStates.State newState)
    {
        previousState = currentState;
        currentState = newState;
    }
}