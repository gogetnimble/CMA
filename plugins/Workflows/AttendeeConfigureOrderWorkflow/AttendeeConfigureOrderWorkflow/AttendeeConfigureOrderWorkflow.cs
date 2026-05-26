// <copyright file="AttendeeConfigureOrderWorkflow.cs" company="Microsoft">
// Copyright (c) 2012 All Rights Reserved
// </copyright>
// <author>Microsoft</author>
// <date>6/25/2012 4:17:54 PM</date>
// <summary>Implements the AttendeeConfigureOrderWorkflow Workflow Activity.</summary>
namespace AttendeeConfigureOrderWorkflow
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



	public class AttendeeConfigureOrderWorkflow : CodeActivity
	{



		protected override void Execute(CodeActivityContext executionContext)
		{
			#region Validation and Preparation

			// Create WorkflowLocalContext
			var localContext = new LocalWorkflowContext(executionContext);
			localContext.Trace(string.Format(CultureInfo.InvariantCulture,
				"Entered AttendeeConfigureOrderWorkflow.Execute(), Activity Instance Id: {0}, Workflow Instance Id: {1}, Correlation Id: {2}, Initiating User: {3}",
				executionContext.ActivityInstanceId,
				executionContext.WorkflowInstanceId,
				localContext.WorkExecutionContext.CorrelationId,
				localContext.WorkExecutionContext.InitiatingUserId));

			#endregion

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
				"Exiting AttendeeConfigureOrderWorkflow.Execute(), Correlation Id: {0}", localContext.WorkExecutionContext.CorrelationId));

			#endregion
		}

		/// <summary>
		/// Workflow step 
		/// </summary>
		/// <param name="localContext"></param>
		private void RunWorkflow(LocalWorkflowContext localContext)
		{
			#region Workflow Body

			#region Preconditions and Required Arguments

			var attendeeLineItems = GetEventAttendee(localContext);
			var attendee = attendeeLineItems[0];
			//0- Only continue if attendee record has a SalesOrder
			if (!attendee.Attributes.Contains("new_salesorderid"))
			{
				return;
			}

			var existingLineItems = (from l in attendeeLineItems.Entities
									 where l.Attributes.Contains("olt.productid")
									 select l).ToList();
			if (existingLineItems.Count > 0)
			{
				CancelAttendee(localContext, attendee);
			}

			//1- Get details of registered events and sub-event, their product, promotions, and product kits
			var registeredEvents = GetEventSubEventProductsPromotions(localContext, (EntityReference)attendee.Attributes["new_eventid"]);

			//2- Get Parent Event 
			var parentEvent = (from e in registeredEvents
							   where e.Value.IsParentEvent
							   select e).First();
			//3- Get Product Kits
			var productKits = (from p in parentEvent.Value.Products
							   where p.Value.IsKit
							   select p.Value).ToList();
			//4- Get Non-Kit Products
			var nonKitProducts = GetAllNonKitEventProducts(registeredEvents);

			#endregion

			//5- If attendee is registered for all sub-events of a kit add kit to the order and remove those products form the nonKitProduc collection
			foreach (var kit in productKits.OrderByDescending(p => p.KitProducts.Count))
			{
				if (IsAttendeeRegisterdInAllKitSubEvents(attendee, kit, nonKitProducts))
				{
					//5.1- Add Kit to order
					AddLineItemToOrder(localContext, attendee, kit);
					//5.2- Remove kit products 
					foreach (var kp in kit.KitProducts)
					{
						nonKitProducts.Remove(nonKitProducts.Where(p => p.ProductId == kp).First());
					}
				}
			}

			//6- Add any product that wasn't in a kit
			foreach (var p in nonKitProducts)
			{

				AddLineItemToOrder(localContext, attendee, p);
			}

			#endregion
		}
		/*******************************************************************************************************************/
		#region Helper Methods

		enum LineItemStatus
		{
			Active = 100000000,
			Cancelled = 100000001,
			Pending = 100000002,
			Submitted = 100000003
		};

		private EntityCollection GetEventAttendee(LocalWorkflowContext localContext)
		{
			var fetchQuery = @"<fetch mapping='logical' count='1000' version='1.0'>";
			fetchQuery += @"<entity name='new_eventattendee'>";
			fetchQuery += @"<attribute name='new_eventid' />";
			fetchQuery += @"<attribute name='new_salesorderid' />";
			fetchQuery += @"<filter>";
			fetchQuery += @"<condition attribute='new_eventattendeeid' operator='eq' value='" + localContext.WorkExecutionContext.PrimaryEntityId + @"' />";
			fetchQuery += @"</filter>";
			//fetchQuery += @"<link-entity name='new_promotion' from='new_promotionid' to='new_promotionid' alias='prm' link-type='outer'>";
			//fetchQuery += @"<attribute name='new_discountcategory' />";
			//fetchQuery += @"<attribute name='new_earlybird' />";
			//fetchQuery += @"<attribute name='new_promotionid' />";
			//fetchQuery += @"<attribute name='new_discountcategory' />";
			//fetchQuery += @"</link-entity>";
			fetchQuery += @"<link-entity name='salesorderdetail' from='new_eventattendeeid' to='new_eventattendeeid' alias='olt' link-type='outer'>";
			fetchQuery += @"<attribute name='productid' />";
			fetchQuery += @"</link-entity>";
			fetchQuery += @"</entity>";
			fetchQuery += @"</fetch>";
			return localContext.OrganizationService.RetrieveMultiple(new FetchExpression(fetchQuery));
		}

		private void AddLineItemToOrder(LocalWorkflowContext localContext, Entity attendee, Product product)
		{
			var orderLineItem = new Entity("salesorderdetail");
			orderLineItem.Attributes["salesorderid"] = (EntityReference)attendee.Attributes["new_salesorderid"];
			orderLineItem.Attributes["productid"] = new EntityReference { LogicalName = "product", Id = product.ProductId };
			orderLineItem.Attributes["quantity"] = 1m;
			orderLineItem.Attributes["uomid"] = product.UoM;
			orderLineItem.Attributes["new_eventattendeeid"] = attendee.ToEntityReference();
			orderLineItem.Attributes["new_status"] = new OptionSetValue((int)LineItemStatus.Pending);

			//if (attendee.Attributes.Contains("prm.new_promotionid"))
			//{
			//	var promo = GetAttendeeApplicablePromotion(attendee, product);
			//	orderLineItem.Attributes["new_promotion"] = promo == null ? null :
			//		new EntityReference { LogicalName = "new_promotion", Id = promo.PromotionId };

			//	Money promoDiscountAmount = promo != null && promo.DiscountAmount != null && promo.DiscountAmount.Value != 0m ? new Money(promo.DiscountAmount.Value) : null;
			//	var promoDiscountPercentage = promo != null && promoDiscountAmount == null ? promo.DiscountPercentage : null;
			//	orderLineItem.Attributes["new_discountcategory"] = promo != null ? promo.Category : null;
			//	orderLineItem.Attributes["manualdiscountamount"] = promoDiscountAmount != null ? promoDiscountAmount : new Money(0);
			//	if (promoDiscountAmount == null && promoDiscountPercentage != null)
			//	orderLineItem.Attributes["new_discountpercentage"] = promoDiscountPercentage;
			//}
			localContext.OrganizationService.Create(orderLineItem);
		}

		private bool IsAttendeeRegisterdInAllKitSubEvents(Entity attendee, Product productKit, List<Product> registeredProducts)
		{
			var productsInKit = (from p in registeredProducts
								 where productKit.KitProducts.Contains(p.ProductId)
								 select p).ToList();

			return productsInKit.Count > 0 && productKit.KitProducts.Count == productsInKit.Count;
		}

		//private Promotion GetAttendeeApplicablePromotion(Entity attendee, Product prodct)
		//{
		//	var attendeePromotionCategory = attendee.Attributes.Contains("prm.new_discountcategory") && attendee.Attributes["prm.new_discountcategory"] != null
		//		? (OptionSetValue)((AliasedValue)attendee.Attributes["prm.new_discountcategory"]).Value : new OptionSetValue(-99999999);
		//	var attendeePromotionEarlyBird = attendee.Attributes.Contains("prm.new_earlybird") && attendee.Attributes["prm.new_earlybird"] != null
		//		? (bool)((AliasedValue)attendee.Attributes["prm.new_earlybird"]).Value : false;
		//	var promo = (from p in prodct.Promotions.Values
		//				 where p.Category.Equals(attendeePromotionCategory)
		//						 && p.IsEarlyBird.Equals(attendeePromotionEarlyBird)
		//				 select p).FirstOrDefault();
		//	return promo; ;
		//}

		private List<Product> GetAllNonKitEventProducts(Dictionary<Guid, Event> eventList)
		{
			var eventProducts = new List<Product>();
			foreach (var e in eventList.Values)
			{
				var products = (from p in e.Products
								where !p.Value.IsKit
								select p.Value).ToList();
				eventProducts.AddRange(products);
			}
			return eventProducts;
		}

		private Dictionary<Guid, Event> GetEventSubEventProductsPromotions(LocalWorkflowContext localContext, EntityReference parentEvent)
		{
			var _events = new Dictionary<Guid, Event>();
			//1- Get Registered Sub-Events
			var registeredEventsQuery = new QueryByAttribute("new_eventattendee_event");
			registeredEventsQuery.Attributes.Add("new_eventattendeeid");
			registeredEventsQuery.Values.Add(localContext.WorkExecutionContext.PrimaryEntityId);
			registeredEventsQuery.ColumnSet = new ColumnSet("new_eventid");
			var registeredEvents = localContext.OrganizationService.RetrieveMultiple(registeredEventsQuery);
			var productKits = new Dictionary<Guid, Product>();

			//2- Get Event Products for parent event and sub events and promotions
			#region Query
			var fetchQuery = @"<fetch mapping='logical' count='1000' version='1.0'>";
			fetchQuery += @"<entity name='new_event'>";
			fetchQuery += @"<attribute name='new_eventid' />";
			fetchQuery += @"<attribute name='new_parenteventid' />";
			fetchQuery += @"<filter type='or'>";
			fetchQuery += @"<condition attribute='new_eventid' operator='eq' value='" + parentEvent.Id + @"' />";
			//2-1- Add ID of sub-event to fetchQuery condition
			foreach (var subEvent in registeredEvents.Entities)
			{
				fetchQuery += @"<condition attribute='new_eventid' operator='eq' value='" + (Guid)subEvent.Attributes["new_eventid"] + @"' />";
			}
			fetchQuery += @"</filter>";
			fetchQuery += @"<link-entity name='product' from='productid' to='new_productid' alias='prd' link-type='outer'>";
				fetchQuery += @"<attribute name='productid' />";
				fetchQuery += @"<attribute name='defaultuomid' />";
			   //fetchQuery += @"<link-entity name='new_promotion' from='new_productid' to='productid' alias='prdprm' link-type='outer'>";
					//fetchQuery += @"<attribute name='new_discountamount' />";
					//fetchQuery += @"<attribute name='new_discountcategory' />";
					//fetchQuery += @"<attribute name='new_discountpercentage' />";
					//fetchQuery += @"<attribute name='new_earlybird' />";
					//fetchQuery += @"<attribute name='new_promotionid' />";
				//fetchQuery += @"</link-entity>";
				fetchQuery += @"<link-entity name='productassociation' from='associatedproduct' to='productid' alias='nnkit' link-type='outer'>";
					fetchQuery += @"<attribute name='associatedproduct' />";
				fetchQuery += @"<link-entity name='product' from='productid' to='productid' alias='kit' link-type='outer'>";
					fetchQuery += @"<attribute name='defaultuomid' />";
					fetchQuery += @"<attribute name='productid' />";
					fetchQuery += @"<filter>";
						fetchQuery += @"<condition attribute='new_eventid' operator='eq' value=' " + parentEvent.Id + "' />";
					fetchQuery += @"</filter>";
					//fetchQuery += @"<link-entity name='new_promotion' from='new_productid' to='productid' alias='kitprdprm' link-type='outer'>";
						//fetchQuery += @"<attribute name='new_discountamount' />";
					//fetchQuery += @"<attribute name='new_discountcategory' />";
			//fetchQuery += @"<attribute name='new_discountpercentage' />";
			//fetchQuery += @"<attribute name='new_earlybird' />";
			//fetchQuery += @"<attribute name='new_promotionid' />";
			//fetchQuery += @"</link-entity>";
			fetchQuery += @"</link-entity>";
			fetchQuery += @"</link-entity>";
			fetchQuery += @"</link-entity>";
			fetchQuery += @"</entity>";
			fetchQuery += @"</fetch>";

			#endregion
			var results = localContext.OrganizationService.RetrieveMultiple(new FetchExpression(fetchQuery));

			//3- Create a collection of all registered events
			foreach (var e in results.Entities)
			{
				//1- If event is not already in the collection create one otherwise update
				var newEvent = _events.ContainsKey(e.Id) ? _events[e.Id] : new Event { EventId = e.Id, IsParentEvent = !e.Attributes.Contains("new_parenteventid") };
				var isRegisteredForParenEvent = true; //registeredEvents.Entities.Where(re => re.Attributes["new_eventid"].Equals(e.Id)).FirstOrDefault() != null;
													  //2- Add/Update product kits and their promotions to Product
				#region Product Kits
				if (e.Attributes.Contains("kit.productid"))
				{
					var kitGuid = (Guid)((AliasedValue)e.Attributes["kit.productid"]).Value;
					var kitUoM = (EntityReference)((AliasedValue)e.Attributes["kit.defaultuomid"]).Value;
					//2.1- If product kit has already been added to Event Products collection update otherwise create
					var newKit = productKits.ContainsKey(kitGuid) ? productKits[kitGuid] :
						new Product { ProductId = kitGuid, IsKit = true, UoM = kitUoM };


					//2.2- Add promotion to the kit if it has any
					//if (e.Attributes.Contains("kitprdprm.new_promotionid"))
					//{
					//	var promotionGuid = (Guid)((AliasedValue)e.Attributes["kitprdprm.new_promotionid"]).Value;
					//	var promoDiscountCategory = (OptionSetValue)((AliasedValue)e.Attributes["kitprdprm.new_discountcategory"]).Value;
					//	var promotionEarlyBird = e.Attributes.Contains("kitprdprm.new_earlybird") && e.Attributes["kitprdprm.new_earlybird"] != null
					//		? (bool)((AliasedValue)e.Attributes["kitprdprm.new_earlybird"]).Value : false;
					//	var promotionAmount = e.Attributes.Contains("kitprdprm.new_discountamount") && e.Attributes["kitprdprm.new_discountamount"] != null
					//		? (decimal?)((Money)((AliasedValue)e.Attributes["kitprdprm.new_discountamount"]).Value).Value : null;
					//	var promotionPercentage = e.Attributes.Contains("kitprdprm.new_discountpercentage") && e.Attributes["kitprdprm.new_discountpercentage"] != null
					//		? (decimal?)((AliasedValue)e.Attributes["kitprdprm.new_discountpercentage"]).Value : null;

					//	//2.2.1- If promotion has already been added to the kit update it otherwise create
					//	var newPromotion = newKit.Promotions.ContainsKey(promotionGuid) ? newKit.Promotions[promotionGuid] :
					//			new Promotion
					//			{
					//				PromotionId = promotionGuid,
					//				Category = promoDiscountCategory,
					//				IsEarlyBird = promotionEarlyBird,
					//				DiscountAmount = promotionAmount,
					//				DiscountPercentage = promotionPercentage
					//			};
					//	newKit.Promotions[newPromotion.PromotionId] = newPromotion;
					//}
					//2.3- Add Kit-Product to kit if it has any
					if (e.Attributes.Contains("nnkit.associatedproduct"))
					{
						var kitProdGuid = (Guid)((AliasedValue)e.Attributes["nnkit.associatedproduct"]).Value;
						if (!newKit.KitProducts.Contains(kitProdGuid))
						{
							newKit.KitProducts.Add(kitProdGuid);
						}
					}
					//2.4- Update product in the dictionary
					productKits[newKit.ProductId] = newKit;
				}

				#endregion
				//3- Add/Update product and their promotions to event
				#region Products
				if ((e.Attributes.Contains("prd.productid") && !e.Id.Equals(parentEvent.Id))
			|| (e.Attributes.Contains("prd.productid") && e.Id.Equals(parentEvent.Id) && isRegisteredForParenEvent))
				{
					var prdGuid = (Guid)((AliasedValue)e.Attributes["prd.productid"]).Value;
					var prdUoM = (EntityReference)((AliasedValue)e.Attributes["prd.defaultuomid"]).Value;
					//3.1- If product kit has already been added to Event Products collection update otherwise create
					var newPrd = newEvent.Products.ContainsKey(prdGuid)
								? newEvent.Products[prdGuid] : new Product { ProductId = prdGuid, IsKit = false, UoM = prdUoM };
					////3.2- Add promotion to the kit if it has any
					//if (e.Attributes.Contains("prdprm.new_promotionid"))
					//{
					//	var promotionGuid = (Guid)((AliasedValue)e.Attributes["prdprm.new_promotionid"]).Value;
					//	var promoDiscountCategory = (OptionSetValue)((AliasedValue)e.Attributes["prdprm.new_discountcategory"]).Value;
					//	var promotionEarlyBird = e.Attributes.Contains("prdprm.new_earlybird") && e.Attributes["prdprm.new_earlybird"] != null
					//		? (bool)((AliasedValue)e.Attributes["prdprm.new_earlybird"]).Value : false;
					//	var promotionAmount = e.Attributes.Contains("prdprm.new_discountamount") && e.Attributes["prdprm.new_discountamount"] != null
					//		? (decimal?)((Money)((AliasedValue)e.Attributes["prdprm.new_discountamount"]).Value).Value : null;
					//	var promotionPercentage = e.Attributes.Contains("prdprm.new_discountpercentage") && e.Attributes["prdprm.new_discountpercentage"] != null
					//		? (decimal?)((AliasedValue)e.Attributes["prdprm.new_discountpercentage"]).Value : null;
					//	//3.2.1- If promotion has already been added to the kit update it otherwise create
					//	var newPromotion = newPrd.Promotions.ContainsKey(promotionGuid) ? newPrd.Promotions[promotionGuid] :
					//			new Promotion
					//			{
					//				PromotionId = promotionGuid,
					//				Category = promoDiscountCategory,
					//				IsEarlyBird = promotionEarlyBird,
					//				DiscountAmount = promotionAmount,
					//				DiscountPercentage = promotionPercentage
					//			};
					//	newPrd.Promotions[newPromotion.PromotionId] = newPromotion;
					//}
					//3.4- Update product in the dictionary
					newEvent.Products[newPrd.ProductId] = newPrd;
				}

				#endregion
				//4- Update event in the collection
				_events[newEvent.EventId] = newEvent;
			}
			//5- Add product kits to parent event
			#region Add Product Kits
			var kitProdfetchXml = @"<fetch mapping='logical' count='50' version='1.0'>";
			kitProdfetchXml += @"<entity name='productassociation'>";
			kitProdfetchXml += @"<attribute name='associatedproduct' />";
			kitProdfetchXml += @"<attribute name='productassociationid' />";
			kitProdfetchXml += @"<attribute name='productid' />";
			kitProdfetchXml += @"<filter type='or'>";
			foreach (var p in productKits)
			{

				kitProdfetchXml += @"<condition attribute='productid' operator='eq' value='" + p.Key + @"' />";
				//AddLineItemToOrder(localContext, attendee, p);
			}
			kitProdfetchXml += @"</filter>";
			kitProdfetchXml += @"</entity>";
			kitProdfetchXml += @"</fetch>";
			results = localContext.OrganizationService.RetrieveMultiple(new FetchExpression(kitProdfetchXml));

			foreach (var kit in results.Entities)
			{
				var kitGuid = (Guid)kit.Attributes["productid"];
				var kitProduct = (Guid)kit.Attributes["associatedproduct"];
				var registredKit = productKits.Keys.Count > 0 ? productKits[kitGuid] : null;
				if (registredKit != null && !registredKit.KitProducts.Contains(kitProduct))
					registredKit.KitProducts.Add(kitProduct);
			}



			var pevt = _events.Values.Where(e => e.IsParentEvent == true).First();
			foreach (var p in productKits.Values)
			{

				pevt.Products.Add(p.ProductId, p);
			}

			#endregion

			return _events;
		}

		private List<Entity> GetAttendeeSubEvents(LocalWorkflowContext localContext, EntityReference attendee)
		{
			var registeredEvents = new List<Entity>();
			//1- Get Sub-Events from N:N relationship
			var query = new QueryByAttribute("new_eventattendee_event");
			query.Attributes.Add("new_eventattendee");
			query.Values.Add(attendee.Id);
			var subEvents = localContext.OrganizationService.RetrieveMultiple(query);

			//2- Get poducts of all 

			return registeredEvents;
		}

		private EntityCollection GetPendingOrSubmittedLineItems(LocalWorkflowContext localcontext, Entity attendee)
		{
			EntityCollection items = new EntityCollection();
			string fetchxml = @"<fetch mapping='logical' version='1.0'>
									<entity name='new_eventattendee'>
										<attribute name='new_cancellationfee' />
										<attribute name='new_eventattendeeid' />
										<attribute name='new_salesorderid' />
										<filter>
											<condition attribute='new_eventattendeeid' operator='eq' value='" + attendee.Id.ToString() + @"' />
										</filter>
										<link-entity name='salesorder' from='salesorderid' to='new_salesorderid' alias='salesorder' link-type='outer'>
											<link-entity name='salesorderdetail' from='salesorderid' to='salesorderid' alias='lineitems' link-type='outer'>
												<attribute name='salesorderdetailid' />
												<attribute name='new_status' />
												<attribute name='new_relatedsalesorderdetailid' />
												<attribute name='salesorderid' />
												<attribute name='manualdiscountamount' />
												<attribute name='productid' />
												<attribute name='quantity' />
												<attribute name='uomid' />
												<attribute name='new_eventattendeeid' />
												<filter>
													<condition attribute='new_eventattendeeid' operator='eq' value='" + attendee.Id.ToString() + @"' />                                                
													<filter type='or'>
														<condition attribute='new_status' operator='eq' value='" + (int)LineItemStatus.Pending + @"' />
														<condition attribute='new_status' operator='eq' value='" + (int)LineItemStatus.Submitted + @"' />
													</filter>
												</filter>   
											</link-entity>
										</link-entity>
									</entity>
								</fetch>";
			EntityCollection c = localcontext.OrganizationService.RetrieveMultiple(new FetchExpression(fetchxml));
			foreach (Entity cc in c.Entities)
			{
				if (cc.Attributes.Contains("new_salesorderid"))
				{
					Entity e = new Entity("salesorderdetail");

					/**attributes from the line item**/

					//sales order id
					if (cc.Attributes.Contains("lineitems.salesorderdetailid"))
						e.Id = (Guid)((AliasedValue)cc.Attributes["lineitems.salesorderdetailid"]).Value;
					//status
					if (cc.Attributes.Contains("lineitems.new_status"))
						e.Attributes["new_status"] = (OptionSetValue)((AliasedValue)cc.Attributes["lineitems.new_status"]).Value;
					//related line item
					if (cc.Attributes.Contains("lineitems.new_relatedsalesorderdetailid"))
						e.Attributes["new_relatedsalesorderdetailid"] = (string)((AliasedValue)cc.Attributes["lineitems.new_relatedsalesorderdetailid"]).Value;
					//order
					if (cc.Attributes.Contains("lineitems.salesorderid"))
						e.Attributes["salesorderid"] = ((AliasedValue)cc.Attributes["lineitems.salesorderid"]).Value;
					//manual discount
					if (cc.Attributes.Contains("lineitems.manualdiscountamount"))
						e.Attributes["manualdiscountamount"] = ((AliasedValue)cc.Attributes["lineitems.manualdiscountamount"]).Value;
					//product
					if (cc.Attributes.Contains("lineitems.productid"))
						e.Attributes["productid"] = ((AliasedValue)cc.Attributes["lineitems.productid"]).Value;
					//quantity
					if (cc.Attributes.Contains("lineitems.quantity"))
						e.Attributes["quantity"] = ((AliasedValue)cc.Attributes["lineitems.quantity"]).Value;
					//uom
					if (cc.Attributes.Contains("lineitems.uomid"))
						e.Attributes["uomid"] = ((AliasedValue)cc.Attributes["lineitems.uomid"]).Value;
					//event attendee
					if (cc.Attributes.Contains("lineitems.new_eventattendeeid"))
						e.Attributes["new_eventattendeeid"] = ((AliasedValue)cc.Attributes["lineitems.new_eventattendeeid"]).Value;

					/**attributes from the attendee**/

					//cancellation fee
					if (cc.Attributes.Contains("new_cancellationfee"))
						e.Attributes["attendee.new_cancellationfee"] = cc.Attributes["new_cancellationfee"];
					//sales order
					if (cc.Attributes.Contains("new_salesorderid"))
						e.Attributes["attendee.salesorderid"] = cc.Attributes["new_salesorderid"];
					//the event attendee id from the event attendee
					if (cc.Attributes.Contains("new_eventattendeeid"))
						e.Attributes["attendee.new_eventattendeeid"] = cc.Attributes["new_eventattendeeid"];

					//add it
					items.Entities.Add(e);
				}
			}
			return items;
		}

		private Entity GetProduct(LocalWorkflowContext localcontext, string productnumber)
		{
			QueryExpression query = new QueryExpression("product");
			// Specify the columns to retrieve
			query.ColumnSet = new ColumnSet("productid");
			//conditions
			query.Criteria = new FilterExpression()
			{
				Conditions = 
				{
					//product number
					new ConditionExpression("productnumber", ConditionOperator.Equal, productnumber)
				}
			};
			var result = localcontext.OrganizationService.RetrieveMultiple(query);
			if (result.Entities.Count > 0)
			{
				return result.Entities[0];
			}
			//else return null
			return null;
		}

		private Guid GetProductUOM(LocalWorkflowContext localcontext, Guid pid)
		{
			Entity p = localcontext.OrganizationService.Retrieve("product", pid, new ColumnSet("defaultuomid"));
			if (p != null && p.Attributes.Contains("defaultuomid"))
			{
				return ((EntityReference)p.Attributes["defaultuomid"]).Id;
			}
			return Guid.Empty;
		}

		private Entity getCancelingLineItem(EntityCollection allitems, Entity item)
		{
			//go through the list of submitted items, and find an item that references the passed item
			foreach (Entity lineitem in allitems.Entities)
			{
				if (lineitem.Attributes.Contains("new_status") &&
					((OptionSetValue)lineitem.Attributes["new_status"]).Value == (int)LineItemStatus.Submitted &&
					lineitem.Attributes.Contains("new_relatedsalesorderdetailid") &&
					(string)lineitem.Attributes["new_relatedsalesorderdetailid"] == item.Id.ToString())
				{
					return lineitem;
				}
			}
			return null;
		}

		private void CancelAttendee(LocalWorkflowContext localcontext, Entity attendee)
		{
			//#region set cancellation date
			////Entity a = new Entity("new_eventattendee");
			////a.Attributes["new_eventattendeeid"] = attendee.Id;
			////a.Attributes["new_cancellationdate"] = DateTime.Now;
			////localcontext.OrganizationService.Update(a);
			//#endregion

			//attendee has an order? we know that by retrieving all pending/submitted line items
			//this method returns a list of line items that are submitted or pending. when there are none,
			//but there is an order for the attendee, it returns one row, where attributes in the entity 
			//are prefeixed with attendee. (example: when there are line items in the collection, you can retrieve
			//the order id like this lineitem[salesorderid], where there aren't, but the attendee has an order you
			//can access the order like this lineitem[attendee.salesorderid]
			EntityCollection lineitems = GetPendingOrSubmittedLineItems(localcontext, attendee);
			
			foreach (Entity lineitem in lineitems.Entities)
			{
				#region delete pending line items
				if (lineitem.Attributes.Contains("new_status") && ((OptionSetValue)lineitem.Attributes["new_status"]).Value == (int)LineItemStatus.Pending)
				{
					localcontext.OrganizationService.Delete("salesorderdetail", lineitem.Id);
				}
				#endregion

				#region process submitted line items
				if (lineitem.Attributes.Contains("new_status") && ((OptionSetValue)lineitem.Attributes["new_status"]).Value == (int)LineItemStatus.Submitted)
				{
					//not a canceling line item AND no related line item?
					if (!lineitem.Attributes.Contains("new_relatedsalesorderdetailid") && getCancelingLineItem(lineitems, lineitem) == null)
					{
						//create related line item
						Entity op = new Entity("salesorderdetail");
						//related linei item
						op.Attributes["new_relatedsalesorderdetailid"] = lineitem.Id.ToString();
						//event attendee
						if (lineitem.Attributes.Contains("new_eventattendeeid")) op.Attributes["new_eventattendeeid"] = lineitem.Attributes["new_eventattendeeid"];
						//order
						if (lineitem.Attributes.Contains("salesorderid")) op.Attributes["salesorderid"] = lineitem.Attributes["salesorderid"];
						//manual discount
						if (lineitem.Attributes.Contains("manualdiscountamount")) op.Attributes["manualdiscountamount"] = new Money(((Money)lineitem.Attributes["manualdiscountamount"]).Value * -1);
						//product
						if (lineitem.Attributes.Contains("productid")) op.Attributes["productid"] = lineitem.Attributes["productid"];
						//quantity
						if (lineitem.Attributes.Contains("quantity")) op.Attributes["quantity"] = Convert.ToDecimal(lineitem.Attributes["quantity"]) * -1m;
						//uom
						if (lineitem.Attributes.Contains("uomid")) op.Attributes["uomid"] = lineitem.Attributes["uomid"];
						//status
						op.Attributes["new_status"] = new OptionSetValue((int)LineItemStatus.Pending);
						localcontext.OrganizationService.Create(op);
					}
				}
				#endregion
			}
		}

		private class Event
		{
			public Dictionary<Guid, Product> _products;
			public Guid EventId { get; set; }
			public bool IsParentEvent { get; set; }
			public Dictionary<Guid, Product> Products
			{
				get
				{
					_products = _products == null ? new Dictionary<Guid, Product>() : _products;
					return _products;
				}
				private set { _products = value; }
			}
		}

		private class Product
		{
			//public Dictionary<Guid, Promotion> _promotions;
			public List<Guid> _kitProducs;
			public Guid ProductId { get; set; }
			public bool IsKit { get; set; }
			public List<Guid> KitProducts
			{
				get
				{
					_kitProducs = _kitProducs == null ? new List<Guid>() : _kitProducs;
					return _kitProducs;
				}
				private set { _kitProducs = value; }
			}

			//public Dictionary<Guid, Promotion> Promotions
			//{
			//	get
			//	{
			//		_promotions = _promotions == null ? new Dictionary<Guid, Promotion>() : _promotions;
			//		return _promotions;
			//	}
			//	private set { _promotions = value; }
			//}
			public EntityReference UoM { get; set; }
		}

		//private class Promotion
		//{
		//	public Guid PromotionId { get; set; }
		//	public OptionSetValue Category { get; set; }
		//	public bool IsEarlyBird { get; set; }
		//	public decimal? DiscountPercentage { get; set; }
		//	public decimal? DiscountAmount { get; set; }
		//}

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
