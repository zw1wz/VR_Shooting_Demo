using System.Collections.Generic;

public enum GameModeId
{
    QuickReactionTraining,
    FreeShoot,
}

public class GameMode
{
    public GameModeId id;
    public string displayName;
    public string description;
    public int bulletCount;
    public int targetCount;
    public bool useBeepCountdown;
    public float minBeepDelay;
    public float maxBeepDelay;
}

public static class GameModes
{
    public static readonly List<GameMode> All = new List<GameMode>
    {
        new GameMode
        {
            id = GameModeId.QuickReactionTraining,
            displayName = "快速反应训练",
            description = "等待信号，完成五发射击",
            bulletCount = 5,
            targetCount = 5,
            useBeepCountdown = true,
            minBeepDelay = 1f,
            maxBeepDelay = 3f,
        },
        new GameMode
        {
            id = GameModeId.FreeShoot,
            displayName = "自由射击",
            description = "不限弹数，自由射击靶场",
            bulletCount = -1,
            targetCount = 5,
            useBeepCountdown = false,
        },
    };

    public static GameMode GetById(GameModeId id)
    {
        return All.Find(m => m.id == id);
    }
}
