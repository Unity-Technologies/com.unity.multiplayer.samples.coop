# Boss Room's Utilities package


This package offers reusable utilities for your own projects.

## Getting the project

You can add this package via the Package Manager window in the Unity Editor by selecting add from Git URL and adding the following URL: https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop.git?path=/Packages/com.unity.multiplayer.samples.coop#main

Or you can directly add this line to your manifest.json file:

"com.unity.multiplayer.samples.coop": "https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop.git?path=/Packages/com.unity.multiplayer.samples.coop#main"

The project is licensed under the Unity Companion License. See [LICENSE.md](LICENSE.md) for more legal information.

> __IMPORTANT__:
> - The Utilities package supports those platforms supported by Netcode for GameObjects.
> - Utilities is compatible with Unity 2020.3 and later.
> - Make sure to include standalone support for Windows/Mac in your installation. 

## Features

Multiple utilities classes are available in the [Utilities](Utilities) folder. For example the [ClientNetworkTransform](Utilities/Net/ClientAuthority/ClientNetworkTransform.cs) and [Session Manager](Utilities/Net/SessionManager.cs).

## Usage

For example usage, please see [Boss Room](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop).
