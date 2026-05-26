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

namespace UpdateContactFromOnlineApplication
{  
    public class UpdateContactFromOnlineApplication : CodeActivity
    {
        #region Input/Output Arguments        
        #endregion 

        protected override void Execute(CodeActivityContext context)
        {
            // Obtain the organization service reference.
            var workflowContext = context.GetExtension<IWorkflowContext>();
            var serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
            var service = serviceFactory.CreateOrganizationService(workflowContext.UserId);
            ITracingService ts = context.GetExtension<ITracingService>();

            EntityReference appRef = new EntityReference("new_onlineapplication", workflowContext.PrimaryEntityId);

            //read valus from online application, contact and account
            Entity application = service.Retrieve(appRef.LogicalName, appRef.Id, new ColumnSet("new_name", "new_applicationsource"
                , "new_parentcustomerid", "new_addresstypecode", "new_careerstatustypeid", "new_practicestatus", "new_salutationcode"
                , "new_preferredemail", "new_mobilephone", "new_telephone1", "new_businessphoneextension", "new_telephone2"
                , "new_collegeofgraduationid", "new_satellitecampusesid", "new_yearenrolledinmedicalschool", "new_gradyear"
                , "new_residencyprogram", "new_universityofresidencyid", "new_yearlicensed", "new_canadianarmedforcesmember"
                , "new_locum_estcompdate", "new_dateretirement", "new_fellowship", "new_residencycompletion", "new_fellowship_estcompdate"
                , "new_specialtyid", "new_preferredname", "new_firstname", "new_lastname", "new_birthdate", "new_familyphysiciannumber"
                , "new_royalcollegenumber", "new_licensingassocaccountid", "new_provincenumber", "new_mincnum", "new_line1", "new_line2"
                , "new_line3", "new_city", "new_postalcode", "new_provincestatecode", "new_countrycode", "new_othercountry"));
            string applicationName = application.GetAttributeValueEx<string>("new_name", ts, "app.");
            OptionSetValue applicationSource = application.GetAttributeValueEx<OptionSetValue>("new_applicationsource", ts, "app.");
            EntityReference contactRef = application.GetAttributeValueEx<EntityReference>("new_parentcustomerid", ts, "app.");
            OptionSetValue addresstype = application.GetAttributeValueEx<OptionSetValue>("new_addresstypecode", ts, "app.");
            if (addresstype == null)
                addresstype = new OptionSetValue((int)AddressType.Other);

            if (contactRef != null)
            {
                //check for previous batch for this application 
                string fetchXML = $@"<fetch>
                                  <entity name='contact' >
                                    <attribute name='new_physicianpreferredname' />
                                    <attribute name='birthdate' />
                                    <attribute name='firstname' />
                                    <attribute name='lastname' />
                                    <attribute name='new_mincnum' />
                                    <attribute name='new_licensingassocaccountid' />
                                    <attribute name='new_provincenumber' />
                                    <attribute name='new_familyphysiciannumber' />
                                    <attribute name='new_royalcollegenumber' />
                                    <attribute name='new_specialtyid' />
                                    <filter>
                                      <condition attribute='contactid' operator='eq' value='{contactRef.Id}' />
                                    </filter>
                                    <link-entity name='customeraddress' from='parentid' to='contactid' link-type='outer' alias='address' >
                                      <attribute name='customeraddressid' />
                                      <attribute name='line1' />
                                      <attribute name='line2' />
                                      <attribute name='line3' />
                                      <attribute name='city' />
                                      <attribute name='new_cmapreferred' />
                                      <attribute name='new_stateorprovincemasterlist' />
                                      <attribute name='postalcode' />
                                      <attribute name='customeraddressid' />
                                      <attribute name='new_country' />
                                      <attribute name='new_status' />
                                      <attribute name='new_returnmail' />
                                      <filter>
                                        <condition attribute='addresstypecode' operator='eq' value='{addresstype.Value}' />
                                      </filter>
                                    </link-entity>
                                  </entity>
                                </fetch>";
                Entity contact = CrmHelpers.GetFirstFromExecuteFetch(service, fetchXML);


                if (contact != null)
                {
                    Entity contactUpdate = new Entity("contact", contact.Id);

                    Guid addressId = contact.GetAttributeValueEx<Guid>("address.customeraddressid", ts, "contact.");
                    Entity address = addressId != Guid.Empty ? new Entity("customeraddress", addressId) : new Entity("customeraddress");

                    //Carrer Status
                    EntityReference careerstatus = application.GetAttributeValueEx<EntityReference>("new_careerstatustypeid", ts, "app.");
                    if (careerstatus != null)
                        contactUpdate.SetAttributeValue("new_contactsubtypeid", careerstatus);
                    //Practice Status 
                    OptionSetValue practicestatus = application.GetAttributeValueEx<OptionSetValue>("new_practicestatus", ts, "app.");
                    if (practicestatus != null)
                    {
                        if (practicestatus.Value == (int)ApplicationPracticeStatus.FirstYearInPractice)
                        {
                             //contactUpdate.SetAttributeValue("new_practicestatus", new OptionSetValue((int)ContactPracticeStatus.InPractice));
                             //contactUpdate.SetAttributeValue("new_1styearinpractice", new DateTime(DateTime.Now.Year, 1, 1));
                        }

                        else if (practicestatus.Value == (int)ApplicationPracticeStatus.Locum)
                        {
                            contactUpdate.SetAttributeValue("new_practicestatus", new OptionSetValue((int)ContactPracticeStatus.InPractice));
                            contactUpdate.SetAttributeValue("new_locum", new OptionSetValue(100000000)); //yes this is the option set value for yes
                        }
                        else if (practicestatus.Value == (int)ApplicationPracticeStatus.MaternityLeave)
                            contactUpdate.SetAttributeValue("new_practicestatus", new OptionSetValue((int)ContactPracticeStatus.MaternityLeave));
                        else if (practicestatus.Value == (int)ApplicationPracticeStatus.NonPracticingAcademic)
                            contactUpdate.SetAttributeValue("new_practicestatus", new OptionSetValue((int)ContactPracticeStatus.NonPracticingAcademic));
                        else if (practicestatus.Value == (int)ApplicationPracticeStatus.PartTime)
                            contactUpdate.SetAttributeValue("new_practicestatus", new OptionSetValue((int)ApplicationPracticeStatus.PartTime));
                        else if (practicestatus.Value == (int)ApplicationPracticeStatus.PracticingPhysician)
                            contactUpdate.SetAttributeValue("new_practicestatus", new OptionSetValue((int)ContactPracticeStatus.InPractice));
                        else if (practicestatus.Value == (int)ApplicationPracticeStatus.Retired)
                            contactUpdate.SetAttributeValue("new_practicestatus", new OptionSetValue((int)ContactPracticeStatus.Retired));
                    }
                    //Saluatation
                    OptionSetValue salutation = application.GetAttributeValueEx<OptionSetValue>("new_salutationcode", ts, "app.");
                    if (salutation != null)
                    {
                        contactUpdate.SetAttributeValue("new_excludesalutation", false);
                        contactUpdate.SetAttributeValue("new_salutation", salutation.Value == (int)ContactSalutation.Phd ? new OptionSetValue((int)ContactSalutation.Drdot) : salutation);
                    }
                    else
                    {
                        contactUpdate.SetAttributeValue("new_excludesalutation", true);
                        contactUpdate.SetAttributeValue("new_salutation", null);
                    }
                    //Preferred Email  
                    string email = application.GetAttributeValueEx<string>("new_preferredemail", ts, "app.");
                    if (email != null)
                        contactUpdate.SetAttributeValue("emailaddress1", email);

                    //Mobile Phone                 
                    string mobile = application.GetAttributeValueEx<string>("new_mobilephone", ts, "app.");
                    if (mobile != null)
                        contactUpdate.SetAttributeValue("mobilephone", mobile);

                    //Business Phone                
                    string busPhone = application.GetAttributeValueEx<string>("new_telephone1", ts, "app.");
                    if (busPhone != null)
                        contactUpdate.SetAttributeValue("telephone1", busPhone);

                    //Business Extension            
                    string busExt = application.GetAttributeValueEx<string>("new_businessphoneextension", ts, "app.");
                    if (busExt != null)
                        contactUpdate.SetAttributeValue("new_businessphoneextension", busExt);

                    //Home Phone
                    string homePhone = application.GetAttributeValueEx<string>("new_telephone2", ts, "app.");
                    if (homePhone != null)
                        contactUpdate.SetAttributeValue("telephone2", homePhone);

                    //University of Graduation                   
                    EntityReference univeristyofGrad = application.GetAttributeValueEx<EntityReference>("new_collegeofgraduationid", ts, "app.");
                    if (univeristyofGrad != null)
                        contactUpdate.SetAttributeValue("new_collegeofgraduation", univeristyofGrad);

                    //Satellite Campus 
                    EntityReference satelliteCampus = application.GetAttributeValueEx<EntityReference>("new_satellitecampusesid", ts, "app.");
                    if (careerstatus != null)
                        contactUpdate.SetAttributeValue("new_satellitecampuses", satelliteCampus);

                    //Year Enrolled in Medical School                   
                    OptionSetValue yearEnrolled = application.GetAttributeValueEx<OptionSetValue>("new_yearenrolledinmedicalschool", ts, "app.");
                    if (yearEnrolled != null)
                        contactUpdate.SetAttributeValue("new_yearenrolledinmedicalschool", new DateTime(yearEnrolled.Value, 1, 1));

                    //Graduation Year  
                    OptionSetValue gradYear = application.GetAttributeValueEx<OptionSetValue>("new_gradyear", ts, "app.");
                    if (gradYear != null)
                        contactUpdate.SetAttributeValue("new_gradyear", gradYear.Value.ToString());

                    //Program of Residency   
                    OptionSetValue residencyProgram = application.GetAttributeValueEx<OptionSetValue>("new_residencyprogram", ts, "app.");
                    if (residencyProgram != null)
                        contactUpdate.SetAttributeValue("new_residencyprogram", residencyProgram);

                    //University of Residency  
                    EntityReference university = application.GetAttributeValueEx<EntityReference>("new_universityofresidencyid", ts, "app.");
                    if (university != null)
                        contactUpdate.SetAttributeValue("new_universityofresidency", university);

                    //Year Licensed   
                    OptionSetValue yearLicensed = application.GetAttributeValueEx<OptionSetValue>("new_yearlicensed", ts, "app.");
                    if (yearLicensed != null)
                        contactUpdate.SetAttributeValue("new_yearlicensed", yearLicensed.Value.ToString());

                    //Canadian Armed Forces Member
                    if (application.Contains("new_canadianarmedforcesmember"))
                    {
                        OptionSetValue armedForces = application.GetAttributeValueEx<OptionSetValue>("new_canadianarmedforcesmember", ts, "app.");
                        contactUpdate.SetAttributeValue("new_canadianarmedforcesmember", (armedForces == null ? false : armedForces.Value == 1 ? true : false));
                    }

                    //Locum completion date 
                    DateTime? locumDate = application.GetAttributeValueEx<DateTime?>("new_locum_estcompdate", ts, "app.");
                    if (locumDate != null)
                        contactUpdate.SetAttributeValue("new_locum_estcompdate", locumDate.Value);

                    //Date of Retirement                  
                    DateTime? retirementDate = application.GetAttributeValueEx<DateTime?>("new_dateretirement", ts, "app.");
                    if (retirementDate != null)
                        contactUpdate.SetAttributeValue("new_dateretirement", retirementDate.Value);


                    //Fellowship
                    bool isFellowship = false;
                    if (application.Contains("new_fellowship"))
                    {
                        OptionSetValue fellowship = application.GetAttributeValueEx<OptionSetValue>("new_fellowship", ts, "app.");
                        contactUpdate.SetAttributeValue("new_fellowship", fellowship?.Value == 0 ? new OptionSetValue(100000001) : new OptionSetValue(100000000) );  // yes is optionset value 100000000 
                        isFellowship = fellowship?.Value == 1;
                    }


                    //Fellowship/Residency Completion Date    
                    OptionSetValue residencyyear = application.GetAttributeValueEx<OptionSetValue>("new_residencycompletion", ts, "app.");
                    DateTime? fellowshipDate = application.GetAttributeValueEx<DateTime?>("new_fellowship_estcompdate", ts, "app.");
                    if (applicationSource?.Value == (int)ApplicationSource.Portal && residencyyear != null)
                    {
                        //from portal, if fellowship is yes, fellowship completion date comes from residency completion year 
                        if (isFellowship)
                            contactUpdate.SetAttributeValue("new_fellowship_estcompdate", new DateTime(residencyyear.Value, 12, 31, 23, 59, 59));
                        else
                            contactUpdate.SetAttributeValue("new_residencycompletion", residencyyear.Value.ToString());
                    }
                    else //non portal
                    {
                        //Expected Year of Residency Completion    
                        if (residencyyear != null)
                            contactUpdate.SetAttributeValue("new_residencycompletion", residencyyear.Value.ToString());

                        //Fellowship Completion Date                    
                        if (fellowshipDate != null)
                            contactUpdate.SetAttributeValue("new_fellowship_estcompdate", fellowshipDate.Value);
                    }
                    // Specialty
                    EntityReference contactSpecialty = CmaHelpers.GetPhysicianSpecialty(service, contact.Id, ts);
                    EntityReference appSpecialty = application.GetAttributeValueEx<EntityReference>("new_specialtyid", ts, "app.");
                    bool needReview = false;
                    if( (contactSpecialty == null && appSpecialty == null)  || (contactSpecialty != appSpecialty) )
                        needReview = true;
                    else
                    { 
                        if (needReview)
                        {
                            Entity updateApplication = new Entity(application.LogicalName, application.Id);
                            updateApplication.SetAttributeValue("new_specialtyreviewrequired", needReview);
                            updateApplication.SetAttributeValue("new_approvalstatuscode", new OptionSetValue((int)ApprovalStatus.RequiresFollowUp));
                            ts.Trace("Update Specialty Review");
                            service.Update(updateApplication);
                        }
                    }

                    //prefered name
                    string preferredName = application.GetAttributeValueEx<string>("new_preferredname", ts, "app.");
                    if (preferredName != null)
                        contactUpdate.SetAttributeValue("new_physicianpreferredname", preferredName);
                    
                    //these item only update if corresponding contact has null values
                    //-------------------------------------------------------------------------------------------------------------------------------------------
                    //first name
                    if (string.IsNullOrWhiteSpace(contact.GetAttributeValueEx<string>("firstname", ts, "contact.")))
                    {
                        string firstname = application.GetAttributeValueEx<string>("new_firstname", ts, "app.");
                        if (firstname != null)
                            contactUpdate.SetAttributeValue("firstname", firstname);
                    }

                    //lastname
                    if (string.IsNullOrWhiteSpace(contact.GetAttributeValueEx<string>("lastname", ts, "contact.")))
                    {
                        string lastname = application.GetAttributeValueEx<string>("new_lastname", ts, "app.");
                        if (lastname != null)
                            contactUpdate.SetAttributeValue("lastname", lastname);
                    }

                    //birth date
                    if (contact.GetAttributeValueEx<DateTime?>("birthdate", ts, "contact.") == null)
                    {
                        DateTime? birthdate = application.GetAttributeValueEx<DateTime?>("new_birthdate", ts, "app.");
                        if (birthdate != null)
                            contactUpdate.SetAttributeValue("birthdate", birthdate.Value);
                    }

                    //Physician # at College of Family Physicians
                    if (string.IsNullOrWhiteSpace(contact.GetAttributeValueEx<string>("new_familyphysiciannumber", ts, "contact.")))
                    {
                        string familyphysiciannumber = application.GetAttributeValueEx<string>("new_familyphysiciannumber", ts, "app.");
                        if (familyphysiciannumber != null)
                            contactUpdate.SetAttributeValue("new_familyphysiciannumber", familyphysiciannumber);
                    }

                    //Physician # at Royal College
                    if (string.IsNullOrWhiteSpace(contact.GetAttributeValueEx<string>("new_royalcollegenumber", ts, "contact.")))
                    {
                        string royalcollegenumber = application.GetAttributeValueEx<string>("new_royalcollegenumber", ts, "app.");
                        if (royalcollegenumber != null)
                            contactUpdate.SetAttributeValue("new_royalcollegenumber", royalcollegenumber);
                    }

                    //Licensing Body
                    if (contact.GetAttributeValueEx<EntityReference>("new_licensingassocaccountid", ts, "contact.") == null)
                    {
                        EntityReference licensingBody = application.GetAttributeValueEx<EntityReference>("new_licensingassocaccountid", ts, "app.");
                        if (licensingBody != null)
                            contactUpdate.SetAttributeValue("new_licensingassocaccountid", licensingBody);
                    }
                   ;
                    //Provincial License Number
                    
                    string provNumber = application.GetAttributeValueEx<string>("new_provincenumber", ts, "app.");
                    if ( !string.IsNullOrWhiteSpace(provNumber) )
                        contactUpdate.SetAttributeValue("new_provincenumber", provNumber);
                    
                    //MINC Number
                    if (string.IsNullOrWhiteSpace(contact.GetAttributeValueEx<string>("new_mincnum", ts, "contact.")))
                    {
                        string mincnum = application.GetAttributeValueEx<string>("new_mincnum", ts, "app.");
                        if (mincnum != null)
                            contactUpdate.SetAttributeValue("new_mincnum", mincnum);
                    }
                    ts.Trace("Contact Update");
                    CrmHelpers.TraceAttributeCollection(service, contactUpdate, ts);
                    service.Update(contactUpdate);


                    if (address != null)
                    {
                        bool createAddress = address.Id == Guid.Empty;
                           
                        //address 1,2,3
                        string addr = application.GetAttributeValueEx<string>("new_line1", ts, "app.");
                        if(createAddress || addr != contact.GetAttributeValueEx<string>("address.line1",ts, "") )
                            address.SetAttributeValue("line1", addr);

                        addr = application.GetAttributeValueEx<string>("new_line2", ts, "app.");
                        if(createAddress || addr != contact.GetAttributeValueEx<string>("address.line2", ts, "") )
                            address.SetAttributeValue("line2", addr);

                        addr = application.GetAttributeValueEx<string>("new_line3", ts, "app.");
                        if (createAddress || addr != contact.GetAttributeValueEx<string>("address.line3", ts, "") )
                            address.SetAttributeValue("line3", addr);

                        //City
                        string city = application.GetAttributeValueEx<string>("new_city", ts, "app.");
                        if (createAddress || city != contact.GetAttributeValueEx<string>("address.city", ts, "") )
                            address.SetAttributeValue("city", city);
                        
                        //Code
                        string postalcode = application.GetAttributeValueEx<string>("new_postalcode", ts, "app.");
                        if (createAddress || postalcode != contact.GetAttributeValueEx<string>("address.postalcode", ts, "") )
                            address.SetAttributeValue("postalcode", postalcode);

                        //Province State
                        OptionSetValue province = application.GetAttributeValueEx<OptionSetValue>("new_provincestatecode", ts, "app.");
                        if ( province?.Value != contact.GetAttributeValueEx<OptionSetValue>("address.new_stateorprovincemasterlist", ts, "")?.Value )
                            address.SetAttributeValue("new_stateorprovincemasterlist", province);

                        //Country
                        OptionSetValue country = application.GetAttributeValueEx<OptionSetValue>("new_countrycode", ts, "app.");
                        if (country != null)
                        {
                            if (country.Value == (int)AppCountryCode.Other)
                                country = application.GetAttributeValueEx<OptionSetValue>("new_othercountry", ts, "app.");
                            if (createAddress || country?.Value != contact.GetAttributeValueEx<OptionSetValue>("address.new_country", ts, "")?.Value )
                                address.SetAttributeValue("new_country", country);
                        }

                        //Satus
                        if (createAddress || !contact.GetAttributeValueEx<bool>("address.new_status", ts, ""))
                            address.SetAttributeValue("new_status", true);

                        //cmapreferred
                        if (createAddress || !contact.GetAttributeValueEx<bool>("address.new_cmapreferred", ts, "") )
                            address.SetAttributeValue("new_cmapreferred", true);

                        //returnmail 
                        if (createAddress || contact.GetAttributeValueEx<bool>("address.new_returnmail", ts, ""))
                            address.SetAttributeValue("new_returnmail", false);

                        //status 

                        CrmHelpers.TraceAttributeCollection(service, address, ts);
                       if( createAddress)
                        { 
                            ts.Trace("Address Create ");
                            ts.Trace("Address Type Value : "+addresstype.Value.ToString());
                            address.SetAttributeValue("parentid", contact.ToEntityReference() );
                            address.SetAttributeValue("addresstypecode", addresstype);
                            address.SetAttributeValue("new_addresssource",  new OptionSetValue( (int)AddressSource.Application ));
                            // Added to stop the creation of Address record with blank fields except Country CRM-253 
                            if (addresstype.Value != ((int)AddressType.Other))
                            {
                                ts.Trace("Creating Address");
                                service.Create(address);
                            }
                        }
                        //see if we have anything to update
                        else if( address.Attributes.Count > 0)
                        {
                            ts.Trace("Address Update");
                            address.SetAttributeValue("new_addresssource", new OptionSetValue((int)AddressSource.Application));
                            service.Update(address);
                        }
                    }
                }
            }
            else
                throw new InvalidPluginExecutionException($"Online Application '{applicationName}' does not have a related contact");

        }
       

    }

}      
        
