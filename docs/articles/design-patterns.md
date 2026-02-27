# Design Patterns Guide

This article walks through every design pattern available in the `WitDesignPattern` package.
For the full source reference, browse the [API documentation](../api/WitShells.DesignPatterns.Core.html).

---

## Singleton — `MonoSingleton<T>`

Guarantees a single instance of a MonoBehaviour exists across all scenes.

```csharp
public class GameManager : MonoSingleton<GameManager>
{
    public int Score { get; private set; }
}

// Access anywhere:
GameManager.Instance.Score++;
```

---

## Observer — `ObserverPattern<T>`

Publish/subscribe without Unity events or C# delegates.

```csharp
var onDied = new ObserverPattern<string>();
onDied.Subscribe(name => Debug.Log($"{name} died"));
onDied.NotifyObservers("Player");
```

---

## Bindable — `Bindable<T>`

A reactive property that fires a `UnityEvent` when its value changes.

```csharp
var health = new Bindable<int>(100);
health.OnValueChanged.AddListener(v => healthBar.fillAmount = v / 100f);
health.Value = 75; // UI updates automatically
```

---

## State Machine — `StateMachine` + `IState`

Clean state management with enter/execute/exit lifecycle.

```csharp
var sm = new StateMachine();
sm.ChangeState(new IdleState());   // calls Enter()
sm.Update();                        // calls Execute() each frame
sm.ChangeState(new MoveState());   // calls Exit() then Enter()
```

---

## Command — `CommandInvoker` + `ICommand`

Encapsulate actions with built-in undo support.

```csharp
var invoker = new CommandInvoker();
invoker.ExecuteCommand(new PrintCommand("Fire!"));
invoker.UndoLastCommand();
```

---

## Object Pool — `ObjectPool<T>`

Reuse objects instead of allocating/GC-ing them.

```csharp
var pool = new ObjectPool<Bullet>(() => new Bullet(), initialCapacity: 20);
Bullet b = pool.Get();
// … use bullet …
pool.Release(b);
```

---

## Factory — `GenericFactory<TKey, TProduct>`

Register and create products by key.

```csharp
var factory = new GenericFactory<string, IWeapon>();
factory.Register("sword", () => new Sword());
factory.Register("bow",   () => new Bow());
IWeapon w = factory.Create("sword");
```

---

## Service Locator — `ServiceLocator`

A global, type-keyed registry for services.

```csharp
ServiceLocator.Register<IAudioService>(new AudioService());
// … later …
var audio = ServiceLocator.Get<IAudioService>();
audio.Play("explosion");
```

---

## Strategy — `StrategyContext<TContext, TResult>`

Swap algorithms at runtime.

```csharp
var ctx = new StrategyContext<(int, int), int>(new AddStrategy());
ctx.ExecuteStrategy((3, 4));      // 7
ctx.SetStrategy(new MultiplyStrategy());
ctx.ExecuteStrategy((3, 4));      // 12
```

---

## Builder — `Builder<T>`

Construct complex objects through a fluent API.

```csharp
var player = new PlayerBuilder()
    .SetName("Hero")
    .SetHealth(100)
    .SetSpeed(5.5f)
    .Build();
```

---

## Mediator — `Mediator`

Decouple systems by routing messages through a central hub.

```csharp
var mediator = new Mediator();
mediator.Subscribe("OnScoreChanged", (sender, data) => Debug.Log(data));
mediator.Notify(this, "OnScoreChanged", 42);
```

---

## Flyweight — `FlyweightFactory<TKey, TFlyweight>`

Share expensive data (meshes, textures) across thousands of instances.

```csharp
var factory = new FlyweightFactory<string, TreeFlyweight>();
var oak = factory.GetFlyweight("oak", () => new TreeFlyweight { Mesh = "oak.fbx" });
oak.Operation(position);
```

---

## Template Method — `TemplateMethod<TInput, TResult>`

Define an algorithm skeleton; let subclasses fill in the steps.

```csharp
public class ParseInt : TemplateMethod<string, int>
{
    protected override void PreProcess(string input) => Debug.Log("Validating…");
    protected override int  Process(string input)    => int.Parse(input);
    protected override void PostProcess(int result)  => Debug.Log($"Got {result}");
}
```

---

## ECS (Entity-Component-System)

Compose entities from pure-data components and drive behaviour from systems.

```csharp
var entity = new Entity();
entity.AddComponent(new HealthComponent { Health = 100 });
new HealthSystem().Update(entity);
```

---

## Prototype — `IPrototype<T>`

Clone configured instances rather than constructing from scratch.

```csharp
public class EnemyConfig : IPrototype<EnemyConfig>
{
    public int Hp; public float Speed;
    public EnemyConfig Clone() => (EnemyConfig)MemberwiseClone();
}
```

---

## Drag & Drop — `DraggableItem<T>` / `DropZone<T>`

Full Unity UI drag-and-drop with type-safe payloads, swap support, and visual feedback.

---

## Formation Utils — `FormationUtils`

Generate world-space `Pose` lists for Circle, Line, V, Wedge, Box, Triangle, Echelon, Column, and Diamond formations.

```csharp
List<Pose> poses = FormationUtils.GenerateCircleFormation(center, radius: 5f, count: 8);
```
