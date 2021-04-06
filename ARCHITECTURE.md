# Architecture
This document describes the high-level architecture of Boss Room.
If you want to familiarize yourself with the code base, you are just in the right place!

Boss Room is an 8-player co-op RPG game experience, where players collaborate to take down some minions, and then a boss. Players can select between classes that each have skills with didactically interesting networking characteristics. The control model is click-to-move, with skills triggered by mouse button or hotkey. 

Code is organized into three separate assemblies: `Client`, `Server` and `Shared` (which, as it's name implies, contains shared functionality that both client and the server require).

## Host model
Boss Room uses a Host model for its server. This means one client acts as a server and hosts the other clients. 

A common pitfall of this pattern is writing the game in such a way that it is virtually impossible to adapt to a dedicated server model. 

We attempted to resolve this by using a compositional model for our client and server logic, rather than having it all combined is single modules:
 - On the Host, each GameObject has `{Server, Shared, Client}` components. 
 - If you start up the game as a dedicated server, the client components will disable themselves, leaving you with `{Server, Shared}` components.
 - If you start up as a client, you get the complementary set of `{Shared, Client}` components. 

This approach works, but requires some care: 
 - If you have server and clients of a shared base class, the shared code will run twice on the host.
 - Be careful with code executing in `Start` and `Awake`: If this code runs contemporaneously with the `NetworkingManager`'s initialization, it may not know yet if the player is a host or client.
 - We judged this extra complexity worth it, as it provides a clear road-map to supporting true dedicated servers. 

## Connection flow
The Boss Room network connection flow is owned by the `GameNetPortal`:
 - The Host will invoke either `GameNetPortal.StartHost` or `StartRelayHost` (if Photon relay is being used). 
 - The client will invoke either `ClientGameNetPortal.StartClient` or `StartClientRelayMode`. 
 - Boss Room's own connection validation logic is performed in `ServerGameNetPortal.ApprovalCheck`, which is plugged in to the `NetworkingManager`'s connection approval callback. Here some basic information about the connection is recorded (including a GUID, to facilitate future reconnect logic), and success or failure is returned. In the future, additional game-level failures will be detected and returned (such as a `ServerFull` scenario). 

## Data model
Game data in Boss Room is defined in `ScriptableObjects`. The `ScriptableObjects` are organized by enum and made available in a singleton class: the `GameDataSource`, in particular `ActionDescription` and `CharacterData`. `Actions` represent discrete verbs (like swinging a weapon, or reviving someone), and are substantially data driven. Characters represent both the different player classes, and also monsters, and represent basic details like health, as well as what "Skill" Actions are available to each Character.

## Transports
Currently two network transport mechanisms are supported: 
 - IP/port connection
 - Relay connection

In the former, Client users must know the IP and port they are connecting to, and establish connection directly.

In the latter, the developer must have signed up with Photon and defined a Photon Realtime app id. Then Hosts create Rooms in Photon (essentially a string name associated with the photon app), and share that information with clients. The advantage of the Relay is that Host users are often on local networks and may need to perform actions like port forwarding (which they may or may not have permissions to do) in order to accept incoming connections. The disadvantage is that it effectively doubles latency to the clients (or potentially worse than that, if host and client are close, but relay is in a different region). The complexity of the Relay is mostly abstracted away from MLAPI. Assuming the right configuration settings in Assets/Photon/Resources/PhotonAppSettings are set, the Photon Relay appears as simply another Transport from the point of view of MLAPI. 

Please see [Multiplayer over internet](README.md) section of our Readme for more information on using either one.

## Game state / Scene flow
In Boss Room, scenes correspond to top-level Game States (see `GameStateBehaviour` class) in a 1:1 way. That is, there is a `MainMenu` scene, `Character Select` scene (and state), and so on. 

Because it is currently challenging to have a client be in a different scene than the server it's connected to, the options for MLAPI developers are either to not use scenes at all, or to use scenes, and let game state transitions on the host drive game state transitions on the client indirectly by forcing client scene transitions through MLAPI's networked scene management. 

We chose the latter approach. 

Each scene has exactly one `GameStateBehaviour` (a specialization of `MLAPI.NetworkBehaviour`), that is responsible for running the global state logic for that scene. States are transitioned by triggered scene transitions.

## Important classes

**Shared**
 - `NetworkCharacterState` contains NetworkVariables that store the state of any given character, and both server and client RPC endpoints. The RPC endpoints only read out the call parameters and then raise events from them; they do not have any additional internal logic.

**Server**
 - `ServerCharacterMovement` manages the movement Finite State Machine (FSM) on the server. Updates the NetworkedVars that synchronize position, rotation and movement speed of the entity on its FixedUpdate.
 - `ServerCharacter` has the `AIBrain`, as well as the ActionQueue. Receives action requests (either from the AIBrain in case of NPCs, or user input in case of player characters), and executes them.
 - `AIBrain` contains main AI FSM.  
 - `Action` is the abstract base class for all server actions
   - `MeleeAction`, `AoeAction`, etc. contain logic for their respective action types. 

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
 - Once pathfinding is finished, server representation of entity starts updating its NetworkVariables at 30fps.
 - Visuals GameObject never outpaces the simulation GameObject, always slightly behind and interpolating towards the networked position and rotation.
