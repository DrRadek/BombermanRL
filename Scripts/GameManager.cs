using Godot;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading.Tasks;
// TODO: Convert other arenas to the new map node
public partial class GameManager : Node3D
{
    GridMap gridmap;

    //[Export]
    //int playerCount = 4;

    [Export]
    public Godot.Collections.Array<Character> players = new();

    public int maxPlayerCount = 4;

    bool waitForServer = true;
    bool playerIsOutOfBounds = false;
    bool resetWhenRLAgentsDie = false;
    bool gameHasRlAgents = false;

    const int playerMapObservationsSize = 11;
    const int playerMapObservationsOffset = playerMapObservationsSize / 2;
    const int enemyMapObservationsSize = 3;
    const int enemyMapObservationsOffset = enemyMapObservationsSize / 2;
    public const int arenaSize = 17;
    const int arenaOffset = (arenaSize / 2);
    const int cornerOffset = arenaOffset - 1;

    public const float bombDetonationSpeed = 2.5f * 0.5f;
    const float fireDuration = 1.5f * 0.5f;
    float[] bombValues = new float[arenaSize * arenaSize];

    // debug info
    bool visualizeObs = false;
    public bool VisualizeObs { get => visualizeObs; }
    public bool ResetWhenRlAgentsDie { set => resetWhenRLAgentsDie = value; }

    int playerIndexToVisualize = 1;
    List<Node3D> obsNodes = new();
    List<List<StandardMaterial3D>> obsNodeMaterials = new();
    double printSpeed = 0.3f;
    double printDelta = 0;

    int alivePlayerCount;
    Vector3I[] lastPlayerPositions;

    Random random = new Random();

    Dictionary<Vector3I, Bomb> bombs = new();
    Dictionary<Vector3I, Fire> fires = new();

    List<Godot.Collections.Array<int>> playerMapObservations = new();
    List<Godot.Collections.Array<float>> playerBombObservations = new();

    public Godot.Collections.Array<int> mapSensor = new();
    public Godot.Collections.Array<int> playerMapSensor = new();

    Vector3I[] spawnPositions = new Vector3I[]{
        new Vector3I(-cornerOffset, 0, -cornerOffset),
        new Vector3I(-cornerOffset, 0, cornerOffset),
        new Vector3I(cornerOffset, 0, -cornerOffset),
        new Vector3I(cornerOffset, 0, cornerOffset),
    };

    public static Vector3I[] directions = {
        new Vector3I(1,0,0),
        new Vector3I(-1,0,0),
        new Vector3I(0,0,1),
        new Vector3I(0, 0, -1)
    };

    //Dictionary<int, float> gridObsValues = new() {
    //    {(int)GridMap.InvalidCellItem, 0 },
    //    {GridIndexes.collectible1, 0.6f },
    //    {GridIndexes.indestructibleWall, 0.25f },
    //    {GridIndexes.destructibleWall, 0.5f },
    //    {GridIndexes.bomb, -0.5f},
    //    {GridIndexes.fire, -1f},
    //    {GridIndexes.friendlyPlayer, 0.75f},
    //    {GridIndexes.enemyPlayer, 1f},

    //};

    Dictionary<int, int> gridObsValues = new() {
        {GridIndexes.empty, 0 },
        {GridIndexes.indestructibleWall, 1 },
        {GridIndexes.destructibleWall, 2 },
        {GridIndexes.friendlyPlayer, 3},
        {GridIndexes.enemyPlayer, 4},

        {GridIndexes.bomb, 0},
        {GridIndexes.fire, 0},
        {GridIndexes.collectible1, 0},
    };

    public class GridIndexes
    {
        public const int empty = -1;
        public const int indestructibleWall = 0;
        public const int destructibleWall = 1;
        public const int bomb = 2;
        public const int fire = 3;
        public const int collectible1 = 4;
        public const int friendlyPlayer = 5;
        public const int enemyPlayer = 6;
    }

    bool IsInnerCell(Vector3I pos)
    {
        return pos.X >= (-arenaOffset + 1) && pos.Z >= (-arenaOffset + 1) && pos.X < (-arenaOffset - 1 + arenaSize) && pos.Z < (-arenaOffset - 1 + arenaSize);
    }

