using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Metadata;
using System.Text.RegularExpressions;


namespace CreatePostBatchForOnlineApplication
{
   
    public static class CrmHelpers
    {      
        public static string GetOptionSetLabelByValue(IOrganizationService service, string entityLogicalName, string attributeLogicalName, int value)
        {
            if (service == null || string.IsNullOrEmpty(entityLogicalName) || string.IsNullOrEmpty(attributeLogicalName))
            {
                return string.Empty;
            }

            var retrieveAttributeRequest = new RetrieveAttributeRequest
            {
                EntityLogicalName = entityLogicalName,
                LogicalName = attributeLogicalName
            };

            var retrieveAttributeResponse = (RetrieveAttributeResponse)service.Execute(retrieveAttributeRequest);

            var attributeMetadata = (EnumAttributeMetadata)retrieveAttributeResponse.AttributeMetadata;

            var option = attributeMetadata.OptionSet.Options.FirstOrDefault(o => o.Value == value);

            return option == null ? string.Empty : option.Label.UserLocalizedLabel.Label;
        }

        public static object GetAttribute(IOrganizationService service, string entityLogicalName, Guid id, string attributeName)
        {
            Entity entity = service.Retrieve(entityLogicalName, id, new ColumnSet(attributeName));
            return entity != null ? entity.Contains(attributeName) ? entity[attributeName] : null : null;
        }

