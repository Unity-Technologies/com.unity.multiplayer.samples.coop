# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Additional documentation and release notes are available at [Multiplayer Documentation](https://docs-multiplayer.unity3d.com).

## [Unreleased] - yyyy-mm-dd

### Added
* Added handling the OnTransportFailure callback (#707). This callback is invoked when a failure happens on the transport's side, for example if the host loses connection to the Relay service. This won't get called when the host is just listening with direct IP, this would need to be handled differently (by pinging an external service like google to test for internet connectivity for example). Boss Room now handles that callback by returning to the Offline state.
* Pickup and Drop action added to the Action system. Actionable once targeting a "Heavy"-tagged NetworkObject. (#372) - This shows NetworkObject parenting with a pattern to follow animation bones (the hands when picking up)
*
### Changed
* Updated tools, authentication and relay packages (#690)
* Replaced our dependency injection solution with VContainer. (#679)
* NetworkedMessageChannels can now be subscribed to before initiating a connection (#670)
* Refactored connection management into simpler state machine (#666)
* Merged GameState bridge classes (the ones that contained no or limited functionality) (#697) This cleans up our sometimes too verbose code split.
* 
### Removed
*
### Fixed
* Subscribing to a message channel while unsubscribing is pending (#675)
* Using ```Visible``` instead of ```Enabled``` to make sure RNSM continues updating when off (#702)

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
* New Vandal Imp and bomb throwing action: NetworkRigidbody-based toss Action, thrown by new VandalImp class [MTT-2333](#671)
  * Art and sound pass for NetworkRigidbody-based toss action [MTT-2732](#689)

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
