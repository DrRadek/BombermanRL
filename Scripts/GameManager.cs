using Godot;
using System;
using System.Collections.Generic;
using static Godot.Projection;

public partial class GameManager : Node3D
{
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


    //[Export]
    //PackedScene playableCharacter;

    //[Export]
    //PackedScene character;

    Random random = new Random();

    int alivePlayerCount;
    
    Dictionary<Vector3I, Bomb> bombs = new();

    List<Fire> fires = new();

    const int arenaSize = 17;
    const float bombDetonationSpeed = 2.5f;
    const float fireDuration = 1.5f;

    Dictionary<Vector3,int> fireCounts = new();

    int arenaOffset = (arenaSize / 2);

    Vector3I[] directions = {
        new Vector3I(1,0,0),
        new Vector3I(-1,0,0),
        new Vector3I(0,0,1),
        new Vector3I(0, 0, -1)
    };


    bool needsReset = false;

    //public Godot.Collections.Array<Godot.Collections.Array<Godot.Collections.Array<int>>> mapSensor = new();
    public Godot.Collections.Array<float> mapSensor = new();
    public Godot.Collections.Array<float> playerMapSensor = new();
    //public int[,] mapSensor = new int[arenaSize, arenaSize];

    public class GridIndexes
    {
        public const int indestructibleWall = 0;
        public const int destructibleWall = 1;
        public const int bomb = 2;
        public const int fire = 3;
        public const int collectible1 = 4;
    }

    public void OnCollectibleCollected(Vector3 position)
    {
        Vector3I pos = GetGridPosition(position);
        UpdateMapCell(pos, -1);
        UpdateMapCell(GetRandomInnerCell(), GridIndexes.collectible1);
    }

    Vector3I GetRandomInnerCell()
    {
        return new Vector3I(-arenaOffset+1 + random.Next(0,arenaSize-2),0, -arenaOffset+1 + random.Next(0, arenaSize-2));
    }

    bool IsInnerCell(Vector3I pos)
    {
        return pos.X >= (-arenaOffset + 1) && pos.Z >= (-arenaOffset + 1) && pos.X < (-arenaOffset - 1 + arenaSize) && pos.Z < (-arenaOffset - 1 + arenaSize);
    }

    public bool PlaceBomb(Vector3 position, int playerID)
    {

        Vector3I pos = GetGridPosition(position);
        if (gridmap.GetCellItem(pos) != GridMap.InvalidCellItem)
            return false;

        AddBomb(pos, playerID);
        //gridmap.SetCellItem(pos, GridIndexes.bomb);

        bombs[pos] = new Bomb(bombDetonationSpeed, playerID);

        return true;
    }

    public int GetObjectOnCell(Vector3 position)
    {
        //Vector3I pos = GetGridPosition(position);
        return gridmap.GetCellItem(GetGridPosition(position)); // == GridIndexes.fire;
    }

    public override void _Ready()
    {


        //players.Resize(playerCount);
        lastPlayerPositions = new Vector3I[players.Count];

        mapSensor.Resize(arenaSize * arenaSize);
        playerMapSensor.Resize(arenaSize * arenaSize);
        for (int y = 0; y < arenaSize; y++)
        {
            //mapSensor[y] = new();
            //mapSensor[y].Resize(arenaSize);
            for (int x = 0; x < arenaSize; x++)
            {
                //mapSensor[y][x] = new();
                //mapSensor[y][x].Resize(2);
                //mapSensor[y][x][0] = 0;
                //mapSensor[y][x][1] = 0;
                mapSensor[x + y * arenaSize] = 0;
                playerMapSensor[x + y * arenaSize] = 0;
            }
        }
        //foreach(var i in mapSensor)
        //{
        //    i.Resize(arenaSize);
        //    foreach(var j in i)
        //    {
        //        i[j] = 0;
        //    }
        //}

        Vector3I currentPos = new Vector3I(-arenaOffset, 0, -arenaOffset);
        for (int i = 0; i < arenaSize; i++)
        {
            UpdateMapCell(currentPos, GridIndexes.indestructibleWall);
            //gridmap.SetCellItem(currentPos, GridIndexes.indestructibleWall);
            currentPos.Z += arenaSize-1;
            UpdateMapCell(currentPos, GridIndexes.indestructibleWall);
            //gridmap.SetCellItem(currentPos, GridIndexes.indestructibleWall);
            currentPos.Z -= arenaSize-1;
            currentPos.X += 1;
        }
        currentPos.X -= arenaSize;
        for (int i = 0; i < arenaSize - 2; i++)
        {
            currentPos.Z += 1;
            UpdateMapCell(currentPos, GridIndexes.indestructibleWall);
            //gridmap.SetCellItem(currentPos, GridIndexes.indestructibleWall);
            currentPos.X += arenaSize - 1;
            UpdateMapCell(currentPos, GridIndexes.indestructibleWall);
            //gridmap.SetCellItem(currentPos, GridIndexes.indestructibleWall);
            currentPos.X -= arenaSize - 1;
        }

        //GD.Print("test");
        //GetParent().AddChild(playableCharacter.Instantiate());
        //players[0] = (Character)playableCharacter.Instantiate();
        //GD.Print("test1");
        //players[0].Reparent(GetParent());
        //players[1] = (Character)playableCharacter.Instantiate();
        //players[1].Reparent(GetParent());
        

        for (int i = 0; i  < players.Count; i++)
        {
            players[i].playerIndex = i;
            lastPlayerPositions[i] = new Vector3I(0,0,0);
        }

        //StartGame();

        //CleanGame();
        //StartGame();
    }

