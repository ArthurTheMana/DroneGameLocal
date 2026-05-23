namespace DroneGameLocal;

// ML-1 CHANGE:
// This label is used for supervised machine learning.
// During gameplay, the player can label the current game state as:
// TooEasy, Balanced, or TooHard.
public enum GameBalanceLabel
{
    TooEasy,
    Balanced,
    TooHard
}