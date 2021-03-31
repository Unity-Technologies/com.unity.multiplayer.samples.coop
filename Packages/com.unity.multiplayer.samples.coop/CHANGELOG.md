# Multiplayer Samples Co-op Changelog

## [0.1.0] - 2021-04-07

v0.1.0 is the first release for Multiplayer Samples Co-op.

Requires and supports Unity v2020LTS and Unity MLAPI v0.1.0.

### New features

The Multiplayer Samples Co-op release provides Boss Room, a small-scale cooperative game sample project built on top of the new experimental netcode library. The release provides example code, assets, and integrations to explore the concepts and patterns behind a multiplayer game flow. It supports up to 8 players for testing multiplayer functionality.

* See the README for installation instructions, available in the downloaded release and [GitHub](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop).
* Learn with Unity using the Boss Room source code, project files, and assets which include: 1 populated dungeon level, 4 character classes with 2 genders, combatant imps and boss, and a simple collaborative puzzle.
* Test co-op games by running multiple instances of the game locally or connecting to a friend over the internet.

### Known issues

* After a round of Boss Room completes and a party member returns to the menu, it pulls all players and leader to menu.
* The welcome player message in the lobby indicates P2 (player 2) regardless of your player number.
* When multiple players select the same character and click **Ready** at the same time, it defaults to host and prevents the game from starting.
