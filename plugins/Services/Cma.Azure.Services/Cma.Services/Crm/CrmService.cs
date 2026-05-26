using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using Cma.Common.Exceptions;
using Cma.Common.Extensions;
using Cma.Services.Crm.Contracts;
using Cma.Services.Crm.Models;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Renci.SshNet.Sftp;

namespace Cma.Services.Crm;

public partial class CrmService : ICrmService
{
    private readonly CrmServiceConfiguration _configuration;
    private readonly ILogger<CrmService> _logger;
    private readonly IOrganizationServiceAsync _serviceClient;

    //TODO: More logging
    public CrmService(IOrganizationServiceAsync serviceClient, ILogger<CrmService> logger,
        CrmServiceConfiguration configuration)
    {
        _serviceClient = serviceClient;
        _logger = logger;
        _configuration = configuration;
    }
    
     public async Task<GetAllTopicsResponse> GetAllTopics()
    {
        var purposeId = _configuration.RealTimeMarketingPurposeId;
        
        var query = new QueryExpression(CrmConstants.Topic.EntityName)
        {
            ColumnSet = new(
                CrmConstants.Topic.Name,
                CrmConstants.Topic.FrenchName,
                CrmConstants.Topic.Code,
                CrmConstants.Topic.EnglishDescription,
                CrmConstants.Topic.FrenchDescription,
                CrmConstants.Topic.TopicCategoryId,
                CrmConstants.Topic.DisplayOrder
            )
        };

        const string categoryAlias = "category";
        
        var categoryEntity = new LinkEntity(
            CrmConstants.Topic.EntityName,
            CrmConstants.TopicCategory.EntityName,
            CrmConstants.Topic.TopicCategoryId,
            CrmConstants.TopicCategory.Id,
            JoinOperator.LeftOuter)
        {
            Columns = new(CrmConstants.TopicCategory.Name, CrmConstants.TopicCategory.FrenchName),
            EntityAlias = categoryAlias
        };
        
        query.LinkEntities.Add(categoryEntity);
        query.Criteria.AddCondition(CrmConstants.Topic.PurposeId, ConditionOperator.Equal, purposeId);
        query.Criteria.AddCondition(CrmConstants.Topic.PublishedToComplianceCenter, ConditionOperator.Equal, true);
        query.Criteria.AddCondition(CrmConstants.Topic.Code, ConditionOperator.NotNull);
        query.Criteria.AddCondition(CrmConstants.Common.Status, ConditionOperator.Equal, CrmConstants.Common.StatusKey.Active);
        
        var topicsResponse = await _serviceClient.RetrieveMultipleAsync(query);
        
        var topics = topicsResponse.Entities.Select(x => new Topic(
                x.Id,
                new(
                    x.GetAttributeValue<string>(CrmConstants.Topic.Name),
                    x.GetAttributeValue<string>(CrmConstants.Topic.FrenchName)
                ),
                x.GetAttributeValue<string>(CrmConstants.Topic.Code),
                new(
                    x.GetAttributeValue<string>(CrmConstants.Topic.EnglishDescription),
                    x.GetAttributeValue<string>(CrmConstants.Topic.FrenchDescription)
                ),
                x.GetAttributeValue<EntityReference>(CrmConstants.Topic.TopicCategoryId)?.Id ?? Guid.Empty,
                x.GetAttributeValue<OptionSetValue>(CrmConstants.Topic.DisplayOrder)?.Value ?? 0
                )
            )
            .ToList();
        
        var categories = topicsResponse.Entities.Select(x => new Category(
            x.GetAttributeValue<EntityReference>(CrmConstants.Topic.TopicCategoryId)?.Id ?? Guid.Empty,
            new(
                x.GetAttributeValue<AliasedValue>($"{categoryAlias}.{CrmConstants.TopicCategory.Name}")?.Value as string ?? "",
                x.GetAttributeValue<AliasedValue>($"{categoryAlias}.{CrmConstants.TopicCategory.FrenchName}")?.Value as string ?? ""
            ))
            )
            .DistinctBy(x => x.CategoryId)
            .ToList();

        var response = new GetAllTopicsResponse(topics, categories);
        
        return response;
    }


    // GT: I think this is what we want to change to get all the topics and not the consent points and return the topics and their Guids.
    public async Task<GetContactPointConsentsResponse> GetContactPointConsents(GetContactPointConsentsRequest request)
    {
        _logger.LogInformationWithMetadata(request);
        new GetContactPointConsentsRequestValidator().ValidateAndThrowBadRequest(request);
        
        await CreateContactPointConsentsIfRequired(request.EmailAddress);
        
        var query = new QueryExpression(CrmConstants.ContactPointConsent.EntityName)
        {
            ColumnSet = new(
                CrmConstants.ContactPointConsent.PurposeId,
                CrmConstants.ContactPointConsent.TopicId,
                CrmConstants.ContactPointConsent.ConsentStatus
            )
        };
        
        query.Criteria.AddCondition(CrmConstants.ContactPointConsent.EmailAddress, ConditionOperator.Equal, request.EmailAddress);
        query.Criteria.AddCondition(CrmConstants.Common.Status, ConditionOperator.Equal, CrmConstants.Common.StatusKey.Active);
        query.Criteria.AddCondition(CrmConstants.ContactPointConsent.PurposeId, ConditionOperator.Equal, _configuration.RealTimeMarketingPurposeId);

        var contactPointConsents = await _serviceClient.RetrieveMultipleAsync(query);

        var consents = contactPointConsents.Entities
            .Select(x => new ContactPointConsent(
                x.Id, 
                x.GetAttributeValue<EntityReference>(CrmConstants.ContactPointConsent.TopicId)?.Id ?? Guid.Empty, 
                x.GetAttributeValue<OptionSetValue>(CrmConstants.ContactPointConsent.ConsentStatus)?.Value ??
                CrmConstants.ContactPointConsent.ConsentStatusKey.NotSet))
            .ToList();

        var response = new GetContactPointConsentsResponse(consents);
        
        _logger.LogInformationWithMetadata(response);
        
        return response;
    }
    
