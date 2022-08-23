
References:
 - [Latency and Packet Loss](https://docs-multiplayer.unity3d.com/netcode/current/learn/lagandpacketloss)
```
TODO: 
  - add references to LP's Session Management doc
  - add reference to LP's connection management doc
  - add reference to LP's performance doc
  - point to Fernando's parenting doc
```

### TEMP Important facts

```
TODO:
move these bits of information to their appropriate places in the document
```

 - It's impossible to establish connection between release and debug builds.

 - We use additive scene loading when it made sense. Specifically our core gameplay scene BossRoom is comprised of four scenes where the primary scene contains the State components and other logic of the game and the navmesh for the level and the trigger areas that let the server know that it needs to load a given subscene. Subscenes contain spawn points (**CURRENTLY IT CONTAINS STATIC NETWORK OBJECTS ACTUALLY**) for the enemies and visual assets for their respective segment of the level. The server unloads subscenes that don't contain any active players and then loads the subscenes that are needed based on the position of the players - if at least one player overlaps with the subscene's trigger area, the subscene is loaded.
   - point to Fernando's doc on static vs dynamic spawn - it covers an important sceen architecture tidbit: basically objects that are statically places shouldn't be Destroyed on the server
   - at editor time we have a special utility `EditorChildSceneLoader` that automatically loads subscenes when we open the primary scene.
   
 - Startup scene hosts the entrypoint into the game - the ApplicationController, and other dependencies that exist for the duration of the game. We never return to this scene from anywhere else. Startup scene is always loaded first, and we have an editor tool that enforces start from that scene even if we're working in some other scene. This tool can be disabled via an editor Menu: `Boss Room > Don't Load Bootsrap Scene On Play` and vice-versa via `Boss Room > Load Bootsrap Scene On Play`.
 - Action system overview:
   - Actions are implementing both client and server logic of any given thing that the characters can do in the game
   - Actions are Scriptable Objects that are registered within GameDataSource - these references serve as runtime "prototype" actions from which clones are created to enact a given action. We transmit integer ID's that map to actions in the GameDataSource to decide which ability we need to play in response.
   - Server and Client ActionPlayers are companion classes to actions that are used to actually play out the actions on both client and server.

  
### Lobby and Relay integration

Boss Room has a lobby system that allows players to find and join games, and a relay system behind the scenes that allows all the players to easily connect to the host.

```
TODO: 
 - cleanup existing doc
 - SDK integrations and how they are wrapped and isolated
 - Shutdown logic (disconnect vs quit)


States:
 - when we start a server - we do autoswitch to the character selection things
 - Our lobby and relay servers are synced using the functionality that relay provides (point to it), plus we handle odd cases manually
 - CharacterSelect is just a fat wrapper around NetworkList 
 - BossRoom scene with three additive scenes and the loading UI that handles progress bars
 - Check Infrastructure folder for things that are worth mentioning

 

 - Talk about a shared netvar with winstate (singleton-esque thing that gets cleaned up and re-created - pure static state at it's verbatim)


 - client server separation and lessons learned from that:
   - it can help with asset stripping
   - 


https://jira.unity3d.com/browse/MTT-4267 :
 - this needs to be additionally scoped with Morwenna and Sam, seems like a repurpose of the architecture.md content for some other purpose



https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/pull/697#discussion_r939048339
Could be a good point to add to the architecture doc
Could also discuss using Owner driven separation too. I remember I had created some rules of thumb with Luke on this. User single class for trivial classes, use client/server separation or Owner/Ghost separation when it’s more complex for 1:1 relationships. Use Client/State/Server separation when you have multiple client classes listening to the same event (this one is a bit less defined, open to discussion)

REVIEW https://drive.google.com/drive/folders/1r8oM5Ol4Av4urgfjduMmWrL4IRjkjOKL 

```

# Architecture
This document describes the high-level architecture of Boss Room.
If you want to familiarize yourself with the code base, you are just in the right place!

Boss Room is an 8-player co-op RPG game experience, where players collaborate to take down some minions, and then a boss. Players can select between classes that each have skills with didactically interesting networking characteristics. Control model is click-to-move, with skills triggered by mouse button or hotkey. 

## Assembly structure
In Boss Room code is organized into a multitude of domain-based assemblies. Each assembly serves a relatively self-contained purpose. 
An exception to this guideline is Gameplay assembly, which houses most of our networked gameplay logic, connection management and other tightly coupled functionality.

This assembly separation style forces us to have better separation of concerns and serves as one of the ways to keep the code-base organized. It also provides a benefit of more granular recompilation during our iterations, which shaves us some time we would've spent looking at the progress bar.

```
TODO: 
 - add an assembly diagram image into the assembly definition section
```

## Important architectural patterns
In Boss Room we made several noteworthy architectural decisions:

1) We use [Dependency Injection](https://en.wikipedia.org/wiki/Dependency_injection) pattern, with our library of choice being [VContainer](https://vcontainer.hadashikick.jp/).

DI allows us to clearly define our dependencies in code, as opposed to using the so-called ScriptableObject architecture or, static access or pervasive singletons. Code is easy to version-control and comparatively easy to understand for a programmer, as opposed to Unity YAML-based objects, such as scenes, scriptable object instances and prefabs. 
DI also allows us to circumvent the problem of cross-scene references to common dependencies, even though we still have to manage the lifecycle of MonoBehaviour-based dependencies by marking them with DontDestroyOnLoad and destroying them manually when appropriate.

2) We have implemented the DI-friendly Publisher-Subscriber pattern (see Infrastructure assembly, PubSub folder).

It allows us various modes of message transfer. This mechanism allows us to both avoid circular references and to have a more limited dependency surface between our assemblies - cross-communicating systems rely on common messages, but don't necessarily need to know about each-other, thus allowing us to separate them into purposeful assemblies.
It allows us to avoid having circular references between assemblies, the code of which needs only to know about the messaging protocol, but doesn't actually need to reference anything else. The other benefit is strong separation of concerns and coupling reduction, which is achieved by using PubSub along with Dependency Injection. DI is used to pass the handles to either `IPublisher` or `ISubscriber` of any given event type, and thus our message publishers and consumers are truly not aware of each-other.
`MessageChannel` classes implement these interfaces and provide the actual messaging logic.

Along with in-process messaging, we have implemented the `NetworkMessageChannel`, which uses the same API, but allows us to send data between peers. It is implemented using custom messaging.

## Application entrypoint and application state
The first scene that the application should load to do a normal launch is the `Startup` scene, which contains a game object with an `ApplicationController.cs` component on it.

This component inherits from the `VContainer`'s `LifetimeScope` - a class that serves as a dependency injection scope and bootrstrapper, where we can bind dependencies. `LifetimeScopes` are organized in a tree, starting from the root scope. By defining in the interface a parent scope we make our lives easier when handling scene loads - all of the logic to start and dispose of a scope would largely be on the `VContainer`.

`ApplicationController` is the root scope and the entry point of the application. It binds the dependencies that are used. It's purpose is to bind all the common dependencies that will live for the lifetime of the application, to serve as the final shut-down location and to transition us to the `MainMenu` scene when all the bootstrap logic has concluded.

`MainMenu` scene has it's own `State` component sitting on a root-level game object in that scene. It serves as a scene-specific entrypoint, which, similar to ApplicationController binds dependencies (but these dependencies are local to the specific scene and will be released when the scene unloads).

# Game state / Scene flow
In Boss Room, scenes correspond to top-level Game States (see `GameStateBehaviour` class) in a 1:1 way. That is, there is a `MainMenu` scene, `Character Select` scene (and state), and so on. 

Because it is currently challenging to have a client be in a different scene than the server it's connected to, the options for Netcode developers are either to not use scenes at all, or to use scenes, and let game state transitions on the host drive game state transitions on the client indirectly by forcing client scene transitions through Netcode's networked scene management. 

We chose the latter approach. 

Each scene has exactly one `GameStateBehaviour` (a specialization of `Netcode.NetworkBehaviour`), that is responsible for running the global state logic for that scene. States are transitioned by triggered scene transitions.

!!!! GAME STATE BEHAVIOURS are no longer network behaviours

For MainMenu scene we only have the client state, however for the scenes that contain networked logic we also have the `server` counterparts to the client scenes, and both exist on the same game object.


```
TODO: rewrite this section
```
## Host model
Boss Room uses a Host model for its server. This means one client acts as a server and hosts the other clients. 

A common pitfall of this pattern is writing the game in such a way that it is virtually impossible to adapt to a dedicated server model. 

We attempted to combat this by using a compositional model for our client and server logic (rather than having it all combined in single modules):
 - On the Host, each GameObject has `{Server, Client}` components. 
 - If you start up the game as a dedicated server, the client components will disable themselves, leaving you with `{Server, Shared}` components.
 - If you start up as a client, you get the complementary set of `{Shared, Client}` components.

This approach works, but requires some care: 
 - If you have server and clients of a shared base class, you need to remember that the shared code will run twice on the host. 
 - You also need to take care about code executing in `Start` and `Awake`: if this code runs contemporaneously with the `NetworkManager`'s initialization, it may not know yet whether the player is a host or client.
 - We judged this extra complexity worth it, as it provides a clear road-map to supporting true dedicated servers. 
 - Client-server separation also allows not having god-classes where both client and server code are intermingled. This way, when reading server code, you do not have to mentally skip client code and vice versa. This helps making bigger classes more readable and maintainable. Please note that this pattern can be applied on a case by case basis. If your class never grows too big, having a single `NetworkBehaviour` is perfectly fine.

## Connection flow
```
TODO: reference to connection management document
```
The Boss Room network connection flow is owned by the `GameNetPortal`:
 - The Host will invoke either `GameNetPortal.StartHost` or `StartUnityRelayHost` if Unity Relay is being used.
 - The client will invoke either `ClientGameNetPortal.StartClient` or `StartClientUnityRelayModeAsync`.
 - Boss Room's own connection validation logic is performed in `ServerGameNetPortal.ApprovalCheck`, which is plugged in to the `NetworkManager`'s connection approval callback. Here, some basic information about the connection is recorded (including a GUID, to facilitate future reconnect logic), and success or failure is returned. In the future, additional game-level failures will be detected and returned (such as a `ServerFull` scenario). 

## Data model
```
TODO: update this section to reflect the current state of Action system
```
Game data in Boss Room is defined in `ScriptableObjects`. The `ScriptableObjects` are organized by enum and made available in a singleton class: the `GameDataSource`, in particular `ActionDescription` and `CharacterData`. `Actions` represent discrete verbs (like swinging a weapon, or reviving someone), and are substantially data driven. Characters represent both the different player classes, and also monsters, and represent basic details like health, as well as what "Skill" Actions are available to each Character.

## Transports
Currently two network transport mechanisms are supported: 
- IP based
- Unity Relay Based

In the first, the clients connect directly to a host via IP address. This will only work if both are in the same local area network or if the host forwards ports.

For Unity Relay based multiplayer sessions, some setup is required. Please see our guide [here](Documentation/Unity-Relay/README.md). 

Please see [Multiplayer over internet](README.md) section of our Readme for more information on using either one.

The transport is set in the transport field in the `NetworkManager`. We are using the following transport:
- **Unity Transport Package (UTP):** Unity Transport Package is a network transport layer, packaged with network simulation tools which are useful for spotting networking issues early during development. This protocol is initialized to use direct IP to connect, but is configured at runtime to use Unity Relay if starting a game as a host using the Lobby Service, or joining a Lobby as a client. Unity Relay is a relay service provided by Unity services, supported by Unity Transport. See the documentation on [Unity Transport Package](https://docs-multiplayer.unity3d.com/docs/transport-utp/about-transport-utp/#unity-transport-package-utp) and on [Unity Relay](https://docs-multiplayer.unity3d.com/docs/relay/relay).

To add new transports in the project, parts of `GameNetPortal` and `ClientGameNetPortal` (transport switches) need to be extended.


## Important classes

------------------------
UPDATE THIS BIT:
-----------------------

**Gameplay/Action**
 - `Action` is the abstract base class for all server actions
   - `MeleeAction`, `AoeAction`, etc. contain logic for their respective action types. 


**Gameplay/**
 - `NetworkCharacterState` contains NetworkVariables that store the state of any given character, and both server and client RPC endpoints. The RPC endpoints only read out the call parameters and then raise events from them; they don’t do any logic internally. 

**Server**
 - `ServerCharacterMovement` manages the movement Finite State Machine (FSM) on the server. Updates the NetworkVariables that synchronize position, rotation and movement speed of the entity on its FixedUpdate.
 - `ServerCharacter` has the `AIBrain`, as well as the ActionQueue. Receives action requests (either from the AIBrain in case of NPCs, or user input in case of player characters), and executes them.
 - `AIBrain` contains main AI FSM.  


**Client**
 - `ClientCharacterVisualization` primarily is a host for the running `ActionFX` class.
 - `ClientInputSender `. On a shadow entity, will self-destruct. Listens to inputs, interprets them, and then calls appropriate RPCs on the RPCStateComponent. 
 - `ActionFX` is the abstract base class for all the client-side action visualizers
   - `MeleeActionFX`, `AoeActionFX`, etc. Contain graphics information for their respective action types. 
  
## Movement action flow
 - Client clicks mouse on target destination. 
 - Client->server RPC, containing target destination. 
 - Anticipatory animation plays immediately on client. 
 - Server performs pathfinding.
 - Once pathfinding is finished, server representation of entity starts updating its NetworkVariables at the same cadence as FixedUpdate.
 - Visuals GameObject never outpaces the simulation GameObject, and so is always slightly behind and interpolating towards the networked position and rotation.

## Navigation System
Each scene which uses navigation or dynamic navigation objects should have a `NavigationSystem` component on a scene GameObject. That object also needs to have the `NavigationSystem` tag.

### Building a navigation mesh
The project is using `NavMeshComponents`. This means direct building from the Navigation window will not give the desired results. Instead find a `NavMeshComponent` in the given scene e.g. a **NavMeshSurface** and use the **Bake** button of that script. Also make sure that there is always only one navmesh file per scene. Navmesh files are stored in a folder with the same name as the corresponding scene. You can recognize them based on their icon in the editor. They follow the naming pattern "NavMesh-\<name-of-creating-object\.asset>"

### Dynamic Navigation Objects
A dynamic navigation object is an object which affects the state of the navigation mesh such as a door which can be opened or closed.
To create a dynamic navigation object add a NavMeshObstacle to it and configure the shape (in most cases this should just match the corresponding collider). Then add a DynamicNavObstacle component to it.

## Player Hierarchy

The `Player Prefab` field inside of Boss Room's `NetworkManager` is populated with `PersistentPlayer` prefab. Netcode will spawn a PersistentPlayer per client connection, with the client designated as the owner of the prefab instance. All `Player Prefab` prefab instances will be migrated between scenes internally by Netcode's scene management, therefore it is not necessary to mark this object as a `DontDestroyOnLoad` object. This object is suitable for storing data, in some cases in the form of `NetworkVariable`s, that could be accessed across scenes (eg. name, avatar GUID, etc). PersistentPlayer's GameObject hierarchy is quite trivial as it is comprised of only one GameObject:

* PersistentPlayer: a `NetworkObject` that will not be destroyed between scenes

#### CharSelect Scene
Inside `CharSelect` scene, clients select from 8 possible avatar classes, and that selection is stored inside PersistentPlayer's `NetworkAvatarGuidState`.

#### BossRoom Scene
Inside `BossRoom` scene, `ServerBossRoomState` spawns a `PlayerAvatar` per PersistentPlayer present. This `PlayerAvatar` prefab instance, that is owned by the corresponding connected client, is destroyed by Netcode when a scene load occurs (either to `PostGame` scene, or back to `MainMenu` scene), or through client disconnection.

`ClientAvatarGuidHandler`, a `NetworkBehaviour` component residing on the `PlayerAvatar` prefab instance will fetch the validated avatar GUID from `NetworkAvatarGuidState`, and spawn a local, non-networked graphics GameObject corresponding to the avatar GUID. This GameObject is childed to PlayerAvatar's `PlayerGraphics` child GameObject.

Once initialized successfully, the in-game PlayerAvatar GameObject hierarchy inside `BossRoom` scene will look something like (in the case of a selected Archer Boy class):

* Player Avatar: a `NetworkObject` that *will* be destroyed when `BossRoom` scene is unloaded
  * Player Graphics: a child GameObject containing `NetworkAnimator` component responsible for replicating animations invoked on the server
    * PlayerGraphics_Archer_Boy: a purely graphical representation of the selected avatar class
