using Godot;
using System;

public partial class ModeManager : Node
{
    [Export]
    int modeIndex = 0;

    [Export]
    Godot.Collections.Array<PackedScene> modeScenes = new();
    
	public override void _Ready()
	{
        var scene = modeScenes[modeIndex].Instantiate();
        AddChild(scene);
    }

}
