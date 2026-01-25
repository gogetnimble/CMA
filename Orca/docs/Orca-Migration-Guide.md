# Orca 2.0 - Migration Guide & Summary

## 🎉 What's New in Orca 2.0

**Orca** is the rebranded and enhanced version of D365Client with a cleaner, more intuitive API.

### Key Changes

| Old (D365Client) | New (Orca) | Notes |
|------------------|------------|-------|
| `d365.webApi` | `orca.Service` | More semantic name |
| `d365.utility` | `orca.Utility` | Capitalized |
| `d365.notification` | `orca.Notification` | Capitalized |
| `d365.info()` | `orca.Log.Info()` | Organized under Log |
| `d365.warn()` | `orca.Log.Warn()` | Organized under Log |
| `d365.error()` | `orca.Log.Error()` | Organized under Log |
| `d365.debug()` | `orca.Log.Verbose()` | Renamed to Verbose |

---

## 🔄 Quick Migration

### Before (D365Client)

```javascript
const d365 = new D365Client();

// Logging
d365.info("MyApp", "Message");
d365.warn("MyApp", "Warning");
d365.error("MyApp", "Error");
d365.debug("MyApp", "Verbose info");

// WebAPI
await d365.webApi.createRecord("account", data);
await d365.webApi.retrieveRecord("account", id);
await d365.webApi.updateRecord("account", id, data);

// Utility
d365.utility.showProgressIndicator("Loading...");
await d365.utility.showAlert("Done!");
d365.utility.closeProgressIndicator();

// Notifications
d365.notification.setFormNotification(formContext, "Message", "INFO", "id");
```

### After (Orca 2.0)

```javascript
const orca = new Orca();

// Logging - Now capitalized and organized
orca.Log.Info("MyApp", "Message");
orca.Log.Warn("MyApp", "Warning");
orca.Log.Error("MyApp", "Error");
orca.Log.Verbose("MyApp", "Verbose info");  // ← Changed from debug

// Service - Renamed from webApi
await orca.Service.createRecord("account", data);
await orca.Service.retrieveRecord("account", id);
await orca.Service.updateRecord("account", id, data);

// Utility - Now capitalized
orca.Utility.showProgressIndicator("Loading...");
await orca.Utility.showAlert("Done!");
orca.Utility.closeProgressIndicator();

// Notification - Now capitalized
orca.Notification.setFormNotification(formContext, "Message", "INFO", "id");
```

---

## 🎯 API Comparison Table

### Logging

| D365Client 1.x | Orca 2.0 | Change |
|----------------|----------|--------|
| `d365.info(component, msg, data)` | `orca.Log.Info(component, msg, data)` | Capitalized, organized |
| `d365.warn(component, msg, data)` | `orca.Log.Warn(component, msg, data)` | Capitalized, organized |
| `d365.error(component, msg, data)` | `orca.Log.Error(component, msg, data)` | Capitalized, organized |
| `d365.debug(component, msg, data)` | `orca.Log.Verbose(component, msg, data)` | Renamed to Verbose |
| `d365.log(level, component, msg, data)` | `orca.log(level, component, msg, data)` | Unchanged (internal) |

### Service (WebAPI)

| D365Client 1.x | Orca 2.0 | Change |
|----------------|----------|--------|
| `d365.webApi.createRecord(...)` | `orca.Service.createRecord(...)` | webApi → Service |
| `d365.webApi.retrieveRecord(...)` | `orca.Service.retrieveRecord(...)` | webApi → Service |
| `d365.webApi.retrieveMultipleRecords(...)` | `orca.Service.retrieveMultipleRecords(...)` | webApi → Service |
| `d365.webApi.updateRecord(...)` | `orca.Service.updateRecord(...)` | webApi → Service |
| `d365.webApi.deleteRecord(...)` | `orca.Service.deleteRecord(...)` | webApi → Service |
| `d365.webApi.execute(...)` | `orca.Service.execute(...)` | webApi → Service |
| `d365.webApi.executeMultiple(...)` | `orca.Service.executeMultiple(...)` | webApi → Service |
| `d365.webApi.isAvailableOffline(...)` | `orca.Service.isAvailableOffline(...)` | webApi → Service |

### Utility

| D365Client 1.x | Orca 2.0 | Change |
|----------------|----------|--------|
| `d365.utility.*` | `orca.Utility.*` | Capitalized |
| All methods | All methods | Same signatures, just capitalized namespace |

### Notification

| D365Client 1.x | Orca 2.0 | Change |
|----------------|----------|--------|
| `d365.notification.*` | `orca.Notification.*` | Capitalized |
| All methods | All methods | Same signatures, just capitalized namespace |

