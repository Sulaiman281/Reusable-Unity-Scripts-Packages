---
_layout: landing
---

# WitShells — Reusable Unity Scripts & Packages

<p align="center">
  <strong>A collection of production-ready, reusable Unity packages</strong><br/>
  <a href="https://witshells.com">witshells.com</a> &nbsp;·&nbsp;
  <a href="https://github.com/Sulaiman281/Reusable-Unity-Scripts-Packages">GitHub</a>
</p>

---

## 📦 How to Install

All packages install via **Unity Package Manager** using Git URLs.

1. Open Unity → **Window** → **Package Manager**
2. Click **+** → **Add package from Git URL**
3. Paste a URL from the table below and click **Add**

---

## 📚 Packages

| # | Package | Description | Git URL |
|---|---------|-------------|---------|
| 1 | **Design Patterns** | Production-ready Observer, Singleton, State Machine, Command, Object Pool, Factory & more | `…?path=Assets/WitShells/DesignPatterns` |
| 2 | **WitPose** | Bio-kinetic posing tool — 95 muscle controls, keyframe recording, pose library | `…?path=Assets/WitShells/WitPose` |
| 3 | **Third Person Control** | Camera-relative locomotion with New Input System + Cinemachine 3 | `…?path=Assets/WitShells/ThirdPersonControl` |
| 4 | **WitActor** | NavMesh AI actor framework — patrol, chase, idle state integration | `…?path=Assets/WitShells/WitActor` |
| 5 | **Animation Rig** | Wizard-based IK rig setup, constraint controllers, runtime weight control | `…?path=Assets/WitShells/WitAnimationRig` |
| 6 | **WitMultiplayer** | Unity Gaming Services wrapper — lobbies, matchmaking, Relay | `…?path=Assets/WitShells/WitMultiplayer` |
| 7 | **WitClientApi** | Lightweight REST client with token auth and JSON manifest | `…?path=Assets/WitShells/WitClientApi` |
| 8 | **Dialogs Manager** | Conversation sequencing, typewriter effects, audio per line | `…?path=Assets/WitShells/DialogsManager` |
| 9 | **Tank Controls** | Rigidbody tank with turret yaw/pitch and weapon-ready interface | `…?path=Assets/WitShells/TankControls` |
| 10 | **Shooting System** | Raycast/projectile weapon system with trajectory preview & object pool | `…?path=Assets/WitShells/ShootingSystem` |
| 11 | **Simple Vehicle Control** | Rigidbody car physics with NavMesh AI, obstacle avoidance & stuck recovery | `…?path=Assets/WitShells/SimpleVehicleControl` |
| 12 | **Military Grid System** | Tactical square grid with labelling (A1, B2 …) and object pooling | `…?path=Assets/WitShells/MilitaryGridSystem` |
| 13 | **Canvas Draw Tool** | Runtime freehand drawing on a UI Canvas | `…?path=Assets/WitShells/CanvasDrawTool` |
| 14 | **Particles Presets** | Editor presets for smoke, fire and rain tuned for mobile | `…?path=Assets/WitShells/ParticlesPresets` |
| 15 | **Spline Runtime** | Bezier path creation and smooth object animation along curves | `…?path=Assets/WitShells/SplineRuntime` |
| 16 | **Threading Job** | Background threads with main-thread callbacks and progress reporting | `…?path=Assets/WitShells/ThreadingJob` |
| 17 | **Broadcast (UDP)** | LAN UDP broadcast discovery | `…?path=Assets/WitShells/Broadcast` |
| 18 | **WebRTC P2P** | Peer-to-peer WebRTC integration | `…?path=Assets/WitShells/WebRTC-Wit` |
| 19 | **Live Microphone** | Real-time microphone capture and streaming | `…?path=Assets/WitShells/LiveMic` |
| 20 | **Map View** | Scrollable/zoomable map view component | `…?path=Assets/WitShells/MapView` |
| 21 | **SQLite Database** | Local CRUD persistence via `com.gilzoide.sqlite-net` | `…?path=Assets/WitShells/SqLite` |

> All URLs share the same base: `https://github.com/Sulaiman281/Reusable-Unity-Scripts-Packages.git`  
> Append the `?path=…` suffix shown above.

---

## 🏗️ Design Patterns — Quick Reference

> 📖 [Full Guide](articles/design-patterns.md) &nbsp;·&nbsp; 🔬 [API Reference](api/WitShells.DesignPatterns.Core.html)

| Pattern | Class | Purpose |
|---------|-------|---------|
| Singleton | `MonoSingleton<T>` | Single persistent MonoBehaviour instance |
| Observer | `ObserverPattern<T>` | Decoupled event broadcasting |
| Bindable | `Bindable<T>` | Reactive property with change callbacks |
| State Machine | `StateMachine` / `IState` | Explicit state transitions |
| Command | `ICommand` / `CommandInvoker` | Undoable action queue |
| Object Pool | `ObjectPool<T>` | Pre-allocated reusable object cache |
| Factory | `GenericFactory<,>` | Runtime object creation by key |
| Service Locator | `ServiceLocator` | Global service registry |
| Strategy | `IStrategy<,>` / `StrategyContext<,>` | Swappable algorithm selection |
| Builder | `IBuilder<T>` / `Builder<T>` | Fluent step-by-step construction |
| Mediator | `IMediator` / `Mediator` | Centralised component communication |
| Flyweight | `FlyweightFactory<,>` | Shared immutable data instances |
| Template Method | `TemplateMethod<,>` | Fixed algorithm skeleton |
| ECS | `Entity` / `Component` | Lightweight entity-component system |
| Prototype | `IPrototype<T>` | Deep clone interface |
| Drag & Drop | `DraggableItem<T>` / `DropZone<T>` | Type-safe UI drag-and-drop |
| Formation | `FormationUtils` | 9 static formation layout helpers |

```csharp
// Example — MonoSingleton
public class GameManager : MonoSingleton<GameManager>
{
    public int Score { get; private set; }
    public void AddScore(int points) => Score += points;
}

// Access from anywhere:
GameManager.Instance.AddScore(10);
```

---

## 🔧 Requirements

| Requirement | Version |
|-------------|---------|
| Unity | 2021.3 LTS or newer |
| .NET | Standard 2.1 |
| Platforms | All Unity-supported platforms |

---

## 🤝 Contributing

1. Report bugs via [GitHub Issues](https://github.com/Sulaiman281/Reusable-Unity-Scripts-Packages/issues)
2. Submit pull requests with improvements
3. Share your use cases and examples

---

<p align="center">
  <strong>WitShells</strong> · <a href="https://witshells.com">witshells.com</a>
</p>
