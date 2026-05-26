using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk.Query;
using System.Runtime;
using System.Runtime.Serialization.Json;
using System.Globalization;

namespace ContactGetRecordWorkflow
{
    public class ContactGetRecordWorkflow : CodeActivity
    {
        #region Input/Output Arguments
        [Input("CMA.CA ID")]
        [RequiredArgument]
        public InArgument<string> cmaID { get; set; }

        [Input("shareHolder ID")]
        [RequiredArgument]
        public InArgument<string> shareHolderID { get; set; }

              
        [Output("Output Value")]
        public OutArgument<string> outPara { get; set; }

        #endregion

        protected override void Execute(CodeActivityContext executionContext)
        {
            // Obtain the organization service reference.
            var context = executionContext.GetExtension<IWorkflowContext>();
            var serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            var service = serviceFactory.CreateOrganizationService(context.UserId);
            ITracingService ts = executionContext.GetExtension<ITracingService>();

            var subId = shareHolderID.Get<string>(executionContext).Replace("'","");
            var cmaId = cmaID.Get<string>(executionContext).Replace("'","");
            string content = "";

            #region NewCode

            QueryExpression qe = new QueryExpression();
            EntityCollection ec = new EntityCollection();

            if (cmaId.ToLower() != "null" && cmaId != "")
            {
                qe.EntityName = "contact";
                //qe.ColumnSet = new ColumnSet(true);
                qe.ColumnSet = new ColumnSet("new_contacttypeid","new_contactsubtypeid","new_cmamembershipdetailid","new_cmaid","firstname","lastname","birthdate","new_othername","new_collegelocation","new_collegeofgraduation","new_gradyear","new_practicestatus","new_provincenumber","new_yearlicensed","emailaddress1","new_deathdate","telephone2","telephone1","new_cmapreferredcontactmethod","new_language","new_gc_audience_type","new_staff_audience_type","new_contacttypeid","new_contactsubtypeid");
                qe.Criteria.AddCondition("new_contact_id", ConditionOperator.Equal, subId);
                qe.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
                qe.Criteria.AddCondition("new_cmaid", ConditionOperator.Equal, cmaId);
                //qe.Criteria.AddCondition("new_contacttype", ConditionOperator.NotEqual, "100000002"); //Non-Personal
                qe.LinkEntities.Add(new LinkEntity("contact", "new_specialty", "new_specialtyid", "new_specialtyid", JoinOperator.LeftOuter));
                qe.LinkEntities[0].Columns.AddColumns("new_name", "new_specialtycode");
                qe.LinkEntities[0].EntityAlias = "specialty";

                ec = service.RetrieveMultiple(qe);

                if (ec.Entities.Count > 1)
                {
                    ts.Trace("Multiple Contacts found in MX360, with the provided Shareholder ID: " + subId);
                }
            }
            else
            //if (ec.Entities.Count == 0 && (cmaId == "null" || cmaId == ""))
            {
                qe = new QueryExpression();
                qe.EntityName = "contact";
                //qe.ColumnSet = new ColumnSet(true);
                qe.ColumnSet = new ColumnSet("new_contacttypeid","new_contactsubtypeid","new_cmamembershipdetailid","new_cmaid","firstname","lastname","birthdate","new_othername","new_collegelocation","new_collegeofgraduation","new_gradyear","new_practicestatus","new_provincenumber","new_yearlicensed","emailaddress1","new_deathdate","telephone2","telephone1","new_cmapreferredcontactmethod","new_language","new_gc_audience_type","new_staff_audience_type","new_contacttypeid","new_contactsubtypeid");
                qe.Criteria.AddCondition("new_contact_id", ConditionOperator.Equal, subId);
                //qe.Criteria.AddCondition("new_cmaid", ConditionOperator.Equal, cmaId);
                qe.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
                //qe.Criteria.AddCondition("new_contacttype", ConditionOperator.NotEqual, "100000002"); //Non-Personal
                qe.LinkEntities.Add(new LinkEntity("contact", "new_specialty", "new_specialtyid", "new_specialtyid", JoinOperator.LeftOuter));
                qe.LinkEntities[0].Columns.AddColumns("new_name", "new_specialtycode");
                qe.LinkEntities[0].EntityAlias = "specialty";

                ec = service.RetrieveMultiple(qe);

                if (ec.Entities.Count > 1)
                {
                    ts.Trace("Multiple Contacts found in MX360, with the provided cma ID: " + cmaID);
                }
            }

          //  outPara.Set(executionContext, "" + ec.Entities.Count + "   :: cmaId="+cmaId);

            if (ec.Entities.Count > 0)
            {
                string ContacType = "";
                string contactSubType = "";
                string ContacTypeEng = "";
                string contactSubTypeEng = "";
                string memDst = "";
                string memexp = "";
                string divAccountName = "";
                string ptmaCode = "";
                EntityReference CMA_MembershipDetailReference = null;
                EntityReference associatedDivisionAccountReference = null;
                DateTime? cmaMembershipDiscontinuedDate = null;
                Guid? contactID = null;

                contactID = ec.Entities[0].Id;
                ContacType = !ec.Entities[0].Contains("new_contacttypeid") ? "" : ((EntityReference)ec.Entities[0]["new_contacttypeid"]).Name.ToLower();
                contactSubType = !ec.Entities[0].Contains("new_contactsubtypeid") ? "" : ((EntityReference)ec.Entities[0]["new_contactsubtypeid"]).Name.ToLower();
                if (ContacType.Contains(" / "))
                {
                    var type = ContacType.Split(new string[] { " / " }, StringSplitOptions.None);
                    ContacTypeEng = type[0];
                }
                else
                    ContacTypeEng = ContacType;

                if (contactSubType.Contains(" / "))
                {
                    var subType = contactSubType.Split(new string[] { " / " }, StringSplitOptions.None);
                    contactSubTypeEng = subType[0];
                }
                else
                    contactSubTypeEng = contactSubType;


                if (ec.Entities[0].Attributes.Contains("new_cmamembershipdetailid"))
                {
                    CMA_MembershipDetailReference = (EntityReference)ec.Entities[0].Attributes["new_cmamembershipdetailid"];
                }

                if (ec.Entities[0].Attributes.Contains("new_cmamembershipdetailid"))
                {
                    // Retrieve the associated cma membership detail record
                    string searchMembershipDetailOnID = @"<fetch version=""1.0"" output-format=""xml-platform"" mapping=""logical"" distinct=""false"">" +
                                                            @"<entity name=""new_cmamembershipdetail"">" +
                                                            @"<attribute name=""new_divassocaccountid""/>" +
                                                            @"<attribute name=""new_discontinueddate""/>" +
                                                            @"<attribute name=""new_expirydate""/>" +
                                                            @"<attribute name=""new_status""/>" +
                                                            @"<filter type=""and"">" +
                                                            @"<condition attribute=""new_cmamembershipdetailid"" operator=""eq"" value=""{0}"" />" +
                                                            @"<condition attribute=""statecode"" operator=""eq"" value=""0"" />" +
                                                            @"<condition attribute=""new_status"" operator=""neq"" value=""100000002"" />" +
                                                            @"</filter>" +
                                                            @"</entity>" +
                                                            @"</fetch>";
                    // Retrieve Accounts with matching account ID
                    EntityCollection cmdMemDetails = service.RetrieveMultiple(new FetchExpression(String.Format(searchMembershipDetailOnID, CMA_MembershipDetailReference.Id)));
                    int cmaMemDetCount = cmdMemDetails.Entities.Count;
                    if (cmaMemDetCount > 0)
                    {
                        Entity cmaMemDetailFound = cmdMemDetails.Entities[0];

                        if (cmaMemDetailFound.Attributes.Contains("new_divassocaccountid"))
                            associatedDivisionAccountReference = (EntityReference)cmaMemDetailFound.Attributes["new_divassocaccountid"];
                        if (cmaMemDetailFound.Attributes.Contains("new_discontinueddate"))
                        {
                            memDst = ((DateTime)cmaMemDetailFound.Attributes["new_discontinueddate"]).ToString("yyyyMMdd");
                            cmaMembershipDiscontinuedDate = (DateTime)cmaMemDetailFound.Attributes["new_discontinueddate"];
                        }
                        if (cmaMemDetailFound.Attributes.Contains("new_expirydate"))
                            memexp = ((DateTime)cmaMemDetailFound.Attributes["new_expirydate"]).ToString("yyyyMMdd");
                    }
                    else
                    {
                        CMA_MembershipDetailReference = null;  // could have found a reference with Inactive Membership record.  In the search above a check is made for Inactive mem detail records. 
                    }

                }

                //License Province (retrieved from Account entity using the Division Association from CMA Membership Detail)
                //Retrieve Account record based on the associatedDivisionAccountReference ID
                if (associatedDivisionAccountReference != null)
                {
                    string searchAccountOnDivAccountID = @"<fetch version=""1.0"" output-format=""xml-platform"" mapping=""logical"" distinct=""false"">" +
                                                        @"<entity name=""account"">" +
                                                        @"<attribute name=""name""/>" +
                                                        @"<attribute name=""new_account_id""/>" +
                                                        @"<attribute name=""new_ptmacode""/>" +
                                                        @"<filter type=""and"">" +
                                                        @"<condition attribute=""accountid"" operator=""eq"" value=""{0}"" />" +
                                                        @"<condition attribute=""statecode"" operator=""eq"" value=""0"" />" +
                                                        @"</filter>" +
                                                        @"</entity>" +
                                                        @"</fetch>";
                    // Retrieve Accounts with matching account ID
                    EntityCollection ptmaAccounts = service.RetrieveMultiple(new FetchExpression(String.Format(searchAccountOnDivAccountID, associatedDivisionAccountReference.Id)));
                    int ptmaAccountCount = ptmaAccounts.Entities.Count;
                    if (ptmaAccountCount == 1)
                    {
                        Entity ptmaAccountFound = ptmaAccounts.Entities[0];
                        if (ptmaAccountFound.Attributes.Contains("name") && ptmaAccountFound.Attributes["name"] != null)
                            divAccountName = (string)ptmaAccountFound.Attributes["name"];
                        if (ptmaAccountFound.Attributes.Contains("new_ptmacode"))
                            ptmaCode = (string)ptmaAccountFound.Attributes["new_ptmacode"];
                    }
                }


                content = "{";
                content += !ec.Entities[0].Contains("new_cmaid") ? @"""cmA-ID"":""""," : @"""cmA-ID"":""" + EscpateDoubleQuoteAndBackSlash(ec.Entities[0]["new_cmaid"] as string)  + @""",";
                content += !ec.Entities[0].Contains("firstname") ? @"""given-Name"":""""," : @"""given-Name"":""" + EscpateDoubleQuoteAndBackSlash(ec.Entities[0]["firstname"] as string) + @""",";
                content += !ec.Entities[0].Contains("lastname") ? @"""last-Name"":""""," : @"""last-Name"":""" + EscpateDoubleQuoteAndBackSlash(ec.Entities[0]["lastname"] as string) + @""",";

                content += !ec.Entities[0].Contains("birthdate") ? @"""birth-Date"":""""," : @"""birth-Date"":""" + EscpateDoubleQuoteAndBackSlash(Convert.ToDateTime(ec.Entities[0].Attributes["birthdate"]).ToString("yyyyMMdd")) + @""",";
                content += !ec.Entities[0].Contains("new_othername") ? @"""other-Name1"":""""," : @"""other-Name1"":""" + EscpateDoubleQuoteAndBackSlash(ec.Entities[0]["new_othername"] as string) + @""",";

                content += @"""other-Name2"":"""",";

                string Grad_Ctry = "";
                if (ec.Entities[0].Contains("new_collegelocation"))
                {
                    Grad_Ctry = ((OptionSetValue)ec.Entities[0]["new_collegelocation"]).Value.ToString();
                }
                if (contactSubType != "")
                {
                    if (((contactSubType.StartsWith("student", true, CultureInfo.InvariantCulture)) || (contactSubType.StartsWith("resident", true, CultureInfo.InvariantCulture))) && (CMA_MembershipDetailReference == null))
                    {
                        content += @"""grad-Ctry"":"""",";
                    }
                    else { content += @"""grad-Ctry"":""" + EscpateDoubleQuoteAndBackSlash(Grad_Ctry) + @""","; }
                }
                else { content += @"""grad-Ctry"":""" + EscpateDoubleQuoteAndBackSlash(Grad_Ctry) + @""","; }


                // College Code from Account Entity
                string collegeCode = "";
                if (ec.Entities[0].Contains("new_collegeofgraduation"))
                {
                    var collegeGraduationReference = (EntityReference)ec.Entities[0]["new_collegeofgraduation"];

                    //Retrieve Account entity based on the collegeGraduationReference ID
                    string searchAccountOnAccountID = @"<fetch version=""1.0"" output-format=""xml-platform"" mapping=""logical"" distinct=""false"">" +
                                                        @"<entity name=""account"">" +
                                                        @"<attribute name=""new_account_id""/>" +
                                                        @"<attribute name=""new_collegecode""/>" +
                                                        @"<filter type=""and"">" +
                                                        @"<condition attribute=""accountid"" operator=""eq"" value=""{0}"" />" +
                                                        @"<condition attribute=""statecode"" operator=""eq"" value=""0"" />" +
                                                        @"</filter>" +
                                                        @"</entity>" +
                                                        @"</fetch>";
                    // Retrieve Accounts with matching account ID
                    EntityCollection accounts = service.RetrieveMultiple(new FetchExpression(String.Format(searchAccountOnAccountID, collegeGraduationReference.Id)));
                    int accountCount = accounts.Entities.Count;
                    if (accountCount == 1)
                    {
                        Entity accountFound = accounts.Entities[0];
                        if (accountFound.Attributes.Contains("new_collegecode"))
                            collegeCode = (string)accountFound.Attributes["new_collegecode"];
                    }
                }
                if (contactSubType != "")
                {
                    // if (((contactSubType == "100000000") || (contactSubType == "100000001")) && (CMA_MembershipDetailReference == null))
                    if (((contactSubType.StartsWith("student", true, CultureInfo.InvariantCulture)) || (contactSubType.StartsWith("resident", true, CultureInfo.InvariantCulture))) && (CMA_MembershipDetailReference == null))
                    {
                        content += @"""grad-Coll"":"""",";
                    }
                    else { content += @"""grad-Coll"":""" + EscpateDoubleQuoteAndBackSlash(collegeCode) + @""","; }
                }
                else { content += @"""grad-Coll"":""" + EscpateDoubleQuoteAndBackSlash(collegeCode) + @""","; }

                // Graduation Year
                string gradYear = "";
                if (ec.Entities[0].Contains("new_gradyear"))
                    gradYear = (string)ec.Entities[0]["new_gradyear"];
                if (contactSubType != null)
                {
                    if (((contactSubType.StartsWith("student", true, CultureInfo.InvariantCulture)) || (contactSubType.StartsWith("resident", true, CultureInfo.InvariantCulture))) && (CMA_MembershipDetailReference == null))
                    {
                        content += @"""grad-Yr"":"""",";
                    }
                    else { content += @"""grad-Yr"":""" + EscpateDoubleQuoteAndBackSlash(gradYear) + @""","; }
                }
                else { content += @"""grad-Yr"":""" + EscpateDoubleQuoteAndBackSlash(gradYear) + @""","; }
                //content += !ec.Entities[0].Contains("new_gradyear") ? """grad-Yr"":""""," : """grad-Yr"":""" + ec.Entities[0]["new_gradyear"] + """,";

                //Practice Status
                string practiceStatus = "";
                if (ec.Entities[0].Contains("new_practicestatus"))
                {
                    OptionSetValue practiceStatusOption = (OptionSetValue)ec.Entities[0]["new_practicestatus"];
                    practiceStatus = (practiceStatusOption.Value == 100000002) ? "Retired" : "Active";
                }
                else { practiceStatus = "Unknown"; }
                content += @"""prac-Status"":""" + EscpateDoubleQuoteAndBackSlash(practiceStatus) + @""",";

                content += !ec.Entities[0].Contains("new_contactsubtypeid") ? @"""activity"":""""," : @"""activity"":""" + EscpateDoubleQuoteAndBackSlash(contactSubTypeEng) + @""",";

                string licProv = "";
                if (ec.Entities[0].Contains("new_provincenumber"))
                    licProv = (string)ec.Entities[0]["new_provincenumber"];
                if (contactSubType != "")
                {
                    if (contactSubType.StartsWith("student", true, CultureInfo.InvariantCulture)) //100000000 is Student
                        licProv = "";
                    else if (ptmaCode.ToUpper() == "ON" || ptmaCode.ToUpper() == "SK")
                        licProv = "";
                }
                content += @"""lic-Prov"":""" + EscpateDoubleQuoteAndBackSlash(licProv)+ @""",";

                content += @"""lic-Ref"":"""",";

                string licYear = "";
                if (ec.Entities[0].Contains("new_yearlicensed"))
                    licYear = (string)ec.Entities[0]["new_yearlicensed"];
                if (contactSubType != "")
                {
                    if (contactSubType.StartsWith("student",true, CultureInfo.InvariantCulture)) //100000000 is Student
                        licYear = "";
                }
                content += @"""lic-Year"":""" + EscpateDoubleQuoteAndBackSlash(licYear) + @""",";

                //Physician Specialty Code
                EntityReference certifyingBodyReference = null;
                EntityReference specialtyReference = null;
                string commonName = "";
                string specialtyCode = "";
                // first retrieve the Physician Specialty record
                string searchPhySpecialtyOnContactID = @"<fetch version=""1.0"" output-format=""xml-platform"" mapping=""logical"" distinct=""false"">" +
                                                        @"<entity name=""new_physicianspecialty"">" +
                                                        @"<attribute name=""new_specialtyid""/>" +
                                                        @"<attribute name=""new_certbodyaccountid""/>" +
                                                        @"<filter type=""and"">" +
                                                        @"<condition attribute=""new_contactid"" operator=""eq"" value=""{0}"" />" +
                                                        @"<condition attribute=""statecode"" operator=""eq"" value=""0"" />" +
                                                        @"<condition attribute=""new_primary"" operator=""eq"" value=""1"" />" +
                                                        @"</filter>" +
                                                        @"</entity>" +
                                                        @"</fetch>";
                // Retrieve Accounts with matching account ID
                EntityCollection phySpecialtyRecords = service.RetrieveMultiple(new FetchExpression(String.Format(searchPhySpecialtyOnContactID, contactID)));
                int phySpecialtyCount = phySpecialtyRecords.Entities.Count;
                if (phySpecialtyCount > 0)
                {
                    Entity phySpecialtyRecordFound = phySpecialtyRecords.Entities[0];
                    if (phySpecialtyRecordFound.Attributes.Contains("new_certbodyaccountid"))
                        certifyingBodyReference = (EntityReference)phySpecialtyRecordFound.Attributes["new_certbodyaccountid"];
                    if (phySpecialtyRecordFound.Attributes.Contains("new_specialtyid"))
                        specialtyReference = (EntityReference)phySpecialtyRecordFound.Attributes["new_specialtyid"];
                }

                if (certifyingBodyReference != null)
                {
                    string searchAccountOnCertifyingBodyID = @"<fetch version=""1.0"" output-format=""xml-platform"" mapping=""logical"" distinct=""false"">" +
                                                                @"<entity name=""account"">" +
                                                                @"<attribute name=""new_commonname""/>" +
                                                                @"<filter type=""and"">" +
                                                                @"<condition attribute=""accountid"" operator=""eq"" value=""{0}"" />" +
                                                                @"<condition attribute=""statecode"" operator=""eq"" value=""0"" />" +
                                                                @"</filter>" +
                                                                @"</entity>" +
                                                                @"</fetch>";
                    // Retrieve Accounts with matching account ID
                    EntityCollection certBodyAccounts = service.RetrieveMultiple(new FetchExpression(String.Format(searchAccountOnCertifyingBodyID, certifyingBodyReference.Id)));
                    int certBodyAccountCount = certBodyAccounts.Entities.Count;
                    if (certBodyAccountCount > 0)
                    {
                        Entity certBodyAccountFound = certBodyAccounts.Entities[0];
                        if (certBodyAccountFound.Attributes.Contains("new_commonname"))
                            commonName = (string)certBodyAccountFound.Attributes["new_commonname"];
                    }

                }

                if (specialtyReference != null)
                {
                    string searchSpecialtyOnID = @"<fetch version=""1.0"" output-format=""xml-platform"" mapping=""logical"" distinct=""false"">" +
                                                                @"<entity name=""new_specialty"">" +
                                                                @"<attribute name=""new_specialtycode""/>" +
                                                                @"<filter type=""and"">" +
                                                                @"<condition attribute=""new_specialtyid"" operator=""eq"" value=""{0}"" />" +
                                                                @"<condition attribute=""statecode"" operator=""eq"" value=""0"" />" +
                                                                @"</filter>" +
                                                                @"</entity>" +
                                                                @"</fetch>";
                    // Retrieve Accounts with matching account ID
                    EntityCollection specialtyRecords = service.RetrieveMultiple(new FetchExpression(String.Format(searchSpecialtyOnID, specialtyReference.Id)));
                    int specialtyRecordCount = specialtyRecords.Entities.Count;
                    if (specialtyRecordCount > 0)
                    {
                        Entity specialtyRecordFound = specialtyRecords.Entities[0];
                        if (specialtyRecordFound.Attributes.Contains("new_specialtycode"))
                            specialtyCode = (string)specialtyRecordFound.Attributes["new_specialtycode"];
                    }

                }

                content += @"""spec-Org"":""" + EscpateDoubleQuoteAndBackSlash(commonName) + @""",";
                content += @"""spec-Code"":""" + EscpateDoubleQuoteAndBackSlash(specialtyCode) + @""",";
                //content += """spec-Org"":""" + ec.Entities[0]["specialty.new_name"] + """,";

                //Membership Status
                string membershipStatus = "";
                if (CMA_MembershipDetailReference == null)
                    membershipStatus = "Never";
                else
                    membershipStatus = (cmaMembershipDiscontinuedDate == null) ? "Active" : "Discontinued";
                content += @"""mem-Status"":""" + EscpateDoubleQuoteAndBackSlash(membershipStatus) + @""",";

                content += @"""mem-Dis-Dt"":""" + EscpateDoubleQuoteAndBackSlash(memDst) + @""",";
                content += @"""mem-Exp-Dt"":""" + EscpateDoubleQuoteAndBackSlash(memexp) + @""",";

                content += !ec.Entities[0].Contains("emailaddress1") ? @"""e-Mail"":""""," : @"""e-Mail"":""" + ec.Entities[0]["emailaddress1"] + @""",";
                content += !ec.Entities[0].Contains("new_deathdate") ? @"""death-Date"":""""," : @"""death-Date"":""" + ((DateTime)ec.Entities[0]["new_deathdate"]).ToString("yyyyMMdd") + @""",";


                // Home Address Fields
                string addressLine1 = String.Empty;
                string addressLine2 = String.Empty;
                string addressLine3 = String.Empty;
                string city = String.Empty;
                string province = String.Empty;
                string postalCode = String.Empty;
                string country = String.Empty;

                // first retrieve the home address record using the contactID
                string searchHomeAddressOnCmaID = @"<fetch version=""1.0"" output-format=""xml-platform"" mapping=""logical"" distinct=""false"">" +
                                                    @"<entity name=""customeraddress"">" +
                                                    @"<attribute name=""line1""/>" +
                                                    @"<attribute name=""line2""/>" +
                                                    @"<attribute name=""line3""/>" +
                                                    @"<attribute name=""city""/>" +
                                                    @"<attribute name=""new_stateorprovincemasterlist""/>" +
                                                    @"<attribute name=""postalcode""/>" +
                                                    @"<attribute name=""new_country""/>" +
                                                    @"<filter type=""and"">" +
                                                    @"<condition attribute=""parentid"" operator=""eq"" value=""{0}"" />" +
                                                    @"<condition attribute=""new_status"" operator=""eq"" value=""1"" />" +
                                                    @"<condition attribute=""new_cmapreferred"" operator=""eq"" value=""1"" />" +
                                                    @"</filter>" +
                                                    @"</entity>" +
                                                    @"</fetch>";
                // Retrieve the Address record
                EntityCollection addressRecords = service.RetrieveMultiple(new FetchExpression(String.Format(searchHomeAddressOnCmaID, contactID)));
                int addressRecordCount = addressRecords.Entities.Count;

                if (addressRecordCount > 0)
                {
                    Entity addressRecordFound = addressRecords.Entities[0];

                    if (addressRecordFound.Attributes.Contains("line1"))
                    { // Only access the address recored, if address line 1 contains any data then 
                        addressLine1 = (string)addressRecordFound.Attributes["line1"];
                        if (addressRecordFound.Attributes.Contains("line2"))
                            addressLine2 = (string)addressRecordFound.Attributes["line2"];
                        if (addressRecordFound.Attributes.Contains("line3"))
                            addressLine3 = (string)addressRecordFound.Attributes["line3"];
                        if (addressRecordFound.Attributes.Contains("city"))
                            city = (string)addressRecordFound.Attributes["city"];
                        if (addressRecordFound.Attributes.Contains("new_stateorprovincemasterlist"))
                            province = (string)addressRecordFound.FormattedValues["new_stateorprovincemasterlist"];
                        if (addressRecordFound.Attributes.Contains("postalcode"))
                            postalCode = (string)addressRecordFound.Attributes["postalcode"];
                        if (addressRecordFound.Attributes.Contains("new_country"))
                            country = (string)addressRecordFound.FormattedValues["new_country"];
                    }

                }
                content += string.IsNullOrWhiteSpace(addressLine1) ? @"""addressLine1"":""""," : @"""addressLine1"":""" + EscpateDoubleQuoteAndBackSlash(addressLine1) + @""",";
                content += string.IsNullOrWhiteSpace(addressLine2) ? @"""addressLine2"":""""," : @"""addressLine2"":""" + EscpateDoubleQuoteAndBackSlash(addressLine2) + @""",";
                content += string.IsNullOrWhiteSpace(addressLine3) ? @"""addressLine3"":""""," : @"""addressLine3"":""" + EscpateDoubleQuoteAndBackSlash(addressLine3) + @""",";
                content += string.IsNullOrWhiteSpace(city) ? @"""city"":""""," : @"""city"":""" + EscpateDoubleQuoteAndBackSlash(city) + @""",";
                content += string.IsNullOrWhiteSpace(province) ? @"""province"":""""," : @"""province"":""" + EscpateDoubleQuoteAndBackSlash(province) + @""",";
                content += string.IsNullOrWhiteSpace(postalCode) ? @"""postalCode"":""""," : @"""postalCode"":""" + EscpateDoubleQuoteAndBackSlash(postalCode) + @""",";
                content += string.IsNullOrWhiteSpace(country) ? @"""country"":""""," : @"""country"":""" + EscpateDoubleQuoteAndBackSlash(country) + @""",";

                content += !ec.Entities[0].Contains("telephone2") ? @"""homePhoneNumber"":""""," : @"""homePhoneNumber"":""" + EscpateDoubleQuoteAndBackSlash(ec.Entities[0]["telephone2"] as string) + @""",";
                content += !ec.Entities[0].Contains("telephone1") ? @"""businessPhoneNumber"":""""," : @"""businessPhoneNumber"":""" + EscpateDoubleQuoteAndBackSlash(ec.Entities[0]["telephone1"] as string)+ @""",";
                content += !ec.Entities[0].Contains("new_cmapreferredcontactmethod") ? @"""preferredContactMethod"":""""," : @"""preferredContactMethod"":""" + EscpateDoubleQuoteAndBackSlash(ec.Entities[0].FormattedValues["new_cmapreferredcontactmethod"]) + @""",";
                content += !ec.Entities[0].Contains("new_language") ? @"""preferredLanguage"":""""," : @"""preferredLanguage"":""" + EscpateDoubleQuoteAndBackSlash(ec.Entities[0].FormattedValues["new_language"]) + @""",";


                #region Standard-Aud Type
                //Standard Audience Type
                string Standard_Audience_Type = String.Empty;

                //Following is the criteria used for Standar Audience Type:
                //1.Member – Practicing Physician  (when Membership Status = Member,  Practice Status is Active, Contact-Type is Physician and Sub-Contact Type is not equal to Student OR Resident)
                //2.Member – Retired Physician (when Membership Status = Member,  Practice Status is Retired, Contact-Type is Physician and Sub-Contact Type is not equal to Student OR Resident)
                //3.Member – Resident  (when Membership Status = Member,  Contact-Type is Physician and Sub-Contact Type is equal to Resident)
                //4.Member – Student (when Membership Status = Member,  Contact-Type is Physician and Sub-Contact Type is equal to Student)
                //5.Non-member – Non-Physician   (when Membership Status = Non-Member,  Contact-Type is NOT equal to Physician)
                //6.Non-member – Practicing Physician (when Membership Status = Non-Member,  Practice Status is Active, Contact-Type is Physician and Sub-Contact Type is not equal to Student OR Resident)
                //7.Non-member – Retired Physician (when Membership Status = Non-Member,  Practice Status is Retired, Contact-Type is Physician and Sub-Contact Type is not equal to Student OR Resident)
                //8.Non-member – Resident  (when Membership Status = Non-Member,  Contact-Type is Physician and Sub-Contact Type is equal to Resident)
                //9.Non-member – Student  (when Membership Status = Non-Member,  Contact-Type is Physician and Sub-Contact Type is equal to Student)
                //10.Employee (when Contact Type == Employee)

                string SAT_memStat = (membershipStatus == "Active") ? "Member" : "Non-Member";
                string SAT_pracStat = (practiceStatus == "Active") ? "Practicing" : (practiceStatus == "Retired") ? "Retired" : String.Empty;

                // contactType.Value = 100000001 is Physician
                // contactSubType.Value = 100000001 is Resident :: contactSubType.ToLower().Contains("resident")
                // contactSubType.Value = 100000000 is Student  :: contactSubType.ToLower().Contains("student")

                //Set the default
                Standard_Audience_Type = "Non-member Non-Physician";

                // if ((SAT_memStat == "Member") && (SAT_pracStat == "Practicing") && (contactType.Value == 100000001) && ((contactSubTypeValue != 100000001) || (contactSubTypeValue != 100000000)))
                if ((SAT_memStat == "Member") && (SAT_pracStat == "Practicing") && (ContacType.StartsWith("physician", true, CultureInfo.InvariantCulture)) && (!contactSubType.StartsWith("resident", true, CultureInfo.InvariantCulture) || !contactSubType.StartsWith("student", true, CultureInfo.InvariantCulture)))
                    Standard_Audience_Type = "Member Practicing Physician";

                //if ((SAT_memStat == "Member") && (SAT_pracStat == "Retired") && (contactType == 100000001) && ((contactSubTypeValue != 100000001) || (contactSubTypeValue != 100000000)))
                if ((SAT_memStat == "Member") && (SAT_pracStat == "Retired") && (ContacType.StartsWith("physician", true, CultureInfo.InvariantCulture)) && (!contactSubType.StartsWith("resident", true, CultureInfo.InvariantCulture) || !contactSubType.StartsWith("student", true, CultureInfo.InvariantCulture)))
                    Standard_Audience_Type = "Member Retired Physician";

                //if ((SAT_memStat == "Member") && (contactType.Value == 100000001) && (contactSubTypeValue == 100000001))
                if ((SAT_memStat == "Member") && (ContacType.StartsWith("physician", true, CultureInfo.InvariantCulture)) && contactSubType.StartsWith("resident", true, CultureInfo.InvariantCulture))
                    Standard_Audience_Type = "Member Resident";

                //if ((SAT_memStat == "Member") && (contactType.Value == 100000001) && (contactSubTypeValue == 100000000))
                if ((SAT_memStat == "Member") && (ContacType.StartsWith("physician", true, CultureInfo.InvariantCulture)) && contactSubType.StartsWith("student", true, CultureInfo.InvariantCulture))
                    Standard_Audience_Type = "Member Student";

                // if ((SAT_memStat == "Non-Member") && (contactType.Value != 100000001))
                if ((SAT_memStat == "Non-Member") && (!ContacType.StartsWith("physician", true, CultureInfo.InvariantCulture)))
                    Standard_Audience_Type = "Non-member Non-Physician";

                //if ((SAT_memStat == "Non-Member") && (SAT_pracStat == "Practicing") && (contactType.Value == 100000001) && ((contactSubTypeValue != 100000001) || (contactSubTypeValue != 100000000)))
                if ((SAT_memStat == "Non-Member") && (SAT_pracStat == "Practicing") && (ContacType.StartsWith("physician", true, CultureInfo.InvariantCulture)) && (!contactSubType.StartsWith("resident", true, CultureInfo.InvariantCulture) || !contactSubType.StartsWith("student", true, CultureInfo.InvariantCulture)))
                    Standard_Audience_Type = "Non-member Practicing Physician";

                //if ((SAT_memStat == "Non-Member") && (SAT_pracStat == "Retired") && (contactType.Value == 100000001) && ((contactSubTypeValue != 100000001) || (contactSubTypeValue != 100000000)))
                if ((SAT_memStat == "Non-Member") && (SAT_pracStat == "Retired") && (ContacType.StartsWith("physician", true, CultureInfo.InvariantCulture)) && (!contactSubType.StartsWith("resident", true, CultureInfo.InvariantCulture) || !contactSubType.StartsWith("student", true, CultureInfo.InvariantCulture)))
                    Standard_Audience_Type = "Non-member Retired Physician";

                //if ((SAT_memStat == "Non-Member") && (contactType.Value == 100000001) && (contactSubTypeValue == 100000001))
                if ((SAT_memStat == "Non-Member") && (ContacType.StartsWith("physician", true, CultureInfo.InvariantCulture)) && contactSubType.StartsWith("resident", true, CultureInfo.InvariantCulture))
                    Standard_Audience_Type = "Non-member Resident";

                //if ((SAT_memStat == "Non-Member") && (contactType.Value == 100000001) && (contactSubTypeValue == 100000000))
                if ((SAT_memStat == "Non-Member") && (ContacType.StartsWith("physician", true, CultureInfo.InvariantCulture)) && contactSubType.StartsWith("student", true, CultureInfo.InvariantCulture))
                    Standard_Audience_Type = "Non-member Student";

                if (ContacType.StartsWith("employee", true, CultureInfo.InvariantCulture) || ContacType.StartsWith("emloyee", true, CultureInfo.InvariantCulture))
                    Standard_Audience_Type = "Employee";

                content += @"""standard-Audience-Type"":""" + EscpateDoubleQuoteAndBackSlash(Standard_Audience_Type) + @""",";

                #endregion

                content += !ec.Entities[0].Contains("new_gc_audience_type") ? @"""gC-Audience-Type"":""""," : @"""gC-Audience-Type"":""" + EscpateDoubleQuoteAndBackSlash(ec.Entities[0].FormattedValues["new_gc_audience_type"]) + @""",";
                content += !ec.Entities[0].Contains("new_staff_audience_type") ? @"""staff-Audience-Type"":""""," : @"""staff-Audience-Type"":""" + EscpateDoubleQuoteAndBackSlash(ec.Entities[0].FormattedValues["new_staff_audience_type"]) + @""",";
                content += !ec.Entities[0].Contains("new_contacttypeid") ? @"""contact-Type"":""""," : @"""contact-Type"":""" + EscpateDoubleQuoteAndBackSlash(ContacTypeEng) + @""",";
                content += !ec.Entities[0].Contains("new_contactsubtypeid") ? @"""contact-Sub-Type"":""""," : @"""contact-Sub-Type"":""" + EscpateDoubleQuoteAndBackSlash(contactSubTypeEng) + @""",";
                content += @"""miP-Status"":"""",";
                content += @"""lastPTMA"":""" + EscpateDoubleQuoteAndBackSlash(divAccountName) + @"""";
                content += "}";

                #endregion

                outPara.Set(executionContext, content);

            }
            else
            {
                content = "{}";
                outPara.Set(executionContext, content);
            }
        }

        static string EscpateDoubleQuoteAndBackSlash(string inputString)
		{
			var outputString = inputString;
			if (!string.IsNullOrWhiteSpace(inputString))
			{
				outputString = outputString.Replace(@"\", @"\\").Replace(@"""", @"\""");
			}
			return outputString; 
		}

    }
}
