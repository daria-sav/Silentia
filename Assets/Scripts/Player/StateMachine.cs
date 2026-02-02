using UnityEngine;

public class StateMachine 
{
    public PlayerStates.State previousState;
    public PlayerStates.State currentState;

    public BaseAbility[] abilitiesArr;

    //public void ChangeState(PlayerStates.State newState)
    //{
    //    foreach (BaseAbility ability in abilitiesArr)
    //    {
    //        if (ability.thisAbilityState == newState)
    //        {
    //            if (!ability.isPermitted)
    //            {
    //                return;
    //            }
    //        }
    //    }

    //    foreach (BaseAbility ability in abilitiesArr)
    //    {
    //        if (ability.thisAbilityState == currentState)
    //        {
    //            ability.ExitAbility();
    //            previousState = currentState;
    //        }
    //    }

    //    foreach (BaseAbility ability in abilitiesArr)
    //    {
    //        if (ability.thisAbilityState == newState)
    //        {
    //            if (ability.isPermitted)
    //            {
    //                currentState = newState;
    //                ability.EnterAbility();
    //            }
    //            Debug.Log("State changed to: " + newState);
    //            break;
    //        }
    //    }
    //}

    public bool ChangeState(PlayerStates.State newState)
    {
        if (abilitiesArr == null || abilitiesArr.Length == 0)
        {
            Debug.LogError("StateMachine: abilitiesArr is null/empty");
            return false;
        }

        // Is there at least one ability for this state?
        bool hasAny = false;

        // Is there at least one permitted ability for this state?
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

        // Exit current state
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

    public void ForceChange(PlayerStates.State newState)
    {
        previousState = currentState;
        currentState = newState;
    }
}
