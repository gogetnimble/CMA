using System.Diagnostics.CodeAnalysis;

namespace Cma.Services.Crm;

[ExcludeFromCodeCoverage]
public static class CrmConstants
{
    public static class Contact
    {
        public const string EntityName = "contact";
        public const string Id = "contactid";
        public const string ContactTypeId = "new_contacttypeid";
        public const string ContactSubTypeId = "new_contactsubtypeid";
        public const string CmahId = "new_cmah_id";
        public const string PreferredEmailAddress = "emailaddress1";
        public const string FirstName = "firstname";
        public const string LastName = "lastname";
        public const string Username = "adx_identity_username";
        public const string Language = "new_language";
        public const string ExcludeSalutation = "new_excludesalutation";
        public const string CreationReason = "new_creationreason";

        public const string PortalInvitationUrl = "adx_portalinvitationurl";
        public const string PasswordHash = "adx_identity_passwordhash";
        public const string MembershipDetailId = "new_cmamembershipdetailid";
        public const string PreferredEmailAddressConfirmed = "adx_identity_emailaddress1confirmed";
        public const string GraduationYear = "new_gradyear";
        public const string MembershipStatus = "new_membershipstatus";
        public const string LastLoginDate = "new_lastlogindate";
        public const string CmaId = "new_contact_id";
        public const string PracticeStatus = "new_practicestatus";

        public const string DateOfBirth = "birthdate";
        public const string DateOfDeath = "new_deathdate";
        public const string FirstYearInPractice = "new_1styearinpractice";
        public const string BusinessPhoneNumber = "telephone1";
        public const string HomePhoneNumber = "telephone2";
        public const string BusinessExtension = "new_businessphoneextension";
        public const string MobilePhone = "mobilephone";
        public const string CmaSmsConsent = "new_cmasmsconsent";
        public const string Salutation = "new_salutation";
        public const string PreferredName = "new_physicianpreferredname";
        public const string Bio = "new_communityplatformbio";
        public const string Pronoun = "new_pronoun";
        public const string OtherPronoun = "new_otherpronoun";
        public const string ProgramOfResidencyOrFellowship = "new_residencyprogram";
        public const string UniversityOfResidencyOrFellowship = "new_universityofresidency";
        public const string MedicalSchoolName = "new_collegeofgraduation";
        public const string YearEnrolledInMedicalSchool = "new_yearenrolledinmedicalschool";
        public const string Fellowship = "new_fellowship";
        public const string ExpectedYearOfResidencyCompletion = "new_residencycompletion";
        public const string LocumEstimatedCompletionDate = "new_locum_estcompdate";
        public const string Locum = "new_locum";
        public const string DateOfRetirement = "new_dateretirement";
        public const string StatusReason = "statuscode";
        public const string DoNotAllowBulkEmails = "donotbulkemail";
        public const string YearObtained = "new_gradyear";
        public const string EmailConfirmed = "adx_identity_emailaddress1confirmed";
        public const string FamilyPhysicianNumber = "new_familyphysiciannumber";
        public const string CfpcId = "new_familyphysiciannumber";
        public const string Address = "customeraddress";
        public const string Address1City = "address1_city";  //for addresses the preferred address is always address1
        public const string Address1Country = "address1_country";
        public const string Address1Province = "address1_stateorprovince";
        public const string CustomerInsightsTrackingId = "new_customerinsightstrackingid";
        public const string WidgetPostalCode = "new_widgetpostalcode";


        public static class LanguageKey
        {
            public const int English = 100000000;
            public const int French = 100000001;
        }

        public static class PracticeStatusKey
        {
            public const int InPractice = 100000000;
            public const int FirstYearInPractice = 100000010;
            public const int Removed = 100000003;
            public const int Retired = 100000002;
            public const int SemiRetired = 100000001;
            public const int Military = 100000004;
            public const int NotInPrivatePractice = 100000005;
            public const int MaternityPaternityLeave = 100000008;
            public const int PartTime = 100000006;
            public const int Salaried = 100000007;
            public const int NonPracticingAcademicPhysician = 100000009;
            
        }
        
        public static class MembershipStatusKey
        {
            public const int Active = 100000000;
            public const int Pending = 100000001;
            public const int Inactive = 100000002;
            public const int Discontinued = 100000003;
        }

