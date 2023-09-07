# Multiplayer Samples Co-op Changelog

## [1.7.0] - 2023-09-07

### Changed
* Updating package dependencies (#869)
  *  Tutorial Framework upgraded to v3.1.3
  *  Netcode for GameObjects upgraded to v1.6.0 
  *  Unity Relay upgraded to v1.0.5

### Changed
* Replaced usages of null-coalescing and null-conditional operators with regular null checks. (#867) These operators can cause issues when used with types inheriting UnityEngine.Object because that type redefines the == operator to define when an object is null. This redefinition applies to regular null checks (if foo == null) but not to those operators, thus this could lead to unexpected behaviour. While those operators were safely used within Boss Room, only with types that were not inheriting UnityEngine.Object, we decided to remove most usages for consistency. This will also help avoid accidental mistakes, such as a user reusing a part of this code, but modifying it so that one of those operators are used with a UnityEngine.Object.

### Fixed
* Clarified fix used to handle clients reconnecting to the server while keeping the same active scene and having additional scenes additively loaded (#864)
* Host loading progress bar is now properly updated on all clients during scene loads (#862)

## [1.6.1] - 2023-06-14

### Fixed
* Updating package dependency to Netcode for GameObjects version 1.4.0 (#839)

## [1.6.0] - 2023-04-27

### Changed
* Removed need for SceneLoaderWrapper.AddOnSceneEventCallback (#830). The OnServerStarted and OnClientStarted callbacks available in NGO 1.4.0 allows us to remove the need for an external method to initialize the SceneLoaderWrapper after starting a NetworkingSession.

## [1.5.1] - 2022-12-13
### Changed
* Bumped RNSM to 1.1.0: Switched x axis units to seconds instead of frames now that it's available. This means adjusting the sample count to a lower value as well to 30 seconds, since the x axis was moving too slowly. (#788)

## [1.5.0] - 2022-12-05

### Changed
* ClientNetworkAnimator component has been added to the Samples Utilities Package. This allows for authority on Animators to be passed onto clients, meaning animations will be client-driven.

## [1.4.1] - 2022-10-25

### Changed
* ClientLoadingScreen now sets raycast blocking to true when the loading screen is visible (#760)

### Removed
* Deprecated Unity Relay Utilities, it should no longer be needed with NGO 1.1.0's new API for setting up Relay (#708)

## [1.4.0] - 2022-10-06

### Added
* Added custom RNSM config with graph for RTT instead of single value (#747)

## [1.3.1-pre] - 2022-09-13

### Fixed
* Updating ClientNetworkTransform for latest NGO 1.0.1+ (#726) This fixes the new behaviour where both RPCs and netvars were sending the same data. Now CNT is netvar based only, making it send on tick just like NetworkTransform and other network variables would.

## [1.3.0-pre] - 2022-06-23

### Added
* feat: other players loading progress in loading screen [MTT-2239] (#580)
* feat: adding editor child scene loader for composed scenes (#653)
* Added an assembly definition for the RNSM utilities so that they are only compiled if using Unity version 2021.2 or newer

### Changed
*
### Removed
*
### Fixed
* Fixed breaking change from NetworkTransform in ClientNetworkTransform
## [1.2.0-pre] - 2022-04-28
### Added
* Client network transform move to samples [MTT-3406] (#629) --> You can now use Boss Room's Utilities package to import ClientNetworkTransform using this line in your manifest file     

## [1.1.1-pre] - 2022-04-13
### Added
* Loading screen (#427) Added SceneLoaderWrapper to control the loading screen

## [1.1.0-pre] - 2022-04-07
### Changes
* Using DTLS for relay (#485)
* Replacing guid with auth playerid for session management (#488)
* fix: leaving lobby when disconnecting (#515) (#553)
feat: flag to prevent dev and release builds to connect (#482)

### Fixes:
fix: decoupling SessionManager from NetworkManager [MTT-2603] (#581)

## [1.0.2-pre] - 2022-02-09
### Added
* Additive scene loading (#425)
* feat: move session manager and relay utilities to utilities package (#438)
