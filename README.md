# WitShells Unity Packages

<p align="center">
  <strong>A collection of production-ready, reusable Unity packages</strong><br>
  <a href="https://witshells.com">witshells.com</a>
</p>

---

## 📦 Quick Install

All packages install via Unity Package Manager using Git URLs.

**How to Install:**
1. Open Unity → **Window** → **Package Manager**
2. Click **+** → **Add package from Git URL**
3. Copy a URL below and paste it, then click **Add**

---

### Design Patterns
```
https://github.com/Sulaiman281/Reusable-Unity-Scripts-Packages.git?path=Assets/WitShells/DesignPatterns
```

### Third Person Control
```
https://github.com/Sulaiman281/Reusable-Unity-Scripts-Packages.git?path=Assets/WitShells/ThirdPersonControl
```

### WitActor System
```
https://github.com/Sulaiman281/Reusable-Unity-Scripts-Packages.git?path=Assets/WitShells/WitActor
```

### Animation Rig
```
https://github.com/Sulaiman281/Reusable-Unity-Scripts-Packages.git?path=Assets/WitShells/WitAnimationRig
```

### WitMultiplayer
```
https://github.com/Sulaiman281/Reusable-Unity-Scripts-Packages.git?path=Assets/WitShells/WitMultiplayer
```

### WitClientApi
```
https://github.com/Sulaiman281/Reusable-Unity-Scripts-Packages.git?path=Assets/WitShells/WitClientApi
```

### Dialogs Manager
```
https://github.com/Sulaiman281/Reusable-Unity-Scripts-Packages.git?path=Assets/WitShells/DialogsManager
```

### Tank Controls
```
https://github.com/Sulaiman281/Reusable-Unity-Scripts-Packages.git?path=Assets/WitShells/TankControls
```

### Shooting System
```
https://github.com/Sulaiman281/Reusable-Unity-Scripts-Packages.git?path=Assets/WitShells/ShootingSystem
```

### Simple Vehicle Control
```
https://github.com/Sulaiman281/Reusable-Unity-Scripts-Packages.git?path=Assets/WitShells/SimpleVehicleControl
```

### Military Grid System
```
https://github.com/Sulaiman281/Reusable-Unity-Scripts-Packages.git?path=Assets/WitShells/MilitaryGridSystem
```

### Canvas Draw Tool
```
https://github.com/Sulaiman281/Reusable-Unity-Scripts-Packages.git?path=Assets/WitShells/CanvasDrawTool
```

### Spline Runtime
```
https://github.com/Sulaiman281/Reusable-Unity-Scripts-Packages.git?path=Assets/WitShells/SplineRuntime
```

### Threading Job
```
https://github.com/Sulaiman281/Reusable-Unity-Scripts-Packages.git?path=Assets/WitShells/ThreadingJob
```

### Broadcast (UDP)
```
https://github.com/Sulaiman281/Reusable-Unity-Scripts-Packages.git?path=Assets/WitShells/Broadcast
```

### WebRTC P2P
```
https://github.com/Sulaiman281/Reusable-Unity-Scripts-Packages.git?path=Assets/WitShells/WebRTC-Wit
```

### Live Microphone
```
https://github.com/Sulaiman281/Reusable-Unity-Scripts-Packages.git?path=Assets/WitShells/LiveMic
```

### Map View
```
https://github.com/Sulaiman281/Reusable-Unity-Scripts-Packages.git?path=Assets/WitShells/MapView
```

### SQLite Database
```
https://github.com/Sulaiman281/Reusable-Unity-Scripts-Packages.git?path=Assets/WitShells/SqLite
```

---

---

## 📚 Package Documentation

### 1. Design Patterns

Production-ready design pattern implementations optimized for Unity.

**Included Patterns:**
- Singleton (MonoBehaviour & ScriptableObject)
- Factory Pattern
- Observer Pattern
- Command Pattern
- State Machine
- Object Pooling

```csharp
// Singleton Example - Create a GameManager that persists across scenes
using WitShells.DesignPatterns;

public class GameManager : Singleton<GameManager>
{
    public int Score { get; private set; }
    
    protected override void Awake()
    {
        base.Awake(); // Handles DontDestroyOnLoad
    }
    
    public void AddScore(int points)
    {
        Score += points;
        // Access from anywhere: GameManager.Instance.AddScore(10);
    }
}
```

