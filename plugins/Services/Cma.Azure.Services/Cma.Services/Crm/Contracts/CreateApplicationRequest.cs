namespace Cma.Services.Crm.Contracts;

public record CreateApplicationRequest(Guid CmahId, Guid PtmaId, bool IsStudentApplication=false, string MembershipYear="", int ApplicationType=0, int GraduationYear=0, int YearEnrolledInMedicalSchool=0, string MedicalSchool="");