# D365Client Entity Schemas - Quick Reference

## Entity 1: Configuration Entity

### Basic Info
- **Schema Name:** `new_cmaconfiguration`
- **Display Name:** CMA Configuration
- **Plural Display Name:** CMA Configurations
- **Ownership:** Organization Owned
- **Primary Field:** new_name

---

### Fields

#### 1. Name (Primary Field)
```
Schema Name: new_name
Display Name: Name
Type: Single Line of Text
Required: Business Required
Max Length: 100
Description: Configuration name or description
```

#### 2. Log Level
```
Schema Name: new_loglevel
Display Name: Log Level
Type: Option Set
Required: Optional
Default: 3 (Info)

Option Set Values:
- 0: None
- 1: Error
- 2: Warning
- 3: Info
- 4: Debug

Description: Determines which log messages are captured
```

#### 3. Auto Reload on Log Level Change
```
Schema Name: new_autoreloadonloglevelchange
Display Name: Auto Reload on Log Level Change
Type: Two Options (Yes/No)
Required: Optional
Default: No

Labels:
- Yes: Yes
- No: No

Description: Reload the page when log level changes
```

#### 4. Log to Console
```
Schema Name: new_logtoconsole
Display Name: Log to Console
Type: Two Options (Yes/No)
Required: Optional
Default: Yes

Labels:
- Yes: Yes
- No: No

Description: Enable browser console logging
```

#### 5. Max Log History
```
Schema Name: new_maxloghistory
Display Name: Max Log History
Type: Whole Number
Required: Optional
Default: 100
Min Value: 10
Max Value: 1000

Description: Maximum number of log entries to keep in memory
```

#### 6. Enable Remote Logging
```
Schema Name: new_enableremotelogging
Display Name: Enable Remote Logging
Type: Two Options (Yes/No)
Required: Optional
Default: No

Labels:
- Yes: Yes
- No: No

Description: Enable logging to D365 table
```

#### 7. Remote Log Only Errors/Warnings
```
Schema Name: new_remotelogonlyerrorswarnings
Display Name: Remote Log Only Errors/Warnings
Type: Two Options (Yes/No)
Required: Optional
Default: Yes

Labels:
- Yes: Yes
- No: No

Description: Only log errors and warnings remotely (saves storage)
```

#### 8. Remote Log Batch Size
```
Schema Name: new_remotelogbatchsize
Display Name: Remote Log Batch Size
Type: Whole Number
Required: Optional
Default: 10
Min Value: 1
Max Value: 100

Description: Number of logs to batch before sending to D365
```

---

### Business Rules

#### Rule 1: Warn if Remote Logging Enabled
```
Condition: Enable Remote Logging = Yes
Action: Show Information - "Remote logging will create records in CMA JavaScript Logs table"
```

#### Rule 2: Default Batch Size
```
Condition: Remote Log Batch Size is empty
Action: Set Value - Remote Log Batch Size = 10
```

---

### Forms

#### Main Form Layout

**Section 1: General Configuration**
- Name
- Log Level
- Auto Reload on Log Level Change
- Log to Console
- Max Log History

**Section 2: Remote Logging**
- Enable Remote Logging
- Remote Log Only Errors/Warnings
- Remote Log Batch Size

---

### Views

#### View 1: Active Configurations
```
Filter: Status = Active
Columns: Name, Log Level, Enable Remote Logging, Modified On
Sort: Modified On (Newest First)
```

---

## Entity 2: JavaScript Log Entity

### Basic Info
- **Schema Name:** `new_cmajavascriptlog`
- **Display Name:** CMA JavaScript Log
- **Plural Display Name:** CMA JavaScript Logs
- **Ownership:** User Owned
- **Primary Field:** new_name
- **Enable for Activities:** No
- **Enable Notes:** No

---

### Fields

#### 1. Name (Primary Field - Auto-Generated)
```
Schema Name: new_name
Display Name: Name
Type: Single Line of Text
Required: Business Required
Max Length: 200
Description: Auto-generated log identifier (format: LEVEL - COMPONENT - TIMESTAMP)
```

#### 2. Level
```
Schema Name: new_level
Display Name: Level
Type: Option Set
Required: Business Required

Option Set Values:
- 0: None
- 1: Error
- 2: Warning
- 3: Info
- 4: Debug

Description: Log severity level
```

#### 3. Component
```
Schema Name: new_component
Display Name: Component
Type: Single Line of Text
Required: Business Required
Max Length: 100

Description: Component or module name that generated the log
```

