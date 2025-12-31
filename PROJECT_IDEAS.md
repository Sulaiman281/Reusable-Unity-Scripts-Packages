# Turn-Based Multiplayer Game Ideas

Using WitShells packages to build complete turn-based multiplayer games.

---

## 🎮 Idea 1: Tank Commander Arena

**Genre:** Turn-Based Tactical Tank Combat

**Concept:**  
A 1v1 or 2v2 tank battle game where players take turns positioning their tanks on a grid, aiming shots with trajectory preview, and firing at opponents. Each player has a limited time per turn. The game supports online matchmaking and local LAN play.

### Packages Used

| Package | Purpose |
|---------|---------|
| **WitMultiplayer** | Lobby creation, matchmaking, Relay for online play |
| **Military Grid System** | Turn-based grid movement and positioning |
| **Tank Controls** | Tank movement and turret aiming |
| **Shooting System** | Projectile firing with trajectory preview |
| **SQLite Database** | Local player stats, win/loss records |
| **Broadcast (UDP)** | LAN game discovery for local multiplayer |

### Core Mechanics

```
Turn Structure:
┌─────────────────────────────────────────┐
│  1. Movement Phase (10 seconds)         │
│     - Select destination tile           │
│     - Tank moves along grid             │
│                                         │
│  2. Aim Phase (15 seconds)              │
│     - Rotate turret                     │
│     - Trajectory preview shows arc      │
│     - Adjust power/angle                │
│                                         │
│  3. Fire Phase                          │
│     - Projectile launches               │
│     - Damage calculated on hit          │
│     - Turn passes to opponent           │
└─────────────────────────────────────────┘
```

### Implementation Outline

```csharp
using WitShells.TankControls;
using WitShells.ShootingSystem;
using WitShells.MilitaryGridSystem;
using WitShells.WitMultiplayer;
using Unity.Netcode;

public class TurnBasedTankGame : NetworkBehaviour
{
    [SerializeField] private GridGenerator grid;
    [SerializeField] private TankController playerTank;
    [SerializeField] private TrajectoryPreview trajectory;
    
    private NetworkVariable<int> currentPlayerTurn = new(0);
    private NetworkVariable<GamePhase> currentPhase = new(GamePhase.Movement);
    
    public enum GamePhase { Movement, Aim, Fire, Waiting }
    
    public void OnTileSelected(Vector2Int gridPos)
    {
        if (!IsMyTurn()) return;
        if (currentPhase.Value != GamePhase.Movement) return;
        
        Vector3 worldPos = grid.GridToWorld(gridPos.x, gridPos.y);
        playerTank.MoveTo(worldPos);
        
        // After movement, transition to aim phase
        TransitionToAimPhaseServerRpc();
    }
    
    public void OnAimUpdated(Vector3 direction, float power)
    {
        if (!IsMyTurn()) return;
        if (currentPhase.Value != GamePhase.Aim) return;
        
        // Show trajectory preview locally
        trajectory.ShowTrajectory(
            playerTank.TurretMuzzle.position,
            direction,
            power
        );
    }
    
    public void OnFirePressed()
    {
        if (!IsMyTurn()) return;
        if (currentPhase.Value != GamePhase.Aim) return;
        
        FireProjectileServerRpc(trajectory.LastDirection, trajectory.LastPower);
    }
    
    [ServerRpc]
    private void FireProjectileServerRpc(Vector3 direction, float power)
    {
        // Spawn projectile, handle damage, end turn
        currentPhase.Value = GamePhase.Fire;
        
        // After projectile resolves, switch turns
        StartCoroutine(EndTurnAfterProjectile());
    }
    
    private bool IsMyTurn()
    {
        return currentPlayerTurn.Value == (int)OwnerClientId;
    }
}
```

### Features Checklist

- [ ] Lobby system with 2-4 player support
- [ ] Turn timer with visual countdown
- [ ] Grid-based movement with pathfinding
- [ ] Trajectory preview with wind effects
- [ ] Destructible terrain (optional)
- [ ] Power-ups on grid tiles
- [ ] Match history in SQLite
- [ ] Spectator mode

---

## 🎮 Idea 2: Squad Tactics Online

**Genre:** Turn-Based Strategy RPG

