// <copyright file="EventCopyEventWorkflow.cs" company="Microsoft">
// Copyright (c) 2012 All Rights Reserved
// </copyright>
// <author>Microsoft</author>
// <date>6/25/2012 4:17:54 PM</date>
// <summary>Implements the EventCopyEventWorkflow Workflow Activity.</summary>
namespace EventCopyEventWorkflow
{
	using System;
	using System.Activities;
	using System.ServiceModel;
	using System.Globalization;
	using System.Collections.Generic;
	using System.Linq;

	using Microsoft.Xrm.Sdk;
	using Microsoft.Xrm.Sdk.Query;
	using Microsoft.Xrm.Sdk.Workflow;

	using System.Threading;



	public class EventCopyEventWorkflow : CodeActivity
	{
		enum Constants
		{
			Yes = 1,
			No = 0,
		};

		[Input("Copy Sub-Events?")]
		[RequiredArgument]
		public InArgument<int> DoCopySubEvents { get; set; }

		[Input("Copy Products?")]
		[RequiredArgument]
		public InArgument<int> DoCopyProducts { get; set; }

		[Input("Copy Promotions?")]
		[RequiredArgument]
		public InArgument<int> DoCopyPromotions { get; set; }

		[Input("Copy Hotel Room Blocks?")]
		[RequiredArgument]
		public InArgument<int> DoCopyHotelRoomBlocks { get; set; }

		[Input("Copy Venues?")]
		[RequiredArgument]
		public InArgument<int> DoCopyVenues { get; set; }

		/// <summary>
		/// Executes the workflow activity.
		/// </summary>
		/// <param name="executionContext">The execution context.</param>
		protected override void Execute(CodeActivityContext executionContext)
		{
            #region Validation and Preparation

            
			// Create WorkflowLocalContext
			var localContext = new LocalWorkflowContext(executionContext);
			localContext.Trace(string.Format(CultureInfo.InvariantCulture,
				"Entered EventCopyEventWorkflow.Execute(), Activity Instance Id: {0}, Workflow Instance Id: {1}, Correlation Id: {2}, Initiating User: {3}",
				executionContext.ActivityInstanceId,
				executionContext.WorkflowInstanceId,
				localContext.WorkExecutionContext.CorrelationId,
				localContext.WorkExecutionContext.InitiatingUserId));

            #endregion

           // throw new Exception("Values :: " + DoCopySubEvents.Get<int>(localContext.ActivityContext) + " Product:" + DoCopyProducts.Get<int>(localContext.ActivityContext) + " Promo:" + DoCopyPromotions.Get<int>(localContext.ActivityContext) + "  venue:" + DoCopyVenues.Get<int>(localContext.ActivityContext));


            #region Workfow Activity Logic
            try
            {
				RunWorkflow(localContext);
			}
			catch (FaultException<OrganizationServiceFault> e)
			{
				localContext.Trace(string.Format(CultureInfo.InvariantCulture, "Exception: {0}", e.ToString()));
				// Handle the exception.
				throw;
			}

			localContext.Trace(string.Format(CultureInfo.InvariantCulture,
				"Exiting EventCopyEventWorkflow.Execute(), Correlation Id: {0}", localContext.WorkExecutionContext.CorrelationId));

			#endregion
		}