        public static class CreationReasonKey
        {
            public const int Membership = 100000000;
            public const int OnlineAccountCreation = 100000001;
            public const int EventRegistrationOrAttendance = 100000002;
            public const int EngagementPlatform = 100000003;
            public const int BusinessContact = 100000004;
            public const int PliCourse = 100000005;
            public const int CmajInterest = 100000006;
            public const int GrantApplication = 100000007;
            public const int ProgramApplication = 100000008;
            public const int StudentOrientation = 100000010;
            public const int CMACAMktgSignUpWidget = 100000012;
            public const int CMAJCAMktgSignUpWidget = 100000013;
            public const int HC4RealMktgSignUpWidget = 100000014;
        }

        public static class StatusReasonKey
        {
            public const int Active = 1;
        }

        public static class LocumKey
        {
            public const int Yes = 100000000;
            public const int No = 100000001;
        }
        
        public static class PhoneTypeName
        {
            public const string TelePhone1 = "telephone1";
            public const string TelePhone2 = "telephone2";
            public const string MobilePhone = "mobilephone";
        }
    }

    public static class Event
    {
        public const string EntityName = "new_event";
        public const string Name = "new_name";
        public const string EventId = "new_aventrieventid";
        public const string Description = "new_description";
        public const string StartDate = "new_startdate";
        public const string EndDate = "new_enddate";
        public const string Location = "new_location";
        public const string Subject = "subject";
        public const string EventType = "new_eventtypeid";
        public const string EventSubType = "new_eventsubtypeid";
    }

    public static class EventAttendee {
        public const string EntityName = "new_eventattendee";
        public const string Contact = "new_contactid";
        public const string Email = "new_email";
        public const string AttendeeType = "new_attendeetypeid";
        public const string EventId = "new_eventid";
        public const string Status = "new_attendancestatus";
        public const string FirstName = "new_attendeefirstname";
        public const string LastName = "new_attendeelastname";
        public const string City = "new_attendeecity";
    }

    public static class Opt
    {
        public const string EntityName = "new_opt";
        public const string ParentId = "new_contactid";
        public const string OptSubscriptionId = "new_subscriptionid";
        public const string ParentAddressId = "new_addressid";
        public const string AddressLine1 = "new_line1";
        public const string AddressLine2 = "new_line2";
        public const string AddressLine3 = "new_line3";
        public const string Country = "new_country";
        public const string PostalCodeOrZipCode = "new_postalcode";
        public const string City = "new_city";
        public const string ProvinceOrState = "new_stateorprovincemasterlist";
        public const string AddressType = "new_addresstype";

        public static class OptSubscriptionKey
        {
            public const string CmajSubscription = "e0768906-ec2a-e211-a68d-101f742f742c";
        }
    }

    public static class Address
    {
        public const string EntityName = "customeraddress";
        public const string ParentId = "parentid";
        public const string ProvinceOrState = "new_stateorprovincemasterlist";
        public const string AddressLine1 = "line1";
        public const string AddressLine2 = "line2";
        public const string AddressLine3 = "line3";
        public const string Country = "new_country";
        public const string PostalCodeOrZipCode = "postalcode";
        public const string City = "city";
        public const string PreferredAddressForCmaMailings = "new_cmapreferred";
        public const string AddressType = "addresstypecode";
        public const string AddressNumber = "addressnumber";
        public const string Verified = "new_isverified";
        public const string Status = "new_status";
        public const string AddressSource = "new_addresssource";
        public const string ReturnMail = "new_returnmail";

        public static class AddressSourceKey
        {
            public const int WebsiteUpdate = 100000001;
            public const int Application = 100000009;
            public const int CmaMarketingWidget = 100000010;
        }
        
        public static class AddressTypeKey
        {
            public const int Home = 100000004;
            public const int Professional = 100000002;
            public const int Other = 100000008;
        }
        
        public static class AddressTypeName
        {
            public const string Home = "home";
            public const string Professional = "office";
            public const string Other = "other";
        }

        public static class StatusKey
        {
            public const bool Active = true;
        }
    }