    HashSet<Vector3I> cellDistances = new();
    Queue<KeyValuePair<int,Vector3I>> currentCellsOld = new();
    Queue<KeyValuePair<int, Vector3I>> currentCellsNew = new();
    public Vector3I? FindDirectionToNearestSafeCell(Vector3I startPos, int playerIndex, out Vector3I? nearestSafeCell, float lowestDetonationTime = bombDetonationSpeed * 0.2f)
    {

        cellDistances.Clear();
        currentCellsOld.Clear();
        currentCellsNew.Clear();

        currentCellsOld.Enqueue(new (-1, startPos));

        int distance = 0;
        while (currentCellsOld.Count > 0)
        {
            distance++;
            while (currentCellsOld.Count > 0)
            {
                KeyValuePair<int, Vector3I> pair = currentCellsOld.Dequeue();
                var pos = pair.Value;
                var moveDirection = pair.Key;
                int moveDirectionIndex = -1;
                bool setDirections = false;

                if (moveDirection == -1)
                {
                    setDirections = true;
                }

                foreach (Vector3I direction in directions)
                {
                    moveDirectionIndex++;
                    Vector3I newPos = pos + direction;

                    if (cellDistances.Contains(newPos) || !IsInnerCell(newPos))
                    {
                        continue;
                    }

                    Vector3I newPosRelative = newPos - startPos;
                    if (Math.Abs(newPosRelative.X) > playerMapObservationsOffset || Math.Abs(newPosRelative.Z) > playerMapObservationsOffset)
                        continue;
                    //newPosRelative += new Vector3I(playerMapObservationsOffset, 0, playerMapObservationsOffset);
                    //int index = newPosRelative.X + newPosRelative.Z * playerMapObservationsSize;
                    int index = GetGridIndex(newPos);
                    //int teamID = playerMapSensor[index];
                    //if (teamID != 0)
                    //{
                    //    if (teamID == sourcePlayerTeamID)
                    //        mapObservations[obsIndex] = gridObsValues[GridIndexes.friendlyPlayer];
                    //    else
                    //        mapObservations[obsIndex] = gridObsValues[GridIndexes.enemyPlayer];
                    //}


                    int item = GetObjectInCell(newPos);
                    int gridTeamID = GridIndexes.empty;
                    try
                    {
                        //GD.Print($"{playerIndex}, {index}");
                        gridTeamID = playerMapSensor[index]; //playerMapObservations[playerIndex][index];
                    }
                    catch (Exception)
                    {
                        GD.Print($"index {index} out of range, relative pos: {newPosRelative}, index: {index} ({nameof(FindDirectionToNearestSafeCell)})");
                        continue;
                    }

                    if (item == GridIndexes.destructibleWall || item == GridIndexes.indestructibleWall || item == GridIndexes.fire || gridTeamID != 0) // || gridTeamID == gridObsValues[GridIndexes.enemyPlayer] || gridTeamID == gridObsValues[GridIndexes.friendlyPlayer])
                    {
                        continue;
                    }

                    var bombValue = GetBombValue(index);
                    //if (bombValue > 0 && bombValue <= lowestDetonationTime && distance <= 2)
                    //    continue;

                    if (setDirections)
                    {
                        moveDirection = moveDirectionIndex;
                    }

                    //if (playerBombObservations[playerIndex][index] > 0)
                    if(bombValue > 0)
                    {
                        currentCellsNew.Enqueue(new (moveDirection ,newPos));
                        cellDistances.Add(newPos);
                    }
                    else
                    {
                        nearestSafeCell = newPos;
                        return startPos + directions[moveDirection];
                        //return newPos;
                    }
                }
            }

            (currentCellsOld, currentCellsNew) = (currentCellsNew, currentCellsOld);
        }
        nearestSafeCell = null;
        return null;
    }
    public float GetBombValue(int index)
    {
        return bombValues[index];
    }

