using System;

/// <summary>
/// Stores the minimum data required to restore the player's last known spawn state.
///
/// This data is used by the main menu to decide which scene to load,
/// and by scene-level spawn systems to place the player at the correct
/// spawn point with the correct facing direction.
/// </summary>
[System.Serializable]
public class SpawnData
{
    public string sceneName;
    public string spawnPintKey;
    public bool facingRight;

    /// <summary>
    /// Creates spawn data with default values
    /// </summary>
    public SpawnData()
    {
        sceneName = "Tutorial1";
        spawnPintKey = "Start";
        facingRight = true;
    }
}
[Serializable]
public class CheckpointData
{
    public string sceneToLoad;
    public string checkPointKey;
    public bool facingRight;

    public CheckpointData()
    {
        sceneToLoad = "Tutorial1";
        checkPointKey = "Check1";
        facingRight = true;
    }
}