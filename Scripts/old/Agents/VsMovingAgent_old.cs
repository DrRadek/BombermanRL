using Godot;
using System;

public partial class VsMovingAgent_old : RlAgent
{
    protected override void OnDeath()
    {
        AddReward(-0.5f);
    }
}
