using Godot;
using System;

public partial class VsStaticAgentPhase2 : Character
{
    protected override void OnDefaultValuesSet()
    {
        DefaultMaxSpawnedBombs = 2;
        DefaultMaxLives = 3;
    }
    protected override void OnBombPlaced(float rating)
    {
        if(rating > 0)
            AddReward(0.001f * rating);
    }
    public override void OnTeamHit()
    {
        AddReward(-0.5f);
    }
    public override void OnEnemyTeamHit()
    {
        AddReward(2);
    }
    public override void OnTeamWin()
    {
        AddReward(3);
    }
    public override void OnNormalTileTouched()
    {
        if (TimeWithoutUsingBomb > 30)
        {
            AddReward(-1);
            gameManager.ForceEndGame();
        }
    }

    public override void OnDangerousTileTouched(float strength)
    {
        if(strength > 0.5f)
            AddReward(-strength * 0.0001f);
    }

    protected override void OnDeath()
    {
        AddReward(-0.5f);
        gameManager.ForceEndGame();
    }
}
