using System.Reflection;
using System.ServiceModel;
using Cma.Common.Exceptions;
using Cma.Common.Extensions;
using Cma.Services.Crm.Attributes;
using Cma.Services.Crm.Contracts;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Cma.Services.Crm;

// Extracting GetEntityModel to its own file because it does a lot and it was making CrmService.cs too big 
public partial class CrmService
{
    public async Task<GetEntityModelResponse<T>> GetEntityModel<T>(GetEntityModelRequest request) where T : new()
    {
        _logger.LogInformationWithMetadata(request);
        var mainType = typeof(T);
        
        // Get the CrmEntityAttribute
        var crmEntityAttribute =  mainType
            .GetCustomAttributes<CrmEntityAttribute>(true)
            .SingleOrDefault() ?? throw new InternalServerErrorException(
                ErrorCode.NotFound.ToSnakeCase(), 
                "CrmEntityAttribute not found on object");

        // Set the CRM entity name on the main query expression
        var query = new QueryExpression(crmEntityAttribute.EntityName);

        // Get static filters from the attribute
        var mainFilterAttributes = mainType
            .GetCustomAttributes<CrmFilterAttribute>(true);

        // Request filters
        AddFilters(query, request.Filters);

        // Static filters
        AddFilters(query, mainFilterAttributes);

        AddMainEntity(query, mainType);

        var mainLink = new LinkEntity
        {
            LinkFromEntityName = query.EntityName,
            LinkToEntityName = query.EntityName,
        };

        AddLinkedEntities(mainLink, mainType);

        query.LinkEntities.AddRange(mainLink.LinkEntities);

        // Retrieve the entity from CRM
        var result = await _serviceClient.RetrieveMultipleAsync(query);

        // Expect 1 entity returned
        if (result.Entities.DistinctBy(x => x.Id).Count() != 1)
        {
            throw new ConflictException(ErrorCode.NotFound.ToSnakeCase(), "Entity not found");
        }

        var entity = result.Entities.First();

        // ---------------------------------------------
        // Map back to the object
        // ---------------------------------------------
        var parentObject = new T();

        ProcessMainEntity(parentObject, entity);
        ProcessLinkedEntities(parentObject, entity);
        
        var response = new GetEntityModelResponse<T>(parentObject);
        _logger.LogInformationWithMetadata(response);
        
        return response;
    }

    /// <summary>
    /// Update all properties on the models passed in regardless if the values are null or not
    /// </summary>
    public async Task SetEntityModel<T>(SetEntityModelRequest<T> request) where T : new()
    {
        //TODO: Validate model is not null
        var entity = ToEntity(request.EntityModel);

        // var allEntities = new Dictionary<Guid, string>
        // {
        //     // Main entity
        //     { entity.Id, entity.LogicalName }
        // };
        //
        // foreach (var relatedEntity in entity.RelatedEntities)
        // {
        //     foreach (var relatedEntityValue in relatedEntity.Value.Entities)
        //     {
        //         allEntities.Add(relatedEntityValue.Id, relatedEntityValue.LogicalName);
        //     }
        // }

        var t = 1;
        //TODO: Do I need to worry about creates?
        // Use the CRM service client to update the entity in CRM
        //await _serviceClient.UpdateAsync(entity);
    }
    
    private bool CheckIfEntityExists(string entityName, Guid entityId)
    {
        try
        {
            // Attempt to retrieve the entity
            var retrievedEntity = _serviceClient.Retrieve(entityName, entityId, new(false));
            return retrievedEntity != null;
        }
        catch (FaultException<OrganizationServiceFault> ex)
        {
            // Entity record does not exist
            if (ex.Detail.ErrorCode == -2147220969) // ErrorCode: Record With Id Does Not Exist
            {
                return false;
            }
            
            throw;
        }
    }
    