		/// <summary>
		/// Workflow step 
		/// </summary>
		/// <param name="localContext"></param>
		private void RunWorkflow(LocalWorkflowContext localContext)
		{
			#region Required Arguments

			var doCopySubEvent = DoCopySubEvents.Get<int>(localContext.ActivityContext) == (int)Constants.No ? false : true;
			var doCopyProducts = DoCopyProducts.Get<int>(localContext.ActivityContext) == (int)Constants.No ? false : true;
			var doCopyPromotions = DoCopyPromotions.Get<int>(localContext.ActivityContext) == (int)Constants.No ? false : true;
			var doCopyHotelRoomBlocks = DoCopyHotelRoomBlocks.Get<int>(localContext.ActivityContext) == (int)Constants.No ? false : true;
			var doCopyVenues = DoCopyVenues.Get<int>(localContext.ActivityContext) == (int)Constants.No ? false : true;
			var eventPriceList = GetMostRecentActiveEventPriceList(localContext);

			#endregion

			#region Exceluded Attributes

			var eventAttributesToBeExcluded = new List<string>(new[] { "new_event_id", "new_productid", "new_registered", "new_available"
				, "new_totalregistered", "new_totalspaceavailable","new_totalnoshows","new_totalfinalattendees" });
			//Store the mapping between product of source events and cloned products associated to new events
			var productMap = new Dictionary<Guid, Guid>();
			#endregion

			//1- Retrieve Parent Event being copied and all its attributes
			var parentEvent = localContext.OrganizationService.Retrieve(localContext.WorkExecutionContext.PrimaryEntityName
																		, localContext.WorkExecutionContext.PrimaryEntityId
																		, new ColumnSet(true));
			#region Copy Event, Event Product, Product Promosions

			//2- Clone parent event and keep a reference
			//2.1- Event name can only hold 100 Characters. 35 Characters for date
			var parentEventName = parentEvent.Attributes.Contains("new_name") && !string.IsNullOrWhiteSpace(  parentEvent.Attributes["new_name"] as string)
				? parentEvent.Attributes["new_name"] as string : string.Empty;
			parentEventName = !string.IsNullOrWhiteSpace(parentEventName.Trim()) && parentEventName.Trim().Length + 35 <= 100 ? parentEventName.Trim() :
				parentEventName.Trim().Substring(0, 100 - 35);
			var newName = !string.IsNullOrWhiteSpace(parentEventName)
				? string.Format("{0} - Copied On {1}", parentEventName as string, DateTime.Now.ToString()) 
				: string.Format("Event - Copied On {0}", DateTime.Now.ToString());
			var includedAttributes = new List<KeyValuePair<string, object>>();
			includedAttributes.Add(new KeyValuePair<string, object>("new_name", newName));

			var cloneEvent = CloneEventRecord(localContext, parentEvent, eventPriceList, eventAttributesToBeExcluded, includedAttributes, doCopyProducts, doCopyPromotions, productMap);

			#endregion

			#region Copy Venues

			//3- If indicated clone all venues associated to parent event
			if (doCopyVenues)
			{
				//3.1- Retrieve venue associations
				var query = new QueryByAttribute("new_events_accountvenues");
				query.Attributes.Add("new_eventid");
				query.Values.Add(parentEvent.Id);
				query.ColumnSet = new ColumnSet("accountid");
				var venue = localContext.OrganizationService.RetrieveMultiple(query);
				var relatedEntities = new EntityReferenceCollection();
				foreach (var ent in venue.Entities)
				{
					relatedEntities.Add(new EntityReference("account", (Guid)ent.Attributes["accountid"]));
				}
				//3.2- Asscociate venue accounts to clone event
				localContext.OrganizationService.Associate(cloneEvent.LogicalName, cloneEvent.Id, new Relationship("new_events_accountvenues"), relatedEntities);
			}

			#endregion

			#region Copy Hotel Room Blocks

			//4- If indicated clone Hotel Room Blocks associated to parent event
			//if (doCopyHotelRoomBlocks)
			//{
			//	var query = new QueryByAttribute("new_hotelroomblock");
			//	query.Attributes.Add("new_eventid");
			//	query.Values.Add(parentEvent.Id);
			//	query.ColumnSet = new ColumnSet(true);
			//	var roomBlockAdditionalAttributes = new List<KeyValuePair<string, object>>(new[] { new KeyValuePair<string, object>("new_eventid", cloneEvent.ToEntityReference()) });
			//	var roomBlocks = localContext.OrganizationService.RetrieveMultiple(query);
			//	foreach (var rb in roomBlocks.Entities)
			//	{
			//		//4.1- Clone Hotel Room Block
			//		var cloneRb = CloneEntityRecord(localContext, rb, new List<string>(), roomBlockAdditionalAttributes);
			//	}
			//}

			#endregion

			#region Copy Sub-Events, Sub-Event Product, Product Promosions

			if (doCopySubEvent)
			{
				//5- if indicated copy Product-Kits
				var query = new QueryByAttribute("new_event");
				query.Attributes.Add("new_parenteventid");
				query.Values.Add(parentEvent.Id);
				query.ColumnSet = new ColumnSet(true);
				var subEvents = localContext.OrganizationService.RetrieveMultiple(query);
				var subEventAdditionalAttributes = new List<KeyValuePair<string, object>>(new[] { new KeyValuePair<string, object>("new_parenteventid", cloneEvent.ToEntityReference()) });
				foreach (var evnt in subEvents.Entities)
				{
					CloneEventRecord(localContext, evnt, eventPriceList, eventAttributesToBeExcluded, subEventAdditionalAttributes, doCopyProducts, doCopyPromotions, productMap);
				}
			}

			#endregion

			#region Copy Product-Kits

			//5- If indicated copy product-kits
			if (doCopyProducts)
			{
				//5.1- Retrieve Parent Event Product-Kit

				#region Query
				var query = new QueryExpression
				{
					EntityName = "product",
					Distinct = true,
					PageInfo = new PagingInfo
					{
						PageNumber = 1,
						Count = 1000,
						PagingCookie = null,
					},
					ColumnSet = new ColumnSet(true),
					Criteria = new FilterExpression
					{
						FilterOperator = LogicalOperator.And,
						Conditions =
										{
											new ConditionExpression("iskit", ConditionOperator.Equal, true),
											new ConditionExpression("new_eventid", ConditionOperator.Equal, parentEvent.Id),
										},
					},
				};
				#endregion
				var parentProductKits = localContext.OrganizationService.RetrieveMultiple(query);

				//5.2- Retrieve details of the kit and clone the kit
				var relatedEntities = new EntityReferenceCollection();
				foreach (var prdKit in parentProductKits.Entities)
				{
					//var clonePrdKit = CloneEntityRecord(localContext, prdKit, new List<string>(new[] { "new_product_id" }), new List<KeyValuePair<string, object>>());
					var clonePrdKit = CloneProductAndPromotions(localContext, prdKit, doCopyPromotions, productMap, eventPriceList, cloneEvent.ToEntityReference());
					relatedEntities.Add(new EntityReference("product", clonePrdKit.Id));
					//5.2.1- Retrieve products in the kit and map to newly created products. By this point these product should be cloned and exist int he product map
					var subquery = new QueryByAttribute("productassociation");
					subquery.Attributes.Add("productid");
					subquery.Values.Add(prdKit.Id);
					subquery.ColumnSet = new ColumnSet("associatedproduct");
					var associatedProducts = localContext.OrganizationService.RetrieveMultiple(subquery);
					var newAssociatedProdcuts = new EntityReferenceCollection();
					foreach (var ascProd in associatedProducts.Entities)
					{
						//5.2.1.1- Get new associated products from the product map
						var ascProdGuid = (Guid)ascProd.Attributes["associatedproduct"];
						if (productMap.ContainsKey(ascProdGuid))
						{
							var newAscProdGuid = productMap[ascProdGuid];
							newAssociatedProdcuts.Add(new EntityReference("product", newAscProdGuid));
						}
					}
					//5.2.2- Associated new products to the map
					localContext.OrganizationService.Associate(clonePrdKit.LogicalName, clonePrdKit.Id,
						new Relationship { SchemaName = "productassociation_association", PrimaryEntityRole = EntityRole.Referencing }, newAssociatedProdcuts);
				}
				////5.3- Asscociate product kits to clone event
				//localContext.OrganizationService.Associate(cloneEvent.LogicalName, cloneEvent.Id, new Relationship("new_new_event_productkit"), relatedEntities);
			}
			#endregion

		}

