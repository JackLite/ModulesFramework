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
        world.InitModule<BattleModule>(true);
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
        world.InitModule<BattleModule>(true);
        // read settings from some JSON or anything else
        _dependencies[typeof(Settings)] = settings;
        return Task.CompletedTask;
    }
    
    public override object GetDependency(Type t)
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
        world.CreateOneData(wallet);
        world.InitModule<BattleModule>(true);
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

### Submodules

In the large project it will be good to keep thing as simple as possible. There is can be hundreds of dependencies and thousands of systems. To simplify complexity you can use submodules. 

*Submodule* is just an another module but it has some differences. First of all submodule inherits dependencies from parent (and grandparent and so on).

For creation of submodule you need just create module as usual and then add ```SubmoduleAttribute``` to it:

```csharp
[Submodule(typeof(ParentModule), initWithParent: true, activeWithParent: true)]
public class LootModule : EcsModule {}
```

Parameters ```initWithParent``` and ```activeWithParent``` are optional and has ```true``` as default. So the second things about submodule is that they initialized and activated with parent module. You can turn off that behaviour by  ```initWithParent``` and ```activeWithParent```.

The order of executing init and activate parent module and submodules below:
1. Parent module calls setup;
2. Submodule calls setup;
3. Parent module ```IPreInitSystem``` and ```IInitSystem``` calls;
4. Submodule ```IPreInitSystem``` and ```IInitSystem``` calls;
5. Parent module activation (including ```IActivateSystem```);
6. Submodule activation.

The order of destroy:
1. Submodule deactivation;
2. Parent module deactivation;
3. Submodule destroy;
4. Parent module destroy.

### Multiple Components
What if you making the cool dynamic game with a lot of thins that happened simultaneously. Hundreds of entities fighting each other, long term effects continiously damage everyone. Base on who damage who the AI change the aggression or healing or buffing. And by the way the damage type can be different. So you need to know value of damage, it's type and source.

```csharp
public struct Damage 
{
    public float value;
    public DamageType type;
    public Entity source;
}
```

Then you add this component to damaged entity. After that every frame you process alive entities with damage and health components. And everything seems fine. But what if more then one damage component will be added? In MF like in some other frameworks the damage component will be replaced by the new one. Thus previous damage will be lost. Sounds not good.

You can find different ways to workaround. For example it's not a bad idea to convert component's fields to arrays. However the MF introduce concept of the Multiple Components.

Please use this feature very accurate. Because it may overcomplicate the code you should use multiple components only when it has clear sense like with the damage or stacking buffs. 

Multiple components have a different api in half of cases so you always know with what you work. Here is some examples:

```csharp
// add new components
entity.AddNewComponent(damage1);
entity.AddNewComponent(damage2);

// get all multiple components from entity
entity.GetAll<Damage>();

// get indices iterator of components at entity
var indicesIt = entity.GetIndices<Damage>();

// remove component from entity by index
indicesIt.RemoveAt<Damage>(index);

// remove all compnents
entity.RemoveAll<Damage>();

// iterate by query
var query = world.Select<MultipleComponent>();
foreach (ref var damage in query.GetMultipleComponents<Damage>()){}
```
In the cases like a `HasComponent<T>` multiple components behave like expected.

### Multiple Worlds

There are cases when you may want to have more then one worlds with their own modules or even with shared modules. For example for the host mode in online game. So all common logic will be in one world and local player logic in another. Anyway this feature is very rare need but because it's remove some unbreakable limits it's was added in core of MF.

Here is example of working with worlds.
```csharp
// just pass number of worlds when creating Ecs object
var ecs = new Ecs(2);

// get second world
var secondWorld = ecs.GetWorld(1); // 1 - index of world, starting from 0

// get main world - it is the world on index 0
var mainWorld = ecs.MainWorld
var mainWorldOtherWay = ecs.GetWorld(0);
```

One module can be included in any count of worlds. By default modules include only in main world.

```csharp
public class SomeModule : EcsModule
{
    // here we override hash set of indices of worlds
    public override HashSet<int> WorldIndex { get; } = new(){0, 1};
}
```

**Note**: systems belong to module will run on every system. For example `IRunSystem`s run twice for module above. Same with all other systems. It allows to use same systems in different world making shared logic.

## FAQ

##### How to create an instance of system?

