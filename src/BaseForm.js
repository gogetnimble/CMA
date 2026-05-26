/*
    Every form inherits from this base class.
    It provides common functionality and utilities for all forms.

    Common Functionality
    ===========================================================================================================
    Logging --> log, warn, error

    Re-used Acrossed Entities Functionality
    ===========================================================================================================
    validateEmail: Validates an email address using a regular expression.
    getAttr: Safely retrieves an attribute from the form context.
    validateNumberInput: Validates numeric input within a text field.
    validateNumberBoxCtx: Validates numeric input within a text field and shows an alert if invalid.
    formatPhoneSimple: Formats a phone number using a simple algorithm (requires HSL library).
    
    HSL Specific
    ===========================================================================================================
    dynamicsVersionForHsl: The version of Dynamics 365 to use with the HSL library.

    Onload Helper functions --> These remove the need to inidividually wire up things from the User Interface
    ===========================================================================================================
    setupOnChangeIfFieldExists: Safely wires an onchange event handler to a field if the field exists on the form.
    fireOnChangeIfExists: Safely fires the onchange event for a field if it exists on the form.


*/
export class BaseForm {

    static dynamicsVersionForHsl = "9.1";

    constructor(formContext) {
        this.formContext = formContext;
    }

    /*
        Logs a message to the console with the entity name as a prefix.
        Parameters:
        - message: The message to log.
    */  
    log(message) {
        console.log(`INFO | [${this.formContext.data.entity.getEntityName()}] ${message}`);
    }

    warn(message) {
        console.log(`WARN | [${this.formContext.data.entity.getEntityName()}] ${message}`);
    }    

    /*
        Logs an error message to the console with the entity name as a prefix.
        Parameters:
        - message: The error message to log.
        - ex: The exception object (optional).
    */
    error(message, ex) {
        console.log(`ERROR| [${this.formContext.data.entity.getEntityName()}] ${message}, Error: ${ex}`);
    }    

    getAttr(name) {
        return this.formContext.getAttribute(name) || null;
    }

    /* 
        Validates an email address using a regular expression.
    */
    onChange_EmailAddress(email) {
        return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
    }

    /*
        Safely wires an onchange event handler to a field if the field exists on the form.
        Parameters:
        - formContext: The form context object.
        - fieldName: The schema name of the field to which the onchange event handler should be added.
        - handler: The function to be called when the field's value changes.
    */
    setupOnChangeIfFieldExists(fieldName, handler) {
        if (!this.formContext) {
            this.warn("Form context is missing");
            return;
        }

        const attr = this.formContext.getAttribute(fieldName);

        if (!attr) {
            this.warn(`⚠ Field '${fieldName}' not present on this form.`);
            return;
        }

        attr.addOnChange((eventContext) => {
            // Always pass the eventContext so the handler can use getEventSource()
            handler(eventContext);
        });

        this.log(`✔ OnChange wired for ${fieldName}`);
    }

    fireOnChangeIfExists(fieldName) 
    {
        const attr = this.formContext?.getAttribute(fieldName);
        if (!attr) {
            this.warn(`⚠ Field '${fieldName}' not present on this form. Skipping fireOnChange.`);
            return;
        }
        attr.fireOnChange();
        this.log(`✔ fireOnChange executed for ${fieldName}`);
    }

    // ON CHANGE: Parameter field schema name e.g. "telephone1". Formats a North American phone number.
    formatPhoneSimple(fieldName) {
        const attr = this.formContext.getAttribute(fieldName);
        if (!attr) return;
        const raw = attr.getValue();
        if (!raw) return;
        const digits = String(raw).replace(/\D/g, '');
        if (digits.length === 10) {
            attr.setValue(`(${digits.slice(0, 3)}) ${digits.slice(3, 6)}-${digits.slice(6)}`);
        } else if (digits.length === 11 && digits[0] === '1') {
            attr.setValue(`(${digits.slice(1, 4)}) ${digits.slice(4, 7)}-${digits.slice(7)}`);
        }
    }

    // //ON CHANGE: Parameter field schema name e.g. "telephone1". Applies a phone formatting algorithm. Dependency on HSL Library. See HSL documentation.
    // formatPhoneAggressive(e, fieldName) 
    // {
    //     const form = Hsl.form(e);
    //     Hsl.Formatter.phoneAggressive(form, fieldName);
    // }

    //ON CHANGE: validates numberic input within text field.
    validateNumberInput(executionContext, fieldname) 
    {
        var formContext = executionContext.getFormContext();
        if (formContext.getAttribute(fieldname).getValue() != null) {
            var input = formContext.getAttribute(fieldname).getValue();
            if (!input.match(/^\d+$/)) {
                //Xrm.Navigation.openAlertDialog({text: getMessageByCode('8025')});
                //clear the field
                formContext.getAttribute(fieldname).setValue(null);
            }
        }
    }

    /*
         Retrieves a localized message from a custom entity based on the user's language settings.
       Parameters:
       - code: The code of the message to retrieve.
       Returns:
       - A promise that resolves to the localized message string.
    */
    // getMessageByCodeFromCodeLookup(code) 
    // {
    //     return new Promise((resolve, reject) => {
    //         const client = Hsl.WebApi.getClient(dynamicsVersionForHsl);
    //         var userSettings = Xrm.Utility.getGlobalContext().userSettings;

    //         client.retrieve({ entityType: 'new_codelookup', new_code: code }, ['new_englishmessage', 'new_frenchmessage'])
    //             .then(resp => {
    //                 const englishMessage = resp.getValue("new_englishmessage");
    //                 const frenchMessage = resp.getValue("new_frenchmessage");

    //                 const message = userSettings.languageId + "" === "1036" ? frenchMessage : englishMessage;
    //                 resolve(message);
    //             })
    //             .catch(error => {
    //                 Hsl.Dialog.showError(error);
    //                 reject(error);
    //             });
    //     });
    // }


    validateNumberBoxCtx(executionContext, fieldname)
    {
        var formContext = executionContext.getFormContext();
        var input = formContext.getAttribute(fieldname).getValue();
        if (!input.match(/^\d+$/)) {
            var alertStrings = { confirmButtonLabel: 'OK', text: 'Please enter a valid number.', title: 'Error' };
            Xrm.Navigation.openAlertDialog(alertStrings);
            formContext.getAttribute(fieldname).setValue(null);
        }
    }
}