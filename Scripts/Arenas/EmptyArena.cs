using Godot;
using System;

public partial class EmptyArena : GameManager
{
    protected override void StartGame()
    {
        StartGame(() => NoMode());
    }

    void NoMode()
    {

    }

    //public override void OnPlayerDeath(int playerIndex)
    //{
    //    if (playerIndex == players[0].playerIndex)
    //    {
    //        ForceEndGame();
    //    }
    //    else
    //    {
    //        base.OnPlayerDeath(playerIndex);
    //    }
        
    //}
}
