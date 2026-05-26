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
        //this.setupOnChangeIfFieldExists("name", (ctx) => this.onNameChange(ctx));
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
    This is an implmeentation of the CMA Form.
*/

class CMAConnectForm extends AccountBaseForm {
    initialize() {
        super.initialize();
        this.log("CMA Connect Form initializing...");

        this.setupOnChangeIfFieldExists("new_accounttypeid", (ctx) => this.OnChange_ShowHideCommitteeEventInformation(ctx));
        this.setupOnChangeIfFieldExists("new_accountsubtypeid", (ctx) => this.OnChange_ShowClearAccountSubType(ctx));
        
        this.fireOnChangeIfExists("new_accounttypeid");
        
        this.OnChange_ShowHideCommitteeEventInformation(ctx);
        this.OnLoad_SetTypeFilters(ctx);
    }


    //ON LOAD: shows Subtype field if selected Type has subtypes. 
    //ON CHANGE: Type. Additionally clears Subtype when Type is changed
    OnChange_ShowClearAccountSubType(ctx, isFormLoad) {
        var formContext = ctx.getFormContext();
        const client = Hsl.WebApi.getClient(this.dynamicGlobalContext);
        var accountTypeValue = formContext.getAttribute("new_accounttypeid").getValue();

        //hide by default
        var showSubType = false;
        
        if (accountTypeValue != null) {
            //Queries for subtypes by selected Type. HSL Library dependency.		
            client.fetch(`<fetch>
                <entity name="new_accounttype">
                <attribute name="new_accounttypeid"/>
                <filter type="and">
                <condition attribute="new_parenttypeid" operator="eq" value="`+ accountTypeValue[0].id +`"/>
                </filter>
                </entity>
                </fetch>`).then(result => {
                //true if subtypes, false if none.
                    if (result.value.length > 0) {
                        showSubType = true;
                    }
                formContext.getControl("new_accountsubtypeid").setVisible(showSubType);
            });		
        }

        //WHY DO WE NEED THIS???? (GT)
        if (!isFormLoad || isFormLoad == null) {
            formContext.getAttribute("new_accountsubtypeid").setValue(null);
        }
    }

    //ON LOAD: checks user roles, then retrict type lookups by access field. Disable form if not create form && type or subtype is locked
    OnLoad_SetTypeFilters(ctx) 
    {
        var formContext = ctx.getFormContext();
        
        //03_Membership Service Centre, System Admin
        //GUIDs consistent across environments. See HSL Documentation https://docs.hitachisolutions.com/hsljslib/general/CurrentUser.html#RoleKeyIds
        var accountTypeEligibleRoles = new Array("8406f723-55d7-e111-9e3b-005056a04b45", "627090ff-40a3-4053-8790-584edc5be201");
        
        //returns true if user has any one of the specified roles. Dependency on HSL Library.
        Hsl.currentUser.hasRoles(accountTypeEligibleRoles).then(result => {
            if (!result) {
                //if user does not have any of the roles, then retrict type lookups by access field
                FilterTypeByAccessField(ctx,"new_accounttypeid");
                FilterTypeByAccessField(ctx,"new_accountsubtypeid");
                
                //disable form if not create form && type or subtype is locked
                var accountTypeLocked = formContext.getAttribute("new_accounttypelocked").getValue();
                var accountSubtypeLocked = formContext.getAttribute("new_accountsubtypelocked").getValue();
                var formType = formContext.ui.getFormType();  
                if (formType != 1 && (accountSubtypeLocked || accountTypeLocked)) {
                    const form = Hsl.form(ctx);
                    form.setDisabled([], true); //empty array means no exceptions to disabled form
                }
            }
        });
    }

    //helper function for SetTypeFilters. filter on access control. HSL Library dependency
    FilterAccountTypesByAccessField(ctx,field) 
    {
        const form = Hsl.form(ctx);
        const typeLookup = form.attribute(field);
        typeLookup.addPreSearchCustomViewFilter(() => {
            return `<filter type='and'>
                <condition attribute='new_parenttypeid' operator='null'/>
                    <filter type='or'>
                    <condition attribute='new_accesscontrollocked' operator='eq' value = '0' />
                    <condition attribute='new_accesscontrollocked' operator='null' />
                    </filter>
            </filter>`;
        });
    }

