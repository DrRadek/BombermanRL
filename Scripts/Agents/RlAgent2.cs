using Godot;
using System;

public partial class RlAgent2 : Character
{
    public override void OnTeamWin()
    {
        AddReward(10);
    }
    protected override void OnDeath()
    {
        AddReward(-5);
    }
}
