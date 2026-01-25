# Orca API Reference

Complete API documentation for Orca - the Dynamics 365 JavaScript client library.

## Table of Contents
- [Initialization](#initialization)
- [orca.Log](#orcalog)
- [orca.Service](#orcaservice)
- [orca.Utility](#orcautility)
- [orca.Notification](#orcanotification)
- [Configuration Methods](#configuration-methods)
- [Constants](#constants)

---

## Initialization

### Constructor

```typescript
new Orca(options?: OrcaOptions)
```

**Parameters:**

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `options` | `OrcaOptions` | `{}` | Configuration options |

**OrcaOptions Interface:**

```typescript
interface OrcaOptions {
    logLevel?: LogLevel;                      // default: LogLevel.INFO
    autoReloadOnLogLevelChange?: boolean;     // default: true
    logToConsole?: boolean;                   // default: true
    maxLogHistory?: number;                   // default: 100
    enableRemoteLogging?: boolean;            // default: false
    remoteLogOnlyErrorsWarnings?: boolean;    // default: true
    remoteLogBatchSize?: number;              // default: 10
    loadConfigFromEntity?: boolean;           // default: true
    configEntityName?: string;                // default: 'new_cmaconfiguration'
}
```

**Example:**

```javascript
// Default initialization (loads config from D365)
const orca = new Orca();

// Manual configuration
const orca = new Orca({
    logLevel: Orca.LogLevel.VERBOSE,
    enableRemoteLogging: true,
    loadConfigFromEntity: false
});
```

---

## orca.Log

Logging methods with automatic remote logging support and user context capture.

### Log.Info()

```typescript
orca.Log.Info(component: string, message: string, data?: any): void
```

Log informational message.

**Parameters:**
- `component` - Component or module name
- `message` - Log message
- `data` - Optional additional data

**Example:**

```javascript
orca.Log.Info("AccountForm", "Form loaded successfully");
orca.Log.Info("DataService", "Retrieved 50 records", { count: 50 });
```

---

### Log.Warn()

```typescript
orca.Log.Warn(component: string, message: string, data?: any): void
```

Log warning message (logged remotely if remote logging enabled).

**Example:**

```javascript
orca.Log.Warn("ValidationService", "Missing optional field", {
    fieldName: "telephone2",
    recordId: contactId
});
```

---

### Log.Error()

```typescript
orca.Log.Error(component: string, message: string, data?: any): void
```

Log error message (always logged remotely if remote logging enabled).

**Example:**

```javascript
try {
    await processPayment();
} catch (error) {
    orca.Log.Error("PaymentProcessor", "Payment processing failed", error);
}
```

---

### Log.Verbose()

```typescript
orca.Log.Verbose(component: string, message: string, data?: any): void
```

Log verbose/verbose message (not logged remotely by default).

**Example:**

```javascript
orca.Log.Verbose("DataService", "Query execution details", {
    query: options,
    duration: 125,
    recordCount: 10
});
```

---

## orca.Service

WebAPI wrapper for data operations. All methods return Promises.

### Service.createRecord()

```typescript
orca.Service.createRecord(
    entityLogicalName: string,
    data: object
): Promise<CreateResult>
```

Create a new record.

**Returns:** `{ id: string, entityType: string }`

**Example:**

```javascript
const account = await orca.Service.createRecord("account", {
    name: "Contoso Ltd",
    telephone1: "555-1234",
    "primarycontactid@odata.bind": `/contacts(${contactId})`
});

console.log("Created:", account.id);
```

---

### Service.retrieveRecord()

```typescript
orca.Service.retrieveRecord(
    entityLogicalName: string,
    id: string,
    options?: string
): Promise<any>
```

Retrieve a single record.

**Parameters:**
- `entityLogicalName` - Entity logical name
- `id` - GUID of the record (without braces)
- `options` - OData query string (e.g., `?$select=name,telephone1`)

**Example:**

```javascript
const options = "?$select=name,telephone1&$expand=primarycontactid($select=fullname)";
const account = await orca.Service.retrieveRecord("account", accountId, options);

console.log("Account:", account.name);
console.log("Primary Contact:", account.primarycontactid.fullname);
```

---

### Service.retrieveMultipleRecords()

```typescript
orca.Service.retrieveMultipleRecords(
    entityLogicalName: string,
    options?: string,
    maxPageSize?: number
): Promise<RetrieveMultipleResult>
```

Retrieve multiple records.

**Returns:** 
```typescript
{
    entities: any[];
    nextLink?: string;  // Present if more pages available
}
```

**Example:**

```javascript
const options = "?$select=name,revenue&$filter=statecode eq 0&$orderby=name asc&$top=50";
const result = await orca.Service.retrieveMultipleRecords("account", options, 5000);

console.log(`Retrieved ${result.entities.length} records`);

result.entities.forEach(account => {
    console.log(account.name);
});

if (result.nextLink) {
    console.log("More records available");
}
```

---

### Service.updateRecord()

```typescript
orca.Service.updateRecord(
    entityLogicalName: string,
    id: string,
    data: object
): Promise<any>
```

Update an existing record.

**Example:**

```javascript
await orca.Service.updateRecord("account", accountId, {
    telephone1: "555-9999",
    address1_city: "Seattle",
    revenue: 1000000
});

orca.Log.Info("UpdateAccount", "Account updated successfully");
```

---

### Service.deleteRecord()

```typescript
orca.Service.deleteRecord(
    entityLogicalName: string,
    id: string
): Promise<any>
```

Delete a record.

**Example:**

```javascript
const confirmed = await orca.Notification.showConfirm(
    "Are you sure you want to delete this record?",
    "Confirm Deletion"
);

if (confirmed) {
    await orca.Service.deleteRecord("account", accountId);
    orca.Log.Info("DeleteAccount", "Account deleted");
}
```

---

### Service.execute()

```typescript
orca.Service.execute(request: any): Promise<any>
```

Execute a Web API request (actions, functions, custom APIs).

**Example:**

```javascript
const request = {
    entity: {
        entityType: "account",
        id: accountId
    },
    
    getMetadata: function() {
        return {
            boundParameter: "entity",
            operationType: 0,
            operationName: "CalculateRollupField",
            parameterTypes: {
                entity: {
                    typeName: "mscrm.account",
                    structuralProperty: 5
                },
                FieldName: {
                    typeName: "Edm.String",
                    structuralProperty: 1
                }
            }
        };
    },
    
    FieldName: "totalrevenue"
};

const result = await orca.Service.execute(request);
```

---

### Service.executeMultiple()

```typescript
orca.Service.executeMultiple(requests: any[]): Promise<any>
```

Execute multiple Web API requests in a batch.

**Example:**

```javascript
const requests = accountIds.map(id => ({
    // ... request objects
}));

const results = await orca.Service.executeMultiple(requests);
```

---

### Service.isAvailableOffline()

```typescript
orca.Service.isAvailableOffline(
    entityLogicalName: string,
    id: string
): Promise<boolean>
```

Check if a record is available offline.

**Example:**

```javascript
const isOffline = await orca.Service.isAvailableOffline("account", accountId);
if (isOffline) {
    console.log("Record is available offline");
}
```

---

## orca.Utility

Wrapper for Xrm.Utility and Xrm.Navigation functions.

### Utility.showProgressIndicator()

```typescript
orca.Utility.showProgressIndicator(message: string): void
```

Show a progress indicator.

**Example:**

```javascript
orca.Utility.showProgressIndicator("Loading data...");
```

---

### Utility.closeProgressIndicator()

```typescript
orca.Utility.closeProgressIndicator(): void
```

Close the progress indicator.

**Example:**

```javascript
orca.Utility.closeProgressIndicator();
```

---

### Utility.showAlert()

```typescript
orca.Utility.showAlert(
    text: string,
    options?: { confirmButtonLabel?: string }
): Promise<void>
```

Show an alert dialog.

**Example:**

```javascript
await orca.Utility.showAlert("Operation completed successfully");

await orca.Utility.showAlert("Changes saved", {
    confirmButtonLabel: "Got it!"
});
```

---

### Utility.showConfirm()

```typescript
orca.Utility.showConfirm(
    text: string,
    options?: {
        title?: string;
        confirmButtonLabel?: string;
        cancelButtonLabel?: string;
    }
): Promise<boolean>
```

Show a confirmation dialog.

**Returns:** `true` if confirmed, `false` if cancelled

**Example:**

```javascript
const confirmed = await orca.Utility.showConfirm(
    "Delete this record?",
    {
        title: "Confirm Deletion",
        confirmButtonLabel: "Delete",
        cancelButtonLabel: "Cancel"
    }
);

if (confirmed) {
    await orca.Service.deleteRecord("account", accountId);
}
```

---

### Utility.showErrorDialog()

```typescript
orca.Utility.showErrorDialog(errorOptions: {
    message: string;
    details?: string;
}): Promise<void>
```

Show an error dialog.

**Example:**

```javascript
await orca.Utility.showErrorDialog({
    message: "An error occurred while saving",
    details: "The record is locked by another user"
});
```

---

### Utility.openForm()

```typescript
orca.Utility.openForm(
    entityFormOptions: EntityFormOptions,
    formParameters?: object
): Promise<any>
```

Open an entity form.

**EntityFormOptions:**

```typescript
interface EntityFormOptions {
    entityName: string;
    entityId?: string;
    formId?: string;
    openInNewWindow?: boolean;
    useQuickCreateForm?: boolean;
    windowPosition?: number;
}
```

**Example:**

```javascript
// Open existing record
await orca.Utility.openForm({
    entityName: "account",
    entityId: accountId,
    openInNewWindow: false
});

// Create new record with default values
await orca.Utility.openForm(
    {
        entityName: "contact",
        useQuickCreateForm: false
    },
    {
        firstname: "Jane",
        lastname: "Doe",
        "parentcustomerid": {
            id: accountId,
            entityType: "account",
            name: "Contoso Ltd"
        }
    }
);
```

---

### Utility.openUrl()

```typescript
orca.Utility.openUrl(
    url: string,
    options?: {
        height?: number;
        width?: number;
    }
): void
```

Open a URL.

**Example:**

```javascript
orca.Utility.openUrl("https://www.microsoft.com", {
    height: 600,
    width: 800
});
```

---

### Utility.openWebResource()

```typescript
orca.Utility.openWebResource(
    webResourceName: string,
    options?: { height?: number; width?: number },
    data?: string
): Promise<any>
```

Open a web resource.

**Example:**

```javascript
await orca.Utility.openWebResource(
    "new_/html/custom_report.htm",
    { width: 1000, height: 700 },
    "reportId=123&format=pdf"
);
```

---

### Utility.openFile()

```typescript
orca.Utility.openFile(
    file: {
        fileContent: string;
        fileName: string;
        fileSize: number;
        mimeType: string;
    },
    options?: { openMode?: number }
): Promise<void>
```

Open or save a file.

**Example:**

```javascript
const fileContent = "SGVsbG8gV29ybGQh";  // Base64

await orca.Utility.openFile({
    fileContent: fileContent,
    fileName: "report.txt",
    fileSize: 1024,
    mimeType: "text/plain"
}, {
    openMode: 1  // 1 = Open, 2 = Save
});
```

---

### Utility.lookupObjects()

```typescript
orca.Utility.lookupObjects(lookupOptions: {
    entityTypes: string[];
    allowMultiSelect?: boolean;
    defaultEntityType?: string;
    defaultViewId?: string;
    viewIds?: string[];
    searchText?: string;
}): Promise<LookupResult[]>
```

Open a lookup dialog.

**Returns:**

```typescript
interface LookupResult {
    id: string;
    name: string;
    entityType: string;
}
```

**Example:**

```javascript
const selected = await orca.Utility.lookupObjects({
    entityTypes: ["account", "contact"],
    allowMultiSelect: false,
    defaultEntityType: "account"
});

if (selected && selected.length > 0) {
    const record = selected[0];
    console.log("Selected:", record.name);
    console.log("Type:", record.entityType);
    console.log("ID:", record.id);
}
```

---

### Utility.getGlobalContext()

```typescript
orca.Utility.getGlobalContext(): Xrm.GlobalContext
```

Get the global context.

**Example:**

```javascript
const context = orca.Utility.getGlobalContext();
console.log("Org Name:", context.organizationSettings.uniqueName);
console.log("User ID:", context.userSettings.userId);
```

---

### Utility.getPageContext()

```typescript
orca.Utility.getPageContext(): any
```

Get the page context.

---

### Utility.getEntityMetadata()

```typescript
orca.Utility.getEntityMetadata(
    entityName: string,
    attributes?: string[]
): Promise<any>
```

Get entity metadata.

**Example:**

```javascript
const metadata = await orca.Utility.getEntityMetadata("account", [
    "name",
    "telephone1",
    "revenue"
]);

console.log("Entity metadata:", metadata);
```

---

### Utility.getResourceString()

```typescript
orca.Utility.getResourceString(
    webResourceName: string,
    key: string
): string
```

Get a localized resource string.

---

### Utility.invokeProcessAction()

```typescript
orca.Utility.invokeProcessAction(
    name: string,
    parameters: object
): Promise<any>
```

Invoke a process action or custom API.

**Example:**

```javascript
const result = await orca.Utility.invokeProcessAction(
    "new_CalculateScore",
    {
        Target: {
            entityType: "opportunity",
            id: opportunityId
        },
        Factor: 1.5
    }
);

console.log("Score:", result.Score);
```

---

### Utility.refreshParentGrid()

```typescript
orca.Utility.refreshParentGrid(lookupOptions?: any): void
```

Refresh the parent grid.

---

## orca.Notification

Notification management for forms, fields, and dialogs.

### Notification.setFormNotification()

```typescript
orca.Notification.setFormNotification(
    formContext: Xrm.FormContext,
    message: string,
    level?: "ERROR" | "WARNING" | "INFO",
    uniqueId?: string
): boolean
```

Set a form-level notification.

**Example:**

```javascript
orca.Notification.setFormNotification(
    formContext,
    "This record is locked for editing",
    "WARNING",
    "lock_warning"
);

// Auto-clear after 5 seconds
setTimeout(() => {
    orca.Notification.clearFormNotification(formContext, "lock_warning");
}, 5000);
```

---

### Notification.clearFormNotification()

```typescript
orca.Notification.clearFormNotification(
    formContext: Xrm.FormContext,
    uniqueId: string
): boolean
```

Clear a form-level notification.

---

### Notification.setAttributeNotification()

```typescript
orca.Notification.setAttributeNotification(
    attribute: Xrm.Attributes.Attribute,
    message: string,
    uniqueId?: string
): boolean
```

Set a field-level notification.

**Example:**

```javascript
const emailAttr = formContext.getAttribute("emailaddress1");

orca.Notification.setAttributeNotification(
    emailAttr,
    "Please enter a valid email address",
    "email_validation"
);
```

---

### Notification.clearAttributeNotification()

```typescript
orca.Notification.clearAttributeNotification(
    attribute: Xrm.Attributes.Attribute,
    uniqueId: string
): boolean
```

Clear a field-level notification.

---

### Notification.showAlert()

```typescript
orca.Notification.showAlert(
    message: string,
    title?: string,
    icon?: string
): Promise<void>
```

Show an alert dialog.

**Icons:** ERROR, WARNING, INFO, SUCCESS, QUESTION

**Example:**

```javascript
await orca.Notification.showAlert(
    "Your changes have been saved",
    "Success",
    "SUCCESS"
);
```

---

### Notification.showError()

```typescript
orca.Notification.showError(
    message: string,
    errorDetails?: any
): Promise<void>
```

Show an error dialog with details.

**Example:**

```javascript
await orca.Notification.showError(
    "Failed to save record",
    {
        errorCode: "0x80040217",
        message: "Duplicate record detected"
    }
);
```

---

### Notification.showConfirm()

```typescript
orca.Notification.showConfirm(
    message: string,
    title?: string
): Promise<boolean>
```

Show a confirmation dialog.

**Returns:** `true` if confirmed, `false` if cancelled

---

## Configuration Methods

### setLogLevel()

```typescript
orca.setLogLevel(level: LogLevel, reload?: boolean): void
```

Set the logging level.

**Example:**

```javascript
// Set to verbose without reloading
orca.setLogLevel(Orca.LogLevel.VERBOSE, false);

// Set to error with reload
orca.setLogLevel(Orca.LogLevel.ERROR, true);
```

---

### getLogLevel()

```typescript
orca.getLogLevel(): LogLevel
```

Get the current log level.

---

### getLogHistory()

```typescript
orca.getLogHistory(level?: LogLevel): LogEntry[]
```

Get log history, optionally filtered by level.

**Example:**

```javascript
// Get all logs
const allLogs = orca.getLogHistory();

// Get only errors
const errors = orca.getLogHistory(Orca.LogLevel.ERROR);

console.table(errors);
```

---

### clearLogHistory()

```typescript
orca.clearLogHistory(): void
```

Clear the log history.

---

### exportLogs()

```typescript
orca.exportLogs(): string
```

Export logs as JSON string.

**Example:**

```javascript
const logsJson = orca.exportLogs();
navigator.clipboard.writeText(logsJson);
console.log("Logs copied to clipboard");
```

---

### reloadConfiguration()

```typescript
orca.reloadConfiguration(): Promise<boolean>
```

Reload configuration from D365 entity.

**Example:**

```javascript
await orca.reloadConfiguration();
console.log("Configuration reloaded");
```

---

### flushRemoteLogs()

```typescript
orca.flushRemoteLogs(): Promise<void>
```

Manually flush queued remote logs to D365.

**Example:**

```javascript
// Force immediate logging
await orca.flushRemoteLogs();
```

---

## Constants

### Orca.LogLevel

```javascript
Orca.LogLevel.NONE      // 0
Orca.LogLevel.ERROR     // 1
Orca.LogLevel.WARN      // 2
Orca.LogLevel.INFO      // 3
Orca.LogLevel.VERBOSE   // 4
```

---

### Orca.NotificationType

```javascript
Orca.NotificationType.ERROR    // 1
Orca.NotificationType.WARNING  // 2
Orca.NotificationType.INFO     // 3
```

---

### Orca.AlertIcon

```javascript
Orca.AlertIcon.ERROR
Orca.AlertIcon.WARNING
Orca.AlertIcon.INFO
Orca.AlertIcon.SUCCESS
Orca.AlertIcon.QUESTION
```

---

## Properties

### Instance Properties

```typescript
orca.userId: string | null          // Current user GUID
orca.userName: string | null        // Current user name
orca.userEmail: string | null       // Current user email
orca.logLevel: LogLevel             // Current log level
orca.enableRemoteLogging: boolean   // Remote logging enabled
orca.configurationLoaded: boolean   // Config loaded from entity
```

**Example:**

```javascript
console.log({
    user: orca.userName,
    logLevel: orca.getLogLevel(),
    remoteLogging: orca.enableRemoteLogging
});
```

---

## Error Handling

All async methods can throw errors. Always use try-catch:

```javascript
try {
    const result = await orca.Service.createRecord("account", data);
    orca.Log.Info("CreateAccount", "Success", { id: result.id });
} catch (error) {
    orca.Log.Error("CreateAccount", "Failed", error);
    await orca.Notification.showError("Failed to create account", error);
}
```

---

## TypeScript Support

Orca includes TypeScript definitions. Import like:

```typescript
import { Orca, LogLevel, OrcaOptions } from './Orca';

const orca: Orca = new Orca({
    logLevel: LogLevel.INFO
});
```

---

**Version:** 2.0.0  
**Author:** Black Ink Developments  
**License:** Free for commercial use