    private async Task CreateContactPointConsentsIfRequired(string emailAddress)
    {
        var purposeId = _configuration.RealTimeMarketingPurposeId;
        
        // Get all topics
        var topicsQuery = new QueryExpression(CrmConstants.Topic.EntityName)
        {
            ColumnSet = new(
                CrmConstants.Topic.Name,
                CrmConstants.Topic.Code,
                CrmConstants.Topic.PurposeId,
                CrmConstants.Common.Status
            )
        };
        
        topicsQuery.Criteria.AddCondition(CrmConstants.Common.Status, ConditionOperator.Equal, CrmConstants.Common.StatusKey.Active);
        topicsQuery.Criteria.AddCondition(CrmConstants.Topic.Code, ConditionOperator.NotNull);
        topicsQuery.Criteria.AddCondition(CrmConstants.Topic.PurposeId, ConditionOperator.Equal, purposeId);
        
        var topics = await _serviceClient.RetrieveMultipleAsync(topicsQuery);
        
        var topicIds = topics.Entities.Select(x => x.Id).ToList();
        
        // Get all contact point consents
        var contactPointConsentQuery = new QueryExpression(CrmConstants.ContactPointConsent.EntityName)
        {
            ColumnSet = new(
                CrmConstants.ContactPointConsent.PurposeId,
                CrmConstants.ContactPointConsent.TopicId,
                CrmConstants.Common.Status,
                CrmConstants.ContactPointConsent.ConsentStatus
            )
        };
        
        contactPointConsentQuery.Criteria.AddCondition(CrmConstants.ContactPointConsent.EmailAddress, ConditionOperator.Equal, emailAddress);
        contactPointConsentQuery.Criteria.AddCondition(CrmConstants.ContactPointConsent.PurposeId, ConditionOperator.Equal, purposeId);
        contactPointConsentQuery.Criteria.AddCondition(CrmConstants.ContactPointConsent.ConsentType, ConditionOperator.Equal, CrmConstants.ContactPointConsent.ConsentTypeKey.Topic);
        
        var contactPointConsents = await _serviceClient.RetrieveMultipleAsync(contactPointConsentQuery);

        // Update inactive consents for topics that are still active
        var inactiveContactPointConsentIds = contactPointConsents
            .Entities
            .Where(e => e.GetAttributeValue<OptionSetValue>(CrmConstants.Common.Status).Value == CrmConstants.Common.StatusKey.Inactive)
            .Select(e => e.Id)
            .ToList();

        foreach (var consentId in inactiveContactPointConsentIds)
        {
            var updatedEntity = new Entity(CrmConstants.ContactPointConsent.EntityName, consentId)
            {
                [CrmConstants.Common.Status] = new OptionSetValue(CrmConstants.Common.StatusKey.Active),
                [CrmConstants.ContactPointConsent.ConsentStatus] = new OptionSetValue(CrmConstants.ContactPointConsent.ConsentStatusKey.NotSet)
            };

            await _serviceClient.UpdateAsync(updatedEntity);
        }

        // Get the not inactive contact point consents that are missing from the active topic ids list
        var activeContactPointConsentTopicIds = contactPointConsents
            .Entities
            .Where(e => e.GetAttributeValue<OptionSetValue>(CrmConstants.Common.Status).Value != CrmConstants.Common.StatusKey.Inactive)
            .Select(e => e.GetAttributeValue<EntityReference>(CrmConstants.ContactPointConsent.TopicId).Id)
            .ToList();

        var missingContactPointConsentTopicIds = topicIds.Except(activeContactPointConsentTopicIds).ToList();

        // Create the missing consents 
        foreach (var topicId in missingContactPointConsentTopicIds)
        {
            var newEntity = new Entity(CrmConstants.ContactPointConsent.EntityName)
            {
                [CrmConstants.ContactPointConsent.TopicId] = new EntityReference(CrmConstants.Topic.EntityName, topicId),
                [CrmConstants.ContactPointConsent.PurposeId] = new EntityReference(CrmConstants.Purpose.EntityName, purposeId),
                [CrmConstants.ContactPointConsent.EmailAddress] = emailAddress,
                [CrmConstants.ContactPointConsent.ConsentStatus] = new OptionSetValue(CrmConstants.ContactPointConsent.ConsentStatusKey.NotSet),
                [CrmConstants.ContactPointConsent.Source] = new OptionSetValue(CrmConstants.ContactPointConsent.SourceKey.Internal),
                [CrmConstants.ContactPointConsent.Reason] = new OptionSetValue(CrmConstants.ContactPointConsent.ReasonKey.NoReasons),
                [CrmConstants.ContactPointConsent.Channel] = new OptionSetValue(CrmConstants.ContactPointConsent.ChannelKey.Email),
                [CrmConstants.ContactPointConsent.ConsentType] = new OptionSetValue(CrmConstants.ContactPointConsent.ConsentTypeKey.Topic)
            };
            await _serviceClient.CreateAsync(newEntity);
        }
    }

    /// <summary>
    /// Updates an individual Consent Point.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task UpdateContactPointConsent(UpdateContactPointConsentRequest request)
    {
        _logger.LogInformationWithMetadata(request);

        Entity consentEntity = _serviceClient.Retrieve(CrmConstants.ContactPointConsent.EntityName, request.ContactPointConsentId, new ColumnSet(true));
        OptionSetValue currentConsentStatus = consentEntity[CrmConstants.ContactPointConsent.ConsentStatus] as OptionSetValue;

        //System.Diagnostics.Debug.WriteLine("DATA: " + currentConsentStatus.Value + "=====" + request.ConsentStatusKey);

        if (currentConsentStatus.Value != request.ConsentStatusKey)
        {
            new UpdateContactPointConsentRequestValidator().ValidateAndThrowBadRequest(request);

            var updatedEntity = new Entity(CrmConstants.ContactPointConsent.EntityName, request.ContactPointConsentId)
            {
                [CrmConstants.ContactPointConsent.ConsentStatus] = new OptionSetValue(request.ConsentStatusKey)
            };

            await _serviceClient.UpdateAsync(updatedEntity);
            _logger.LogInformation("Contact point consent updated");
        }
        else
        {
            _logger.LogInformation("Not updating Contact Point Consent because no changes were detected.");
        }
    }

    
    public async Task SetContactProperties(SetEntityPropertiesRequest request)
    {
        await SetEntityProperties(request with { EntityName = CrmConstants.Contact.EntityName });
    }
    
    public async Task SetOptProperties(SetEntityPropertiesRequest request)
    {
        await SetEntityProperties(request with { EntityName = CrmConstants.Opt.EntityName });
    }
    
    public async Task SetAddressProperties(SetEntityPropertiesRequest request)
    {
        await SetEntityProperties(request with { EntityName = CrmConstants.Address.EntityName });
    }

    public async Task SetApplicationProperties(SetEntityPropertiesRequest request)
    {
        await SetEntityProperties(request with { EntityName = CrmConstants.Application.EntityName });
    }

    public async Task SetEventProperties(SetEntityPropertiesRequest request)
    {
        await SetEntityProperties(request with { EntityName = CrmConstants.Event.EntityName });
    }

    public async Task<CreateEntityResponse> CreateEntity(CreateEntityRequest request)
    {
        _logger.LogInformationWithMetadata(request);
        
        var entityId = await _serviceClient.CreateAsync(new(request.EntityName));

        var response = new CreateEntityResponse(entityId);
        _logger.LogInformationWithMetadata(response);
        
        return response;
    }
    
