using Godot;
using System;

public partial class VsTest : Character
{
    //public override void OnEnemyTeamHit()
    //{
    //    AddReward(2);
    //}

    public override void OnTeamHit()
    {
        AddReward(-0.0003f);
    }

    protected override void OnBombPlaced(float rating)
    {
        AddReward(0.0003f);
    }

    //protected override void OnBombFailedToPlace()
    //{
    //    AddReward(-0.001f);
    //}

    //public override void OnWallDestroyed()
    //{
    //    AddReward(0.1f);
    //}
    public override void OnEnemyDeath()
    {
        AddReward(1);
    }
    public override void OnTeamWin()
    {
        AddReward(2);
    }

    //public override void OnDangerousTileTouched(float strength)
    //{
    //    AddReward(-0.01f * strength);
    //}
}
