using Godot;
using System;

public partial class MapNodeReference : Node
{
    [Export] Node gameInfoControlNode;
    public Label playerLabel;
    public Label gameInfoLabel;

    public override void _Ready()
    {
        playerLabel = (Label)gameInfoControlNode.GetNode("player_info");
        gameInfoLabel = (Label)gameInfoControlNode.GetNode("win_info");
    }
}