#### 4. Message
```
Schema Name: new_message
Display Name: Message
Type: Multiple Lines of Text
Required: Business Required
Max Length: 2000
Format: Text

Description: Log message describing the event
```

#### 5. Timestamp
```
Schema Name: new_timestamp
Display Name: Timestamp
Type: Single Line of Text
Required: Business Required
Max Length: 50
Format: Text

Description: ISO 8601 timestamp when log was created (client-side)
```

#### 6. Data
```
Schema Name: new_data
Display Name: Data
Type: Multiple Lines of Text
Required: Optional
Max Length: 10000
Format: Text

Description: Additional JSON data or error details
```

#### 7. URL
```
Schema Name: new_url
Display Name: URL
Type: Single Line of Text
Required: Optional
Max Length: 500
Format: URL

Description: Page URL where the log occurred
```

#### 8. User Agent
```
Schema Name: new_useragent
Display Name: User Agent
Type: Single Line of Text
Required: Optional
Max Length: 500

Description: Browser user agent string
```

#### 9. User ID
```
Schema Name: new_userid
Display Name: User
Type: Lookup
Target Entity: systemuser
Required: Optional
Relationship Name: new_systemuser_cmajavascriptlog

Description: User who triggered the log event
```

---

### Calculated Fields (Optional)

#### Browser Name (Calculated)
```
Schema Name: new_browsername
Display Name: Browser
Type: Single Line of Text (Calculated)
Format: Text

Formula: 
IF(CONTAINS(new_useragent, "Chrome"), "Chrome",
IF(CONTAINS(new_useragent, "Firefox"), "Firefox",
IF(CONTAINS(new_useragent, "Safari"), "Safari",
IF(CONTAINS(new_useragent, "Edge"), "Edge", "Other"))))

Description: Parsed browser name from user agent
```

---

### Views

#### View 1: Active JavaScript Logs
```
Name: Active JavaScript Logs
Type: Public
Filter: Status = Active
Columns: 
  - Name
  - Level (with conditional formatting)
  - Component
  - Timestamp
  - User
  - Created On
Sort: Created On (Newest First)

Conditional Formatting:
- Level = Error → Red background
- Level = Warning → Yellow background
```

#### View 2: Errors Only
```
Name: Errors Only
Type: Public
Filter: Status = Active AND Level = Error
Columns:
  - Name
  - Component
  - Message
  - Timestamp
  - User
  - URL
Sort: Created On (Newest First)
```

#### View 3: Warnings and Errors
```
Name: Warnings and Errors
Type: Public
Filter: Status = Active AND (Level = Error OR Level = Warning)
Columns:
  - Name
  - Level
  - Component
  - Message
  - Timestamp
  - User
Sort: Created On (Newest First)
```

#### View 4: My Logs
```
Name: My Logs
Type: Public
Filter: Status = Active AND User = Current User
Columns:
  - Name
  - Level
  - Component
  - Message
  - Timestamp
Sort: Created On (Newest First)
```

#### View 5: Logs by Component
```
Name: Logs by Component
Type: Public
Filter: Status = Active
Columns:
  - Component
  - Level
  - Message
  - Timestamp
  - User
Group By: Component
Sort: Component (A to Z), then Created On (Newest First)
```

---

### Charts

#### Chart 1: Errors Over Time
```
Type: Line Chart
X-Axis: Created On (by Day)
Y-Axis: Count of Logs
Filter: Level = Error
View: Last 30 Days
```

#### Chart 2: Logs by Level
```
Type: Pie Chart
Legend: Level
Value: Count of Logs
Filter: Last 7 Days
```

#### Chart 3: Top Components with Errors
```
Type: Bar Chart
X-Axis: Component
Y-Axis: Count of Logs
Filter: Level = Error, Last 30 Days
Sort: Count (Descending)
Top: 10
```

---

### Forms

#### Main Form Layout

**Header Section:**
- Level (with color coding)
- User
- Timestamp

**Section 1: Log Details**
- Name
- Component
- Message
- Data (expandable)

**Section 2: Context Information**
- URL
- User Agent
- Browser Name (if calculated field exists)

**Section 3: System Information**
- Created On
- Created By
- Modified On
- Modified By

---

### Business Rules

#### Rule 1: Make Data Section Collapsible
```
Condition: Always
Action: Default collapsed state for Data section
```

