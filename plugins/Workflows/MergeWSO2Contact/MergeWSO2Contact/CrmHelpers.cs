using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;


namespace MergeWSO2Contact
{

    #region enums    
    public enum AddressSource
        {
            CMAUpdate = 100000001,
            LicencingBody = 100000005,
            IndividualChange = 100000000,
            MeetingTravel = 100000007,
            OtherSocieties = 100000006,
            PTMA = 100000004,
            Subscriber = 100000008
        }
        public enum AddressType
        {
            Home = 100000004,
            Professional = 100000002,
            Other = 100000008
        }
        public enum AppCountryCode
        {
            Canada = 100000038,
            USA = 100000232,
            Other = 100000002
        }
        public enum ApprovalStatus
        {
            Draft = 100000000,
            Approved = 100000001,
            UnderReview = 100000002,
            RequiresFollowUp = 100000003,
            FollowUpCompleted = 100000004,
            Cancelled = 100000005
        }
        public enum ApplicationPracticeStatus
        {
            PracticingPhysician = 100000000,
            NonPracticingAcademic = 100000009,
            FirstYearInPractice = 100000010,
            PartTime = 100000006,
            MaternityLeave = 100000008,
            Retired = 100000002,
            Locum = 100000011
        }
        public enum ContactPracticeStatus
        {
            InPractice = 100000000,
            Removed = 100000003,
            Retired = 100000002,
            SemiRetired = 100000001,
            Military = 100000004,
            NotinPrivatePractice = 100000005,
            PartTime = 100000006,
            Salaried = 100000007,
            MaternityLeave = 100000008,
            NonPracticingAcademic = 100000009
        }

        public enum ContactLanguage
        {
            English = 100000000,
            French = 100000001
        }

        public enum ContactSalutation
        {
            BrgGeneral = 100000026,
            Captn = 100000005,
            Col = 100000006,
            Dr = 100000023,
            Drdot = 100000004,
            Dre = 100000017,
            Hon = 100000007,
            Judge = 100000008,
            Juge = 100000024,
            LHon = 100000021,
            Lieut = 100000009,
            M = 100000018,
            Major = 100000010,
            Miss = 100000002,
            Mlle = 100000020,
            Mme = 100000019,
            Mr = 100000000,
            Mrs = 100000001,
            Ms = 100000003,
            Mx = 100000025,
            Prof = 100000016,
            Rev = 100000011,
            Sen = 100000012,
            Sgt = 100000013,
            Sir = 100000014,
            BGFrMale = 100000027,
            BGFrFemale = 100000028,
            Sister = 100000015,
            Soeur = 100000022,
            Phd = 100000029
        }
        public enum CMAPricelistType
        {
            Event = 1,
            Membership = 2,
            Other = 3
        }
        public enum PortalReviewStatus
        {
            New = 100000000,
            ReviewedApproved = 100000001,
            ReviewedMerged = 100000002
        }
        #endregion enums
        public static class CmaHelpers
        {
            public static T GetConfigLookupValue<T>(IOrganizationService service, string key)
            {
                string result = null;
                string fetchXML = $@"<fetch>
                                 <entity name='new_customconfiguration' >
                                    <attribute name='new_value' />
                                    <attribute name='new_longvalue' />
                                    <filter>
                                        <condition attribute='new_key' operator='eq' value='{key}' />
                                    </filter>
                                 </entity>
                                 </fetch>";

                var lookupResult = service.RetrieveMultiple(new FetchExpression(fetchXML));
                if (lookupResult.Entities.Count > 0)
                {
                    string shortValue = lookupResult.Entities[0].GetAttributeValue<string>("new_value");
                    string longValue = lookupResult.Entities[0].GetAttributeValue<string>("new_longvalue");
                    result = !string.IsNullOrWhiteSpace(shortValue) ? shortValue : longValue;
                }
            result = result != null ? result.Trim() : result;
            return (T)Convert.ChangeType(result, typeof(T));
            }
            public static string GetErrorMessage(IOrganizationService service, Guid userId, string errorCode)
            {
                string result = null;
                string fetchXML = $@"<fetch>
                                 <entity name='new_codelookup' >
                                    <attribute name='new_englishmessage' />
                                    <attribute name='new_frenchmessage' />
                                    <order attribute = 'new_code' descending = 'false' />
                                    <filter>
                                        <condition attribute='new_code' operator='eq' value='{errorCode}' />
                                    </filter>
                                 </entity>
                                 </fetch>";

                var lookupResult = service.RetrieveMultiple(new FetchExpression(fetchXML));
                if (lookupResult.Entities.Count > 0)
                {
                    Entity codelookup = lookupResult.Entities[0];
                    switch (getUserLanguage(service, userId))
                    {
                        case "FR":
                            result = codelookup.GetAttributeValue<string>("new_frenchmessage");
                            break;
                        default:
                            result = codelookup.GetAttributeValue<string>("new_englishmessage");
                            break;
                    }
                }
                return result;
            }

