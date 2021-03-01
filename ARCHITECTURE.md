# Architecture

This document describes the high-level architecture of BossRoom.
If you want to familiarize yourself with the code base, you are just in the right place!

> __IMPORTANT__: 
> This doc is heavily WIP

## Bird's Eye View

## Exploring the project
BossRoom is an 8-player co-op RPG game experience, where players collaborate to take down some minions, and then a boss. Players can select between classes that each have skills with didactically interesting networking characteristics. Control model is click-to-move, with skills triggered by mouse button or hotkey. 

One of the 8 clients acts as the host/server. That client will use a compositional approach so that its entities have both server and client components.

The game is server-authoritative, with latency-masking animations. Position updates are done through NetworkTransforms. NetworkedVars and RPC endpoints are isolated in a class that is shared between the server and client specialized logic components. All gamelogic runs in FixedUpdate at 30 Hz, matching our network update rate. 

Code is organized into three separate assemblies: **Client**, **Shared** and **Server** which reference each other when appropriate.

For an in-depth overview of the project's architecture please check out our [ARCHITECTURE.md](ARCHITECTURE.md).

### Key classes

**Shared**
 - `NetworkCharacterState` Contains all NetworkedVars, and both server and client RPC endpoints. The RPC endpoints only read out the call parameters and then raise events from them; they don’t do any logic internally. 

**Server**
 - `ServerCharacterMovement` manages the movement FSM on the server (example states: IDLE, PATHPLANNING, PATHFOLLOWING, FREEMOVING). Updates the NetworkedTransform of the entity on its FixedUpdate
 - `ServerCharacter` has the AIBrain, as well as the ActionQueue. Receives action requests (either from the AIBrain in case of NPCs, or user input in case of player characters), and executes them.
 - `AIBrain` contains main AI FSM.  
 - `Action` is the abstract base class for all server actions
   - `MeleeAction`, `AoeAction`, etc. contain logic for their respective action types. 

**Client**
 - `ClientCharacterComponent` primarily is a host for the running `ActionFX` class. This component will probably be on the graphics GO, rather than the sim GO. 
 - `CliemtInputComponent`. On a shadow entity, will suicide. Listens to inputs, interprets them, and then calls appropriate RPCs on the RPCStateComponent. 
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