---

### 2. Third Person Control

Complete third-person character controller with New Input System and Cinemachine 3.0.

**Features:**
- Smooth locomotion (walk, run, crouch, sprint)
- Jump with ground detection
- Camera-relative movement
- ScriptableObject settings
- PlayerInput component integration

**Dependencies:** `com.unity.inputsystem`, `com.unity.cinemachine`

```csharp
// Setup via code or use the Editor menu: WitShells → ThirdPersonSetup
using WitShells.ThirdPersonControl;
using UnityEngine;

public class PlayerSetup : MonoBehaviour
{
    [SerializeField] private ThirdPersonSettings settings;
    
    void Start()
    {
        // The ThirdPersonControl reads settings from ScriptableObject
        var controller = GetComponent<ThirdPersonControl>();
        
        // Customize at runtime
        controller.Sprint = true;  // Enable sprint
        controller.Direction = new Vector2(1, 0);  // Move right
        controller.Jump = true;    // Trigger jump
    }
}
```

---

### 3. WitActor System

Advanced actor framework for AI characters with navigation and state management.

**Features:**
- NavMesh-based AI navigation
- Modular component system
- State machine integration
- Patrol, chase, and idle behaviors

```csharp
using WitShells.WitActor;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    private NavMeshAgent agent;
    private Transform player;
    
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float attackRange = 2f;
    
    void Update()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        
        if (distance <= attackRange)
        {
            // Attack state
            agent.isStopped = true;
            Attack();
        }
        else if (distance <= detectionRange)
        {
            // Chase state
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
        else
        {
            // Patrol state
            Patrol();
        }
    }
}
```

---

### 4. Animation Rig

Powerful Animation Rigging setup tools and constraint controllers for humanoid characters.

**Features:**
- Wizard-based rig setup with auto bone detection
- Humanoid avatar bone mapping support
- Constraint target controllers (head, hands, legs)
- Per-axis position/rotation constraints
- Smooth interpolation with configurable speed
- Runtime weight control
- Full Undo support

**Dependencies:** `com.unity.animation.rigging`

```csharp
using WitShells.AnimationRig;
using UnityEngine;

public class IKController : MonoBehaviour
{
    [SerializeField] private ConstraintTargetController constraintController;
    [SerializeField] private Transform lookAtTarget;
    [SerializeField] private Transform leftHandTarget;
    [SerializeField] private Transform rightHandTarget;
    
    void Start()
    {
        // Set head to look at a target
        constraintController.SetHeadLookAt(lookAtTarget);
        
        // Set hand targets (e.g., for holding objects)
        constraintController.SetHandTargets(leftHandTarget, rightHandTarget);
    }
    
    void Update()
    {
        // Dynamically adjust constraint weights
        float distance = Vector3.Distance(transform.position, lookAtTarget.position);
        constraintController.MasterWeight = Mathf.Clamp01(1f - distance / 10f);
        
        // Toggle position/rotation constraints independently
        constraintController.HeadTarget.ConstrainPosition = false; // Only rotation
        constraintController.LeftHandTarget.ConstrainRotation = true;
    }
}

// Setup via Editor: WitShells → Animation Rig → Rig Setup Wizard
// Or quick setup: WitShells → Animation Rig → Quick Rig Setup (Auto)
```

---

### 5. WitMultiplayer

Simplified Unity Gaming Services integration for lobbies, matchmaking, and Relay.

**Features:**
- Lobby creation and management
- Matchmaking with UGS Matchmaker
- Relay integration for NAT traversal
- In-lobby chat support

```csharp
using WitShells.WitMultiplayer;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

public class MultiplayerManager : MonoBehaviour
{
    private Lobby currentLobby;
    
    public async void CreateLobby(string lobbyName, int maxPlayers)
    {
        var options = new CreateLobbyOptions
        {
            IsPrivate = false,
            Data = new Dictionary<string, DataObject>
            {
                { "GameMode", new DataObject(DataObject.VisibilityOptions.Public, "TeamDeathmatch") }
            }
        };
        
        currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
        Debug.Log($"Lobby created: {currentLobby.LobbyCode}");
    }
    
    public async void JoinLobbyByCode(string code)
    {
        currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code);
        Debug.Log($"Joined lobby: {currentLobby.Name}");
    }
}
```

