![Banner](Documentation/Images/Banner.png)
# Boss Room - co-op multiplayer RPG built with Unity MLAPI

| 🛑  IMPORTANT - Early Access 🛑  | 
| -- |
| Boss Room: Small Scale Co-op Sample is built on top of the MLAPI package. The MLAPI package  is on the road to being a fully featured solution. We have solutions architects available on Discord and forums to help you work through issues you encounter. |

Boss Room is a fully functional co-op multiplayer RPG made with Unity MLAPI. It is built to serve as an educational sample that showcases certain typical gameplay patterns that are frequently featured in similar games.

Our intention is that you can use everything in this project as a starting point or as bits and pieces in your own Unity games. The project is licensed under the Unity Companion License. See [LICENSE.md](LICENSE.md) for more legal information.

> __IMPORTANT__:
> - Boss Room supports those platforms supported by MLAPI (Windows and Mac).
> - Boss Room is compatible with Unity 2020.3 and later.
> - Make sure to include standalone support for Windows/Mac in your installation. 


```
Platforms : Windows, Mac
```

![](Documentation/Images/3Players.png)
![](Documentation/Images/Boss.png)

## Getting the project
 - The early access version can be downloaded from the [Releases](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/releases) page. 
 - Alternatively: click the green `Code` button and then choose to download the zip archive. Remember, that you would download the branch that you are currently viewing in Github.
 - For Windows users: Using Windows' built-in extracting tool may generate a "Error 0x80010135: Path too long" error window which can invalidate the extraction process. A workaround for this is to shorten the zip file to a single character (eg. "c.zip") and move it to the shortest path on your computer (most often right at C:\\) and retry. If that solution fails, another workaround is to extract the downloaded zip file using 7zip.


## Installing Git LFS

This project uses Git Large Files Support (LFS), which ensures all large assets required locally are handled for the project. See [Git LFS installation options](https://github.com/git-lfs/git-lfs/wiki/Installation) for Windows and Mac instructions. 

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

In order to see the multiplayer functionality in action we can either run multiple instances of the game locally on our computer or choose to connect to a friend over the internet.

---------------
**Local multiplayer setup**

First we need to build an executable.

To build an executable  press _File/Build Settings_ in the menu bar, and then press **Build**.
![](Documentation/Images/BuildProject.png)

Once the build has completed you can launch several instances of the built executable in order to both host and join a game.

> Mac users: to run multiple instances of the same app, you need to use the command line.
> Run `open -n BossRoom.app`

---------------
**Multiplayer over internet**

To play over internet, we need to build an executable that is shared between all players. See the previous section.

It is possible to connect between multiple instances of the same executable OR between executables and the editor that produced said executable.

Running the game over internet currently requires setting up a [Photon Transport for MLAPI](https://github.com/Unity-Technologies/mlapi-community-contributions), which uses Photon relay server to facilitate communication between clients and server living on different networks.

> Checkout our Photon-Realtime setup guide, here:
> [Boss Room Photon Setup Guide](Documentation/Photon-Realtime/Readme.md)

Alternatively you can use Port Forwarding. The https://portforward.com/ site has guides on how to enable port forwarding on a huge number of routers. Boss Room uses `UDP` and needs a `9998` external port to be open. 

------------------------------------------

## Exploring the project
BossRoom is an eight-player co-op RPG game experience, where players collaborate to take down some minions, and then a boss. Players can select between classes that each have skills with didactically interesting networking characteristics. Control model is click-to-move, with skills triggered by mouse button or hotkey. 

One of the eight clients acts as the host/server. That client will use a compositional approach so that its entities have both server and client components.

The game is server-authoritative, with latency-masking animations. Position updates are done through NetworkedVars that sync position, rotation and movement speed. NetworkedVars and Remote Procedure Calls (RPC) endpoints are isolated in a class that is shared between the server and client specialized logic components. All game logic runs in FixedUpdate at 30 Hz, matching our network update rate. 

Code is organized into three separate assemblies: **Client**, **Shared** and **Server** which reference each other when appropriate.

For an overview of the project's architecture please check out our [ARCHITECTURE.md](ARCHITECTURE.md).

---------------

For a deep dive in MLAPI and Boss Room, visit our [doc](https://docs-multiplayer.unity3d.com/) and [Learn](https://docs-multiplayer.unity3d.com/docs/learn/introduction) sections.

## Contributing

The project uses the `git-flow` branching strategy, as such:
 - `develop` branch contains all active development
 - `master` branch contains release versions

To get the project on your machine you need to clone the repository from GitHub using the following command-line command:
```
git clone https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop.git
```

> __IMPORTANT__: 
> You should have [Git LFS](https://git-lfs.github.com/) installed on your local machine.

Please check out [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines on submitting issues and PRs to BossRoom!

For futher discussion points and to connect with the team, join us on the MLAPI by Unity Discord Server - Channel #dev-samples

[![Discord](https://img.shields.io/discord/449263083769036810.svg?label=discord&logo=discord&color=informational)](https://discord.gg/FM8SE9E)
