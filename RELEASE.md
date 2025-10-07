# Version 2.0.0

## Breaking Changes
- Removed `ratio` parameter from `DopProvider` constructor
- Removed automatic DSD conversion functionality from `DopProvider`

## What's New
- `DopProvider` now directly encapsulates DSD to DoP from source data
- Simplified API surface by removing conversion overhead
- Better performance for direct DoP operations

## Migration Guide
- Update your code to remove the `ratio` parameter from `DopProvider` calls.
- DSD conversion must now be handled separately if needed.