    public List<int> GetThingsInBombRadius(Vector3I pos, int bombStrength, int playerTeamID)
    {
        List<int> things = new();

        foreach (Vector3I direction in directions)
        {
            for (int i = 1; i <= bombStrength; i++)
            {
                Vector3I newPos = pos + direction * i;
                int item = GetObjectInCell(newPos);
                if (item == GridIndexes.bomb || item == GridIndexes.destructibleWall)
                {
                    things.Add(item);
                    break;
                }
                else if (item != GridIndexes.empty && item != GridIndexes.fire)
                {
                    break;
                }

                int teamID = playerMapSensor[GetGridIndex(newPos)];
                if (teamID != 0)
                {
                    if (teamID != playerTeamID)
                        things.Add(GridIndexes.enemyPlayer);
                    else
                        things.Add(GridIndexes.friendlyPlayer);
                }
            }
        }
        return things;
    }
    public Godot.Collections.Dictionary<string, Variant> GetObservationsAroundPlayer(int sourcePlayerIndex, int targetPlayerIndex)
    {
        Godot.Collections.Array<int> mapObservations; //= new();
        Godot.Collections.Array<float> bombObservations; // = new();
        Godot.Collections.Dictionary<string, Variant> dict = new();

        bool isEnemy = sourcePlayerIndex != targetPlayerIndex;

        int obsOffset;
        if (isEnemy)
        {
            obsOffset = enemyMapObservationsOffset;
            mapObservations = new();
            bombObservations = new();
            mapObservations.Resize(enemyMapObservationsSize * enemyMapObservationsSize);
            bombObservations.Resize(enemyMapObservationsSize * enemyMapObservationsSize);
        }
        else
        {
            obsOffset = playerMapObservationsOffset;
            mapObservations = playerMapObservations[sourcePlayerIndex];
            bombObservations = playerBombObservations[sourcePlayerIndex];
        }

        dict["obs_around_player"] = mapObservations;
        dict["obs_around_player_bomb"] = bombObservations;


        var sourcePlayer = players[sourcePlayerIndex];
        if (sourcePlayer.IsDead || targetPlayerIndex >= players.Count)
        {
            mapObservations.Fill(0);
            bombObservations.Fill(0);
            return dict;
        }

        var sourcePlayerTeamID = sourcePlayer.TeamID;
        var targetPlayer = players[targetPlayerIndex];
        if (targetPlayer.IsDead)
        {
            mapObservations.Fill(0);
            bombObservations.Fill(0);
            return dict;
        }

        try
        {
            Vector3I pos = GetGridPosition(targetPlayer.Position);

            int obsIndex = 0;

            for (int y = -obsOffset; y <= obsOffset; y++)
            {
                for (int x = -obsOffset; x <= obsOffset; x++)
                {
                    Vector3I gridPos = pos + new Vector3I(x, 0, y);

                    int index = GetGridIndex(gridPos);

                    if (IsInnerCell(gridPos))
                    {
                        int obj = GetObjectInCell(gridPos);
                        float bombValue = bombValues[index];
                        if (bombValue != 0)
                        {
                            bombObservations[obsIndex] = 0.5f * ((bombDetonationSpeed - bombValues[index]) / bombDetonationSpeed);
                        }
                        else
                        {
                            bombObservations[obsIndex] = obj == GridIndexes.fire ? 1 : 0;
                        }

                        int teamID = playerMapSensor[index];
                        if (teamID != 0)
                        {
                            if (teamID == sourcePlayerTeamID)
                                mapObservations[obsIndex] = gridObsValues[GridIndexes.friendlyPlayer];
                            else
                                mapObservations[obsIndex] = gridObsValues[GridIndexes.enemyPlayer];
                        }
                        else
                        {
                            mapObservations[obsIndex] = mapSensor[index];
                        }

                    }
                    else
                    {
                        mapObservations[obsIndex] = gridObsValues[GridIndexes.indestructibleWall];
                        bombObservations[obsIndex] = 0;
                    }
                    obsIndex++;
                }


            }

            if (!isEnemy)
            {
                var strength = bombObservations[mapObservations.Count / 2];
                if (strength > 0)
                {
                    targetPlayer.OnDangerousTileTouched(strength);
                }
                else
                {
                    targetPlayer.CheckTimeWithoutUsingBomb();
                    targetPlayer.OnNormalTileTouched();
                }
            }


            //if (printObs)
            //{
            //    String msg = "";
            //    obsIndex = 0;
            //    for (int y = -obsOffset; y <= obsOffset; y++)
            //    {
            //        String msg1 = "";
            //        for (int x = -obsOffset; x <= obsOffset; x++)
            //        {
            //            msg1 += playerMapObservations[obsIndex] + " ";
            //            obsIndex++;
            //        }
            //        msg += msg1 + "\n";
            //    }

            //    GD.Print(msg);
            //}

            if (visualizeObs && sourcePlayerIndex == playerIndexToVisualize)
            {
                Vector3I? nearestSafeCell;
                var safeCellDirection = FindDirectionToNearestSafeCell(GetGridPosition(sourcePlayer.Position), 0, out nearestSafeCell);
                obsNodes[targetPlayer.playerIndex].Position = GetGridPosition(targetPlayer.Position);
                obsIndex = 0;
                for (int y = -obsOffset; y <= obsOffset; y++)
                {
                    for (int x = -obsOffset; x <= obsOffset; x++)
                    {
                        var obs1 = mapObservations[obsIndex] / 4f;
                        var obs2 = bombObservations[obsIndex];
                        var material = (StandardMaterial3D)obsNodeMaterials[targetPlayer.playerIndex][obsIndex];
                        material.AlbedoColor = new Color(obs2, obs1, 0, 0.8f);

                        Vector3I gridPos = pos + new Vector3I(x, 0, y);
                        if (gridPos == nearestSafeCell && gridPos == safeCellDirection)
                        {
                            material.AlbedoColor = new Color(1, 1, 0, 0.8f);
                        }
                        else
                        {
                            if (gridPos == nearestSafeCell)
                            {
                                material.AlbedoColor = new Color(0, 0, 1, 0.8f);
                            }
                            if (gridPos == safeCellDirection)
                            {
                                material.AlbedoColor = new Color(1, 0, 1, 0.8f);
                            }
                        }
                        obsIndex++;
                    }
                }
            }
        }
        catch (Exception e)
        {
            GD.Print(e);
            mapObservations.Fill(0);
            bombObservations.Fill(0);
        }

        return dict;
    }
    public int GetGridIndex(Vector3I pos)
    {
        return (pos.Z + arenaOffset) * arenaSize + pos.X + arenaOffset;
    }
    public static Vector3I GetGridPosition(Vector3 position)
    {
        position.Y = 0;
        return new Vector3I(
            (int)(position.X + 0.5f * (position.X > 0 ? 1 : -1)),
            (int)position.Y,
             (int)(position.Z + 0.5f * (position.Z > 0 ? 1 : -1))
        );
    }
    protected Vector3I GetRandomInnerCell()
    {
        return new Vector3I(-arenaOffset + 1 + random.Next(0, arenaSize - 2), 0, -arenaOffset + 1 + random.Next(0, arenaSize - 2));
    }
    protected Vector3I GetRandomEmptyInnerCell()
    {
        Vector3I pos;
        do
        {
            pos = GetRandomInnerCell();
        } while (GetObjectInCell(pos) != GridIndexes.empty || playerMapSensor[GetGridIndex(pos)] != 0);

        return pos;
    }
    public int GetObjectInCell(Vector3 position)
    {
        position.Y = 0;
        return gridmap.GetCellItem(GetGridPosition(position));
    }

