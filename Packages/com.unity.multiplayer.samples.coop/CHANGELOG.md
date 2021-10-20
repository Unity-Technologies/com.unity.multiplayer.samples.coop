# Multiplayer Samples Co-op Changelog

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

v0.2.1 is a hotfix for an Early Access release for Boss Room: Small Scale Co-op Sample.

### Fixes

* [GitHub 343](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/pull/343) - Fixed parameter exception when connecting to lobby caused by an old MLAPI version. This fix reverts the change until the package is updated.

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

v0.1.1 is a hotfix for an Early Access release for Boss Room: Small Scale Co-op Sample.

### Updates

* Added [Third Party Contributors](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/master/third-party%20contributors.md) file listing external partner contributors.
* Refactored `IsStealthy` from `NetworkVariableByte` to `NetworkVariableBool` to indicate state.

## [0.1.0] - 2021-04-07

v0.1.0 is an Early Access release for Multiplayer Samples Co-op.

It requires and supports Unity 2020.3.8f1 LTS and Unity MLAPI v0.1.0. For additional information on MLAPI, see the changelog and release notes for the Unity MLAPI package.

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
