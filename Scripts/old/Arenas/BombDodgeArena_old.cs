using Godot;
using System;

public partial class BombDodgeArena_old : GameManager
{
    double bombSpawnDelay = 0.5;
    double bombDelta;

    protected override void GameMode()
    {
        bombDelta = bombSpawnDelay;
        BombDodgeMode();
    }

    void BombDodgeMode()
    {
        //AddIndestructibleWalls();
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        //bombDelta -= delta;
        //if(bombDelta <= 0) {
        //    bombDelta = bombSpawnDelay;
        //    PlaceBomb(GetRandomEmptyInnerCell(), 0);
        //}
    }
}