    public async Task SetEntityProperties(SetEntityPropertiesRequest request)
    {
        _logger.LogInformationWithMetadata(request);
        new SetEntityPropertiesRequestValidator().ValidateAndThrowBadRequest(request);

        var entity = new Entity(request.EntityName)
        {
            Id = request.EntityId
        };

        foreach (var property in request.Properties)
            if (property.IsOptionSet)
            {
                if (property.Value == null)
                {
                    entity[property.Key] = null;
                }
                else
                {
                    entity[property.Key] = new OptionSetValue((int)property.Value);
                }
            }
            else if (property.IsEntityReference)
            {
                if (property.Value == null)
                {
                    entity[property.Key] = null;
                }
                else if (property.Value.GetType() != typeof(Guid))
                {
                    entity[property.Key] = null;
                }
                else if ((Guid)property.Value == Guid.Empty)
                {
                    entity[property.Key] = null;
                }
                else
                {
                    entity[property.Key] = new EntityReference(property.ReferencedEntityName, (Guid)property.Value);
                }
            }
            else
            {
                entity[property.Key] = property.Value;
            }

        await _serviceClient.UpdateAsync(entity);
    }
    
    public async Task<GetEntityPropertyResponse<T>> GetContactProperty<T>(GetEntityPropertyRequest request)
    {
        _logger.LogInformationWithMetadata(request);

        var entity = await _serviceClient.RetrieveAsync(CrmConstants.Contact.EntityName, request.EntityId,
            new(request.PropertyKey));

        var response = new GetEntityPropertyResponse<T>(entity.GetAttributeValue<T>(request.PropertyKey));
        _logger.LogInformationWithMetadata(response);

        return response;
    }

    public async Task<GetEntityPropertiesResponse> GetContactProperties(GetEntityPropertiesRequest request)
    {
        return await GetEntityProperties(new(request.EntityId, request.PropertyKeys)
            { EntityName = CrmConstants.Contact.EntityName });
    }

    public async Task<GetEntityPropertiesResponse> GetContactTypeProperties(GetEntityPropertiesRequest request)
    {
        return await GetEntityProperties(new(request.EntityId, request.PropertyKeys)
            { EntityName = CrmConstants.ContactType.EntityName });
    }

    private async Task<GetEntityPropertiesResponse> GetEntityProperties(GetEntityPropertiesRequest request)
    {
        _logger.LogInformationWithMetadata(request);

        var columnSet = new ColumnSet();

        foreach (var key in request.PropertyKeys)
        {
            columnSet.Columns.Add(key);
        }

        var entity = await _serviceClient.RetrieveAsync(request.EntityName, request.EntityId, columnSet);

        var dictionary = new Dictionary<string, object?>();
        foreach (var attribute in entity.Attributes)
        {
            var value = attribute.Value;

            if (attribute.Value is OptionSetValue)
            {
                value = entity.GetAttributeValue<OptionSetValue>(attribute.Key).Value;
            }
            else if (attribute.Value is EntityReference)
            {
                value = entity.GetAttributeValue<EntityReference>(attribute.Key).Id;
            }

            dictionary.Add(attribute.Key, value);
        }

        foreach (var key in request.PropertyKeys)
        {
            dictionary.TryAdd(key, null);
        }

        var response = new GetEntityPropertiesResponse(dictionary);

        _logger.LogInformationWithMetadata(response);

        return response;
    }

    public async Task<CreateContactResponse> CreateContact(CreateContactRequest request)
    {
        _logger.LogInformationWithMetadata(request);

        var contactTypesResponse = await GetContactTypes(new([
            CrmConstants.ContactType.ContactTypeKey.Other,
            CrmConstants.ContactType.ContactTypeKey.MemberOfPublic,
            CrmConstants.ContactType.ContactTypeKey.SelfIdentifiedPhysician
        ]));

        var crmContactTypeOther = contactTypesResponse
            .ContactTypes
            .First(ct => ct.Code.Equals(CrmConstants.ContactType.ContactTypeKey.Other));
        var crmContactTypeMemberOfPublic = contactTypesResponse
            .ContactTypes
            .First(ct => ct.Code.Equals(request.IsSelfIdentifiedPhysician ? CrmConstants.ContactType.ContactTypeKey.SelfIdentifiedPhysician : CrmConstants.ContactType.ContactTypeKey.MemberOfPublic));


        var crmContact = new Entity(CrmConstants.Contact.EntityName)
        {
            [CrmConstants.Contact.PreferredEmailAddress] = request.PreferredEmailAddress,
            [CrmConstants.Contact.FirstName] = request.FirstName,
            [CrmConstants.Contact.LastName] = request.LastName,
            [CrmConstants.Contact.ContactTypeId] = GetContactTypeReference(crmContactTypeOther.Id),
            [CrmConstants.Contact.ContactSubTypeId] = GetContactTypeReference(crmContactTypeMemberOfPublic.Id),
            [CrmConstants.Contact.Username] = request.PreferredEmailAddress,
            [CrmConstants.Contact.CmahId] = Guid.NewGuid().ToString(),
            [CrmConstants.Contact.Language] = new OptionSetValue(CrmMapper.Language.Map(request.LanguageCode)),
            [CrmConstants.Contact.ExcludeSalutation] = true,
            [CrmConstants.Contact.CreationReason] = new OptionSetValue(request.CreationReason)
        };

        var contactId = await _serviceClient.CreateAsync(crmContact);

        var getContactResponse = await GetContact(new(contactId));

        var response = new CreateContactResponse(getContactResponse.Contact!);
        _logger.LogInformationWithMetadata(response);

        return response;
    }

    public async Task DeleteContact(DeleteContactRequest request)
    {
        _logger.LogInformationWithMetadata(request);

        await _serviceClient.DeleteAsync(CrmConstants.Contact.EntityName, request.ContactId);
    }

    public async Task<GetContactTypesResponse> GetContactTypes(GetContactTypesRequest request)
    {
        _logger.LogInformationWithMetadata(request);

        var query = new QueryExpression(CrmConstants.ContactType.EntityName)
        {
            ColumnSet = new(),
            Criteria = new(LogicalOperator.Or),
            Distinct = true
        };

        // ReSharper disable once CoVariantArrayConversion
        query.Criteria.AddCondition(CrmConstants.ContactType.Code, ConditionOperator.In, request.ContactTypeCodes);

        query.ColumnSet.Columns.Add(CrmConstants.ContactType.Id);
        query.ColumnSet.Columns.Add(CrmConstants.ContactType.Code);
        query.ColumnSet.Columns.Add(CrmConstants.ContactType.Name);

        var response = await _serviceClient.RetrieveMultipleAsync(query);
        var crmContactTypes = response.Entities;

        var contactTypes = crmContactTypes?.Select(crmContactType => new ContactType(
            crmContactType.Id,
            crmContactType.GetAttributeValue<string>(CrmConstants.ContactType.Code),
            crmContactType.GetAttributeValue<string>(CrmConstants.ContactType.Name)
        )).ToList() ?? [];

        // Check if all the codes requested were returned. If not, throw.
        if (request.ContactTypeCodes.Any(contactTypeCode =>
                contactTypes.All(contactType => contactType.Code != contactTypeCode)))
        {
            throw new ConflictException(ErrorCode.NotFound.ToSnakeCase(), "Cannot find contact type", new
            {
                Code = CrmConstants.ContactType.ContactTypeKey.Other
            });
        }

        var contactTypesResponse = new GetContactTypesResponse(contactTypes);
        _logger.LogInformationWithMetadata(contactTypesResponse);

        return contactTypesResponse;
    }
    
