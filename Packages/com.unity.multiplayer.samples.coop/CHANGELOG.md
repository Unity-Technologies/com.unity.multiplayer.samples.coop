# Multiplayer Samples Co-op Changelog

## [Unreleased] - yyyy-mm-dd

### Added
feat: other players loading progress in loading screen [MTT-2239] (#580)

### Changed

### Removed

### Fixed

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
