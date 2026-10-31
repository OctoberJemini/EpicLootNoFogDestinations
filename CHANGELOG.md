# Changelog

## 0.3.0 — OctoberJemini maintained fork

- Replaced the fixed Epic Loot version allowlist with guarded checks of required types, methods, fields, enum values and placement-hook parameter names.
- Compatible older and newer Epic Loot releases can load without a version-only rebuild. API checks pass on 13 cached releases from 0.14.4 through 0.15.4; 0.13.3 is rejected because required placement APIs are missing.
- Retained the inactive warning and cleanup of this add-on's patches if installation fails.
- Preserved explored-centre placement, personal/shared exploration, purchase-time validation, existing quest locations and rewards.
- Added the OCT map-and-mist icon, updated installation/compatibility documentation, and retained Ken Jorissen's MIT attribution.

## Earlier local builds

The 0.2.x local maintenance builds updated Epic Loot version checks through 0.15.4, refined explored-centre selection and added compatibility warnings. This changelog describes this fork, not the upstream project's release history.
