using System;
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk.Query; 
using System.Net.Http;

namespace ContactSetCmaCaIdWorkflow
{
	public class ContactSetCmaCaIdWorkflow : CodeActivity
	{

		#region Input/Output Arguments
		[Input("Shareholder ID")]
		[RequiredArgument]
		public InArgument<string> ShareholderId { get; set; }

		[Input("CMA.CA ID")]
		[RequiredArgument]
		public InArgument<string> CmaCaId { get; set; }
		

		[Output("Contact Guid")]
		public OutArgument<string> ContactGuid { get; set; }

		#endregion

		protected override void Execute(CodeActivityContext executionContext)
		{
			// Obtain the organization service reference.
			var context = executionContext.GetExtension<IWorkflowContext>();
			var serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
			var service = serviceFactory.CreateOrganizationService(context.UserId);
			ITracingService ts = executionContext.GetExtension<ITracingService>();

			var shareholderId = ShareholderId.Get<string>(executionContext);
			var cmaCaId = CmaCaId.Get<string>(executionContext);

			var getContactByContactIDQuery = string.Format(@"<fetch distinct='false' mapping='logical' output-format='xml-platform' version='1.0' no-lock='true'>
				<entity name='contact'>
				<attribute name='new_cmaid'/>
				<attribute name='contactid'/>
				<attribute name='new_contact_id'/>
				<filter type='and'>
				<condition attribute='new_contact_id' value='{0}' operator='eq'/>
				</filter>
                    <link-entity name='new_contacttype' from='new_contacttypeid' to='new_contacttypeid' visible='false' link-type='outer' alias='ContactType'>
                      <attribute name='new_contacttype_id' />
                    </link-entity >
                </entity>
				</fetch>", shareholderId);

			ContactGuid.Set(executionContext, new Guid().ToString());
			var results = service.RetrieveMultiple(new FetchExpression(getContactByContactIDQuery));
			var contactToBeUpdated = results != null && results.Entities.Count > 0 ? results.Entities[0] : null;
			if (contactToBeUpdated != null)
			{
				ContactGuid.Set(executionContext, contactToBeUpdated.Id.ToString());
				contactToBeUpdated.Attributes["new_cmaid"] = cmaCaId;
                contactToBeUpdated.Attributes["adx_identity_username"] = cmaCaId;
                contactToBeUpdated.Attributes["adx_identity_securitystamp"] = Guid.NewGuid().ToString();
                contactToBeUpdated.Attributes["adx_identity_logonenabled"] = true; //new OptionSetValue(1);
                //contactToBeUpdated.Attributes["new_azureb2cusername"] = cmaCaId;

                service.Update(contactToBeUpdated);
                //throw new InvalidPluginExecutionException("Rooz   -   " + ContactGuid.Get<string>(executionContext));

                //Insert External Identity
                var identityProvider = "";
                string siteSettingsQuery = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>" +
                                  "<entity name = 'adx_sitesetting' >" +
                                  "<attribute name = 'adx_value' />" +
                                  "<attribute name='adx_name' />" +
                                  "<order attribute = 'adx_name' descending = 'false' />" +
                                  "<filter type = 'and' >" +
                                    "<condition attribute = 'adx_name' operator= 'eq' value = 'Authentication/OpenIdConnect/cma/ExternalIdentity' />" +
                                  "</filter>" +
                                  "</entity>" +
                                  "</fetch> ";
                EntityCollection settingColl = service.RetrieveMultiple(new FetchExpression(siteSettingsQuery));
                if (settingColl.Entities.Count > 0)
                {
                    identityProvider = settingColl.Entities[0].Attributes["adx_value"].ToString();
                }

                QueryExpression qe = new QueryExpression();
                qe.EntityName = "adx_externalidentity";
                qe.ColumnSet = new ColumnSet();
                qe.ColumnSet.Columns.Add("adx_contactid");
                qe.ColumnSet.Columns.Add("adx_username");
                qe.ColumnSet.Columns.Add("adx_identityprovidername");
                qe.Criteria.AddCondition("adx_contactid", ConditionOperator.Equal, contactToBeUpdated.Id);
                EntityCollection ec = service.RetrieveMultiple(qe);
                if (ec.Entities.Count == 0)
                {
                    Entity externalidentity = new Entity("adx_externalidentity");
                    externalidentity["adx_contactid"] = new EntityReference(contactToBeUpdated.LogicalName, contactToBeUpdated.Id);
                    externalidentity["adx_username"] = cmaCaId;
                    externalidentity["adx_identityprovidername"] = identityProvider;
                    ts.Trace("insert External Idientity");
                    service.Create(externalidentity);
                }
                else
                {
                    var externalidentity = ec.Entities[0];
                    externalidentity["adx_username"] = cmaCaId;
                    externalidentity["adx_identityprovidername"] = identityProvider;
                    service.Update(externalidentity);
                }

                //check for role
                //string contactType = "";
                //string contactTypeNum = "20";
                //if (contactToBeUpdated.Attributes.Contains("ContactType.new_contacttype_id"))
                //{
                    //contactType = ec.Entities[0].FormattedValues["ContactType.new_contacttype_id"].ToString();
                    //throw new InvalidPluginExecutionException($"Role names: {contactType} not found");
                    //if (contactType == "CT1017")
                    //{
                    //    contactTypeNum = "13"; //Physician - Member or Physician - non Member
                    //}
                    //else if (contactType == "CT1004")
                    //    contactTypeNum = "30"; //Employee
                    //else
                    //    contactTypeNum = "20"; // Memberof public
                //}
               // AssociateContactWebRole(service, contactToBeUpdated.Id, contactTypeNum, ts, false);
            }
        }

        static void AssociateContactWebRole(IOrganizationService service, Guid contactId, string contactType, ITracingService ts, bool isMember = false)
        {
            string roleName = "";
            switch (contactType)
            {
                case "11":  //Student
                case "12":  //Resident
                case "13":  //Physician
                    if (isMember)
                        roleName = "Physician - Non Member";
                    else
                        roleName = "Physician - Member";
                    break;
                case "20":
                    roleName = "Member of Public";
                    break;
                case "30":  // Employee
                    roleName = "Employee - Regular";
                    break;
            }

            if (roleName != "")
            {
                //Check if role exists
                string fetchXML = $@"<fetch>
                                    <entity name='adx_webrole_contact' >                           
                                    <filter>
                                      <condition attribute='contactid' operator='eq' value='{contactId}' />
                                    </filter>                                   
                                      <link-entity name='adx_webrole' from='adx_webroleid' to='adx_webroleid' link-type='outer' alias='webrole'>
                                        <attribute name='adx_name' />
                                      </link-entity>
                                  </entity>
                                </fetch>";
                

               // var contactRoles = CrmHelpers.ExecuteFetchEnumerable(service, fetchXML).ToList();
                var contactRoles = service.RetrieveMultiple(new FetchExpression(fetchXML));
                bool roleExists = false;
                ts.Trace("before contact Roles "+ contactId);
                if (contactRoles != null && contactRoles.Entities.Count > 0)
                {
                    ts.Trace("count1 :: " + contactRoles.Entities.Count);
                    foreach (Entity cr in contactRoles.Entities)
                    {
                        if (cr.Attributes.Contains("webrole.adx_name"))
                        {
                            ts.Trace("Roles2 :: " +  (string)((AliasedValue)cr.Attributes["webrole.adx_name"]).Value);
                            if (roleName.ToLower() == ((string)((AliasedValue)cr.Attributes["webrole.adx_name"]).Value).ToLower())
                                roleExists = true;
                        }
                    }
                }

                if (roleExists == true)
                {
                    ts.Trace("Role Exist true");
                    return;
                }

                ts.Trace("Role Exist false");
                //Assign WebRoles
                QueryExpression query = new QueryExpression
                {
                    EntityName = "adx_webrole",
                    ColumnSet = new ColumnSet("adx_name"),
                    Criteria = new FilterExpression()
                    {
                        Conditions =
                        {
                            new ConditionExpression("adx_name", ConditionOperator.Equal, roleName),
                        }
                    }
                };
                ts.Trace("Before Retrieve Multiple");
                //get role id
                var role = service.RetrieveMultiple(query).Entities;

                if (role.Count >= 1)
                {
                    ts.Trace("Role count >= 1");
                    //do in a try in case role is alraedy associated
                    try
                    {
                        service.Associate("contact",
                            contactId,
                            new Relationship("adx_webrole_contact"),
                            new EntityReferenceCollection() { new EntityReference("adx_webrole", role[0].Id) });
                        ts.Trace("Association Completed");
                    }
                    catch (Exception e)
                    {

                    }
                }
                //else
                //    throw new InvalidPluginExecutionException($"Role names: {roleName} not found");
            }
        }

    }

}
