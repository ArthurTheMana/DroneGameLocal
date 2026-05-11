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

    public void StartGame()
    {
        Current = GameStateType.Playing;
        Lives = 3;
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