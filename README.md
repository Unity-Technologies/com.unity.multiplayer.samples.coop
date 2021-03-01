![Banner](Documentation/Images/Banner.png)
# BossRoom - co-op multiplayer RPG built with Unity MLAPI

BossRoom is a fully functional co-op multiplayer RPG made in Unity and MLAPI. It is built to serve as an educational sample that showcases certain typical gameplay patterns that are frequently featured in similar games.

Our intention is that you can use everything in this project as a starting point or as bits and pieces in your own Unity games. The project is licensed under the Unity Companion License. See [LICENSE.md](LICENSE.md) for more legal information.


## Prerequisites
```
Platforms    : Windows, Mac
```

## Getting the project

> __IMPORTANT__: 
> This project uses Git Large Files Support (LFS). See the [link with Git LFS installation options](https://git-lfs.github.com/).
> 
> Downloading the project using `Download` button would not work,

The project uses the `git-flow` branching strategy, as such:
 - `develop` branch contains all active development
 - `master` branch contains release versions

To get the project on your machine you need to clone the repository from GitHub using the following command-line command:
```
git clone https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop.git
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

> Mac users:, to run multiple instances of the same app, you need to use the command line.
> Run `open -n BossRoom.app`


## Testing multiplayer over internet

Running the game over internet currently requires setting up a [Photon Transport for MLAPI](https://github.com/Unity-Technologies/mlapi-community-contributions), which uses Photon relay server to facilitate communication between clients and server living on different networks.  

------------------------------------------

## Exploring the project
BossRoom is an 8-player co-op RPG game experience, where players collaborate to take down some minions, and then a boss. Players can select between classes that each have skills with didactically interesting networking characteristics. Control model is click-to-move, with skills triggered by mouse button or hotkey. 

One of the 8 clients acts as the host/server. That client will use a compositional approach so that its entities have both server and client components.

The game is server-authoritative, with latency-masking animations. Position updates are done through NetworkedVars that sync position, rotation and movement speed. NetworkedVars and Remote Procedure Calls (RPC) endpoints are isolated in a class that is shared between the server and client specialized logic components. All gamelogic runs in FixedUpdate at 30 Hz, matching our network update rate. 

Code is organized into three separate assemblies: **Client**, **Shared** and **Server** which reference each other when appropriate.

For an in-depth overview of the project's architecture please check out our [ARCHITECTURE.md](ARCHITECTURE.md).