---

### 6. WitClientApi

Lightweight REST API client with JSON manifest support and built-in authentication.

**Features:**
- Define endpoints in JSON manifest
- Token-based authentication
- Thread-safe token storage
- Envelope-aware response parsing

```csharp
using WitShells.WitClientApi;
using UnityEngine;

public class ApiExample : MonoBehaviour
{
    private WitApiClient apiClient;
    
    void Start()
    {
        apiClient = new WitApiClient("https://api.example.com");
    }
    
    public async void Login(string username, string password)
    {
        var response = await apiClient.PostAsync<LoginResponse>("/auth/login", new
        {
            username,
            password
        });
        
        if (response.Success)
        {
            apiClient.SetAuthToken(response.Data.Token);
            Debug.Log("Logged in successfully!");
        }
    }
    
    public async void GetUserProfile()
    {
        var profile = await apiClient.GetAsync<UserProfile>("/user/profile");
        Debug.Log($"Welcome, {profile.Data.Name}!");
    }
}
```

---

### 7. Dialogs Manager

Comprehensive dialog and conversation system with typewriter effects.

**Features:**
- Conversation sequencing
- Typewriter text animation
- Audio playback per dialog
- Trigger-based activation
- New Input System support

**Dependencies:** `com.witshells.designpatterns`, `com.unity.textmeshpro`, `com.unity.inputsystem`

```csharp
using WitShells.DialogsManager;
using UnityEngine;

public class NPCDialog : MonoBehaviour
{
    [SerializeField] private DialogController dialogController;
    [SerializeField] private Conversation greetingConversation;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            dialogController.StartConversation(greetingConversation);
        }
    }
}

// Create conversations as ScriptableObjects with dialog lines,
// speaker names, audio clips, and events per line.
```

---

### 8. Tank Controls

Rigidbody-based tank controller with smooth turret handling.

**Features:**
- Smooth movement and turning
- Turret yaw and pitch control
- Configurable acceleration/deceleration
- Integration-ready for weapons

```csharp
using WitShells.TankControls;
using UnityEngine;

public class TankInput : MonoBehaviour
{
    [SerializeField] private TankController tank;
    [SerializeField] private Transform turret;
    
    void Update()
    {
        // Movement
        float moveInput = Input.GetAxis("Vertical");
        float turnInput = Input.GetAxis("Horizontal");
        tank.SetInput(moveInput, turnInput);
        
        // Turret aim at mouse position
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            tank.AimTurretAt(hit.point);
        }
        
        // Fire
        if (Input.GetButtonDown("Fire1"))
        {
            tank.Fire();
        }
    }
}
```

---

### 9. Shooting System

Modular weapon system with raycast and projectile modes.

**Features:**
- Raycast and projectile firing modes
- Trajectory preview/prediction
- Object pooling for projectiles
- Configurable weapon stats

```csharp
using WitShells.ShootingSystem;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [SerializeField] private Weapon weapon;
    [SerializeField] private TrajectoryPreview trajectoryPreview;
    
    void Update()
    {
        // Show trajectory preview while aiming
        if (Input.GetButton("Fire2"))
        {
            trajectoryPreview.ShowTrajectory(
                weapon.MuzzlePosition,
                weapon.MuzzleDirection,
                weapon.ProjectileSpeed
            );
        }
        else
        {
            trajectoryPreview.HideTrajectory();
        }
        
        // Fire weapon
        if (Input.GetButtonDown("Fire1"))
        {
            weapon.Fire();
        }
    }
}
```

---

### 10. Simple Vehicle Control

Smart vehicle physics with AI navigation.

**Features:**
- Realistic Rigidbody physics
- NavMesh-based AI driving
- Obstacle avoidance
- Stuck detection and recovery
- Destination braking

