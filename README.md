# ModulesFramework

## Getting started

All you need for start using ModulesFramework
is this simple code:

```csharp
public void MyEntryPoint() 
{
    var ecs = new Ecs();
    ecs.Start();
}
```

Next steps depends on what you want to do. 
Here example for simple server:

```csharp
public class MyServer
{
    private Ecs _ecs;
    
    public MyServer()
    {
        _ecs = new Ecs();
    }
    
    public void StartServer()
    {
        _ecs.Start();
    }
    
    public void Tick()
    {
        _ecs.Run();
        _ecs.PostRun();
    }
    
    public void StopServer()
    {
        _ecs.Destroy();
    }
}
```

##### Simple example

Let's create some battle feature. We want to create one
player and three enemy. Then we want to make some damage.
Finally we want to destroy entities when they hp <= 0.

First of all we need a global module that startup our simple example.

**Note:** we do not want make all logic bind to global,
because our battle may be part of complex game.

So let's create global module:

```csharp
[GlobalModule]
public class StartupModule : EcsModule
{
    protected override Task Setup()
    {
        return Task.CompletedTask;
    }
}
```
And create our battle module:

```csharp
public class BattleModule : EcsModule
{
}
```
Now we just init and activate battle module from startup.

```csharp
[GlobalModule]
public class StartupModule : EcsModule
{
    protected override Task Setup()
    {
        World.InitModule<BattleModule>(true);
        return Task.CompletedTask;
    }
}
```

Now let's create some components.

```csharp
public struct Hp
{
    public int current;
    public int max; // just for example
}

public struct Damage
{
    public int damageValue;
}

public struct PlayerTag {}
public struct EnemyTag {}
public struct DeadTag {}
```
As you see all components is a struct.

Now we ready to create our systems. 
Let's start from creating player and enemies.

```csharp
[EcsSystem(typeof(BattleModule))] // bind system to module
public class InitBattleSystem : IInitSystem
{
    private DataWorld _world;
    
    public void Init()
    {
        _world.NewEntity()
            .AddComponent(new PlayerTag())
            .AddComponent(new Hp() { 
                maxValue = 100, 
                current = 100 
            });
            
        for (var i = 0; i < 3; ++i)
        {
             _world.NewEntity()
                .AddComponent(new PlayerTag())
                .AddComponent(new Hp() { 
                    maxValue = 20, 
                    current = 20 
                });
        }
    }
}
```

`Init()` called when module initialized. There is no 
need to create system by your own. All systems creates
when module initialized.

Now damage!


```csharp
[EcsSystem(typeof(BattleModule))] // bind system to module
public class DamageSystem : IRunSystem
{
    private DataWorld _world;
    
    public void Run()
    {
        // get all entities with hp and damage 
        using var query = _world.Select<Hp>()
            .With<Damage>();
            
        foreach(var entity in query.GetEntities())
        {
            ref var hp = ref entity.GetComponent<Hp>();
            ref var damage = ref entity.GetComponent<Damage>();
            hp.current -= damage.value;
            // we do not want apply same damage twice
            entity.RemoveComponent<Damage>();
            if (hp.current <= 0)
                entity.AddComponent<DeadTag>();
        }
    }
}
```

And finally death system.

```csharp
[EcsSystem(typeof(BattleModule))] // bind system to module
public class DeathSystem : IPostRunSystem
{
    private DataWorld _world;
    
    public void PostRun()
    {
        // get dead entities
        using var query = _world.Select<DeadTag>();
            
        foreach(var entity in query.GetEntities())
        {
            if (entity.HasComponent<PlayerTag>())
            {
                // game over
            }
            entity.Destroy();
        }
    }
}
```
This is a very simple example but it shows main 
concepts of ModulesFramework and Ecs. Let's do a 
couple more things.

First off all let's create a settings so we able control
count of enemies. We will do it by dependencies of module.

```csharp
public class Settings 
{
    public readonly int enemiesCount;
}
```

```csharp
[GlobalModule]
public class StartupModule : EcsModule
{
    private readonly Dictionary<Type, object> _dependencies = new();
    protected override Task Setup()
    {
        World.InitModule<BattleModule>(true);
        // read settings from some JSON or anything else
        _dependencies[typeof(Settings)] = settings;
        return Task.CompletedTask;
    }
    
    public override Dictionary GetDependency(Type t)
    {
        return _dependencies[t];
    }
}
```
Now we can do this.


```csharp
[EcsSystem(typeof(BattleModule))] // bind system to module
public class InitBattleSystem : IInitSystem
{
    private DataWorld _world;
    // it injects by creating system
    private Settings _settings;
    
    public void Init()
    {
        // creating player
            
        for (var i = 0; i < _settings.enemiesCount; ++i)
        {
             // creating enemy
        }
    }
}
```
You can create any dependencies and inject them in any
system of your module. If module is global *all* systems
can use it's dependencies.

