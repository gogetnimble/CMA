// Mock an attribute for Dynamics 365
export function createMockAttribute(name, initialValue = null) {
  let value = initialValue;
  const onChangeHandlers = [];

  return {
    getName: () => name,
    getValue: () => value,
    setValue: (newValue) => { value = newValue; },
    addOnChange: (handler) => onChangeHandlers.push(handler),
    fireOnChange: () => {
      onChangeHandlers.forEach((h) => h({ getEventSource: () => ({ getValue: () => value, getName: () => name }) }));
    }
  };
}

// Mock the form context
export function createMockFormContext(attributes = {}) {
  const attrObjects = {};
  for (const key of Object.keys(attributes)) {
    attrObjects[key] = createMockAttribute(key, attributes[key]);
  }
  return {
    data: { entity: { getEntityName: () => "account" } },
    getAttribute: (name) => attrObjects[name] || null,
    ui: { formSelector: { getCurrentItem: () => ({ getLabel: () => "CMA Connect" }) } }
  };
}

// Mock execution context
export function createMockExecutionContext(entityName = "account", attributes = {}) {
  const formContext = createMockFormContext(attributes);
  return {
    getFormContext: () => formContext
  };
}