    /*
    This replaces the ShowHideCommiteeInformation method below. (but uses HSL)
    */

    // OnAccountTypeChangeToCommittee(ctx) 
    // {
    //     const form = new Hsl.Form(ctx);
    //     const typeField = form.getField("new_accounttypeid");
    //     const section = form.getSection("SUMMARY_TAB", "SUMMARY_TAB_section_5");

    //     const typeValue = typeField.getValue();

    //     if (typeValue && typeValue[0]?.name?.startsWith("Committee")) {
    //         section.show();
    //     } else {
    //         section.hide();
    //     }
    // }

    OnChange_ShowHideCommitteeEventInformation(ctx) 
    {
        const formContext = new Hsl.Form(ctx);
        
        // Get the Account Type lookup value once
        const accountTypeValue = formContext.getAttribute("new_accounttypeid").getValue();
        const typeName = accountTypeValue?.[0]?.name || "";
        
        // Handle Committee Information Section visibility
        const committeeInfoSection = formContext.ui.tabs.get("SUMMARY_TAB").sections.get("SUMMARY_TAB_section_5");
        if (committeeInfoSection) {
            committeeInfoSection.setVisible(typeName.startsWith("Committee"));
        }
        
        // Handle Navigation Items visibility
        const eventNav = formContext.getNavigationItem("nav_new_events_accountvenues");
        const committeeNav = formContext.getNavigationItem("nav_new_account_event_Committee");
        
        // Hide both navigation items by default
        if (eventNav) eventNav.hide();
        if (committeeNav) committeeNav.hide();
        
        // Show the appropriate navigation based on Account Type
        if (typeName.startsWith("Event Location") && eventNav) {
            eventNav.show();
        } else if (typeName.startsWith("Committee") && committeeNav) {
            committeeNav.show();
        }
    }
}

class CMALLiteForm extends AccountBaseForm {
    initialize() {
        super.initialize();
        this.log("CMA Lite Form initializing...");

        this.OnLoad_SetPrimaryContactFilter(ctx);
    }

    //ON LOAD: sets XML filter to Primary Contact lookup field to filter on only Contact records with Connection to Account. Dependency on HSL Library.
    OnLoad_SetPrimaryContactFilter(ctx) 
    {
        const form = Hsl.form(ctx);
        if (form.xrmForm.ui.getFormType() == 1) {return;} //exit if create form

        const acctId = form.getRecordId();
        const primaryContactLookup = form.attribute('primarycontactid');
        primaryContactLookup.addPreSearchCustomViewFilter(() => {
            return `<link-entity name="connection" from="record2id" to="contactid" link-type="inner" alias="ab">
            <filter type="and">
            <condition attribute="record1id" operator="eq" uitype="account" value="`+ acctId +`"/>
            </filter>
            </link-entity>`;
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
        this.log("Admin Form initializing...");

        this.setupOnChangeIfFieldExists("new_accountsubtypeid", (ctx) => this.OnLoad_toggleUniversityDetails(ctx));
    }

    OnLoad_toggleUniversityDetails(ctx) {
        const form = Hsl.form(ctx);

        const lookupAttr = form.attribute("new_accounttypeid");
        const lookupValue = lookupAttr ? lookupAttr.getValue() : null;

        const isUniversity = lookupValue && lookupValue.length > 0 &&
                            lookupValue[0].name === "University / Université";

        const tab = form.xrmForm.ui.tabs.get("SUMMARY_TAB");
        if (tab) {
            const section = tab.sections.get("university_details_section");
            if (section) {
                section.setVisible(isUniversity);
            }
        }
    }

}

/*
    After you create a set for forms, you can add them to this object.
*/
const AccountForms = {
    "CMA Form": CMAConnectForm,
    "CMA Lite": CMALLiteForm,
    "Admin": AdminForm,
    "MSC Form": MSCForm,
    "MSC Form V2": MSCForm,
};

export { CMALLiteForm, AdminForm,MSCForm, CMAConnectForm, AccountForms };
