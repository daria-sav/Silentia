[System.Serializable]
public class ExampleData
{
    public int exampleInt;
    public string exampleString;
}

[System.Serializable]
public class SpawnData
{
    public string spawnPintKey;
    public bool facingRight;

    public SpawnData()
    {
        spawnPintKey = "Start";
        facingRight = true;
    }
}