    public async Task<FindEntityIdResponse> FindEntityIdBy(FindEntityIdByRequest request)
    {
        _logger.LogInformationWithMetadata(request);
        // TODO
        // new FindEntityByRequestValidator().ValidateAndThrowBadRequest(request);

        var response = new FindEntityIdResponse(null);

        var query = new QueryExpression(request.EntityName)
        {
            ColumnSet = new(false),
            Distinct = true
        };
        var filter = query.Criteria.AddFilter(LogicalOperator.And);
        
        foreach (var keyValue in request.FindByPropertyKeyValues)
        {
            filter.AddCondition(keyValue.Key, ConditionOperator.Equal, keyValue.Value);
        }
        
        var retrieveResponse = await _serviceClient.RetrieveMultipleAsync(query);
        _logger.LogInformationWithMetadata(retrieveResponse);
            
        if (retrieveResponse.Entities.Count >= 1)
        {
            response = new(retrieveResponse.Entities.First().Id);    
        }
        
        _logger.LogInformationWithMetadata(response);
        
        return response;
    }
    
    public async Task<Entity?> FindEntityBy(FindEntityIdByRequest request)
    {
        _logger.LogInformationWithMetadata(request);

        var response = new EntityReference();

        var query = new QueryExpression(request.EntityName)
        {
            ColumnSet = new(request.ReturnAllProperties),
            Distinct = true
        };
        var filter = query.Criteria.AddFilter(LogicalOperator.And);
        
        foreach (var keyValue in request.FindByPropertyKeyValues)
        {
            filter.AddCondition(keyValue.Key, ConditionOperator.Equal, keyValue.Value);
        }
        
        var retrieveResponse = await _serviceClient.RetrieveMultipleAsync(query);
        _logger.LogInformationWithMetadata(retrieveResponse);
            
        if (retrieveResponse.Entities.Count <= 0)
        {
           return default;    
        }
        
        _logger.LogInformationWithMetadata(response);
        
        return retrieveResponse.Entities.First();
    }

    public async Task<FindContactResponse> FindContactBy(FindContactByRequest request)
    {
        _logger.LogInformationWithMetadata(request);
        new FindContactByRequestValidator().ValidateAndThrowBadRequest(request);

        var response = new FindContactResponse(null);
        foreach (var keyValue in request.KeyValues)
        {
            var conditionExpression = new ConditionExpression(
                keyValue.Key,
                ConditionOperator.Equal,
                keyValue.Value);

            response = await FindContact(conditionExpression);
            _logger.LogInformationWithMetadata(response);

            // Stop searching if a contact was found
            if (response.ContactFound)
            {
                break;
            }
        }

        return response;
    }
    
    public async Task<FindContactResponse> FindContactByPreferredEmailAddress(
        FindContactByPreferredEmailAddressRequest request)
    {
        _logger.LogInformationWithMetadata(request);
        new FindContactByPreferredEmailAddressRequestValidator().ValidateAndThrowBadRequest(request);

        var conditionExpression = new ConditionExpression(
            CrmConstants.Contact.PreferredEmailAddress,
            ConditionOperator.Equal,
            request.PreferredEmailAddress);

        var response = await FindContact(conditionExpression);
        _logger.LogInformationWithMetadata(response);

        return response;
    }

    public async Task<FindContactResponse> FindContactByCmahId(FindContactByCmahIdRequest request)
    {
        _logger.LogInformationWithMetadata(request);
        new FindContactByCmahIdRequestValidator().ValidateAndThrowBadRequest(request);

        var conditionExpression = new ConditionExpression(
            CrmConstants.Contact.CmahId,
            ConditionOperator.Equal,
            request.CmahId.ToString());

        var response = await FindContact(conditionExpression);
        _logger.LogInformationWithMetadata(response);

        return response;
    }
    
    public async Task<FindContactResponse> FindContactByCustomerInsightsTrackingId(FindContactByCustomerInsightsTrackingIdRequest request)
    {
        _logger.LogInformationWithMetadata(request);
        new FindContactByCustomerInsightsTrackingIdRequestValidator().ValidateAndThrowBadRequest(request);

        var conditionExpression = new ConditionExpression(
            CrmConstants.Contact.CustomerInsightsTrackingId,
            ConditionOperator.Equal,
            request.TrackingId.ToString());

        var response = await FindContact(conditionExpression);
        _logger.LogInformationWithMetadata(response);

        return response;
    }
    
    public async Task<FindContactResponse> FindContactByTrackingContextId(FindContactByTrackingContextIdRequest request)
    {
        _logger.LogInformationWithMetadata(request);
        new FindContactByTrackingContextIdRequestValidator().ValidateAndThrowBadRequest(request);

        var trackingContextQuery = new QueryExpression(CrmConstants.TrackingContext.EntityName)
        {
            ColumnSet = new(CrmConstants.TrackingContext.EntityId),
            Distinct = true
        };
        
        trackingContextQuery.Criteria.AddCondition(new(CrmConstants.TrackingContext.TrackingContextId, ConditionOperator.Equal, request.TrackingContextId));
        trackingContextQuery.Criteria.AddCondition(new(CrmConstants.TrackingContext.EntityType, ConditionOperator.Equal, CrmConstants.Contact.EntityName));

        var trackingContextResponse = await _serviceClient.RetrieveMultipleAsync(trackingContextQuery);

        var trackingContextEntity = trackingContextResponse.Entities.FirstOrDefault();

        if (trackingContextEntity == null)
        {
            throw new NotFoundException(ErrorCode.NotFound.ToSnakeCase(), "Tracking context id not found");
        }

        var contactId = trackingContextEntity.GetAttributeValue<string>(CrmConstants.TrackingContext.EntityId);
        
        var conditionExpression = new ConditionExpression(
            CrmConstants.Contact.Id,
            ConditionOperator.Equal,
            contactId);

        var response = await FindContact(conditionExpression);
        _logger.LogInformationWithMetadata(response);

        return response;
    }

    public Task SendContactEmailDirectlyFromTemplate(SendContactEmailDirectlyFromTemplateRequest request)
    {
        _logger.LogInformationWithMetadata(request);
        new SendContactEmailDirectlyFromTemplateRequestValidator().ValidateAndThrowBadRequest(request);

        var toParty = new Entity(CrmConstants.ActivityParty.EntityName)
        {
            [CrmConstants.ActivityParty.AddressUsed] = request.DirectEmailAddress
        };

        return SendContactEmailFromTemplate(request.TemplateName, request.ContactId, request.ContactLanguage, toParty,
            request.Parameters);
    }

    public async Task PostNoteToTimeline(PostNoteToTimelineRequest request)
    {
        _logger.LogInformationWithMetadata(request);

        var note = new Entity(CrmConstants.Annotation.EntityName)
        {
            [CrmConstants.Annotation.ObjectId] = GetContactReference(request.ContactId),
            [CrmConstants.Annotation.Subject] = request.Subject,
            [CrmConstants.Annotation.NoteText] = request.Message
        };

        await _serviceClient.CreateAsync(note);
    }