    public override void _PhysicsProcess(double delta)
    {
        alivePlayerCount = playerCount;
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

            StartGame();
        }

        try
        {
            var enumerator = bombs.GetEnumerator();

            bool isNextValid = enumerator.MoveNext();
            while (isNextValid)
            {
                var bomb = enumerator.Current;
                isNextValid = enumerator.MoveNext();
                bomb.Value.timeToDetonate -= delta;
                if (bomb.Value.timeToDetonate <= 0)
                {
                    SetTilesOnFire(bomb.Key, bomb.Value.playerID); // bomb.Value.strength
                    bombs.Remove(bomb.Key);
                }
            }
        }
        catch (Exception e)
        {
            GD.Print($"???: {e}, {bombs.Count}");
        }
        

        for (int i = fires.Count - 1; i >= 0; i--)
        {
            var fire = fires[i];
            fire.timeToDisappear -= delta;
            if (fire.timeToDisappear <= 0)
            {
                foreach(var pos in fire.positions)
                {
                    HandleFireRemoval(pos);
                }

                fires.RemoveAt(i);
            }
        }

        for (int i = 0; i < playerCount; i++)
        {
            var lastPos = lastPlayerPositions[i];
            UpdatePlayerCell(lastPos, 0);
        }

        for (int i = 0; i < playerCount; i++)
        {
            //UpdatePlayerCell(playerID);
            var player = players[i];
            if (player.Lives == 0)
                continue;
            //var lastPos = lastPlayerPositions[i];
            var pos = GetGridPosition(player.Position);
            //if (lastPos == pos)
            //    continue;

            //if(lastPos.X != int.MaxValue)
            //    UpdatePlayerCell(lastPos, 0);
            UpdatePlayerCell(pos, player.GetID());
            lastPlayerPositions[i] = pos;
        }

        //printDelta += delta;
        //if (printDelta >= printSpeed)
        //{
        //    string message = "";

        //    for (int y = 0; y < arenaSize; y++)
        //    {
        //        for (int x = 0; x < arenaSize; x++)
        //        {
        //            message += playerMapSensor[x + y * arenaSize];
        //        }

        //        message += " ";

        //        for (int x = 0; x < arenaSize; x++)
        //        {
        //            message += mapSensor[x + y * arenaSize];
        //        }

        //        message += "\n";
        //    }

