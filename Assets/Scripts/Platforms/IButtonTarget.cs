using UnityEngine;

/// <summary>
/// Common interface for level objects that react to a pressed / released signal
/// from a button, switch or pressure plate
/// </summary>
public interface IButtonTarget
{
    void SetPressed(bool pressed);
}