# D365Client Configuration & Remote Logging Setup Guide

This guide explains how to set up the configuration entity and remote logging table for the D365Client library.

## Table of Contents
1. [Configuration Entity Setup](#configuration-entity-setup)
2. [Remote Logging Entity Setup](#remote-logging-entity-setup)
3. [Creating Sample Configuration](#creating-sample-configuration)
4. [Usage Examples](#usage-examples)
5. [Power Automate Integration](#power-automate-integration)

---

## Configuration Entity Setup

### Entity: `new_cmaconfiguration`

**Display Name:** CMA Configuration  
**Plural Name:** CMA Configurations  
**Primary Field:** `new_name` (Text)

### Fields

| Schema Name | Display Name | Type | Description | Required |
|------------|--------------|------|-------------|----------|
| `new_name` | Name | Single Line of Text | Configuration name/description | Yes |
| `new_loglevel` | Log Level | Option Set | Logging verbosity level | No |
| `new_autoreloadonloglevelchange` | Auto Reload on Log Level Change | Two Options (Yes/No) | Reload page when log level changes | No |
| `new_logtoconsole` | Log to Console | Two Options (Yes/No) | Enable browser console logging | No |
| `new_maxloghistory` | Max Log History | Whole Number | Maximum log entries to keep in memory | No |
| `new_enableremotelogging` | Enable Remote Logging | Two Options (Yes/No) | Enable logging to D365 table | No |
| `new_remotelogonlyerrorswarnings` | Remote Log Only Errors/Warnings | Two Options (Yes/No) | Only log errors and warnings remotely | No |
| `new_remotelogbatchsize` | Remote Log Batch Size | Whole Number | Number of logs to batch before sending | No |

### Option Set: `new_loglevel`

| Label | Value |
|-------|-------|
| None | 0 |
| Error | 1 |
| Warning | 2 |
| Info | 3 |
| Debug | 4 |

### Default Values

- **Log Level:** Info (3)
- **Auto Reload on Log Level Change:** No
- **Log to Console:** Yes
- **Max Log History:** 100
- **Enable Remote Logging:** No
- **Remote Log Only Errors/Warnings:** Yes
- **Remote Log Batch Size:** 10

---

## Remote Logging Entity Setup

### Entity: `new_cmajavascriptlog`

**Display Name:** CMA JavaScript Log  
**Plural Name:** CMA JavaScript Logs  
**Primary Field:** `new_name` (Text)

### Fields

| Schema Name | Display Name | Type | Length/Format | Description | Required |
|------------|--------------|------|---------------|-------------|----------|
| `new_name` | Name | Single Line of Text | 200 | Auto-generated log identifier | Yes |
| `new_level` | Level | Option Set | - | Log level (Error/Warning/Info/Debug) | Yes |
| `new_component` | Component | Single Line of Text | 100 | Component/module name | Yes |
| `new_message` | Message | Multiple Lines of Text | 2000 | Log message | Yes |
| `new_timestamp` | Timestamp | Single Line of Text | 50 | ISO timestamp | Yes |
| `new_data` | Data | Multiple Lines of Text | 10000 | Additional JSON data | No |
| `new_url` | URL | Single Line of Text | 500 | Page URL where log occurred | No |
| `new_useragent` | User Agent | Single Line of Text | 500 | Browser user agent | No |
| `new_userid` | User | Lookup | systemuser | User who triggered the log | No |

### Option Set: `new_level`

| Label | Value |
|-------|-------|
| None | 0 |
| Error | 1 |
| Warning | 2 |
| Info | 3 |
| Debug | 4 |

### Views to Create

#### 1. Active JavaScript Logs
- Filter: Status = Active
- Columns: Name, Level, Component, Timestamp, User, Created On
- Sort: Created On (Newest First)

#### 2. Errors Only
- Filter: Status = Active AND Level = Error
- Columns: Name, Component, Message, Timestamp, User, Created On
- Sort: Created On (Newest First)

#### 3. Warnings and Errors
- Filter: Status = Active AND (Level = Error OR Level = Warning)
- Columns: Name, Level, Component, Message, Timestamp, User
- Sort: Created On (Newest First)

---

## Creating the Entities via Solution

### PowerShell Script to Export Schema

You can use the following as a reference for creating your entities:

```xml
<!-- new_cmaconfiguration Entity -->
<Entity>
  <Name>new_cmaconfiguration</Name>
  <DisplayName>CMA Configuration</DisplayName>
  <PluralDisplayName>CMA Configurations</PluralDisplayName>
  <Attributes>
    <Attribute PhysicalName="new_loglevel" Type="Picklist">
      <DisplayName>Log Level</DisplayName>
      <RequiredLevel>None</RequiredLevel>
      <OptionSet>
        <Option Value="0" Label="None" />
        <Option Value="1" Label="Error" />
        <Option Value="2" Label="Warning" />
        <Option Value="3" Label="Info" />
        <Option Value="4" Label="Debug" />
      </OptionSet>
    </Attribute>
    <!-- Additional attributes... -->
  </Attributes>
</Entity>
```

---

## Creating Sample Configuration

### Method 1: Via UI

1. Navigate to **CMA Configurations** 
2. Click **New**
3. Fill in the following:
   - **Name:** Default Configuration
   - **Log Level:** Info
   - **Auto Reload on Log Level Change:** No
   - **Log to Console:** Yes
   - **Max Log History:** 100
   - **Enable Remote Logging:** Yes
   - **Remote Log Only Errors/Warnings:** Yes
   - **Remote Log Batch Size:** 10
4. Click **Save**

### Method 2: Via JavaScript Console

```javascript
// Create default configuration
Xrm.WebApi.createRecord("new_cmaconfiguration", {
    new_name: "Default Configuration",
    new_loglevel: 3, // Info
    new_autoreloadonloglevelchange: false,
    new_logtoconsole: true,
    new_maxloghistory: 100,
    new_enableremotelogging: true,
    new_remotelogonlyerrorswarnings: true,
    new_remotelogbatchsize: 10,
    statecode: 0,
    statuscode: 1
}).then(result => {
    console.log("Configuration created:", result.id);
});
```

### Method 3: Via Web API (C#)

```csharp
var config = new Entity("new_cmaconfiguration");
config["new_name"] = "Default Configuration";
config["new_loglevel"] = new OptionSetValue(3); // Info
config["new_autoreloadonloglevelchange"] = false;
config["new_logtoconsole"] = true;
config["new_maxloghistory"] = 100;
config["new_enableremotelogging"] = true;
config["new_remotelogonlyerrorswarnings"] = true;
config["new_remotelogbatchsize"] = 10;

service.Create(config);
```

---

## Usage Examples

### Example 1: Load Configuration from Entity (Default Behavior)

```javascript
// Configuration is automatically loaded from new_cmaconfiguration
const d365 = new D365Client();

// Wait for configuration to load (it loads asynchronously)
setTimeout(() => {
    console.log("Log Level:", d365.getLogLevel());
    console.log("Remote Logging Enabled:", d365.enableRemoteLogging);
}, 1000);
```

### Example 2: Disable Auto-Configuration Loading

```javascript
// Use manual configuration instead of loading from entity
const d365 = new D365Client({
    loadConfigFromEntity: false,
    logLevel: D365Client.LogLevel.DEBUG,
    enableRemoteLogging: false,
    logToConsole: true
});
```

### Example 3: Override Configuration Entity Name

```javascript
// Load from a different configuration entity
const d365 = new D365Client({
    configEntityName: 'new_customconfig',
    loadConfigFromEntity: true
});
```

### Example 4: Manually Reload Configuration

```javascript
const d365 = new D365Client();

// Later, after configuration changes in D365
d365.reloadConfiguration()
    .then(() => {
        console.log("Configuration reloaded successfully");
    })
    .catch(error => {
        console.error("Failed to reload configuration:", error);
    });
```

### Example 5: Test Remote Logging

```javascript
const d365 = new D365Client({
    enableRemoteLogging: true,
    remoteLogOnlyErrorsWarnings: true
});

// These will be logged remotely
d365.error("TestComponent", "This is a test error", { data: "error data" });
d365.warn("TestComponent", "This is a test warning");

// This will NOT be logged remotely (only console)
d365.info("TestComponent", "This is info - not logged remotely");

// Manually flush logs (normally happens automatically)
d365.flushRemoteLogs();
```

### Example 6: Development vs Production Configuration

Create two configuration records:

**Development Configuration:**
```javascript
{
    new_name: "Development Configuration",
    new_loglevel: 4, // Debug
    new_logtoconsole: true,
    new_enableremotelogging: false,
    new_remotelogonlyerrorswarnings: false
}
```

**Production Configuration:**
```javascript
{
    new_name: "Production Configuration",
    new_loglevel: 1, // Error only
    new_logtoconsole: false,
    new_enableremotelogging: true,
    new_remotelogonlyerrorswarnings: true,
    new_remotelogbatchsize: 20
}
```

Activate the appropriate configuration for your environment.

---

## Power Automate Integration

### Email Alerts for Critical Errors

Create a Power Automate flow to send email alerts when errors are logged:

**Trigger:** When a row is added (new_cmajavascriptlog)  
**Condition:** Level equals 1 (Error)  
**Action:** Send an email

```
Subject: Critical JavaScript Error in D365
Body:
Component: {new_component}
Message: {new_message}
User: {new_userid.fullname}
Timestamp: {new_timestamp}
URL: {new_url}

Data:
{new_data}
```

### Slack/Teams Notifications

Similar to email, you can send notifications to Slack or Teams channels:

**Trigger:** When a row is added (new_cmajavascriptlog)  
**Condition:** Level equals 1 (Error)  
**Action:** Post message to Teams/Slack

### Log Cleanup/Archival

Create a scheduled flow to archive or delete old logs:

**Trigger:** Recurrence (Daily at 2 AM)  
**Action:** List rows (new_cmajavascriptlog)  
**Filter:** Created On older than 30 days  
**Action:** Delete rows OR Update status to Inactive

---

## Security Configuration

### Security Role Permissions

Grant appropriate permissions to your security roles:

**For Users (Read-Only):**
- **new_cmaconfiguration:** Read (Organization level)
- **new_cmajavascriptlog:** Create (User level), Read (User level)

**For Administrators:**
- **new_cmaconfiguration:** Full (Organization level)
- **new_cmajavascriptlog:** Full (Organization level)

### Field-Level Security

Consider adding field-level security for sensitive log data:
- `new_data` field - may contain sensitive information
- `new_userid` field - limit who can see which user triggered errors

---

## Monitoring & Dashboard

### Create a Power BI Dashboard

Connect Power BI to your D365 environment and create visualizations:

1. **Error Trend Chart** - Errors over time
2. **Top Components with Errors** - Bar chart
3. **Errors by User** - Identify users encountering issues
4. **Average Response Time** - Track performance
5. **Error Rate by Browser** - Parse user agent

### Model-Driven App Dashboard

Create dashboards in your Model-Driven App:

1. **Error Count Chart** - Count of errors by day
2. **Recent Errors List** - Last 10 errors
3. **Errors by Component** - Pie chart
4. **Top Users Affected** - List view

---

## Troubleshooting

### Configuration Not Loading

**Issue:** Configuration not being applied  
**Solution:** 
1. Check that an active configuration record exists
2. Verify the entity name matches `new_cmaconfiguration`
3. Check browser console for errors during config load
4. Ensure user has read permission on the configuration entity

### Remote Logs Not Being Created

**Issue:** Logs not appearing in new_cmajavascriptlog  
**Solution:**
1. Verify `enableRemoteLogging` is true in configuration
2. Check user has create permission on new_cmajavascriptlog entity
3. Look for errors in browser console
4. Verify the lookup field name matches the systemuser entity

### Performance Issues with Remote Logging

**Issue:** Page is slow or unresponsive  
**Solution:**
1. Increase `new_remotelogbatchsize` to reduce API calls
2. Set `new_remotelogonlyerrorswarnings` to true
3. Reduce `new_loglevel` to capture fewer logs
4. Consider using Power Automate to batch process logs instead of real-time

---

## Best Practices

1. **Use One Active Configuration:** Only keep one configuration record active at a time
2. **Monitor Log Volume:** Remote logging can create a lot of records - implement cleanup
3. **Start Conservative:** Begin with `remoteLogOnlyErrorsWarnings: true`
4. **Test in Development:** Validate configuration changes in a dev environment first
5. **Regular Cleanup:** Archive or delete logs older than 30-90 days
6. **Alert on Errors:** Set up Power Automate flows for critical errors
7. **Review Regularly:** Weekly review of error logs to identify patterns
8. **Document Components:** Use clear, consistent component names for easier filtering

---

## Migration from Manual Config to Entity Config

If you're currently using manual configuration:

```javascript
// Old way
const d365 = new D365Client({
    logLevel: D365Client.LogLevel.INFO,
    enableRemoteLogging: true
});

// New way (loads from entity)
const d365 = new D365Client();
```

To maintain backward compatibility while testing:

```javascript
// Load from entity, but override specific settings
const d365 = new D365Client({
    loadConfigFromEntity: true,
    logToConsole: true  // Override this one setting
});
```

---

## Support & Additional Resources

- Microsoft Dataverse Web API Documentation
- Power Automate Documentation
- Model-Driven Apps Documentation
- Power BI Integration Guide
