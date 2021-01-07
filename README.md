# BossRoom Co-op game made with MLAPI

## Installation and getting started
- Clone the repository
- Open the root folder of the repository in the Unity Hub.
- The Multipie package is currently internal only. If it causes issues while trying to open the project remove it from Packages/manifest.json
- The entry scene is the *MainMenu* scene. From there a game can be hosted or an existing game can be joined.



## Using the Photon Transport
- Add the PhotonRealtimeTransport component to the NetworkingManager GameObject.
- Set the appID to: bc5b8b0d-edf3-4c61-9593-ce38da7f0c79 (for internal use only) or create your own appid on https://www.photonengine.com/
- Set the RoomName to something which isn't an empty string like test.
- Replace the transport variable of the NetworkingManager to the PhotonRealtimeTransport component.
- Enter Playmode as host/clients => It should just work.

## Using the Navigation System

### Building a navigation mesh
The project is using NavMeshComponents. This means direct building from the Navigation window will not give the desired results. Instead find a NavMeshComponent in the given scene e.g. a **NavMeshSurface** and use the bake button of that script. Also make sure that there is always only one navmesh file per scene. Navmesh files are stored in a folder with the same name as the corresponding scene. You can recognize them based on their icon in the editor. They follow the naming pattern "NavMesh-\<name-of-creating-object\.asset>"

### Dynamic Navigation Objects
A dynamic navigation object is an object which affects the state of the navigation mesh such as a door which can be openend or closed.
To create a dynamic navigation object add a NavMeshObstacle to it and configure the shape (in most cases this should just match the corresponding collider). Then add a DynamicNavObstacle component to it.

### Navigation System
Each scene which uses navigation or dynamic navigation objects should have a NavigationSystem component on a scene gameobject. That object also needs to have the "NavigationSystem" tag.