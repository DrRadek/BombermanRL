using Godot;
using System;

public partial class VsStaticAgentPhase2 : Character
{
    protected override void OnDefaultValuesSet()
    {
        //DefaultMaxSpawnedBombs = 2;
        //DefaultMaxLives = 3;
    }
    protected override void OnBombPlaced(float rating)
    {
        if (rating > 0)
            AddReward(0.001f * rating);
    }
    public override void OnTeamHit()
    {
        AddReward(-1f);
    }
    public override void OnEnemyTeamHit()
    {
        AddReward(2);
    }
    public override void OnTeamWin()
    {
        AddReward(3);
    }
    //public override void OnNormalTileTouched()
    //{
    //    if (TimeWithoutUsingBomb > MaxTimeWithoutUsingBomb)
    //    {
    //        HandleFireHit(true);
    //        AddReward(-1);
    //        PlaceBomb(); // Bomb won't be placed due to a player taking a hit, but will reset the timer,
    //                     // preventing a player from taking multiple hits
    //    }
    //    //else if(TimeWithoutUsingBomb > 15)
    //    //{
    //    //    AddReward(-(float)((TimeWithoutUsingBomb - 15) / (MaxTimeWithoutUsingBomb - 15)) * 0.0001f);
    //    //}
    //}
    protected override void OnForgotToUseBomb()
    {
        AddReward(-1);
    }
    public override void OnWallDestroyed()
    {
        AddReward(0.01f);
    }

    public override void OnDangerousTileTouched(float strength)
    {
        if (strength > 0.5f)
            AddReward(-strength * 0.0001f);
    }

    protected override void OnDeath()
    {
        AddReward(-0.5f);
        gameManager.RestartGame();
    }
}