```csharp
using WitShells.SimpleVehicleControl;
using UnityEngine;

public class CarController : MonoBehaviour
{
    [SerializeField] private CarDriver carDriver;
    [SerializeField] private CarDriverAI carAI;
    
    void Update()
    {
        // Manual control
        if (!carAI.enabled)
        {
            float throttle = Input.GetAxis("Vertical");
            float steering = Input.GetAxis("Horizontal");
            carDriver.SetInput(throttle, steering);
        }
    }
    
    public void SetAIDestination(Vector3 destination)
    {
        carAI.enabled = true;
        carAI.SetDestination(destination);
    }
}
```

---

### 11. Military Grid System

Tactical grid overlay for strategy games and map displays.

**Features:**
- Square grid generation
- Configurable cell sizes
- Grid labeling (A1, B2, etc.)
- Object pooling for performance

```csharp
using WitShells.MilitaryGridSystem;
using UnityEngine;

public class TacticalMap : MonoBehaviour
{
    [SerializeField] private GridGenerator gridGenerator;
    
    void Start()
    {
        // Generate a 10x10 grid with 100 unit cells
        gridGenerator.GenerateGrid(10, 10, 100f);
    }
    
    public Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        return gridGenerator.WorldToGrid(worldPos);
    }
    
    public Vector3 GridToWorldPosition(int x, int y)
    {
        return gridGenerator.GridToWorld(x, y);
    }
}
```

---

### 12. Spline Runtime

Path creation and object animation along curves.

**Features:**
- Bezier curve generation
- Object path following
- Speed and easing control
- Editor visualization

```csharp
using WitShells.SplineRuntime;
using UnityEngine;

public class PathFollower : MonoBehaviour
{
    [SerializeField] private SplinePath splinePath;
    [SerializeField] private float speed = 5f;
    
    private float progress = 0f;
    
    void Update()
    {
        progress += speed * Time.deltaTime / splinePath.Length;
        
        if (progress >= 1f)
        {
            progress = 0f; // Loop
        }
        
        transform.position = splinePath.GetPointAtProgress(progress);
        transform.forward = splinePath.GetDirectionAtProgress(progress);
    }
}
```

---

### 13. Threading Job

Background threading for heavy computations.

**Features:**
- Easy-to-use job patterns
- Main thread callbacks
- Thread-safe operations
- Progress reporting

```csharp
using WitShells.ThreadingJob;
using UnityEngine;

public class HeavyComputation : MonoBehaviour
{
    public void ProcessLargeDataset(int[] data)
    {
        ThreadingJob.Run(() =>
        {
            // This runs on a background thread
            int sum = 0;
            foreach (int value in data)
            {
                sum += value;
                // Heavy processing here
            }
            return sum;
        },
        (result) =>
        {
            // This callback runs on the main thread
            Debug.Log($"Sum: {result}");
        });
    }
}
```

---

### 14. SQLite Database

Local data persistence with SQLite.

**Features:**
- Easy CRUD operations
- Offline storage
- Cross-platform support

**Dependencies:** `com.gilzoide.sqlite-net`

```csharp
using WitShells.SqLite;
using SQLite;

[Table("players")]
public class PlayerData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; }
    public int HighScore { get; set; }
}

public class DatabaseManager : MonoBehaviour
{
    private SQLiteConnection db;
    
    void Start()
    {
        string dbPath = Application.persistentDataPath + "/game.db";
        db = new SQLiteConnection(dbPath);
        db.CreateTable<PlayerData>();
    }
    
    public void SavePlayer(PlayerData player)
    {
        db.InsertOrReplace(player);
    }
    
    public PlayerData GetPlayer(int id)
    {
        return db.Find<PlayerData>(id);
    }
}
```

---

## 🔧 Requirements

- **Unity:** 2021.3 LTS or newer
- **Platforms:** All Unity-supported platforms
- **.NET:** Standard 2.1

---

## 📄 License

MIT License - See individual package documentation for details.

---

## 🤝 Contributing

1. Report bugs via GitHub Issues
2. Submit pull requests with improvements
3. Share your use cases and examples

---

<p align="center">
  <strong>WitShells</strong><br>
  <a href="https://witshells.com">witshells.com</a>
</p>
