# Contributing to Orca

Thank you for your interest in contributing to Orca! This document provides guidelines and instructions for contributing.

## 🤝 How to Contribute

### Reporting Bugs

Before creating bug reports, please check existing issues to avoid duplicates. When creating a bug report, include:

- **Clear title and description**
- **Steps to reproduce** the issue
- **Expected behavior** vs actual behavior
- **D365 version** and environment details
- **Browser** and version
- **Code samples** if applicable
- **Error messages** or logs

### Suggesting Enhancements

Enhancement suggestions are welcome! Please include:

- **Use case** - Why is this enhancement needed?
- **Proposed solution** - How should it work?
- **Alternatives** - Other approaches you've considered
- **Examples** - Code examples if applicable

### Pull Requests

1. **Fork** the repository
2. **Create a branch** for your feature (`git checkout -b feature/amazing-feature`)
3. **Make your changes** following our coding standards
4. **Test thoroughly** in a D365 environment
5. **Commit your changes** (`git commit -m 'Add amazing feature'`)
6. **Push to your branch** (`git push origin feature/amazing-feature`)
7. **Open a Pull Request**

## 📝 Coding Standards

### JavaScript Style

- Use **ES6+** syntax where appropriate
- Follow **camelCase** for variables and functions
- Use **PascalCase** for classes
- **Capitalize** public API namespaces (Service, Utility, Notification, Log)
- Add **JSDoc comments** for all public methods
- Keep functions **focused and small**
- Use **async/await** over promises where readable

### Example

```javascript
/**
 * Create a new record
 * @param {string} entityLogicalName - Entity logical name
 * @param {object} data - Record data
 * @returns {Promise<CreateResult>}
 */
async createRecord(entityLogicalName, data) {
    this.client.info('Service', `Creating ${entityLogicalName} record`, data);
    
    try {
        const result = await Xrm.WebApi.createRecord(entityLogicalName, data);
        this.client.info('Service', `Successfully created ${entityLogicalName}`, result);
        return result;
    } catch (error) {
        this.client.error('Service', `Failed to create ${entityLogicalName}`, error);
        throw error;
    }
}
```

### Naming Conventions

- **Classes**: `PascalCase` (e.g., `ServiceWrapper`, `LogWrapper`)
- **Public Methods**: `camelCase` (e.g., `createRecord`, `showAlert`)
- **Private Methods**: Prefix with `_` (e.g., `_loadUserContext`, `_flushRemoteLogs`)
- **Constants**: `UPPER_SNAKE_CASE` (e.g., `LOG_LEVEL`, `BATCH_SIZE`)
- **Public API**: `PascalCase` (e.g., `orca.Log.Info`, `orca.Service`)

### Error Handling

- **Always** use try-catch for async operations
- **Log errors** with appropriate log level
- **Rethrow** errors after logging
- **Provide context** in error messages

```javascript
try {
    const result = await someOperation();
    this.client.info("Component", "Operation successful");
    return result;
} catch (error) {
    this.client.error("Component", "Operation failed", error);
    throw error;
}
```

### Logging Best Practices

- Use appropriate log levels:
  - `Verbose` - Detailed diagnostic information
  - `Info` - General informational messages
  - `Warn` - Warning messages for potentially harmful situations
  - `Error` - Error messages for failures
- Include **contextual data** with logs
- Use **descriptive component names**

## 🧪 Testing

### Manual Testing Checklist

Before submitting a PR, test in a D365 environment:

- [ ] Forms load without errors
- [ ] Create operations work correctly
- [ ] Read operations return expected data
- [ ] Update operations persist changes
- [ ] Delete operations remove records
- [ ] Logging captures appropriate messages
- [ ] Remote logging creates records (if enabled)
- [ ] Notifications display correctly
- [ ] Progress indicators show/hide properly
- [ ] User context is captured
- [ ] No console errors
- [ ] Works across different browsers

### Test Scenarios

Create test cases for:
- Form OnLoad events
- Field OnChange events
- Save operations
- Ribbon button clicks
- Web resources
- Batch operations
- Error conditions

## 📚 Documentation

### Documentation Requirements

When adding new features:

- Update **README.md** with new functionality
- Add examples to **Quick Start guide**
- Document in **API Reference**
- Update **TypeScript definitions**
- Add to **CHANGELOG.md**

### Documentation Style

- Use **clear, concise language**
- Include **code examples**
- Explain **why**, not just **what**
- Add **common use cases**
- Document **edge cases**

## 🏗️ Project Structure

```
orca/
├── src/
│   └── Orca.js              # Main library
├── docs/
│   ├── Orca-QuickStart.md   # Quick start guide
│   ├── Orca-API-Reference.md # API documentation
│   ├── Orca-Migration-Guide.md # Migration guide
│   ├── Configuration-Setup.md # Setup instructions
│   └── Entity-Schemas.md    # Entity definitions
├── examples/
│   └── form-examples.js     # Usage examples
├── types/
│   └── Orca.d.ts           # TypeScript definitions
├── .gitignore
├── CHANGELOG.md
├── CONTRIBUTING.md
├── LICENSE
└── README.md
```

## 🔄 Release Process

1. Update version in `Orca.js`
2. Update `CHANGELOG.md`
3. Create git tag (`git tag v2.0.0`)
4. Push tag (`git push origin v2.0.0`)
5. Create GitHub release
6. Update documentation

## 💬 Communication

- **Issues** - For bugs and feature requests
- **Discussions** - For questions and ideas
- **Pull Requests** - For code contributions

## 📋 Commit Message Format

Use clear, descriptive commit messages:

```
feat: Add batch update capability to Service wrapper
fix: Resolve user context loading in offline mode
docs: Update API reference for Notification methods
refactor: Simplify log batching logic
test: Add tests for Service.createRecord method
```

Prefixes:
- `feat:` - New feature
- `fix:` - Bug fix
- `docs:` - Documentation changes
- `refactor:` - Code refactoring
- `test:` - Adding tests
- `chore:` - Maintenance tasks

## ⚖️ License

By contributing, you agree that your contributions will be licensed under the MIT License.

## 🎯 Priorities

Current priorities for contributions:

1. **Bug fixes** - Always welcome
2. **Documentation improvements** - Highly valued
3. **Performance optimizations** - Appreciated
4. **New features** - Discuss first in an issue
5. **Example code** - Very helpful

## ❓ Questions?

If you have questions:

1. Check the [documentation](docs/)
2. Search [existing issues](https://github.com/yourusername/orca/issues)
3. Ask in [Discussions](https://github.com/yourusername/orca/discussions)
4. Create a new issue

## 🙏 Thank You!

Your contributions make Orca better for the entire Dynamics 365 community!

---

**Happy Contributing! 🐋**
