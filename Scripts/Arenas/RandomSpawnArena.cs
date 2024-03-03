using Godot;
using System;

public partial class RandomSpawnArena : GameManager
{
    //protected override void StartGame()
    //{
    //    StartGame(() => RandomSpawn());
    //}

    protected override void GameMode()
    {
        RandomSpawn();
    }

    void RandomSpawn()
    {
        AdddestructibleWalls();
        AddIndestructibleWalls();
        SpawnRandom();
    }
}