### Configuration

| D365Client 1.x | Orca 2.0 | Change |
|----------------|----------|--------|
| `D365Client.LogLevel.DEBUG` | `Orca.LogLevel.VERBOSE` | DEBUG → VERBOSE |
| `D365Client.LogLevel.*` | `Orca.LogLevel.*` | Unchanged (except DEBUG) |
| All other constants | All other constants | Unchanged |

---

## 📝 Step-by-Step Migration

### Step 1: Update Imports/References

```javascript
// Find and replace in your codebase
D365Client → Orca
d365 → orca (variable names)
```

### Step 2: Update Logging Calls

```javascript
// Old
d365.info("Component", "Message");
d365.warn("Component", "Message");
d365.error("Component", "Message");
d365.debug("Component", "Message");

// New
orca.Log.Info("Component", "Message");
orca.Log.Warn("Component", "Message");
orca.Log.Error("Component", "Message");
orca.Log.Verbose("Component", "Message");
```

### Step 3: Update WebAPI Calls

```javascript
// Old
d365.webApi.createRecord(...)
d365.webApi.retrieveRecord(...)
d365.webApi.updateRecord(...)

// New
orca.Service.createRecord(...)
orca.Service.retrieveRecord(...)
orca.Service.updateRecord(...)
```

### Step 4: Update Utility Calls

```javascript
// Old
d365.utility.showAlert(...)
d365.utility.showProgressIndicator(...)

// New
orca.Utility.showAlert(...)
orca.Utility.showProgressIndicator(...)
```

### Step 5: Update Notification Calls

```javascript
// Old
d365.notification.setFormNotification(...)
d365.notification.showAlert(...)

// New
orca.Notification.setFormNotification(...)
orca.Notification.showAlert(...)
```

### Step 6: Update Log Level References

```javascript
// Old
D365Client.LogLevel.DEBUG
d365.setLogLevel(D365Client.LogLevel.DEBUG);

// New
Orca.LogLevel.VERBOSE
orca.setLogLevel(Orca.LogLevel.VERBOSE);
```

---

## 🔧 Automated Migration Script

Use this find-and-replace script:

```javascript
// Regular expression replacements (case-sensitive)
D365Client → Orca
\.webApi\. → .Service.
\.utility\. → .Utility.
\.notification\. → .Notification.
\.info\( → .Log.Info(
\.warn\( → .Log.Warn(
\.error\( → .Log.Error(
\.debug\( → .Log.Verbose(
LogLevel\.DEBUG → LogLevel.VERBOSE
```

---

## 💡 Why These Changes?

### 1. **More Semantic Names**
- `Service` is clearer than `webApi` for data operations
- `Log.Info()` is more organized than scattered logging methods

### 2. **Consistent Capitalization**
- All namespaces are capitalized (Service, Utility, Notification, Log)
- Matches C# conventions for better cross-platform consistency

### 3. **Better Organization**
- Logging methods grouped under `Log` namespace
- Clearer separation of concerns

### 4. **Industry Standard Terminology**
- `Verbose` is more standard than `Debug` for verbose logging
- Matches .NET, Java, and other logging frameworks

---

## 🎨 Real-World Migration Example

### Before: D365Client Contact Form

```javascript
var MyApp = {
    d365: null,

    onLoad: function(executionContext) {
        this.d365 = new D365Client();
        const formContext = executionContext.getFormContext();
        
        this.d365.info("ContactForm", "Form loaded");
        
        this.loadData(formContext);
    },

    loadData: async function(formContext) {
        this.d365.utility.showProgressIndicator("Loading...");
        
        try {
            const contactId = formContext.data.entity.getId();
            const options = "?$select=firstname,lastname,emailaddress1";
            
            const contact = await this.d365.webApi.retrieveRecord(
                "contact",
                contactId.replace(/[{}]/g, ""),
                options
            );
            
            this.d365.debug("ContactForm", "Data loaded", contact);
            
            this.d365.notification.setFormNotification(
                formContext,
                "Contact loaded successfully",
                "INFO",
                "load_success"
            );
        } catch (error) {
            this.d365.error("ContactForm", "Load failed", error);
            await this.d365.notification.showError("Failed to load contact", error);
        } finally {
            this.d365.utility.closeProgressIndicator();
        }
    },

    validateEmail: function(executionContext) {
        const formContext = executionContext.getFormContext();
        const emailAttr = formContext.getAttribute("emailaddress1");
        const email = emailAttr.getValue();

        if (email && !this.isValidEmail(email)) {
            this.d365.notification.setAttributeNotification(
                emailAttr,
                "Invalid email",
                "email_validation"
            );
            this.d365.warn("ContactForm", "Invalid email", { email });
        }
    },

    isValidEmail: function(email) {
        return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
    }
};
```