		#region Helper Methods

		private EntityReference GetMostRecentActiveEventPriceList(LocalWorkflowContext localContext)
		{
			var query = new QueryExpression
			{
				EntityName = "pricelevel",
				PageInfo = new PagingInfo
				{
					PagingCookie = null,
					PageNumber = 1,
					Count = 1,
				},
				ColumnSet = new ColumnSet("pricelevelid"),
				Criteria = new FilterExpression
				{
					FilterOperator = LogicalOperator.And,
					Conditions =
					{
						new ConditionExpression("new_type", ConditionOperator.Equal, 1),
						new ConditionExpression("statecode", ConditionOperator.Equal, 0),
					}
				},
				Orders =
				{
					new OrderExpression("createdon", OrderType.Descending)
				}
			};

			var results = localContext.OrganizationService.RetrieveMultiple(query);

			return results.Entities.Count > 0 ? results.Entities[0].ToEntityReference() : null;
		}

		private Entity GetProductPriceListItem(LocalWorkflowContext localContext, EntityReference product, EntityReference eventPriceList)
		{
			var query = new QueryExpression
			{
				EntityName = "productpricelevel",
				PageInfo = new PagingInfo
				{
					PagingCookie = null,
					PageNumber = 1,
					Count = 1,
				},
				ColumnSet = new ColumnSet(true),
				Criteria = new FilterExpression
				{
					FilterOperator = LogicalOperator.And,
					Conditions =
					{
						new ConditionExpression("productid", ConditionOperator.Equal, product.Id),
						new ConditionExpression("pricelevelid", ConditionOperator.Equal, eventPriceList.Id),
					}
				},
			};
			var results = localContext.OrganizationService.RetrieveMultiple(query);
			return results.Entities.Count > 0 ? results.Entities[0] : null;
		}

