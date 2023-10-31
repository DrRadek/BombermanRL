using Godot;
using System;
using System.Drawing;

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
    bool isPlayer = false;

    NodePath gameManagerPath = "../Game";
    protected GameManager gameManager;
    StandardMaterial3D material;
    
    //float bombCoolDown = 0f;
    //float defaultBombCoolDown = 0f;

	const float speed = 10.0f;
    const float flickerSpeed = 0.2f;
    const int maxLives = 3;
    int defaultMaxSpawnedBombs = 0;
    int defaultBombStrength = 1;
    double flickerDelta = 0;
    bool flickerFrame = false;

    int lives = maxLives;
    //bool isAlive = true;
    bool isInvulnerable = false;
    double invulnerabilityTime = 0f;
    float invulnerabilityTimeDefault = 2f;

    int myID;
    int maxSpawnedBombs = 0; // TODO: Add to AI parameters
    int spawnedBombs = 0; // TODO: Add to AI parameters
    int bombStrength = 0; // TODO: Add to AI parameters

    public int Lives { get => lives; private set => lives = value; }
    //public bool IsAlive { get => isAlive; private set => isAlive = value; }
    private bool IsInvulnerable { get => isInvulnerable; set => isInvulnerable = value; }
    public double InvulnerabilityTime { get => invulnerabilityTime; set => invulnerabilityTime = value; }
    private int MaxSpawnedBombs { get => maxSpawnedBombs; set => maxSpawnedBombs = value; }
    public int SpawnedBombs { get => spawnedBombs; set => spawnedBombs = value; }
    public int BombStrength { get => bombStrength; private set => bombStrength = value; }
    public int TeamID { get => teamID; private set => teamID = value; }
    public int MyID { get => myID; set => myID = value; }
    public int DefaultMaxSpawnedBombs { get => defaultMaxSpawnedBombs; protected set => defaultMaxSpawnedBombs = value; }
    public int DefaultBombStrength { get => defaultBombStrength; protected set => defaultBombStrength = value; }

    bool isAiInit = false;

    public override void _Ready()
    {
        OnDefaultValuesSet();
        gameManager = (GameManager)GetTree().CurrentScene;
        material = (StandardMaterial3D)mesh.GetActiveMaterial(0);
        //collider.Disabled = true;
        //Visible = false;
        //lives = 0;
        aiController?.Call(aiMethodName.init, this);
        Despawn();
    }





    public override void _PhysicsProcess(double delta)
	{
        //AddReward(-0.0001f); // time reward

        if (Lives == 0)
            return;



        Vector2 inputDir = Vector2.Zero;
        bool bombInput = false;
        if(aiController != null)
        {
            if ((bool)aiController.Get(aiPropertyName.needsReset))
            {
                gameManager.ForceEndGame();
                return;
            }

            if ((String)aiController.Get(aiPropertyName.heuristic) != "human")
            {
                bombInput = (int)aiController.Get(aiPropertyName.placeBomb) == 1;
                inputDir = new Vector2((float)aiController.Get(aiPropertyName.moveX), (float)aiController.Get(aiPropertyName.moveY)).Normalized();
            }
        }

        if (isPlayer || (aiController != null && (String)aiController.Get(aiPropertyName.heuristic) == "human"))
        {
            bombInput = Input.IsActionJustPressed("PlaceBomb");
            inputDir = Input.GetVector("LEFT", "RIGHT", "UP", "DOWN");
        }

        if (bombInput)
        {
            if (isInvulnerable || spawnedBombs >= maxSpawnedBombs)
            {
                OnBombFailedToPlace();
            }
            else
            {
                if (!gameManager.PlaceBomb(Position, MyID))
                {
                    OnBombFailedToPlace();
                }
                else
                {
                    OnBombPlaced();
                    spawnedBombs++;
                }
            }
        }


        Vector3 velocity = Velocity;
        Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized(); ;



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

        MoveAndSlide();

        if (Velocity.LengthSquared() <= Mathf.Epsilon * 10 && inputDir != Vector2.Zero)
        {
            OnTriedToRunIntoObject();
        }

        if (isInvulnerable)
        {
            flickerDelta += delta;
            var color = material.AlbedoColor;
            if(flickerDelta >= flickerSpeed)
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
            if(invulnerabilityTime <= 0)
            {
                isInvulnerable = false;
                flickerFrame = false;
                flickerDelta = 0;
                color.A = 1f;
            }
            material.AlbedoColor = color;
        }

        switch (gameManager.GetObjectOnCell(Position))
        {
            case GameManager.GridIndexes.fire:
                HandleFireHit();
                break;
            case GameManager.GridIndexes.collectible1:
                OnCollectibleCollected(0);
                
                break;


            default: break;
        }
    }


    void HandleFireHit()
    {
        if (!isInvulnerable)
        {
            lives -= 1;
            GD.Print("Hit");

            isInvulnerable = true;
            flickerFrame = true;
            flickerDelta = flickerSpeed;

            invulnerabilityTime = invulnerabilityTimeDefault;

            if (lives == 0)
            {
                GD.Print("Death");
                Despawn();
                gameManager.OnPlayerDeath(myID);
                //GD.Print("Game Over for you");

                //return;
            }
            gameManager.OnPlayerHit(teamID);
            //OnTeamHit();

        }
        
    }

    protected virtual void OnBombPlaced()
    {
        // AddReward(0.01f);
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
    public virtual void OnTeamHit()
    {
        // AddReward(-0.01f);
    }
    public virtual void OnEnemyTeamHit()
    {
        // AddReward(0.01f);
    }


    public virtual void Spawn(Vector3 pos)
    {
        //if (!isAiInit)
        //{
        //    GD.Print("true");
        //    aiController.Call(aiMethodName.init, this);
        //    isAiInit = true;
        //}
        if(aiController != null)
            aiController.Call(aiMethodName.reset);

        //SetProcess(true);

        //color.A = 1f;

        lives = maxLives;
        Velocity = Vector3.Zero;
        Position = pos;
        collider.Disabled = false;
        Visible = true;
        maxSpawnedBombs = defaultMaxSpawnedBombs;
        bombStrength = defaultBombStrength;
        spawnedBombs = 0;

        isInvulnerable = false;
        flickerFrame = false;
        flickerDelta = 0;
    }

    public void Despawn()
    {
        if(aiController != null)
        {
            aiController.Set(aiPropertyName.done, true);
            //aiController.Set(aiPropertyName.needsReset, true);
        }
        
        //SetProcess(false);
        collider.Disabled = true;
        Visible = false;
        lives = 0;
        Velocity = Vector3.Zero;
    }

    public void AddReward(float reward)
    {
        if(aiController != null)
            aiController.Set(aiPropertyName.reward, (float)aiController.Get(aiPropertyName.reward) + reward);
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