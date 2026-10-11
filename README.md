# ActorFramework
[![Unity Version](https://img.shields.io/badge/unity-6000.0+-000.svg)](https://unity3d.com/get-unity/download/archive)
![Unity Tests](https://github.com/DanilChizhikov/ActorFramework/actions/workflows/tests.yml/badge.svg?branch=master)

ECS-like actor framework for Unity. Actors are composed of struct features stored in dense pools, systems iterate cached queries, and an Actor Debugger window shows and edits live worlds in the Editor and in Development Builds.

## Table of Contents
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Manual Installation](#manual-installation)
  - [UPM Installation](#upm-installation)
- [Features](#features)
- [Usage](#usage)
  - [Define features](#define-features)
  - [Create and run a world](#create-and-run-a-world)
  - [Spawn actors](#spawn-actors)
  - [Read and write features](#read-and-write-features)
  - [Write a typed system](#write-a-typed-system)
  - [Write a manual system](#write-a-manual-system)
  - [Queries](#queries)
  - [Activation](#activation)
  - [Structural changes](#structural-changes)
  - [Feature type budget](#feature-type-budget)
- [Actor Debugger](#actor-debugger)
  - [Naming worlds](#naming-worlds)
  - [Choosing feature fields](#choosing-feature-fields)
  - [Debugging a device](#debugging-a-device)
- [Tests](#tests)
- [License](#license)

## Getting Started

### Prerequisites
- [GIT](https://git-scm.com/downloads)
- [Unity](https://unity.com/releases/editor/archive) 6000.0+

### Manual Installation
1. Download the .unitypackage from the [releases](https://github.com/DanilChizhikov/ActorFramework/releases/) page.
2. Import com.dtech.actor-framework.x.x.x.unitypackage into your project.

### UPM Installation
1. Open the manifest.json file in your project's Packages folder.
2. Add the following line to the dependencies section:
    ```json
    "com.dtech.actor-framework": "https://github.com/DanilChizhikov/ActorFramework.git",
    ```
3. Unity will automatically import the package.

If you want to set a target version, ActorFramework uses the `v*.*.*` release tag so you can specify a version like #v1.0.0.

For example `https://github.com/DanilChizhikov/ActorFramework.git#v1.0.0`.

## Features
- **Struct features, no boxing.** Features are plain structs stored in one sparse-set pool per type. Every generic call is constrained to `struct, IActorFeature`, and feature access returns `ref` values.
- **Cached queries.** A query keeps its member list up to date on structural change, so a system walks a flat array instead of matching actors every frame. Queries with the same shape are shared.
- **Two system tiers.** `ActorSystem<T1..T4>` calls `Execute` once per actor with `ref` access to its features; `ActorSystemBase` hands you the world and the query and lets you write the loop yourself.
- **Three update phases.** `Update`, `FixedUpdate` and `LateUpdate`, driven by your own MonoBehaviour or game loop.
- **Safe structural changes.** Systems may create, destroy, (de)activate actors and add or remove features while iterating. Query membership is synced after each system.
- **Token based activation.** Any number of activators and deactivators per actor; deactivators always win. Inactive actors are left out of queries by default.
- **Actor Debugger.** An Editor window that shows every live world, its actors, features, systems and queries, edits feature fields in place and connects to Development Builds on a device.
- **Engine-free runtime.** The runtime assembly has no `UnityEngine` reference.

## Usage

All runtime types live in the `DTech.ActorFramework` namespace.

### Define features

A feature is a struct that implements `IActorFeature` and holds data only. All behaviour lives in systems.

```csharp
public struct Position : IActorFeature
{
    public Vector3 Value;
}

public struct Velocity : IActorFeature
{
    public Vector3 Value;
}

public struct Dead : IActorFeature
{
}
```

Never assign a feature to an `IActorFeature` variable: that boxes it.

### Create and run a world

A world owns actors, feature pools, queries and systems. Register the systems, call `Initialize`, drive the update phases and dispose the world when you are done. Every member of `ActorWorld` is main-thread only.

```csharp
public sealed class GameplayLoop : MonoBehaviour
{
    private ActorWorld _world;

    private void Awake()
    {
        _world = new ActorWorld("Gameplay");
        _world.AddSystem(new MovementSystem());
        _world.Initialize();
    }

    private void Update() => _world.Update(Time.deltaTime);

    private void FixedUpdate() => _world.FixedUpdate(Time.fixedDeltaTime);

    private void LateUpdate() => _world.LateUpdate(Time.deltaTime);

    private void OnDestroy() => _world.Dispose();
}
```

- `Initialize` initializes the systems registered so far, in registration order. A system added later is initialized when it is added.
- Systems run in registration order within each phase. A system takes part in every phase whose interface it implements: `IUpdateSystem`, `IFixedUpdateSystem`, `ILateUpdateSystem`.
- `RemoveSystem` unregisters a system without disposing it. `Dispose` disposes the remaining systems in reverse order and destroys every actor.
- Updating a world before `Initialize` throws.

### Spawn actors

`CreateActor` creates an empty actor. `BuildActor` is the spawn fast path: features are written straight into their pools and the queries are synced once, on `Build`, instead of once per feature.

```csharp
Actor bullet = _world.BuildActor()
    .With(new Position { Value = origin })
    .With(new Velocity { Value = direction * speed })
    .Build();

Actor marker = _world.CreateActor();
marker.AddFeature(new Position { Value = point });
```

A builder that is never built leaves an actor that no query can see.

Destroy an actor with `actor.Destroy()` or `world.DestroyActor(actor)`. Actors are never pooled: a destroyed actor stays destroyed, so it is safe to keep a reference and check `IsAlive`. `Actor.OnDestroyed` and the world events `OnActorCreated`, `OnActorDestroying`, `OnActorDestroyed`, `OnFeatureAdded` and `OnFeatureRemoved` let you observe the lifecycle.

### Read and write features

```csharp
if (actor.HasFeature<Velocity>())
{
    ref Velocity velocity = ref actor.GetFeature<Velocity>();
    velocity.Value *= 0.5f;
}

if (actor.TryGetFeature(out Position position))
{
    Debug.Log(position.Value);
}

ref Position target = ref actor.GetOrAddFeature<Position>();
actor.RemoveFeature<Dead>();
```

- `GetFeature` throws when the feature is missing, `AddFeature` throws when it is already there.
- A `ref` returned by any of these calls is invalidated by the next structural change. Never hold it across one.

### Write a typed system

`ActorSystem<T1>` ... `ActorSystem<T1, T2, T3, T4>` query the actors that have every listed feature and call `Execute` once per actor. Override `ConfigureQuery` and call `base` to narrow the query further.

```csharp
public sealed class MovementSystem : ActorSystem<Position, Velocity>
{
    protected override void ConfigureQuery(ActorQueryBuilder builder)
    {
        base.ConfigureQuery(builder);
        builder.Without<Dead>();
    }

    protected override void Execute(in ActorSystemContext<Position, Velocity> context)
    {
        ref Position position = ref context.Feature1;
        position.Value += context.Feature2.Value * context.DeltaTime;
    }
}
```

Each `FeatureN` property resolves through the pool on every access, so take it into a `ref` local once. `OnBeforeUpdate` and `OnAfterUpdate` run around the loop. Typed systems run in the `Update` phase.

### Write a manual system

`ActorSystemBase` gives a system its `World` and the `Query` built in `ConfigureQuery` and leaves the loop to you. Implement the phase interfaces you need.

```csharp
public sealed class GravitySystem : ActorSystemBase, IFixedUpdateSystem
{
    private FeaturePool<Velocity> _velocities;

    public override void Initialize(ActorWorld world)
    {
        base.Initialize(world);
        _velocities = world.GetPool<Velocity>();
    }

    public void FixedUpdate(float fixedDeltaTime)
    {
        ActorQuery query = Query;
        for (int i = query.Count - 1; i >= 0; i--)
        {
            Actor actor = query.GetActorAt(i);
            if (!query.IsMatching(actor))
            {
                continue;
            }

            _velocities.GetUnchecked(actor.Id).Value += Physics.gravity * fixedDeltaTime;
        }
    }

    public override void Dispose()
    {
        _velocities = null;
        base.Dispose();
    }

    protected override void ConfigureQuery(ActorQueryBuilder builder)
    {
        builder.With<Velocity>().Without<Dead>();
    }
}
```

Iterate backwards and skip actors for which `IsMatching` is `false`: a system may destroy, deactivate or strip actors that are still listed in the query it is iterating.

When a system does not need a query filter, walk a `FeaturePool<T>` directly: `Values` and `ActorIds` are parallel dense arrays valid for `[0, Count)`. Never cache them across a structural change, they are replaced when the pool grows.

### Queries

```csharp
ActorQuery enemies = _world.CreateQuery()
    .With<Position, Health>()
    .WithAny<EnemyTag, BossTag>()
    .Without<Dead>()
    .Build();

for (int i = 0; i < enemies.Count; i++)
{
    Actor enemy = enemies.GetActorAt(i);
}
```

- `With` requires every listed feature, `WithAny` at least one of them, `Without` none of them. Each call takes up to three features (two for `Without`) and can be repeated.
- Inactive actors are left out unless the query calls `IncludeInactive()`.
- Building the same shape twice returns the same `ActorQuery`.
- `Contains` and `GetActorsNonAlloc` are available as well.

### Activation

Activity is controlled by tokens: any reference type instance can be an activator or a deactivator.

```csharp
actor.AddDeactivator(stunEffect);   // the actor turns inactive
actor.RemoveDeactivator(stunEffect); // and active again once no deactivator is left

Actor turret = _world.BuildActor()
    .With(new Position())
    .StartInactive()
    .Build();

turret.AddActivator(powerSource);   // active while it has at least one activator
```

- Actors are active by default; on such an actor activators are only recorded.
- An actor built with `StartInactive()` stays inactive until it gets an activator and turns inactive again when the last one is removed.
- Deactivators always win.
- `Actor.OnActivated` and `Actor.OnDeactivated` report the transitions; `GetActivatorsNonAlloc` and `GetDeactivatorsNonAlloc` list the tokens.

### Structural changes

Creating, destroying and (de)activating actors and adding or removing features are structural changes. Inside a system they are safe at any point:

- Query membership is synced after each system, not in the middle of one. Until then an actor stays listed in a query it no longer matches, which is what `IsMatching` checks.
- A destroyed actor is torn down at the same point; its features are removed and `OnDestroyed` fires.
- Outside of a system, changes are synced immediately.
- `AddSystem` and `RemoveSystem` can be called from inside an update. A system added during a phase is not run by the phase in progress; a removed one is not run again.
- Disposing the world from inside an update or a structural event throws, and an update phase cannot be re-entered.

### Feature type budget

Feature type ids are global and capped at 256 per process. Assert against the budget at startup to fail fast instead of discovering the cap mid-session:

```csharp
_world.GetFeatureTypeBudget(out int used, out int capacity);
```

`PreallocateFeatures<T>(capacity)` sizes a pool up front to avoid growing it during gameplay.

## Actor Debugger

`Window > DTech > Actor Debugger` shows every live `ActorWorld`: its actors, their activity, activators and deactivators, the features each actor holds and the values of their fields. Fields can be edited in place. The Systems and Queries tabs list the registered systems (with their update phases) and queries (with their feature filters and match counts); **Actors** on a query filters the actor list down to its members.

### Naming worlds

```csharp
var world = new ActorWorld("Gameplay");
```

A world created without a name is shown as `ActorWorld #N`.

### Choosing feature fields

Public instance fields are shown by default. Use the attributes to change that:

```csharp
public struct Health : IActorFeature
{
    public float Current;

    [HideInActorDebugger]
    public float CachedRatio;

    [ShowInActorDebugger]
    private float _regenTimer;

    [field: ShowInActorDebugger]
    public float Max { get; private set; }
}
```

`readonly` fields are shown but cannot be edited. Unity's `[Range]`, `[Tooltip]`, `[Header]`, `[TextArea]` and `[Multiline]` are respected.

### Debugging a device

Worlds are tracked in the Editor and in Development Builds. To inspect a player:

1. Make a Development Build and run it on the device.
2. Open the Actor Debugger and pick the player in the connection dropdown on the left of the toolbar.

The window then reads the player through `PlayerConnection` and sends field edits back to it. The agent that answers lives in the `com.dtech.actor-framework.diagnostics` assembly, which is compiled only for the Editor and Development Builds. Define `DTECH_ACTOR_DEBUGGER_DISABLED` to leave it out of Development Builds too; that also removes the window.

> **Security:** any Editor that connects to a Development Build with the agent can read and change the state of its worlds. Do not ship Development Builds to untrusted users.

## Tests

The package ships EditMode tests in `com.dtech.actor-framework.editor-mode-tests`. When the package is installed through UPM, list it in `testables` of `Packages/manifest.json` so the Test Runner picks the tests up:

```json
"testables": [
    "com.dtech.actor-framework"
]
```

The performance benchmarks in `com.dtech.actor-framework.performance-tests` are opt-in:

1. Add `com.unity.test-framework.performance` to your project.
2. Add `UNITY_INCLUDE_PERFORMANCE_TESTS` to the Scripting Define Symbols.

## License

ActorFramework is distributed under the MIT License. See [LICENSE](LICENSE) for details.