You should never create instance of system by your own. 
Just create a class, implement one ore more interfaces
(see section [systems](#api-systems) in [API](#z24api)) and add `EcsSystem` attribute.

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

## Best practice

### Getting data

Here's the range of speed of getting components:
1. `GetRawData` - as fast as the simple array;
2. getting ecs table and iterate through entities id from query - it is 7 times slower then first method but still very fast cause we get data from table itself;
3. iterate through components by `Query.GetComponents<T>` - slightly slower then previous;
4. iterate through entities and getting component from entity - this is teh slowest way cause every time we getting component from entity (or from world) MF checks if table exists.

Note that this range has sense when there is thousands of components. In the other cases you can use any method.

**Important**: you may want to workaround limit of `GetRawData` by storing entity inside of component. Still you can do this it's a very bad decision if you want to remove same component from this entity or destroy entity itself. Because components stores on dense array when some of them destroyed last component will be moved in place of removed. So you may miss some data. But even worse because `GetRawData` returns slide of dense array you may iterate some data twice! Consider using entities id and getting data from ecs table. It's safer and fast enough for the most cases. Still if you want to get access to entity and it's critical please let me know.

### Multiple components

Cause it may lead to very complex code you must use multiple components only when it simplify the way to make system. Do not use them when you not sure it's the best option.

### Modules

- follow "long initialization, fast activation" principle;
- do not forget delete components when modules destroyed;
- do not create very many small modules. Start with big one and separate them along project grow;
- any global module must be strongly separated from any other global module;
- feel free to expand module setup for your favorite DI; 

### Systems

- create OneData in IPreInit. Fill in IInit;
- do not forget using IDeactivate when there is IActivate;
- use services or static stateless utils for common logic;
- make small systems (less then 100-200 code lines is good metric);

### Query

- use `using` keyword when you select components. It will safe memory and time;
- start `Select<T>` from components with lesser count. It will reduce any `With<T>`, `Without<T>` and `Where` calls and iterations;

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
- `void OnSetupEnd()` - virtual method, calls when all dependencies updated before any systems of module;
- `void OnInit()` - Calls after all `IPreInitSystem` and `IInitSystem` proceed but before activation if activated immediately;
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
type `T`. Use it for very fast iterations through components but do not allow to change entity.

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

- `int Id` - id of entity. You can always use entity id to get same things from world that you get from entity;
- `bool IsAlive()` - checks if entity still exists. Use it when you stored the entity for a while;
- `void Destroy()` - destroy entity;
- `bool HasComponent<T>()` - return true if `T` bind to `Entity`;

##### Add and remove single components

- `Entity AddComponent(T)` - adds component `T` to `Entity`. If component exists this method removes old and add new;
  In fact it creates element in `EcsTable<T>` and bind it
  to entity by entity id. `T` is a struct;
- `Entity RemoveComponent(T)` - removes component `T`
  from `Entity`. Again, it removes element from `EcsTable<T>`.
  If there is no `T` do nothing. `T` is a struct;
- `ref T GetComponent<T>()` - return reference to `T`
  at `Entity`. If there is no `T` returns `default`.
  Use `HasComponent<T>()` if you not sure. Also use
  more specific `Query` to avoid the case;

##### Working with multiple components

- `Entity AddNewComponent<T>(T component)` - adds component `T` to `Entity`. If component exists this method add one more component;
- `Span<int> GetIndices<T>()` - returns indices of internal data where `T` components could be found for entity;
- `ref T GetComponentAt<T>(int index)` - allows to get `T` component at the index;
- `MultipleComponentsEnumerable<T> GetAll<T>()` - returns all `T` components from the entity;
- `Entity RemoveAll<T>()` - removes all `T` components from entity;
- `int Count<T>()` - returns count of `T` components for entity;

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
- `MultipleComponentsQueryEnumerable<T> GetMultipleComponents<T>()` - returns enumerable to iterate through multiple components;
- `Query WhereAll<T>(Func<T, bool> customFilter)` - allows to filter entities where all multiple components pass the filter;
- `Query WhereAny<T>(Func<T, bool> customFilter)` - allows to filter entities where some of multiple components pass the filter;

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

`Ecs` is a entry point to modules framework.

- `DataWorld MainWorld` - world at 0 index;
- `async void Start()` - inits and activate all global modules (all exceptions checks internally);
- `void Run()` - calls `Run()` on `IRunSystem`;
- `void PostRun()` - calls `PostRun()` on `IPostRunSystem`;
- `void RunPhysic()` - calls `RunPhysic()` on `IRunPhysicSystem`;
- `void Destroy()` - destroys all modules;
- `DataWorld GetWorld(int index)` - returns world by index;

### Attributes

- `EcsSystemAttribute` - marks that class is system.
Takes one argument about to what module belongs the system;
- `GlobalModuleAttribute` - marks that `EcsModule` is global;
- `GlobalSystemAttribute` - marks that system not in module,
so it will run all the time and can't contain any dependency but DataWorld;
- `SubmoduleAttribute` - marks that module is a submodule of other module;