    public override void _Ready()
    {
        gridmap = GetNode<GridMap>("Map/GridMap");
        if (visualizeObs)
        {
            //obsNodeMaterials.Add(new());

            var scene = GD.Load<PackedScene>("res://Scenes//ObsTile.tscn");

            var obsNode = GetNode<Node3D>("Map/Obs");

            //obsNodes.Add(new Node3D());
            //obsNode.AddChild(obsNodes[0]);

            //for (int y = -playerMapObservationsOffset; y <= playerMapObservationsOffset; y++)
            //{
            //    for (int x = -playerMapObservationsOffset; x <= playerMapObservationsOffset; x++)
            //    {
            //        MeshInstance3D instance = (MeshInstance3D)scene.Instantiate();
            //        instance.Position = new Vector3(x, 1.02f, y);

            //        instance.MaterialOverride = (StandardMaterial3D)instance.GetActiveMaterial(0).Duplicate();
            //        obsNodes[0].AddChild(instance);
            //       obsNodeMaterials[0].Add((StandardMaterial3D)instance.GetActiveMaterial(0));
            //    }
            //}

            for (int i = 0; i < players.Count; i++)
            {
                obsNodes.Add(new Node3D());
                obsNode.AddChild(obsNodes[i]);

                obsNodeMaterials.Add(new());
                if(i == playerIndexToVisualize)
                {
                    for (int y = -playerMapObservationsOffset; y <= playerMapObservationsOffset; y++)
                    {
                        for (int x = -playerMapObservationsOffset; x <= playerMapObservationsOffset; x++)
                        {
                            MeshInstance3D instance = (MeshInstance3D)scene.Instantiate();
                            instance.Position = new Vector3(x, 1.01f, y);

                            instance.MaterialOverride = (StandardMaterial3D)instance.GetActiveMaterial(0).Duplicate();
                            obsNodes[i].AddChild(instance);
                            obsNodeMaterials[i].Add((StandardMaterial3D)instance.GetActiveMaterial(0));
                        }
                    }
                }
                else
                {
                    for (int y = -enemyMapObservationsOffset; y <= enemyMapObservationsOffset; y++)
                    {
                        for (int x = -enemyMapObservationsOffset; x <= enemyMapObservationsOffset; x++)
                        {
                            MeshInstance3D instance = (MeshInstance3D)scene.Instantiate();
                            instance.Position = new Vector3(x, 1.01f, y);

                            instance.MaterialOverride = (StandardMaterial3D)instance.GetActiveMaterial(0).Duplicate();
                            obsNodes[i].AddChild(instance);
                            obsNodeMaterials[i].Add((StandardMaterial3D)instance.GetActiveMaterial(0));
                        }
                    }
                }
            }

        }

        mapSensor.Resize(arenaSize * arenaSize);
        playerMapSensor.Resize(arenaSize * arenaSize);

        lastPlayerPositions = new Vector3I[players.Count];

        Vector3I currentPos = new Vector3I(-arenaOffset, 0, -arenaOffset);
        for (int i = 0; i < arenaSize; i++)
        {
            UpdateMapCell(currentPos, GridIndexes.indestructibleWall);
            currentPos.Z += arenaSize - 1;
            UpdateMapCell(currentPos, GridIndexes.indestructibleWall);
            currentPos.Z -= arenaSize - 1;
            currentPos.X += 1;
        }
        currentPos.X -= arenaSize;
        for (int i = 0; i < arenaSize - 2; i++)
        {
            currentPos.Z += 1;
            UpdateMapCell(currentPos, GridIndexes.indestructibleWall);
            currentPos.X += arenaSize - 1;
            UpdateMapCell(currentPos, GridIndexes.indestructibleWall);
            currentPos.X -= arenaSize - 1;
        }

        gameHasRlAgents = false;
        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            player.playerIndex = i;
            lastPlayerPositions[i] = new Vector3I(0, 0, 0);
            playerMapObservations.Add(new());
            playerBombObservations.Add(new());
            playerMapObservations[i].Resize(playerMapObservationsSize * playerMapObservationsSize);
            playerBombObservations[i].Resize(playerMapObservationsSize * playerMapObservationsSize);

            if (player.IsRlAgent)
                gameHasRlAgents = true;
        }

