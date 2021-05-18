# Multiplayer Samples Co-op Changelog

## [0.2.0] - 2021-05-18

v0.2.0 is an Early Access release for Multiplayer Samples Co-op.

It requires and supports Unity v2020.3 and later and Unity MLAPI v0.1.0. For additional information on MLAPI, see the changelog and release notes for the Unity MLAPI package.

### New features

* Updated the user interface including the following:

  * When joining a game, a "Connecting..." UI loads. When disconnecting from a game, you are returned to the MainMenuScene with a "Connection to Host lost" message. If the game fails to connect, a general message "Connection to Host failed" loads. 
  * Added an option to leave the lobby after joining it. You no longer have to restart the game to leave the lobby.
  * Added option to cancel the game after clicking **Ready**. 
  * Added a gear icon for accessing and modifying app settings. 
  * The UI now tracks and displays player has arrived, number of connected players in the lobby, and player status. 

* Added F/X and animation assets for the game including:

  * Audio files for boss sound effects
  * Ramp-up animation for hero movement and actions
  * Visual effects and particles for Tank charge skill 
  * Art assets to wave spawner, including animations for ReceiveDamage, Broken (died), and Revive 
  * Game music 

* Added audiomixer to Boss Room for separate audio channels and a master mixer. 

* Updated and added various hero abilities:

  * Added a cooldown to Archer's PowerShot. 
  * Used generic actions to implement Rogue's Dagger skill. 
  * Used generic actions to implement Rogue's Sneak skill using local stealth, applying a graphical effect to the stealthy character while still sending all network events normally. 
  * Used generic actions to implement Rogue's Dash skill. 
  * Properly display Heal abilities when targeting a fallen ally character. 
  * Character attack actions properly support Hold to charge options. 

* To show how UI elements and game objects can be networked, added networked functionality using `INetworkSerializable` for UI elements on the Character Selection screen including networked mouse cursors. 
* Added a Photon filter for host room names not to allow profanity, racial slurs, and additional socially acceptable words. 
* Boss Room now uses the [UnityToonShader](https://github.com/IronWarrior/UnityToonShader) for rendering the 3D surfaces to emulate 2D, flat surfaces.
* Added disconnection error message to load when a player or host disconnects due to limited or no network connectivity. Client logic was also updated to detect Host disconnection scenarios, such as losting connectivity.
* Balanced hero and enemy stats, spawners, trigger areas, and enemy detetction areas. 
* Added healthbars to display when damaged and properly track imp health locally and across clients. 

### Changes

* Updated the Photon Setup Guide, indicating you need only app ID when playing with friends. For users connecting across regions, you may need to hard code a region in your app settings by using the room code and region instead of just the room code sharing in game. 
* Removed Singleton usage, allowing multiple instances of MLAPI networking stack to start up in the same process. 
* Removed a duplicated `GameObject` from the MainMenu scene. 
* Reviewed and revised code to better following quality standards.
* Updated the mage character base attack to better support the new enqueuing ability and handle game behaviors. Updates include:

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
  * Player connection if they experience a game-level connection failure

* Updated code to allow hosts to specify a port to listen to, removing the hard-coded port. 
* Refactored Action Bar code including the following:

  * Removed the `ButtonID` from `UIHudButton`
  * Removed hard-coded values from `HeroActionBar`
  * Removed switch statements
  * Completed minor code cleanup
  * Verify and only show skill and ability buttons for character abilities. Empty buttons no longer load for characters.

* Added a call to warm up shaders when the project starts to ensure animations issues do not occur.
* Removed collision from objects that have a Broken (dead) state.
* Implemented a better cooldown solution and calculations for tracking and managing character, imp, and boss actions. 
* Fixed the ignored health amount (HP parameter) for revived characters. The correct value correctly sets the revived character to a lower amount than maximum. 

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
* On Windows, investigated and fixed issues with visible effects for character actions including Mage Freeze attack and Rogue Dash.
* On Wizards, fixed issue with imp spawners not respawning new imps after exploring the room.
* Fixed an issue where the door state does not reflect the existing state when players connect late to a game, for example if other players open the door and a player joins late the door displays as closed. 
* Removed a previous work-around for character selections when host replays a completed game. The issue was resolved, allowing players to see character selections during replay. 
* Fixed collision wall settings, fixing an issues where the boss knock-back ability sent players through walls.
* Resolved an issue where any players leaving the lobby sent all players to the lobby.
* Fixed animations for enemies including the smoke animation for destroyed imps and the boss helmet when crying.

### Known issues

* An MLAPI soft sync error on cleanup between scene transitions may break the game, for example imps do not spawn and pots are intangible.
* Sometimes after completing a match and the host starts a new match from the Victory or Loss screen, connected players may have no visible interactions to join or select characters. A work-around is implemented to not block entry into the game. 

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