		private Entity CloneEventRecord(LocalWorkflowContext localContext, Entity eventToBeCopied, EntityReference eventPriceList, List<string> excludedAttributes, List<KeyValuePair<string, object>> includedAttributes, bool copyProducts, bool copyPromotions, Dictionary<Guid, Guid> productMap)
		{
			var incAttribCopy = includedAttributes.ToList();
			var excAttribCopy = excludedAttributes.ToList();

			//1- If indicated and there is a product associated to the event clone the product
			if (copyProducts && eventToBeCopied.Attributes.Contains("new_productid") && eventToBeCopied.Attributes["new_productid"] != null)
			{
				//1.1- Retrieve 
				var eventProduct = localContext.OrganizationService.Retrieve("product", ((EntityReference)eventToBeCopied.Attributes["new_productid"]).Id, new ColumnSet(true));
				var cloneEventProduct = CloneProductAndPromotions(localContext, eventProduct, copyPromotions, productMap, eventPriceList, null);
				incAttribCopy.Add(new KeyValuePair<string, object>("new_productid", cloneEventProduct.ToEntityReference()));
			}

			//2- If indicated and there is a cancellation product associated to the event, clone the cancellation product
			if (copyProducts && eventToBeCopied.Attributes.Contains("new_cancelproductid") && eventToBeCopied.Attributes["new_cancelproductid"] != null)
			{
				//1.1- Retrieve 
				var eventCancellationProduct = localContext.OrganizationService.Retrieve("product", ((EntityReference)eventToBeCopied.Attributes["new_cancelproductid"]).Id, new ColumnSet(true));
				var cloneEventCancellationProduct = CloneProductAndPromotions(localContext, eventCancellationProduct, copyPromotions, productMap, eventPriceList, null);
				incAttribCopy.Add(new KeyValuePair<string, object>("new_cancelproductid", cloneEventCancellationProduct.ToEntityReference()));
			}

			//2- Clone Event
			var cloneEvent = CloneEntityRecord(localContext, eventToBeCopied, excAttribCopy, incAttribCopy);

			return cloneEvent;
		}

		private Entity CloneProductAndPromotions(LocalWorkflowContext localContext, Entity eventProductToBeCopied, bool copyPromotions, Dictionary<Guid, Guid> productMap, EntityReference eventPriceList, EntityReference ParentEventForKits)
		{
			var tobeIncluded = new List<KeyValuePair<string, object>>();
			if (ParentEventForKits != null)
			{
				tobeIncluded.Add(new KeyValuePair<string, object>("new_eventid", ParentEventForKits));
			}
			var cloneEventProduct = CloneEntityRecord(localContext, eventProductToBeCopied, new List<string>(new[] { "new_product_id" }), tobeIncluded);

			//1.2- Add mapping to product map so we can create kits from these products
			productMap.Add(eventProductToBeCopied.Id, cloneEventProduct.Id);

			//1.3- If indicated clone associated promotions
			if (copyPromotions)
			{
				var query = new QueryByAttribute("new_promotion");
				query.Attributes.Add("new_productid");
				query.Values.Add(eventProductToBeCopied.Id);
				query.ColumnSet = new ColumnSet(true);
				var promotions = localContext.OrganizationService.RetrieveMultiple(query);
				var promotionsAdditionalAttributes = new List<KeyValuePair<string, object>>(new[] { new KeyValuePair<string, object>("new_productid", cloneEventProduct.ToEntityReference()) });
				foreach (var promo in promotions.Entities)
				{
					CloneEntityRecord(localContext, promo, new List<string>(), promotionsAdditionalAttributes);
				}
			}
			//1.4- If product has a price list item copy the list item
			var eventProductPriceListItem = GetProductPriceListItem(localContext, eventProductToBeCopied.ToEntityReference(), eventPriceList);
			if (eventProductPriceListItem != null)
			{
				eventProductPriceListItem["productid"] = cloneEventProduct.ToEntityReference();
				eventProductPriceListItem["pricelevelid"] = eventPriceList;
				var cloneProductPriceList = CloneEntityRecord(localContext, eventProductPriceListItem, new List<string>(), new List<KeyValuePair<string, object>>());
			}

			return cloneEventProduct;
		}