    private static Entity ToEntity<T>(T model) where T : new()
    {
        var entityType = typeof(T);

        // Get the CrmEntityAttribute
        var crmEntityAttribute = entityType
            .GetCustomAttributes<CrmEntityAttribute>(true)
            .SingleOrDefault() ?? throw new InternalServerErrorException(
            ErrorCode.NotFound.ToSnakeCase(), 
            "CrmEntityAttribute not found on object");

        // Create the entity from CRM
        var entity = new Entity(crmEntityAttribute.EntityName);
    
        // Set main entity and any linked entities
        SetEntityProperties(entity, model!);
    
        return entity;
    }

    private static void SetEntityProperties(Entity entity, object model)
    {
        // Get the properties of the class
        var properties = model.GetType().GetProperties();

        foreach (var property in properties)
        {
            var crmPropertyAttribute = property
                .GetCustomAttributes<CrmPropertyAttribute>(true)
                .FirstOrDefault();

            // Set the values for all properties with the crm property attribute
            if (crmPropertyAttribute != null)
            {
                var value = property.GetValue(model);
                entity[crmPropertyAttribute.PropertyName] = value;
            }

            var crmLinkedEntityAttribute = property
                .GetCustomAttributes<CrmLinkedEntityAttribute>(true)
                .FirstOrDefault();
            
            // Check for linked entities, if this is not a linked entity, continue to the next property
            if (crmLinkedEntityAttribute == null)
            {
                continue;
            }
            
            var linkedEntity = new Entity(crmLinkedEntityAttribute.LinkedEntityName);
            
            // Recursively set linked entity properties, if the linked model is null continue to the next property
            var linkedModel = property.GetValue(model);
            if (linkedModel == null)
            {
                //TODO: Maybe null here should be a create.
                continue;
            }
            
            SetEntityProperties(linkedEntity, linkedModel);
            entity.RelatedEntities.Add(new(
                crmLinkedEntityAttribute.LinkAlias), new(new List<Entity> {linkedEntity}));
        }
    }

    private static void AddMainEntity(QueryExpression query, Type mainType)
    {
        // Get the properties of the class
        var mainProperties = mainType.GetProperties();

        // Loop through all main properties
        foreach (var mainProperty in mainProperties)
        {
            // Get the main property attributes
            var crmPropertyAttributes = mainProperty
                .GetCustomAttributes<CrmPropertyAttribute>(true);

            // Add columns from the property attributes
            foreach (var attribute in crmPropertyAttributes)
            {
                query.ColumnSet.AddColumn(attribute.PropertyName);
            }
        }
    }

    private static void AddFilters(QueryExpression query, IEnumerable<ICrmFilter> filterAttributes)
    {
        foreach (var filterAttribute in filterAttributes)
        {
            query.Criteria.AddCondition(
                filterAttribute.FilterProperty, 
                CrmMapper.Operators.MapFilterOperator(filterAttribute.FilterOperator),
                filterAttribute.FilterValue);
        }
    }

    private static void ProcessMainEntity(object mainObject, Entity entity)
    {
        var mainType = mainObject.GetType();
        
        foreach (var mainProperty in mainType.GetProperties())
        {
            // Get the property attributes of each property
            var mainPropertyAttributes = mainProperty
                .GetCustomAttributes<CrmPropertyAttribute>(true);

            // Set the values from the main entity
            foreach (var attribute in mainPropertyAttributes)
            {
                var value = GetEntityPropertyValue(attribute.PropertyName, entity);
                mainProperty.SetValue(mainObject, value, null);
            }
        }
    }
    
