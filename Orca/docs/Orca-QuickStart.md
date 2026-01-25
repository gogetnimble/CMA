# Orca - Quick Start Guide

**Orca** is a comprehensive JavaScript wrapper for Dynamics 365, providing elegant APIs for WebAPI operations, utilities, notifications, and logging with full configuration and remote logging support.

## 🚀 Quick Start

### Basic Usage

```javascript
// Initialize Orca (configuration loads automatically from D365)
const orca = new Orca();

// Logging with capitalized methods
orca.Log.Info("MyApp", "Application started");
orca.Log.Warn("MyApp", "Low memory detected");
orca.Log.Error("MyApp", "Failed to load data", errorObject);
orca.Log.Verbose("MyApp", "Detailed verbose information");

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

## 📋 API Structure

### **orca.Log** - Logging Methods

```javascript
// Capitalized logging methods
orca.Log.Info(component, message, data?)
orca.Log.Warn(component, message, data?)
orca.Log.Error(component, message, data?)
orca.Log.Verbose(component, message, data?)

// Examples
orca.Log.Info("ContactForm", "Form loaded successfully");
orca.Log.Error("PaymentProcessor", "Payment failed", { 
    orderId: "12345",
    errorCode: "DECLINED" 
});
```

### **orca.Service** - Data Operations (WebAPI)

```javascript
// Create
const result = await orca.Service.createRecord(entityName, data);

// Retrieve
const record = await orca.Service.retrieveRecord(entityName, id, options?);

// Retrieve Multiple
const results = await orca.Service.retrieveMultipleRecords(
    entityName, 
    options?,
    maxPageSize?
);

// Update
await orca.Service.updateRecord(entityName, id, data);

// Delete
await orca.Service.deleteRecord(entityName, id);

// Execute
const response = await orca.Service.execute(request);

// Execute Multiple
const responses = await orca.Service.executeMultiple(requests);

// Check Offline Availability
const isAvailable = await orca.Service.isAvailableOffline(entityName, id);
```

### **orca.Utility** - Utility Functions

```javascript
// Progress Indicators
orca.Utility.showProgressIndicator(message);
orca.Utility.closeProgressIndicator();

// Alerts & Confirmations
await orca.Utility.showAlert(text, options?);
const confirmed = await orca.Utility.showConfirm(text, options?);
await orca.Utility.showErrorDialog(errorOptions);

// Navigation
await orca.Utility.openForm(entityFormOptions, formParameters?);
orca.Utility.openUrl(url, options?);
await orca.Utility.openWebResource(webResourceName, options?, data?);
await orca.Utility.openFile(file, options?);

// Lookups
const selected = await orca.Utility.lookupObjects(lookupOptions);

// Context & Metadata
const context = orca.Utility.getGlobalContext();
const pageContext = orca.Utility.getPageContext();
const metadata = await orca.Utility.getEntityMetadata(entityName, attributes?);

// Resources & Actions
const string = orca.Utility.getResourceString(webResourceName, key);
const result = await orca.Utility.invokeProcessAction(name, parameters);

// Grid Operations
orca.Utility.refreshParentGrid(lookupOptions?);
```

### **orca.Notification** - Notification Management

```javascript
// Form Notifications
orca.Notification.setFormNotification(
    formContext,
    message,
    level,  // "ERROR", "WARNING", or "INFO"
    uniqueId?
);
orca.Notification.clearFormNotification(formContext, uniqueId);

// Attribute Notifications
orca.Notification.setAttributeNotification(attribute, message, uniqueId?);
orca.Notification.clearAttributeNotification(attribute, uniqueId);

// Dialog Notifications
await orca.Notification.showAlert(message, title?, icon?);
await orca.Notification.showError(message, errorDetails?);
const confirmed = await orca.Notification.showConfirm(message, title?);
```

## 🎯 Complete Form Example

```javascript
var MyCompany = MyCompany || {};