        public static object GetAttribute(IExecutionContext context, IOrganizationService service, string attributeName)
        {
            try
            {
                var entity = service.Retrieve(context.PrimaryEntityName, context.PrimaryEntityId, new ColumnSet(attributeName));
                if (entity != null)
                {
                    return entity != null ? entity.Contains(attributeName) ? entity[attributeName] : null : null;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static object GetEntityAttribute(Entity entity, string attributeName, ITracingService tracingService = null)
        {
            if (!entity.Attributes.Contains(attributeName))
            {
                if (tracingService != null)
                {
                    tracingService.Trace(string.Format("Attribute {0} not present", attributeName));
                }
                return null;
            }

            if (tracingService != null)
            {
                tracingService.Trace(string.Format("Attribute {0} = {1}", attributeName, entity.Attributes[attributeName]));
            }

            return entity.Attributes[attributeName];

        }

        public static Entity GetCurrentEntity(IExecutionContext context, IOrganizationService service)
        {
            return service.Retrieve(context.PrimaryEntityName, context.PrimaryEntityId, new ColumnSet(true));
        }
        
        public static Entity GetEntityByEntityReference(IOrganizationService service, EntityReference entityReference)
        {
            return service.Retrieve(entityReference.LogicalName, entityReference.Id, new ColumnSet(true));
        }

        public static QueryExpression GetQueryForFetchXml(IOrganizationService service, string fetchXml)
        {
            //create query
            FetchXmlToQueryExpressionRequest fetchToQueryReq = new FetchXmlToQueryExpressionRequest();
            fetchToQueryReq.FetchXml = fetchXml;
            return ((FetchXmlToQueryExpressionResponse)service.Execute(fetchToQueryReq)).Query;
        }
        public static void UpdateEntity(OrganizationServiceContext serviceContext, Entity entity, ITracingService tracingService)
        {
            tracingService.Trace("In Update function");
            if (!serviceContext.IsAttached(entity))
            {
                serviceContext.Attach(entity);
                tracingService.Trace("attach entity");
            }
            serviceContext.UpdateObject(entity);
            tracingService.Trace("update object");
            serviceContext.SaveChanges();
            tracingService.Trace("save");

        }
        public static void TraceAttributeCollection(IOrganizationService service, Entity entity, ITracingService tracingService)
        {
            string value = "";
            if (entity != null)
            {
                tracingService.Trace("\n------------------------------------------------------------------------------------------------");
                tracingService.Trace("Dump Entity {0}", entity.LogicalName);
                foreach (KeyValuePair<string, object> attribute in entity.Attributes)
                {
                    value = "null";
                    if (attribute.Value != null)
                    {
                        if (attribute.Value is EntityReference)
                            value = ((EntityReference)attribute.Value).Name + ": " + ((EntityReference)attribute.Value).LogicalName + ": " + ((EntityReference)attribute.Value).Id.ToString();
                        else if (attribute.Value is OptionSetValue)
                            value = GetOptionSetLabelByValue(service, entity.LogicalName, attribute.Key, ((OptionSetValue)attribute.Value).Value) + ": " + ((OptionSetValue)attribute.Value).Value.ToString();
                        else if (attribute.Value is Money)
                            value = ((Money)attribute.Value).Value.ToString("C");
                        else
                            value = attribute.Value.ToString();
                    }
                    tracingService.Trace(string.Format("{0}: {1}", attribute.Key, value));
                }

                tracingService.Trace("\n");
            }
        }
        public static string DumpAttributeCollection(IOrganizationService service, Entity entity)
        {
            StringBuilder result = new StringBuilder();
            string value;
            if (entity != null)
            {
                foreach (KeyValuePair<string, object> attribute in entity.Attributes)
                {
                    value = "null";
                    if (attribute.Value != null)
                    {
                        if (attribute.Value is EntityReference)
                            value = ((EntityReference)attribute.Value).Name + ": " + ((EntityReference)attribute.Value).LogicalName + ": " + ((EntityReference)attribute.Value).Id.ToString();
                        else if (attribute.Value is OptionSetValue)
                            value = GetOptionSetLabelByValue(service, entity.LogicalName, attribute.Key, ((OptionSetValue)attribute.Value).Value) + ": " + ((OptionSetValue)attribute.Value).Value.ToString();
                        else if (attribute.Value is Money)
                            value = ((Money)attribute.Value).Value.ToString("C");
                        else
                            value = attribute.Value.ToString();
                    }
                    result.AppendLine(string.Format("{0}: {1}", attribute.Key, value));
                }
            }
            return result.ToString();
        }

        public static void TraceParameterCollection(IOrganizationService service, ParameterCollection parameters, ITracingService tracingService)
        {
            foreach (string key in parameters.Keys)
            {
                tracingService.Trace(string.Format("{0}: {1}: {2}", key, parameters[key].GetType().ToString(), parameters[key].ToString()));
            }
            tracingService.Trace("\n");
        }

        public static void TraceAttributeCollection(Entity entity, ITracingService tracingService)
        {
            foreach (KeyValuePair<string, object> attribute in entity.Attributes)
            {
                tracingService.Trace(AttributeToString(attribute.Value, attribute.Key, ""));
            }
            tracingService.Trace("\n");
        }
        public static void SetAttributeValue(Entity entity, string attributename, object value, ITracingService tracingService = null)
        {
            //if (value != null) {
            if (!entity.Attributes.Contains(attributename))
            {
                if (tracingService != null)
                {
                    tracingService.Trace("Adding missing attribute");
                }
                entity.Attributes.Add(attributename, value);
            }
            else
            {
                entity.Attributes[attributename] = value;
            }
            //}
        }

        public static SetStateResponse SetStatus(IOrganizationService xrmService, string entityName, Guid entityId, int state, int status)
        {
            SetStateRequest setState = new SetStateRequest();
            setState.EntityMoniker = new EntityReference(entityName, entityId);
            setState.State = new OptionSetValue(state);
            setState.Status = new OptionSetValue(status);
            return (SetStateResponse)xrmService.Execute(setState);
        }
        public static Entity GetEntityByAttributeValue(IOrganizationService service, string entityLogicalName, string attributeName, object attributeValue)
        {
            var query = new QueryByAttribute(entityLogicalName) { ColumnSet = new ColumnSet(true) };

            query.Attributes.Add(attributeName);
            query.Values.Add(attributeValue);

            return service.RetrieveMultiple(query).Entities.FirstOrDefault();

            //return serviceContext.CreateQuery(entityLogicalName).FirstOrDefault(entity => entity.Attributes[attributeName] == attributeValue);
        }

        public static T GetAttributeValueEx<T>(Entity entity, string attributeName, ITracingService ts = null, string prefix = null)
        {
            T result;// = (T)Convert.ChangeType(null, typeof(T));
            //Guid id = Guid.Empty;

            object temp = entity != null && entity.Contains(attributeName) ? entity[attributeName] : null;
            result = GetAttributeValueEx<T>(temp, attributeName, ts, prefix);

            return result;
        }
        public static string AttributeToString(Entity entity, string attributeName, string prefix = "", bool hideAttributeName = false)
        {
            object entityValue = entity.Contains(attributeName) ? entity[attributeName] : null;
            return AttributeToString(entityValue, attributeName, prefix, hideAttributeName);
            entityValue = entityValue is AliasedValue ? ((AliasedValue)entityValue).Value : entityValue;
            string result = $"{prefix}{ (hideAttributeName ? "" : attributeName) }: ";
            if (entityValue is Guid)
                result += $"{entityValue}";
            else if (entityValue is EntityReference)
            {
                result += $"'{ (entityValue != null ? ((EntityReference)entityValue).Name : "")}' - { (entityValue != null ? ((EntityReference)entityValue).Id : Guid.Empty) }";
            }
            else if (entityValue is OptionSetValue)
                result += $"'{ (entity.FormattedValues.Contains(attributeName) ? entity.FormattedValues[attributeName] : "") }' - { (entityValue != null ? ((OptionSetValue)entityValue).Value.ToString() : "null") }";
            else if (entityValue is Money)
                result += $"{ (entityValue != null ? ((Money)entityValue).Value.ToString() : "null")}";
            else if (entityValue is DateTime)
                result += $"{(((DateTime)entityValue) != DateTime.MinValue && entityValue != null ? ((DateTime)entityValue).ToString() : "Default to MinDate")}";
            else if (entityValue is int ||
                     entityValue is decimal ||
                     entityValue is double
            )
                result += $"{ (entityValue != null ? entityValue.ToString() : "Default to 0")}";
            else if (entityValue is bool)
                result += $"{(entityValue != null ? entityValue.ToString() : "Default to false")}";
            else
                result += $"{(entityValue != null ? entityValue.ToString() : "null")}";
            return result;
        }
        public static string AttributeToString(object entityValue, string attributeName, string prefix = "", bool hideAttributeName = false)
        {
            entityValue = entityValue is AliasedValue ? ((AliasedValue)entityValue).Value : entityValue;
            string result = $"{prefix}{ (hideAttributeName ? "" : attributeName) }: ";
            if (entityValue is Guid)
                result += $"{entityValue}";
            else if (entityValue is EntityReference)
            {
                result += $"'{ (entityValue != null ? ((EntityReference)entityValue).Name : "")}' - { (entityValue != null ? ((EntityReference)entityValue).Id : Guid.Empty) }";
            }
            else if (entityValue is OptionSetValue)
                result += $"'{ (entityValue != null ? ((OptionSetValue)entityValue).Value.ToString() : "null") }'";
            else if (entityValue is Money)
                result += $"{ (entityValue != null ? ((Money)entityValue).Value.ToString() : "null")}";
            else if (entityValue is DateTime)
                result += $"{(((DateTime)entityValue) != DateTime.MinValue && entityValue != null ? ((DateTime)entityValue).ToString() : "Default to MinDate")}";
            else if (entityValue is int ||
                     entityValue is decimal ||
                     entityValue is double
            )
                result += $"{ (entityValue != null ? entityValue.ToString() : "Default to 0")}";
            else if (entityValue is bool)
                result += $"{(entityValue != null ? entityValue.ToString() : "Default to false")}";
            else
                result += $"{(entityValue != null ? entityValue.ToString() : "null")}";
            return result;
        }
        //public static string AttributeToString(object entityValue, string attributeName, string prefix = null)
        //{
        //    string result = "";
        //    if (entityValue is Guid)
        //        result = string.Format("{0}{1}: {2}", prefix == null ? "" : prefix, attributeName, entityValue);
        //    else if (entityValue is EntityReference)
        //    {
        //        result = string.Format("{0}{1}: {2}\n", prefix == null ? "" : prefix, attributeName, entityValue != null ? ((EntityReference)entityValue).Id : Guid.Empty);
        //        result += string.Format("{0}{1}name: {2}", prefix == null ? "" : prefix, attributeName, entityValue != null ? ((EntityReference)entityValue).Name : "null");
        //    }
        //    else if (entityValue is OptionSetValue)
        //        result = string.Format("{0}{1}: {2}", prefix == null ? "" : prefix, attributeName, entityValue != null ? ((OptionSetValue)entityValue).Value.ToString() : "null");
        //    else if (entityValue is Money)
        //        result = string.Format("{0}{1}: {2}", prefix == null ? "" : prefix, attributeName, entityValue != null ? ((Money)entityValue).Value.ToString() : "null");
        //    else if (entityValue is DateTime)
        //        result = string.Format("{0}{1}: {2}", prefix == null ? "" : prefix, attributeName, ((DateTime)entityValue) != DateTime.MinValue && entityValue != null ? ((DateTime)entityValue).ToString() : "Default to MinDate");
        //    else if (entityValue is int ||
        //             entityValue is decimal ||
        //             entityValue is double
        //    )
        //        result = string.Format("{0}{1}: {2}", prefix == null ? "" : prefix, attributeName, entityValue != null ? entityValue.ToString() : "Default to 0");
        //    else if (entityValue is bool)
        //        result = string.Format("{0}{1}: {2}", prefix == null ? "" : prefix, attributeName, entityValue != null ? entityValue.ToString() : "Default to false");
        //    else
        //        result = string.Format("{0}{1}: {2}", prefix == null ? "" : prefix, attributeName, entityValue != null ? entityValue.ToString() : "null");
        //    return result;
        //}
        public static T GetAttributeValueEx<T>(object entityValue, string attributeName, ITracingService ts = null, string prefix = null)
        {
            T result;// = (T)Convert.ChangeType(null, typeof(T));

            object theValue = entityValue is AliasedValue ? ((AliasedValue)entityValue).Value : entityValue;


            if (typeof(T) == typeof(Guid))
            {
                Guid temp = theValue != null ? (Guid)theValue : Guid.Empty;
                result = (T)Convert.ChangeType(temp, typeof(T));

            }
            else if (typeof(T) == typeof(EntityReference))
            {
                EntityReference temp = theValue != null ? (EntityReference)theValue : null;
                result = (T)Convert.ChangeType(temp, typeof(T));
            }
            else if (typeof(T) == typeof(OptionSetValue))
            {
                OptionSetValue temp = theValue != null ? (OptionSetValue)theValue : null;
                result = (T)Convert.ChangeType(temp, typeof(T));
            }
            else if (typeof(T) == typeof(Money))
            {
                Money temp = theValue != null ? (Money)theValue : null;
                result = (T)Convert.ChangeType(temp, typeof(T));
            }
            else if (typeof(T) == typeof(DateTime))
            {
                DateTime temp = theValue != null ? (DateTime)theValue : DateTime.MinValue;
                result = (T)Convert.ChangeType(temp, typeof(T));
            }
            else if (typeof(T) == typeof(int) ||
                     typeof(T) == typeof(decimal) ||
                     typeof(T) == typeof(double)
            )
                result = (T)(theValue != null ? Convert.ChangeType(theValue, typeof(T)) : Convert.ChangeType(0, typeof(T)));
            else if (typeof(T) == typeof(bool))
                result = (T)(theValue != null ? Convert.ChangeType(theValue, typeof(T)) : Convert.ChangeType(false, typeof(T)));
            else
                result = (T)Convert.ChangeType(theValue, typeof(T));

            if (ts != null)
                ts.Trace(CrmHelpers.AttributeToString(theValue, attributeName, prefix));
            return result;
        }
        public static Guid GetIdByAttributeValue(IOrganizationService service, string entityName, String queryField, string queryValue)
        {
            string @newRHXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                <entity name='" + entityName + @"'>
                                  <attribute name='" + entityName + @"id' /> 
                                  <filter type='and'>
                                  <condition attribute='" + queryField + "' operator='eq' value='" +
                                    queryValue + @"' /> 
                                  </filter>
                                  </entity>
                                  </fetch>";
            var response = (RetrieveMultipleResponse)service.Execute(new RetrieveMultipleRequest
            {
                Query = new FetchExpression(newRHXml)
            });

            if (response.EntityCollection.Entities.Count > 0)
            {
                return response.EntityCollection.Entities[0].Id;
            }
            else
            {
                return Guid.Empty;
            }
        }

        public static IEnumerable<Entity> ExecuteFetch(OrganizationServiceContext serviceContext, string fetchXml)
        {
            var response = (RetrieveMultipleResponse)serviceContext.Execute(new RetrieveMultipleRequest
            {
                Query = new FetchExpression(fetchXml)
            });

            return response.EntityCollection.Entities;
        }
        public static IEnumerable<Entity> ExecuteFetch(IOrganizationService service, string fetchXml)
        {
            var response = (RetrieveMultipleResponse)service.Execute(new RetrieveMultipleRequest
            {
                Query = new FetchExpression(fetchXml)
            });

            return response.EntityCollection.Entities;
        }
        public static Entity GetFirstFromExecuteFetch(IOrganizationService service, string fetchXml)
        {
            Entity result = null;
            var response = (RetrieveMultipleResponse)service.Execute(new RetrieveMultipleRequest
            {
                Query = new FetchExpression(fetchXml)
            });

            if (response.EntityCollection.Entities.Count > 0)
            {
                result = response.EntityCollection.Entities[0];
            }
            return result;
        }
        public static IEnumerable<Entity> ExecuteFetchEnumerable(IOrganizationService service, string fetchXml)
        {
            var response = (RetrieveMultipleResponse)service.Execute(new RetrieveMultipleRequest
            {
                Query = new FetchExpression(fetchXml)
            });

            return response.EntityCollection.Entities;
        }

        public static string GetCrmSetting(OrganizationServiceContext context, string settingName, bool throwException)
        {
            string result = null;
            var setting = context.CreateQuery("adx_setting").FirstOrDefault(entity => (string)entity["adx_name"] == settingName);
            if (setting != null)
            {
                if (setting.Contains("adx_value"))
                    result = setting["adx_value"].ToString();
                //read long value if empty and entity has been changed to support long values
                if (string.IsNullOrWhiteSpace(result) && setting.Contains("sgi_longvalue"))
                {
                    result = setting["sgi_longvalue"].ToString();
                }
            }
            else if (throwException)
                throw new Exception(settingName + " - setting not found");
            return result;

        }
        public static string GetCrmSetting(IOrganizationService service, string settingName, bool throwException)
        {
            string result = null;
            QueryByAttribute query = new QueryByAttribute("adx_setting");
            query.ColumnSet = new ColumnSet(true);
            query.AddAttributeValue("adx_name", settingName);
            EntityCollection ec = service.RetrieveMultiple(query);
            if (ec != null && ec.Entities.Count > 0)
            {
                if (ec.Entities[0].Attributes.Contains("adx_value"))
                    result = ec.Entities[0].Attributes["adx_value"].ToString();
                //read long value if empty and entity has been changed to support long values
                if (string.IsNullOrWhiteSpace(result) && ec.Entities[0].Attributes.Contains("sgi_longvalue"))
                {
                    result = ec.Entities[0].Attributes["sgi_longvalue"].ToString();
                }
            }
            else if (throwException)
                throw new Exception(settingName + " - setting not found");
            return result;

        }
        public static string GetCrmSetting(IOrganizationService service, string settingName, string defaultValue)
        {
            string result = CrmHelpers.GetCrmSetting(service, settingName, false);
            if (result == null)
                result = defaultValue;
            return result;

        }
        public static string GetAutoNumberValue(IOrganizationService service, string autonumberName)
        {
            var numberEntity = new Entity("adx_autonumberingrequest");
            // Specify your Auto Numbering Definition Name 
            numberEntity.Attributes["adx_name"] = autonumberName;

            var numberEntityId = service.Create(numberEntity);
            numberEntity = service.Retrieve("adx_autonumberingrequest", numberEntityId, new ColumnSet(new[] { "adx_name", "adx_formattednumber" }));
            var formattedNumber = numberEntity.GetAttributeValue<string>("adx_formattednumber");

            return formattedNumber.ToString();
        }
        public static string GetEntityAttributeAsString(Entity entity, string attributeName)
        {
            string result = "";
            if (entity.Attributes.Contains(attributeName))
                result = entity.Attributes[attributeName].ToString();
            return result;
        }
       
        public static Guid GetEntityAttributeAsEntityRefId(Entity entity, string attributeName)
        {
            Guid result = Guid.Empty;
            if (entity.Attributes.Contains(attributeName))
                result = ((EntityReference)entity.Attributes[attributeName]).Id;
            return result;
        }

        public static int GetEntityAttributeAsInt(Entity entity, string attributeName)
        {
            int result = 0;
            if (entity.Attributes.Contains(attributeName))
                if (((int?)entity.Attributes[attributeName]).HasValue)
                    result = ((int?)entity.Attributes[attributeName]).Value;
            return result;
        }

        public static Guid SearchFor(IOrganizationService service, string entityName, string searchField, object searchValue)
        {
            Guid result = Guid.Empty;
            QueryByAttribute q = new QueryByAttribute(entityName);
            q.AddAttributeValue(searchField, searchValue);
            service.RetrieveMultiple(q);
            EntityCollection ec = service.RetrieveMultiple(q);
            if (ec != null && ec.Entities.Count > 0)
            {
                result = ec.Entities[0].Id;
            }
            return result;
        }
        public static Guid SearchForWithCount(IOrganizationService service, string entityName, string searchField, object searchValue, ref int numberFound)
        {
            Guid result = Guid.Empty;
            QueryByAttribute q = new QueryByAttribute(entityName);
            q.AddAttributeValue(searchField, searchValue);
            service.RetrieveMultiple(q);
            EntityCollection ec = service.RetrieveMultiple(q);
            if (ec != null)
            {
                numberFound = ec.Entities.Count;
                if (numberFound > 0)
                {
                    result = ec.Entities[0].Id;
                }
            }
            return result;
        }
        public static Entity SearchFor(IOrganizationService service, string entityName, string searchField, object searchValue, string[] resultFields)
        {
            Entity result = null;
            QueryByAttribute q = new QueryByAttribute(entityName);
            ColumnSet cols;
            if (resultFields != null)
                cols = new ColumnSet(resultFields);
            else
                cols = new ColumnSet(true);
            q.ColumnSet = cols;
            q.AddAttributeValue(searchField, searchValue);
            service.RetrieveMultiple(q);
            EntityCollection ec = service.RetrieveMultiple(q);
            if (ec != null && ec.Entities.Count > 0)
            {
                result = ec.Entities[0];
            }
            return result;
        }
        public static Entity SearchForWithCount(IOrganizationService service, string entityName, string searchField, object searchValue, string[] resultFields, ref int numberFound)
        {
            Entity result = null;
            QueryByAttribute q = new QueryByAttribute(entityName);
            ColumnSet cols;
            if (resultFields != null)
                cols = new ColumnSet(resultFields);
            else
                cols = new ColumnSet(true);
            q.ColumnSet = cols;
            q.AddAttributeValue(searchField, searchValue);
            service.RetrieveMultiple(q);
            EntityCollection ec = service.RetrieveMultiple(q);
            if (ec != null)
            {
                numberFound = ec.Entities.Count;
                if (numberFound > 0)
                {
                    result = ec.Entities[0];
                }
            }
            return result;
        }

        public static bool DoesUserHaveTeamMemberhip(IOrganizationService service, Guid systemUserId, string teamName)
        {
            string fetchXML = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false' top='1'>
                              <entity name='teammembership'>    
                                <attribute name='teammembershipid' />
                                <filter type='and'>
                                  <condition attribute='systemuserid' operator='eq' value='{systemUserId}' />
                                </filter>
                                <link-entity name='team' from='teamid' to='teamid' alias='team' >
                                  <filter type='and'>
                                    <condition attribute='name' operator='eq' value='{teamName}' />
                                  </filter>
                                </link-entity>
                              </entity>
                            </fetch>";
            return CrmHelpers.GetFirstFromExecuteFetch(service, fetchXML) != null;

        }

        public static bool DoesUserHaveSecurityRole(IOrganizationService service, Guid systemUserId, string roleName)
        {

            string fetchXML = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true' top='1'>
                                  <entity name='systemuserroles'>
                                    <attribute name='systemuserid' />
                                    <filter type='and'>
                                      <condition attribute='systemuserid' operator='eq' value='{systemUserId}' />
                                    </filter>
                                    <link-entity name='role' from='roleid' to='roleid' alias='role'>
                                      <filter type='and'>
                                        <condition attribute='name' operator='eq' value='{roleName}' />
                                      </filter>
                                    </link-entity>
                                  </entity>
                                </fetch>";

            return CrmHelpers.GetFirstFromExecuteFetch(service, fetchXML) != null;

        }
        public static Entity GetPluginImage(IOrganizationService service, IPluginExecutionContext executionContext, string targetImageName, string imageType, ITracingService ts = null)
        {
            Entity result = null;
            bool isPost = imageType.ToLower().StartsWith("po");
            if (isPost)
            {
                if (!executionContext.PostEntityImages.Contains(targetImageName))
                    throw new Exception($"Plugin Post image {targetImageName} not found");
                result = executionContext.PostEntityImages[targetImageName];
            }
            else
            {
                if (!executionContext.PreEntityImages.Contains(targetImageName))
                    throw new Exception($"Plugin Pre image {targetImageName} not found");
                result = executionContext.PreEntityImages[targetImageName];
            }
            if (ts != null)
            {
                ts.Trace( $"\n{(isPost ? "Post" : "Pre") } Image -----------------------------------");
                CrmHelpers.TraceAttributeCollection(service, result, ts);
            }
            return result;
        }
    }

}