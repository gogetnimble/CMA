using Cma.Common;
using Cma.Common.Exceptions;
using Cma.Common.Extensions;
using Microsoft.Xrm.Sdk.Query;

namespace Cma.Services.Crm;

public static class CrmMapper
{
    public static class Language
    {
        public static int Map(string? languageCode)
        {
            return languageCode switch
            {
                Constants.Language.French => CrmConstants.Contact.LanguageKey.French,
                Constants.Language.English => CrmConstants.Contact.LanguageKey.English,
                _ => throw new BadRequestException(ErrorCode.ArgumentInvalid.ToSnakeCase(), "LanguageCode undefined.",
                    new
                    {
                        LanguageCode = languageCode
                    })
            };
        }
        
        public static string Map(int? contactLanguage)
        {
            return contactLanguage switch
            {
                CrmConstants.Contact.LanguageKey.French => Constants.Language.French,
                CrmConstants.Contact.LanguageKey.English => Constants.Language.English,
                _ => throw new BadRequestException(ErrorCode.ArgumentInvalid.ToSnakeCase(), "ContactLanguage undefined.",
                    new
                    {
                        ContactLanguage = contactLanguage
                    })
            };
        }
        
    }
    
    public static Guid? MapToNullableStringToEntityReference(string? key,string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }
        if (!Guid.TryParse(value, out var output))
        {
            throw new BadRequestException(ErrorCode.ArgumentInvalid.ToSnakeCase(), $"{key} undefined.",
                new
                {
                    variable = value
                });
        }

