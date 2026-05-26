// tests/mockHsl.js
export const Hsl = {
  Formatter: {
        phoneSimple: (formContext, fieldName) => {
            const attr = formContext.getAttribute(fieldName);
            if (!attr) return;

            let val = attr.getValue();
            if (!val) return;

            // Strip non-digits
            val = val.replace(/\D/g, "");

            // Format as (XXX) XXX-XXXX
            if (val.length === 10) {
                val = `(${val.slice(0,3)}) ${val.slice(3,6)}-${val.slice(6,10)}`;
            }

            attr.setValue(val);
        },
        phoneAggressive: (formContext, fieldName) => {
            // Could implement a more strict formatting if needed
            Hsl.Formatter.phoneSimple(formContext, fieldName);
        }
  },
  form: (ctx) => ctx,
  WebApi: {
    getClient: () => ({
      retrieve: () => Promise.resolve({
        getValue: (field) => `mocked ${field}`
      })
    })
  },
  Dialog: {
    showError: (msg) => console.error(`[Mock Hsl Dialog] ${msg}`)
  }
};