    public async Task AutoPostToTimeline(AutoPostToTimelineRequest request)
    {
        _logger.LogInformationWithMetadata(request);
        new AutoPostToTimelineRequestValidator().ValidateAndThrowBadRequest(request);

        var post = new Entity(CrmConstants.Post.EntityName)
        {
            [CrmConstants.Post.Source] = new OptionSetValue(CrmConstants.Post.SourceKey.AutoPost),
            [CrmConstants.Post.Type] = new OptionSetValue(CrmConstants.Post.TypeKey.StatusUpdate),
            [CrmConstants.Post.Text] = request.Message,
            [CrmConstants.Post.RegardingObjectId] = GetContactReference(request.ContactId)
        };

        await _serviceClient.CreateAsync(post);
    }

    public async Task<GetEntityPropertyResponse<T>> GetApplicationProperty<T>(GetEntityPropertyRequest request)
    {
        _logger.LogInformationWithMetadata(request);

        var entity = await _serviceClient.RetrieveAsync(CrmConstants.Application.EntityName, request.EntityId,
            new(request.PropertyKey));

        var response = new GetEntityPropertyResponse<T>(entity.GetAttributeValue<T>(request.PropertyKey));
        _logger.LogInformationWithMetadata(response);

        return response;
    }

    public async Task SendContactEmailFromTemplate(string templateName, Guid contactId, int contactLanguage,
        Entity toParty, Dictionary<string, string>? parameters = null)
    {
        if (toParty.LogicalName != CrmConstants.ActivityParty.EntityName)
        {
            throw new BadRequestException(ErrorCode.ArgumentInvalid.ToSnakeCase(), "Entity is not an ActivityParty", new
            {
                toParty.LogicalName
            });
        }

        var email = await GetEmailFromTemplate(templateName, contactId);

        var fromParty = GetFromParty(contactLanguage);

        // Attach to email
        email[CrmConstants.Email.From] = new[] { fromParty };
        email[CrmConstants.Email.To] = new[] { toParty };
        email[CrmConstants.Email.RegardingObjectId] = GetContactReference(contactId);

        // Replace parameters with new values
        parameters ??= new();

        foreach (var parameter in parameters)
        {
            var subject = email[CrmConstants.Email.Subject].ToString();
            var body = email[CrmConstants.Email.Description].ToString();
            var key = "{{" + parameter.Key + "}}";
            var value = parameter.Value;
            var newSubject = subject?.Replace(key, value, StringComparison.InvariantCultureIgnoreCase);
            var newBody = body?.Replace(key, value, StringComparison.InvariantCultureIgnoreCase);

            email[CrmConstants.Email.Subject] = newSubject;
            email[CrmConstants.Email.Description] = newBody;
        }

        var emailId = await _serviceClient.CreateAsync(email);

        // Send the email
        var sendEmailRequest = new SendEmailRequest
        {
            EmailId = emailId,
            TrackingToken = string.Empty,
            IssueSend = true
        };

        await _serviceClient.ExecuteAsync(sendEmailRequest);
    }

    public async Task<CreateApplicationResponse> CreateApplication(CreateApplicationRequest request)
    {
        _logger.LogInformationWithMetadata(request);
        new CreateApplicationRequestValidator().ValidateAndThrowBadRequest(request);

        // Retrieve the contact
        var contactResponse = await FindContactByCmahId(new(request.CmahId));

        if (contactResponse.Contact == null)
        {
            throw new BadRequestException(ErrorCode.NotFound.ToSnakeCase(), "Contact not found", request);
        }

        var membershipDetailResponse = await GetContactProperty<EntityReference>(new(contactResponse.Contact.Id,
            CrmConstants.Contact.MembershipDetailId));

        // Create the application
        var crmApplication = new Entity(CrmConstants.Application.EntityName)
        {
            [CrmConstants.Application.Name] = "new portal application",
            [CrmConstants.Application.ApplicationSource] =
                new OptionSetValue(CrmConstants.Application.ApplicationSourceKey.Portal),
            [CrmConstants.Application.EligibilityType] =
                new OptionSetValue(CrmConstants.Application.EligibilityTypeKey.Direct),
            [CrmConstants.Application.ApplicationType] = new OptionSetValue(
                membershipDetailResponse.PropertyValue == null
                    ? CrmConstants.Application.ApplicationTypeKey.Join
                    : CrmConstants.Application.ApplicationTypeKey.Renew),
            [CrmConstants.Application.PhysicianContact] = GetContactReference(contactResponse.Contact.Id),
            [CrmConstants.Application.PtmaId] = new EntityReference(CrmConstants.Account.EntityName, request.PtmaId),
            [CrmConstants.Application.Country] = new OptionSetValue(
                request.PtmaId == CrmConstants.Account.PtmaIdKey.Abroad
                    ? CrmConstants.CountryKey.Other
                    : CrmConstants.CountryKey.Canada
            )
        };
        if (request.IsStudentApplication)
        {
            crmApplication[CrmConstants.Application.PaymentStatus] = new OptionSetValue(CrmConstants.Application.PaymentStatusKey.NotRequired);
            crmApplication[CrmConstants.Application.ApprovalStatus] = new OptionSetValue(CrmConstants.Application.ApprovalStatusKey.Approved);
            crmApplication[CrmConstants.Application.MembershipYear] = request.MembershipYear;
            crmApplication[CrmConstants.Application.GraduationYear] = new OptionSetValue(request.GraduationYear);
            crmApplication[CrmConstants.Application.UniversityofGraduation] = new EntityReference(CrmConstants.Account.EntityName, Guid.Parse(request.MedicalSchool));
            crmApplication[CrmConstants.Application.YearEnrolledinMedicalSchool] = new OptionSetValue(request.YearEnrolledInMedicalSchool);
            if (request.ApplicationType!=0)
            {
                crmApplication[CrmConstants.Application.ApplicationType] = new OptionSetValue(request.ApplicationType);
            }
        }
        
        _logger.LogInformationWithMetadata("Create Application", crmApplication);

        var applicationId = await _serviceClient.CreateAsync(crmApplication);

        _logger.LogInformationWithMetadata("New Application Id", applicationId);

        var getApplicationResponse = await GetApplication(new GetApplicationRequest(applicationId));

        var response = new CreateApplicationResponse(getApplicationResponse.Application);
        _logger.LogInformationWithMetadata(response);

        return response;
    }

    public async Task DeleteApplication(DeleteApplicationRequest request)
    {
        _logger.LogInformationWithMetadata("Delete application", request);

        await _serviceClient.DeleteAsync(CrmConstants.Application.EntityName, request.ApplicationId);
    }

