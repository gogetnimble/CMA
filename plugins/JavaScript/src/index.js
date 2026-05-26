/*
    Add each new entity and the forms you are exporting.
*/
import './BaseForm.js';
//import { CMALLiteForm, CMAConnectForm, AccountForms } from './Account.js';
import { AccountForms } from './Account.js';

// =======================================
// GLOBAL EXPOSURE (Dynamics 365)
// =======================================
window.CMA = window.CMA || {};

/*
    The below must be added for each entity.
    The form name must match exactly what is shown in the form selector.
    The class name must match what is imported above.
*/

    window.CMA.Account = window.CMA.Account || {};
    window.CMA.Account.Forms = AccountForms;
    window.CMA.Account.OnLoad = function(executionContext) 
    {
    const formContext = executionContext.getFormContext();
    const formName = formContext.ui.formSelector?.getCurrentItem()?.getLabel();
    const FormClass = window.CMA.Account.Forms[formName];
  
    if (FormClass) 
    { new FormClass(formContext).initialize();
    }
    else {
        console.warn(`No form class registered for "${formName}"`);
    }
    };