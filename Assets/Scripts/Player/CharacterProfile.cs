using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines a playable clone: which body prefab to use
/// and which ability states are allowed.
/// </summary>
[CreateAssetMenu(menuName = "Characters/Character Profile")]
public class CharacterProfile : ScriptableObject
{
    [Header("Identity")]
    public string id; // may be change to enum later

    [Header("Body")]
    public GameObject bodyPrefab; // body prefab to spawn under the persistent player root

    [Header("UI")]
    public Sprite slotIcon;

    [Header("Permitted States")]
    public List<PlayerStates.State> permittedStates;
}