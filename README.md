# Drone Game Local

A simple C# game where the player controls a drone, avoids obstacles, and tries to get the best score.

This project is a (For now) local-only MVP version of a Drone Obstacle Navigation Game. It focuses on basic gameplay first: drone movement, obstacles, collision detection, lives, score, pause, win, game over, and local high score.

## Features

- Move the drone with keyboard
- Avoid moving obstacles
- Score points by passing obstacles
- Lives system
- Game Over screen
- Win screen
- Pause system
- Local high score save
- No backend required
- No AWS required
- No login required

## Technologies

- C#
- .NET 8
- MonoGame DesktopGL

## Project Structure

```text
DroneGameLocal/
├── Program.cs
├── Game1.cs
├── DroneGameLocal.csproj
├── src/
│   ├── Drone.cs
│   ├── Obstacle.cs
│   ├── CollisionChecker.cs
│   ├── GameState.cs
│   ├── LocalSaveManager.cs
│   └── PixelText.cs
└── README.md
