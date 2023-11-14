using Godot;
using System;

public partial class VsStaticAgent : Character
{
    protected override void OnDefaultValuesSet()
    {
        DefaultMaxSpawnedBombs = 1;
    }
    protected override void OnBombPlaced(float rating)
    {
        AddReward(rating * 0.1f);
    }
    protected override void OnBombFailedToPlace()
    {
        AddReward(-0.01f);
    }
    public override void OnTeamHit()
    {
        AddReward(-2);
    }
    public override void OnEnemyTeamHit()
    {
        AddReward(1);
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
