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
        if(strength > 0.3f)
            AddReward(-0.01f * strength);
    }
    public override void OnNormalTileTouched()
    {
        if(TimeWithoutUsingBomb <= 5)
        {
            AddReward(0.01f);
        }
        else if (TimeWithoutUsingBomb > 15)
        {
            gameManager.ForceEndGame();
        }
        else if (TimeWithoutUsingBomb > 10)
        {
            AddReward(-0.01f);
        }


    }
}
