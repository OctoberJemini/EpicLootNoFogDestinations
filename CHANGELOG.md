## 0.3.3

- Support Epic Loot 0.16.0's placement API after removal of its unused saveData parameter.
- Preserve compatibility with the older three-argument placement API and retain strict signature and parameter-name checks for both forms.
- Explored-centre selection, purchase guards, existing quest locations and rewards are unchanged.
- Validated with offline policy tests and released-assembly API checks; no in-game playtest is claimed.

## 0.3.2

- Standardized the package icon background and OCT wordmark placement to match the original OctoberJemini artwork.
- Artwork-only package update; mod DLLs and gameplay behavior are unchanged.

# Changelog

## 0.3.1
- Added prominent bug-report, feature-request and source links to the main mod description.
- Documentation-only package update; mod binaries are unchanged.

## 0.3.0 — OctoberJemini maintained fork

- Replaced the fixed Epic Loot version allowlist with guarded checks of required types, methods, fields, enum values and placement-hook parameter names.
- Compatible older and newer Epic Loot releases can load without a version-only rebuild. API checks pass on 13 cached releases from 0.14.4 through 0.15.4; 0.13.3 is rejected because required placement APIs are missing.
- Retained the inactive warning and cleanup of this add-on's patches if installation fails.
- Preserved explored-centre placement, personal/shared exploration, purchase-time validation, existing quest locations and rewards.
- Added the OCT map-and-mist icon, updated installation/compatibility documentation, and retained Ken Jorissen's MIT attribution.

## Earlier local builds

The 0.2.x local maintenance builds updated Epic Loot version checks through 0.15.4, refined explored-centre selection and added compatibility warnings. This changelog describes this fork, not the upstream project's release history.
