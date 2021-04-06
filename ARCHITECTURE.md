# Architecture
This document describes the high-level architecture of Boss Room.
If you want to familiarize yourself with the code base, you are just in the right place!

Boss Room is an 8-player co-op RPG game experience, where players collaborate to take down some minions, and then a boss. Players can select between classes that each have skills with didactically interesting networking characteristics. Control model is click-to-move, with skills triggered by mouse button or hotkey. 

Code is organized into three separate assemblies: `Client`, `Server` and `Shared` (which, as it's name implies, contains shared functionality that both client and the server require).

## Host model
Boss Room uses a Host model for its server. This means one client acts as a server and hosts the other clients. 

A common pitfall of this pattern is writing the game in such a way that it is virtually impossible to adapt to a dedicated server model. 

We attempted to combat this by using a compositional model for our client and server logic (rather than having it all combined is single modules):
 - On the Host, each GameObject has `{Server, Shared, Client}` components. 
 - If you start up the game as a dedicated server, the client components will disable themselves, leaving you with `{Server, Shared}` components.
 - If you start up as a client, you get the complementary set of `{Shared, Client}` components. 

This approach works, but requires some care: 
 - if you have server and clients of a shared base class, you need to remember that the shared code will run twice on the host; 
 - you also need to take care about code executing in `Start` and `Awake`: if this code runs contemporaneously with the `NetworkingManager`'s initialization, it may not know yet whether the player is a host or client.
 - We judged this extra complexity worth it, as it provides a clear road-map to supporting true dedicated servers. 
 - Client server separation also allows not having god-classes where both client and server code are intermingled. This way, when reading server code, you do not have to mentally skip client code and vice versa. This helps making bigger classes more readable and maintainable. Please note that this pattern can be applied on a case by case basis. If your class never grows too big, having a single `NetworkBehaviour` is perfectly fine.

## Connection flow
The Boss Room network connection flow is owned by the `GameNetPortal`:
 - The Host will invoke either `GameNetPortal.StartHost`, or `StartRelayHost` (if Photon relay is being used). 
 - The client will invoke either `ClientGameNetPortal.StartClient`, or `StartClientRelayMode`. 
 - Boss Room's own connection validation logic is performed in `ServerGameNetPortal.ApprovalCheck`, which is plugged in to the `NetworkingManager`'s connection approval callback. Here some basic information about the connection is recorded (including a GUID, to facilitate future reconnect logic), and success or failure is returned. In the future, additional game-level failures will be detected and returned (such as a `ServerFull` scenario). 

## Data model
Game data in Boss Room is defined in `ScriptableObjects`. The `ScriptableObjects` are organized by enum and made available in a singleton class: the `GameDataSource`, in particular `ActionDescription` and `CharacterData`. `Actions` represent discrete verbs (like swinging a weapon, or reviving someone), and are substantially data driven. Characters represent both the different player classes, and also monsters, and represent basic details like health, as well as what "Skill" Actions are available to each Character.

## Transports
Currently two network transport mechanisms are supported: 
- IP based
- Relay Based

In the former, the clients connect directy to a host via IP address. This will only work if both are in the same local area network or if the host forwards ports.

In the latter, some setup is required. Please see our guide [here](Documentation/Photon-Realtime/Readme.md) on how to setup our current relay.  

Please see [Multiplayer over internet](README.md) section of our Readme for more information on using either one.

To allow for both of these options to be chosen at runtime we created `TransportPicker`. It allows to chose between an IP-based and a Relay-based transport and will hook up the game UI to use those transports. The transport field in the `NetworkManager` will be ignored. Currently we support the following transports:
- **UNet(IP):** UNet is the default MLAPI transport and the default IP transport for Boss Room.
- **LiteNetLib(IP):** We use LiteNetLib in Boss Room because it has a built in way to simulate latency which is useful for spotting networking issues early during development.
- **Photon Realtime (Relay):** Photon Realtime is a relay transport using the [Photon Realtime Service](https://www.photonengine.com/Realtime).

To add new transport in the project parts of `GameNetPortal` and `ClientGameNetPortal` (transport switches) need to be extended.

## Game state / Scene flow
In Boss Room, scenes correspond to top-level Game States (see `GameStateBehaviour` class) in a 1:1 way. That is, there is a `MainMenu` scene, `Character Select` scene (and state), and so on. 

Because it is currently challenging to have a client be in a different scene than the server it's connected to, the options for MLAPI developers are either to not use scenes at all, or to use scenes, and let game state transitions on the host drive game state transitions on the client indirectly by forcing client scene transitions through MLAPI's networked scene management. 

We chose the latter approach. 

Each scene has exactly one `GameStateBehaviour` (a specialization of `MLAPI.NetworkBehaviour`), that is responsible for running the global state logic for that scene. States are transitioned by triggered scene transitions.

## Important classes

**Shared**
 - `NetworkCharacterState` Contains NetworkedVars that store the state of any given character, and both server and client RPC endpoints. The RPC endpoints only read out the call parameters and then raise events from them; they don’t do any logic internally. 

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
 - Once pathfinding is finished, server representation of entity starts updating it's NetworkVariables at 30fps.
 - Visuals GameObject never outpaces the simulation GameObject, always slightly behind and interpolating towards the networked position and rotation.

## Navigation System
Each scene which uses navigation or dynamic navigation objects should have a `NavigationSystem` component on a scene GameObject. That object also needs to have the `NavigationSystem` tag.

### Building a navigation mesh
The project is using `NavMeshComponents`. This means direct building from the Navigation window will not give the desired results. Instead find a `NavMeshComponent` in the given scene e.g. a **NavMeshSurface** and use the **Bake** button of that script. Also make sure that there is always only one navmesh file per scene. Navmesh files are stored in a folder with the same name as the corresponding scene. You can recognize them based on their icon in the editor. They follow the naming pattern "NavMesh-\<name-of-creating-object\.asset>"

### Dynamic Navigation Objects
A dynamic navigation object is an object which affects the state of the navigation mesh such as a door which can be openend or closed.
To create a dynamic navigation object add a NavMeshObstacle to it and configure the shape (in most cases this should just match the corresponding collider). Then add a DynamicNavObstacle component to it.
