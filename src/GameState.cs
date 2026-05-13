namespace DroneGameLocal;

public enum GameStateType
{
    Start,
    Playing,
    Paused,
    Fail,
    Win
}

public sealed class GameState
{
    public GameStateType Current { get; private set; } = GameStateType.Start;
    public int Lives { get; private set; } = 3;

    // LEVEL 3C CHANGE:
    // Starting lives now comes from the selected difficulty.
    // Easy gives more lives, Hard gives fewer lives.
    public void StartGame(int startingLives)
    {
        Current = GameStateType.Playing;
        Lives = startingLives;
    }

    public void Pause()
    {
        if (Current == GameStateType.Playing)
        {
            Current = GameStateType.Paused;
        }
    }

    public void Resume()
    {
        if (Current == GameStateType.Paused)
        {
            Current = GameStateType.Playing;
        }
    }

    public void LoseLife()
    {
        Lives--;

        if (Lives <= 0)
        {
            Current = GameStateType.Fail;
        }
    }

    public void Win()
    {
        Current = GameStateType.Win;
    }

    public bool IsGameOver()
    {
        return Current == GameStateType.Fail ||
               Current == GameStateType.Win;
    }
}