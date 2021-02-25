![Banner](Documentation/Images/Banner.png)
# BossRoom - co-op multiplayer RPG built with Unity MLAPI

BossRoom is a fully functional co-op multiplayer RPG made in Unity and MLAPI. It is built to serve as an educational sample that showcases certain typical gameplay patterns that are frequently featured in similar games.

Our intention is that you can use everything in this project as a starting point or as bits and pieces in your own Unity games. See [LICENSE.md](LICENSE.md) for more legal information.


## Prerequisites
```
Unity version: 2020.2.0f1
Platforms    : Windows, Mac
```

## Getting the project

> __IMPORTANT__: 
> This project uses Git Large Files Support (LFS). See the [link](https://git-lfs.github.com/) for Git LFS installation options.

The project uses the `git-flow` branching strategy, as such:
 - `develop` branch contains all active development
 - `master` branch contains release versions

To get the project on your machine you need to clone the repository from GitHub using the following command-line command:
```
git clone git@github.com:Unity-Technologies/com.unity.multiplayer.samples.coop.git
```
Alternatively you can download a release version from the [Releases](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/releases) page. 


## Opening the project for the first time

Once you have downloaded the project the steps below should get you up and running:
 - Make sure you have installed the version of Unity that is listed above in the prerequisites section.
 	- Make sure to include standalone support for Windows/Mac in your installation. 
 - Add the project in _Unity Hub_ by clicking on **Add** button and pointing it to the root folder of the downloaded project.
 	- The first time you open the project Unity will import all assets, which will take longer than usual - it is normal.
 - Once the editor is ready, navigate to the _Project_ window and open the _Project/MainMenu_ scene.
![](Documentation/Images/ProjectWindowMainMenuScene.png)
 - From there you can click the **Play** button. You can host a new game or join an existing game using the in-game UI.


## Testing multiplayer localy

In order to test multiplayer functionality we need to have a built executable. 

To make a build in the menu bar press _File/Build Settings_ and then press **Build**.
![](Documentation/Images/BuildProject.png)

After the build has completed you can launch several instances of the built executable to be able to both host and join a game.


## Testing multiplayer over internet

Running the game over internet currently requires setting up a [Photon Transport for MLAPI](https://github.com/Unity-Technologies/mlapi-community-contributions), which uses Photon relay server to facilitate communication between clients and server living on different networks.  

------------------------------------------

## Exploring the project
BossRoom is an 8-player co-op RPG game experience, where players collaborate to take down some minions, and then a boss. Players can select between classes that each have skills with didactically interesting networking characteristics. Control model is click-to-move, with skills triggered by mouse button or hotkey. 

One of the 8 clients acts as the host/server. That client will use a compositional approach so that its entities have both server and client components.

The game is server-authoritative, with latency-masking animations. Position updates are done through NetworkTransforms. NetworkedVars and RPC endpoints are isolated in a class that is shared between the server and client specialized logic components. All gamelogic runs in FixedUpdate at 30 Hz, matching our network update rate. 

Code is organized into three separate assemblies: **Client**, **Shared** and **Server** which reference each other when appropriate.

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

----------------------------
----------------------------
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

# TODOs (trimmed down version of tasks listed at: https://github.cds.internal.unity3d.com/unity/com.unity.template-starter-kit)

##### Fill in your project template's package information

	Update the following required fields in `Packages/com.unity.template.mytemplate/package.json`:
	- `name`: Project template's package name, it should follow this naming convention: `com.unity.template.[your-template-name]`
    (Example: `com.unity.template.3d`)
	- `displayName`: Package user friendly display name. (Example: `"First person shooter"`). <br>__Note:__ Use a display name that will help users understand what your project template is intended for.
	- `version`: Package version `X.Y.Z`, your project **must** adhere to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).
	- `unity`: Minimum Unity Version your project template is compatible with. (Example: `2018.3`)
	- `description`: This is the description for your template which will be displayed to the user to let them know what this template is for. This description shouldn't include anything version-specific and should stay pretty consistent across template versions.
	- `dependencies`: Specify the dependencies the template requires. If you add a package to your project, you should also add it here. We try to keep this list as lean as possible to avoid conflicts as much as possible.

##### Update **README.md**

    The README.md file should contain all pertinent information for template developers, such as:
	* Prerequisites
	* External tools or development libraries
	* Required installed Software

The Readme file at the root of the project should be the same as the one found in the template package folder. 

##### Prepare your documentation

    Rename and update **Packages/com.unity.template.mytemplate/Documentation~/your-package-name.md** documentation file.

    Use this documentation template to create preliminary, high-level documentation for the _development_ of your template's package. This document is meant to introduce other developers to the features and sample files included in your project template.

    Your template's documentation will be made available online and in the editor during publishing to guide our users.

##### Update the changelog   

    **Packages/com.unity.template.mytemplate/CHANGELOG.md**.

	Every new feature or bug fix should have a trace in this file. For more details on the chosen changelog format, see [Keep a Changelog](http://keepachangelog.com/en/1.0.0/).

	Changelogs will be made available online to inform users about the changes they can expect when downloading a project template. As a consequence, the changelog content should be customer friendly and present clear, meaningful information.

#### Complete the rest of the steps in the link regarding Legal & Testing