            public static string getUserLanguage(IOrganizationService service, Guid userId)
            {
                var code = "EN";
                const int UI_LANGUAGE_FRENCH = 1036;
                string fetchXML = $@"<fetch>
                                    <entity name='usersettings' >
                                    <attribute name='uilanguageid' />
                                    <filter>
                                        <condition attribute='systemuserid' operator='eq' value='{userId}' />
                                    </filter>
                                    </entity>
                                    </fetch>";

                var result = service.RetrieveMultiple(new FetchExpression(fetchXML));
                if (result.Entities.Count != 0)
                {
                    Entity e = result.Entities[0];
                    if (e.Contains("uilanguageid"))
                        code = e.GetAttributeValue<int>("uilanguageid") == UI_LANGUAGE_FRENCH ? "FR" : "EN";
                }
                return code;
            }
            public static EntityReference GetPricelist(IOrganizationService service, int year, CMAPricelistType type)
            {
                EntityReference result = null;

                string beginDateString = $"{year - 1}-12-31";
                string endDateString = $"{year + 1}-1-1";
                //get unique suffix for this year
                string fetchXml = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                    <entity name='pricelevel'>
                        <filter type='and'>
                            <condition attribute='new_type' operator='eq' value='{(int)type}' />
                            <condition attribute='begindate' operator='gt' value='{beginDateString}' />
                            <condition attribute='enddate' operator='lt' value='{endDateString}' />
                        </filter>
                        <order attribute='enddate' descending='true' />
                    </entity>
                </fetch>";
                result = CrmHelpers.GetFirstFromExecuteFetch(service, fetchXml)?.ToEntityReference();

                return result;
            }
            public static string GetBatchId(IOrganizationService service, int year, string ptmaCode)
            {
                string result = "";
                if (year < 2019)
                    throw new Exception($"GetBatchNumber() Year: '{year}' is illegal");
                result = (year - 2000).ToString("D2");

                result += ptmaCode;

                //get unique suffix for this year
                string fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                    <entity name='new_batch'>
                        <attribute name='new_name' />
                        <filter type='and'>
                            <condition attribute='new_name' operator='like' value='" + result + @"%' />
                        </filter>
                    </entity>
                </fetch>";

                int largestnum = 1;
                foreach (Entity existingBatch in CrmHelpers.ExecuteFetchEnumerable(service, fetchXml))
                {
                    string batchName = existingBatch.GetAttributeValue<string>("new_name");
                    int digitsinBatch = 2;
                    while (batchName.Length > (4 + digitsinBatch) && batchName[4 + digitsinBatch] != '-')
                        digitsinBatch++;
                    string existingBatchNo = batchName.Substring(4, digitsinBatch);
                    int tempInt = 0;
                    if (Int32.TryParse(existingBatchNo, out tempInt))
                    {
                        if (tempInt > largestnum)
                            largestnum = tempInt;
                    }
                }
                result += (largestnum + 1).ToString("D2");

                return result;
            }
            public static EntityReference GetMembershipXref(IOrganizationService service, Guid productId, Guid divisionId)
            {
                EntityReference result = null;

                string fetchXml = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false' >
                                  <entity name='new_membercategoryxref' >
                                    <filter>
                                      <condition attribute='new_divassocaccountid' operator='eq' value='{divisionId}' />
                                      <condition attribute='new_categoryproductid' operator='eq' value='{productId}' />
                                    </filter>
                                  </entity>
                                </fetch>";
                result = CrmHelpers.GetFirstFromExecuteFetch(service, fetchXml)?.ToEntityReference();

                return result;
            }
            public static bool OnlineApplicationHasBatch(IOrganizationService service, Guid applicationId)
            {
                //check for previous batch for this application 
                string fetchXML = $@"<fetch>
                                  <entity name='new_batch' >
                                    <attribute name='new_batchid' />
                                    <filter>
                                      <condition attribute='new_onlineapplicationid' operator='eq' value='{applicationId}' />
                                    </filter>
                                  </entity>
                                </fetch>";

                return (CrmHelpers.GetFirstFromExecuteFetch(service, fetchXML) != null);
            }


        }

    }