using Godot;
using System;

public partial class BombDodgeAgent_old : Character
{
    protected override void OnDefaultValuesSet()
    {

        //DefaultMaxSpawnedBombs = 0;
    }

    protected override void OnBombPlaced(float rating)
    {
        AddReward(0.1f);
    }
    public override void OnTeamHit()
    {
        AddReward(-2);
    }
    public override void OnEnemyTeamHit()
    {
        AddReward(10);
    }

    public override void OnTeamWin()
    {
        AddReward(5);
    }

    public override void OnDangerousTileTouched(float strength)
    {
        //AddReward(-0.01f * strength);
    }
    public override void OnNormalTileTouched()
    {
        AddReward(0.03f);
    }
}