**Concept:**  
A tactical squad combat game where each player controls 3-4 characters on a grid map. Characters have unique abilities (medic, sniper, assault, engineer). Players take turns moving all units and executing actions. Supports 1v1 ranked matches and 2v2 team battles.

### Packages Used

| Package | Purpose |
|---------|---------|
| **WitMultiplayer** | Ranked matchmaking, lobbies, team formation |
| **Military Grid System** | Hex or square grid for tactical positioning |
| **WitActor System** | Unit AI for abilities, pathfinding, states |
| **Third Person Control** | Camera control for viewing the battlefield |
| **Shooting System** | Ranged attacks with hit calculations |
| **Dialogs Manager** | Pre-battle briefings, character banter |
| **WitClientApi** | Leaderboards, player profiles from backend |
| **SQLite Database** | Offline progression, unit unlocks |

### Core Mechanics

```
Match Flow:
┌─────────────────────────────────────────┐
│  Pre-Match:                             │
│  - Select 4 units from roster           │
│  - Place units in deployment zone       │
│                                         │
│  Turn Structure (per player):           │
│  - Each unit gets 2 Action Points       │
│  - Move costs 1 AP per tile             │
│  - Abilities cost 1-2 AP                │
│  - End turn when all AP spent or skip   │
│                                         │
│  Victory Conditions:                    │
│  - Eliminate all enemy units            │
│  - Capture objective (King of Hill)     │
│  - Survive X turns (Defense mode)       │
└─────────────────────────────────────────┘
```

### Unit Classes

```
┌──────────────┬────────────────────────────────────┐
│ Class        │ Abilities                          │
├──────────────┼────────────────────────────────────┤
│ Assault      │ Move+Attack, Grenade (AOE)         │
│ Sniper       │ Long-range shot, Overwatch         │
│ Medic        │ Heal ally, Revive downed unit      │
│ Engineer     │ Deploy turret, Repair cover        │
│ Scout        │ Double move, Reveal hidden units   │
└──────────────┴────────────────────────────────────┘
```

### Implementation Outline

```csharp
using WitShells.WitActor;
using WitShells.MilitaryGridSystem;
using WitShells.WitMultiplayer;
using WitShells.ShootingSystem;
using Unity.Netcode;
using System.Collections.Generic;

public class SquadTacticsGame : NetworkBehaviour
{
    [SerializeField] private GridGenerator battleGrid;
    [SerializeField] private List<TacticalUnit> playerUnits;
    
    private NetworkList<UnitState> unitStates;
    private NetworkVariable<int> activePlayerId = new(0);
    
    [System.Serializable]
    public struct UnitState : INetworkSerializable
    {
        public ulong OwnerId;
        public int UnitId;
        public Vector2Int GridPosition;
        public int CurrentHP;
        public int ActionPoints;
        public UnitClass Class;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref OwnerId);
            serializer.SerializeValue(ref UnitId);
            serializer.SerializeValue(ref GridPosition);
            serializer.SerializeValue(ref CurrentHP);
            serializer.SerializeValue(ref ActionPoints);
            serializer.SerializeValue(ref Class);
        }
    }
    
    public enum UnitClass { Assault, Sniper, Medic, Engineer, Scout }
    
    public void SelectUnit(int unitId)
    {
        if (!IsMyTurn()) return;
        
        var unit = GetUnit(unitId);
        if (unit.OwnerId != OwnerClientId) return;
        
        // Highlight valid move tiles
        var validMoves = CalculateValidMoves(unit);
        battleGrid.HighlightTiles(validMoves, Color.blue);
        
        // Highlight valid attack targets
        var validTargets = CalculateValidTargets(unit);
        battleGrid.HighlightTiles(validTargets, Color.red);
    }
    
    public void MoveUnit(int unitId, Vector2Int destination)
    {
        if (!IsMyTurn()) return;
        
        var unit = GetUnit(unitId);
        int moveCost = CalculateMoveCost(unit.GridPosition, destination);
        
        if (unit.ActionPoints >= moveCost)
        {
            MoveUnitServerRpc(unitId, destination);
        }
    }
    
    public void UseAbility(int unitId, AbilityType ability, Vector2Int target)
    {
        if (!IsMyTurn()) return;
        
        var unit = GetUnit(unitId);
        int abilityCost = GetAbilityCost(ability);
        
        if (unit.ActionPoints >= abilityCost)
        {
            UseAbilityServerRpc(unitId, ability, target);
        }
    }
    
    [ServerRpc]
    private void UseAbilityServerRpc(int unitId, AbilityType ability, Vector2Int target)
    {
        switch (ability)
        {
            case AbilityType.Attack:
                ResolveAttack(unitId, target);
                break;
            case AbilityType.Heal:
                ResolveHeal(unitId, target);
                break;
            case AbilityType.Grenade:
                ResolveAOEDamage(unitId, target, radius: 2);
                break;
            case AbilityType.Overwatch:
                SetOverwatchTrigger(unitId);
                break;
        }
        
        DeductActionPoints(unitId, GetAbilityCost(ability));
        CheckTurnEnd();
    }
    
    public void EndTurn()
    {
        if (!IsMyTurn()) return;
        EndTurnServerRpc();
    }
    
    [ServerRpc]
    private void EndTurnServerRpc()
    {
        // Reset AP for next player's units
        // Switch active player
        // Check win conditions
    }
}
```

