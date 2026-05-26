using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Messages;
using System.Net.Http;

namespace CreateContactforOnlineApplication
{
    public class CreateContactforOnlineApplication : CodeActivity
    {
       
        const string CONTACTTYPE_PHYSICIAN_ID = "CT1017";
        #region Input/Output Arguments        
        #endregion

        protected override void Execute(CodeActivityContext context)
        {            
            // Obtain the organization service reference.
            var workflowContext = context.GetExtension<IWorkflowContext>();
            var serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
            var service = serviceFactory.CreateOrganizationService(workflowContext.UserId);
            ITracingService ts = context.GetExtension<ITracingService>();

            EntityReference onlineApp = new EntityReference("new_onlineapplication", workflowContext.PrimaryEntityId);

            Entity application = service.Retrieve(onlineApp.LogicalName, onlineApp.Id, new ColumnSet("new_applicationsource"
                , "new_parentcustomerid", "new_firstname", "new_lastname", "new_salutationcode", "new_preferredname", "new_birthdate"
                , "new_mobilephone", "new_telephone1", "new_businessphoneextension", "new_mincnum", "new_provincenumber", "new_royalcollegenumber"
                , "new_familyphysiciannumber", "new_careerstatustypeid", "new_practicestatus", "new_canadianarmedforcesmember"
                , "new_collegeofgraduationid", "new_satellitecampusesid", "new_yearenrolledinmedicalschool", "new_gradyear"
                , "new_universityofresidencyid", "new_residencyprogram", "new_yearlicensed", "new_specialtyid", "new_fellowship"
                , "new_residencycompletion", "new_fellowship_estcompdate", "new_dateretirement", "new_contactpreferredlanguage"
                , "new_preferredemail", "new_licensingassocaccountid", "new_line1", "new_line2", "new_city", "new_countrycode"
                , "new_othercountry", "new_provincestatecode", "new_postalcode", "new_addresstypecode"
));
            if (application != null)
            {
                
                OptionSetValue applicationSource = application.GetAttributeValueEx<OptionSetValue>("new_applicationsource", ts, "app.");

                EntityReference parentCustomer = application.GetAttributeValueEx<EntityReference>("new_parentcustomerid", ts, "app.");
                if (parentCustomer != null)
                    throw new InvalidPluginExecutionException("Online Application 'Parent Customer' field  is not NULL");

                #region Create new contact
                Entity newContact = new Entity("contact");
                string firstname = application.GetAttributeValueEx<string>("new_firstname", ts, "app.");
                if (!string.IsNullOrEmpty(firstname))
                    newContact.SetAttributeValue("firstname", firstname);

                string lastname = application.GetAttributeValueEx<string>("new_lastname", ts, "app.");
                if (!string.IsNullOrEmpty(lastname))
                    newContact.SetAttributeValue("lastname", lastname);

                OptionSetValue salutation = application.GetAttributeValueEx<OptionSetValue>("new_salutationcode", ts, "app.");
                if (salutation != null)
                {
                    newContact.SetAttributeValue("new_salutation", salutation);
                    newContact.SetAttributeValue("new_excludesalutation", false );
                }
                else
                {
                    newContact.SetAttributeValue("new_excludesalutation", true);
                    newContact.SetAttributeValue("new_salutation", null);
                }


                    string preferredName = application.GetAttributeValueEx<string>("new_preferredname", ts, "app.");
                if (!string.IsNullOrEmpty(firstname))
                    newContact.SetAttributeValue("new_physicianpreferredname", preferredName);

                DateTime? dateOfBirth = application.GetAttributeValueEx<DateTime>("new_birthdate", ts, "app.");
                if (dateOfBirth != null)
                    newContact.SetAttributeValue("birthdate", dateOfBirth);

                string mobilephone = application.GetAttributeValueEx<string>("new_mobilephone", ts, "app.");
                if (!string.IsNullOrEmpty(mobilephone))
                    newContact.SetAttributeValue("mobilephone", mobilephone);

                string businessphone = application.GetAttributeValueEx<string>("new_telephone1", ts, "app.");
                if (!string.IsNullOrEmpty(businessphone))
                    newContact.SetAttributeValue("telephone1", businessphone);

                string businessphoneext = application.GetAttributeValueEx<string>("new_businessphoneextension", ts, "app.");
                if (!string.IsNullOrEmpty(businessphoneext))
                    newContact.SetAttributeValue("new_businessphoneextension", businessphoneext);

                string new_mincnum = application.GetAttributeValueEx<string>("new_mincnum", ts, "app.");
                if (!string.IsNullOrEmpty(new_mincnum))
                    newContact.SetAttributeValue("new_mincnum", new_mincnum);

                string provincenumber = application.GetAttributeValueEx<string>("new_provincenumber", ts, "app.");
                if (!string.IsNullOrEmpty(provincenumber))
                    newContact.SetAttributeValue("new_provincenumber", provincenumber);

                string royalcollegenumber = application.GetAttributeValueEx<string>("new_royalcollegenumber", ts, "app.");
                if (!string.IsNullOrEmpty(royalcollegenumber))
                    newContact.SetAttributeValue("new_royalcollegenumber", royalcollegenumber);

                string familyphysiciannumber = application.GetAttributeValueEx<string>("new_familyphysiciannumber", ts, "app.");
                if (!string.IsNullOrEmpty(familyphysiciannumber))
                    newContact.SetAttributeValue("new_familyphysiciannumber", familyphysiciannumber);

                Entity physicianType = CrmHelpers.SearchFor(service, "new_contacttype", "new_contacttype_id", CONTACTTYPE_PHYSICIAN_ID, new string[] { "new_contacttypeid" });
                if (physicianType != null)
                    newContact.SetAttributeValue("new_contacttypeid", physicianType.ToEntityReference());

                EntityReference careerStatus = application.GetAttributeValueEx<EntityReference>("new_careerstatustypeid", ts, "app.");
                if (careerStatus != null)
                    newContact.SetAttributeValue("new_contactsubtypeid", careerStatus);

                //TODO practive status not mapped ??????
                OptionSetValue practicestatus = application.GetAttributeValueEx<OptionSetValue>("new_practicestatus", ts, "app.");
                if (practicestatus != null)
                {
                    if (practicestatus.Value == (int)ApplicationPracticeStatus.PracticingPhysician ||
                        practicestatus.Value == (int)ApplicationPracticeStatus.FirstYearInPractice ||
                        practicestatus.Value == (int)ApplicationPracticeStatus.Locum )
                    {
                        newContact.SetAttributeValue("new_practicestatus", new OptionSetValue((int)ContactPracticeStatus.InPractice));
                        if (practicestatus.Value == (int)ApplicationPracticeStatus.FirstYearInPractice)
                            newContact.SetAttributeValue("new_1styearinpractice", new DateTime(DateTime.Now.Year, 1, 1));
                        else if (practicestatus.Value == (int)ApplicationPracticeStatus.Locum)
                            newContact.SetAttributeValue("new_locum", new OptionSetValue(100000000)); //yes this is the option set value for yes
                    }
                    else if (practicestatus.Value == (int)ApplicationPracticeStatus.MaternityLeave)
                        newContact.SetAttributeValue("new_practicestatus", new OptionSetValue((int)ContactPracticeStatus.MaternityLeave));
                    else if (practicestatus.Value == (int)ApplicationPracticeStatus.NonPracticingAcademic)
                        newContact.SetAttributeValue("new_practicestatus", new OptionSetValue((int)ContactPracticeStatus.NonPracticingAcademic));
                    else if (practicestatus.Value == (int)ApplicationPracticeStatus.PartTime)
                        newContact.SetAttributeValue("new_practicestatus", new OptionSetValue((int)ApplicationPracticeStatus.PartTime));                    
                    else if (practicestatus.Value == (int)ApplicationPracticeStatus.Retired)
                        newContact.SetAttributeValue("new_practicestatus", new OptionSetValue((int)ContactPracticeStatus.Retired));
                }

                OptionSetValue armedforcesmember = application.GetAttributeValueEx<OptionSetValue>("new_canadianarmedforcesmember", ts, "app.");
                newContact.SetAttributeValue("new_canadianarmedforcesmember", (armedforcesmember == null ? false : armedforcesmember.Value == 1 ? true : false ) );

                EntityReference univeristyofgrad = application.GetAttributeValueEx<EntityReference>("new_collegeofgraduationid", ts, "app.");
                if (univeristyofgrad != null)
                    newContact.SetAttributeValue("new_collegeofgraduation", univeristyofgrad);

                EntityReference satellitecampus = application.GetAttributeValueEx<EntityReference>("new_satellitecampusesid", ts, "app.");
                if (satellitecampus != null)
                    newContact.SetAttributeValue("new_satellitecampuses", satellitecampus);

                OptionSetValue yearenrolled = application.GetAttributeValueEx<OptionSetValue>("new_yearenrolledinmedicalschool", ts, "app.");
                if (yearenrolled != null)
                    newContact.SetAttributeValue("new_yearenrolledinmedicalschool", new DateTime(yearenrolled.Value, 1, 1));

                OptionSetValue gradyear = application.GetAttributeValueEx<OptionSetValue>("new_gradyear", ts, "app.");
                if (gradyear != null)
                    newContact.SetAttributeValue("new_gradyear", gradyear.Value.ToString());

                EntityReference universityofresidency = application.GetAttributeValueEx<EntityReference>("new_universityofresidencyid", ts, "app.");
                if (universityofresidency != null)
                    newContact.SetAttributeValue("new_universityofresidency", universityofresidency);

                OptionSetValue residencyprogram = application.GetAttributeValueEx<OptionSetValue>("new_residencyprogram", ts, "app.");
                if (residencyprogram != null)
                    newContact.SetAttributeValue("new_residencyprogram", residencyprogram);
                
                OptionSetValue yearlicensed = application.GetAttributeValueEx<OptionSetValue>("new_yearlicensed", ts, "app.");
                if (yearlicensed != null)
                    newContact.SetAttributeValue("new_yearlicensed", yearlicensed.Value.ToString());

                Entity specialty = null;
                EntityReference specialtyRef = application.GetAttributeValueEx<EntityReference>("new_specialtyid", ts, "app.");
                if (specialtyRef != null)
                {
                    newContact.SetAttributeValue("new_specialtyid", specialtyRef);
                    specialty = service.Retrieve("new_specialty", specialtyRef.Id, new ColumnSet("new_certifyingbody", "new_type", "new_english", "new_french", "new_name"));
                }
                //Fellowship     
                bool isFellowship = false;
                if (application.Contains("new_fellowship"))
                {
                    OptionSetValue fellowship = application.GetAttributeValueEx<OptionSetValue>("new_fellowship", ts, "app.");
                    newContact.SetAttributeValue("new_fellowship", fellowship?.Value == 0 ? new OptionSetValue(100000001) : new OptionSetValue(100000000));  // yes is optionset value 100000000 
                    isFellowship = fellowship?.Value == 1;
                }


                //Fellowship/Residency Completion Date    
                OptionSetValue residencyyear = application.GetAttributeValueEx<OptionSetValue>("new_residencycompletion", ts, "app.");
                DateTime? fellowshipDate = application.GetAttributeValueEx<DateTime?>("new_fellowship_estcompdate", ts, "app.");

                if (applicationSource?.Value == (int)ApplicationSource.Portal && residencyyear != null )
                {
                    //from portal, if fellowship is yes, fellowship completion date comes from residency completion year 
                    if (isFellowship)
                        newContact.SetAttributeValue("new_fellowship_estcompdate", new DateTime(residencyyear.Value, 12, 31, 23, 59, 59));
                    else
                        newContact.SetAttributeValue("new_residencycompletion", residencyyear.Value.ToString());
                }
                else //non portal
                {
                    if (residencyyear != null)
                        newContact.SetAttributeValue("new_residencycompletion", residencyyear.Value.ToString());
                    if (fellowshipDate != null)
                        newContact.SetAttributeValue("new_fellowship_estcompdate", fellowshipDate.Value);
                }

                //Date of Retirement                  
                DateTime? retirementDate = application.GetAttributeValueEx<DateTime?>("new_dateretirement", ts, "app.");
                if (retirementDate != null)
                    newContact.SetAttributeValue("new_dateretirement", retirementDate.Value);

                OptionSetValue language = application.GetAttributeValueEx<OptionSetValue>("new_contactpreferredlanguage", ts, "app.");
                if (language != null)
                    newContact.SetAttributeValue("new_language", new OptionSetValue(language.Value));

                string email = application.GetAttributeValueEx<string>("new_preferredemail", ts, "app.");
                if (!string.IsNullOrEmpty(email))
                    newContact.SetAttributeValue("emailaddress1", email);

                EntityReference licensingaccount  = application.GetAttributeValueEx<EntityReference>("new_licensingassocaccountid", ts, "app.");
                if (licensingaccount != null )
                    newContact.SetAttributeValue("new_licensingassocaccountid", licensingaccount );

                //mark this as created froim application 
                newContact.SetAttributeValue("new_createdviaapplication", new OptionSetValue(1) );
                //create it 
                CrmHelpers.TraceAttributeCollection(service, newContact, ts);
                Guid newContactId = service.Create(newContact);

                #endregion Create new contact

                if (newContactId != Guid.Empty)
                {
                    #region Create new Physician Specialty
                    if (specialty != null)
                    {
                        Entity newPhysicianSpecialty = new Entity("new_physicianspecialty");
                        newPhysicianSpecialty.SetAttributeValue("new_contactid", new EntityReference("contact", newContactId));
                        newPhysicianSpecialty.SetAttributeValue("new_specialtyid", specialtyRef);

                        //Specialty Cert. Body is Option set but Physician Specialty.Cert.Body  is Lookup
                        //OptionSetValue certBody = specialty.GetAttributeValueEx<OptionSetValue>("new_certifyingbody", ts, "specialty.");
                        //if( certBody != null )
                        //    newPhysicianSpecialty.SetAttributeValue("new_specialtyid", specialtyRef);

                        OptionSetValue specialtyType = specialty.GetAttributeValueEx<OptionSetValue>("new_type", ts, "specialty.");
                        if (specialtyType != null)
                            newPhysicianSpecialty.SetAttributeValue("new_specialtytypecode", specialtyType);

                        string specialtyName = specialty.GetAttributeValueEx<string>("new_name", ts, "specialty.");
                        if (specialtyName != null)
                            newPhysicianSpecialty.SetAttributeValue("new_name", specialtyName);
                        newPhysicianSpecialty.SetAttributeValue("new_primary", true);

                        //create it 
                        CrmHelpers.TraceAttributeCollection(service, newPhysicianSpecialty, ts);
                        service.Create(newPhysicianSpecialty);
                    }
                    #endregion Create new Physician Specialty

                    #region Create address                
                    Entity newAddress = new Entity("customeraddress");
                    newAddress.SetAttributeValue("parentid", new EntityReference("contact", newContactId));
                    newAddress.SetAttributeValue("addressnumber", 4);

                    string line1 = application.GetAttributeValueEx<string>("new_line1", ts, "app.");
                    if (!string.IsNullOrWhiteSpace(line1))
                        newAddress.SetAttributeValue("line1", line1);

                    string line2 = application.GetAttributeValueEx<string>("new_line2", ts, "app.");
                    if (!string.IsNullOrWhiteSpace(line2))
                        newAddress.SetAttributeValue("line2", line2);

                    string city = application.GetAttributeValueEx<string>("new_city", ts, "app.");
                    if (!string.IsNullOrWhiteSpace(city))
                        newAddress.SetAttributeValue("city", city);

                    OptionSetValue countrycode = application.GetAttributeValueEx<OptionSetValue>("new_countrycode", ts, "app.");
                    OptionSetValue othercountry = application.GetAttributeValueEx<OptionSetValue>("new_othercountry", ts, "app.");
                    if (countrycode != null)
                    {
                        int? code = (countrycode.Value == (int)AppCountryCode.Other) ? othercountry?.Value : countrycode.Value;
                        if (code != null)
                        {
                            newAddress.SetAttributeValue("new_country", new OptionSetValue(code.Value));
                            newAddress.SetAttributeValue("country", CrmHelpers.GetOptionSetLabelByValue(service, "customeraddress", "new_country", code.Value));
                        }
                    }

                    OptionSetValue province = application.GetAttributeValueEx<OptionSetValue>("new_provincestatecode", ts, "app.");
                    if (province != null)
                    {
                        newAddress.SetAttributeValue("new_stateorprovincemasterlist", province);
                        newAddress.SetAttributeValue("stateorprovince", CrmHelpers.GetOptionSetLabelByValue(service, "customeraddress", "new_stateorprovincemasterlist", province.Value));
                    }

                    string postalcode = application.GetAttributeValueEx<string>("new_postalcode", ts, "app.");
                    if (!string.IsNullOrWhiteSpace(line1))
                        newAddress.SetAttributeValue("postalcode", postalcode);

                    OptionSetValue addressType = application.GetAttributeValueEx<OptionSetValue>("new_addresstypecode", ts, "app.");
                    if (addressType != null)
                        newAddress.SetAttributeValue("addresstypecode", addressType);

                    newAddress.SetAttributeValue("new_addresssource", new OptionSetValue((int)AddressSource.CMAUpdate));
                    newAddress.SetAttributeValue("new_cmapreferred", true);
                    newAddress.SetAttributeValue("new_addresssource", new OptionSetValue((int)AddressSource.Application));
                    //create it 
                    CrmHelpers.TraceAttributeCollection(service, newAddress, ts);
                    service.Create(newAddress);
                    #endregion Create address

                    //update application with new contact 
                    Entity applicationUpdate = new Entity(application.LogicalName, application.Id);
                    applicationUpdate.SetAttributeValue("new_parentcustomerid", new EntityReference("contact", newContactId));
                    service.Update(applicationUpdate);

                }

            }

        }
        
    }
}      
        
