using Godot;
using System;

public partial class VsMovingAgents : VsStaticAgentPhase2
{
    protected override void OnDeath()
    {
        AddReward(-0.5f);
    }
}
