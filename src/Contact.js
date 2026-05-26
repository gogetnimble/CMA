import { BaseForm } from './BaseForm.js';

class ContactBaseForm extends BaseForm {
    initialize() {
        this.log("ContactBaseForm initializing...");
        this.getAttr("firstname")?.addOnChange(this.onFirstNameChange.bind(this));
    }

    onFirstNameChange(ctx) {
        this.log("First name changed: " + ctx.getEventSource().getValue());
    }
}

class ContactMainForm extends ContactBaseForm {
    initialize() {
        super.initialize();
        this.log("Main Form extra setup for Contact...");
    }
}

// Expose entry point for Dynamics
if (typeof window !== "undefined") {
    window.CMA = window.CMA || {};
    window.CMAContact_Main_OnLoad = function (ctx) {
        new ContactMainForm(ctx.getFormContext()).initialize();
    };
}

export { ContactBaseForm, ContactMainForm };
