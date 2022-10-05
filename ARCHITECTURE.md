# Boss Room architecture overview

This document describes the high-level architecture of Boss Room.

If you want to familiarize yourself with the code base, you are just in the right place!

Boss Room is an 8-player co-op RPG game experience, where players collaborate to take down some minions, and then a boss. Players can select between classes that each have skills with didactically interesting networking characteristics. Control model is click-to-move, with skills triggered by mouse button or hotkey.

- [Assembly structure](#assembly-structure)
- [Application flow](#application-flow)
- [Game state and scene flow](#game-state-and-scene-flow)
- [Application Flow Diagram](#application-flow-diagram)
- [Transports](#transports)
- [Connection flow state machine](#connection-flow-state-machine)
- [Session management and reconnection](#session-management-and-reconnection)
- [UGS Services integration - Lobby and Relay](#ugs-services-integration---lobby-and-relay)
- [Core gameplay structure](#core-gameplay-structure)
- [Characters](#characters)
- [Game data setup](#game-data-setup)
- [Action System](#action-system)
  - [Movement action flow](#movement-action-flow)
- [Navigation system](#navigation-system)
  - [Building a navigation mesh](#building-a-navigation-mesh)
- [Important architectural patterns and decisions](#important-architectural-patterns-and-decisions)
- [Dependency Injection](#dependency-injection)
- [Client/Server code separation](#clientserver-code-separation)
- [Publisher-Subscriber Messaging](#publisher-subscriber-messaging)
  - [NetworkedMessageChannel](#networkedmessagechannel)

## Assembly structure

In Boss Room code is organized into a multitude of domain-based assemblies. Each assembly serves a relatively self-contained purpose.

An exception to this guideline is Gameplay assembly, which houses most of our networked gameplay logic and other functionality that is tightly coupled to the gameplay logic.

This assembly separation style forces us to have better separation of concerns and serves as one of the ways to keep the code-base organized. It also provides a benefit of more granular recompilation during our iterations, which saves us some time we would've spent looking at the progress bar.

![boss room assemblies](Documentation/Images/BossRoomAssemblies.png "Boss Room Assemblies")

## Application flow

Boss Room assumes that the `Startup` scene is loaded first.  \

> __An interesting trick__:
>
> We have an editor tool that enforces start from that scene even if we're working in some other scene. This tool can be disabled via an editor Menu: `Boss Room > Don't Load Bootsrap Scene On Play` and vice-versa via `Boss Room > Load Bootsrap Scene On Play`.

ApplicationController class serves as the entrypoint and composition root of the application. It lives on a GameObject in the Startup scene. Here we bind dependencies that should exist throughout the lifetime of the application - the core DI-managed “singletons” of our game.  \

> __Note__:
>
> `ApplilcationController` inherits from the `VContainer`'s `LifetimeScope` - a class that serves as a dependency injection scope and bootstrapper, where we can bind dependencies.

## Game state and scene flow

After the initial bootstrap logic is complete, the ApplicationController loads the `MainMenu` scene.

Each scene has its own entrypoint component sitting on a root-level game object. It serves as a scene-specific entrypoint, which, similar to ApplicationController binds dependencies (but these dependencies are local to the specific scene and will be released when the scene unloads). These `State` classes inherit from `LifetimeScope` too, and in the inspector they are set up to have ApplicationController to be their parent scope.

For MainMenu scene we only have the “MainMenuClientState”, however for the scenes that contain networked logic we also have the `server` counterparts to the client scenes, and both exist on the same game object.

As soon as we get into the CharSelect scene (either by joining or hosting a game) - our NetworkManager instance is running. The host drives game state transitions and also controls the set of scenes that are currently loaded in the game - this indirectly forces all the clients to load the same set of scenes as the server they are connected to (via Netcode's networked scene management).

> __Note__:
>
> In Inspector we can choose a parent scope for any `LifetimeScope`s - it’s useful to set a cross-scene reference to some parent scope (most commonly ApplicationController). This allows us to bind our scene-specific dependencies, and still have easy access to the global dependencies of the ApplicationController in our State-specific version of a `LifetimeScope` object.

### Application Flow Diagram

![boss room scene flow](Documentation/Images/BossRoomSceneFlowDiagram.png "Boss Room Scene Flow")

> __Note__:
>
> BossRoom scene consists of four scenes where the primary scene (BossRoom itself) contains the State components and other logic of the game, along with the navmesh for the level and the trigger areas that let the server know that it needs to load a given subscene.
>
> Subscenes contain spawn points for the enemies and visual assets for their respective segment of the level. The server unloads subscenes that don't contain any active players and then loads the subscenes that are needed based on the position of the players - if at least one player overlaps with the subscene's trigger area, the subscene is loaded.

## Transports

Currently two network transport mechanisms are supported:

- IP based

- Unity Relay Based

In the first, the clients connect directly to a host via IP address. This will only work if both are in the same local area network or if the host forwards ports.

For Unity Relay based multiplayer sessions, some setup is required. Please see our guide [here](Documentation/Unity-Relay/README.md).

Please see [Multiplayer over internet](README.md) section of our Readme for more information on using either one.

The transport is set in the transport field in the `NetworkManager`. We are using __Unity Transport Package (UTP)__.

Unity Transport Package is a network transport layer, packaged with network simulation tools which are useful for spotting networking issues early during development. This protocol is initialized to use direct IP to connect, but is configured at runtime to use Unity Relay if starting a game as a host using the Lobby Service, or joining a Lobby as a client. Unity Relay is a relay service provided by Unity services, supported by Unity Transport. See the documentation on [Unity Transport Package](https://docs-multiplayer.unity3d.com/docs/transport-utp/about-transport-utp/#unity-transport-package-utp) and on [Unity Relay](https://docs-multiplayer.unity3d.com/docs/relay/relay).

`StartingHostState` and `ClientConnectingState` are the ones that would need to be extended if we needed to add a new transport. Both of these classes assume that we are using UTP.

## Connection flow state machine

The Boss Room network connection flow is owned by the `ConnectionManager`, which is a simple state machine. It receives inputs from Netcode or from the user, and handles it according to its current state. Each state inherits from the ConnectionState abstract class. The following diagram shows how each state transitions to the others based on outside inputs.

![boss room connection manager state machine](Documentation/Images/BossRoomConnectionManager.png "connection manager state machine")

## Session management and reconnection

In order to allow the users to reconnect to the game and to be able to restore their game state - we are keeping a map of their GUIDs to their respective data. This way we ensure that when a player disconnects, some data is kept and accurately assigned back to that player when they reconnect.

For more information check out the page on [Session Management](https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/session-management/index.html) in NGO documentation.

## UGS Services integration - Lobby and Relay

Boss Room is a multiplayer experience that’s meant to be playable over internet, and this is why we’ve integrated [Unity Gaming Services](https://unity.com/solutions/gaming-services) – a combination of Authentication, Lobby, and Relay is what allows us to make it easy for players to host and join games that are playable over the internet, without the need for port forwarding or out-of-game coordination.

The following classes are of interest to learn more about our UGS wrappers and integration:

- [AuthenticationServiceFacade.cs](Assets/Scripts/UnityServices/Auth/AuthenticationServiceFacade.cs)
- [LobbyServiceFacade.cs](Assets/Scripts/UnityServices/Lobby/LobbyServiceFacade.cs)
- Lobby and relay - client join - JoinLobbyRequest() in [Assets/Scripts/Gameplay/UI/Lobby/LobbyUIMediator.cs](Assets/Scripts/Gameplay/UI/Lobby/LobbyUIMediator.cs)
- Relay Join - StartClientLobby() in [Assets/Scripts/ConnectionManagement/ConnectionState/OfflineState.cs](Assets/Scripts/ConnectionManagement/ConnectionState/OfflineState.cs)
- Relay Create - StartHostLobby() in [Assets/Scripts/ConnectionManagement/ConnectionState/OfflineState.cs](Assets/Scripts/ConnectionManagement/ConnectionState/OfflineState.cs)
- Lobby and relay - host creation - CreateLobbyRequest() in [Assets/Scripts/Gameplay/UI/Lobby/LobbyUIMediator.cs](Assets/Scripts/Gameplay/UI/Lobby/LobbyUIMediator.cs)

## Core gameplay structure

`Persistent Player` prefab is what goes into the `Player Prefab` slot in the `Network Manager` of the Boss Room. As such - there will be one spawned per client, with the clients owning their respective `Persistent Player` instances.

Note: there is no need to mark these `Persistent Player` instances as `DontDestroyOnLoad` - NGO automatically keeps these prefabs alive between scene loads while the connections are live.

The purpose of `Persistent Player` is to store synchronized data about player, such as their name, chosen avatar GUID etc.

This `PlayerAvatar` prefab instance, that is owned by the corresponding connected client, is destroyed by Netcode when a scene load occurs (either to `PostGame` scene, or back to `MainMenu` scene), or through client disconnection.

Inside `CharSelect` scene, clients select from 8 possible avatar classes, and that selection is stored inside PersistentPlayer's `NetworkAvatarGuidState`.

Inside `BossRoom` scene, `ServerBossRoomState` spawns a `PlayerAvatar` per PersistentPlayer present.

`ClientAvatarGuidHandler`, a `NetworkBehaviour` component residing on the `PlayerAvatar` prefab instance will fetch the validated avatar GUID from `NetworkAvatarGuidState`, and spawn a local, non-networked graphics GameObject corresponding to the avatar GUID.

### Characters

`ServerCharacter` contains NetworkVariables that store the state of any given character, and the server RPCs. It is responsible for executing or kicking off the server-side logic for the characters, which includes:

- movement and pathfinding via `ServerCharacterMovement` - it uses NavMeshAgent that lives on the server to translate the character’s transform, which is synchronized using NetworkTransform component;
- player action queueing and execution via `ServerActionPlayer`;
- AI logic via `AIBrain` (applies to NPCs);
- Character animations via `ServerAnimationHandler`, which themselves are synchronized using NetworkAnimator;

`ClientCharacter` primarily is a host for the `ClientActionPlayer` class, and it also contains the client RPCs for the character gameplay logic.

### Game data setup

Game data in Boss Room is defined in `ScriptableObjects`.

A singleton class [GameDataSource.cs](Assets/Scripts/Gameplay/GameplayObjects/RuntimeDataContainers/GameDataSource.cs) is responsible for storing all the actions and character classes in the game.

[CharacterClass](Assets/Scripts/Gameplay/Configuration/CharacterClass.cs) is the data representation of a Character, containing such things as its starting stats and a list of Actions that it can perform. This covers both player characters and NPCs alike.

[Action](Assets/Scripts/Gameplay/Configuration/Action.cs) subclasses represent discrete verbs (like swinging a weapon, or reviving someone), and are substantially data driven.

### Action System

Action System is a generalized mechanism for Characters to "do stuff" in a networked way. ScriptableObject-derived Actions are implementing both client and server logic of any given thing that the characters can do in the game.

We have a variety of actions that serve different purposes - some actions are generic and reused by different types of characters, and others are specific to the type of the character.

There is only ever one active Action (also called the "blocking" action) at a time on a character, but multiple Actions may exist at once, with subsequent Actions pending behind the currently active one, and possibly "non-blocking" actions running in the background.

The way we synchronize actions is by calling a `ServerCharacter.RecvDoActionServerRPC` and passing the `ActionRequestData`, a struct which implements `INetworkSerializable` interface.

> __Note__:
>
> `ActionRequestData` has a field of `ActionID`, which is a simple struct that wraps an integer, which stores the index of a given scriptable object Action in the registry of abilities available to characters, which is stored in `GameDataSource`.

From this struct we are able to reconstruct the action that was requested (by creating a pooled clone of the scriptable object Action that corresponds to the action we’re playing), and play it on the server. Clients will play out the visual part of the ability, along with the particle effects and projectiles.

On the client that is requesting an ability we can play an anticipatory animation, like for instance a small jump animation we play when the character receives movement input, but hasn’t yet been brought to motion by synchronized data coming from the server, that is actually computing the motion.

[Server]() and [Client]() ActionPlayers are companion classes to actions that are used to actually play out the actions on both client and server

#### Movement action flow

- Client clicks mouse on target destination.
- Client->server RPC, containing target destination.
- Anticipatory animation plays immediately on client.
- Server performs pathfinding.
- Once pathfinding is finished, server representation of entity starts updating its NetworkVariables at the same cadence as FixedUpdate.
- Visuals GameObject never outpaces the simulation GameObject, and so is always slightly behind and interpolating towards the networked position and rotation.

### Navigation system

Each scene which uses navigation or dynamic navigation objects should have a `NavigationSystem` component on a scene GameObject. That object also needs to have the `NavigationSystem` tag.

#### Building a navigation mesh

The project is using `NavMeshComponents`. This means direct building from the Navigation window will not give the desired results. Instead find a `NavMeshComponent` in the given scene e.g. a __NavMeshSurface__ and use the __Bake__ button of that script. Also make sure that there is always only one navmesh file per scene. Navmesh files are stored in a folder with the same name as the corresponding scene. You can recognize them based on their icon in the editor. They follow the naming pattern "NavMesh-<name-of-creating-object\.asset>"

## Important architectural patterns and decisions

In Boss Room we made several noteworthy architectural decisions:

### Dependency Injection

We use [Dependency Injection](https://en.wikipedia.org/wiki/Dependency_injection) pattern, with our library of choice being [VContainer](https://vcontainer.hadashikick.jp/).

DI allows us to clearly define our dependencies in code, as opposed to using static access, pervasive singletons or scriptable object references (aka Scriptable Object Architecture). Code is easy to version-control and comparatively easy to understand for a programmer, as opposed to Unity YAML-based objects, such as scenes, scriptable object instances and prefabs.

DI also allows us to circumvent the problem of cross-scene references to common dependencies, even though we still have to manage the lifecycle of MonoBehaviour-based dependencies by marking them with DontDestroyOnLoad and destroying them manually when appropriate.

### Client/Server code separation

Original problem: reading code with client and server code mixing together adds complexity and makes it easier to make mistakes. That code will often run in a single context, either client or server.

Boss Room explored different client-server code separation approaches. We decided in the end to revert our initial client/server/shared assemblies to a more classic domain driven assembly architecture, but still keeping certain more complex classes with a client/server separation.

Our initial thinking was separating assemblies by client and server would allow for easier porting to Dedicated Game Server (DGS) afterward. You’d only need to strip a single assembly to make sure that code only runs when necessary.

Issues with this approach:

- Callback hell: this makes code that should be trivial, too complex. You can look at our different action implementations in our 1.3.1 version to see this.
- Lots of components could be single simple classes instead of 3 class horrors.

We figured in the end this was not needed since:

- You can ifdef out single classes, there’s no need for asmdef stripping.
- ifdeffing classes isn’t 100% required. It’s a compile time insurance that certain parts of client side code will never run, but isn’t purely required.
  - We realized the little pros that’d help with stripping whole assemblies out in one go were outweighed by the complexity this added to the project.
- Most client/server class couples are tightly coupled and will call one another (they are two splitted implementations of the same logical object after all). Separating them in different assemblies makes you create “bridge classes” so you don’t have circular dependencies between your client and server assemblies. By putting your client and server classes in the same assemblies, you allow those circular dependencies in those tightly coupled classes and make sure you remove unnecessary bridging and abstractions.
- Whole assembly stripping has issues with NGO. NGO doesn’t support NetworkBehaviour stripping. Components related to a NetworkObject need to match client and server side, else it’ll mess with your NetworkBehaviour indexing.

After those experimentations, we established new rules for the team:

- Domain based assemblies
- Use single classes for small components (think the boss room door with a simple on/off state)
  - If your class never grows too big, having a single `NetworkBehaviour` is perfectly fine and easy to keep the whole class in mind when maintaining it.
- Use client and server classes (with each pointing to the other) for client/server separation.
  - Client/Server pair is in the same assembly.
  - If you start up the game as a client, the server components will disable themselves, leaving you with `{Client}` components executing (make sure you don’t completely destroy the server components, as NGO still requires them for network message sending.
  - Client would have an m_Server and Server would have an m_Client property.
  - Server class would own server driven netvars, same for Client with Owner driven netvars.
  - This way, when reading server code, you do not have to mentally skip client code and vice versa. This helps make bigger classes more readable and maintainable.
- Use partial classes when the above isn’t possible
  - Still use the Client/Server prefix for keeping each context in your mind.
  - Note: you can’t use prefixes for ScriptableObjects that have file name requirements to work.
- Use Client/Server/Shared separation when you have a 1 to many relationship where your server class needs to send info to many client classes.
  - This can also be achieved with our NetworkedMessageChannel

You still need to take care of code executing in `Start` and `Awake`: if this code runs contemporaneously with the `NetworkManager`'s initialization, it may not know yet whether the player is a host or client.

### Publisher-Subscriber Messaging

We have implemented a DI-friendly Publisher-Subscriber pattern (see Infrastructure assembly, PubSub folder).

It allows us to send and receive strongly-typed messages in a loosely-coupled manner, where communicating systems only know about the IPublisher/ISubscriber of a given message type. And since publishers and subscribers are classes, we can have more interesting behavior for message transfer, such as Buffered messages (that keep the last message that went through the pipe and gives it to any new subscriber) and networked messaging (see NetworkedMessageChannel section).

This mechanism allows us to both avoid circular references and to have a more limited dependency surface between our assemblies - cross-communicating systems rely on common messages, but don't necessarily need to know about each-other, thus allowing us to more easily separate them into smaller assemblies.

It allows us to avoid having circular references between assemblies, the code of which needs only to know about the messaging protocol, but doesn't actually need to reference anything else.

The other benefit is strong separation of concerns and coupling reduction, which is achieved by using PubSub along with Dependency Injection. DI is used to pass the handles to either `IPublisher` or `ISubscriber` of any given event type, and thus our message publishers and consumers are truly not aware of each-other.

`MessageChannel` classes implement these interfaces and provide the actual messaging logic.

#### NetworkedMessageChannel

Along with in-process messaging, we have implemented the `NetworkedMessageChannel`, which uses the same API, but allows us to send data between peers. The actual netcode synchronization for these is implemented using custom NGO messaging. It serves as a useful synchronization primitive in our arsenal.