    public static class CmaMembershipDetail
    {
        public const string EntityName = "new_cmamembershipdetail";
        public const string Id = "new_cmamembershipdetailid";
        public const string MembershipStatus = "new_status";
        public const string ExpiryDate = "new_expirydate";
        public const string MembershipYear = "new_membershipyear";
        public const string Category = "new_categoryproductid";
        public const string OrderId = "new_orderid";
        public const string Ptma = "new_divassocaccountid";
        public const string Contact = "new_contact";
    }
    
    public static class Product
    {
        public const string EntityName = "product";
        public const string Id = "productid";
        public const string Name = "name";
        public const string IsHonoraryProduct = "new_ishonoraryproduct";
        public const string ProductNumber = "productnumber";
        public const string ProductTypeCode = "producttypecode";
        public const string MemberClass = "new_memberclass";
    }

    public static class Order
    {
        public const string EntityName = "salesorder";
        public const string Id = "salesorderid";
        public const string AmountOutstanding = "new_amountoutstanding";
        public const string LineItemDiscountAmount = "new_totallineitemdiscount";
        public const string AmountPaid = "new_amountpaid";
        public const string TotalAmount = "totalamount";
        public const string CreatedOn = "createdon";
        public const string SubTotal = "totallineitemamount";
        public const string Gst = "new_tax1";
        public const string Hst = "new_tax2";
        public const string Qst = "new_tax3";
        public const string GstPercentage = "new_tax1_percentage";
        public const string HstPercentage = "new_tax2_percentage";
        public const string QstPercentage = "new_tax3_percentage";
        public const string TaxJurisdiction = "new_jurisdiction";
    }

    public static class ContactType
    {
        public const string EntityName = "new_contacttype";
        public const string Id = "new_contacttypeid";
        public const string Code = "new_contacttype_id";
        public const string Name = "new_name";

        public static class ContactTypeKey
        {
            public const string Other = "CT1016";
            public const string MemberOfPublic = "CT1014";
            public const string Physician = "CT1017";
            public const string Resident = "CT1019";
            public const string Student = "CT1021";
            public const string Employee = "CT1004";
            public const string SelfIdentifiedPhysician = "CT1106";
        }
    }

    public static class Email
    {
        public const string EntityName = "email";
        public const string Subject = "subject";
        public const string Body = "body";
        public const string SystemUser = "systemuser";
        public const string Description = "description";
        public const string From = "from";
        public const string To = "to";
        public const string RegardingObjectId = "regardingobjectid";
    }

    public static class EmailTemplate
    {
        public const string EntityName = "template";
        public const string TemplateId = "templateid";
        public const string Subject = "subject";
        public const string Body = "body";
        public const string Title = "title";
    }

    public static class ActivityParty
    {
        public const string EntityName = "activityparty";
        public const string PartyId = "partyid";

        public const string AddressUsed = "addressused";
    }

    public static class Queue
    {
        public const string EntityName = "queue";
        public const string Name = "name";
        public const string EmailAddress = "emailaddress";
    }

    public static class Annotation
    {
        public const string EntityName = "annotation";
        public const string ObjectId = "objectid";
        public const string NoteText = "notetext";
        public const string Subject = "subject";
    }

    public static class Post
    {
        public const string EntityName = "post";
        public const string Source = "source";
        public const string Type = "type";
        public const string Text = "text";
        public const string RegardingObjectId = "regardingobjectid";

        public static class SourceKey
        {
            public const int AutoPost = 1;
            public const int ManualPost = 2;
            public const int ActionHubPost = 3;
        }

        public static class TypeKey
        {
            public const int CheckIn = 1;
            public const int StatusUpdate = 2;
            public const int ManualPost = 3;
        }
    }

    public static class Application
    {
        public const string EntityName = "new_onlineapplication";
        public const string Id = "new_onlineapplicationid";
        public const string PaymentStatus = "new_paymentstatus";
        public const string PaymentMethod = "new_paymentmethod";
        public const string PaymentCreditCardType = "new_paymentcreditcardtype";
        public const string ApplicationSubmittedDate = "new_applicationsubmitteddate";
        public const string ExternalPartyTransactionId = "new_externalpartytransaction_id";
        public const string Name = "new_name";
        public const string ApplicationSource = "new_applicationsource";
        public const string EligibilityType = "new_eligibilitytypecode";
        public const string ApplicationType = "new_applicationtypecode";
        public const string PhysicianContact = "new_parentcustomerid";
        public const string PtmaId = "new_ptmaid";
        public const string Country = "new_countrycode";

