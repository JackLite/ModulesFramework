# ModulesFramework

## API

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
- `void OnActivate(), void OnDeactivate` - virtual methods
that calls when you activate or deactivate module 
(see `EcsWorld.ActivateModule<T>` and `EcsWorld.DeactivateModule<T>` );
- `void OnDestroy()` - like an `OnDeactivate()` but calls
when module destroyed (see `EcsWorld.DestroyModule()`). 
It should be used for release any resources initialized 
by `Setup()`;
- `Dictionary<Type, int> GetSystemsOrder()` - allows to 
set order to concrete system. By default all systems has
0 order. Ordering by ascending;

### DataWorld

`DataWorld` is the main class you should use for 
retrieve and create any data.

##### Work with Entities

- `Entity NewEntity()` - creates empty entity. 
`OnEntityCreated` event called at this point.
- `Entity CreateOneFrame()` - create `Entity` and add 
`OneFrameComponent` to it. That `Entity` will be destroyed
after all systems in `PostRun`;

##### Queries

- `Query Select<T>` - main method to get any entities.
See about `Query` below for more information;
- `bool Exist<T>()` - fast way to check if there exists
any entities with `T`;
- `bool TrySelectFirst<T>(out T)` - select first `Entity`
with `T`. If there is no such entities return false. 
Be careful: out parameter is not a reference.
`T` is a struct;

##### Modules

- `void InitModule<T>(bool activateImmediately = false)`
\- initialize module `T`;
- `void InitModule<TModule, TParent>(bool activateImmediately = false)` -
initialize module `T` as *submodule*. Submodule has the
same dependencies as parent. But submodule do not 
share lifecycle (activation, deactivation and destroying);
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
- `void DestroyAll()` - helper for destroy all entities
from query;
- `int Count()` - count of entities corresponds to query;


### Systems

Systems works with data: create data, change it and destroy.
There is several types of system's interfaces:

- `IPreInitSystem` - calls once when module initialized;
- `IInitSystem` - calls once when module initialized *after*
every `IPreInitSystem`s works;
- `IActivateSystem` - calls once when module activated;
- `IDeactivateSystem` - calls once when module deactivated;
- `IRunSystem` - calls every `Ecs.Run()`;
- `IPostRunSystem` - calls every `Ecs.PostRun()`;
- `IRunPhysicSystem` - calls every `RunPhysic()`'
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