Now let's take a look on another thing. We want to
give player a coin for every killed enemy. We could
create new component `Wallet` and add it to some 
entity that lives between battles. But it must live forever,
must be created at start (to live between sessions)
and take it by query too boilerplated. There is 
better way - the one data concept.

**OneData** is a struct that holds some information 
that exists *only in one* copy. That's it you can 
very simple controls it. Let's see the example.

```csharp
public struct Wallet
{
    public int coins;
    public int someOtherResource;
}
```
```csharp
[GlobalModule]
public class StartupModule : EcsModule
{
    private readonly Dictionary<Type, object> _dependencies = new();
    protected override Task Setup()
    {
        // load wallet from save
        World.CreateOneData(wallet);
        World.InitModule<BattleModule>(true);
       _dependencies[typeof(Settings)] = settings;
        return Task.CompletedTask;
    }
    // other methods
}
```
```csharp
[EcsSystem(typeof(BattleModule))] 
public class DeathSystem : IPostRunSystem
{
    private DataWorld _world;
    
    public void PostRun()
    {
        using var query = _world.Select<DeadTag>();
        // get one data similar to get component
        ref var wallet = ref _world.OneData<Wallet>();
        foreach(var entity in query.GetEntities())
        {
            if (entity.HasComponent<PlayerTag>())
                // game over
            
            if (entity.HasComponent<EnemyTag>())
                wallet.coins++;

            entity.Destroy();
        }
    }
}
```
As you see it's very simple.

### Events

Let's do one more thing. We do not want that dead system shows game over UI or does
something like this. It's good to keep such logic in separated system. Usually 
event concept is using for such thing. Very often in pure ecs frameworks we create
entity that exists only one frame (one frame entity) and try to find that entity
in `Run()` or `PostRun()`. ModulesFramework introduces other way - event systems.

Event is struct like an other components.
```csharp
public struct GameOverEvent 
{
    public GameOverReason reason; // enum why game is over
}
```
Fire event is simple:
```csharp
[EcsSystem(typeof(BattleModule))] 
public class DeathSystem : IPostRunSystem
{
    private DataWorld _world;
    
    public void PostRun()
    {
        // other code
        foreach(var entity in query.GetEntities())
        {
            if (entity.HasComponent<PlayerTag>())
                // if event is empty we can use _world.RiseEvent<EventType>()
                _world.RiseEvent(new GameOverEvent { reason = GameOverReason.Dead };
            
            // other code
        }
    }
}
```
And then we need *event* system:
```csharp
[EcsSystem(typeof(BattleModule))] 
public class DeathSystem : IRunEventSystem<GameOverEvent>
{
    public void RunEvent(GameOverEvent ev)
    {
        // show game over and do some logic
    }
}
```
Method `RunEvent<T>(T ev)` calls only when there is event. Every event system
subscribe when module activated and unsubscribe when deactivated.

There is three types of event systems. Every calls in particular time:
- `IRunEventSystem<T>` - calls **before** all `IRunSystem`s with the same order;
- `IPostRunEventSystem<T>` - calls **after** all `IRunSystem`s (and `IRunEventSystem<T>`)
and **before** all `IPostRunSystem`s with the same order;
- `IFrameEndEventSystem<T>` - calls **after** all `IRunSystem`s and `IPostRunSystem`s
systems.

**Note**: in example above we created event in `PostRun()` and check in `RunEvent<T>()`
so game over will be showing in *next* frame (i.e. next `Ecs.Run()` call) but
**will not** be lost.

## FAQ

##### How to create an instance of system?

