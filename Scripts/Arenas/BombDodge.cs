using Godot;
using System;

public partial class BombDodge : GameManager
{
    double bombSpawnDelay = 0.5;
    double bombDelta;

    protected override void StartGame()
    {
        bombDelta = bombSpawnDelay;
        StartGame(() => BombDodgeMode());
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
