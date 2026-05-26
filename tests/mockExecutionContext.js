// Mock an attribute for Dynamics 365
// getFormContext is an optional callback so fireOnChange can supply a full execution context.
export function createMockAttribute(name, initialValue = null, getFormContext = null) {
    let value = initialValue;
    const onChangeHandlers = [];

    return {
        getName: () => name,
        getValue: () => value,
        setValue: (newValue) => { value = newValue; },
        addOnChange: (handler) => onChangeHandlers.push(handler),
        fireOnChange: () => {
            onChangeHandlers.forEach((h) =>
                h({
                    getEventSource: () => ({ getValue: () => value, getName: () => name }),
                    getFormContext: getFormContext || (() => null),
                })
            );
        },
    };
}

// Mock a form control (visibility, pre-search, custom filters, disabled state)
export function createMockControl(name) {
    let visible = true;
    let disabled = false;
    const preSearchHandlers = [];
    const customFilters = [];

    return {
        getName: () => name,
        getVisible: () => visible,
        setVisible: (val) => { visible = val; },
        setDisabled: (val) => { disabled = val; },
        getDisabled: () => disabled,
        addPreSearch: (handler) => preSearchHandlers.push(handler),
        addCustomFilter: (filter) => customFilters.push(filter),
        getCustomFilters: () => [...customFilters],
        triggerPreSearch: () => preSearchHandlers.forEach((h) => h()),
    };
}

// Mock a form section (visibility)
export function createMockSection(name) {
    let visible = true;
    return {
        getName: () => name,
        getVisible: () => visible,
        setVisible: (val) => { visible = val; },
    };
}

// Mock a form tab containing named sections
export function createMockTab(sectionNames = []) {
    const sections = {};
    for (const name of sectionNames) {
        sections[name] = createMockSection(name);
    }
    return {
        sections: { get: (name) => sections[name] || null },
    };
}

// Mock a navigation item (visibility)
export function createMockNavItem(name) {
    let visible = true;
    return {
        getName: () => name,
        getVisible: () => visible,
        setVisible: (val) => { visible = val; },
    };
}

/*
    Mock the form context.
    attributes: { fieldName: initialValue, ... }
    options:
      entityName  – defaults to "account"
      entityId    – defaults to "mock-entity-id"
      formLabel   – defaults to "CMA Connect"
      formType    – 1 = create, 2 = update (default 2)
      controls    – extra control names beyond those in attributes []
      tabs        – { tabName: [sectionName, ...], ... }
      navItems    – [navItemName, ...]
*/
export function createMockFormContext(attributes = {}, options = {}) {
    // formContextRef is populated after the object is constructed so attribute
    // fireOnChange calls can include a valid getFormContext() reference.
    let formContextRef = null;
    const getFormContext = () => formContextRef;

    const attrObjects = {};
    for (const key of Object.keys(attributes)) {
        attrObjects[key] = createMockAttribute(key, attributes[key], getFormContext);
    }

    // Controls mirror attributes by default plus any extras
    const controls = {};
    for (const key of Object.keys(attributes)) {
        controls[key] = createMockControl(key);
    }
    for (const name of (options.controls || [])) {
        controls[name] = createMockControl(name);
    }

    // Tabs / sections
    const tabs = {};
    for (const [tabName, sectionNames] of Object.entries(options.tabs || {})) {
        tabs[tabName] = createMockTab(sectionNames);
    }

    // Navigation items
    const navItems = {};
    for (const name of (options.navItems || [])) {
        navItems[name] = createMockNavItem(name);
    }

    const formContext = {
        data: {
            entity: {
                getEntityName: () => options.entityName || 'account',
                getId: () => options.entityId || 'mock-entity-id',
            },
        },
        getAttribute: (name) => attrObjects[name] || null,
        getControl: (name) => controls[name] || null,
        ui: {
            formSelector: {
                getCurrentItem: () => ({ getLabel: () => options.formLabel || 'CMA Connect' }),
            },
            getFormType: () => options.formType || 2,
            tabs: {
                get: (name) => tabs[name] || null,
            },
            navigation: {
                items: {
                    get: (name) => navItems[name] || null,
                },
            },
            controls: {
                forEach: (fn) => Object.values(controls).forEach(fn),
            },
        },
    };

    formContextRef = formContext;
    return formContext;
}

// Mock execution context
export function createMockExecutionContext(entityName = 'account', attributes = {}, options = {}) {
    const formContext = createMockFormContext(attributes, { entityName, ...options });
    return {
        getFormContext: () => formContext,
    };
}

// Mock global Xrm object for tests that call Xrm.WebApi / Xrm.Utility / Xrm.Navigation
export function createMockXrm(options = {}) {
    return {
        WebApi: {
            retrieveMultipleRecords: (_entityType, _query) =>
                Promise.resolve({ entities: options.webApiEntities || [] }),
        },
        Utility: {
            getGlobalContext: () => ({
                userSettings: {
                    securityRoles: options.userRoles || [],
                },
            }),
        },
        Navigation: {
            openAlertDialog: () => Promise.resolve(),
        },
    };
}