### After: Orca Contact Form

```javascript
var MyApp = {
    orca: null,  // ← Changed from d365

    onLoad: function(executionContext) {
        this.orca = new Orca();  // ← Changed from D365Client
        const formContext = executionContext.getFormContext();
        
        this.orca.Log.Info("ContactForm", "Form loaded");  // ← Changed
        
        this.loadData(formContext);
    },

    loadData: async function(formContext) {
        this.orca.Utility.showProgressIndicator("Loading...");  // ← Changed
        
        try {
            const contactId = formContext.data.entity.getId();
            const options = "?$select=firstname,lastname,emailaddress1";
            
            const contact = await this.orca.Service.retrieveRecord(  // ← Changed
                "contact",
                contactId.replace(/[{}]/g, ""),
                options
            );
            
            this.orca.Log.Verbose("ContactForm", "Data loaded", contact);  // ← Changed
            
            this.orca.Notification.setFormNotification(  // ← Changed
                formContext,
                "Contact loaded successfully",
                "INFO",
                "load_success"
            );
        } catch (error) {
            this.orca.Log.Error("ContactForm", "Load failed", error);  // ← Changed
            await this.orca.Notification.showError("Failed to load contact", error);  // ← Changed
        } finally {
            this.orca.Utility.closeProgressIndicator();  // ← Changed
        }
    },

    validateEmail: function(executionContext) {
        const formContext = executionContext.getFormContext();
        const emailAttr = formContext.getAttribute("emailaddress1");
        const email = emailAttr.getValue();

        if (email && !this.isValidEmail(email)) {
            this.orca.Notification.setAttributeNotification(  // ← Changed
                emailAttr,
                "Invalid email",
                "email_validation"
            );
            this.orca.Log.Warn("ContactForm", "Invalid email", { email });  // ← Changed
        }
    },

    isValidEmail: function(email) {
        return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
    }
};
```

---

## ✅ Testing Checklist

After migration, verify:

- [ ] All logging calls work
- [ ] WebAPI operations function correctly
- [ ] Utility methods work as expected
- [ ] Notifications display properly
- [ ] Remote logging captures errors (if enabled)
- [ ] Configuration loads from entity (if enabled)
- [ ] User context is captured
- [ ] No console errors
- [ ] Forms load and save correctly
- [ ] Custom actions execute successfully

---

## 📚 Documentation

**Updated Documentation Files:**
1. **Orca-QuickStart.md** - Quick reference and examples
2. **Orca-API-Reference.md** - Complete API documentation
3. **Orca.js** - Main library file (40KB)

**Legacy Documentation (D365Client):**
- Still available for reference
- All functionality preserved in Orca

---

## 🆕 New Features in Orca 2.0

Beyond the naming changes, Orca 2.0 includes:

1. ✨ **LogWrapper Class** - Organized logging namespace
2. 🎯 **Clearer API** - More intuitive method names
3. 📝 **Better Documentation** - Updated with new conventions
4. 🔄 **Backward Compatible** - Old D365Client still works alongside Orca

---

## ❓ FAQ

**Q: Can I use both D365Client and Orca in the same project?**  
A: Yes, they can coexist. However, we recommend migrating fully to Orca.

**Q: Will my existing configuration entity work?**  
A: Yes, both use the same configuration entity (`new_cmaconfiguration`).

**Q: Do I need to update my remote logging table?**  
A: No, both use the same logging table (`new_cmajavascriptlog`).

**Q: What about TypeScript definitions?**  
A: Updated TypeScript definitions are included with Orca.

**Q: Is there a performance difference?**  
A: No, performance is identical. Only naming has changed.

**Q: Can I gradually migrate?**  
A: Yes, you can migrate file by file. Both versions can run simultaneously.

---

## 🎉 Benefits of Orca 2.0

✅ **Cleaner API** - More intuitive and organized  
✅ **Better IntelliSense** - Capitalized names are easier to discover  
✅ **Industry Standard** - Follows .NET and JavaScript conventions  
✅ **Easier to Learn** - More logical organization  
✅ **Future-Proof** - Better foundation for future enhancements  

---

## 🚀 Next Steps

1. Review **Orca-QuickStart.md** for usage examples
2. Check **Orca-API-Reference.md** for complete API details
3. Test in a development environment first
4. Migrate one module at a time
5. Update your team's documentation

---

**Version:** Orca 2.0  
**Migration Difficulty:** Easy (mostly find-and-replace)  
**Estimated Migration Time:** 15-30 minutes per form/module  
**Backward Compatibility:** Full (D365Client still available)