    private static void AddLinkedEntities(LinkEntity link, Type type)
    {
        var properties = type.GetProperties();

        // Get all properties under the linked entity
        foreach (var property in properties)
        {
            var linkAttributes = property
                .GetCustomAttributes<CrmLinkedEntityAttribute>(true);

            // Look at any properties that have the linked entity attribute
            foreach (var linkAttribute in linkAttributes)
            {
                var newLink = new LinkEntity
                {
                    LinkFromEntityName = link.LinkToEntityName,
                    LinkFromAttributeName = linkAttribute.ParentPropertyName,
                    LinkToEntityName = linkAttribute.LinkedEntityName,
                    LinkToAttributeName = linkAttribute.LinkedPropertyName,
                    JoinOperator = CrmMapper.Operators.MapJoinOperator(linkAttribute.LinkType),
                    EntityAlias = linkAttribute.LinkAlias,
                };

                // Get the filter attribute
                var filterAttributes = property
                    .GetCustomAttributes<CrmFilterAttribute>(true);
                
                foreach (var filterAttribute in filterAttributes)
                {
                    newLink.LinkCriteria.AddCondition(filterAttribute.FilterProperty,
                        CrmMapper.Operators.MapFilterOperator(filterAttribute.FilterOperator), filterAttribute.FilterValue);
                }

                var childProperties = property.PropertyType.GetProperties();
                foreach (var childProperty in childProperties)
                {
                    var childPropertyAttributes = childProperty.GetCustomAttributes(true);
                    var childCrmPropertyAttributes = childPropertyAttributes.OfType<CrmPropertyAttribute>();

                    foreach (var childCrmPropertyAttribute in childCrmPropertyAttributes)
                    {
                        newLink.Columns.AddColumn(childCrmPropertyAttribute.PropertyName);
                    }
                }

                // Recursive call to get any sub-linked entities
                AddLinkedEntities(newLink, property.PropertyType);

                // Add the new link to the link entities collection
                link.LinkEntities.Add(newLink);
            }
        }
    }

    private void ProcessLinkedEntities(object parentObject, Entity entity)
    {
        // Get all the properties that have the CrmLinkedEntityAttribute and return that and the property
        var linkedEntities = parentObject.GetType().GetProperties()
            .SelectMany(prop => prop.GetCustomAttributes<CrmLinkedEntityAttribute>(true),
                (prop, attr) => new
                {
                    PropertyInfo = prop,
                    Attribute = attr
                });

        // Loop through all linked entities
        foreach (var linkedEntity in linkedEntities)
        {
            var parentProperty = linkedEntity.PropertyInfo;

            // Create the linked entity object
            var childObject = Activator.CreateInstance(parentProperty.PropertyType);

            // Get all the properties of the child property
            var childProperties = parentProperty.PropertyType.GetProperties();

            // Loop through all the linked entity's properties
            foreach (var childProperty in childProperties)
            {
                var childCrmPropertyAttributes = childProperty
                    .GetCustomAttributes<CrmPropertyAttribute>(true);

                foreach (var childCrmPropertyAttribute in childCrmPropertyAttributes)
                {
                    var value = GetEntityPropertyValue(
                        $"{linkedEntity.Attribute.LinkAlias}.{childCrmPropertyAttribute.PropertyName}",
                        entity);

                    childProperty.SetValue(childObject, value, null);
                }

                // Recursing if the child property also contains the CrmLinkedEntityAttribute
                var childLinkedEntityAttributes =
                    childProperty.GetCustomAttributes<CrmLinkedEntityAttribute>(true);

                if (childObject != null && childLinkedEntityAttributes.Any())
                {
                    ProcessLinkedEntities(childObject, entity);
                }
            }

            // Set parent property once after child object is fully created and all its properties are set
            parentProperty.SetValue(parentObject, childObject, null);
        }
    }

    private static object? GetEntityPropertyValue(string key, Entity entity)
    {
        if (!entity.Attributes.ContainsKey(key))
        {
            return null;
        }

        var value = entity.Attributes[key] switch
        {
            AliasedValue aliasedValue => aliasedValue.Value,
            _ => entity.Attributes[key]
        };

        value = value switch
        {
            OptionSetValue optionSetValue => optionSetValue.Value,
            EntityReference entityReference => entityReference.Id,
            Money money => money.Value,
            _ => value
        };

        return value;
    }
}