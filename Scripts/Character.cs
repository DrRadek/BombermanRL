using Godot;
using System;

public partial class Character : CharacterBody3D
{
    [Export]
    Node3D aiController;

    [Export]
    MeshInstance3D mesh;

    [Export]
    CollisionShape3D collider;

    [Export]
    int teamID = 0;

    [Export]
    int myID;

    [Export]
    bool isPlayer = false;

    public int playerIndex;

    protected GameManager gameManager;
    StandardMaterial3D material;

    Vector3 posChange = Vector3.Zero;

    // character speed
    const float speed = 4.0f * 2f;

    bool isHuman = false;
    bool waitAfterSpawn = true;

    int defaultMaxSpawnedBombs = 3;
    int defaultBombStrength = 3;

    int defaultMaxLives = 3;
    int lives = 0;

    // invulnerability info
    const float flickerSpeed = 0.2f * 0.5f;
    bool flickerFrame = false;
    bool isInvulnerable = false;
    double flickerDelta = 0;
    double invulnerabilityTime = 0f;
    double invulnerabilityTimeDefault = 2f;

    // bomb info
    const double bombCooldownAfterSpawn = 0.75f;
    const double bombCooldownAfterUse = 0.5f;
    int maxSpawnedBombs = 0;
    int spawnedBombs = 0;
    int bombStrength = 0;
    double maxBombCooldown = Mathf.Max(bombCooldownAfterUse, bombCooldownAfterSpawn);
    double bombDelta = bombCooldownAfterSpawn;
    double timeWithoutUsingBomb = 0;

    public int Lives { get => lives; private set => lives = value; }
    public double InvulnerabilityTime { get => invulnerabilityTime; set => invulnerabilityTime = value; }
    public int SpawnedBombs { get => spawnedBombs; set => spawnedBombs = value; }
    public int BombStrength { get => bombStrength; }
    public int TeamID { get => teamID; }
    public int DefaultMaxSpawnedBombs { get => defaultMaxSpawnedBombs; protected set => defaultMaxSpawnedBombs = value; }
    public int DefaultBombStrength { get => defaultBombStrength; protected set => defaultBombStrength = value; }
    public Vector3 PosChange { get => posChange; }
    public bool IsHuman { get => isHuman; private set => isHuman = value; }
    public bool IsDead { get => lives == 0; }
    private bool IsInvulnerable { get => isInvulnerable; set => isInvulnerable = value; }
    private int MaxSpawnedBombs { get => maxSpawnedBombs; set => maxSpawnedBombs = value; }
    protected double TimeWithoutUsingBomb { get => timeWithoutUsingBomb; private set => timeWithoutUsingBomb = value; }
    public int DefaultMaxLives { get => defaultMaxLives;protected set => defaultMaxLives = value; }

    Vector3 GetLocalPlayerPos()
    {
        return Position - GameManager.GetGridPosition(Position);
    }
    float GetNormalizedLives()
    {
        return lives / (float)defaultMaxLives;
    }
    public Godot.Collections.Array<float> GetObs()
    {
        Godot.Collections.Array<float> obs = new();
        obs.Resize(7);
        if (!IsDead)
        {
            Vector3 pos = GetLocalPlayerPos();
            obs[0] = pos.X;
            obs[1] = pos.Z;
            obs[2] = posChange.X;
            obs[3] = posChange.Z;
            obs[4] = GetNormalizedLives();
            obs[5] = (float)(invulnerabilityTime / invulnerabilityTimeDefault);
            obs[6] = (spawnedBombs == maxSpawnedBombs) ? (float)maxBombCooldown : (float)(bombDelta / maxBombCooldown);
        }
        else
        {
            obs.Fill(0);
        }
        return obs;
    }
    //
    float lastLowestDistanceFromEnemies = float.MaxValue;
    float lowestDistanceFromEnemies = float.MaxValue;

    public void CheckDistanceFromEnemies()
    {
        if(lowestDistanceFromEnemies < lastLowestDistanceFromEnemies)
            OnGetsCloserToEnemy();

        lastLowestDistanceFromEnemies = lowestDistanceFromEnemies;
        lowestDistanceFromEnemies = float.MaxValue;
    }

