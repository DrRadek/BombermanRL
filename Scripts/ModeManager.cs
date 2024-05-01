using Godot;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public partial class ModeManager : Node
{
    [Export] PackedScene syncScene;

    [Export] bool useGlobalCamera = true;

    [Export] bool resetWhenRlAgentsDie = true;

    [Export] bool resetWhenPlayersDie = false;

    [Export] bool isArenaShrinking = false;

    [Export] bool randomizeArenaShrinking = false;

    [Export] int instanceCount = 1;

    [Export] int rlAgentTypeCount = 0;

    [Export] int rlAgentActionRepeat = 4;

    [Export] float speedUp = 1;

    [Export] string dataFileName = String.Empty;

    [Export] int dataToCollect = 0;

    int dataCollected = 0;

    [Export]
    int modeIndex = 0;

    [Export]
    Godot.Collections.Array<PackedScene> modeScenes = new();

    //[Export] bool saveData = false;
    //[Export] int saveDataCount = 100;

    List<Node> syncNodes = new();

    //StreamWriter dataFile;

    public override void _Ready()
    {
        if (dataToCollect > 0)
        {
            dataFileName = $"{dataFileName}.csv";

            if (!File.Exists(dataFileName))
            {
                using (StreamWriter dataFile = new(dataFileName))
                {
                    dataFile.WriteLine($"vyhral;delka_hry");
                }
            }
            else
            {
                dataCollected = File.ReadLines(dataFileName).Count() - 1;
            }
            // Play the whole game

            
            //dataFile = new($"{dataFileName}.csv");
            resetWhenRlAgentsDie = false;
        }

        if (!useGlobalCamera)
            GetNode("Camera3D").QueueFree();

        for (int i = 0; i < instanceCount; i++)
        {
            GameManager scene = (GameManager)modeScenes[modeIndex].Instantiate();
            scene.Position = new Vector3(i * 20, 0, 0);
            scene.ConnectedRlAgentCount = rlAgentTypeCount;
            scene.ResetWhenRlAgentsDie = resetWhenRlAgentsDie;
            scene.ResetWhenPlayersDie = resetWhenPlayersDie;
            scene.IsArenaShrinking = isArenaShrinking;
            scene.RandomizeArenaShrinking = randomizeArenaShrinking;
            AddChild(scene);

            if (dataToCollect > 0)
            {
                scene.onGameEndCallback += CollectGameEndStats;
                scene.MaxTotalGamesPlayed = (int)Math.Ceiling((double)(dataToCollect - dataCollected) / instanceCount);
            }

        }

        for (int i = 0; i < rlAgentTypeCount; i++)
        {
            Node syncNode = syncScene.Instantiate();
            syncNode.Set("action_repeat", rlAgentActionRepeat);
            syncNode.Set("speed_up", speedUp);
            syncNode.Set("should_connect_to_server", true);
            syncNode.Set("agent_group_name", $"AGENT{i + 1}");
            syncNode.Set("port", $"{11008 + i + 1}");
            AddChild(syncNode);
            syncNodes.Add(syncNode);
        }

        if(rlAgentTypeCount == 0)
        {
            Engine.PhysicsTicksPerSecond = (int)(speedUp * 60);
            Engine.TimeScale = speedUp * 1.0;
        }
    }

    void CollectGameEndStats(GameEndInfo gameEndInfo)
    {
        dataCollected++;

        //GD.Print(gameEndInfo.won);
        //GD.Print(gameEndInfo.gameLength);
        using (StreamWriter dataFile = new(dataFileName, true))
        {
            dataFile.WriteLine($"{gameEndInfo.won};{gameEndInfo.gameLength.ToString("0.00")}");
        }

        if (dataCollected == dataToCollect)
        {
            GD.Print($"{dataCollected} data collected, done");
            GetTree().Quit();
        }
        else
        {
            GD.Print($"{dataCollected} data collected");
        }

    }

    //public override void _PhysicsProcess(double delta)
    //{
    //    foreach(Node node in syncNodes)
    //    {
    //        node.Call("_run_physics_process", delta);
    //    }
    //}

    //public override void _ExitTree()
    //{
    //    dataFile.Close();
    //}
}

public struct GameEndInfo
{
    public int won;
    public double gameLength;
}