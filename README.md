# ActorFramework

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
