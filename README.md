![Banner](Documentation/Images/Banner.png)
<br><br>

# Boss Room: a Co-op, Multiplayer RPG Sample
###  Made with and Including Utilities for Netcode for GameObjects
<br>

[![UnityVersion](https://img.shields.io/badge/Unity%20Version:-2022.3%20LTS-57b9d3.svg?logo=unity&color=2196F3)](https://unity.com/releases/editor/whats-new/2022.3.0)
[![NetcodeVersion](https://img.shields.io/badge/Netcode%20Version:-1.8.1-57b9d3.svg?logo=unity&color=2196F3)](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/releases/tag/ngo%2F1.8.1)
[![LatestRelease](https://img.shields.io/badge/Latest%20Github%20Release:-v2.5.0-57b9d3.svg?logo=github&color=brightgreen)](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/releases/tag/v2.5.0)
<br><br>

Boss Room is a fully functional co-op multiplayer RPG made with Unity Netcode. It is an educational sample designed to showcase typical netcode [patterns](https://docs-multiplayer.unity3d.com/netcode/current/learn/bossroom/bossroom-actions/index.html) that are frequently featured in similar multiplayer games.
<br><br>

# Boss Room Sample Overview

Boss Room is designed to be used in its entirety to help you explore the concepts and patterns behind a multiplayer game flow; such as character abilities, casting animations to hide latency, replicated objects, RPCs, and integration with the [Relay](https://unity.com/products/relay), [Lobby](https://unity.com/products/lobby), and [Authentication](https://unity.com/products/authentication) services.

You can use the project as a reference starting point for your own Unity game or use elements individually.
<br><br>

---

### 💡 Utilities Package
This repository also contains a [Utilities](Packages/com.unity.multiplayer.samples.coop) package, containing reusable sample scripts. You can install it using the following manifest file entry:
<br>
`"com.unity.multiplayer.samples.coop": "https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop.git?path=/Packages/com.unity.multiplayer.samples.coop",`

---


<br>

For more information on the art of Boss Room, see [ART_NOTES.md](Documentation/ART_NOTES.md).


![](Documentation/Images/Boss.png)
<br><br>
  
-----

## Readme Contents and Quick Links
<!-- TOC generated from https://luciopaiva.com/markdown-toc/ -->
<details open>
<summary> <b>Click to expand/collapse contents</b> </summary>

- ### [Getting the project](#getting-the-project-1)
  - [Direct download](#direct-download)
  - [Installing Git LFS to clone locally](#installing-git-lfs-to-clone-locally)
- ### [Requirements](#requirements-1)
  - [Min Spec Devices](#boss-rooms-min-spec-devices-are)
- ### [Opening the project for the first time](#opening-the-project-for-the-first-time-1) 
- ### [Exploring the project](#exploring-the-project-1)
  - [Registering with Unity Gaming Services (UGS)](#registering-the-project-with-unity-gaming-services-ugs)
- ### [Testing multiplayer](#testing-multiplayer-1) 
  - [Local Multiplayer Setup](#local-multiplayer-setup)
  - [Multiplayer over Internet](#multiplayer-over-internet)
  - [Relay Setup](#relay-setup) 
- ### [Index of resources in this project](#index-of-resources-in-this-project-1)
  - [Gameplay](#gameplay)
  - [Game Flow](#game-flow)
  - [Connectivity](#connectivity)
  - [Services (Lobby, Relay, etc)](#services-lobby-relay-etc)
  - [Tools and Utilities](#tools-and-utilities)
- ### [Troubleshooting](#troubleshooting-1)
  - [Bugs](#bugs)
  - [Documentation](#documentation)
- ### [License](#license-1)
- ### [Contributing](#contributing-1)
- ### [Community](#community-1)
- ### [Feedback Form](#feedback-form-1)
- ### [Other samples](#other-samples-1)
  - [Bite-size Samples](#bite-size-samples)
</details>

------
<br>

## Getting the project
### Direct download
 - You can download the latest version of Boss Room from our [Releases](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/releases) page. 
 - __Alternatively:__ click the green `Code` button and then select the 'Download Zip' option.  Please note that this will download the branch that you are currently viewing on Github.  
 - __Windows users:__ Using Windows' built-in extraction tool may generate an "Error 0x80010135: Path too long" error window which can invalidate the extraction process. A workaround for this is to shorten the zip file to a single character (eg. "c.zip") and move it to the shortest path on your computer (most often right at C:\\) and retry. If that solution fails, another workaround is to extract the downloaded zip file using [7zip](https://www.7-zip.org/).
<br><br>

## Requirements

BossRoom is compatible with the latest Unity Long Term Support (LTS) editor version, currently [2022 LTS](https://unity.com/releases/editor/qa/lts-releases?version=2022.3). Please include standalone support for Windows/Mac in your installation.

**PLEASE NOTE:** You will also need Netcode for Game Objects to use these samples. See the [Installation Documentation](https://docs-multiplayer.unity3d.com/netcode/current/installation) to prepare your environment. You can also complete the [Get Started With NGO](https://docs-multiplayer.unity3d.com/netcode/current/tutorials/get-started-ngo) tutorial to familiarize yourself with Netcode For Game Objects.
<br><br>

#### Boss Room has been developed and tested on the following platforms:
- Windows
- Mac
- iOS
- Android <br>

#### Boss Room's min spec devices are:
- iPhone 6S
- Samsung Galaxy J2 Core 
<br><br>

### Installing Git LFS to clone locally

Boss Room uses Git Large Files Support (LFS) to handle all large assets required locally. See [Git LFS installation options](https://github.com/git-lfs/git-lfs/wiki/Installation) for Windows and Mac instructions. This step is only needed if cloning locally. You can also just download the project which will already include large files.
<br><br>



## Opening the project for the first time

Once you have downloaded the project, follow the steps below to get up and running:
 - Check that you have installed the most recent [LTS editor version](https://unity.com/releases/2021-lts).
 	- Include standalone support for Windows/Mac in your installation. 
 - Add the project to the _Unity Hub_ by clicking on the **Add** button and pointing it to the root folder of the downloaded project.
 	- __Please note :__ the first time you open the project Unity will import all assets, which will take longer than usual.

 - Hit the **Play** button. You can then host a new game or join an existing one using the in-game UI.

  ![](Documentation/Images/StartupScene.png)
<br><br><br>

## Exploring the project
BossRoom is an eight-player co-op RPG game experience, where players collaborate to fight imps, and then a boss. Players can select between classes that each have skills with didactically interesting networking characteristics. Control model is click-to-move, with skills triggered by a mouse button or hotkey.

One of the eight clients acts as the host/server. That client will use a compositional approach so that its entities have both server and client components.

- The game is server-authoritative, with latency-masking animations. 
- Position updates are carried out through NetworkTransform that sync position and rotation. 

Code is organized in domain-based assemblies. See the [Boss Room architecture documentation](https://docs-multiplayer.unity3d.com/netcode/current/learn/bossroom/bossroom-architecture) file for more details.
<br><br>

### Registering the project with Unity Gaming Services (UGS)

Boss Room leverages several services from UGS to facilitate connectivity between players. To use these services inside your project, you must [create an organization](https://support.unity.com/hc/en-us/articles/208592876-How-do-I-create-a-new-Organization-) inside the Unity Dashboard, and enable the [Relay](https://docs.unity.com/relay/get-started.html) and [Lobby](https://docs.unity.com/lobby/game-lobby-sample.html) services. Otherwise, you can still use Boss Room without UGS.
<br><br><br>
 
## Testing multiplayer

In order to see the multiplayer functionality in action we can either run multiple instances of the game locally on your computer - using either ParrelSync or builds - or choose to connect to a friend over the internet. See [how to test](https://docs-multiplayer.unity3d.com/netcode/current/tutorials/testing/testing_locally) for more info.
<br><br>

### Local multiplayer setup

First, build an executable by clicking **'File/Build Settings'** in the menu bar, and then click **'Build'**.<br>  
![](Documentation/Images/BuildProject.png)

Once the build has completed you can launch several instances of the built executable in order to both host and join a game. When using several instances locally, you will have to set different profiles for each instance for authentication purposes, by using the **'Change Profile'** button. <br>  

---

💡  **Mac users:** To run multiple instances of the same app, you need to use the command line. Run `open -n BossRoom.app`

---

<br>

### Multiplayer over Internet

To play over internet, first build an executable that is shared between all players - as above.

It is possible to connect between multiple instances of the same executable OR between executables and the editor that produced it.

Running the game over internet currently requires setting up a relay. 
  
  
### Relay Setup
 
- Boss Room provides an integration with [Unity Relay](https://docs-multiplayer.unity3d.com/netcode/current/relay/). You can find our Unity Relay setup guide [here](https://docs.unity.com/ugs/en-us/manual/relay/manual/get-started)

- Alternatively you can use Port Forwarding. The https://portforward.com/ site has guides on how to enable port forwarding on a huge number of routers.
- Boss Room uses `UDP` and needs a `9998` external port to be open. 
- Make sure your host's address listens on 0.0.0.0 (127.0.0.1 is for local development only).
<br><br><br>

-----

## Index of resources in this project

<details open>
<summary> <b>Click to expand/collapse contents</b> </summary>

### Gameplay
* Action anticipation - AnticipateActionClient() in [Assets/Scripts/Gameplay/Action/Action.cs](Assets/Scripts/Gameplay/Action/Action.cs)
* Object spawning for  long actions (archer arrow) - LaunchProjectile() in [Assets/Scripts/Gameplay/Action/ConcreteActions/LaunchProjectileAction.cs ](Assets/Scripts/Gameplay/Action/ConcreteActions/LaunchProjectileAction.cs)
* Quick actions with  RPCs (ex: mage bolt) - [Assets/Scripts/Gameplay/Action/ConcreteActions/FXProjectileTargetedAction.cs ](Assets/Scripts/Gameplay/Action/ConcreteActions/FXProjectileTargetedAction.cs)
* Teleport - [Assets/Scripts/Gameplay/Action/ConcreteActions/DashAttackAction.cs ](Assets/Scripts/Gameplay/Action/ConcreteActions/DashAttackAction.cs)
* Client side input tracking  before an action (archer AOE) - OnStartClient() in [Assets/Scripts/Gameplay/Action/ConcreteActions/AOEAction.cs ](Assets/Scripts/Gameplay/Action/ConcreteActions/AOEAction.cs)
* Time based action (charged shot) - [Assets/Scripts/Gameplay/Action/ConcreteActions/ChargedLaunchProjectileAction.cs ](Assets/Scripts/Gameplay/Action/ConcreteActions/ChargedLaunchProjectileAction.cs)
* Object parenting to animation - [Assets/Scripts/Gameplay/Action/ConcreteActions/PickUpAction.cs ](Assets/Scripts/Gameplay/Action/ConcreteActions/PickUpAction.cs)
* Physics object throwing (using NetworkRigidbody) - [Assets/Scripts/Gameplay/Action/ConcreteActions/TossAction.cs ](Assets/Scripts/Gameplay/Action/ConcreteActions/TossAction.cs)
* NetworkAnimator usage - All actions, in particular [Assets/Scripts/Gameplay/Action/ConcreteActions/ChargedShieldAction.cs](Assets/Scripts/Gameplay/Action/ConcreteActions/ChargedShieldAction.cs)
* NetworkTransform local space - [Assets/Scripts/Gameplay/GameplayObjects/ServerDisplacerOnParentChange.cs](Assets/Scripts/Gameplay/GameplayObjects/ServerDisplacerOnParentChange.cs)
* Dynamic imp spawning with portals - [Assets/Scripts/Gameplay/GameplayObjects/ServerWaveSpawner.cs ](Assets/Scripts/Gameplay/GameplayObjects/ServerWaveSpawner.cs)
* In scene placed dynamic objects (imps) - [Packages/com.unity.multiplayer.samples.coop/Utilities/Net/NetworkObjectSpawner.cs ](Packages/com.unity.multiplayer.samples.coop/Utilities/Net/NetworkObjectSpawner.cs)
* Static objects (non-destroyables like doors, switches, etc) - [Assets/Scripts/Gameplay/GameplayObjects/SwitchedDoor.cs](Assets/Scripts/Gameplay/GameplayObjects/SwitchedDoor.cs)
* State tracking with breakables, switch, doors
  * [Assets/Scripts/Gameplay/GameplayObjects/Breakable.cs ](Assets/Scripts/Gameplay/GameplayObjects/Breakable.cs)
  * [Assets/Scripts/Gameplay/GameplayObjects/FloorSwitch.cs](Assets/Scripts/Gameplay/GameplayObjects/FloorSwitch.cs)
  * [Assets/Scripts/Gameplay/GameplayObjects/SwitchedDoor.cs](Assets/Scripts/Gameplay/GameplayObjects/SwitchedDoor.cs)
* NetworkVariable with Enum - [Assets/Scripts/Gameplay/GameState/NetworkPostGame.cs](Assets/Scripts/Gameplay/GameState/NetworkPostGame.cs)
* NetworkVariable with custom serialization (GUID) - [Assets/Scripts/Infrastructure/NetworkGuid.cs](Assets/Scripts/Infrastructure/NetworkGuid.cs)
* NetworkVariable with fixed string - [Assets/Scripts/Utils/NetworkNameState.cs](Assets/Scripts/Utils/NetworkNameState.cs)
* NetworkList with custom serialization (LobbyPlayerState) - [Assets/Scripts/Gameplay/GameState/NetworkCharSelection.cs](Assets/Scripts/Gameplay/GameState/NetworkCharSelection.cs)
* Persistent player (over multiple scenes) - [Assets/Scripts/Gameplay/GameplayObjects/PersistentPlayer.cs ](Assets/Scripts/Gameplay/GameplayObjects/PersistentPlayer.cs)
* Character logic (including player's avatar) - [Assets/Scripts/Gameplay/GameplayObjects/Character/ ](Assets/Scripts/Gameplay/GameplayObjects/Character/) <br> [ Assets/Scripts/Gameplay/GameplayObjects/Character/ServerCharacter.cs ](Assets/Scripts/Gameplay/GameplayObjects/Character/ServerCharacter.cs)
* Character movements - [Assets/Scripts/Gameplay/GameplayObjects/Character/ServerCharacterMovement.cs](Assets/Scripts/Gameplay/GameplayObjects/Character/ServerCharacterMovement.cs)
* Client driven movements - Boss Room is server driven with anticipation animation. See [Client Driven bitesize](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/tree/main/Basic/ClientDriven) for client driven gameplay
* Player spawn - SpawnPlayer() in [Assets/Scripts/Gameplay/GameState/ServerBossRoomState.cs](Assets/Scripts/Gameplay/GameState/ServerBossRoomState.cs)
* Player camera setup (with cinemachine) - OnNetworkSpawn() in [Assets/Scripts/Gameplay/GameplayObjects/Character/ClientCharacter.cs](Assets/Scripts/Gameplay/GameplayObjects/Character/ClientCharacter.cs)
* INetworkSerializable (bandwidth optimization) vs INetworkSerializeByMemcpy (performance optimization) usage. See LobbyPlayerState vs ActionID structs [Assets/Scripts/Gameplay/GameState/NetworkCharSelection.cs](Assets/Scripts/Gameplay/GameState/NetworkCharSelection.cs) vs [Assets/Scripts/Gameplay/Action/ActionID.cs](Assets/Scripts/Gameplay/Action/ActionID.cs)

### Game Flow
* Application Controller - [Assets/Scripts/ApplicationLifecycle/ApplicationController.cs ](Assets/Scripts/ApplicationLifecycle/ApplicationController.cs)
* Game flow state machine - All child classes in [Assets/Scripts/Gameplay/GameState/GameStateBehaviour.cs ](Assets/Scripts/Gameplay/GameState/GameStateBehaviour.cs)
* Scene loading and progress sharing - [ackages/com.unity.multiplayer.samples.coop/Utilities/SceneManagement/](Packages/com.unity.multiplayer.samples.coop/Utilities/SceneManagement/)
* Synced UI with character select - [Assets/Scripts/Gameplay/GameState/ClientCharSelectState.cs ](Assets/Scripts/Gameplay/GameState/ClientCharSelectState.cs)
* In-game lobby (character selection) - [Assets/Scripts/Gameplay/GameState/NetworkCharSelection.cs ](Assets/Scripts/Gameplay/GameState/NetworkCharSelection.cs)<br>[Assets/Scripts/Gameplay/GameState/ServerCharSelectState.cs](Assets/Scripts/Gameplay/GameState/ServerCharSelectState.cs)
* Win state - [Assets/Scripts/Gameplay/GameState/PersistentGameState.cs](Assets/Scripts/Gameplay/GameState/PersistentGameState.cs)

### Connectivity
* Disconnecting every client with reason - OnUserRequestedShutdown() in [Assets/Scripts/ConnectionManagement/ConnectionState/HostingState.cs ](Assets/Scripts/ConnectionManagement/ConnectionState/HostingState.cs)
* Connection approval with reason sent to the client when denied - ApprovalCheck() in [Assets/Scripts/ConnectionManagement/ConnectionState/HostingState.cs ](Assets/Scripts/ConnectionManagement/ConnectionState/HostingState.cs)
* Connection state machine with error handling - [Assets/Scripts/ConnectionManagement/ConnectionManager.cs ](Assets/Scripts/ConnectionManagement/ConnectionManager.cs) <br> [Assets/Scripts/ConnectionManagement/ConnectionState/](Assets/Scripts/ConnectionManagement/ConnectionState/)
* UTP setup for IP - ConnectionMethodIP in [Assets/Scripts/ConnectionManagement/ConnectionMethod.cs](Assets/Scripts/ConnectionManagement/ConnectionMethod.cs)
* UTP setup for Relay - ConnectionMethodRelay in [Assets/Scripts/ConnectionManagement/ConnectionMethod.cs](Assets/Scripts/ConnectionManagement/ConnectionMethod.cs)
* Session manager - [Packages/com.unity.multiplayer.samples.coop/Utilities/Net/SessionManager.cs ](Packages/com.unity.multiplayer.samples.coop/Utilities/Net/SessionManager.cs)
* RTT stats - [Assets/Scripts/Utils/NetworkOverlay/NetworkStats.cs](Assets/Scripts/Utils/NetworkOverlay/NetworkStats.cs)

### Services (Lobby, Relay, etc)
* Lobby and relay - host creation - CreateLobbyRequest() in [Assets/Scripts/Gameplay/UI/Lobby/LobbyUIMediator.cs ](Assets/Scripts/Gameplay/UI/Lobby/LobbyUIMediator.cs)
* Lobby and relay - client join - JoinLobbyRequest() in [Assets/Scripts/Gameplay/UI/Lobby/LobbyUIMediator.cs ](Assets/Scripts/Gameplay/UI/Lobby/LobbyUIMediator.cs)
* Relay Join - StartClientLobby() in [Assets/Scripts/ConnectionManagement/ConnectionState/OfflineState.cs ](Assets/Scripts/ConnectionManagement/ConnectionState/OfflineState.cs)
* Relay Create - StartHostLobby() in [Assets/Scripts/ConnectionManagement/ConnectionState/OfflineState.cs ](Assets/Scripts/ConnectionManagement/ConnectionState/OfflineState.cs)
* Subscribing to LobbyEvents - SubscribeToJoinedLobby() in [Assets/Scripts/UnityServices/Lobbies/LobbyServiceFacade.cs ](Assets/Scripts/UnityServices/Lobbies/LobbyServiceFacade.cs)
* Authentication - EnsurePlayerIsAuthorized() in [Assets/Scripts/UnityServices/Auth/AuthenticationServiceFacade.cs ](Assets/Scripts/UnityServices/Auth/AuthenticationServiceFacade.cs)
* Authentication - Profile management for ParrelSync/local instances - GetProfile() in [Assets/Scripts/Utils/ProfileManager.cs](Assets/Scripts/Utils/ProfileManager.cs)
* Profile manager for ParrelSync and local play [Assets/Scripts/Utils/ProfileManager.cs](Assets/Scripts/Utils/ProfileManager.cs)

### Tools and Utilities
* Networked message channel (inter-class and networked messaging) - [Assets/Scripts/Infrastructure/PubSub/NetworkedMessageChannel.cs](Assets/Scripts/Infrastructure/PubSub/NetworkedMessageChannel.cs)
* Simple interpolation - [Assets/Scripts/Utils/PositionLerper.cs ](Assets/Scripts/Utils/PositionLerper.cs)
* Network Object Pooling - [Assets/Scripts/Infrastructure/NetworkObjectPool.cs ](Assets/Scripts/Infrastructure/NetworkObjectPool.cs)
* NetworkGuid - [Assets/Scripts/Infrastructure/NetworkGuid.cs ](Assets/Scripts/Infrastructure/NetworkGuid.cs)
* Netcode hooks - [Packages/com.unity.multiplayer.samples.coop/Utilities/Net/NetcodeHooks.cs ](Packages/com.unity.multiplayer.samples.coop/Utilities/Net/NetcodeHooks.cs)
* Spawner for in-scene objects - [Packages/com.unity.multiplayer.samples.coop/Utilities/Net/NetworkObjectSpawner.cs ](Packages/com.unity.multiplayer.samples.coop/Utilities/Net/NetworkObjectSpawner.cs)
* Session manager for reconnection - [Packages/com.unity.multiplayer.samples.coop/Utilities/Net/SessionManager.cs ](Packages/com.unity.multiplayer.samples.coop/Utilities/Net/SessionManager.cs)
* Relay utils - [Packages/com.unity.multiplayer.samples.coop/Utilities/Net/UnityRelayUtilities.cs ](Packages/com.unity.multiplayer.samples.coop/Utilities/Net/UnityRelayUtilities.cs)
* Client authority - [Packages/com.unity.multiplayer.samples.coop/Utilities/Net/ClientAuthority/](Packages/com.unity.multiplayer.samples.coop/Utilities/Net/ClientAuthority/)
* Scene utils with synced loading screens - [Packages/com.unity.multiplayer.samples.coop/Utilities/SceneManagement/ ](Packages/com.unity.multiplayer.samples.coop/Utilities/SceneManagement/)
* RNSM custom config - [Packages/com.unity.multiplayer.samples.coop/Utilities/Net/RNSM/CustomNetStatsMonitorConfiguration.asset ](Packages/com.unity.multiplayer.samples.coop/Utilities/Net/RNSM/CustomNetStatsMonitorConfiguration.asset)
* NetworkSimulator usage through UI - [Assets/Scripts/Utils/NetworkSimulatorUIMediator.cs ](Assets/Scripts/Utils/NetworkSimulatorUIMediator.cs)
* ParrelSync - [ Packages/manifest.json ](Packages/manifest.json)
</details>

-------
<br>

## Troubleshooting
  
### Bugs 
- Report bugs in Boss Room using Github [issues](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/issues)
- Report NGO bugs using NGO Github [issues](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects)
- Report Unity bugs using the [Unity bug submission process](https://unity3d.com/unity/qa/bug-reporting).
  
### Documentation  
For a deep dive into Unity Netcode and Boss Room, visit our [documentation site](https://docs-multiplayer.unity3d.com/).
<br><br>
  
## License
Boss Room is licensed under the Unity Companion License. See [LICENSE.md](LICENSE.md) for more legal information.

For a deep dive in Unity Netcode and Boss Room, visit our [docs site](https://docs-multiplayer.unity3d.com/).
<br><br>

## Contributing
We welcome your contributions to this sample code and objects. See our [contribution guidelines](CONTRIBUTING.md) for details.
  
Our projects use the `git-flow` branching strategy:
 - our **`develop`** branch contains all active development
 - our **`main`** branch contains release versions

To get the project on your machine you need to clone the repository from GitHub using the following command-line command:
```
git clone https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop.git
```

**PLEASE NOTE:** You will need to have [Git LFS](https://git-lfs.github.com/) installed on your local machine in order to clone our repo.
<br><br>

## Community
For help, questions, networking advice, or discussions about Netcode for GameObjects and its samples, please join our [Discord Community](https://discord.gg/FM8SE9E) or create a post in the [Unity Multiplayer Forum](https://forum.unity.com/forums/netcode-for-gameobjects.661/).
<br><br>

## Feedback Form

Thank you for cloning Boss Room and taking a look at the project. To help us improve and build better samples in the future, please consider submitting feedback about your experiences with Boss Room and let us know if you were able to learn everything you needed to today. It'll only take a couple of minutes. Thanks!

[Enter the Boss Room Feedback Form](https://unitytech.typeform.com/bossroom)
<br><br>

## Other samples
### Bite-size Samples
- The [Bitesize Samples](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize)  repository is currently being expanded and contains a collection of smaller samples and games, showcasing sub-features of NGO. You can review these samples with documentation to understand our APIs and features better.
<br><br>

[![Documentation](https://img.shields.io/badge/Unity-boss--room--docs-57b9d3.svg?logo=unity&color=2196F3)](https://docs-multiplayer.unity3d.com/netcode/current/learn/bossroom/bossroom)
[![Forums](https://img.shields.io/badge/Unity-multiplayer--forum-57b9d3.svg?logo=unity&color=2196F3)](https://forum.unity.com/forums/multiplayer.26/)
[![Discord](https://img.shields.io/discord/449263083769036810.svg?label=discord&logo=discord&color=5865F2)](https://discord.gg/FM8SE9E)
<br><br>
  
  ![](Documentation/Images/Players.png)