MyCompany.ContactForm = {
    orca: null,

    onLoad: function(executionContext) {
        // Initialize Orca
        this.orca = new Orca();
        
        const formContext = executionContext.getFormContext();
        
        this.orca.Log.Info("ContactForm", "Form loaded");
        
        // Load related data
        this.loadRelatedData(formContext);
    },

    loadRelatedData: async function(formContext) {
        const contactId = formContext.data.entity.getId().replace(/[{}]/g, "");
        
        this.orca.Utility.showProgressIndicator("Loading related activities...");
        
        try {
            const options = `?$select=subject,scheduledstart&$filter=_regardingobjectid_value eq ${contactId}&$top=10`;
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
            await this.orca.Notification.showError("Failed to load related activities", error);
        } finally {
            this.orca.Utility.closeProgressIndicator();
        }
    },

    onEmailChange: function(executionContext) {
        const formContext = executionContext.getFormContext();
        const emailAttr = formContext.getAttribute("emailaddress1");
        const email = emailAttr.getValue();

        if (email && !this.validateEmail(email)) {
            this.orca.Notification.setAttributeNotification(
                emailAttr,
                "Please enter a valid email address",
                "email_validation"
            );
            this.orca.Log.Warn("ContactForm", "Invalid email format", { email });
        } else {
            this.orca.Notification.clearAttributeNotification(emailAttr, "email_validation");
        }
    },

    validateEmail: function(email) {
        return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
    },

    onSave: function(executionContext) {
        const formContext = executionContext.getFormContext();
        const eventArgs = executionContext.getEventArgs();

        this.orca.Log.Info("ContactForm", "Save initiated");

        // Validation
        const firstName = formContext.getAttribute("firstname").getValue();
        const lastName = formContext.getAttribute("lastname").getValue();

        if (!firstName || !lastName) {
            eventArgs.preventDefault();
            this.orca.Notification.setFormNotification(
                formContext,
                "First name and last name are required",
                "ERROR",
                "name_required"
            );
            this.orca.Log.Warn("ContactForm", "Save prevented - missing required fields");
            return;
        }

        this.orca.Log.Info("ContactForm", "Save validation passed");
    }
};
```

## 🔧 Configuration

### Manual Configuration

```javascript
const orca = new Orca({
    logLevel: Orca.LogLevel.INFO,
    logToConsole: true,
    enableRemoteLogging: true,
    remoteLogOnlyErrorsWarnings: true,
    loadConfigFromEntity: false  // Disable entity-based config
});
```

### Entity-Based Configuration (Default)

Orca automatically loads configuration from `new_cmaconfiguration` entity:

```javascript
// Just initialize - configuration loads automatically
const orca = new Orca();

// Manually reload configuration if needed
await orca.reloadConfiguration();
```

## 📊 Log Levels

```javascript
Orca.LogLevel.NONE      // 0 - No logging
Orca.LogLevel.ERROR     // 1 - Errors only
Orca.LogLevel.WARN      // 2 - Warnings and errors
Orca.LogLevel.INFO      // 3 - Info, warnings, and errors (default)
Orca.LogLevel.VERBOSE   // 4 - All logging including verbose

// Change log level at runtime
orca.setLogLevel(Orca.LogLevel.VERBOSE, false);  // Don't reload page
```

## 🗄️ Remote Logging

When enabled, errors and warnings are automatically logged to D365 with full context:

```javascript
// Error logged remotely with:
// - User ID, Name, Email
// - Page URL
// - Browser User Agent
// - Timestamp
// - Component name
// - Error data

orca.Log.Error("PaymentModule", "Credit card declined", {
    amount: 150.00,
    cardType: "VISA",
    errorCode: "INSUFFICIENT_FUNDS"
});

// Manually flush queued logs
await orca.flushRemoteLogs();
```

## 🎨 Notification Types

```javascript
// Form notification levels
"ERROR"    // Red - Critical errors
"WARNING"  // Yellow - Warnings
"INFO"     // Blue - Information

// Alert icons
Orca.AlertIcon.ERROR
Orca.AlertIcon.WARNING
Orca.AlertIcon.INFO
Orca.AlertIcon.SUCCESS
Orca.AlertIcon.QUESTION
```

## 💡 Best Practices

### 1. Initialize Once Per Context

```javascript
// Good: Initialize once
var MyApp = {
    orca: new Orca(),
    
    method1: function() {
        this.orca.Log.Info("MyApp", "Method 1");
    }
};

// Avoid: Initializing multiple times
function myFunction() {
    const orca = new Orca();  // ❌ Don't do this repeatedly
}
```

### 2. Use Descriptive Component Names

```javascript
// Good: Clear, specific component names
orca.Log.Info("AccountForm", "Record saved");
orca.Log.Error("PaymentGateway", "Transaction failed");
orca.Log.Warn("ValidationService", "Missing required field");

