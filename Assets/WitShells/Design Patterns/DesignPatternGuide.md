# WitShells Design Patterns Guide

Welcome to the **WitShells Design Patterns** package!  
This guide explains each included pattern, what it does, how to use it, and provides practical Unity code examples and use cases.  
Whether you're a Unity beginner, intermediate, or advanced developer, this guide will help you apply robust, reusable solutions in your projects.

---

## Table of Contents

- [WitShells Design Patterns Guide](#witshells-design-patterns-guide)
  - [Table of Contents](#table-of-contents)
  - [Singleton Pattern](#singleton-pattern)
  - [Observer Pattern](#observer-pattern)
  - [Component/ECS Pattern](#componentecs-pattern)
  - [Factory Pattern](#factory-pattern)
  - [State Pattern](#state-pattern)
  - [Strategy Pattern](#strategy-pattern)
  - [Command Pattern](#command-pattern)
  - [Prototype Pattern](#prototype-pattern)
  - [Decorator Pattern](#decorator-pattern)
  - [Flyweight Pattern](#flyweight-pattern)
  - [Mediator Pattern](#mediator-pattern)
  - [Builder Pattern](#builder-pattern)
  - [Service Locator Pattern](#service-locator-pattern)
  - [Object Pool Pattern](#object-pool-pattern)
  - [Template Method Pattern](#template-method-pattern)
- [More Patterns to Explore](#more-patterns-to-explore)
  - [Tips for Using These Patterns](#tips-for-using-these-patterns)
  - [Need Help?](#need-help)
  - [Observer Pattern](#observer-pattern-1)
  - [Component/ECS Pattern](#componentecs-pattern-1)
  - [Factory Pattern](#factory-pattern-1)
  - [State Pattern](#state-pattern-1)
  - [Strategy Pattern](#strategy-pattern-1)
  - [Command Pattern](#command-pattern-1)
  - [Prototype Pattern](#prototype-pattern-1)
  - [Decorator Pattern](#decorator-pattern-1)
  - [Flyweight Pattern](#flyweight-pattern-1)
  - [Mediator Pattern](#mediator-pattern-1)
  - [Builder Pattern](#builder-pattern-1)
  - [Service Locator Pattern](#service-locator-pattern-1)
  - [Object Pool Pattern](#object-pool-pattern-1)
  - [Template Method Pattern](#template-method-pattern-1)
- [More Patterns to Explore](#more-patterns-to-explore-1)
  - [Tips for Using These Patterns](#tips-for-using-these-patterns-1)
  - [Need Help?](#need-help-1)

---

## Singleton Pattern

**What it does:**  
Ensures a class has only one instance and provides a global point of access.

**Unity Example:**
```csharp
public class GameManager : MonoSingleton<GameManager>
{
    public int score;
}
```
**Usage:**
```csharp
GameManager.Instance.score += 10;
```

**Use Cases:**
- Game managers
- Audio managers
- Input managers

---

## Observer Pattern

**What it does:**  
Allows objects to subscribe and react to events or changes in another object.

**Unity Example:**
```csharp
public class Health : MonoObserverPattern<int>
{
    public void TakeDamage(int amount)
    {
        // ...damage logic...
        NotifyObservers(currentHealth);
    }
}
```
**Usage:**
```csharp
health.Subscribe(OnHealthChanged);
```

**Use Cases:**
- UI updates on health/score changes
- Event-driven systems
- Achievement tracking

---

## Component/ECS Pattern

**What it does:**  
Composes entities from reusable components, promoting flexibility over inheritance.

**Unity Example:**
```csharp
Entity player = new Entity();
player.AddComponent(new HealthComponent { Health = 100 });
player.AddComponent(new MovementComponent { Speed = 5f });
```

**Use Cases:**
- Character and enemy systems
- Power-up and ability systems
- Modular gameplay mechanics

---

## Factory Pattern

**What it does:**  
Creates objects without specifying the exact class of object to create.

**Unity Example:**
```csharp
var factory = new GenericFactory<string, Enemy>();
factory.Register("Zombie", () => new Zombie());
Enemy zombie = factory.Create("Zombie");
```

**Use Cases:**
- Spawning enemies or items
- UI element creation
- Procedural content generation

---

## State Pattern

**What it does:**  
Allows an object to alter its behavior when its internal state changes.

**Unity Example:**
```csharp
StateMachine stateMachine = new StateMachine();
stateMachine.ChangeState(new IdleState());
stateMachine.Update();
```

**Use Cases:**
- Player or enemy AI states (idle, attack, patrol)
- Game state management (menu, play, pause)
- Animation state machines

---

## Strategy Pattern

**What it does:**  
Defines a family of algorithms, encapsulates each one, and makes them interchangeable.

**Unity Example:**
```csharp
var context = new StrategyContext<(int, int), int>(new AddStrategy());
int result = context.ExecuteStrategy((3, 4)); // 7
context.SetStrategy(new MultiplyStrategy());
result = context.ExecuteStrategy((3, 4)); // 12
```

**Use Cases:**
- AI movement or attack strategies
- Sorting or filtering game data
- Customizable player abilities

---

## Command Pattern

**What it does:**  
Encapsulates a request as an object, allowing for parameterization and queuing.

**Unity Example:**
```csharp
var invoker = new CommandInvoker();
var printHello = new PrintCommand("Hello");
invoker.ExecuteCommand(printHello);
invoker.UndoLastCommand();
```

**Use Cases:**
- Input handling and remapping
- Undo/redo systems
- Scripting and cutscene triggers

---

## Prototype Pattern

**What it does:**  
Creates new objects by copying an existing object (prototype).

**Unity Example:**
```csharp
public class Bullet : MonoBehaviour, IPrototype<Bullet>
{
    public Bullet Clone()
    {
        GameObject cloneObj = Instantiate(this.gameObject);
        return cloneObj.GetComponent<Bullet>();
    }
}
```
**Usage:**
```csharp
bullet.Clone(); // Creates a copy of the bullet in the scene
```

**Use Cases:**
- Cloning projectiles, enemies, or power-ups
- Runtime prefab instantiation
- Procedural object duplication

---

## Decorator Pattern

**What it does:**  
Adds new functionality to objects dynamically.

**Unity Example:**
```csharp
IComponent component = new ConcreteComponent();
component = new ExtraBehaviorDecorator(component);
component.Operation();
```

**Use Cases:**
- Power-up effects
- Weapon upgrades
- Dynamic ability stacking

---

## Flyweight Pattern

**What it does:**  
Reduces memory usage by sharing as much data as possible with similar objects.

**Unity Example:**
```csharp
var factory = new FlyweightFactory<string, TreeFlyweight>();
var oak = factory.GetFlyweight("Oak", () => new TreeFlyweight { Mesh = "OakMesh", Texture = "OakTexture" });
oak.Operation(new { x = 10, y = 0, z = 5 });
```

**Use Cases:**
- Rendering forests, crowds, or particle systems
- Shared mesh/material data
- Large numbers of similar objects

---

## Mediator Pattern

**What it does:**  
Centralizes complex communications and control between related objects.

**Unity Example:**
```csharp
var mediator = new Mediator();
mediator.Subscribe("OnPlayerDeath", (sender, data) => Debug.Log("Player died!"));
mediator.Notify(this, "OnPlayerDeath");
```

**Use Cases:**
- UI systems communication
- Dialogue and quest systems
- Decoupling gameplay systems

---

## Builder Pattern

**What it does:**  
Constructs complex objects step by step.

**Unity Example:**
```csharp
var player = new PlayerBuilder()
    .SetName("Hero")
    .SetHealth(100)
    .SetSpeed(5.5f)
    .Build();
```

**Use Cases:**
- Character or item creation
- Level or environment generation
- Configurable object construction

---

## Service Locator Pattern

**What it does:**  
Provides a global point to access various services.

**Unity Example:**
```csharp
ServiceLocator.Register<IAudioService>(new AudioService());
var audio = ServiceLocator.Get<IAudioService>();
audio.PlaySound("explosion");
```

**Use Cases:**
- Audio, input, or save/load services
- Logging and analytics
- Centralized resource management

---

## Object Pool Pattern

**What it does:**  
Reuses objects from a fixed pool instead of creating and destroying them.

**Unity Example:**
```csharp
var bulletPool = new ObjectPool<Bullet>(() => new Bullet(), 10);
Bullet bullet = bulletPool.Get();
// Use bullet...
bulletPool.Release(bullet);
```

**Use Cases:**
- Bullets, enemies, or particle effects
- Reusable UI elements
- Network object pooling

---

## Template Method Pattern

**What it does:**  
Defines the skeleton of an algorithm, deferring some steps to subclasses.

**Unity Example:**
```csharp
var template = new UserInputTemplate();
int value = template.Execute("42");
```

**Use Cases:**
- AI routines
- Game loop steps
- Customizable data processing

---

# More Patterns to Explore

- **Adapter Pattern:** Integrate incompatible interfaces.
- **Proxy Pattern:** Control access to objects.
- **Chain of Responsibility:** Pass requests along a chain of handlers.

---

## Tips for Using These Patterns

- Use patterns as tools, not rules—apply them where they simplify your code.
- Combine patterns for more robust solutions (e.g., Singleton + Service Locator).
- Refactor existing code to use patterns for better maintainability.

---

## Need Help?

If you have questions or want to contribute, open an issue or pull request on the repository!

---
```<!-- filepath: d:\WitShells\CustomAssets\Assets\WitShells\Design Patterns\DesignPatternGuide.md -->

# WitShells Design Patterns Guide

Welcome to the **WitShells Design Patterns** package!  
This guide explains each included pattern, what it does, how to use it, and provides practical Unity code examples and use cases.  
Whether you're a Unity beginner, intermediate, or advanced developer, this guide will help you apply robust, reusable solutions in your projects.

---

## Table of Contents

- [Singleton Pattern](#singleton-pattern)
- [Observer Pattern](#observer-pattern)
- [Component/ECS Pattern](#componenteecs-pattern)
- [Factory Pattern](#factory-pattern)
- [State Pattern](#state-pattern)
- [Strategy Pattern](#strategy-pattern)
- [Command Pattern](#command-pattern)
- [Prototype Pattern](#prototype-pattern)
- [Decorator Pattern](#decorator-pattern)
- [Flyweight Pattern](#flyweight-pattern)
- [Mediator Pattern](#mediator-pattern)
- [Builder Pattern](#builder-pattern)
- [Service Locator Pattern](#service-locator-pattern)
- [Object Pool Pattern](#object-pool-pattern)
- [Template Method Pattern](#template-method-pattern)

---

## Singleton Pattern

**What it does:**  
Ensures a class has only one instance and provides a global point of access.

**Unity Example:**
```csharp
public class GameManager : MonoSingleton<GameManager>
{
    public int score;
}
```
**Usage:**
```csharp
GameManager.Instance.score += 10;
```

**Use Cases:**
- Game managers
- Audio managers
- Input managers

---

## Observer Pattern

**What it does:**  
Allows objects to subscribe and react to events or changes in another object.

**Unity Example:**
```csharp
public class Health : MonoObserverPattern<int>
{
    public void TakeDamage(int amount)
    {
        // ...damage logic...
        NotifyObservers(currentHealth);
    }
}
```
**Usage:**
```csharp
health.Subscribe(OnHealthChanged);
```

**Use Cases:**
- UI updates on health/score changes
- Event-driven systems
- Achievement tracking

---

## Component/ECS Pattern

**What it does:**  
Composes entities from reusable components, promoting flexibility over inheritance.

**Unity Example:**
```csharp
Entity player = new Entity();
player.AddComponent(new HealthComponent { Health = 100 });
player.AddComponent(new MovementComponent { Speed = 5f });
```

**Use Cases:**
- Character and enemy systems
- Power-up and ability systems
- Modular gameplay mechanics

---

## Factory Pattern

**What it does:**  
Creates objects without specifying the exact class of object to create.

**Unity Example:**
```csharp
var factory = new GenericFactory<string, Enemy>();
factory.Register("Zombie", () => new Zombie());
Enemy zombie = factory.Create("Zombie");
```

**Use Cases:**
- Spawning enemies or items
- UI element creation
- Procedural content generation

---

## State Pattern

**What it does:**  
Allows an object to alter its behavior when its internal state changes.

**Unity Example:**
```csharp
StateMachine stateMachine = new StateMachine();
stateMachine.ChangeState(new IdleState());
stateMachine.Update();
```

**Use Cases:**
- Player or enemy AI states (idle, attack, patrol)
- Game state management (menu, play, pause)
- Animation state machines

---

## Strategy Pattern

**What it does:**  
Defines a family of algorithms, encapsulates each one, and makes them interchangeable.

**Unity Example:**
```csharp
var context = new StrategyContext<(int, int), int>(new AddStrategy());
int result = context.ExecuteStrategy((3, 4)); // 7
context.SetStrategy(new MultiplyStrategy());
result = context.ExecuteStrategy((3, 4)); // 12
```

**Use Cases:**
- AI movement or attack strategies
- Sorting or filtering game data
- Customizable player abilities

---

## Command Pattern

**What it does:**  
Encapsulates a request as an object, allowing for parameterization and queuing.

**Unity Example:**
```csharp
var invoker = new CommandInvoker();
var printHello = new PrintCommand("Hello");
invoker.ExecuteCommand(printHello);
invoker.UndoLastCommand();
```

**Use Cases:**
- Input handling and remapping
- Undo/redo systems
- Scripting and cutscene triggers

---

## Prototype Pattern

**What it does:**  
Creates new objects by copying an existing object (prototype).

**Unity Example:**
```csharp
public class Bullet : MonoBehaviour, IPrototype<Bullet>
{
    public Bullet Clone()
    {
        GameObject cloneObj = Instantiate(this.gameObject);
        return cloneObj.GetComponent<Bullet>();
    }
}
```
**Usage:**
```csharp
bullet.Clone(); // Creates a copy of the bullet in the scene
```

**Use Cases:**
- Cloning projectiles, enemies, or power-ups
- Runtime prefab instantiation
- Procedural object duplication

---

## Decorator Pattern

**What it does:**  
Adds new functionality to objects dynamically.

**Unity Example:**
```csharp
IComponent component = new ConcreteComponent();
component = new ExtraBehaviorDecorator(component);
component.Operation();
```

**Use Cases:**
- Power-up effects
- Weapon upgrades
- Dynamic ability stacking

---

## Flyweight Pattern

**What it does:**  
Reduces memory usage by sharing as much data as possible with similar objects.

**Unity Example:**
```csharp
var factory = new FlyweightFactory<string, TreeFlyweight>();
var oak = factory.GetFlyweight("Oak", () => new TreeFlyweight { Mesh = "OakMesh", Texture = "OakTexture" });
oak.Operation(new { x = 10, y = 0, z = 5 });
```

**Use Cases:**
- Rendering forests, crowds, or particle systems
- Shared mesh/material data
- Large numbers of similar objects

---

## Mediator Pattern

**What it does:**  
Centralizes complex communications and control between related objects.

**Unity Example:**
```csharp
var mediator = new Mediator();
mediator.Subscribe("OnPlayerDeath", (sender, data) => Debug.Log("Player died!"));
mediator.Notify(this, "OnPlayerDeath");
```

**Use Cases:**
- UI systems communication
- Dialogue and quest systems
- Decoupling gameplay systems

---

## Builder Pattern

**What it does:**  
Constructs complex objects step by step.

**Unity Example:**
```csharp
var player = new PlayerBuilder()
    .SetName("Hero")
    .SetHealth(100)
    .SetSpeed(5.5f)
    .Build();
```

**Use Cases:**
- Character or item creation
- Level or environment generation
- Configurable object construction

---

## Service Locator Pattern

**What it does:**  
Provides a global point to access various services.

**Unity Example:**
```csharp
ServiceLocator.Register<IAudioService>(new AudioService());
var audio = ServiceLocator.Get<IAudioService>();
audio.PlaySound("explosion");
```

**Use Cases:**
- Audio, input, or save/load services
- Logging and analytics
- Centralized resource management

---

## Object Pool Pattern

**What it does:**  
Reuses objects from a fixed pool instead of creating and destroying them.

**Unity Example:**
```csharp
var bulletPool = new ObjectPool<Bullet>(() => new Bullet(), 10);
Bullet bullet = bulletPool.Get();
// Use bullet...
bulletPool.Release(bullet);
```

**Use Cases:**
- Bullets, enemies, or particle effects
- Reusable UI elements
- Network object pooling

---

## Template Method Pattern

**What it does:**  
Defines the skeleton of an algorithm, deferring some steps to subclasses.

**Unity Example:**
```csharp
var template = new UserInputTemplate();
int value = template.Execute("42");
```

**Use Cases:**
- AI routines
- Game loop steps
- Customizable data processing

---

# More Patterns to Explore

- **Adapter Pattern:** Integrate incompatible interfaces.
- **Proxy Pattern:** Control access to objects.
- **Chain of Responsibility:** Pass requests along a chain of handlers.

---

## Tips for Using These Patterns

- Use patterns as tools, not rules—apply them where they simplify your code.
- Combine patterns for more robust solutions (e.g., Singleton + Service Locator).
- Refactor existing code to use patterns for better maintainability.

---

## Need Help?

If you have questions or want to contribute, open an issue or pull request on the repository!

---