import { BaseForm } from './BaseForm.js';
/*
    Each entity has a base form - this is where common logic can be placed for ALL forms.
    i.e., if you are doing phone number formatting that you want implemented consistently across all forms, put it here.

    By using the safeWireOnChange method from BaseForm, you can safely wire up onchange events to fields that may or may not be present on the form.
    This way, if the field is not present, no error will occur.

    Use the following prefixes for functions;

    OnChange_<FieldName> - for onchange events
    OnSave - for onsave events
    OnLoad - for onload events

    Example:
    OnChange_name - for the name field's onchange event
    OnSave - for the form's onsave event
    OnLoad - for the form's onload event

    You can also create other helper functions as needed.

*/
class AccountBaseForm extends BaseForm {
    initialize() {
        this.log("AccountBaseForm initializing...");

        //Wire up the handlers for event changes
        this.setupOnChangeIfFieldExists("telephone1", (ctx) => this.onChange_Phone(ctx));
        this.setupOnChangeIfFieldExists("telephone2", (ctx) => this.onChange_Phone(ctx));
        this.setupOnChangeIfFieldExists("fax", (ctx) => this.onChange_Phone(ctx));
        this.setupOnChangeIfFieldExists("emailaddress1", (ctx) => this.onChange_EmailAddress(ctx));

        //Fire events Onload if needed
        this.fireOnChangeIfExists("telephone1");
        this.fireOnChangeIfExists("telephone2");
        this.fireOnChangeIfExists("fax");
    }

    onChange_Phone(ctx) {
        this.log("Phone number changed: " + ctx.getEventSource().getValue());
        this.formatPhoneSimple(ctx.getEventSource().getName());
    }
}

/*
    This is an implementation of the CMA Form.
*/

class CMAConnectForm extends AccountBaseForm {
    initialize() {
        super.initialize();
        this.log("CMA Connect Form initializing...");

        this.setupOnChangeIfFieldExists("new_accounttypeid", (ctx) => this.OnChange_ShowHideCommitteeEventInformation(ctx));
        this.setupOnChangeIfFieldExists("new_accountsubtypeid", (ctx) => this.OnChange_ShowClearAccountSubType(ctx));

        this.fireOnChangeIfExists("new_accounttypeid");

        this.OnLoad_SetTypeFilters();
    }

    // ON LOAD: shows Subtype field if selected Type has subtypes.
    // ON CHANGE: Type. Additionally clears Subtype when Type is changed.
    OnChange_ShowClearAccountSubType(ctx, isFormLoad) {
        var formContext = ctx.getFormContext();
        var accountTypeValue = formContext.getAttribute("new_accounttypeid").getValue();

        if (accountTypeValue != null) {
            const typeId = accountTypeValue[0].id;
            Xrm.WebApi.retrieveMultipleRecords(
                "new_accounttype",
                `?$select=new_accounttypeid&$filter=_new_parenttypeid_value eq ${typeId}`
            ).then((result) => {
                formContext.getControl("new_accountsubtypeid").setVisible(result.entities.length > 0);
            });
        }

        if (!isFormLoad) {
            formContext.getAttribute("new_accountsubtypeid").setValue(null);
        }
    }

    // ON LOAD: checks user roles, then restricts type lookups by access field.
    // Disables form if not a create form and type or subtype is locked.
    OnLoad_SetTypeFilters() {
        const formContext = this.formContext;

        // 03_Membership Service Centre, System Admin
        const accountTypeEligibleRoles = [
            "8406f723-55d7-e111-9e3b-005056a04b45",
            "627090ff-40a3-4053-8790-584edc5be201",
        ];

        const userRoles = Xrm.Utility.getGlobalContext().userSettings.securityRoles;
        const hasRole = accountTypeEligibleRoles.some((roleId) => userRoles.includes(roleId));

        if (!hasRole) {
            this.FilterAccountTypesByAccessField("new_accounttypeid");
            this.FilterAccountTypesByAccessField("new_accountsubtypeid");

            const accountTypeLocked = formContext.getAttribute("new_accounttypelocked")?.getValue();
            const accountSubtypeLocked = formContext.getAttribute("new_accountsubtypelocked")?.getValue();
            const formType = formContext.ui.getFormType();

            if (formType !== 1 && (accountSubtypeLocked || accountTypeLocked)) {
                formContext.ui.controls.forEach((ctrl) => ctrl.setDisabled && ctrl.setDisabled(true));
            }
        }
    }

