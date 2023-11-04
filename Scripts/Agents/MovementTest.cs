using Godot;
using System;

public partial class MovementTest : Character
{
    int collected = 0;

    double timeSinceLastPickup = 0;

    public override void _PhysicsProcess(double delta)
    {
        
        timeSinceLastPickup += delta;
        base._PhysicsProcess(delta);
        
        if (timeSinceLastPickup >= 5)
        {
            //AddReward(-1);
            gameManager.ForceEndGame();
        }
        //else if (timeSinceLastPickup >= 2f)
        //{
        //    AddReward(-0.001f * (float)timeSinceLastPickup); // time reward
        //}
    }

    protected override void OnDefaultValuesSet()
    {
        
        //DefaultMaxSpawnedBombs = 1;
        //DefaultBombStrength = 10;
    }
    protected override void OnTriedToRunIntoObject()
    {
        //GD.Print("TRUE");
        //AddReward(-1f);
    }

    public override void Spawn(Vector3 pos)
    {
        base.Spawn(pos);
        timeSinceLastPickup = 0;
    }

    protected override void OnCollectibleCollected(int index)
    {
        collected++;
        timeSinceLastPickup = 0;

        AddReward(1);
        gameManager.OnCollectibleCollected(Position);
        if (collected == 30)
        {
            //AddReward(100);
            collected = 0;
            //Despawn();
            gameManager.ForceEndGame();
        }
    }
}
