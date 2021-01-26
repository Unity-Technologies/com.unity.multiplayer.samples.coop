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
