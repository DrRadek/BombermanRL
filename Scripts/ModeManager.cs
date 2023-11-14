using Godot;
using System;

public partial class ModeManager : Node
{
    [Export]
    int modeIndex = 0;

    [Export]
    Godot.Collections.Array<PackedScene> modeScenes = new();

    [Export]
    Godot.Collections.Array<int> instanceCounts = new();

    public override void _Ready()
	{
        for(int i = 0; i < instanceCounts[modeIndex]; i++)
        {
            Node3D scene = (Node3D)modeScenes[modeIndex].Instantiate();
            scene.Position = new Vector3(i * 20, 0, 0);
            AddChild(scene);
        }
    }
}
