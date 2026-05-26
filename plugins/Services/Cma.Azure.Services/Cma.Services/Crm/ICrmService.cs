using Cma.Services.Crm.Contracts;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

namespace Cma.Services.Crm;

public interface ICrmService
{
    Task<FindContactResponse> FindContactByPreferredEmailAddress(FindContactByPreferredEmailAddressRequest request);
    Task<GetContactTypesResponse> GetContactTypes(GetContactTypesRequest request);
    Task<CreateContactResponse> CreateContact(CreateContactRequest request);
    Task DeleteContact(DeleteContactRequest request);
    Task SetContactProperties(SetEntityPropertiesRequest request);
    Task<GetEntityPropertyResponse<T>> GetContactProperty<T>(GetEntityPropertyRequest request);
    Task SendContactEmailDirectlyFromTemplate(SendContactEmailDirectlyFromTemplateRequest request);
    Task AutoPostToTimeline(AutoPostToTimelineRequest request);
    Task SetApplicationProperties(SetEntityPropertiesRequest request);
    Task PostNoteToTimeline(PostNoteToTimelineRequest request);

    Task<GetEntityPropertyResponse<T>> GetApplicationProperty<T>(GetEntityPropertyRequest request);
    Task<FindContactResponse> FindContactByCmahId(FindContactByCmahIdRequest request);
    Task<CreateApplicationResponse> CreateApplication(CreateApplicationRequest request);
    Task DeleteApplication(DeleteApplicationRequest request);
    Task SendContactEmailFromTemplate(SendContactEmailFromTemplateRequest request);
    Task<GetEntityPropertiesResponse> GetContactProperties(GetEntityPropertiesRequest request);
    Task<GetEntityPropertiesResponse> GetContactTypeProperties(GetEntityPropertiesRequest request);
    Task<GetContactPointConsentsResponse> GetContactPointConsents(GetContactPointConsentsRequest request);
    Task UpdateContactPointConsent(UpdateContactPointConsentRequest request);
    Task<GetAllTopicsResponse> GetAllTopics();
    Task<FindContactResponse> FindContactByTrackingContextId(FindContactByTrackingContextIdRequest request);
    Task<FindContactResponse> FindContactByCustomerInsightsTrackingId(FindContactByCustomerInsightsTrackingIdRequest request);
    Task<FindContactResponse> FindContactBy(FindContactByRequest request);
    Task SetAddressProperties(SetEntityPropertiesRequest request);
    Task<FindEntityIdResponse> FindEntityIdBy(FindEntityIdByRequest request);
    Task<Entity?> FindEntityBy(FindEntityIdByRequest request);
    Task<CreateEntityResponse> CreateEntity(CreateEntityRequest request);
    Task<GetEntityModelResponse<T>> GetEntityModel<T>(GetEntityModelRequest request) where T : new();
    Task SetEntityModel<T>(SetEntityModelRequest<T> request) where T : new();
    Task SetOptProperties(SetEntityPropertiesRequest request);
    Task<Guid> CreateAddress(Entity entity);
    Task<GetCmaMembershipDetailResponse?> GetMembershipDetailsByParentContactId(GetApplicationsByParentContactIdRequests request);
    Task SetEntityProperties(SetEntityPropertiesRequest request);
    Task SetEventProperties(SetEntityPropertiesRequest request);
}