# Multiplayer Samples Co-op Changelog

## [0.2.2] - 2021-06-02

v0.2.2 is a hotfix for an Early Access release for Boss Room: Small Scale Co-op Sample.

### Fixes

* Updated the Boss Room sample to use the updated MLAPI registry version 0.1.1.

## [0.2.1] - 2021-05-27

v0.2.1 is a hotfix for an Early Access release for Boss Room: Small Scale Co-op Sample.

### Fixes

* [GitHub 343](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/pull/343) - Fixed parameter exception when connecting to lobby caused by an old MLAPI version. This fix reverts the change until the package is updated.

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