        public const string TotalMembershipCost = "new_totalmembershipcost";
        public const string TotalMembershipDiscount = "new_totalmembershipdiscount";
        public const string Subtotal = "new_subtotal";
        public const string Hst = "new_tax2";
        public const string HstPercentage = "new_tax2_percentage";
        public const string Gst = "new_tax1";
        public const string GstPercentage = "new_tax1_percentage";
        public const string Qst = "new_tax3";
        public const string QstPercentage = "new_tax3_percentage";
        public const string GrandTotal = "new_grandtotal";
        public const string ProvinceStateCode = "new_provincestatecode";
        public const string OnlineApplicationId = "new_onlineapplicationid";
        public const string MembershipExpiryDate = "new_membershipexpirydate";
        public const string MembershipYear = "new_membershipyear";
        public const string ApplicationNumber = "new_application_id";
        public const string MembershipPriceListId = "new_membershippricelistid";
        public const string ApprovalStatus = "new_approvalstatuscode";
        public const string UniversityofGraduation = "new_collegeofgraduationid";
        public const string YearEnrolledinMedicalSchool = "new_yearenrolledinmedicalschool";
        public const string GraduationYear = "new_gradyear";

        public static class PaymentMethodKey
        {
            public const int CreditCard = 100000000;
            public const int PaymentNotRequired = 100000008;
        }

        public static class ApplicationTypeKey
        {
            public const int Join = 100000001;
            public const int Renew = 100000000;
        }

        public static class EligibilityTypeKey
        {
            public const int Direct = 100000000;
            public const int Indirect = 100000001;
        }

        public static class ApplicationSourceKey
        {
            public const int Portal = 100000000;
        }

        public static class PaymentStatusKey
        {
            public const int Success = 100000001;
            public const int Failed = 100000002;
            public const int NotRequired = 100000004;
        }

        public static class PaymentCreditCardTypeKey
        {
            public const int MasterCard = 100000004;
            public const int Visa = 100000003;
            public const int AmericanExpress = 100000005;
            public const int VisaDebit = 100000006;
            public const int MasterCardDebit = 100000007;
        }
        
        public static class ApprovalStatusKey
        {
            public const int Draft = 100000000;
            public const int Approved = 100000001;
            public const int UnderReview = 100000002;
            public const int RequiresFollowUp = 100000003;
            public const int FollowUpCompleted = 100000004;
            public const int Cancelled = 100000005;
        }
        
        public static class PracticeStatusKey
        {
            public const int PracticingPhysician = 100000000;
            public const int NonPracticingAcademicPhysician = 100000009;
            public const int FirstYearInPractice = 100000010;
            public const int PartTime = 100000006;
            public const int MaternityLeave = 100000008;
            public const int Retired = 100000002;
            public const int Locum = 100000011;
        }
    }

    public static class Account
    {
        public const string EntityName = "account";
        public const string Id = "accountid";
        public const string EnglishName = "new_englishname";
        public const string FrenchName = "new_frenchname";

        public static class PtmaIdKey
        {
            public static Guid Abroad = Guid.Parse("fc8350d6-d5d6-e811-9446-005056a81ff3");
            public static Guid AlbertaMedicalAssociation = Guid.Parse("34c891ef-d3d6-e811-9445-005056a81ff3");
            public static Guid Quebec = Guid.Parse("6138863f-c2a4-e911-a98c-000d3af3d307");
            public static Guid DoctorsManitoba = Guid.Parse("9a286ef8-ca48-ee11-be6f-0022483dd726");
            public static Guid DoctorsNovaScotia = Guid.Parse("4705f558-d5d6-e811-9446-005056a81ff3");
            public static Guid DoctorsOfBc = Guid.Parse("e9498532-d4d6-e811-9445-005056a81ff3");

            public static Guid MedicalSocietyOfPrinceEdwardIsland = Guid.Parse("25d2c272-d5d6-e811-9446-005056a81ff3");

            public static Guid NewBrunswickMedicalSociety = Guid.Parse("7e59ef48-d5d6-e811-9446-005056a81ff3");

