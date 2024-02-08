using Godot;
using System;

public partial class ModeManager : Node
{
    [Export] bool useGlobalCamera = true;

    [Export] bool resetWhenRlAgentsDie = true;

    [Export]
    int modeIndex = 0;

    [Export]
    Godot.Collections.Array<PackedScene> modeScenes = new();

    [Export]
    Godot.Collections.Array<int> instanceCounts = new();

    public override void _Ready()
    {
        if (!useGlobalCamera)
            GetNode("Camera3D").QueueFree();

        for (int i = 0; i < instanceCounts[modeIndex]; i++)
        {
            GameManager scene = (GameManager)modeScenes[modeIndex].Instantiate();
            scene.Position = new Vector3(i * 20, 0, 0);
            AddChild(scene);
            scene.ResetWhenRlAgentsDie = resetWhenRlAgentsDie;
        }
    }
}
