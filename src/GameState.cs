namespace DroneGameLocal;

public enum GameStateType
{
    Start,
    Playing,
    Fail,
    Win
}

public sealed class GameState
{
    public GameStateType Current { get; set; } = GameStateType.Start;
    public int Score { get; set; } = 0;
    public int Lives { get; set; } = 3;

    public void StartGame()
    {
        Current = GameStateType.Playing;
        Score = 0;
        Lives = 3;
    }
}