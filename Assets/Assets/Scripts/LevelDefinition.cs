[System.Serializable]
public struct LevelGoal
{
    public int targetLevel;
    public int targetCount;
    public bool isMoveLimit;
}

[System.Serializable]
public struct LevelDefinition
{
    public LevelGoal[] goals;
    public bool fillFieldWithAllLevels;
    public int moveLimit;
    public float[] spawnWeights;
}
