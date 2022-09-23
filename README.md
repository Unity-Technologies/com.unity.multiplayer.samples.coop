![Banner](Documentation/Images/Banner.png)
## Co-op multiplayer RPG and utilities built with Unity Netcode for GameObjects

| Support is available on [Discord](https://discord.gg/mNgM2XRDpb) and [forums](https://forum.unity.com/forums/multiplayer.26/) to help you work through issues you may encounter when using Boss Room. |
| -- |

## Table of content

<!-- TOC generated from https://luciopaiva.com/markdown-toc/ -->

- [Boss Room](#boss-room)
- [Index of ressources in this project](#index-of-ressources-in-this-project)
  - [Gameplay](#gameplay)
  - [Connectivity](#connectivity)
  - [Services (Lobby, Relay, etc)](#services-lobby-relay-etc)
  - [Boss Room specific utils](#boss-room-specific-utils)
  - [Reusable utils](#reusable-utils)
- [Getting the project](#getting-the-project)
  - [Direct download](#direct-download)
  - [Installing Git LFS to clone locally](#installing-git-lfs-to-clone-locally)
- [Registering the project with Unity Gaming Services (UGS)](#registering-the-project-with-unity-gaming-services-ugs)
- [Opening the project for the first time](#opening-the-project-for-the-first-time)
- [Testing multiplayer](#testing-multiplayer)
- [Exploring the project](#exploring-the-project)
- [Other samples](#other-samples)
  - [Bite-size Samples](#bite-size-samples)
- [Contributing](#contributing)

## Boss Room

Boss Room is a fully functional co-op multiplayer RPG made with Unity Netcode. It is built to serve as an educational sample that showcases certain typical gameplay [patterns](https://docs-multiplayer.unity3d.com/netcode/current/learn/bossroom-examples/bossroom-actions) that are frequently featured in similar networked games.

You can use everything in this project as a starting point or as bits and pieces in your own Unity games. The project is licensed under the Unity Companion License. See [LICENSE.md](LICENSE.md) for more legal information.

This repo also contains a [Utilities](Packages/com.unity.multiplayer.samples.coop) package, containing sample scripts reusable in your own projects. You can install it using the following manifest file entry:

`"com.unity.multiplayer.samples.coop": "https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop.git?path=/Packages/com.unity.multiplayer.samples.coop",
`

See [ART_NOTES.md](Documentation/ART_NOTES.md) for more information on the art of Boss Room.

> __IMPORTANT__:
> - Boss Room has been developed and tested on these Platforms (Windows, Mac, iOS, and Android).
>     - Tested on iPhone 6 and Pixel 3.
> - Boss Room is compatible with the latest Unity LTS version.
> - Make sure to include standalone support for Windows/Mac in your installation. 

![](Documentation/Images/Boss.png)

![](Documentation/Images/Players.png)

## Index of ressources in this project

### Gameplay
* All Actions
  * [Action anticipation]()
  * [Object spawning for long actions (archer arrow)]()
  * [Quick actions with RPCs (mage bolt)]()
  * [Teleport]()
  * [Client side input tracking before an action (archer AOE)]()
  * [Time based action (charged shot)]()
  * [object parenting]()
  * [physics object throwing]()
* Characters and objects
  * [Imp spawning]()
  * [In scene placed imps]()
  * [State tracking with breakables, switch, doors]()
  * [Player/avatar structure]()
  * [Character logic]()
* [Interpolation]()
* Game flow
  * [Application Controller]()
  * [Scene state machine]()
* [Gameplay messaging between classes]()
* [In game lobby (character selection)]()
### Connectivity
* [connection state machine]()
* [session manager]()
### Services (Lobby, Relay, etc)
* [Lobby calls - LobbyServiceFacade]()
* [Relay setup]()
* [Auth]()
### Boss Room specific utils
* [Pooling]()
* [NetworkGuid]()
### Reusable utils
* Tools
  * [RNSM]()
* [Netcode hooks]()
* [Spawner]()
* [Session manager]()
* [Relay utils]()
* [Client authority]()
* [Scene utils]()

## Getting the project
### Direct download
 - The pre-release version can be downloaded from the [Releases](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/releases) page. 
 - Alternatively: click the green `Code` button and then choose to download the zip archive. Remember, that you would download the branch that you are currently viewing in Github.
 - For Windows users: Using Windows' built-in extracting tool may generate a "Error 0x80010135: Path too long" error window which can invalidate the extraction process. A workaround for this is to shorten the zip file to a single character (eg. "c.zip") and move it to the shortest path on your computer (most often right at C:\\) and retry. If that solution fails, another workaround is to extract the downloaded zip file using 7zip.


### Installing Git LFS to clone locally

This project uses Git Large Files Support (LFS), which ensures all large assets required locally are handled for the project. See [Git LFS installation options](https://github.com/git-lfs/git-lfs/wiki/Installation) for Windows and Mac instructions. 

## Registering the project with Unity Gaming Services (UGS)

This project leverages several services from UGS to facilitate connectivity between players. In order to use these services inside your project, one must first [create an organization](https://support.unity.com/hc/en-us/articles/208592876-How-do-I-create-a-new-Organization-) inside Unity Dashboard, and enable both the [Relay](https://docs.unity.com/relay/get-started.html) and [Lobby](https://docs.unity.com/lobby/game-lobby-sample.html) services. Otherwise, Boss Room can still be used without UGS.

## Opening the project for the first time

Once you have downloaded the project, the steps below should get you up and running:
 - Make sure you have installed the version of Unity that is listed above in the prerequisites section.
 	- Make sure to include standalone support for Windows/Mac in your installation. 
 - Add the project in _Unity Hub_ by clicking on **Add** button and pointing it to the root folder of the downloaded project.
 	- The first time you open the project Unity will import all assets, which will take longer than usual - it is normal.
 - Once the editor is ready, navigate to the _Project_ window and open the _Project/Startup_ scene.
![](Documentation/Images/StartupScene.png)
 - From there you can click the **Play** button. You can host a new game or join an existing game using the in-game UI.

## Testing multiplayer

In order to see the multiplayer functionality in action we can either run multiple instances of the game locally on your computer, using either ParrelSync or builds or choose to connect to a friend over the internet. See [how to test](https://docs-multiplayer.unity3d.com/netcode/current/tutorials/testing/testing_locally) for more info.

---------------
**Local multiplayer setup**

First we need to build an executable.

To build an executable, press _File/Build Settings_ in the menu bar, and then press **Build**.
![](Documentation/Images/BuildProject.png)

Once the build has completed you can launch several instances of the built executable in order to both host and join a game.

> Mac users: to run multiple instances of the same app, you need to use the command line.
> Run `open -n BossRoom.app`

---------------
**Multiplayer over internet**

To play over internet, we need to build an executable that is shared between all players. See the previous section.

It is possible to connect between multiple instances of the same executable OR between executables and the editor that produced said executable.

Running the game over internet currently requires setting up a relay. Boss Room provides an integration with [Unity Relay](https://docs-multiplayer.unity3d.com/netcode/current/relay/relay).

> Checkout our Unity Relay setup guide [here](https://docs-multiplayer.unity3d.com/netcode/current/relay/relay)

Alternatively you can use Port Forwarding. The https://portforward.com/ site has guides on how to enable port forwarding on a huge number of routers. Boss Room uses `UDP` and needs a `9998` external port to be open. Make sure your host's address listens on 0.0.0.0 (127.0.0.1 is for local development only).

------------------------------------------

## Exploring the project
BossRoom is an eight-player co-op RPG game experience, where players collaborate to take down some minions, and then a boss. Players can select between classes that each have skills with didactically interesting networking characteristics. Control model is click-to-move, with skills triggered by mouse button or hotkey. 

One of the eight clients acts as the host/server. That client will use a compositional approach so that its entities have both server and client components.

The game is server-authoritative, with latency-masking animations. Position updates are done through NetworkedVars that sync position, rotation and movement speed. NetworkedVars and Remote Procedure Calls (RPC) endpoints are isolated in a class that is shared between the server and client specialized logic components. All game logic runs in FixedUpdate at 30 Hz, matching our network update rate. 

Code is organized into three separate assemblies: **Client**, **Shared** and **Server** which reference each other when appropriate.

For an overview of the project's architecture please check out our [ARCHITECTURE.md](ARCHITECTURE.md).

---------------

For a deep dive in Unity Netcode and Boss Room, visit our [docs site](https://docs-multiplayer.unity3d.com/).

## Other samples
### Bite-size Samples
This repository contains a collection of bitesize sample projects and games that showcase different sub-features of NGO. You can review these samples with documentation to understand APIs and features better.
- [Our various bitesize samples](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize)

## Contributing

The project uses the `git-flow` branching strategy, as such:
 - `develop` branch contains all active development
 - `main` branch contains release versions

To get the project on your machine you need to clone the repository from GitHub using the following command-line command:
```
git clone https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop.git
```

> __IMPORTANT__: 
> You should have [Git LFS](https://git-lfs.github.com/) installed on your local machine.

Please check out [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines on submitting issues and PRs to BossRoom!

For further discussion points and to connect with the team, join us on the Unity Multiplayer Networking Discord Server - Channel #dev-samples

[![Discord](https://img.shields.io/discord/449263083769036810.svg?label=discord&logo=discord&color=informational)](https://discord.gg/FM8SE9E)
