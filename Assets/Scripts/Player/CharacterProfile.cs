using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Characters/Character Profile")]
public class CharacterProfile : ScriptableObject
{
    [Header("Identity")]
    public string id; // "Hero", "Clone1"...

    [Header("Body")]
    public GameObject bodyPrefab; // after spawn him as a child!!!

    [Header("Movement")]
    public float walkSpeed = 4f;
    public float baseGravity = 8f;

    [Header("Jump")]
    public float jumpForce = 12f;
    public int maxJumps = 2;
    public float airSpeed = 5f;
    public float gravityDivider = 2f;

    [Header("Permitted States")]
    public List<PlayerStates.State> permittedStates;
}