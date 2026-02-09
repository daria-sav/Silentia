[System.Serializable]
public class ExampleData
{
    public int exampleInt;
    public string exampleString;
}

[System.Serializable]
public class SpawnData
{
    public string sceneName;
    public string spawnPintKey;
    public bool facingRight;

    public SpawnData()
    {
        sceneName = "Level1";
        spawnPintKey = "Start";
        facingRight = true;
    }
}