            public static Guid NewfoundlandAndLabradorMedicalAssociation =
                Guid.Parse("50b1db8f-d5d6-e811-9446-005056a81ff3");

            public static Guid NorthwestTerritoriesMedicalAssociation =
                Guid.Parse("1977e3c1-d5d6-e811-9446-005056a81ff3");

            public static Guid OntarioMedicalAssociation = Guid.Parse("8084e506-3afd-e711-80e7-a0d3c1044d63");
            public static Guid SaskatchewanMedicalAssociation = Guid.Parse("8884575d-d4d6-e811-9445-005056a81ff3");
            public static Guid YukonMedicalAssociation = Guid.Parse("5edabda0-d5d6-e811-9446-005056a81ff3");
        }
    }

    public static class CountryKey
    {
        public const int Canada = 100000038;
        public const int UnitedStates = 100000232;
        public const int Other = 100000002;
    }
    public static class FilterOperator
    {
        public const string Equal = "eq";
    }

    public static class LinkType
    {
        public const string Outer = "outer";
    }
     public static class ContactPointConsent
    {
        public const string EntityName = "msdynmkt_contactpointconsent4";

        public const string Id = "msdynmkt_contactpointconsent4id";
        public const string PurposeId = "msdynmkt_purposeid";
        public const string EmailAddress = "msdynmkt_contactpointvalue";
        public const string TopicId = "msdynmkt_topicid";
        public const string ConsentStatus = "msdynmkt_value";
        public const string Reason = "msdynmkt_logicalreason";
        public const string Source = "msdynmkt_source";
        public const string StatusCode = "statuscode";
        public const string Channel = "msdynmkt_contactpointtype";
        public const string ConsentType = "msdynmkt_contactpointconsenttype";

        public static class StatusCodeKey
        {
            public const int Active = 1;
        }

        public static class SourceKey
        {
            public const int Internal = 534120000;
        }

        public static class ReasonKey
        {
            public const int NoReasons = 534119999;
        }

        public static class ChannelKey
        {
            public const int Email = 534120000;
        }

        public static class ConsentTypeKey
        {
            public const int Topic = 534120001;
        }

        public static class ConsentStatusKey
        {
            public const int NotSet = 534120000;
            public const int OptedIn = 534120001;
            public const int OptedOut = 534120002;
        }
    }

    public static class Topic
    {
        public const string EntityName = "msdynmkt_topic";
        public const string Id = "msdynmkt_topicid";
        public const string PurposeId = "msdynmkt_purposeid";
        public const string Name = "msdynmkt_name";
        public const string FrenchName = "new_frenchname";
        public const string Code = "new_code";
        public const string EnglishDescription = "new_englishdescription";
        public const string FrenchDescription = "new_frenchdescription"; 
        public const string TopicCategoryId = "new_category";
        public const string DisplayOrder = "new_displayorder";
        public const string PublishedToComplianceCenter = "new_publishedtocompliancecenter";
    }

    public static class TopicCategory
    {
        public const string EntityName = "new_topiccategory";
        public const string Id = "new_topiccategoryid";
        public const string Name = "new_name";
        public const string FrenchName = "new_frenchname";
    }

    public static class Purpose
    {
        public const string EntityName = "msdynmkt_purpose";
        public const string Id = "msdynmkt_purposeid";
        public const string Name = "msdynmkt_name";
        public const string ComplianceProfileId = "msdynmkt_msdynmkt_purpose_msdynmkt_compliancev4id";

        public static class NameKey
        {
            public const string Commercial = "Commercial";
        }
    }

    public static class ComplianceProfile
    {
        public const string EntityName = "msdynmkt_compliancesettings4";
        public const string Name = "msdynmkt_name";
    }

    public static class TrackingContext
    {
        public const string EntityName = "msdynmkt_trackingcontext";
        public const string EntityId = "msdynmkt_entityid";
        public const string EntityType = "msdynmkt_entitytype";
        public const string TrackingContextId = "msdynmkt_trackingcontextid";
    }

    public static class Common
    {
        public const string Status = "statecode";
        
        public static class StatusKey
        {
            public const int Active = 0;
            public const int Inactive = 1;
        }
        
        public static class VersionNumber
        {
            public const int VersionOne = 1;
            public const int VersionTwo = 2;
        }
    }
}