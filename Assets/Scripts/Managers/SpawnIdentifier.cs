using UnityEngine;

/// <summary>
/// Marks a scene object as a named spawn point that can be resolved
/// by spawn-related systems using a string key
/// </summary>
public class SpawnIdentifier : MonoBehaviour
{
    public string spawnKey;
}