        //    GD.Print(message);
        //    printDelta = 0;
        //}


    }

    //void SpawnPlayer(Vector3 pos)
    //{

    //}

    void StartGame()
    {
        bombs.Clear();
        fireCounts.Clear();
        fires.Clear();

        Vector3I pos = new Vector3I();
        for (int z = -arenaOffset + 1; z < -arenaOffset + arenaSize - 1; z++)
        {
            for (int x = -arenaOffset + 1; x < -arenaOffset + arenaSize - 1; x++)
            {
                pos.X = x;
                pos.Z = z;
                UpdateMapCell(pos, -1);
                //UpdatePlayerCell(pos, 0);
                //gridmap.SetCellItem(pos, -1);
            }
        }

        for (int i = 0; i < 30; i++)
        {
            //UpdateMapCell(GetRandomInnerCell(), GridIndexes.destructibleWall);
            //UpdateMapCell(GetRandomInnerCell(), GridIndexes.indestructibleWall);
            var cell = GetRandomInnerCell();
            //for (int zz = -1; zz <= 1; zz++)
            //{
            //    for (int xx = -1; xx <= 1; xx++)
            //    {
            //        var newCell = cell + new Vector3I(xx, 0, zz);
            //        if(IsInnerCell(newCell))
            //            UpdateMapCell(newCell, GridIndexes.collectible1);
            //    }
            //}
            UpdateMapCell(cell, GridIndexes.collectible1);
        }

        alivePlayerCount = playerCount;
        for (int i = 0; i < playerCount; i++)
        {
            var player = players[i];
            //if (players[i].Lives > 0)
            //{

            //}
            
            //player.Despawn();
            
            
            //players[i].Spawn(new Vector3(1, 0, 1) * (i+1)*5 - new Vector3(7, 0, 7));
            Vector3 playerPos;
            do {
                playerPos = GetRandomInnerCell();
            } while(GetObjectOnCell(playerPos) != -1);
            //var cellPos = GetGridPosition(playerPos);

            player.Spawn(playerPos);
            //lastPlayerPositions[i] = cellPos;
            //UpdatePlayerCell(cellPos, player.TeamID);

            //UpdatePlayerCell(cellPos, players[i].TeamID);
            //GD.Print("Pos:" +  players[i].Position);
        }


        //UpdateMapCell(GetRandomInnerCell(), GridIndexes.collectible1);

    }

    public void OnPlayerHit(int teamID)
    {
        for (int i = 0; i < playerCount; i++)
        {
            var currentPlayer = players[i];
            if (currentPlayer.TeamID != teamID)
            {
                currentPlayer.OnEnemyTeamHit();
            }
            else
            {
                currentPlayer.OnTeamHit();
            }
        }
    }

    //public void OnPlayerHit(Character player)
    //{
    //    for(int i = 0;i < playerCount;i++) {
    //        var currentPlayer = players[i];
    //        if (player == currentPlayer)
    //        {
    //            currentPlayer.AddReward(-playerCount);
    //        }
    //        else
    //        {
    //            currentPlayer.AddReward(1);
    //        }
    //    }
    //}

    public void OnPlayerDeath(int playerID)
    {
        //UpdatePlayerCell(GetGridPosition(players[playerID].Position), 0);
        alivePlayerCount--;
        if(alivePlayerCount <= 1)
        {
            StartGame();
        }
    }

    public void ForceEndGame()
    {
        for (int i = 0; i < playerCount; i++)
        {
            var player = players[i];
            //if(!(bool)player.Get(aiPropertyName.done))
                player.Despawn();
        }

        StartGame();
        
    }

    void UpdateMapCell(Vector3I pos, int what)
    {
        gridmap.SetCellItem(pos, what);

        float valueToAdd = 0;

        switch (what)
        {
            case -1:
                valueToAdd = 0;
                break;
            case GridIndexes.collectible1:
                valueToAdd = 1;
                break;
            case GridIndexes.indestructibleWall:
                valueToAdd = 0.1f; break;
            case GridIndexes.destructibleWall:
                valueToAdd = 0.2f;
                break;

        }

        //var value = mapSensor[pos.Z + arenaOffset][pos.X + arenaOffset];
        //value[0] = what + 1;
        //mapSensor[pos.Z + arenaOffset][pos.X + arenaOffset] = value;

        int index = (pos.Z + arenaOffset) * arenaSize + pos.X + arenaOffset;
        mapSensor[index] = valueToAdd;
    }

    //void UpdatePlayerCell(int playerID)
    //{
    //    var player = players[playerID];
    //    var lastPlayerPos = lastPlayerPositions[playerID];
    //    var playerPos = GetGridPosition(player.Position);

    //    if (playerPos == lastPlayerPos)
    //        return;

    //    var value = mapSensor[playerPos.Z + arenaOffset][playerPos.X + arenaOffset];
    //    value[1] = player.TeamID;
    //    mapSensor[playerPos.Z + arenaOffset][playerPos.X + arenaOffset] = value;


    //    if (lastPlayerPos.X == int.MaxValue)
    //        return;

    //    value = mapSensor[lastPlayerPos.Z + arenaOffset][lastPlayerPos.X + arenaOffset];
    //    value[1] = 0;
    //    mapSensor[lastPlayerPos.Z + arenaOffset][lastPlayerPos.X + arenaOffset] = value;
    //}

    void UpdatePlayerCell(Vector3I pos, float ID)
    {
        //var value = mapSensor[pos.Z + arenaOffset][pos.X + arenaOffset];
        //value[1] = teamID;
        //mapSensor[pos.Z + arenaOffset][pos.X + arenaOffset] = value;

        int index = (pos.Z + arenaOffset) * arenaSize + pos.X + arenaOffset;
        if(index >= playerMapSensor.Count || index < 0)
        {
            GD.PrintErr($"Error :: {index}, {pos}");
            ForceEndGame(); // asi?
        }
        else
        {
            playerMapSensor[index] = ID;
        }
        
    }

    void AddBomb(Vector3I pos, int playerIndex)
    {
        gridmap.SetCellItem(pos, GridIndexes.bomb);

        //var value = mapSensor[pos.Z + arenaOffset][pos.X + arenaOffset];
        //value[0] = 8 + players[playerID].BombStrength;
        //mapSensor[pos.Z + arenaOffset][pos.X + arenaOffset] = value;

        int index = (pos.Z + arenaOffset) * arenaSize + pos.X + arenaOffset;
        playerMapSensor[index] = 8 + players[playerIndex].BombStrength;
    }



    void SetTilesOnFire(Vector3I pos, int playerID)
    {
        Character player = players[playerID];
        int bombStrength = player.BombStrength;
        player.SpawnedBombs--;

        Fire fire = new();
        fire.timeToDisappear = fireDuration;

        HandleFireInsertion(pos,fire);
        foreach (Vector3I direction in directions)
        {
            for(int i = 1; i <= bombStrength; i++)
            {
                Vector3I newPos = pos + direction * i;

                int item = gridmap.GetCellItem(newPos);
                if(item == GridIndexes.bomb)
                {
                    SetTilesOnFire(newPos, bombs[newPos].playerID); //  bombs[newPos].strength
                    bombs.Remove(newPos);
                    break;
                }
                if(item == GridIndexes.destructibleWall)
                {
                    HandleFireInsertion(newPos, fire);
                    break;
                }
                else if (item != GridMap.InvalidCellItem && item != GridIndexes.fire)
                {
                    break;
                }

                HandleFireInsertion(newPos, fire);

            }
        }
        fires.Add(fire);
    }
    void HandleFireInsertion(Vector3I pos, Fire fire)
    {
        if (!fireCounts.TryGetValue(pos, out var count))
        {
            fireCounts[pos] = 1;
            UpdateMapCell(pos, GridIndexes.fire);
            //gridmap.SetCellItem(pos, GridIndexes.fire);
        }
        else
        {
            fireCounts[pos] += 1;
        }

        fire.positions.Add(pos);
    }

    void HandleFireRemoval(Vector3I pos)
    {
        fireCounts[pos] -= 1;
        if (fireCounts[pos] == 0)
        {
            fireCounts.Remove(pos);
            UpdateMapCell(pos, -1);
            //gridmap.SetCellItem(pos, (int)GridMap.InvalidCellItem);
        }
    }
    static public Vector3I GetGridPosition(Vector3 position)
    {
        return new Vector3I(
            (int)(position.X + 0.5f * (position.X > 0 ? 1 : -1)),
            (int)position.Y,
             (int)(position.Z + 0.5f * (position.Z > 0 ? 1 : -1))
        );
    }

    class Bomb
    {
        public double timeToDetonate;
        public int playerID = 5;

        public Bomb(double timeToDetonate, int playerID)
        {
            this.timeToDetonate = timeToDetonate;
            this.playerID = playerID;
        }
    }

    class Fire
    {
        public List<Vector3I> positions = new();
        public double timeToDisappear;
    }
}