        return output;
    }
    
    public static int? MapToNullableIntOptionSet(string? key,string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }
        if (!int.TryParse(value, out var output))
        {
            throw new BadRequestException(ErrorCode.ArgumentInvalid.ToSnakeCase(), $"{key} undefined.",
                new
                {
                    variable = value
                });
        }

        return output;
    }

    public static DateTime? MapYear(string? key,string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        if (!int.TryParse(value, out var output))
        {
            throw new BadRequestException(ErrorCode.ArgumentInvalid.ToSnakeCase(), $"{key} undefined.",
                new
                {
                    variable = value
                });
        }

        return new DateTime(output, 1, 1);
    }
    
    public static DateTime? MapStringToDateTime(string? key,string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        if (!DateTime.TryParse(value, out var output))
        {
            throw new BadRequestException(ErrorCode.ArgumentInvalid.ToSnakeCase(), $"{key} undefined.",
                new
                {
                    variable = value
                });
        }

        return output;
    }

    public static class Operators
    {
        public static JoinOperator MapJoinOperator(string linkType)
        {
            return linkType switch
            {
                CrmConstants.LinkType.Outer => JoinOperator.LeftOuter,
                _ => throw new ArgumentOutOfRangeException(nameof(linkType), linkType, linkType)
            };
        }
        
        public static ConditionOperator MapFilterOperator(string filterOperator)
        {
            return filterOperator switch
            {
                CrmConstants.FilterOperator.Equal => ConditionOperator.Equal,
                _ => throw new ArgumentOutOfRangeException(nameof(filterOperator), filterOperator, filterOperator)
            };
        }
    }

    public static DateTime? MapPortalToCrmFirstYearInPractice(string? portalFirstYearInPractice, DateTime? crmFirstYearInPractice)
    {
        if (string.IsNullOrEmpty(portalFirstYearInPractice) && crmFirstYearInPractice is null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(portalFirstYearInPractice) && crmFirstYearInPractice is null)
        {
            if (!int.TryParse(portalFirstYearInPractice?.Trim(), out var year))
            {
                throw new ArgumentException("Invalid year provided for portalFirstYearInPractice.");
            }

            var localTime=new  DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
            
            // Assume CRM is using a timezone offset of -8 hours (e.g., Pacific Time)
            DateTime utcTime = localTime.AddHours(8); // Add hours to "reverse" the timezone difference for saving in UTC

            return utcTime;
        }

        if (!string.IsNullOrEmpty(portalFirstYearInPractice) && crmFirstYearInPractice is not null)
        {
            if (!int.TryParse(portalFirstYearInPractice?.Trim(), out var year))
            {
                throw new ArgumentException("Invalid year provided for portalFirstYearInPractice.");
            }
            var localTime=new  DateTime(year, crmFirstYearInPractice.Value.Month, crmFirstYearInPractice.Value.Day, 0, 0, 0,DateTimeKind.Unspecified);
            
            // Assume CRM is using a timezone offset of -8 hours (e.g., Pacific Time)
            DateTime utcTime = localTime.AddHours(8); // Add hours to "reverse" the timezone difference for saving in UTC

            return utcTime;
        }
        return null;
    }
    
    public static int? MapContactPracticeStatusToApplicationPracticeStatus(int? contactPracticeStatus, DateTime? firstYearInPractice, int? locum)
    {
        if (contactPracticeStatus == null)
        {
            return null;
        }

        switch (contactPracticeStatus.Value)
        { 
            case CrmConstants.Contact.PracticeStatusKey.InPractice:
                var practiceStatus = CrmConstants.Application.PracticeStatusKey.PracticingPhysician;
                    
                if (firstYearInPractice?.Year == DateTime.UtcNow.Year)
                {
                    practiceStatus = CrmConstants.Application.PracticeStatusKey.FirstYearInPractice;
                }

                if (locum == CrmConstants.Contact.LocumKey.Yes)
                {
                    practiceStatus = CrmConstants.Application.PracticeStatusKey.Locum;
                }

                return practiceStatus;
            case CrmConstants.Contact.PracticeStatusKey.MaternityPaternityLeave:
                return CrmConstants.Application.PracticeStatusKey.MaternityLeave;
            case CrmConstants.Contact.PracticeStatusKey.NonPracticingAcademicPhysician:
                return CrmConstants.Application.PracticeStatusKey.NonPracticingAcademicPhysician;
            case CrmConstants.Contact.PracticeStatusKey.PartTime:
                return CrmConstants.Application.PracticeStatusKey.PartTime;
            case CrmConstants.Contact.PracticeStatusKey.Retired:
                return CrmConstants.Application.PracticeStatusKey.Retired;
            case CrmConstants.Contact.PracticeStatusKey.FirstYearInPractice:
                return CrmConstants.Application.PracticeStatusKey.FirstYearInPractice;
            case CrmConstants.Contact.PracticeStatusKey.Removed:
                return null;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    public static int? MapApplicationPracticeStatusToContactPracticeStatus(string? applicationPracticeStatus, string? yearEnrolledMedSchool, string? expectedYearOfCompletion, string? retirementDate)
    {
        if (string.IsNullOrEmpty(applicationPracticeStatus))
        {
            //contact is retired
           if (!string.IsNullOrEmpty(retirementDate))
           {
               return CrmConstants.Contact.PracticeStatusKey.Retired;
           }

           if (!string.IsNullOrEmpty(expectedYearOfCompletion))
           {
               return CrmConstants.Contact.PracticeStatusKey.InPractice;
           }
           
           if (!string.IsNullOrEmpty(yearEnrolledMedSchool))
           {
               return CrmConstants.Contact.PracticeStatusKey.InPractice;
           }
            
           return null;
        }

        if (!int.TryParse(applicationPracticeStatus, out var practiceStatus))
        {
            return null;
        }
        
        switch (practiceStatus)
        {
            case CrmConstants.Application.PracticeStatusKey.PracticingPhysician:
            case CrmConstants.Application.PracticeStatusKey.Locum:
                return CrmConstants.Contact.PracticeStatusKey.InPractice;
            case CrmConstants.Application.PracticeStatusKey.MaternityLeave:
                return CrmConstants.Contact.PracticeStatusKey.MaternityPaternityLeave;
            case CrmConstants.Application.PracticeStatusKey.NonPracticingAcademicPhysician:
                return CrmConstants.Contact.PracticeStatusKey.NonPracticingAcademicPhysician;
            case CrmConstants.Application.PracticeStatusKey.PartTime:
                return CrmConstants.Contact.PracticeStatusKey.PartTime;
            case CrmConstants.Application.PracticeStatusKey.Retired:
                return CrmConstants.Contact.PracticeStatusKey.Retired;
            case CrmConstants.Application.PracticeStatusKey.FirstYearInPractice:
                return CrmConstants.Contact.PracticeStatusKey.FirstYearInPractice;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}