### Features Checklist

- [ ] Unit roster with unlockable characters
- [ ] Ranked 1v1 matchmaking with ELO
- [ ] 2v2 team battles with shared vision
- [ ] Fog of war (units only see nearby tiles)
- [ ] Cover system (half/full cover reduces hit chance)
- [ ] Overwatch (reaction fire on enemy turn)
- [ ] Persistent progression (unit XP, unlocks)
- [ ] Replay system (watch past matches)
- [ ] Seasonal leaderboards via API

---

## 🛠️ Shared Architecture

Both games can share core systems:

```
┌─────────────────────────────────────────────────────┐
│                  Game Architecture                  │
├─────────────────────────────────────────────────────┤
│                                                     │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │
│  │ Networking  │  │ Turn System │  │    Grid     │ │
│  │ (Multiplayer│  │  (Phases,   │  │ (Movement,  │ │
│  │   Lobby)    │  │   Timer)    │  │  Targeting) │ │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘ │
│         │                │                │        │
│         └────────────────┼────────────────┘        │
│                          │                         │
│                   ┌──────▼──────┐                  │
│                   │ Game State  │                  │
│                   │  Manager    │                  │
│                   └──────┬──────┘                  │
│                          │                         │
│         ┌────────────────┼────────────────┐        │
│         │                │                │        │
│  ┌──────▼──────┐  ┌──────▼──────┐  ┌──────▼──────┐ │
│  │    Units    │  │   Combat    │  │     UI      │ │
│  │  (Actor,    │  │ (Shooting,  │  │  (Canvas    │ │
│  │   Tank)     │  │  Damage)    │  │   Builder)  │ │
│  └─────────────┘  └─────────────┘  └─────────────┘ │
│                                                     │
└─────────────────────────────────────────────────────┘
```

---

## 📋 Development Roadmap

### Phase 1: Core Systems (2-3 weeks)
- [ ] Grid system integration
- [ ] Basic turn management
- [ ] Unit/tank movement on grid
- [ ] Local 2-player hotseat mode

### Phase 2: Combat (2 weeks)
- [ ] Attack/ability system
- [ ] Damage calculation
- [ ] Trajectory preview (Tank Commander)
- [ ] Cover system (Squad Tactics)

### Phase 3: Multiplayer (2-3 weeks)
- [ ] Lobby creation and joining
- [ ] Turn synchronization via Netcode
- [ ] Matchmaking integration
- [ ] LAN discovery (optional)

### Phase 4: Polish (2 weeks)
- [ ] UI/UX improvements
- [ ] Sound effects and music
- [ ] Particle effects
- [ ] Tutorial/onboarding

### Phase 5: Persistence (1 week)
- [ ] SQLite for local saves
- [ ] API integration for leaderboards
- [ ] Player profiles and stats

---

## 💡 Quick Start

1. Install required packages from [README.md](README.md)
2. Create a new Unity project (2021.3+)
3. Set up Netcode for GameObjects
4. Implement TurnManager as NetworkBehaviour
5. Build grid system with Military Grid System
6. Add units using WitActor or Tank Controls
7. Connect multiplayer with WitMultiplayer

---

<p align="center">
  <strong>WitShells</strong><br>
  <a href="https://witshells.com">witshells.com</a>
</p>