    // Helper for OnLoad_SetTypeFilters. Adds a pre-search filter to restrict access-controlled types.
    FilterAccountTypesByAccessField(field) {
        const control = this.formContext.getControl(field);
        if (!control) return;
        control.addPreSearch(() => {
            control.addCustomFilter(`<filter type='and'>
                <condition attribute='new_parenttypeid' operator='null'/>
                <filter type='or'>
                    <condition attribute='new_accesscontrollocked' operator='eq' value='0' />
                    <condition attribute='new_accesscontrollocked' operator='null' />
                </filter>
            </filter>`);
        });
    }

    OnChange_ShowHideCommitteeEventInformation(ctx) {
        const formContext = ctx.getFormContext();

        const accountTypeValue = formContext.getAttribute("new_accounttypeid").getValue();
        const typeName = accountTypeValue?.[0]?.name || "";

        // Handle Committee Information Section visibility
        const committeeInfoSection = formContext.ui.tabs
            .get("SUMMARY_TAB")
            ?.sections.get("SUMMARY_TAB_section_5");
        if (committeeInfoSection) {
            committeeInfoSection.setVisible(typeName.startsWith("Committee"));
        }

        // Handle Navigation Items visibility
        const eventNav = formContext.ui.navigation.items.get("nav_new_events_accountvenues");
        const committeeNav = formContext.ui.navigation.items.get("nav_new_account_event_Committee");

        if (eventNav) eventNav.setVisible(false);
        if (committeeNav) committeeNav.setVisible(false);

        if (typeName.startsWith("Event Location") && eventNav) {
            eventNav.setVisible(true);
        } else if (typeName.startsWith("Committee") && committeeNav) {
            committeeNav.setVisible(true);
        }
    }
}

class CMALLiteForm extends AccountBaseForm {
    initialize() {
        super.initialize();
        this.log("CMA Lite Form initializing...");

        this.OnLoad_SetPrimaryContactFilter();
    }

    // ON LOAD: filters Primary Contact lookup to only show Contacts with a Connection to this Account.
    OnLoad_SetPrimaryContactFilter() {
        const formContext = this.formContext;
        if (formContext.ui.getFormType() === 1) { return; } // exit on create form

        const acctId = formContext.data.entity.getId();
        const control = formContext.getControl("primarycontactid");
        if (!control) return;

        control.addPreSearch(() => {
            control.addCustomFilter(`<link-entity name="connection" from="record2id" to="contactid" link-type="inner" alias="ab">
            <filter type="and">
            <condition attribute="record1id" operator="eq" uitype="account" value="${acctId}"/>
            </filter>
            </link-entity>`);
        });
    }
}

class AdminForm extends AccountBaseForm {
    initialize() {
        super.initialize();
        this.log("Admin Form initializing...");
    }
}

class MSCForm extends AccountBaseForm {
    initialize() {
        super.initialize();
        this.log("MSC Form initializing...");

        this.setupOnChangeIfFieldExists("new_accounttypeid", (ctx) => this.OnChange_ToggleUniversityDetails(ctx));
        this.fireOnChangeIfExists("new_accounttypeid");
    }

    OnChange_ToggleUniversityDetails(ctx) {
        const formContext = ctx.getFormContext();

        const lookupAttr = formContext.getAttribute("new_accounttypeid");
        const lookupValue = lookupAttr ? lookupAttr.getValue() : null;

        const isUniversity = !!(
            lookupValue &&
            lookupValue.length > 0 &&
            lookupValue[0].name === "University / Université"
        );

        const section = formContext.ui.tabs
            .get("SUMMARY_TAB")
            ?.sections.get("university_details_section");
        if (section) {
            section.setVisible(isUniversity);
        }
    }
}

/*
    After you create a set of forms, you can add them to this object.
*/
const AccountForms = {
    "CMA Form": CMAConnectForm,
    "CMA Lite": CMALLiteForm,
    "Admin": AdminForm,
    "MSC Form": MSCForm,
    "MSC Form V2": MSCForm,
};

export { AccountBaseForm, CMALLiteForm, AdminForm, MSCForm, CMAConnectForm, AccountForms };