    public async Task<GetContactResponse> GetContact(GetContactRequest request)
    {
        _logger.LogInformationWithMetadata(request);

        var crmContact =
            await _serviceClient.RetrieveAsync(CrmConstants.Contact.EntityName, request.ContactId,
                DefaultContactColumnSet());

        var contact = MapCrmContactToContact(crmContact);

        var response = new GetContactResponse(contact);
        _logger.LogInformationWithMetadata(response);

        return response;
    }

    public async Task<GetApplicationResponse> GetApplication(GetApplicationRequest request)
    {
        _logger.LogInformationWithMetadata(request);

        var crmApplication = await _serviceClient.RetrieveAsync(CrmConstants.Application.EntityName,
            request.ApplicationId,
            DefaultApplicationColumnSet());

        var application = MapCrmApplicationToApplication(crmApplication);

        var response = new GetApplicationResponse(application);
        _logger.LogInformationWithMetadata(response);

        return response;
    }

    public async Task<GetApplicationWithMembershipPaymentInformationResponse> GetApplication(GetApplicationWithMembershipPaymentInformationRequest request)
    {
        _logger.LogInformationWithMetadata(request);

        // Membership payment information columns
        var columns = DefaultApplicationColumnSet();
        columns.AddColumns([
            CrmConstants.Application.TotalMembershipCost,
            CrmConstants.Application.TotalMembershipDiscount,
            CrmConstants.Application.Subtotal,
            CrmConstants.Application.Hst,
            CrmConstants.Application.HstPercentage,
            CrmConstants.Application.Gst,
            CrmConstants.Application.GstPercentage,
            CrmConstants.Application.Qst,
            CrmConstants.Application.QstPercentage,
            CrmConstants.Application.GrandTotal,
            CrmConstants.Application.ProvinceStateCode,
            CrmConstants.Application.OnlineApplicationId,
            CrmConstants.Application.MembershipExpiryDate,
            CrmConstants.Application.ApplicationNumber,
            CrmConstants.Application.MembershipPriceListId
        ]);

        var crmApplication = await _serviceClient.RetrieveAsync(CrmConstants.Application.EntityName,
            request.ApplicationId,
            columns);
        _logger.LogInformationWithMetadata("Application with membership payment information", crmApplication);

        var application = MapCrmApplicationToApplicationWithMembershipPaymentInformation(crmApplication);

        var response = new GetApplicationWithMembershipPaymentInformationResponse(application);
        _logger.LogInformationWithMetadata(response);

        return response;
    }
    