// Avoid: Vague component names
orca.Log.Info("Form", "Something happened");  // ❌ Too vague
```

### 3. Include Contextual Data

```javascript
// Good: Include relevant context
orca.Log.Error("OrderProcessor", "Failed to process order", {
    orderId: "ORD-12345",
    customerId: accountId,
    amount: 1500.00,
    errorCode: "PAYMENT_DECLINED"
});

// Avoid: Error without context
orca.Log.Error("OrderProcessor", "Error");  // ❌ No context
```

### 4. Clean Up Notifications

```javascript
// Set notification with unique ID
orca.Notification.setFormNotification(
    formContext,
    "Processing...",
    "INFO",
    "processing_msg"
);

// Clear it when done
setTimeout(() => {
    orca.Notification.clearFormNotification(formContext, "processing_msg");
}, 3000);
```

### 5. Handle Errors Gracefully

```javascript
try {
    await orca.Service.createRecord("account", data);
    orca.Log.Info("CreateAccount", "Account created successfully");
} catch (error) {
    orca.Log.Error("CreateAccount", "Failed to create account", error);
    await orca.Notification.showError(
        "Unable to create account",
        "Please try again or contact support"
    );
}
```

## 🔍 Debugging

### View Log History

```javascript
// Get all logs
const allLogs = orca.getLogHistory();
console.table(allLogs);

// Get only errors
const errors = orca.getLogHistory(Orca.LogLevel.ERROR);

// Export logs as JSON
const logsJson = orca.exportLogs();
console.log(logsJson);

// Clear log history
orca.clearLogHistory();
```

### Change Log Level at Runtime

```javascript
// In browser console for debugging
orca.setLogLevel(Orca.LogLevel.VERBOSE, false);

// Reset to normal
orca.setLogLevel(Orca.LogLevel.INFO, false);
```

## 📱 User Context

Orca automatically captures user context:

```javascript
console.log({
    userId: orca.userId,        // "8f3e4567-e89b-12d3-a456-426614174000"
    userName: orca.userName,    // "John Doe"
    userEmail: orca.userEmail   // "john.doe@company.com"
});
```

## 🎭 Service Examples

### Create with Related Records

```javascript
const contactData = {
    firstname: "Jane",
    lastname: "Smith",
    emailaddress1: "jane.smith@company.com",
    "parentcustomerid_account@odata.bind": `/accounts(${accountId})`
};

const result = await orca.Service.createRecord("contact", contactData);
orca.Log.Info("CreateContact", "Contact created", { contactId: result.id });
```

### Retrieve with Expand

```javascript
const options = `?$select=name,telephone1&$expand=primarycontactid($select=fullname,emailaddress1)`;
const account = await orca.Service.retrieveRecord("account", accountId, options);

orca.Log.Verbose("RetrieveAccount", "Account retrieved", {
    name: account.name,
    primaryContact: account.primarycontactid?.fullname
});
```

### Batch Operations

```javascript
const accountIds = ["id1", "id2", "id3"];
const updateData = { address1_city: "Seattle" };

orca.Utility.showProgressIndicator(`Updating ${accountIds.length} accounts...`);

try {
    for (const id of accountIds) {
        await orca.Service.updateRecord("account", id, updateData);
    }
    orca.Log.Info("BatchUpdate", `Successfully updated ${accountIds.length} accounts`);
    await orca.Notification.showAlert("All accounts updated successfully");
} catch (error) {
    orca.Log.Error("BatchUpdate", "Batch update failed", error);
    await orca.Notification.showError("Some updates failed", error);
} finally {
    orca.Utility.closeProgressIndicator();
}
```

## 🌐 Migration from D365Client

```javascript
// Old API
const d365 = new D365Client();
d365.webApi.createRecord(...);
d365.utility.showAlert(...);
d365.notification.setFormNotification(...);
d365.info("Component", "Message");

// New Orca API
const orca = new Orca();
orca.Service.createRecord(...);
orca.Utility.showAlert(...);
orca.Notification.setFormNotification(...);
orca.Log.Info("Component", "Message");
```

## 🔗 Related Documentation

- **Orca-Configuration-Setup.md** - Detailed entity setup guide
- **Orca-Entity-Schemas.md** - Complete entity field definitions
- **Orca-Examples.js** - Additional usage examples

---

**Version:** 2.0.0  
**License:** Use freely in your Dynamics 365 projects
