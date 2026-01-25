# Changelog

All notable changes to the Orca project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] - 2024-11-27

### Added
- **Rebranded to Orca** - Complete rename from D365Client to Orca
- **Capitalized API** - All namespaces now capitalized (Service, Utility, Notification, Log)
- **LogWrapper Class** - New organized logging namespace with `orca.Log.Info()`, `orca.Log.Warn()`, etc.
- **Service namespace** - Renamed from `webApi` for better semantics
- **Verbose log level** - Renamed from DEBUG for industry standard terminology
- **Entity-based configuration** - Load settings from `new_cmaconfiguration` entity
- **Remote logging** - Automatic error tracking to `new_cmajavascriptlog` table
- **User context capture** - Automatic logging of user ID, name, email, URL, and browser
- **Smart batching** - Intelligent log batching for efficient API usage
- **TypeScript definitions** - Full type support included
- **Comprehensive documentation** - Quick start, API reference, migration guide
- **Migration tools** - Complete guide for migrating from D365Client

### Changed
- `d365.webApi` → `orca.Service`
- `d365.utility` → `orca.Utility`
- `d365.notification` → `orca.Notification`
- `d365.info()` → `orca.Log.Info()`
- `d365.warn()` → `orca.Log.Warn()`
- `d365.error()` → `orca.Log.Error()`
- `d365.debug()` → `orca.Log.Verbose()`
- `LogLevel.DEBUG` → `LogLevel.VERBOSE`

### Features
- ✨ Clean, intuitive API design
- 🗄️ Remote logging with full user context
- ⚙️ Entity-based configuration management
- 📊 Configurable log levels (NONE, ERROR, WARN, INFO, VERBOSE)
- 🔍 Automatic user and browser context capture
- 🚀 Latest D365 API version (no hardcoded versions)
- 📝 Comprehensive error tracking and monitoring
- ✅ Production-ready with enterprise features

### Backward Compatibility
- Full backward compatibility maintained
- D365Client can run alongside Orca
- Same configuration entities (`new_cmaconfiguration`)
- Same logging table (`new_cmajavascriptlog`)

## [1.0.0] - 2024-11-27

### Added
- Initial release as D365Client
- WebAPI wrapper for CRUD operations
- Utility wrapper for Xrm.Utility functions
- Notification system for forms and fields
- Basic logging with console output
- In-memory log history
- Error handling and tracking

### Features
- WebAPI operations (create, retrieve, update, delete)
- Progress indicators and dialogs
- Form and field notifications
- Configurable log levels
- Log history export
- TypeScript support

---

## Migration Notes

### From 1.x to 2.0

The migration from D365Client 1.x to Orca 2.0 is straightforward:

1. Replace library file: `D365Client.js` → `Orca.js`
2. Find and replace in your codebase:
   - `D365Client` → `Orca`
   - `.webApi` → `.Service`
   - `.utility` → `.Utility`
   - `.notification` → `.Notification`
   - `.info(` → `.Log.Info(`
   - `.warn(` → `.Log.Warn(`
   - `.error(` → `.Log.Error(`
   - `.debug(` → `.Log.Verbose(`
   - `LogLevel.DEBUG` → `LogLevel.VERBOSE`

See [Migration Guide](docs/Orca-Migration-Guide.md) for detailed instructions.

---

## Planned Features

### [2.1.0] - Future
- Enhanced caching strategies
- Performance monitoring capabilities
- Additional utility helpers
- Plugin/extension system

### [2.2.0] - Future
- Advanced analytics dashboard
- Custom reporting tools
- Integration with Power BI

---

**For complete details, see the [documentation](docs/).**
