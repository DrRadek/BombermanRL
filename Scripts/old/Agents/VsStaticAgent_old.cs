using Godot;
using System;

public partial class VsStaticAgent_old : Character
{
    //protected override void OnDefaultValuesSet()
    //{
    //    DefaultMaxSpawnedBombs = 1;
    //}
    //protected override void OnBombPlaced(float rating)
    //{
    //    AddReward(rating * 0.1f);
    //}
    //protected override void OnBombFailedToPlace()
    //{
    //    AddReward(-0.01f);
    //}
    //public override void OnTeamHit()
    //{
    //    AddReward(-2);
    //}
    //public override void OnEnemyTeamHit()
    //{
    //    AddReward(1);
    //}
    //public override void OnTeamWin()
    //{
    //    AddReward(5);
    //}
    //public override void OnDangerousTileTouched(float strength)
    //{
    //    if (strength > 0.3f)
    //        AddReward(-0.01f * strength);
    //}
    //public override void OnNormalTileTouched()
    //{
    //    if (TimeWithoutUsingBomb <= 5)
    //    {
    //        AddReward(0.0002f);
    //    }
    //    else if (TimeWithoutUsingBomb > 15)
    //    {
    //        gameManager.ForceEndGame();
    //    }
    //    else if (TimeWithoutUsingBomb > 10)
    //    {
    //        AddReward(-0.01f);
    //    }
    //}
    //protected override void OnDeath()
    //{
    //    AddReward(-1);
    //    gameManager.ForceEndGame();
    //}

    protected override void OnDefaultValuesSet()
    {
        DefaultMaxSpawnedBombs = 1;
        DefaultMaxLives = 9;
    }
    protected override void OnBombPlaced(float rating)
    {
        if(rating > 0)
            AddReward(0.001f * rating);
    }
    protected override void OnBombFailedToPlace()
    {
        
    }
    public override void OnTeamHit()
    {
        AddReward(-0.05f);
    }
    public override void OnEnemyTeamHit()
    {
        AddReward(2);
    }
    public override void OnTeamWin()
    {
        AddReward(3);
    }
    public override void OnDangerousTileTouched(float strength)
    {
        //AddReward(-0.0001f * strength);
    }
    public override void OnNormalTileTouched()
    {
        if (TimeWithoutUsingBomb > 30) // 15
        {
            AddReward(-1);
            gameManager.RestartGame();
        }
    }
    public override void OnGetsCloserToEnemy()
    {
        //AddReward(0.0001f);
    }

    protected override void OnDeath()
    {
        AddReward(-0.5f);
        gameManager.RestartGame();
    }
}
