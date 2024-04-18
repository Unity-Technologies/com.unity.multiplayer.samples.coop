# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Additional documentation and release notes are available at [Multiplayer Documentation](https://docs-multiplayer.unity3d.com).

## [2.5.0] - 2024-04-18

### Changed
* Upgraded Boss Room to Netcode for GameObjects v1.8.1 (#883)
  * Upgraded to the newer API for Rpcs, Universal Rpcs
  * ClientConnectedState has been modified to account for server/host now populating DisconnectReason before disconnecting a client before shutting down
* Upgraded editor version to 2022.3.22f1 (#884)
  * com.unity.render-pipelines.universal upgraded to v14.0.10
* ClientPlayerAvatarNetworkAnimator has been created to: instantiate the player model based on a networked GUID, rebind this rig to the player's Animator, and apply synchronize data to said Animator (#886)
  * This change allows for NetworkAnimator's synchronize step to properly apply its sync data to clients instead of applying an animation state change on OnNetworkSpawn()
  * A side-effect of this change has been that a coroutine that had been awaiting the assignment of NetworkAnimator has since been removed as it is no longer an issue on Netcode for GameObjects (since v1.3.1)

### Cleanup
* Removed NetworkObject from MainMenuState (#881)

### Fixed
* Changed Canvas Sort order of multiple UI elements to enable visibility of RNSM and reconnection attempts during the loading screen (#879)
* Added Null reference check to ClientInputSender to fix null reference for missing ability (#880)

## [2.4.0] - 2023-12-13

### Changed
* Upgraded editor version to 2022.3.14f1 (#871)
  * com.unity.ai.navigation upgraded to v1.1.5
  * com.unity.render-pipelines.universal upgraded to v14.0.9
  * com.unity.services.authentication upgraded to v2.7.1
* Upgraded Boss Room to Netcode for GameObjects v1.7.1 (#871)

### Fixed
* Fixed NetworkVariable warnings that would be logged when a player was spawned (#863) For a player, certain NetworkVariable values were previously modified before the player's NetworkObject was spawned, resulting in warnings. Now, the NetworkVariable itself is instantiated on the server pre-spawn, such that it is instantiated with the new default value, ensuring the new default value is ready to be read on subsequent OnNetworkSpawn methods for said NetworkObject.

## [2.3.0] - 2023-09-07

### Changed
* Upgraded editor version to 2022.3.7f1 (#855)
  * Upgraded Authentication Service package to v2.7.1
* Replaced usages of null-coalescing and null-conditional operators with regular null checks. (#867) These operators can cause issues when used with types inheriting UnityEngine.Object because that type redefines the == operator to define when an object is null. This redefinition applies to regular null checks (if foo == null) but not to those operators, thus this could lead to unexpected behaviour. While those operators were safely used within Boss Room, only with types that were not inheriting UnityEngine.Object, we decided to remove most usages for consistency. This will also help avoid accidental mistakes, such as a user reusing a part of this code, but modifying it so that one of those operators are used with a UnityEngine.Object.
* Upgraded Boss Room to Netcode for GameObjects v1.6.0 (#865)
  * A package Version Define has been created for Netcode for GameObjects v.1.5.2 - v1.6.0. Recent refactorings to NetworkManager's shutdown have prevented the ability to invoke CustomMessages when OnClientDisconnected callbacks are invoked during a shutdown as host. This regression has caused one of our runtime tests, namely Unity.BossRoom.Tests.Runtime.ConnectionManagementTests.UnexpectedServerShutdown_ClientsFailToReconnect, to fail and it does not impact gameplay. This is a known issue and will be addressed in a future NGO version.
* Upgraded to Lobby 1.1.0 (#860).
  * Lobbies are now locked when being created and are only unlocked when the relay allocation is ready.
  * Removed explicit reference to Wire in the package manifest, since Wire is already a dependency of Lobby
  
### Fixed
* Fixed colliders on diagonal walls to not have negative scale (#854).
* Fixed order of components in networked GameObjects (#866). NetworkObjects are now always above NetworkBehaviours in the component order in GameObjects. This fixes a bug where during scene unloading the NetworkBehaviours would be destroyed before the NetworkObject on the host, which caused these NetworkBehaviours to not have their OnNetworkDespawned invoked in that situation on the host.
* Unnecessary update requests are no longer being sent to Lobby after receiving update events from the service (#860).
* Fixed a condition where one would be unable to quit the application through OS-level quit button, nor the in-game quit button (#863)

## [2.2.0] - 2023-07-06

### Added
* Adding NetworkSimulator tool (#843). It can be used through the NetworkSimulator component's editor (see the [NetworkSimulator documentation](https://docs-multiplayer.unity3d.com/tools/current/tools-network-simulator/)), but only in-editor. To be able to use it in a build, a custom in-game UI window was added. The in-game UI window opens up automatically when starting or joining a networked session, and can be opened and closed again by pressing 'tab' on a keyboard, or using five fingers at once on mobile.

### Changed
* Upgraded editor version to 2022.3.0f1 (#840)
* Updated Multiplayer Tools to version 2.0.0-pre.3 (#840)
* NetworkTransform bandwidth optimizations applied to NetworkObject prefabs inside project (#836) Netcode for GameObjects v1.4.0 introduced bandwidth compression techniques to further reduce the bandwidth footprint of a NetworkTransform's synchronization payload. Inside Boss Room, the base prefab for PCs and NPCs, Character, had its NetworkTransform modified to now utilize half float precision, ie. "Use Half Float Precision" set to true. Its y position is also explicitly no longer synced. This results in a net 5 byte reduction in a NetworkTransform's synchronization payload. This bandwidth reduction was applied also to the Archer's arrow NetworkObject prefabs. Additionally, several NetworkObjects have now their "Synchronize Transform" flag disabled inside their NetworkObject component, meaning that its transform properties will not be synced when spawning and/or when late-joining clients connect. This is particularly useful if the NetworkObject is used more for management related tasks and has no spatial synchronization needs. For more information, see [Netcode for GameObjects' v1.4.0 release notes](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/releases/tag/ngo%2F1.4.0).
* Updated Unity Transport Package to version 2.0.2 (#843). This gives access to the NetworkSimulator tool.
* Changed quality settings to allow full resolution MipMaps on mobile as a workaround for a regression in UI UV scaling (#848)  

## [2.1.0] - 2023-04-27

### Added
* Added OnServerStopped event to ConnectionManager and ConnectionState (#826). This allows for the detection of an unexpected shutdown on the server side.

### Changed
* Replaced our polling for lobby updates with a subscription to the new Websocket based LobbyEvents (#805). This saves up a significant amount of bandwidth usage to and from the service, since updates are infrequent in this game. Now clients and hosts only use up bandwidth on the Lobby service when it is needed. With polling, we used to send a GET request per client once every 2s. The responses were between ~550 bytes and 900 bytes, so if we suppose an average of 725 bytes and 100 000 concurrent users (CCU), this amounted to around 725B * 30 calls per minute * 100 000 CCU = 2.175 GB per minute. Scaling this to a month would get us 93.96 TB per month. In our case, since the only changes to the lobbies happen when a user connects or disconnects, most of that data was not necessary and can be saved to reduce bandwidth usage. Since the cost of using the Lobby service depends on bandwidth usage, this would also save money on an actual game.
* Simplified reconnection flow by offloading responsibility to ConnectionMethod (#804). Now the ClientReconnectingState uses the ConnectionMethod it is configured with to handle setting up reconnection (i.e. reconnecting to the Lobby before trying to reconnect to the Relay server if it is using Relay and Lobby). It can now also fail early and stop retrying if the lobby doesn't exist anymore.
* Replaced our custom pool implementation using queues with ObjectPool (#824)(#827)
* Upgraded Boss Room to NGO 1.3.1 (#828) NetworkPrefabs inside NetworkManager's NetworkPrefabs list have been converted to NetworkPrefabsList ScriptableObject. 
* Upgraded Boss Room to NGO 1.4.0 (#829)
* Profile names generated are now only 30 characters or under to fit Authentication Service requirements (#831)

### Cleanup
* Clarified a TODO comment inside ClientCharacter, detailing how anticipation should only be executed on owning client players (#786)
* Removed now unnecessary cached NetworkBehaviour status on some components, since they now do not allocate memory (#799) 
* Certain structs converted to implement interface INetworkSerializeByMemcpy instead of INetworkSerializable (#822) INetworkSerializeByMemcpy optimizes for performance at the cost of bandwidth usage and flexibility, however it will only work with structs containing value types. For more details see the official [doc](https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/serialization/inetworkserializebymemcpy/index.html).

### Fixed
* EnemyPortals' VFX get disabled and re-enabled once the breakable crystals are broken (#784)
* Elements inside the Tank's and Rogue's AnimatorTriggeredSpecialFX list have been revised to not loop AudioSource clips, ending the logging of multiple warnings to the console (#785)
* ClientConnectedState now inherits from OnlineState instead of the base ConnectionState (#801)
* UpdateRunner now sends the right value for deltaTime when updating its subscribers (#805)
* Inputs are better sanitized when entering IP address and port (#821). Now all invalid characters are prevented, and UnityTransport's NetworkEndpoint.TryParse is used to verify the validity of the IP address and port that are entered before making the join/host button interactable.
* Fixed failing connection management test (#826). This test had to be ignored previously because there was no mechanism to detect unexpected server shutdowns. With the OnServerStopped callback introduced in NGO 1.4.0, this is no longer an issue. 
* Decoupled SceneLoaderWrapper and ConnectionStates (#830). The OnServerStarted and OnClientStarted callbacks available in NGO 1.4.0 allows us to remove the need for an external method to initialize the SceneLoaderWrapper after starting a NetworkingSession.

## [2.0.4] - 2022-12-13
### Changed
* Updated Boss Room to NGO 1.2.0 (#791).
  * Removed a workaround in our tests waiting for two frames before shutting down a client that is attempting to connect to a server. (see https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/pull/2261)
* Replaced the workaround using custom messages to send a disconnect reason to clients with the new DisconnectReason feature in NGO. (#790)
* Updating editor version to 2021.3.15f1 (#795)

## [2.0.3] - 2022-12-05

### Changed
* Hosts now delete their lobby when shutting down instead of only leaving it (#772) Since Boss Room doesn't support host migration, there is no need to keep the lobby alive after the host shuts down. This also changes how LobbyServiceExceptions are handled to prevent popup messages on clients trying to leave a lobby that is already deleted, following the best practices outlined in this doc : https://docs.unity.com/lobby/delete-a-lobby.html

### Fixed
* Mage's heal FX plays out on itself and on targets. Added ability for SpecialFXGraphic components to remain at spawn rotation (#771)

## [2.0.2] - 2022-11-01
### Fixed
* Bumped Unity editor version to fix android build error (#779)

## [2.0.1] - 2022-10-25

### Changed
* Updated Boss Room to NGO 1.1.0 (#708)
  *  Now uses managed types for custom INetworkSerializable in NetworkVariables. NetworkGUID is now a class instead of a struct.
  * Cleanup Relay and UTP setup. Flow is now simpler, no need for the RelayUtilities anymore.
  * This cleans up various setup steps and puts them all in a new "ConnectionMethod.cs".
  * MaxSendQueueSize value is removed, reserialized NetworkManager to remove that now useless value.
  * Reverted the default value for max payload size, this is no longer useful as NGO is mostly reliable.
  * Set connection approval timeout higher, 1 sec is pretty short. If there's a packet drop, some hangups on the network, clients would get timedout too easily.

### Cleanup
* Removed unnecessary FindObjectOfType usage inside of ClientCharSelectState (#754)

### Fixed
* Reenabled depth buffer in the URP settings to enable the use of soft particles (#762)
* Moved a torch out of a corner so that the flame VFX don't clip (#768)
* Fixed issue where pressing 1 on keyboard would not invoke Revive or Pickup/Drop Actions (#770) Authority on modification of displayed Action now comes from a single spot, ClientInputSender.

## [2.0.0] - 2022-10-06

### Added
* Added TOC and Index of educational concepts to readme (#736) Boss Room can be quite intimidating for first time users. This index will hopefully help soften the onboarding.
* Added tests for connection management (#692). These are integration tests to validate that the state machine works properly. They use Netcode's NetworkIntegrationTest
* Added handling the OnTransportFailure callback (#707). This callback is invoked when a failure happens on the transport's side, for example if the host loses connection to the Relay service. This won't get called when the host is just listening with direct IP, this would need to be handled differently (by pinging an external service like google to test for internet connectivity for example). Boss Room now handles that callback by returning to the Offline state.
* Pickup and Drop action added to the Action system. Actionable once targeting a "Heavy"-tagged NetworkObject. (#372) - This shows NetworkObject parenting with a pattern to follow animation bones (the hands when picking up)
* Art and sound polish for the pick up and drop action (#749)
* LODs setup for some art assets [MTT-4451] (#712)
* Introduced a mechanism for identifying actions by their runtime-generated ActionID, instead of relying on a fragile ActionType enumeration (#705)
* NetworkObjectSpawner handles dynamically spawning in-scene placed NetworkObjects (#717) - You can't place a NetworkObject in scene directly and destroy it at runtime. This PR showcases proper handling of NetworkObjects that you'd wish to place inside of scenes, but would still want to destroy at game-time. Examples of these are: Imps, VandalImps, ImpBoss. NetworkObjects such as doors, crystals, door switch, etc. remain the same, statically-placed in scene.
* Quality levels settings set up for Desktop [MTT-4450] (#713)
* Added custom RNSM config with graph for RTT instead of single value (#747) Being able to *see* latency bumps and variation is helpful to identify the cause of in-game issues. This also adds clearer headers for each RNSM graphs.
* Vandal Imp and bomb throwing action integrated in main game: NetworkRigidbody-based toss Action, thrown by VandalImp class (code already in 1.3.0)
* Art and sound pass for NetworkRigidbody-based toss action [MTT-2732] (#689) This also adds the Vandal imp to the main game.

### Changed
* Updated tools, authentication and relay packages (#690)
* NetworkedMessageChannels can now be subscribed to before initiating a connection (#670) This allows a subscription's lifetime to not be restricted by a connection, so subscribing before a connection is possible, and subscriptions will still work properly if the connection ends and a new one begins.
* Modified the red arrow of the boss charge attack to fade in and out (rather than just being enabled disabled) (#715)
* Action and ActionFX classes have been merged into a single pooled Scriptable Object-based Action class; all the existing actions have been refactored to follow this new design (#705) This should make these more readable and consistent following our client/server/shared to domain based assemblies refactor.
* Configured the NetworkTransform components of every NetworkObject to reduce the bandwidth usage (#716). This prevents the unnecessary synchronization of data that clients do not need, i.e. a character's scale or y position. In the case of a character, it reduced the size of each update from 47B to 23B.
* Instead of a NetworkBehaviour that carries a WinState netvar we now pass the win state on the server to the PostGame scene and it then stores that state in the netvar, eliminating the need to preserve a NetworkBehaviour-bearing gameObject across scenes. (#724)
* Reduced the MaxPacketQueueSize UTP parameter value from 512 to 256 (#728). This reduces the amount of memory used by UTP by around 1 MB. Boss Room does not need a bigger queue size than this because there can only be 7 clients connected to a host and UTP already limits the maximum number of in-flight packets to 32 per connection.
* Updated Lobby package to 1.0.3 and reworked our auto-reconnect flow to use the Reconnect feature from the Lobby API (#737). Now, clients do not leave the lobby when they are disconnected, and the host does not remove them from it. They are marked as disconnected by the Relay server and can attempt to reconnect to the lobby directly, until a certain timeout (specified by the Disconnect removal time parameer, set in the dashboard configuration).

### Cleanup
* Refactored connection management into simpler state machine (#666) This makes the connection flow easier to follow and maintain. Each connection state now handles user inputs and Netcode callbacks as they should, removing the need for big switch-cases of if-else statements. These inputs and callbacks also trigger transitions between these states.
* Rearranged the Action system by adding more folders that separate different pieces more clearly (#701)
* Merged GameState bridge classes (the ones that contained no or limited functionality) (#697 #732) This cleans up our sometimes too verbose code split.
* Replaced our dependency injection solution with VContainer. (#679) This helps us reduce the amount of code we have to maintain.
* Added Unsubscribe API for the ISubscriber<T> along with refactoring of the codebase to use this API instead of IDisposable handle when there is just one subscription (#612)
* Namespaces in the project have been changed to map to their assembly definitions (#732)
* Numerous name changes for fields and variables to match their new type names (#732)
* Removed DynamicNavObstacle - an unused class (#732)
* Merged networked data classes into their Server counterparts. An example of that change is the contents of NetworkCharacterState getting moved into ServerCharacter, contents of NetworkDoorState getting moved into SwitchedDoor etc. (#732)
* Engine version bump to 2021.3.10f1 (#740)
* Updated the Architecture.md to match the current state of the project, with all of our recent refactorings. Architecture.md now also has a lot of links to the relevant classes, simplifying the navigation in our code-base (#763)

### Fixed
* Subscribing to a message channel while unsubscribing is pending (#675). This issue prevented us from subscribing to a message channel after having unsubscribed to it if no message had been sent between the un-subscription and the new subscription.
* Using ```Visible``` instead of ```Enabled``` to make sure RNSM continues updating when off (#702)
* Some NetworkBehaviours are disabled instead of being destroyed (#718) - This preserves the index order for NetworkBehaviours between server and clients, resulting in no indexing issue for sending/receiving RPCs.
* Scene Bootstrapper: future proofing bootstrap scene so we don't rely on Startup's path. MTT-3707. (#735)
* Better instructions for host listen IP. (#738) Most useful cases are usually 127.0.0.1 and 0.0.0.0.
* Tank's shield charge animation not getting stuck due to multiple invocations. (#742)
* Lobby join button not interactable if no join code is provided. (#744) This prevents an ArgumentNullException happening when we try to join a Lobby with an empty join code.
* Lobby UI unblocking before it should. (#748) This makes sure that we are not unblocking the UI while we are in the middle of the connection process, to prevent users from starting a second one at the same time. Now the UI stays blocked until the connection either succeeds of fails.

## [1.3.1-pre] - 2022-09-13

### Fixed
* Bumped the project to NGO 1.0.2 (#726)

## [v1.3.0-pre] - 2022-06-23

### Added
* Adding RNSM (Runtime Network Stats Monitor) to boss room [MTT-3267] (#621)
* Other players loading progress in loading screen [MTT-2239] (#580)
* Auto reconnect [MTT-2617] (#611)
* Bumping relay version so we now have auto region selection (with the QoS package). This allows selecting the right relay region instead of the default us-central and should reduce latency for non-central folks. (#657)
* Bad network conditions warning [MTT-3242] (#632)
* Added basis for automated testing and CI (#484) (#487) (#639)	
* Connection feedback + IP connection window [MTT-2315] [MTT-3234] (#613)	
* First import of all the vandal imp artwork (#637)
* Updated boss room's root scene to automatically load child scenes at editor time (#653)
* Users can change profile in-game in addition to the -AuthProfile command line argument (#636)
* New Vandal Imp and bomb throwing action: NetworkRigidbody-based toss Action, thrown by new VandalImp class [MTT-2333] (#671)

### Changed
* Bump NGO to pre.10 (#678) --> Fix in Boss Room related to the connection approval breaking change. Removing useless ForceNetworkSerializeByMemcpy for player names.
* Bump Boss Room to Unity 2021 [MTT-3022] (#620)
* Remove initial ugs popup [MTT-3563] (#650) --> Users who do not use UGS will no longer receive a popup when starting the application telling them how to set it up. It is replaced with a tooltip that appears when hovering on the "Start with Lobby" button with the cursor.
* Folders and assemblies refactor MTT-2623, MTT-2615 (#628)(#668)(#669)(#673)(#674)
* Docs: Readme Image Updates (#680)

### Removed
* Remove UNET [MTT-3435] (#638) --> removed deprecated UNET transport from Boss Room

## [1.2.0-pre] - 2022-04-28
### Changes
* Bump to pre.8 and fix compile issues [MTT-3413] (#631) --> Custom message structs now need new interfaces
* Client network transform move to samples [MTT-3406] (#629) --> You can now use Boss Room's Utilities package to import ClientNetworkTransform using this line in your manifest file     
`"com.unity.multiplayer.samples.coop": "https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop.git?path=/Packages/com.unity.multiplayer.samples.coop",`
* In-game message feed [MTT-2678] [MTT-2318] (#601) --> Displays in-game events, including networked events
* Networked message channel (#605) --> To support message feed

### Dependencies
* "com.unity.netcode.gameobjects": "1.0.0-pre.8",
* "com.unity.services.authentication": "1.0.0-pre.4",
* "com.unity.multiplayer.tools": "1.0.0-pre.6",
* "com.unity.services.lobby": "1.0.0-pre.6",
* "com.unity.services.relay": "1.0.1-pre.5",
* "com.veriorpies.parrelsync": "https://github.com/VeriorPies/ParrelSync.git?path=/ParrelSync#bb3d5067e49e403d8b8ba15c036d313b4dd2c696",
* Editor is 2020.3.33f1

### Fixes:
* Fixed Z Fighting of Floor Tiles Near Edge of Main Boss Room (#616)
* SceneBootstrapper detects and allows TestRunner launches (#483)
* Removed feature to set all players unready in char select when a player leaves or joins (#625)
* Removed setting disconnect reason to UserRequested on clients entering post-game (#626)
* Disallowing portait orientation for auto rotation (#627)
* Chore: removing QoS (#623) --> waiting for official release

## [1.1.2-pre] - 2022-04-14
### Fixes

* Readme and projectID-monitoring popup tweaks (#618)

## [1.1.1-pre] - 2022-04-13
### Changes
* **Adding relay auto region selection using the QoS package #617 --> this should reduce ping times for people not in us-central**
* Loading screen (#427)
* Added popup that tells users to set up project id if it's not set up [MTT-3265][MTT-3237] (#607)
* Message to clients when host ends game session intentionally [MTT-3202] (#594)

### Dependencies
* "com.unity.netcode.gameobjects": "1.0.0-pre.7",
* "com.unity.services.authentication": "1.0.0-pre.4",
* "com.unity.multiplayer.tools": "1.0.0-pre.6",
* "com.unity.services.lobby": "1.0.0-pre.6",
* "com.unity.services.relay": "1.0.1-pre.5",
* "com.veriorpies.parrelsync": "https://github.com/VeriorPies/ParrelSync.git?path=/ParrelSync#bb3d5067e49e403d8b8ba15c036d313b4dd2c696",
* Editor is 2020.3.33f1

### Fixes:
* Alpha4 input press opens emote drawer (#599)
* Add Unity Gaming Services info to readmes [MTT-3263] (#608)
* General Project Clean Up, Reduction of Import Times, Minor Material Bug MTT-2737] [MTT-3116] (#606)
* Only server finds navigation object, asserts refactored [MTT-3248] (#602)
* Relogin when auth token is stale [MTT-3253] (#604)
* Scene loading issue when shutting down [MTT-2828] (#610)
* Silencing errors that are not errors [MTT-2930] [MTT-3249] (#597)
* Chore: bump to latest tools version, so we get the awesome new perf improvements (#614)
* Chore: bumping package version and cleaning up dependencies for boss room package (#600)
* Chore: upgrade to 2020.3.33f1 (#615)

## [1.1.0-pre] - 2022-04-07
### Changes
* Adding Lobby service integration and removing Photon (#480) (#567) (#486) (#547) (#585) (#525) (#566) (#511) (#494) (#506) (#510) (#526) (#595) (#551) (#556) (#515) (#553) (#544) (#522) (#524) (#539) (#565) (#550) (#592) (#505) (#518) (#596))
* Adding separate IP window button after lobby integration (#538) (#502)
* Bumping NGO to pre.7 and removing now redundant UTP dependency [MTT-3235] (#598)
* Flag to prevent dev and release builds to connect (#482)
* Replacing guid with auth playerid for session management (#488)
* Using DTLS for relay (#485)
* Adding new cheats (#446): heal player (#448), kill enemies (#447), portals toggle (#452), speedhack toggle (#449), teleport mode (#450), toggle door (#451)
* Adding transport RTT to UI stats and using exponential moving average for calculations (#528)
* Boss and run VFX Optimizations (#514)
* Made a shared menu camera prefab for all of the menu scenes and swapped out the old cameras in the scenes for it (#473)
* Player architecture ARCHITECTURE.md addition (#476)

### Dependencies
* "com.unity.netcode.gameobjects": "1.0.0-pre.7",
* "com.unity.services.authentication": "1.0.0-pre.4",
* "com.unity.multiplayer.tools": "1.0.0-pre.2",
* "com.unity.services.lobby": "1.0.0-pre.6",
* "com.unity.services.relay": "1.0.1-pre.5",
* "com.veriorpies.parrelsync": "https://github.com/VeriorPies/ParrelSync.git?path=/ParrelSync#bb3d5067e49e403d8b8ba15c036d313b4dd2c696",
* Editor is 2020.3.27f1

### Fixes:
* Better flows for authentication, generate unique Profile ID on clone project (#577)
* Fixing imps self-destroying on clients when late-joining with multiple additive scenes loaded (#574)
* Fixing send queue error (#496)
* Fixing connecting while starting game (#481)
* Dead player not dead when reconnecting [MTT-2965] (#583)
* Null refs when quitting (#563) (#572)
* Better environment layer pathing [MTT-2412], [MTT-2830], [MTT-1990] (#576)
* Simpler quitting flow (#579)
* Adjusted layer collision matrix, removing collision with column pieces (#541)
* Arrow not sending a clientrpc when despawned, vfx get reset on spawn/despawn (#557) (#560)
* Client writing to netvar error (#468)
* Darker Eyebrows for Player Characters (#555)
* Decoupling SessionManager from NetworkManager [MTT-2603] (#581)
* Filled in some holes in the floor (#527)
* Leavebeforequit coroutine not starting (#589)
* Mage FX graphics not offset (#571) (#575)
* NetworkAnimator being called on clients (#512) (#529)
* NREs when trying to quit (#516) (#531)
* OnNetworkDespawn added to ServerProjectileLogic to prevent unspawned RPCs (#593)
* Refactor of click target feedback (#569)
* Same player number for multiple players in second playthrough (#490) (#498)
* Setting to 120hz refresh rate so it doesn't influence RTT too much, but still shaves off about 1/3 of CPU usage when running 2 builds on the same machine (#573)
* Simplified Floor Tiles (#507)
* Small CPU performance improvements (#590)
* Switched out the shadow mesh from a plane to a quad :) also turned off collision and shadow casting! (#586)
* Type and rtt text is now TextMeshPro and sized properly (#559) (#561)
* Unready button not clickable on wide screens (#552)
* Updating only navmesh generated (#542) (#546)
* Username changing before game (#499) (#530)
* VFX Optimization Pass (#584)
* Blocking pass (#578)
* React animation on trample attack while fainted (#478)

### Known issues:
* Dead imps spawn as if still alive for clients who join late
* Wrong anticipation animations played on client
* When reconnecting, the player has a big teleport to it's old position
* Proper-Fix: Retrieve Lobby list hit the rate limit. Will try again soon... error spam
* improve auth flow, it's not clear at the moment how to run two players locally
* Disconnecting wifi client side couldn’t reconnect
* "Service Error - Cannot resolve destination host" when attempting to reconnect
* Received "No client guid found mapped to the given client ID" when leaving game
* Remove workaround for wait to disconnect
* NotServerException: Destroy a spawned NetworkObject on a non-host client is not valid. Call Destroy or Despawn on the server/host instead - thrown when a host joins Lobby as a client
* When start host fails, we shouldn't go to char select screen
* heal not consistent, sometimes will not see health bar move
* additive scene loading produces some lag in editor
* Bringing up cheats screen on touchscreens should not invoke any game-side actions
* Idling in Lobby Join window causes a curl error from Lobby Services
* PlayerGraphics_Rogue_Boy(Clone) doesn't have enough AudioSources to loop all desired sound effects. (Have 2, need at least 1 more) spams console
* Android UI/Click issues
* Fix environment material on lowest setting for android
* [Connection] UI feedback on hosting and joining
* Emote drawer is not opened when 4 is pressed on keyboard
* Controls modal does not scale with the window
* Walk is triggered when trying to hit emotes on mobile
* Late join clients have green names

## [1.0.2-pre] - 2022-02-09
### Changes
* New session manager to allow reconnection (#361)
* Min spec Android setup for Boss Room (#429)
* Additive scene loading (#425)
* Cancel during join or host (#428)
* Disconnect/quit button (#432)
* Boss Room actions are all now faster with game design tweaks (#436)
* Update to NGO 1.0.0-pre.5 (#456)
* Adding on screen warning when using simulated latency (#412)
* Button to copy room name to clipboard (#431)
* Character run smoke particle optimization (#426)
* Cheat window (#444)
* God mode cheat (#430)
* Player hierarchy QoL updates, anim event listener added for player anim events (#411)

### Dependencies
* “com.unity.services.relay”: “1.0.1-pre.4",
* “com.unity.netcode.gameobjects”: “1.0.0-pre.5",
* “com.unity.netcode.adapter.utp”: “1.0.0-pre.5",
* “com.unity.multiplayer.tools”: “1.0.0-pre.2",
* “com.unity.transport”: “1.0.0-pre.12",
* Photon Realtime v4.1.6.0
* Editor is 2020.3.27f1

### Fixes
* Added transition between “Action - Boss Trample” and “(Trample Stop)” (#437)
* Cannot host a game after having previously played as a client (#457)
* Cheat window toggling on mobile
* Hitpoint depletion inconsistency with clamped values (#454)
* Host name not showing (#442)
* Improving UI during connection flow (#415)
* Join code not displayed for clients using Unity Relay (#433)
* Re-enabling positional smoothing on graphics GameObject (#417)
* Removing workaround for NGO issue 745 (#419)
* Smooth host movement cleanup (#439)
* Reduced default master volume value (#445)
* Removed room name textbox when using IP-based connection (#418)
* Removed the !IsConnectedClient condition for handling client disconnect (#435)
* Replaced instance of PostGameState prefab inside PostGame scene to fix malformed GlobalObjectIdHash (#443)
* Fix/randomly rotate breakable pots #472
* Deleted an overlapping object #471
* Fix: Background missing in Post Game scene #470
* Fixed a broken material ref on boss pillars #469
* Dotnet format over solution based on project’s .editorconfig file (#465)
* Deleted a pot that was overlapping with a torch in the transition area prefab (#471)
* Rotated breakable pots in random directions so they don’t look so copy pasted (#472)

### Chore
* Updated Relay to latest (#474)
* Updated IET package version to 2.1.1 (#422)
* Bumped Unity LTS version (#396)

### Known Issues
* NetworkTransform overflow exception
* Successive game sessions may cause a duplication of in-scene placed NetworkObjects
* Photon get increasing RTT when too much bandwidth
* When joining w/ photon relay, text should display “”Join code”" and not “”IP host”"
* Client needs a reboot to join a match after a previous game using photon
* Have better error message when getting rate limited by Relay
* Possible to get phantom players in Boss Room, we were 6 players, but got 8 players in the char select (hard to repro)
* In-scene placed imps not spawning on clients when late-joining with multiple additive scenes loaded
* Additive scene loading produces a CPU spike
* Rare client can’t write to NetVar error with additive scene loading and late join
* High latency (1 sec RTT) will produce null ref after char select
* When start host fails, we shouldn’t go to char select screen
* Dead imps spawn as if still alive for clients who join late
* When reconnecting, the player has a big teleport to it’s old position
* Rogue can teleport to top of column
* Bringing up cheats screen on touchscreens should not invoke any game-side actions
* Join with Unity Relay with empty join code throws exception
* heal not consistent, sometimes will not see health bar move
* Null ref in UnityEngine.Camera.WorldToScreenPoint (and other?)
* Wrong anticipation animations played on client
* With high lag, camera jitter was observed when rotating
* While running, character plays hit react animation but doesn’t get HP decrease
* Game can be initiated while a second player is connecting to host in CharSelect
* Rogue looks like it’s still dashing after teleport
* Top of door shouldn’t be clickable/traversable

## [1.0.1-pre] - 2021-11-18

### Changes

* Updated to NGO-pre.3 (#400)
* Updated Photon version (#402)
* Ported the game to URP (#371)
* Updated to Unity 2020.3.21f1 (#396)
* Updated Boss Room banner image (#393)
* Small update to platform support on the readme (#391)
* Replace MLAPI references with NGO (#410)
* Adding utilities asmdef. (#404)
* Visual improvements:
    * Touched up the character select background prefab (#416)
    * A matte around the border of the map to block the camera from seeing lava, edges of meshes, particles, etc
    * Lighting changes and optimizations
    * Reduced number of realtime lights to 4 (from 27)
    * Baked 3x1024 and 1x128 lightmaps (about 4MB), these are for area lights to make the lava glow more convincing
    * Added little blob shadows to characters (a plane with a transparent soft black circle attached to the physics object in the character prefab)
    * Changed the lights for the enemy spawners from point lights to spot lights (still not shadow casting)
    * Rotated all of the little pots to be in different directions so they don't look so copy-pasted
    * Tightened radii of the torch lights and reduced the amount of realtime lights for each torch from 2 to 1
    * Also added a couple of extra torches
    * Upped the contrast and adjusted the ambient lighting of the scene to be a more saturated blue

### Fixes

* Fixed pathfinding issues with doors (#407)
* NGO-pre.3  asset fixes for NetworkAnimator (#400)
* Invincibility counter reset on cancel, animator invincibility parameter reset (#409)
* Changed ready button text to UNREADY when player is locked in (#413)
* Fixed arrows despawning error while switching scenes (#406)
* Fixed a bug involving the NetworkObjectPool's GlobalObjectIdHash (#405)
* Fixed missing Boss Room title in the main menu screen for mac builds (same fix as before, but this time for the URP version of the main menu) (#403)
* NGO-pre.3 asset fixes for NetworkAnimator (#400)
* Fixed arrows not spawning on second playthrough on client side (#394)

## [1.0.0-pre] - 2021-10-21

v1.0.0-pre is a pre-release release for Multiplayer Samples Co-op.

It requires and supports Unity v2020.3 LTS and Netcode for GameObjects (Netcode) v1.0.0-pre. For additional information on Netcode, see the changelog and release notes for the Unity Netcode package.

### New features

* Player persistence hierarchical modifications to Netcode's Player Prefab https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/pull/342, https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/pull/349, &
& https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/pull/356
* Radio button introduced for main menu UX improvements (default is still IP) https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/pull/345
* Archer's base attack & charged shot 1-3 replicated via `NetworkTransform` https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/pull/353
* `NetworkTransform` handles players' movement syncing https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/pull/354
* Adding ParrelSync and updating third party notice.md https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/pull/357
* `NetworkAnimator` added to boss door & floor switch with server-authority https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/pull/360
* Integrating Unity Transport and Relay into Boss Room https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/pull/366
* Updated image in the UnityLogo prefab with the new Unity logo https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/pull/374
* NetworkObject pool (arrows are pooled) https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/pull/362
* Server-authoritative character `NetworkAnimator` https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/pull/367

### Changes

* Collider and Layer cleanup & optimizations https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/pull/341
* Photon Transport send queue batch size incremented to 8192 https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/pull/350
* OnNetworkSpawn() refactoring, player prefab removed from NetworkManager prefab list https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/pull/352
* Netcode rebranding https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/pull/368
* Added link to bitesize samples https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/pull/370
* Update compatible Unity version in Readme https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/pull/376
* Renaming connection methods in main menu https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/pull/382
* Updated Animations to use an additional anticipation animation, to properly work with NetworkAnimator. https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/pull/383
* Updated actions to latest NetworkAnimator APIs https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/pull/383

### Fixes

This release includes the following fixes:

* A Netcode soft sync error on cleanup between scene transitions previously broke the game. For example imps did not spawn and pots were intangible.
* Sometimes after completing a match and the host starts a new match from the Victory or Loss screen, connected players had no visible interactions to join or select characters.
* Sometimes the client may have been disconnected from Photon which caused a timeout and `PhotonRealtimeTransport` to be in a bad state after the shutdown. An exception developed that fired every frame.

### Known issues

* The game can be initiated while a second player is connecting to the host in `CharSelect`. Players may join without selected characters spawning and in an unresponsive state.
* The game may not transition completely into the game scene past the character select screen on lower-end Android devices.
* This version currently includes a patched NetworkAnimator. This will be reverted back once this patch goes live in Netcode for GameObjects


## [0.2.1] - 2021-05-27

### Fixes

This release includes the following issue fixes:
- Fixing parameter exception caused by old MLAPI version. Reverting until package is updated.


## [0.2.0] - 2021-05-19

v0.2.0 is an Early Access release for Multiplayer Samples Co-op.

It requires and supports Unity v2020.3.8f1 LTS and Unity MLAPI v0.1.0. For additional information on MLAPI, see the changelog and release notes for the Unity MLAPI package.

### New features

* Introduced static scene `NetworkObject`s to to BossRoom scene including the following updates:

    * Implemented a `ScriptableObject` based event system to encapsulate events inside assets. These objects include a `GameEvent` (ScriptableObject) and `GameEventListener` (MonoBehaviour) to encapsulate events inside assets, located in the `ServerBossRoomState` prefab which now has a `GameEventListener` component. The event associated to this listener is `BossDefeated`, which the Boss raises when the `LifeState` is Dead in the `RaiseEventOnLifeChange` component.
    * Added two separator `GameObject`s for scene readability: runtime `NetworkObject`s and `NetworkObject`s already placed in the scene.
    * Added a custom editor for GameEvents to fire in the editor (greatly enhances testing).
    * The `LifeState` NetworkVariable was moved from `NetworkCharacterState` into its own component, `NetworkLifeState`.
    * Cleaned up and removed old spawn prefab collections and spawner scripts (`NetSpawnPoint`).

* Added ramp-up animation for hero movement and actions
* Added F/X and animation assets for the game including:

  * Audio files for boss sound effects
  * Visual effects and particles for Tank charge skill 
  * Art assets to wave spawner, including animations for ReceiveDamage, Broken (died), and Revive 

* Added Boss fight theme.
* Updated and added various hero abilities:

  * Added a cooldown to Archer's PowerShot. 
  * Added the Rogue's Dagger and Dash skills. The dash skill shows an instinct teleport (using an RPC) instead of a charge like the boss' (which updates its position over time). 
  * Added the Rogue's Sneak skill using local stealth, applying a graphical effect to the stealthy character while still sending all network events normally.
  * Properly display Heal abilities when targeting a fallen ally character. 
  * Character attack actions properly support Hold to charge options. 

* To show how UI elements and game objects can be networked, added networked functionality using `INetworkSerializable` in the `CharSelect` screen to network player's selected character on the Character Selection screen. 
* Added input sanitization to remove any invisible characters introduced by other chat programs when copy-pasting room names, IP addresses, or ports. (Useful when sharing with friends.) 
* Added healthbars to display when damaged and properly track imp health locally and across clients. 

### Changes

* The Boss Room project now loads MLAPI 0.1.0-experimental package through the Unity Package Manager Registry. See the [MLAPI install guide](https://docs-multiplayer.unity3d.com/docs/migration/install) for details.
* Updated the user interface including the following:

  * When joining a game, a "Connecting..." UI loads. When disconnecting from a game, you are returned to the MainMenuScene with a "Connection to Host lost" message. If the game fails to connect, a general message "Connection to Host failed" loads. 
  * Added an option to leave the lobby after joining it. You no longer have to restart the game to leave the lobby.
  * Added option to cancel my selection after clicking **Ready**. 
  * Added a gear icon for accessing and modifying app settings. 
  * The UI now tracks and displays player has arrived, number of connected players in the lobby, and player status. 

* Updated Boss Room per asset and project settings for increased performance and better resource management:

  * Disabled GPU Skinning to optimize GPU usage and FPS.
  * Lowered quality of ambient occlusion from high to medium.
  * Switched SMAA High to FXAA (fast mode) reducing GPU cost.
  * Modified GPU Instancing on imps, heroes, and the boss to significantly reduce the number of draw calls.
  * Turned off Cast Shadows on Imp and Imp Boss.
  * Disabled mesh colliders of lava, which is more decorative than interactive.
  * Refactored the S_SimpleDissolve shader which consumed most import time.

* Added disconnection error message to be displayed when a player or host disconnects. Client logic was also updated to detect Host disconnection scenarios, such as losting connectivity.
* Balanced hero and enemy stats, spawners, trigger areas, and enemy detetction areas.
* Removed a duplicated `GameObject` from the MainMenu scene. 
* Reviewed and revised code to better following quality standards.
* Updated the Mage character base attack to better support the new enqueuing ability and handle game behaviors. Updates include:

  * Actions being non-blocking now allow other actions while a mage-bolt is in flight
  * Actions send in `ActionSequence` groups to better handle actions when players spam click enemies
  * Timing issues with animations, actions, and character location
  * Bolt animation for legitimate hits
  * Updated attacks not to knock rogues out of stealth 

* Updated character attacks to not cause friendly-fire damage between players. 
* Updated and resolved issues with 3D models including polygon count of coins and chest, artifacts where level graphics are stitched together or overlapping characters, and asset map consistency used by objects (color + normal map + emission).
* Merged `ConnectStatus` and `DisconnectReason` into a single `ConnectStatus`. 
* Updated `ServerGameNetPortal` to properly handle the following:

  * Per-connection state on client disconnect
  * Additional errors including a full server and player ID (GUID) already playing

* Refactored Action Bar code including the following:

  * Removed the `ButtonID` from `UIHudButton`.
  * Removed hard-coded values from `HeroActionBar`.
  * Removed switch statements.
  * Completed minor code cleanup.
  * Added verification to only show skill and ability buttons for available character abilities. Empty buttons no longer load for characters.

* Added a call to warm up shaders when the project starts to ensure animations issues do not occur.
* Removed collision from objects that have a Broken (dead) state.
* Implemented a better cooldown solution and calculations for tracking and managing character, imp, and boss actions. 
* Updated event registration and unregistration code to be symmetrical across the project.

### Fixes

This release includes the following issue fixes:

* Fixed an issue where any player that plays the game and then returns to the Main Menu may be unable to Start or Join properly again, requiring you to restart the client.
* Updates and refinements in Boss Room resolved an issue that occurred in some degraded network conditions that caused a replicated entity on a client to vanish from that client, creating the effect of being assailed by an invisible enemy. 
* Fixed displayed graphical affects for casting and blocking a Bolt to correctly match the caster and target, and properly stops animations for cancelled actions across clients. 
* Fixed a rare exception when the Tank character uses her Shield Aura ability to intercept the Boss charge attack. 
* Fixed an issue returning all clients to the Main Menu when the host leaves the Boss Room. 
* Green quads no longer show on impact when the Archer arrow strikes enemies. 
* Fixed issue to correctly allow one player to receive a character when two players in the Character Select click **Ready** for the same hero at the same time. Character Select is no longer blocked.  
* Fixed an issue with boss collisions with a Pillar correctly applying a stun effect and shattering the pillar when using the Trample attack. 
* Fixed the lobby welcome messages to correctly list the player names, including a previous issues for P1 and P2. 
* On Windows, investigated and fixed issues with visible effects for character actions including Mage Freeze attack.
* On Windows, fixed issue with imp spawners not respawning new imps after exploring the room.
* Fixed an issue where the door state does not reflect the existing state when players connect late to a game, for example if other players open the door and a player joins late the door displays as closed. 
* Removed a previous work-around for character selections when host replays a completed game. The issue was resolved, allowing players to see character selections during replay. 
* Fixed collision wall settings, fixing an issues where the boss knock-back ability sent players through walls.
* Resolved an issue where any players leaving the lobby sent all players to the lobby.
* Fixed the ignored health amount (HP parameter) for revived characters. The correct value correctly sets the revived character to a lower amount than maximum. 
* Fixed animations for enemies including the smoke animation for destroyed imps and the boss helmet when crying.
* Fixed loading of the game skybox before the menu loaded.

### Known issues

* An MLAPI soft sync error on cleanup between scene transitions may break the game, for example imps do not spawn and pots are intangible.
* The game can be initiated while a second player is connecting to the host in `CharSelect`. Players may join without selected characters spawning and in an unresponsive state.
* Sometimes after completing a match and the host starts a new match from the Victory or Loss screen, connected players may have no visible interactions to join or select characters. A work-around is implemented to not block entry into the game.
* Sometimes the client may be disconnected from Photon which causes a timeout and `PhotonRealtimeTransport` to be in a bad state after the shutdown. An exception is developed that fires every frame.  


## [0.1.2] - 2021-04-23

v0.1.2 is a hotfix for an Early Access release for Boss Room: Small Scale Co-op Sample.

### Updates

* License updated to [Unity Companion License (UCL)](https://unity3d.com/legal/licenses/unity_companion_license) for Unity-dependent projects. See LICENSE in package for details.
* The GitHub repository `master` branch has been renamed to `main`. If you have local clones of the repository, you may need to perform the following steps or reclone the repo:

```
# Switch to the "master" branch:
$ git checkout master
# Rename it to "main":
$ git branch -m master main
# Get the latest commits (and branches!) from the remote:
$ git fetch
# Remove the existing tracking connection with "origin/master":
$ git branch --unset-upstream
# Create a new tracking connection with the new "origin/main" branch:
$ git branch -u origin/main
```

## [0.1.1] - 2021-04-09

### Fix
Adding missing 3rd party contributors file
Fixing wrong code/comment about NetworkVariableBool not working (that's not the case!)

## [0.1.0] - 2021-04-07

v0.1.0 is an Early Access release for Multiplayer Samples Co-op.

It requires and supports Unity v2020.3 and later and Unity MLAPI v0.1.0. For additional information on MLAPI, see the changelog and release notes for the Unity MLAPI package.

### New features

Boss Room is a small-scale cooperative game sample project built on top of the new experimental netcode library. The release provides example code, assets, and integrations to explore the concepts and patterns behind a multiplayer game flow. It supports up to 8 players for testing multiplayer functionality.

* See the README for installation instructions, available in the downloaded [release](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/releases/latest) and [GitHub](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop).
* Learn with Unity using the Boss Room source code, project files, and assets which include: 1 populated dungeon level, 4 character classes with 2 genders, combatant imps and boss, and a simple collaborative puzzle.
* Test co-op games by running multiple instances of the game locally or connecting to a friend over the internet.

### Known issues

* Sometimes when the host leaves the Boss Room, not all clients will return to the Main Menu, but will remain stuck in the Boss Room scene. 
* Sometimes after completing a match and the host starts a new match from the Victory or Loss screen, connected players may have no visible interactions to join or select characters.
* A player may encounter a rare exception when the Tank character uses her Shield Aura ability. This issue may be due to intercepting the Boss charge attack.
* If two players in the Character Select **Ready** for the same hero at the same time, the UI will update to *Readied* on both clients, but only one will have actually selected the hero on the Host. This issue blocks Character Select from proceeding.
* Any player that plays the game and then returns to the Main Menu may be unable to Start or Join properly again, requiring you to restart the client.
* Green quads may show on impact when the Archer arrow strikes enemies. This issue may only occur in the editor.
* You may encounter a game error due to a Unity MLAPI exception. Not all MLAPI exceptions are fully exposed few informing users.
* The Photon Transport currently generates some errors in the Player log related to the `PhotonCryptoPlugin`.
* The welcome player message in the lobby indicates P2 (player 2) regardless of your generated name. Currently the Character Select scene displays “Player1” and “P1” in two locations, where it is intended that the user’s name be displayed.  
* The spawner portal does not work in this release.
* Players may not reliably play another match when selecting **Return to Main Menu** during the post-game scene. This may be due to states not properly clearing.
* Some actions may feel unresponsive and require action anticipation animations.
* In some degraded network conditions, a replicated entity on a client can vanish from that client, creating the effect of being assailed by an invisible enemy.
* Boss collisions with a Pillar may not correctly apply a stun effect and shatter the pillar when using the Trample attack. 
* The displayed graphical affects for casting and blocking a Bolt do not correctly match the caster and target. 
* Some areas of the Boss Room require updates to geometry seams and collisions, for short walls and lava pits.
