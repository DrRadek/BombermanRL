using Godot;
using System;
using System.Collections.Generic;

public partial class DecisionTreeAgent : Character
{
    bool isMoving = false;
    Vector2 inputDir = Vector2.Zero;
    Vector3 moveEndPos = Vector3I.Zero;

    Random random = new Random();
    float bombChance = 0.01f;
    float bombChanceWhenInDanger = 0.05f;
    float blockBombChance = 0.2f;
    float enemyBombChance = 0.5f;

    public override void Spawn(Vector3 pos)
    {
        base.Spawn(pos);
        isMoving = false;
    }

    protected override void GetInput(out Vector2 inputDir, out bool bombInput)
    {
        // Make sure to place a bomb before the timer runs out
        if (TimeWithoutUsingBomb > MaxTimeWithoutUsingBomb * 0.95f)
        {
            PlaceBomb();
        }

        // for debug
        if (gameManager.VisualizeObs)
            gameManager.GetObservationsAroundPlayer(playerIndex, playerIndex);

        inputDir = Vector2.Zero;
        bombInput = false;


        if (isMoving)
        { // TODO: The agent sometimes gets stuck
            Move(out inputDir);
            return;
        }
        

        var gridPos = GameManager.GetGridPosition(Position);


        if (IsTileDangerous(gridPos))
        {
            // Try to move to a safe place, otherwise do a random move
            Vector3I? nearestSafeCell;
            var safeCellDirection = gameManager.FindDirectionToNearestSafeCell(gridPos, playerIndex, out nearestSafeCell);
            if (nearestSafeCell.HasValue)
            {
                //if(!IsTileDangerous((Vector3I)safeCellDirection, GameManager.bombDetonationSpeed * 0.1f))
                SetMove((Vector3)safeCellDirection, out inputDir);
            }
            else
            {
                DoRandomMove(out inputDir, out bombInput, bombChanceWhenInDanger, gridPos);
            }
            return;
        }

        // Check if there is something to hit with a bomb
        float localBombChance = 0;
        var things = gameManager.GetThingsInBombRadius(gridPos, BombStrength, TeamID);
        foreach (var thing in things)
        {

            if (thing == GameManager.GridIndexes.enemyPlayer)
            {
                localBombChance = enemyBombChance;
                break;
            }
            else if(thing == GameManager.GridIndexes.destructibleWall)
            {
                localBombChance = blockBombChance;
            } 
        }

        bool placeBomb = random.NextDouble() < localBombChance;

        // Can place a bomb that hits something useful OR the bomb will explode soon?
        if (TimeWithoutUsingBomb > MaxTimeWithoutUsingBomb * 0.5f || placeBomb)
        {
            // Place it safely, otherwise do a random move
            PlaceSafeBombOrDoRandomMove(out inputDir, out bombInput, gridPos);
            return;
        }

        // Try to move closer to the player
        var directionToClosestPlayer = GetDirectionToClosestPlayer(gridPos);
        if(directionToClosestPlayer == Vector3I.Zero)
        {
            DoRandomMove(out inputDir, out bombInput, 0, gridPos);
        }
        else
        {
            SetMove(Position + GetDirectionToClosestPlayer(gridPos), out inputDir);
        }
        
    }

    void SetMove(Vector3 endPos, out Vector2 inputDir)
    {
        moveEndPos = endPos;
        isMoving = true;
        Move(out inputDir);
       
    }
    void Move(out Vector2 inputDir)
    {
        Vector2 direction = new Vector2(moveEndPos.X, moveEndPos.Z) - new Vector2(Position.X, Position.Z);

        this.inputDir = direction.Normalized();
        inputDir = this.inputDir;

        if (direction.LengthSquared() < 0.02)
            isMoving = false;
        
    }

    protected override void OnTriedToRunIntoObject()
    {
        isMoving = false;
        Velocity = -Velocity;
    }

