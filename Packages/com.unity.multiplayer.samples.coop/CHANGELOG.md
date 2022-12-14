# Multiplayer Samples Co-op Changelog

## [unreleased] - yyyy-mm-dd

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