		private Entity CloneEntityRecord(LocalWorkflowContext localContext, Entity entityToBeCopied, List<string> excludedAttributes, List<KeyValuePair<string, object>> includedAttributes)
		{
			var allAttributesToBeExcluded = new List<string>(new[] { "createdon", "createdby", "modifiedon", "modifiedby", "createdonbehalfby",
																		"importsequencenumber","modifiedonbehalfby","overriddencreatedon","ownerid",
																		"owningbusinessunit","owningteam","owninguser","statecode","statuscode",
																		"versionnumber"});
			allAttributesToBeExcluded.AddRange(excludedAttributes);
			var cloneEntity = new Entity(entityToBeCopied.LogicalName);
			foreach (var attr in entityToBeCopied.Attributes)
			{
				if (!allAttributesToBeExcluded.Contains(attr.Key) && !attr.GetType().Equals(typeof(AliasedValue)))
				{
					cloneEntity.Attributes.Add(attr);
				}
			}

			foreach (var attr in includedAttributes)
			{
				cloneEntity.Attributes[attr.Key] = attr.Value;
			}
			cloneEntity.Attributes.Remove(entityToBeCopied.LogicalName + "id");

			var cloneEntityGuid = localContext.OrganizationService.Create(cloneEntity);
			//cloneEntity.Attributes[entityToBeCopied.LogicalName + "id"] = cloneEntityGuid;
			cloneEntity.Id = cloneEntityGuid;
			return cloneEntity;
		}

		#endregion

		#region Internal Methods & Classes

		private class LocalWorkflowContext
		{
			internal IServiceProvider ServiceProvider
			{
				get;

				private set;
			}

			internal IOrganizationService OrganizationService
			{
				get;

				private set;
			}

			internal IWorkflowContext WorkExecutionContext
			{
				get;

				private set;
			}

			internal CodeActivityContext ActivityContext
			{
				get;
				private set;
			}

			internal ITracingService TracingService
			{
				get;

				private set;
			}

			private LocalWorkflowContext()
			{
			}

			internal LocalWorkflowContext(CodeActivityContext executionContext)
			{
				if (executionContext == null)
				{
					throw new ArgumentNullException("executionContext");
				}
				this.ActivityContext = executionContext;
				// Obtain the execution context service from the service provider.
				this.WorkExecutionContext = executionContext.GetExtension<IWorkflowContext>();
				// Obtain the tracing service from the service provider.
				this.TracingService = executionContext.GetExtension<ITracingService>();
				// Obtain the Organization Service factory service from the service provider
				var factory = executionContext.GetExtension<IOrganizationServiceFactory>();
				// Use the factory to generate the Organization Service.
				this.OrganizationService = factory.CreateOrganizationService(this.WorkExecutionContext.UserId);
			}

			internal void Trace(string message)
			{
				if (string.IsNullOrWhiteSpace(message) || this.TracingService == null)
				{
					return;
				}
				if (this.WorkExecutionContext == null)
				{
					this.TracingService.Trace(message);
				}
				else
				{
					this.TracingService.Trace(
						"{0}, Correlation Id: {1}, Initiating User: {2}",
						message,
						this.WorkExecutionContext.CorrelationId,
						this.WorkExecutionContext.InitiatingUserId);
				}
			}
		}

		#endregion

	}
}
