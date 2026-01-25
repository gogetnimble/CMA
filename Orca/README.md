# Orca 🐋

**A comprehensive JavaScript wrapper for Microsoft Dynamics 365**

Orca provides an elegant, enterprise-grade API for Dynamics 365 development with built-in logging, configuration management, and remote error tracking.

[![Version](https://img.shields.io/badge/version-2.0.0-blue.svg)](https://github.com/yourusername/orca)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

## ✨ Features

- 🎯 **Clean API** - Intuitive, capitalized namespaces (`Service`, `Utility`, `Notification`, `Log`)
- 🗄️ **Remote Logging** - Automatic error tracking with full user context
- ⚙️ **Entity Configuration** - Load settings from D365 instead of hardcoding
- 📊 **Smart Batching** - Efficient API usage with intelligent log batching
- 🔍 **User Context** - Automatic capture of user ID, name, email, and browser info
- 🚀 **Latest API** - Always uses current D365 API version (no hardcoded versions)
- 📝 **TypeScript Support** - Full type definitions included
- ✅ **Production Ready** - Comprehensive error handling and monitoring

## 🚀 Quick Start

### Installation

1. Upload `Orca.js` as a web resource to your D365 environment
2. Add it to your form libraries or HTML web resources

```html
<script src="your_prefix_/scripts/Orca.js"></script>
```

### Basic Usage

```javascript
// Initialize (configuration loads automatically from D365)
const orca = new Orca();

// Logging with capitalized methods
orca.Log.Info("MyApp", "Application started");
orca.Log.Warn("MyApp", "Low memory detected");
orca.Log.Error("MyApp", "Failed to load data", errorObject);

// Service (WebAPI) operations
const account = await orca.Service.createRecord("account", {
    name: "Contoso Ltd",
    telephone1: "555-1234"
});

// Utility functions
orca.Utility.showProgressIndicator("Loading...");
const confirmed = await orca.Utility.showConfirm("Are you sure?");
orca.Utility.closeProgressIndicator();

// Notifications
orca.Notification.setFormNotification(
    formContext,
    "Record saved successfully",
    "INFO",
    "save_success"
);
```

## 📚 Documentation

- **[Quick Start Guide](docs/Orca-QuickStart.md)** - Get up and running quickly
- **[API Reference](docs/Orca-API-Reference.md)** - Complete API documentation
- **[Migration Guide](docs/Orca-Migration-Guide.md)** - Migrate from D365Client
- **[Configuration Setup](docs/Configuration-Setup.md)** - Entity configuration guide
- **[Entity Schemas](docs/Entity-Schemas.md)** - Database schema reference

## 🎯 API Overview

### orca.Log - Organized Logging

```javascript
orca.Log.Info(component, message, data?)
orca.Log.Warn(component, message, data?)
orca.Log.Error(component, message, data?)
orca.Log.Verbose(component, message, data?)
```

### orca.Service - Data Operations

```javascript
await orca.Service.createRecord(entityName, data)
await orca.Service.retrieveRecord(entityName, id, options?)
await orca.Service.retrieveMultipleRecords(entityName, options?, maxPageSize?)
await orca.Service.updateRecord(entityName, id, data)
await orca.Service.deleteRecord(entityName, id)
await orca.Service.execute(request)
await orca.Service.executeMultiple(requests)
```

### orca.Utility - Helper Functions

```javascript
orca.Utility.showProgressIndicator(message)
orca.Utility.closeProgressIndicator()
await orca.Utility.showAlert(text, options?)
await orca.Utility.showConfirm(text, options?)
await orca.Utility.openForm(entityFormOptions, formParameters?)
await orca.Utility.lookupObjects(lookupOptions)
const context = orca.Utility.getGlobalContext()
```

### orca.Notification - User Feedback

```javascript
orca.Notification.setFormNotification(formContext, message, level, uniqueId?)
orca.Notification.clearFormNotification(formContext, uniqueId)
orca.Notification.setAttributeNotification(attribute, message, uniqueId?)
await orca.Notification.showAlert(message, title?, icon?)
await orca.Notification.showError(message, errorDetails?)
```

## 🔧 Configuration

### Manual Configuration

```javascript
const orca = new Orca({
    logLevel: Orca.LogLevel.INFO,
    logToConsole: true,
    enableRemoteLogging: true,
    remoteLogOnlyErrorsWarnings: true,
    loadConfigFromEntity: false
});
```

### Entity-Based Configuration (Recommended)

Create configuration entity in D365:

**Entity:** `new_cmaconfiguration`

| Field | Type | Description |
|-------|------|-------------|
| `new_loglevel` | Option Set | Log verbosity (0-4) |
| `new_logtoconsole` | Boolean | Enable console logging |
| `new_enableremotelogging` | Boolean | Enable D365 logging |
| `new_remotelogonlyerrorswarnings` | Boolean | Log only errors/warnings |
| `new_remotelogbatchsize` | Number | Batch size for API efficiency |

See [Configuration Setup](docs/Configuration-Setup.md) for complete instructions.

## 🗄️ Remote Logging

When enabled, errors are automatically logged to D365 with full context:

**Logging Entity:** `new_cmajavascriptlog`

Each log entry captures:
- ✅ User ID, Name, Email
- ✅ Page URL
- ✅ Browser User Agent
- ✅ Timestamp
- ✅ Component Name
- ✅ Error Details (JSON)

```javascript
// Automatically logged remotely with full context
orca.Log.Error("PaymentModule", "Payment failed", {
    orderId: "12345",
    amount: 150.00,
    errorCode: "DECLINED"
});
```

## 📊 Log Levels

```javascript
Orca.LogLevel.NONE      // 0 - No logging
Orca.LogLevel.ERROR     // 1 - Errors only
Orca.LogLevel.WARN      // 2 - Warnings and errors
Orca.LogLevel.INFO      // 3 - Info, warnings, and errors (default)
Orca.LogLevel.VERBOSE   // 4 - All logging including verbose
```

## 🎨 Complete Form Example

```javascript
var MyCompany = MyCompany || {};

MyCompany.ContactForm = {
    orca: null,

    onLoad: function(executionContext) {
        this.orca = new Orca();
        const formContext = executionContext.getFormContext();
        
        this.orca.Log.Info("ContactForm", "Form loaded");
        this.loadRelatedData(formContext);
    },

    loadRelatedData: async function(formContext) {
        const contactId = formContext.data.entity.getId().replace(/[{}]/g, "");
        
        this.orca.Utility.showProgressIndicator("Loading activities...");
        
        try {
            const options = `?$select=subject&$filter=_regardingobjectid_value eq ${contactId}&$top=10`;
            const result = await this.orca.Service.retrieveMultipleRecords("activity", options);
            
            this.orca.Log.Info("ContactForm", `Loaded ${result.entities.length} activities`);
            
            if (result.entities.length > 5) {
                this.orca.Notification.setFormNotification(
                    formContext,
                    `This contact has ${result.entities.length} activities`,
                    "INFO",
                    "activity_count"
                );
            }
        } catch (error) {
            this.orca.Log.Error("ContactForm", "Failed to load activities", error);
            await this.orca.Notification.showError("Failed to load activities", error);
        } finally {
            this.orca.Utility.closeProgressIndicator();
        }
    },

    onSave: function(executionContext) {
        const formContext = executionContext.getFormContext();
        this.orca.Log.Info("ContactForm", "Form saved successfully");
    }
};
```

## 🔄 Migration from D365Client

Simple find-and-replace migration:

| Old (D365Client) | New (Orca) |
|------------------|------------|
| `d365.webApi` | `orca.Service` |
| `d365.utility` | `orca.Utility` |
| `d365.notification` | `orca.Notification` |
| `d365.info()` | `orca.Log.Info()` |
| `d365.warn()` | `orca.Log.Warn()` |
| `d365.error()` | `orca.Log.Error()` |
| `d365.debug()` | `orca.Log.Verbose()` |

See [Migration Guide](docs/Orca-Migration-Guide.md) for complete instructions.

## 💡 Best Practices

### 1. Initialize Once

```javascript
// Good
var MyApp = {
    orca: new Orca(),
    
    method1: function() {
        this.orca.Log.Info("MyApp", "Method 1");
    }
};

// Avoid
function myFunction() {
    const orca = new Orca();  // ❌ Don't initialize repeatedly
}
```

### 2. Use Descriptive Component Names

```javascript
// Good
orca.Log.Info("AccountForm", "Record saved");
orca.Log.Error("PaymentGateway", "Transaction failed");

// Avoid
orca.Log.Info("Form", "Something happened");  // ❌ Too vague
```

### 3. Include Contextual Data

```javascript
// Good
orca.Log.Error("OrderProcessor", "Failed to process order", {
    orderId: "ORD-12345",
    customerId: accountId,
    amount: 1500.00,
    errorCode: "PAYMENT_DECLINED"
});

// Avoid
orca.Log.Error("OrderProcessor", "Error");  // ❌ No context
```

### 4. Handle Errors Gracefully

```javascript
try {
    await orca.Service.createRecord("account", data);
    orca.Log.Info("CreateAccount", "Success");
} catch (error) {
    orca.Log.Error("CreateAccount", "Failed", error);
    await orca.Notification.showError("Unable to create account", error);
}
```

## 🔍 Debugging

```javascript
// View log history
const allLogs = orca.getLogHistory();
console.table(allLogs);

// Get only errors
const errors = orca.getLogHistory(Orca.LogLevel.ERROR);

// Export logs
const logsJson = orca.exportLogs();

// Change log level at runtime
orca.setLogLevel(Orca.LogLevel.VERBOSE, false);
```

## 🏗️ Project Structure

```
orca/
├── src/
│   └── Orca.js                 # Main library (40KB)
├── docs/
│   ├── Orca-QuickStart.md
│   ├── Orca-API-Reference.md
│   ├── Orca-Migration-Guide.md
│   ├── Configuration-Setup.md
│   └── Entity-Schemas.md
├── examples/
│   └── form-examples.js
├── types/
│   └── Orca.d.ts              # TypeScript definitions
├── LICENSE
└── README.md
```

## 📦 What's Included

- ✅ **Orca.js** - Main library with all features
- ✅ **TypeScript Definitions** - Full IntelliSense support
- ✅ **Comprehensive Docs** - Quick start, API reference, migration guide
- ✅ **Entity Schemas** - Complete field definitions for setup
- ✅ **Examples** - Real-world usage patterns

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

MIT License - feel free to use in your Dynamics 365 projects.

## 🆘 Support

- 📖 [Documentation](docs/)
- 🐛 [Report Issues](https://github.com/yourusername/orca/issues)
- 💬 [Discussions](https://github.com/yourusername/orca/discussions)

## 🎯 Why Orca?

- **Cleaner API** - More intuitive and organized than raw Xrm
- **Production Ready** - Enterprise-grade error tracking
- **Well Documented** - Comprehensive guides and examples
- **TypeScript Support** - Full type safety
- **Future Proof** - Always uses latest D365 API version
- **Battle Tested** - Built from real-world D365 projects

## 🚀 Roadmap

- [ ] Additional utility helpers
- [ ] Enhanced TypeScript support
- [ ] Performance monitoring
- [ ] Advanced caching strategies
- [ ] Plugin/extension system

---

**Made with ❤️ for the Dynamics 365 community**

**Version:** 2.0.0  
**Author:** Greg Smith  
**Organization:** Black Ink Developments
