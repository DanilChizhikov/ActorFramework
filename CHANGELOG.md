# Changelog

## [1.0.0] - 2026-10-11

Initial release.

### Added
- **World**
  - `ActorWorld` owns actors, feature pools, queries and systems. It can be named (`new ActorWorld("Gameplay")`); an unnamed world is called `ActorWorld #N`. Every member is main-thread only
  - Lifecycle: `Initialize`, `Update`, `FixedUpdate`, `LateUpdate`, `Dispose`, with the current state in `Phase`, `IsInitialized` and `IsDisposed`. Updating a world that is not initialized throws
  - Events: `OnActorCreated`, `OnActorDestroying`, `OnActorDestroyed`, `OnFeatureAdded`, `OnFeatureRemoved`
  - `TryGetActor`, `GetActorsNonAlloc`, `ActorCount`, `SystemCount`, `QueryCount`
- **Actors**
  - `Actor` with a stable `Id`, `IsAlive`, `IsActive`, `FeatureCount` and the `OnActivated`, `OnDeactivated`, `OnDestroyed` events. Actors are never pooled, so a destroyed actor stays destroyed and a held reference is safe to check
  - `ActorWorld.CreateActor` for an empty actor, `ActorWorld.BuildActor` for the spawn fast path: `ActorBuilder.With<T>(value)` writes features straight into their pools and `Build()` syncs the queries once
  - `Actor.Destroy` / `ActorWorld.DestroyActor`
- **Features**
  - Features are structs implementing `IActorFeature`. Generic calls are constrained to `struct, IActorFeature` and never box
  - `AddFeature`, `GetFeature`, `GetOrAddFeature`, `TryGetFeature`, `RemoveFeature` and `HasFeature` on `Actor`; the getters return `ref` values
  - `FeaturePool<T>`: a sparse set per feature type with O(1) add, remove and lookup, exposed through `ActorWorld.GetPool<T>` with `Values`, `ActorIds`, `Has`, `GetUnchecked`, `GetAt` and `GetActorIdAt` for hot loops
  - `ActorWorld.PreallocateFeatures<T>(capacity)` to size a pool up front
  - Up to 256 feature types per process; `ActorWorld.GetFeatureTypeBudget(out used, out capacity)` reports the budget
- **Activation**
  - Token based activity: `AddActivator`, `RemoveActivator`, `AddDeactivator`, `RemoveDeactivator`, `ContainsActivator`, `ContainsDeactivator`, `GetActivatorsNonAlloc`, `GetDeactivatorsNonAlloc`. Deactivators always win
  - `ActorBuilder.StartInactive()`: the actor stays inactive until it gets an activator and turns inactive again when the last one is removed
- **Queries**
  - `ActorWorld.CreateQuery()` with `With`, `WithAny`, `Without` (up to three features per call) and `IncludeInactive`, then `Build()`. Queries with the same shape are shared
  - `ActorQuery` keeps its members up to date on structural change, so iterating it is a flat array walk: `Count`, `GetActorAt`, `GetActorsNonAlloc`, `Contains`
  - `ActorQuery.IsMatching` tells whether an actor taken from the query still belongs in it before the next sync
  - Inactive actors are excluded unless the query asks for them
- **Systems**
  - `IActorSystem` with the `IUpdateSystem`, `IFixedUpdateSystem` and `ILateUpdateSystem` phases. A system can implement several phases; systems run in registration order
  - `ActorSystemBase`: the manual tier, which gives a system its `World` and the `Query` built in `ConfigureQuery`
  - `ActorSystem<T1>` ... `ActorSystem<T1, T2, T3, T4>`: the typed tier, which calls `Execute(in ActorSystemContext<...>)` once per matching actor, with `ref` access to `Feature1` ... `Feature4`, plus `OnBeforeUpdate` and `OnAfterUpdate` hooks
  - `AddSystem` and `RemoveSystem` work at any time, also from inside an update. A system added to an initialized world is initialized at once; `RemoveSystem` does not dispose the system
- **Structural changes**
  - Creating, destroying, (de)activating actors and adding or removing features from inside a system is safe. Query membership is synced after each system, and destroyed actors are torn down at the same point
  - Disposing the world from inside an update or a structural event throws, and an update phase cannot be re-entered
- **Actor Debugger**
  - `Window > DTech > Actor Debugger` lists every live `ActorWorld`, its actors with their activity, activators and deactivators, the features they hold and the values of their fields, which can be edited in place
  - Systems tab with the update phases of every system, Queries tab with the feature filters and match count of every query, and **Actors** on a query to filter the actor list down to its members
  - `[ShowInActorDebugger]` and `[HideInActorDebugger]` to choose which feature fields are shown. Unity's `[Range]`, `[Tooltip]`, `[Header]`, `[TextArea]` and `[Multiline]` are respected, `readonly` fields are read-only
  - Debugging a Development Build on a device through `PlayerConnection`, by picking the player in the connection dropdown
  - The `com.dtech.actor-framework.diagnostics` assembly is compiled only for the Editor and Development Builds. `DTECH_ACTOR_DEBUGGER_DISABLED` removes it and the window
- **Tests**
  - EditMode tests for the world, actors, activation, queries, feature pools, structural changes, allocations and the debugger layer
  - Performance benchmarks, opt-in through `com.unity.test-framework.performance` and the `UNITY_INCLUDE_PERFORMANCE_TESTS` define
