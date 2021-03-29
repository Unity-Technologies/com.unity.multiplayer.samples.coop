# Architecture

This document describes the high-level architecture of BossRoom.
If you want to familiarize yourself with the code base, you are just in the right place!

//
 - Connection flow
The BossRoom network connection flow is owned by the GameNetPortal. The Host will invoke either GameNetPortal.StartHost, or StartRelayHost (if Photon relay is being used). The client will invoke either ClientGameNetPortal.StartClient, or StartClientRelayMode. Bossroom's own connection validation logic is performed in ServerGameNetPortal.ApprovalCheck, which is plugged in to the NetworkingManager's connection approval callback. Here some basic information about the connection is recorded (including a GUID, to facilitate future reconnect logic), and success or failure is returned. In the future, additional game-level failures will be detected and returned (such as a ServerFull scenario). 
 - host model
BossRoom uses a Host model for its server. This means one client acts as a server and hosts the other clients. A common pitfall of this pattern is writing the game in such a way that it is virtually impossible to adapt to a dedicated server model. We attempted to combat this by using a compositional model for our client and server logic (rather than having it all combined is single modules). On the Host, each GameObject has {Server, Shared, Client} components. If you start up the game as a dedicated server, the client components will disable themselves, leaving you with {Server, Shared} components. If you start up as a client, you get the complementary set of {Shared, Client} components. This approach works, but requires some care: if you have server and clients of a shared base class, you need to remember that the shared code will run TWICE on the host; you also need to take care about code executing in Start+Awake; if this code runs contemporaneously with the NetworkingManager's initialization, it may not know yet whether the player is a host or client. We judged this extra complexity worth it, as it provides a clear road-map to supporting true dedicated servers. 
 - data model: gamedatasource, characters and actions (scriptable objects)
Game data in BossRoom is defined in ScriptableObjects. The ScriptableObjects are organized by enum and made available in a singleton class: the GameDataSource, in particular ActionDescription and CharacterData. Actions represent discrete verbs (like swinging a weapon, or reviving someone), and are substantially data driven. Characters represent both the different player classes, and also monsters, and represent basic details like health, as well as what "Skill" Actions are available to each Character. 
 - touch on transports used (ip and relay?)
Currently two network transport mechanisms are supported: IP/port connection, and Relay connection. In the former, Client users must know the IP and port they are connecting to, and establish connection directly. In the latter, the develop must have signed up with Photon and defined a Photon Realtime app. Then Hosts create Rooms in Photon (essentially a string name associated with the photon app), and share that information with clients. The advantage of the Relay is that Host users are often on local networks and may need to perform actions like port forwarding (which they may or may not have permissions to do) in order to accept incoming connections. The disadvantage is that it effectively doubles latency to the clients (or potentially worse than that, if host and client are close, but relay is in a different region). The complexity of the Relay is mostly abstracted away from MLAPI. Assuming the right configuration settings in Assets/Photon/Resources/PhotonAppSettings are set, the Photon Relay appears as simply another Transport from the point of view of MLAPI. 
 - GameNetPortal 
[I think this was already touched on adequately in Connection Flow ]
   - GameStateBehaviour (scene stuff)
In BossRoom, scenes correspond to top-level Game States in a 1:1 way. That is, there is a MainMenu scene (and state), Character Select scene (and state), and so on. Because it is currently challenging to have a client be in a different scene than the server it's connected to, the options for MLAPI developers are either to not use scenes at all, or to use scenes, and let game state transitions on the host drive game state transitions on the client indirectly by forcing client scene transitions through MLAPI's networked scene management. We chose the latter approach. Each scene has exactly one GameStateBehaviour (a specialization of MLAPI.NetworkBehaviour), that is responsible for running the global state logic for that scene. States are transitioned by triggered scene transitions. The GameStateBehaviour was written in such a way that it could be adapted to many-to-one usage, with multiple "game" scenes using the same state object, but the BossRoom demo doesn't use this concept. 
//

## Bird's Eye View

## Exploring the project
BossRoom is an 8-player co-op RPG game experience, where players collaborate to take down some minions, and then a boss. Players can select between classes that each have skills with didactically interesting networking characteristics. Control model is click-to-move, with skills triggered by mouse button or hotkey. 

One of the 8 clients acts as the host/server. That client will use a compositional approach so that its entities have both server and client components.

The game is server-authoritative, with latency-masking animations. Position updates are done through NetworkTransforms. NetworkedVars and RPC endpoints are isolated in a class that is shared between the server and client specialized logic components. All gamelogic runs in FixedUpdate at 30 Hz, matching our network update rate. 

Code is organized into three separate assemblies: **Client**, **Shared** and **Server** which reference each other when appropriate.

For an in-depth overview of the project's architecture please check out our [ARCHITECTURE.md](ARCHITECTURE.md).

### Key classes


### Key classes

**Shared**
 - `NetworkCharacterState` Contains all NetworkedVars, and both server and client RPC endpoints. The RPC endpoints only read out the call parameters and then raise events from them; they don’t do any logic internally. 

**Server**
 - `ServerCharacterMovement` manages the movement Finite State Machine (FSM) on the server. Updates the NetworkedVars that synchronize position, rotation and movement speed of the entity on its FixedUpdate.
 - `ServerCharacter` has the AIBrain, as well as the ActionQueue. Receives action requests (either from the AIBrain in case of NPCs, or user input in case of player characters), and executes them.
 - `AIBrain` contains main AI FSM.  
 - `Action` is the abstract base class for all server actions
   - `MeleeAction`, `AoeAction`, etc. contain logic for their respective action types. 

**Client**
 - `ClientCharacterVisualization` primarily is a host for the running `ActionFX` class.
 - `ClientInputSender `. On a shadow entity, will self-destruct. Listens to inputs, interprets them, and then calls appropriate RPCs on the RPCStateComponent. 
 - `ActionFX` is the abstract base class for all the client-side action visualizers
   - `MeleeActionFX`, `AoeActionFX`, etc. Contain graphics information for their respective action types. 

   

### Movement action flow
 - Client clicks mouse on target destination. 
 - Client->server RPC, containing target destination. 
 - Anticipatory animation plays immediately on client. 
 - Server path-plans. 
 - Once path-plan is finished, server representation of entity starts updating its NetworkedTransform at 30fps. Graphics is on a separate GO and is connected to the networked GO via a spring, to smooth out small corrections.
 - Graphics GO never passes the simulation GO; if it catches up to the sim due to a network delay, the user will see a hitch. 

### Navigation System

#### Building a navigation mesh
The project is using NavMeshComponents. This means direct building from the Navigation window will not give the desired results. Instead find a NavMeshComponent in the given scene e.g. a **NavMeshSurface** and use the **Bake** button of that script. Also make sure that there is always only one navmesh file per scene. Navmesh files are stored in a folder with the same name as the corresponding scene. You can recognize them based on their icon in the editor. They follow the naming pattern "NavMesh-\<name-of-creating-object\.asset>"

#### Dynamic Navigation Objects
A dynamic navigation object is an object which affects the state of the navigation mesh such as a door which can be openend or closed.
To create a dynamic navigation object add a NavMeshObstacle to it and configure the shape (in most cases this should just match the corresponding collider). Then add a DynamicNavObstacle component to it.

#### Navigation System
Each scene which uses navigation or dynamic navigation objects should have a NavigationSystem component on a scene gameobject. That object also needs to have the "NavigationSystem" tag.

---------------------------