    public virtual void OnGetsCloserToEnemy()
    {

    }
    //\
    public Godot.Collections.Array<float> GetEnemyObs(Character character)
    {
        Godot.Collections.Array<float> obs = new();
        obs.Resize(8);
        if (!IsDead)
        {
            Vector3 pos = GetLocalPlayerPos();
            Vector3 direction = (character.Position - Position) / GameManager.arenaSize;
            direction.Y = 0;
            lowestDistanceFromEnemies = Math.Min(lowestDistanceFromEnemies, direction.Length());
            //direction = direction.Normalized();

            obs[0] = pos.X;
            obs[1] = pos.Z;
            obs[2] = posChange.X;
            obs[3] = posChange.Z;
            obs[4] = GetNormalizedLives();
            obs[5] = (float)(invulnerabilityTime / invulnerabilityTimeDefault);
            obs[6] = direction.X;
            obs[7] = direction.Z;
        }
        else
        {
            obs.Fill(0);
        }
        return obs;
    }

    public override void _Ready()
    {
        OnDefaultValuesSet();
        gameManager = GetParent<GameManager>();
        aiController?.Call(aiMethodName.init, this);

        material = (StandardMaterial3D)mesh.GetActiveMaterial(0);

    }
    public override void _PhysicsProcess(double delta)
    {
        if (IsDead)
            return;

        Vector2 inputDir = Vector2.Zero;
        bool bombInput = false;
        if (aiController != null)
        {
            if ((bool)aiController.Get(aiPropertyName.needsReset))
                return;

            if (!isHuman)
            {
                bombInput = (int)aiController.Get(aiPropertyName.placeBomb) == 1;
                inputDir = new Vector2((float)aiController.Get(aiPropertyName.moveX), (float)aiController.Get(aiPropertyName.moveY));
            }
            else if (isPlayer || (isHuman))
            {
                bombInput = Input.IsActionJustPressed("PlaceBomb");
                inputDir = Input.GetVector("LEFT", "RIGHT", "UP", "DOWN");
            }
        }

        if (bombInput)
        {
            timeWithoutUsingBomb = 0;
            if (isInvulnerable || spawnedBombs >= maxSpawnedBombs || bombDelta > 0)
            {
                OnBombFailedToPlace();
            }
            else
            {
                if (!gameManager.PlaceBomb(Position, playerIndex, out float rating))
                {
                    OnBombFailedToPlace();
                }
                else
                {
                    OnBombPlaced(rating);
                    bombDelta = bombCooldownAfterUse;
                    spawnedBombs++;
                }
            }
        }
        else
        {
            timeWithoutUsingBomb += delta;
        }

        Vector3 velocity = Velocity;
        Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized() * (Mathf.Min(inputDir.Length(), 1));

        if (direction != Vector3.Zero)
        {
            velocity.X = direction.X * speed;
            velocity.Z = direction.Z * speed;
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, speed);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, speed);
        }
        Velocity = velocity;
        var pos = Position;

        if (!waitAfterSpawn)
            MoveAndSlide();
        waitAfterSpawn = false;

        posChange = Position - pos;

        if (Velocity.LengthSquared() <= Mathf.Epsilon * 10 && inputDir != Vector2.Zero)
        {
            OnTriedToRunIntoObject();
        }

        if (isInvulnerable)
        {
            flickerDelta += delta;
            var color = material.AlbedoColor;
            if (flickerDelta >= flickerSpeed)
            {
                flickerFrame = !flickerFrame;
                if (flickerFrame)
                {
                    color.A = 0.5f;
                }
                else
                {
                    color.A = 1f;
                }
                flickerDelta = 0;
            }

            invulnerabilityTime -= delta;
            if (invulnerabilityTime <= 0)
            {
                invulnerabilityTime = 0;
                isInvulnerable = false;
                flickerFrame = false;
                flickerDelta = 0;
                color.A = 1f;
            }
            material.AlbedoColor = color;
        }

        switch (gameManager.GetObjectInCell(Position))
        {
            case GameManager.GridIndexes.fire:
                HandleFireHit();
                break;
            case GameManager.GridIndexes.collectible1:
                OnCollectibleCollected(0);

                break;

            default: break;
        }

        bombDelta = Mathf.Max(bombDelta - delta, 0);
    }
    public virtual void Spawn(Vector3 pos)
    {
        if (aiController != null)
        {
            isHuman = (String)aiController.Get(aiPropertyName.heuristic) == "human";
            aiController.Call(aiMethodName.reset);
        }
        else
        {
            isHuman = true;
        }

        lives = defaultMaxLives;

        Position = new Vector3(pos.X, Position.Y, pos.Z);
        Velocity = Vector3.Zero;
        collider.Disabled = false;
        Visible = true;

        maxSpawnedBombs = defaultMaxSpawnedBombs;
        bombStrength = defaultBombStrength;
        spawnedBombs = 0;
        invulnerabilityTime = 0;

        isInvulnerable = false;
        flickerFrame = false;
        flickerDelta = 0;
        bombDelta = bombCooldownAfterSpawn;
        timeWithoutUsingBomb = 0;

        waitAfterSpawn = true;

        lastLowestDistanceFromEnemies = float.MaxValue;
        lowestDistanceFromEnemies = float.MaxValue;
    }
    public void Despawn()
    {
        if (aiController != null)
        {
            aiController.Set(aiPropertyName.done, true);
            aiController.Set(aiPropertyName.needsReset, true);
        }

        collider.Disabled = true;
        Visible = false;
        lives = 0;
        Velocity = Vector3.Zero;
    }
    public bool NeedsReset()
    {
        if (aiController != null)
        {
            return (bool)aiController.Get(aiPropertyName.needsReset);
        }
        else
        {
            return true;
        }
    }
    public void AddReward(float reward)
    {
        if (aiController != null)
        {
            aiController.Set(aiPropertyName.reward, (float)aiController.Get(aiPropertyName.reward) + reward);
        }

    }
    void HandleFireHit()
    {
        if (!isInvulnerable)
        {
            lives -= 1;

            isInvulnerable = true;
            flickerFrame = true;
            flickerDelta = flickerSpeed;

            invulnerabilityTime = invulnerabilityTimeDefault;

            if (IsDead)
            {
                Despawn();
                OnDeath();
                gameManager.OnPlayerDeath(playerIndex);
            }

            gameManager.OnPlayerHit(teamID, Position);
        }

    }

    public virtual void OnTeamHit()
    {
        // AddReward(-0.01f);
    }
    public virtual void OnEnemyTeamHit()
    {
        // AddReward(0.01f);
    }
    public virtual void OnTeamWin()
    {
        // AddReward(0.01f);
    }
    public virtual void OnWallDestroyed()
    {
        // AddReward(0.01f);
    }
    public virtual void OnDangerousTileTouched(float strength)
    {
        // AddReward(0.01f);
    }
    public virtual void OnNormalTileTouched()
    {
        // AddReward(0.01f);
    }
    protected virtual void OnBombPlaced(float rating)
    {

    }
    protected virtual void OnTriedToRunIntoObject()
    {
        // AddReward(0.01f);
    }
    protected virtual void OnBombFailedToPlace()
    {
        // AddReward(-0.01f);
    }
    protected virtual void OnCollectibleCollected(int index)
    {
        //AddReward(1);
    }
    protected virtual void OnDefaultValuesSet()
    {

    }
    protected virtual void OnDeath()
    {

    }
}

class aiMethodName
{
    public static readonly StringName init = "init";
    public static readonly StringName reset = "reset";
}

class aiPropertyName
{
    public static readonly StringName needsReset = "needs_reset";
    public static readonly StringName heuristic = "heuristic";
    public static readonly StringName moveX = "move_x";
    public static readonly StringName moveY = "move_y";
    public static readonly StringName placeBomb = "place_bomb";
    public static readonly StringName done = "done";
    public static readonly StringName reward = "reward";
}