    bool IsTileDangerous(Vector3I gridPos, float maxBombDetonationTime = GameManager.bombDetonationSpeed)
    {
        var bombValue = gameManager.GetBombValue(gameManager.GetGridIndex(gridPos));
        return (bombValue > 0 && bombValue <= maxBombDetonationTime)
            || gameManager.GetObjectInCell(gridPos) == GameManager.GridIndexes.fire;
    }
    bool IsTileEmpty(Vector3I gridPos)
    {
        return gameManager.GetObjectInCell(gridPos) == GameManager.GridIndexes.empty;
    }
    bool IsTileEmptyAndNotDangerous(Vector3I gridPos)
    {
        return !IsTileDangerous(gridPos) && IsTileEmpty(gridPos);
    }

    bool CanSafelyPlaceBomb()
    {
        // TODO: implement
        return true;
    }

    void DoRandomMove(out Vector2 inputDir, out bool bombInput, float bombChance, Vector3I gridPos)
    {
        bombInput = random.NextDouble() < bombChance;
        inputDir = Vector2.Zero;
        var choice = random.Next(0, 5);
        if(choice != 4)
        {
            var direction = GameManager.directions[choice];
            var pos = gridPos + direction;
            if (IsTileEmptyAndNotDangerous(pos))
            {
                SetMove(pos, out inputDir);
            }
        }
       
    }
    void PlaceSafeBombOrDoRandomMove(out Vector2 inputDir, out bool bombInput, Vector3I gridPos)
    {
        if (CanSafelyPlaceBomb())
        {
            bombInput = true;
            inputDir = Vector2.Zero;
        }
        else
        {
            DoRandomMove(out inputDir, out bombInput, bombChance, gridPos);
        }
    }

    Vector3I GetDirectionToClosestPlayer(Vector3I gridPos)
    {
        //Vector2I gridpos2D = new Vector2I(gridPos.X, gridPos.Z);
        Character closestPlayer = null;
        float closestPlayerDistance = float.PositiveInfinity;

        foreach (var player in gameManager.Players)
        {
            if (player.playerIndex == playerIndex || player.IsDead)
                continue;

            var distance = (Position - player.Position).LengthSquared();
            if (distance < closestPlayerDistance)
            {
                closestPlayerDistance = distance;
                closestPlayer = player;
            }
        }
        if (closestPlayer == null)
            return Vector3I.Zero;


        int directionX = Math.Clamp((int)(closestPlayer.Position.X - Position.X), -1, 1);
        int directionZ = Math.Clamp((int)(closestPlayer.Position.Z - Position.Z), -1, 1);

        Vector3I pos;
        Vector3I direction;
        List<Vector3I> primaryDirections = new();
        List<Vector3I> secondaryDirections = new();
        if (directionX != 0 && directionZ != 0)
        {
            direction =new Vector3I(directionX, 0, 0);
            primaryDirections.Add(direction);
            secondaryDirections.Add(-direction);

            direction = new Vector3I(0, 0, directionZ);
            primaryDirections.Add(direction);
            secondaryDirections.Add(-direction);

        }
        else if(directionX != 0)
        {
            direction = new Vector3I(directionX, 0, 0);
            primaryDirections.Add(direction);
            secondaryDirections.Add(-direction);

            direction =  new Vector3I(0, 0, 1);
            secondaryDirections.Add(direction);
            secondaryDirections.Add(-direction);
        }
        else if (directionZ != 0)
        {
            direction = new Vector3I(0, 0, directionZ);
            primaryDirections.Add(direction);
            secondaryDirections.Add(-direction);

            direction = new Vector3I(1, 0, 0);
            secondaryDirections.Add(direction);
            secondaryDirections.Add(-direction);
        }

        while(primaryDirections.Count > 0)
        {
            var randomIndex = random.Next(primaryDirections.Count);

            direction = primaryDirections[randomIndex];
            pos = gridPos + direction;
            if (IsTileEmptyAndNotDangerous(pos))
            {
                return direction;
            }

            primaryDirections.RemoveAt(randomIndex);
        }

        while (secondaryDirections.Count > 0)
        {
            var randomIndex = random.Next(secondaryDirections.Count);

            direction = secondaryDirections[randomIndex];
            pos = gridPos + direction;
            if (IsTileEmptyAndNotDangerous(pos))
            {
                return direction;
            }

            secondaryDirections.RemoveAt(randomIndex);
        }

        return Vector3I.Zero;
    }
}