#### Rule 2: Color Code Level Field
```
Condition: Level = Error
Action: Set field color to Red

Condition: Level = Warning
Action: Set field color to Yellow

Condition: Level = Info
Action: Set field color to Blue
```

---

## Quick Import Script

### PowerShell - Create Configuration Record
```powershell
# Connect to your D365 instance first
$config = @{
    "new_name" = "Production Configuration"
    "new_loglevel" = 1  # Error
    "new_autoreloadonloglevelchange" = $false
    "new_logtoconsole" = $false
    "new_maxloghistory" = 100
    "new_enableremotelogging" = $true
    "new_remotelogonlyerrorswarnings" = $true
    "new_remotelogbatchsize" = 10
    "statecode" = 0
    "statuscode" = 1
}

# Use your preferred method to create the record
```

### JavaScript Console - Create Test Log
```javascript
Xrm.WebApi.createRecord("new_cmajavascriptlog", {
    new_name: "Test Log - Error - 2024-01-15",
    new_level: 1, // Error
    new_component: "TestComponent",
    new_message: "This is a test error message",
    new_timestamp: new Date().toISOString(),
    new_data: JSON.stringify({ test: true, errorCode: "TEST001" }),
    new_url: window.location.href,
    new_useragent: navigator.userAgent,
    "new_userid@odata.bind": "/systemusers(" + Xrm.Utility.getGlobalContext().userSettings.userId.replace(/[{}]/g, "") + ")"
}).then(result => {
    console.log("Test log created:", result.id);
});
```

---

## Security Roles Configuration

### Privileges Required

#### For End Users
```
new_cmaconfiguration:
- Read: Organization level

new_cmajavascriptlog:
- Create: User level
- Read: User level
- Write: User level (own records)
```

#### For Support/IT Team
```
new_cmaconfiguration:
- Read: Organization level

new_cmajavascriptlog:
- Read: Organization level
- Write: Organization level
- Delete: Organization level
```

#### For System Administrators
```
new_cmaconfiguration:
- All privileges: Organization level

new_cmajavascriptlog:
- All privileges: Organization level
```

---

## Field-Level Security (Optional)

Consider adding field-level security for:

### new_cmajavascriptlog.new_data
- **Reason:** May contain sensitive error information
- **Secured:** Yes
- **Profiles:** Only System Administrators and IT Support

### new_cmajavascriptlog.new_userid
- **Reason:** User privacy
- **Secured:** Yes
- **Profiles:** System Administrators, IT Support, and Managers

---

## Audit Settings

### Recommended Audit Configuration

#### new_cmaconfiguration
- **Audit Enabled:** Yes
- **Track Changes:** All fields
- **Reason:** Track who changes logging configuration

#### new_cmajavascriptlog
- **Audit Enabled:** No
- **Reason:** These are already audit logs; auditing them is redundant

---

## Data Retention Policy

### Recommended Settings

```
Entity: new_cmajavascriptlog
Retention Period: 90 days
Method: Power Automate Scheduled Flow

Flow Trigger: Daily at 2:00 AM
Flow Action: 
  1. List Rows (Created On older than 90 days)
  2. Delete Rows OR Set Status = Inactive
```

---

## Relationship Diagrams

```
new_cmaconfiguration (1) ─────────────── (0..1) Organization
    │
    │ (No direct relationships)
    │

systemuser (1) ─────────────── (0..*) new_cmajavascriptlog
    │
    │ Relationship: new_systemuser_cmajavascriptlog
    │ Type: 1:N
    │ Lookup Field: new_userid
```

---

## Testing Checklist

- [ ] Create new_cmaconfiguration entity with all fields
- [ ] Create new_cmajavascriptlog entity with all fields
- [ ] Create option sets with correct values
- [ ] Set up lookup relationship to systemuser
- [ ] Create all views
- [ ] Configure security roles
- [ ] Create sample configuration record
- [ ] Test with D365Client library
- [ ] Verify logs are created with user context
- [ ] Test Power Automate flows (if applicable)
- [ ] Set up data retention policy

---

## Notes

1. **Entity Naming:** Adjust the publisher prefix ("new_") to match your organization's prefix
2. **Option Set Values:** Keep the numeric values as specified for compatibility with D365Client
3. **Calculated Fields:** Browser Name field is optional but helpful for reporting
4. **Performance:** Index the following fields for better performance:
   - new_cmajavascriptlog.new_level
   - new_cmajavascriptlog.new_component
   - new_cmajavascriptlog.new_timestamp
   - new_cmajavascriptlog.new_userid