        if (!(bool)GetTree().Root.GetChild(0).FindChild("Sync", false).Get("should_connect_to_server"))
        {
            waitForServer = false;
            StartGame();
        }
    }
    public override void _PhysicsProcess(double delta)
    {
        if (playerIsOutOfBounds)
        {
            GD.Print("Player is out of bounds");
            playerIsOutOfBounds = false;
            RestartGame();
            return;
        }

        if (waitForServer)
        {

            bool resetGame = true;
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (!player.NeedsReset())
                {
                    resetGame = false;
                    break;
                }

            }
            if (resetGame)
            {
                waitForServer = false;
                StartGame();
            }
            return;
        }

        try
        {
            var enumerator = bombs.GetEnumerator();

            bool isNextValid = enumerator.MoveNext();
            while (isNextValid)
            {
                var bomb = enumerator.Current;
                isNextValid = enumerator.MoveNext();

                if (bomb.Value.isRemoved)
                {
                    bombs.Remove(bomb.Key);
                    continue;
                }

                bomb.Value.timeToDetonate -= delta;

                if (bomb.Value.timeToDetonate <= 0)
                {
                    SetTilesOnFire(bomb.Key);
                }
            }

            SimulateDetonationTimes();
        }
        catch (Exception e)
        {
            GD.Print($"Invalid bomb: {e}, {bombs.Count}");
        }

        try
        {
            var enumerator = fires.GetEnumerator();

            bool isNextValid = enumerator.MoveNext();
            while (isNextValid)
            {
                var fire = enumerator.Current;
                isNextValid = enumerator.MoveNext();
                fire.Value.timeToDisappear -= delta;
                if (fire.Value.timeToDisappear <= 0)
                {
                    UpdateMapCell(fire.Key, GridIndexes.empty);
                    fires.Remove(fire.Key);
                }
            }
        }
        catch (Exception e)
        {
            GD.Print($"Invalid fire: {e}, {bombs.Count}");
        }

        for (int i = 0; i < players.Count; i++)
        {
            var lastPos = lastPlayerPositions[i];
            UpdatePlayerCell(lastPos, 0);
        }

        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            if (player.IsDead)
                continue;

            var pos = GetGridPosition(player.Position);

            UpdatePlayerCell(pos, player.TeamID);
            lastPlayerPositions[i] = pos;
        }

        //printDelta += delta;
        //if (printDelta >= printSpeed)
        //{
        //    GetObservationsAroundPlayer(0, true);
        //    printDelta = 0;
        //}

        //if (players[0].IsHuman)
        //{
        //    GetObservationsAroundPlayer(0, 1, false, false);
        //}
    }
    protected virtual void StartGame()
    {
        StartGame(() => DefaultGameMode());
    }
    protected void StartGame(Action gameMode)
    {
        // cleanup
        bombs.Clear();
        fires.Clear();
        Array.Clear(bombValues, 0, bombValues.Length);

        // cleanup
        for (int i = 0; i < playerMapSensor.Count; i++)
        {
            playerMapSensor[i] = 0;
        }
        Vector3I pos = new Vector3I();
        for (int z = -arenaOffset + 1; z < -arenaOffset + arenaSize - 1; z++)
        {
            for (int x = -arenaOffset + 1; x < -arenaOffset + arenaSize - 1; x++)
            {
                pos.X = x;
                pos.Z = z;
                UpdateMapCell(pos, GridIndexes.empty);
            }
        }

        gameMode();
    }
    public void RestartGame()
    {
        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            player.Despawn();
        }

        StartGame();

    }
    public bool PlaceBomb(Vector3 position, int playerIndex, out float rating)
    {
        rating = 0;

        Vector3I pos = GetGridPosition(position);
        if (gridmap.GetCellItem(pos) != GridIndexes.empty)
            return false;

        UpdateMapCell(pos, GridIndexes.bomb);

        bombs[pos] = new Bomb(bombDetonationSpeed, playerIndex);

        rating = RateBombPlacement(pos, players[playerIndex].TeamID, playerIndex);
        return true;
    }
    float RateBombPlacement(Vector3I pos, int sourcePlayerTeamID, int sourcePlayerIndex)
    {
        float rating = 0;
        // can player get out of the situation? no -> -1, return

        // The agent will figure this out on their own. TODO: remove?
        //if(FindNearestSafeCell(pos, sourcePlayerIndex) == null)
        //{
        //    return -1;
        //}

        // is anyone around? no -> -0.25, yes -> +0.2 per player
        bool foundEnemy = false;
        for (int y = -playerMapObservationsOffset; y <= playerMapObservationsOffset; y++)
        {
            for (int x = -playerMapObservationsOffset; x <= playerMapObservationsOffset; x++)
            {
                Vector3I gridPos = pos + new Vector3I(x, 0, y);

                int index = GetGridIndex(gridPos);

                if (IsInnerCell(gridPos))
                {
                    int teamID = playerMapSensor[index];
                    if (teamID != 0)
                    {
                        if (teamID != sourcePlayerTeamID)
                        {
                            rating += 0.2f;
                            foundEnemy = true;
                        }
                    }
                }
            }
        }
        if (!foundEnemy)
            rating -= 0.25f;


        // is something in bomb radius? yes -> +0.4 if there is a player, +0.2 if there is a destructible wall
        var things = GetThingsInBombRadius(pos, players[sourcePlayerIndex].BombStrength, sourcePlayerTeamID);
        float bestRating = 0;
        foreach (var thing in things)
        {
            switch (thing)
            {
                case GridIndexes.enemyPlayer:
                    bestRating = Math.Max(0.4f, bestRating);
                    break;
                case GridIndexes.destructibleWall:
                    bestRating = Math.Max(0.2f, bestRating);
                    break;
            }

        }
        rating += bestRating;
        return rating;
    }
    protected void DefaultGameMode()
    {
        AdddestructibleWalls();
        AddIndestructibleWalls();
        SpawnInCorners();
    }
    protected void AddIndestructibleWalls()
    {
        var currentPos = new Vector3I(-arenaOffset + 2, 0, -arenaOffset + 2);
        for (int z = 0; z < arenaOffset - 1; z++)
        {
            for (int x = 0; x < arenaOffset - 1; x++)
            {
                UpdateMapCell(currentPos, GridIndexes.indestructibleWall);
                currentPos.X += 2;
            }
            currentPos.X = -arenaOffset + 2;
            currentPos.Z += 2;
        }
    }
    protected void AdddestructibleWalls()
    {
        for (int i = 0; i < 100; i++)
        {
            UpdateMapCell(GetRandomInnerCell(), GridIndexes.destructibleWall);
        }
    }
    protected void SpawnRandom()
    {
        alivePlayerCount = players.Count;
        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];

            var pos = GetRandomEmptyInnerCell();
            SpawnPlayer(pos, player);
            //UpdateMapCell(pos, GridIndexes.empty);
            //player.Spawn(pos);
            //UpdatePlayerCell(pos, player.TeamID);
            //lastPlayerPositions[i] = pos;
        }
        //FreeSpaceAroundPlayers();
    }
    protected void SpawnInCorners()
    {
        HashSet<int> usedSpawnPositions = new();

        alivePlayerCount = players.Count;
        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];

            int spawnPositionIndex;
            do
            {
                spawnPositionIndex = random.Next(0, players.Count);
            } while (usedSpawnPositions.Contains(spawnPositionIndex));
            usedSpawnPositions.Add(spawnPositionIndex);

            var spawnPos = spawnPositions[spawnPositionIndex];

            //player.Spawn(spawnPos);
            SpawnPlayer(spawnPos, player);
        }
        usedSpawnPositions.Clear();
        //FreeSpaceAroundPlayers();
    }
    //protected void FreeSpaceAroundPlayers()
    //{
    //    // make empty space around players
    //    for (int i = 0; i < players.Count; i++)
    //    {
    //        var player = players[i];
    //        var pos = GetGridPosition(player.Position);

    //        for(int z = -1; z <= 1; z++)
    //        {
    //            for(int x = -1; x <= 1; x++)
    //            {
    //                Vector3I newPos = pos + new Vector3I(x, 0, z);
    //                if (IsInnerCell(newPos) && GetObjectInCell(newPos) == GridIndexes.destructibleWall)
    //                    UpdateMapCell(newPos, GridIndexes.empty);
    //            }
    //        }
    //    }
    //    //    foreach (Vector3I spawnPos in spawnPositions)
    //    //{
    //    //    for (int x = -1; x <= 1; x++)
    //    //    {
    //    //        Vector3I newPos = spawnPos + new Vector3I(x, 0, 0);
    //    //        if (IsInnerCell(newPos))
    //    //            UpdateMapCell(newPos, GridIndexes.empty);
    //    //    }

    //    //    for (int z = -1; z <= 1; z += 2)
    //    //    {
    //    //        Vector3I newPos = spawnPos + new Vector3I(0, 0, z);
    //    //        if (IsInnerCell(newPos))
    //    //            UpdateMapCell(newPos, GridIndexes.empty);
    //    //    }
    //    //}
    //}
    protected void SpawnPlayer(Vector3I pos, Character player)
    {
        pos.Y = 0;
        for (int z = -1; z <= 1; z++)
        {
            for (int x = -1; x <= 1; x++)
            {
                Vector3I newPos = pos + new Vector3I(x, 0, z);
                if (IsInnerCell(newPos) && GetObjectInCell(newPos) == GridIndexes.destructibleWall)
                    UpdateMapCell(newPos, GridIndexes.empty);
            }
        }
        //UpdatePlayerCell(lastPlayerPositions[player.playerIndex], 0);
        //GD.Print($"pos: {pos}, index: {player.playerIndex	}");
        UpdatePlayerCell(pos, player.TeamID);
        player.Spawn(pos);
        //GD.Print($": {playerMapSensor[GetGridIndex(pos)]}");
        lastPlayerPositions[player.playerIndex] = pos;
    }
    void UpdateMapCell(Vector3I pos, int what)
    {
        pos.Y = 0;
        int index = 0;
        try
        {
            gridmap.SetCellItem(pos, what);

            int valueToAdd;
            try
            {
                valueToAdd = gridObsValues[what];
            }
            catch (Exception e)
            {
                GD.PrintErr($"obs value {what} not implemented, error: {e}");
                valueToAdd = 0;
            }


            index = GetGridIndex(pos);
            mapSensor[index] = valueToAdd;
        }
        catch (Exception e)
        {
            GD.PrintErr($"Failed to update map cell in position {pos} (what = {what}, index = {index}), error:\n{e}");
            RestartGame();
        }
    }
    void UpdatePlayerCell(Vector3I pos, int teamID)
    {
        int index = GetGridIndex(pos);
        if (pos.Y != 0 || !IsInnerCell(pos) || index >= playerMapSensor.Count || index < 0)
        {
            GD.PrintErr($"Error :: {index}, {pos}");
            playerIsOutOfBounds = true;
            // ForceEndGame();
        }
        else
        {
            playerMapSensor[index] = teamID;
        }

    }

    HashSet<Vector3I> usedBombs = new();
    HashSet<Vector3I> currentBombs = new();
    void SimulateDetonationTimes()
    {
        Array.Clear(bombValues, 0, bombValues.Length);
        usedBombs.Clear();

        foreach (var bomb in bombs)
        {
            if (usedBombs.Contains(bomb.Key))
                continue;

            var detonationTime = GetLowestDetonationTime(bomb.Key, bomb.Value, (float)bomb.Value.timeToDetonate);
            FillDetonationTimes(detonationTime);
            currentBombs.Clear();
        }
    }
    float GetLowestDetonationTime(Vector3I pos, Bomb bomb, float currentLowestValue)
    {
        float lowestValue = Mathf.Min(currentLowestValue, (float)bomb.timeToDetonate);
        currentBombs.Add(pos);
        usedBombs.Add(pos);

        int playerIndex = bomb.playerIndex;
        Character player = players[playerIndex];
        int bombStrength = player.BombStrength;

        foreach (Vector3I direction in directions)
        {
            for (int i = 1; i <= bombStrength; i++)
            {
                Vector3I newPos = pos + direction * i;
                int item = GetObjectInCell(newPos);
                if (item == GridIndexes.bomb)
                {
                    if (!currentBombs.Contains(newPos))
                        lowestValue = GetLowestDetonationTime(newPos, bombs[newPos], lowestValue);

                    break;
                }
                else if (item == GridIndexes.destructibleWall || item == GridIndexes.indestructibleWall)
                {
                    break;
                }
            }
        }

        return lowestValue;
    }
    void FillDetonationTimes(float detonationTime)
    {
        foreach (Vector3I pos in currentBombs)
        {
            var bomb = bombs[pos];

            int playerIndex = bomb.playerIndex;
            Character player = players[playerIndex];
            int bombStrength = player.BombStrength;

            bombValues[GetGridIndex(pos)] = detonationTime;

            foreach (Vector3I direction in directions)
            {
                for (int i = 1; i <= bombStrength; i++)
                {
                    Vector3I newPos = pos + direction * i;
                    int item = GetObjectInCell(newPos);

                    if (item == GridIndexes.bomb)
                    {
                        break;
                    }
                    else if (item == GridIndexes.destructibleWall || item == GridIndexes.indestructibleWall)
                    {
                        break;
                    }
                    else if (item == GridIndexes.fire)
                    {
                        continue;
                    }

                    bombValues[GetGridIndex(newPos)] = detonationTime;
                }
            }
        }
    }

    void SetTilesOnFire(Vector3I pos)
    {
        if (!bombs.TryGetValue(pos, out Bomb bomb))
        {
            GD.Print($"non-existing bomb found at{pos}");
            return;
        }

        if (bomb.isRemoved)
        {
            GD.Print($"removed bomb found at{pos}");
            return;
        }


        int playerIndex = bomb.playerIndex;
        Character player = players[playerIndex];
        int teamID = player.TeamID;
        int bombStrength = player.BombStrength;
        player.SpawnedBombs = Math.Max(0, player.SpawnedBombs - 1);

        bomb.isRemoved = true;
        HandleFireInsertion(pos, teamID);

        foreach (Vector3I direction in directions)
        {
            for (int i = 1; i <= bombStrength; i++)
            {
                Vector3I newPos = pos + direction * i;

                int item = GetObjectInCell(newPos);
                if (item == GridIndexes.bomb)
                {
                    SetTilesOnFire(newPos);
                    break;
                }
                if (item == GridIndexes.destructibleWall)
                {
                    player.OnWallDestroyed();
                    HandleFireInsertion(newPos, teamID);
                    break;
                }
                else if (item != GridIndexes.empty && item != GridIndexes.fire)
                {
                    break;
                }

                HandleFireInsertion(newPos, teamID);
            }
        }
    }
    void HandleFireInsertion(Vector3I pos, int teamID)
    {
        pos.Y = 0;
        fires[pos] = new Fire(fireDuration, teamID);

        UpdateMapCell(pos, GridIndexes.fire);
    }

    public void OnCollectibleCollected(Vector3 position)
    {
        Vector3I pos = GetGridPosition(position);
        UpdateMapCell(pos, GridIndexes.empty);

        var playerPos = GetGridPosition(position);
        var cell = GetRandomInnerCell();

        while (playerPos == cell)
        {
            cell = GetRandomInnerCell();
        }
        UpdateMapCell(cell, GridIndexes.collectible1);
    }
    public void OnPlayerHit(int teamID, Vector3 playerPosition)
    {
        if (!fires.TryGetValue(GetGridPosition(playerPosition), out var fire))
        {
            return;
        }
        var fireTeamID = fire.teamID;
        if (fireTeamID == teamID)
        {
            for (int i = 0; i < players.Count; i++)
            {
                var currentPlayer = players[i];
                if (currentPlayer.TeamID == teamID)
                {
                    // GD.Print($"player from team {currentPlayer.TeamID} hit themselves");
                    currentPlayer.OnTeamHit();
                }
            }
        }
        else
        {
            for (int i = 0; i < players.Count; i++)
            {
                var currentPlayer = players[i];
                if (currentPlayer.TeamID == fireTeamID)
                {
                    //GD.Print($"player from team {currentPlayer.TeamID} hit someone from team {teamID}");
                    currentPlayer.OnEnemyTeamHit();
                }
                else if (currentPlayer.TeamID == teamID)
                {
                    //GD.Print($"player from team {currentPlayer.TeamID} received a hit from team {fireTeamID}");
                    currentPlayer.OnTeamHit();
                }
            }
        }

    }
    public virtual void OnPlayerDeath(int playerIndex)
    {
        alivePlayerCount--;
        var deadPlayer = players[playerIndex];
        foreach (var player in players)
        {
            if (player.TeamID != deadPlayer.TeamID)
                player.OnEnemyDeath();
        }

        if (alivePlayerCount <= 1)
        {
            foreach (var player in players)
            {
                if (player.Lives > 0)
                {
                    player.OnTeamWin();
                    player.Despawn();
                }
            }
            StartGame();
            return;
        }

        if (gameHasRlAgents && resetWhenRLAgentsDie)
        {
            bool isRlAgentAlive = false;
            foreach (var player in players)
            {
                if (player.IsRlAgent && !player.IsDead)
                {
                    isRlAgentAlive = true;
                    return;
                }
            }

            if(!isRlAgentAlive)
                RestartGame();
        }
    }

    class Bomb
    {
        public double timeToDetonate;
        public int playerIndex;
        public bool isRemoved = false;

        public Bomb(double timeToDetonate, int playerIndex)
        {
            this.timeToDetonate = timeToDetonate;
            this.playerIndex = playerIndex;
        }
    }
    class Fire
    {
        public double timeToDisappear;
        public int teamID;

        public Fire(double timeToDisappear, int teamID)
        {
            this.timeToDisappear = timeToDisappear;
            this.teamID = teamID;
        }
    }
}