    public async Task<GetCmaMembershipDetailResponse?> GetMembershipDetailsByParentContactId(GetApplicationsByParentContactIdRequests request)
    {
            _logger.LogInformationWithMetadata(request);
            // Define the QueryExpression for Membership Details entity
            var query = new QueryExpression(CrmConstants.CmaMembershipDetail.EntityName) // Logical name of the Application entity
            {
                ColumnSet = new ColumnSet(
                    CrmConstants.CmaMembershipDetail.MembershipStatus, 
                    CrmConstants.CmaMembershipDetail.ExpiryDate, 
                    CrmConstants.CmaMembershipDetail.MembershipYear,
                    CrmConstants.CmaMembershipDetail.Category
                ), 
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        // Filter by related contact ID and Membership Years
                        new ConditionExpression(CrmConstants.CmaMembershipDetail.Contact, ConditionOperator.Equal, request.ParentContactId),
                        new ConditionExpression(CrmConstants.CmaMembershipDetail.MembershipYear, ConditionOperator.Between, [request.StartYear, request.EndYear])

                    }
                }
            };
            
            // Adding LinkEntity to join Membership (CmaMembershipDetail) and SalesOrder
            var salesOrderLink = new LinkEntity
            {
                LinkFromEntityName = CrmConstants.CmaMembershipDetail.EntityName, // Source entity
                LinkFromAttributeName = CrmConstants.CmaMembershipDetail.OrderId, // Linking field in Membership entity
                LinkToEntityName = CrmConstants.Order.EntityName, // Logical name of SalesOrder entity
                LinkToAttributeName = CrmConstants.Order.Id, // Linking field in SalesOrder entity
                JoinOperator = JoinOperator.Inner, // Use inner join for records with matching relationship
                EntityAlias = CrmConstants.Order.EntityName, // Alias for retrieving SalesOrder data
                Columns = new ColumnSet(
                    CrmConstants.Order.AmountOutstanding,      // Add specific columns for SalesOrder
                    CrmConstants.Order.LineItemDiscountAmount,
                    CrmConstants.Order.AmountPaid,
                    CrmConstants.Order.TotalAmount,
                    CrmConstants.Order.CreatedOn,
                    CrmConstants.Order.SubTotal,
                    CrmConstants.Order.Gst,
                    CrmConstants.Order.Hst,
                    CrmConstants.Order.Qst,
                    CrmConstants.Order.GstPercentage,
                    CrmConstants.Order.HstPercentage,
                    CrmConstants.Order.QstPercentage,
                    CrmConstants.Order.TaxJurisdiction
                )
            };
            
            var accountLink = new LinkEntity
            {
                LinkFromEntityName = CrmConstants.CmaMembershipDetail.EntityName, // Source entity
                LinkFromAttributeName = CrmConstants.CmaMembershipDetail.Ptma, // Linking field in Membership entity
                LinkToEntityName = CrmConstants.Account.EntityName, // Logical name of Account entity
                LinkToAttributeName = CrmConstants.Account.Id, // Linking field in Account entity
                JoinOperator = JoinOperator.Inner, // Use inner join for records with matching relationship
                EntityAlias = CrmConstants.Account.EntityName, // Alias for retrieving Account data
                Columns = new ColumnSet(
                    CrmConstants.Account.Id, // Add specific columns for Account entity
                    CrmConstants.Account.EnglishName,
                    CrmConstants.Account.FrenchName
                )
            };

            // Add the LinkEntities to the query
            query.LinkEntities.Add(salesOrderLink);
            query.LinkEntities.Add(accountLink);

            // Retrieve data
            var membershipDetailsEntityCollection = await  _serviceClient.RetrieveMultipleAsync(query);
            
            return MapMembershipDetailFromEntityCollectionResult(membershipDetailsEntityCollection);
    }
    
    private static GetCmaMembershipDetailResponse? MapMembershipDetailFromEntityCollectionResult(EntityCollection? membershipDetailCollection)
    {
       
        if (membershipDetailCollection is null || membershipDetailCollection.Entities.Count <= 0)
        {
            return new GetCmaMembershipDetailResponse(null);
        }
        List<CmaMembershipDetail> membershipDetails = new List<CmaMembershipDetail>();
        string connectingDot = ".";

        foreach (var membershipDetailItem in membershipDetailCollection.Entities)
        {
            var membershipDetail = new CmaMembershipDetail();
            membershipDetail.MembershipStatus = membershipDetailItem?.GetAttributeValue<OptionSetValue>(CrmConstants.CmaMembershipDetail.MembershipStatus)?.Value;
            membershipDetail.ExpiryDate = membershipDetailItem?.GetAttributeValue<DateTime>(CrmConstants.CmaMembershipDetail.ExpiryDate);
            membershipDetail.MembershipYear = membershipDetailItem?.GetAttributeValue<string>(CrmConstants.CmaMembershipDetail.MembershipYear);
            membershipDetail.Category = membershipDetailItem?.GetAttributeValue<EntityReference>(CrmConstants.CmaMembershipDetail.Category).Id;
            
            var order = new Order();
            order.AmountOutstanding = GetAliasedMoneyValue(membershipDetailItem, string.Concat(CrmConstants.Order.EntityName, connectingDot, CrmConstants.Order.AmountOutstanding)).ToString(CultureInfo.InvariantCulture);
            order.LineItemDiscountAmount = GetAliasedMoneyValue(membershipDetailItem, string.Concat(CrmConstants.Order.EntityName, connectingDot, CrmConstants.Order.LineItemDiscountAmount)).ToString(CultureInfo.InvariantCulture);
            order.AmountPaid = GetAliasedMoneyValue(membershipDetailItem, string.Concat(CrmConstants.Order.EntityName, connectingDot, CrmConstants.Order.AmountPaid)).ToString(CultureInfo.InvariantCulture);
            order.TotalAmount = GetAliasedMoneyValue(membershipDetailItem, string.Concat(CrmConstants.Order.EntityName, connectingDot, CrmConstants.Order.TotalAmount)).ToString(CultureInfo.InvariantCulture);
            order.SubTotal = GetAliasedMoneyValue(membershipDetailItem, string.Concat(CrmConstants.Order.EntityName, connectingDot, CrmConstants.Order.SubTotal)).ToString(CultureInfo.InvariantCulture);
            order.Gst = GetAliasedMoneyValue(membershipDetailItem, string.Concat(CrmConstants.Order.EntityName, connectingDot, CrmConstants.Order.Gst)).ToString(CultureInfo.InvariantCulture);
            order.Hst = GetAliasedMoneyValue(membershipDetailItem, string.Concat(CrmConstants.Order.EntityName, connectingDot, CrmConstants.Order.Hst)).ToString(CultureInfo.InvariantCulture);
            order.Qst = GetAliasedMoneyValue(membershipDetailItem, string.Concat(CrmConstants.Order.EntityName, connectingDot, CrmConstants.Order.Qst)).ToString(CultureInfo.InvariantCulture);
            order.GstPercentage = GetAliasedMoneyValue(membershipDetailItem, string.Concat(CrmConstants.Order.EntityName, connectingDot, CrmConstants.Order.GstPercentage)).ToString(CultureInfo.InvariantCulture);
            order.HstPercentage = GetAliasedMoneyValue(membershipDetailItem, string.Concat(CrmConstants.Order.EntityName, connectingDot, CrmConstants.Order.HstPercentage)).ToString(CultureInfo.InvariantCulture);
            order.QstPercentage = GetAliasedMoneyValue(membershipDetailItem, string.Concat(CrmConstants.Order.EntityName, connectingDot, CrmConstants.Order.QstPercentage)).ToString(CultureInfo.InvariantCulture);
            order.TaxJurisdiction = GetAliasedOptionSetValue(membershipDetailItem, string.Concat(CrmConstants.Order.EntityName, connectingDot, CrmConstants.Order.TaxJurisdiction));
            
            order.CreatedOn = (membershipDetailItem?.GetAttributeValue<AliasedValue>(string.Concat(CrmConstants.Order.EntityName, connectingDot, CrmConstants.Order.CreatedOn))?.Value as DateTime?) ?? DateTime.MinValue;

            var account = new Account();
            account.AccountId = (membershipDetailItem?.GetAttributeValue<AliasedValue>(string.Concat(CrmConstants.Account.EntityName, connectingDot, CrmConstants.Account.Id))?.Value as Guid?) ?? Guid.Empty;
            account.EnglishName = membershipDetailItem?.GetAttributeValue<AliasedValue>(string.Concat(CrmConstants.Account.EntityName,connectingDot,CrmConstants.Account.EnglishName))?.Value as string;
            account.FrenchName = membershipDetailItem?.GetAttributeValue<AliasedValue>(string.Concat(CrmConstants.Account.EntityName,connectingDot,CrmConstants.Account.FrenchName))?.Value as string;
            
            membershipDetail.Order = order;
            membershipDetail.Account = account;
            membershipDetails.Add(membershipDetail);
        }
        
        
        return new GetCmaMembershipDetailResponse(membershipDetails);
    }
    private static decimal GetAliasedMoneyValue(Entity? entity, string alias)
    {
        if (entity == null || string.IsNullOrEmpty(alias)) return 0;

        var aliasedValue = entity.GetAttributeValue<AliasedValue>(alias);
        if (aliasedValue == null) return 0;

        if (aliasedValue.Value is Money moneyValue)
        {
            return moneyValue.Value; // Extract the decimal value from the Money class
        }

        return aliasedValue.Value is decimal decimalValue ? decimalValue : 0;
    }

    private static int GetAliasedOptionSetValue(Entity? entity, string alias)
    {
        if (entity == null || string.IsNullOrEmpty(alias)) return 0;

        var aliasedValue = entity.GetAttributeValue<AliasedValue>(alias);
    
        if (aliasedValue?.Value is OptionSetValue optionSetValue)
        {
            return optionSetValue.Value; // Extract integer value from OptionSetValue
        }

        return 0; // Default value if not found or not an OptionSetValue
    }
    
    private static T GetAliasedValue<T>(Entity? entity, string alias)
    {
        if (entity == null || string.IsNullOrEmpty(alias)) return default!;
    
        var aliasedValue = entity.GetAttributeValue<AliasedValue>(alias);
        return aliasedValue != null && aliasedValue.Value is T value ? value : default!;
    }


    public Task SendContactEmailFromTemplate(SendContactEmailFromTemplateRequest request)
    {
        var toParty = new Entity(CrmConstants.ActivityParty.EntityName)
        {
            [CrmConstants.ActivityParty.PartyId] = GetContactReference(request.ContactId)
        };

        return SendContactEmailFromTemplate(request.TemplateName, request.ContactId, request.ContactLanguage, toParty,
            request.Parameters);
    }

    private static EntityReference GetContactTypeReference(Guid contactTypeId)
    {
        return new(CrmConstants.ContactType.EntityName, contactTypeId);
    }

    private Entity GetFromParty(int contactLanguage)
    {
        EntityReference queueReference = contactLanguage switch
        {
            CrmConstants.Contact.LanguageKey.French => new(CrmConstants.Queue.EntityName,
                _configuration.EmailQueueIdFrench),
            CrmConstants.Contact.LanguageKey.English => new(CrmConstants.Queue.EntityName,
                _configuration.EmailQueueIdEnglish),
            _ => throw new InternalServerErrorException(ErrorCode.ArgumentInvalid.ToSnakeCase(), "Language undefined",
                new { ContactLanguage = contactLanguage })
        };

        // Create From Party
        var fromParty = new Entity(CrmConstants.ActivityParty.EntityName)
        {
            [CrmConstants.ActivityParty.PartyId] = queueReference
        };

        return fromParty;
    }

    private static EntityReference GetContactReference(Guid contactId)
    {
        return new(CrmConstants.Contact.EntityName, contactId);
    }

    private async Task<Entity> GetEmailFromTemplate(string templateName, Guid contactId)
    {
        // Get the template to use
        var query = new QueryExpression(CrmConstants.EmailTemplate.EntityName);

        query.Criteria.AddCondition(CrmConstants.EmailTemplate.Title, ConditionOperator.Equal, templateName);
        query.ColumnSet = new(
            CrmConstants.EmailTemplate.TemplateId,
            CrmConstants.EmailTemplate.Subject,
            CrmConstants.EmailTemplate.Body,
            CrmConstants.EmailTemplate.Title);

        var response = await _serviceClient.RetrieveMultipleAsync(query);
        var templateFound = response.Entities.Any();

        switch (templateFound)
        {
            case false:
                throw new ConflictException(ErrorCode.NotFound.ToSnakeCase(), "Template not found", new
                {
                    TemplateName = templateName
                });
            case true when response.Entities.Count > 1:
                throw new ConflictException(ErrorCode.MultipleRecordsFound.ToSnakeCase(),
                    "More than one entity found with the template name", new
                    {
                        TemplateName = templateName
                    });
        }

        var template = response.Entities.First();

        // Reference the contact to use contact dynamic variables
        var contactReference = GetContactReference(contactId);

        // Instantiate the template
        var instantiateTemplateRequest = new InstantiateTemplateRequest
        {
            TemplateId = template.Id,
            ObjectId = contactReference.Id,
            ObjectType = contactReference.LogicalName
        };

        var organizationResponse = await _serviceClient.ExecuteAsync(instantiateTemplateRequest);

        var instantiateTemplateResponse = (InstantiateTemplateResponse)organizationResponse;

        // Use the instantiated template to create the email record
        var email = instantiateTemplateResponse.EntityCollection.Entities.FirstOrDefault();

        if (email == null)
        {
            throw new InternalServerErrorException(ErrorCode.UnknownError.ToSnakeCase(),
                "Email could not be instantiated",
                new
                {
                    TemplateName = templateName,
                    ContactId = contactId
                });
        }

        return email;
    }

    private static ColumnSet DefaultContactColumnSet()
    {
        var columnSet = new ColumnSet();
        columnSet.Columns.Add(CrmConstants.Contact.Id);
        columnSet.Columns.Add(CrmConstants.Contact.CmahId);
        columnSet.Columns.Add(CrmConstants.Contact.PreferredEmailAddress);
        columnSet.Columns.Add(CrmConstants.Contact.FirstName);
        columnSet.Columns.Add(CrmConstants.Contact.LastName);
        columnSet.Columns.Add(CrmConstants.Contact.Language);

        return columnSet;
    }

    private static ColumnSet DefaultApplicationColumnSet()
    {
        var columnSet = new ColumnSet();
        columnSet.Columns.Add(CrmConstants.Application.Id);
        columnSet.Columns.Add(CrmConstants.Application.ApplicationNumber);

        return columnSet;
    }

    private async Task<FindContactResponse> FindContact(ConditionExpression conditionExpression)
    {
        var query = new QueryExpression(CrmConstants.Contact.EntityName)
        {
            ColumnSet = DefaultContactColumnSet(),
            Criteria = new(),
            Distinct = true
        };
        query.Criteria.AddCondition(conditionExpression);

        var response = await _serviceClient.RetrieveMultipleAsync(query);

        if (response.Entities.Count > 1)
        {
            throw new ConflictException(ErrorCode.MultipleRecordsFound.ToSnakeCase(), "Multiple contacts found",
                new Dictionary<string, object>
                {
                    { conditionExpression.AttributeName, conditionExpression.Values }
                });
        }

        if (!response.Entities.Any())
        {
            _logger.LogWarningWithMetadata("Contact not found", new
            {
                conditionExpression.AttributeName,
                conditionExpression.Values
            });

            return new(null);
        }

        var crmContact = response.Entities.First();
        var contact = MapCrmContactToContact(crmContact);

        return new(contact);
    }

    private static Contact MapCrmContactToContact(Entity crmContact)
    {
        var contact = new Contact(
            crmContact.Id,
            crmContact.GetAttributeValue<string>(CrmConstants.Contact.CmahId),
            crmContact.GetAttributeValue<string>(CrmConstants.Contact.PreferredEmailAddress),
            crmContact.GetAttributeValue<string>(CrmConstants.Contact.FirstName),
            crmContact.GetAttributeValue<string>(CrmConstants.Contact.LastName),
            crmContact.GetAttributeValue<OptionSetValue>(CrmConstants.Contact.Language)?.Value
        );
        return contact;
    }

    private static Application MapCrmApplicationToApplication(Entity crmApplication)
    {
        var application = new Application(
            crmApplication.Id,
            crmApplication.GetAttributeValue<string>(CrmConstants.Application.ApplicationNumber)
        );
        return application;
    }

    private static ApplicationWithMembershipPaymentInformation MapCrmApplicationToApplicationWithMembershipPaymentInformation(Entity crmApplication)
    {
        var application = new ApplicationWithMembershipPaymentInformation(
            crmApplication.Id,
            crmApplication.GetAttributeValue<string>(CrmConstants.Application.ApplicationNumber),
            crmApplication.GetAttributeValue<Money>(CrmConstants.Application.GrandTotal).Value,
            crmApplication.GetAttributeValue<DateTime>(CrmConstants.Application.MembershipExpiryDate),
            crmApplication.GetAttributeValue<EntityReference>(CrmConstants.Application.MembershipPriceListId).Id
                .ToString(), //TODO
            crmApplication.GetAttributeValue<OptionSetValue>(CrmConstants.Application.ProvinceStateCode).Value,
            crmApplication.GetAttributeValue<Money>(CrmConstants.Application.TotalMembershipCost).Value,
            crmApplication.GetAttributeValue<Money>(CrmConstants.Application.Subtotal).Value,
            crmApplication.GetAttributeValue<Money>(CrmConstants.Application.TotalMembershipDiscount).Value,
            crmApplication.GetAttributeValue<Money>(CrmConstants.Application.Gst).Value,
            crmApplication.GetAttributeValue<decimal>(CrmConstants.Application.GstPercentage),
            crmApplication.GetAttributeValue<Money>(CrmConstants.Application.Hst).Value,
            crmApplication.GetAttributeValue<decimal>(CrmConstants.Application.HstPercentage),
            crmApplication.GetAttributeValue<Money>(CrmConstants.Application.Qst).Value,
            crmApplication.GetAttributeValue<decimal>(CrmConstants.Application.QstPercentage)
        );
        return application;
    }
    
    public async Task<Guid> CreateAddress(Entity entity)
    {
        _logger.LogInformationWithMetadata(entity);

        var addressId = await _serviceClient.CreateAsync(entity);
        
        _logger.LogInformationWithMetadata(addressId);

        return addressId;
    }
}