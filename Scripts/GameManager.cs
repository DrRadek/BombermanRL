using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static Godot.Projection;

public partial class GameManager : Node3D
{
    bool waitForServer = true;

    bool visualizeObs = true;
    Node3D obsNode;
    List<StandardMaterial3D> obsNodeMaterials;

    double printSpeed = 0.3f;
    double printDelta = 0;

    [Export]
    GridMap gridmap;

    [Export]
    int playerCount = 2;

    [Export]
    int playerID = 0;

    [Export]
    public Godot.Collections.Array<Character> players = new();

    [Export]
    public int maxPlayerCount = 4;

    Vector3I[] lastPlayerPositions;

    //public Godot.Collections.Array<float> playerMapObservations = new();
    const int playerMapObservationsSize = 7;//21
    const int playerMapObservationsOffset = playerMapObservationsSize/2;

    //[Export]
    //PackedScene playableCharacter;

    //[Export]
    //PackedScene character;

    Random random = new Random();

    int alivePlayerCount;
    
    Dictionary<Vector3I, Bomb> bombs = new();

    Dictionary<Vector3I, Fire> fires = new();
    //List<Fire> fires = new();

    float[] bombValues = new float[arenaSize * arenaSize]; // = new(arenaSize * arenaSize);


    const int arenaSize = 17;
    const float bombDetonationSpeed = 2.5f * 0.5f;
    const float fireDuration = 1.5f * 0.5f;

    Vector3I[] spawnPositions = new Vector3I[]{
        new Vector3I(-cornerOffset, 0, -cornerOffset),
        new Vector3I(-cornerOffset, 0, cornerOffset),
        new Vector3I(cornerOffset, 0, -cornerOffset),
        new Vector3I(cornerOffset, 0, cornerOffset),
    };

    const int arenaOffset = (arenaSize / 2);
    const int cornerOffset = arenaOffset - 1;

    Vector3I[] directions = {
        new Vector3I(1,0,0),
        new Vector3I(-1,0,0),
        new Vector3I(0,0,1),
        new Vector3I(0, 0, -1)
    };

    Dictionary<int, float> gridObsValues = new() {
        {(int)GridMap.InvalidCellItem, 0 },
        {GridIndexes.collectible1, 0.6f },
        {GridIndexes.indestructibleWall, 0.25f },
        {GridIndexes.destructibleWall, 0.5f },
        {GridIndexes.bomb, -0.5f},
        {GridIndexes.fire, -1f},
        {GridIndexes.friendlyPlayer, 0.75f},
        {GridIndexes.enemyPlayer, 1f},

    };

    //bool needsReset = false;

    //public Godot.Collections.Array<Godot.Collections.Array<Godot.Collections.Array<int>>> mapSensor = new();
    public Godot.Collections.Array<float> mapSensor = new();
    public Godot.Collections.Array<int> playerMapSensor = new();
    //public int[,] mapSensor = new int[arenaSize, arenaSize];

    public class GridIndexes
    {
        public const int indestructibleWall = 0;
        public const int destructibleWall = 1;
        public const int bomb = 2;
        public const int fire = 3;
        public const int collectible1 = 4;
        public const int friendlyPlayer = 5;
        public const int enemyPlayer = 6;
    }

    public void OnCollectibleCollected(Vector3 position)
    {
        Vector3I pos = GetGridPosition(position);
        UpdateMapCell(pos, -1);

        var playerPos = GetGridPosition(position);
        var cell = GetRandomInnerCell();
        
        while(playerPos == cell)
        {
            cell = GetRandomInnerCell();
        }
        UpdateMapCell(cell, GridIndexes.collectible1);
    }

    Vector3I GetRandomInnerCell()
    {
        return new Vector3I(-arenaOffset+1 + random.Next(0,arenaSize-2),0, -arenaOffset+1 + random.Next(0, arenaSize-2));
    }

    bool IsInnerCell(Vector3I pos)
    {
        return pos.X >= (-arenaOffset + 1) && pos.Z >= (-arenaOffset + 1) && pos.X < (-arenaOffset - 1 + arenaSize) && pos.Z < (-arenaOffset - 1 + arenaSize);
    }

    public bool PlaceBomb(Vector3 position, int playerIndex)
    {

        Vector3I pos = GetGridPosition(position);
        if (gridmap.GetCellItem(pos) != GridMap.InvalidCellItem)
            return false;

        //AddBomb(pos, playerID);
        UpdateMapCell(pos, GridIndexes.bomb);

        bombs[pos] = new Bomb(bombDetonationSpeed, playerIndex);
        return true;
    }

