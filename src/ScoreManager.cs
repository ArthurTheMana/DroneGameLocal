namespace DroneGameLocal;

public sealed class ScoreManager
{
    public int Score { get; private set; }
    public int HighScore { get; private set; }

    public ScoreManager()
    {
        HighScore = LocalSaveManager.LoadHighScore();
    }

    public void ResetScore()
    {
        Score = 0;
    }

    public void AddScore(int points)
    {
        if (points <= 0)
        {
            return;
        }

        Score += points;
    }

    public void SaveHighScoreIfNeeded()
    {
        if (Score <= HighScore)
        {
            return;
        }

        HighScore = Score;
        LocalSaveManager.SaveHighScore(HighScore);
    }
}