You should never create instance of system by your own. 
Just create a class, implement one ore more interfaces
(see section [systems](#api-systems) in [API](#api)) and add `EcsSystem` attribute.

```csharp
[EcsSystem(typeof(MyModule))]
public class MySystem : IRunSystem {}
```
##### What is a module?
Module is a main concept in framework (that's why 
it calls Modules Framework). You can look at module
as an abstraction like a class or feature. One module
can be very large or very small - it depends on your
vision of project and it's architecture. But it's good
to separate modules by gamedesign's logic. For example
module with crafting doesn't should contains move logic
(of course if your game not about moving by crafting).
Because you can easily turn on/off modules you can 
control what systems running and what not, so you can
control what features exist in your game at the moment.
For another example you may not want to let players
craft items until they finish third level or create
a crafting house. 

Thus module is not just a class but a group of many 
data and functionality that implements concrete part
of your game.

##### How to get entities with three components?

```csharp
_world.Select<FirstComponent>()
    .With<SecondComponent>()
    .With<ThirdComponent>()
    .GetEntities();
```

##### How to get entities without some component?

```csharp
_world.Select<FirstComponent>()
    .Without<BadComponent>()
    .GetEntities();
```

##### How to get entities with hp > 0 but < 100?

```csharp
_world.Select<Hp>()
    .Where<Hp>(hp => hp.current > 0 && hp.current < 100)
    .GetEntities();
```

##### How to create OneData?

```csharp
_world.CreateOneData(dataInstance);
```

##### How to get OneData?

```csharp
ref var data = ref _world.OneData<MyData>();
```

## <a id="api"></a>API

### EcsModule

To create your own module just inherit from `EcsModule`.

`EcsModule` has several method that you can override:

- `Task Setup()` - use this method for create and setup your 
objects. `Setup()` can be async, but no system will be 
called before `Setup()` finished;
- `Dictionary<Type, object> GetDependencies` - 
this method must return any dependencies that your 
system needs. If module is global dependencies will 
be available in any systems. In over way they will 
be available only for systems that belongs to the module.
**Note:** if you have many global modules their systems will get dependency
only from its module but not from other global modules;
- `object GetDependency(Type t)` - this method like above but must return
one dependency by type. You can override method above or this. You also can
use some third-party IoC container to manage dependencies;
- `void OnSetupEnd()` - virtual method, calls when all dependencies updated before
any systems of module;
- `void OnActivate(), void OnDeactivate` - virtual methods
that calls when you activate or deactivate module 
(see `EcsWorld.ActivateModule<T>` and `EcsWorld.DeactivateModule<T>` );
- `void OnDestroy()` - like an `OnDeactivate()` but calls
when module destroyed (see `EcsWorld.DestroyModule()`). 
It should be used for release any resources initialized 
by `Setup()`;
- `Dictionary<Type, int> GetSystemsOrder()` - allows to 
set order to concrete system. By default all systems has
0 order. Ordering by ascending.

### DataWorld

`DataWorld` is the main class you should use for 
retrieve and create any data.

##### Work with Entities

- `Entity NewEntity()` - creates empty entity. 
`OnEntityCreated` event called at this point.
- `Entity CreateOneFrame()` - create `Entity` and add 
`OneFrameComponent` to it. That `Entity` will be destroyed
after all systems in `PostRun`;
- `EcsTable<T> GetEcsTable<T>()` - return raw data container that allows
iterate more fast. Should be used *only* if you iterate through thousands
and thousands entities.

##### Queries

- `Query Select<T>` - main method to get any entities.
See about `Query` below for more information;
- `bool Exist<T>()` - fast way to check if there exists
any entities with `T`;
- `bool TrySelectFirst<T>(out T)` - select first `Entity`
with `T`. If there is no such entities return false. 
Be careful: out parameter is not a reference.
`T` is a struct;

##### Data

- `Span<T> GetRawData<T>` - return raw span of components by
type `T`. Use it for very fast iterations through components;

##### Modules

- `void InitModule<T>(bool activateImmediately = false)`
\- initialize module `T`;
- `void InitModule<TModule, TParent>(bool activateImmediately = false)` -
initialize module `T` as *submodule*. Submodule has the
same dependencies as parent. But submodule do not 
share lifecycle (activation, deactivation and destroying);
- `void InitModuleAsync<T>(bool activateImmediately = false)` - async version
of `InitModule<T>`;
- `void InitModuleAsync<TModule, TParent>(bool activateImmediately = false)` -
async version of `InitModule<TModule, TParent>`;
- `void DestroyModule<T>()` - deactivate and destroy module.
If module wasn't active deactivation will not be processed.
- `void ActivateModule<T>()` - activate module. If module
already active do nothing;
- `void DeactivateModule<T>()` - deactivate module. If
module isn't active do nothing;
- `bool IsModuleActive<TModule>()` - check if module `T`
is active;

##### OneData

- `void CreateOneData<T>()` - create one data container
with default `T`. `T` is a struct;
- `void CreateOneData<T>(T data)` - create one data 
container. `T` is a struct;
- `ref T OneData<T>()` - return reference to one data.
If `T` one data not exists method will create it with 
default `T`. `T` is a struct;

##### Events

**Note**: event systems subscribes only after module activated.

- `void RiseEvent<T>()` - create *event* `T` with default fields (`new T()`);
- `void RiseEvent<T>(T)` - create *event* `T`;

##### Logs

- `void SetLogger(IModulesLogger)` - allow to use your own logger. By 
default `Console.WriteLine` is used;
- `void SetLogFilter(LogFilter)` - set log filter in logger. It can help
when you want see less logs. For example you may not need logs about
creating/destroying entities because it happens too lot in your game.
By default no logs filtered.

### Entity

- `Entity AddComponent(T)` - adds component `T` to `Entity`.
  In fact it creates element in `EcsTable<T>` and bind it
  to entity by entity id. `T` is a struct;
- `Entity RemoveComponent(T)` - removes component `T`
  from `Entity`. Again, it removes element from `EcsTable<T>`.
  If there is no `T` do nothing. `T` is a struct;
- `ref T GetComponent<T>()` - return reference to `T`
  at `Entity`. If there is no `T` returns `default`.
  Use `HasComponent<T>()` if you not sure. Also use
  more specific `Query` to avoid the case;
- `bool HasComponent<T>()` - return true if `T` bind to `Entity`;
- `void Destroy()` - destroy entity;

### Query

`Query` is a tool to get particular `Entity`s. You must 
never create it by yourself. Instead use `DataWorld.Select<T>`
method. Every `Query` specified by first component type
that entities must be bind with.

- `Query<T> With<TW>()` - add new component type to filter.
Every entity returned by query contains `TW` component;
- `Query<T> Without<TW>()` - add component that entities
must *not* be bind with;
- `Query<T> Where<TW>(Func<TW, bool> customFilter)` - 
allow to add custom filter. It *not* checks if entity
has `TW` component;
- `EntitiesEnumerable GetEntities()` - return enumerable
of entities that corresponds to query;
- `EntityDataEnumerable GetEntitiesId()` - return enumerable
  of ids of entities that corresponds to query;
- `bool Any()` - helper for check if any entity corresponded
to query exists;
- `bool TrySelectFirst<TRet>(out TRet)` - helper for
get first component from first `Entity`. Be careful - 
`TRet` not a reference;
- `ref TRet SelectFirst<TRet>()` - another helper for
get first component. It returns reference to component.
If there is no entity with `TRet` component method throws
`QuerySelectException<TRet>`;
- `bool TrySelectFirstEntity(out Entity)` - helper for 
get first `Entity`;
- `Entity SelectFirstEntity()` - another helper for get first
`Entity`. If there is no `Entity` method throws 
`QuerySelectEntityException`;
- `void DestroyAll()` - helper for destroy all entities
from query;
- `int Count()` - count of entities corresponds to query;
- `ComponentsEnumerable<T> GetComponents<T>` - return enumerable
of components type `T` that filtered by query;

### <a id="api-systems"></a>Systems

Systems works with data: create data, change it and destroy.
There is several types of system's interfaces:

- `IPreInitSystem` - calls once when module initialized;
- `IInitSystem` - calls once when module initialized *after*
every `IPreInitSystem`s works;
- `IActivateSystem` - calls once when module activated;
- `IDeactivateSystem` - calls once when module deactivated;
- `IRunSystem` - calls every `Ecs.Run()`;
- `IPostRunSystem` - calls every `Ecs.PostRun()`;
- `IRunPhysicSystem` - calls every `Ecs.RunPhysic()`;
- `IRunEventSystem<T>` - calls *before* `IRunSystem` if event `T` was raised;
- `IPostRunEventSystem<T>` - calls *after* `IRunSystem` and *before*
`IPostRunSystem` if event `T` was raised;
- `IFrameEndEventSystem<T>` - calls *after* `IPostRunSystem` if event `T` 
was raised;
- `IDestroySystem` - calls once when module destroyed; 

### Ecs

`Ecs` is a enter point to modules framework.

- `async void Start()` - init and activate all global modules;
- `void Run()` - calls `Run()` on `IRunSystem`;
- `void PostRun()` - calls `PostRun()` on `IPostRunSystem`;
- `void RunPhysic()` - calls `RunPhysic()` on `IRunPhysicSystem`;
- `void Destroy()` - destroy all modules;

### Attributes

- `EcsSystemAttribute` - mark that class is system.
Takes one argument about to what module belongs the system;
- `GlobalModuleAttribute` - mark that `EcsModule` is
global;

## Roadmap

### v0.5.x

- [x] Subscription systems `IEventSystem<T>`;
- [x] Add `GetEcsTable<T>` for fast iterations;
- [x] Add `GetDependency<T>` in module for more flexibility
how you manage your dependencies. It allows to use third-party
IoC container;

### v0.6.x
- [X] Ability to turn on debug mode and add your logger
to see what happening in runtime;
- [x] Add `Query.GetComponents<T>` and a couple overloads
to iterate through components more fast;
- [X] Add `GetModule<TModule>` that makes possible get
  dependencies from other module if they initialized;

### v0.7.x
- [x] Remove one-frame-components support - use `IEventSystem<T>`
instead of;
- [ ] Add one included global module

### v0.8.x
- [ ] Submodules