    public int GetObjectInCell(Vector3 position)
    {
        position.Y = 0;
        return gridmap.GetCellItem(GetGridPosition(position));
    }



    public override void _Ready()
    {
        if (visualizeObs)
        {
            obsNodeMaterials = new();
            var scene = GD.Load<PackedScene>("res://Scenes//ObsTile.tscn");
            obsNode = GetNode<Node3D>("Obs");

            for (int y = -playerMapObservationsOffset; y <= playerMapObservationsOffset; y++)
            {
                for (int x = -playerMapObservationsOffset; x <= playerMapObservationsOffset; x++)
                {
                    MeshInstance3D instance = (MeshInstance3D)scene.Instantiate();
                    instance.Position = new Vector3(x, 1.01f, y);
                    //instance.Transparency = 0.5f;

                    instance.MaterialOverride = (StandardMaterial3D)instance.GetActiveMaterial(0).Duplicate();
                    obsNode.AddChild(instance);
                    obsNodeMaterials.Add((StandardMaterial3D)instance.GetActiveMaterial(0));
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
            currentPos.Z += arenaSize-1;
            UpdateMapCell(currentPos, GridIndexes.indestructibleWall);
            currentPos.Z -= arenaSize-1;
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


        for (int i = 0; i  < players.Count; i++)
        {
            players[i].playerIndex = i;
            lastPlayerPositions[i] = new Vector3I(0,0,0);
        }

        if (!(bool)GetTree().Root.GetChild(0).FindChild("Sync", false).Get("should_connect_to_server"))
        {
            waitForServer = false;
            StartGame();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (waitForServer)
        {
            
            bool resetGame = true;
            for (int i = 0; i < playerCount; i++)
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
                    SetTilesOnFire(bomb.Key); // bomb.Value.strength
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
                    UpdateMapCell(fire.Key, -1);
                    fires.Remove(fire.Key);
                }
            }
        }
        catch (Exception e)
        {
            GD.Print($"Invalid fire: {e}, {bombs.Count}");
        }

        for (int i = 0; i < playerCount; i++)
        {
            var lastPos = lastPlayerPositions[i];
            UpdatePlayerCell(lastPos, 0);
        }

        for (int i = 0; i < playerCount; i++)
        {
            var player = players[i];
            if (player.Lives == 0)
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

        
        //GetObservationsAroundPlayer(0, false);
    }

    void StartGame()
    {
        // clean
        bombs.Clear();
        fires.Clear();
        Array.Clear(bombValues, 0, bombValues.Length);

        // clean
        Vector3I pos = new Vector3I();
        for (int z = -arenaOffset + 1; z < -arenaOffset + arenaSize - 1; z++)
        {
            for (int x = -arenaOffset + 1; x < -arenaOffset + arenaSize - 1; x++)
            {
                pos.X = x;
                pos.Z = z;
                UpdateMapCell(pos, -1);
            }
        }

        // add random destructible walls
        for (int i = 0; i < 100; i++)
        {
            UpdateMapCell(GetRandomInnerCell(), GridIndexes.destructibleWall);
        }

        // add indestructible walls
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

        // make empty space around corners
        foreach (Vector3I spawnPos in spawnPositions)
        {
            for (int x = -1; x <= 1; x++)
            {
                Vector3I newPos = spawnPos + new Vector3I(x, 0, 0);
                if (IsInnerCell(newPos))
                    UpdateMapCell(newPos, -1);
            }

            for (int z = -1; z <= 1; z += 2)
            {
                Vector3I newPos = spawnPos + new Vector3I(0, 0, z);
                if (IsInnerCell(newPos))
                    UpdateMapCell(newPos, -1);
            }
        }



        //{

        //for (int i = 0; i < 10; i++)
        //{
        //    var cell = GetRandomInnerCell();
        //    //for (int zz = -1; zz <= 1; zz++)
        //    //{
        //    //    for (int xx = -1; xx <= 1; xx++)
        //    //    {
        //    //        var newCell = cell + new Vector3I(xx, 0, zz);
        //    //        if(IsInnerCell(newCell))
        //    //            UpdateMapCell(newCell, GridIndexes.collectible1);
        //    //    }
        //    //}
        //    UpdateMapCell(cell, GridIndexes.collectible1);
        //}

        //spawnPositions

        alivePlayerCount = playerCount;

        //int[] spawnOrder = new int[]{ 0,1,2,3};
        //int[] MyRandomNumbers = spawnOrder.OrderBy(x => random.Next()).ToArray();

        HashSet<int> usedSpawnPositions = new();


        for (int i = 0; i < playerCount; i++)
        {
            var player = players[i];

            int spawnPositionIndex;
            do
            {
                spawnPositionIndex = random.Next(0, playerCount);
            } while (usedSpawnPositions.Contains(spawnPositionIndex));
            usedSpawnPositions.Add(spawnPositionIndex);

            var spawnPos = spawnPositions[spawnPositionIndex];
            player.Spawn(spawnPos);
        }
        usedSpawnPositions.Clear();
    }

    public void OnPlayerHit(int teamID, Vector3 playerPosition)
    {
        if (!fires.TryGetValue(GetGridPosition(playerPosition), out var fire)){
            return;
        }
        var fireTeamID = fire.teamID;
        if (fireTeamID == teamID)
        {
            for (int i = 0; i < playerCount; i++)
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
            for (int i = 0; i < playerCount; i++)
            {
                var currentPlayer = players[i];
                if (currentPlayer.TeamID == fireTeamID)
                {
                    // GD.Print($"player from team {currentPlayer.TeamID} hit someone from team {teamID}");
                    currentPlayer.OnEnemyTeamHit();
                }
                else if(currentPlayer.TeamID == teamID)
                {
                    // GD.Print($"player from team {currentPlayer.TeamID} received a hit from team {fireTeamID}");
                    currentPlayer.OnTeamHit();
                }
            }
        }
        
    }

    public void OnPlayerDeath(int playerID)
    {
        alivePlayerCount--;
        if(alivePlayerCount <= 1)
        {
            for(int i = 0; i < playerCount; i++)
            {
                var player = players[i];
                if(player.Lives > 0)
                {
                    player.OnTeamWin();
                }
            }

            StartGame();
        }
    }

    public void ForceEndGame()
    {
        for (int i = 0; i < playerCount; i++)
        {
            var player = players[i];
            player.Despawn();
        }

        StartGame();
        
    }

    void UpdateMapCell(Vector3I pos, int what)
    {
        pos.Y = 0;
        int index = 0;
        try
        {
            gridmap.SetCellItem(pos, what);

            float valueToAdd;
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
        catch(Exception e)
        {
            GD.PrintErr($"Failed to update map cell in position {pos} (what = {what}, index = {index})");
            ForceEndGame();
        }
    }


    void UpdatePlayerCell(Vector3I pos, int teamID)
    {
        pos.Y = 0;
        int index = GetGridIndex(pos);
        if(index >= playerMapSensor.Count || index < 0)
        {
            GD.PrintErr($"Error :: {index}, {pos}");
            ForceEndGame(); // asi?
        }
        else
        {
            playerMapSensor[index] = teamID;
        }
        
    }

    int GetGridIndex(Vector3I pos)
    {
        return (pos.Z + arenaOffset) * arenaSize + pos.X + arenaOffset;
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
            // GD.Print($"value after: {detonationTime}, bomb: {bomb.Key}");
            FillDetonationTimes(detonationTime);
            currentBombs.Clear();
        }
    }


    float GetLowestDetonationTime(Vector3I pos, Bomb bomb, float currentLowestValue)
    {
        float lowestValue = Mathf.Min(currentLowestValue, (float)bomb.timeToDetonate);
        // GD.Print($"values before: {lowestValue}, bomb: {pos}");
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
        foreach(Vector3I pos in currentBombs)
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
                    else if(item == GridIndexes.fire)
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
        player.SpawnedBombs--;

        bomb.isRemoved = true;
        HandleFireInsertion(pos, teamID);

        foreach (Vector3I direction in directions)
        {
            for(int i = 1; i <= bombStrength; i++)
            {
                Vector3I newPos = pos + direction * i;

                int item = GetObjectInCell(newPos); // gridmap.GetCellItem(newPos);
                if(item == GridIndexes.bomb)
                {
                    SetTilesOnFire(newPos);
                    //bombs.Remove(newPos);
                    break;
                }
                if(item == GridIndexes.destructibleWall)
                {
                    player.OnWallDestroyed();
                    HandleFireInsertion(newPos, teamID);
                    break;
                }
                else if (item != GridMap.InvalidCellItem && item != GridIndexes.fire)
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

    public Godot.Collections.Array<float> GetObservationsAroundPlayer(int playerIndex, bool printObs = false)
    {
        Godot.Collections.Array<float> playerMapObservations = new();
        playerMapObservations.Resize(playerMapObservationsSize * playerMapObservationsSize);

        var player = players[playerIndex];
        if(player.Lives == 0)
        {
            for (int i = 0; i < playerMapObservations.Count; i++)
            {
                playerMapObservations[i] = 0;
            }
            return playerMapObservations;
        }
        try
        {
            Vector3I pos = GetGridPosition(player.Position);

            int obsIndex = 0;

            for (int y = -playerMapObservationsOffset; y <= playerMapObservationsOffset; y++)
            {
                for (int x = -playerMapObservationsOffset; x <= playerMapObservationsOffset; x++)
                {
                    Vector3I gridPos = pos + new Vector3I(x, 0, y);

                    int index = GetGridIndex(gridPos);

                    if (IsInnerCell(gridPos))
                    {
                        int obj = GetObjectInCell(gridPos);
                        float bombValue = bombValues[index];
                        if (/*obj == GridIndexes.bomb*/ bombValue != 0)
                        {
                            //var bomb = bombs[gridPos];
                            // { GridIndexes.bomb, -0.5f},
                            // { GridIndexes.fire, -1f},
                            playerMapObservations[obsIndex] = gridObsValues[GridIndexes.fire] 
                                + (gridObsValues[GridIndexes.bomb] - gridObsValues[GridIndexes.fire]) * (bombValue / bombDetonationSpeed);
                        }
                        else
                        {
                            int teamID = playerMapSensor[index];
                            if (teamID != 0 && obj != GridIndexes.fire)
                            {
                                if (teamID == player.TeamID)
                                    playerMapObservations[obsIndex] = gridObsValues[GridIndexes.friendlyPlayer];
                                else
                                    playerMapObservations[obsIndex] = gridObsValues[GridIndexes.enemyPlayer];
                            }
                            else
                            {
                                playerMapObservations[obsIndex] = mapSensor[index];
                            }
                        }
                    }
                    else
                    {
                        playerMapObservations[obsIndex] = gridObsValues[GridIndexes.indestructibleWall];
                    }

                   

                    obsIndex++;
                }


            }

            // TODO: Update bomb detonation times for obs



            // 
            var strength = playerMapObservations[playerMapObservations.Count / 2];
            if (strength <= gridObsValues[GridIndexes.bomb])
            {
                //GD.Print(-strength);
                player.OnDangerousTileTouched(-strength);
            }



            if (printObs)
            {
                String msg = "";
                obsIndex = 0;
                for (int y = -playerMapObservationsOffset; y <= playerMapObservationsOffset; y++)
                {
                    String msg1 = "";
                    for (int x = -playerMapObservationsOffset; x <= playerMapObservationsOffset; x++)
                    {
                        msg1 += playerMapObservations[obsIndex] + " ";
                        obsIndex++;
                    }
                    msg += msg1 + "\n";
                }

                GD.Print(msg);
            }

            if (visualizeObs)
            {
                obsNode.Position = GetGridPosition(player.Position);
                obsIndex = 0;
                for (int y = -playerMapObservationsOffset; y <= playerMapObservationsOffset; y++)
                {
                    for (int x = -playerMapObservationsOffset; x <= playerMapObservationsOffset; x++)
                    {
                        var obs = playerMapObservations[obsIndex];
                        var idk = (StandardMaterial3D)obsNodeMaterials[obsIndex];
                        if(obs >= 0)
                        {
                            idk.AlbedoColor = new Color(0, obs, 0, 0.8f);
                        }
                        else
                        {
                            idk.AlbedoColor = new Color(-obs, 0, 0, 0.8f);
                        }
                        
                        obsIndex++;
                    }
                }
            }
        }
        catch (Exception e)
        {
            GD.Print(e);
            for (int i = 0; i < playerMapObservations.Count; i++)
            {
                playerMapObservations[i] = 0;
            }
        }

        return playerMapObservations;
    }

    public Vector3I GetGridPosition2(Vector3 position)
    {
        position.Y = 0;
        return new Vector3I(
            (int)(position.X + 0.5f * (position.X > 0 ? 1 : -1)),
            (int)position.Y,
             (int)(position.Z + 0.5f * (position.Z > 0 ? 1 : -1))
        );
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
        //public List<Vector3I> positions = new();
        public double timeToDisappear;
        public int teamID;

        public Fire(double timeToDisappear, int teamID)
        {
            this.timeToDisappear = timeToDisappear;
            this.teamID = teamID;
        }
    }
}
