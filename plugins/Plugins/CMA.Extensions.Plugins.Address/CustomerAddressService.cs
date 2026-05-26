using CMA.Extensions.Constants;
using CMA.Extensions.Model;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CMA.Extensions.Plugins.Address
{

    public class CustomerAddressService
    {
        private readonly IOrganizationService _service;
        private readonly PlugInLogger _logger;

        public CustomerAddressService(IOrganizationService service, PlugInLogger logger)
        {
            _service = service;
            _logger = logger;
        }

        #region Public Methods(ENTRY POINTS)
        /// <summary>
        /// Handles create operation for CustomerAddress.
        /// Initializes processing logic without a pre-image.
        /// </summary>
        public void HandleCreate(CustomerAddress address)
        {
            Process(address, null);
        }

        /// <summary>
        /// Handles update operation for CustomerAddress.
        /// Passes both current and previous (pre-image) state for comparison.
        /// </summary>
        public void HandleUpdate(CustomerAddress address, CustomerAddress preImage)
        {
            Process(address, preImage);
        }

        /// <summary>
        /// Handles update of CMA Preferred Address logic.
        /// Determines whether to opt-out subscriptions or switch preferred address,
        /// and clears the triggering field to prevent recursion.
        /// </summary>
        public void HandleUpdateCMAPreferred(CustomerAddress target, CustomerAddress postImage)
        {
            _logger.Info("HandleUpdateCMAPreferred START");
            if (target == null || postImage == null)
            {
                _logger.Warning("Target or PostImage is null. Exiting.");
                return;
            }

            if (string.IsNullOrEmpty(postImage.new_AddressTypeForCMAPreferredAddress))
            {
                _logger.Trace("AddressTypeForCMAPreferredAddress is null/empty. Nothing to process.");
                return;
            }

            var parent = postImage.ParentId;
            if (parent == null)
            {
                _logger.Trace("AddressTypeForCMAPreferredAddress is null/empty. Nothing to process.");
                return;
            }

            var contactId = parent.Id;
            var addressId = postImage.Id;
            var type = postImage.new_AddressTypeForCMAPreferredAddress;

            _logger.Info($"Processing CMA Preferred update. ContactId: {contactId}, AddressId: {addressId}, Type: {type}");
            if (type == CustomerAddressConstants.NotCMAPreferred)
            {
                _logger.Info("Address marked as NOT CMA Preferred. Triggering opt-out logic.");
                HandleNotPreferred(contactId, postImage);
            }
            else
            {
                _logger.Info("Address marked as NOT CMA Preferred. Triggering opt-out logic.");
                HandlePreferredSwitch(contactId, addressId, type, postImage);
            }

            _logger.Trace("Clearing CMA Preferred trigger field to avoid recursion.");
            ClearCMAPreferredField(addressId);
            _logger.Info("HandleUpdateCMAPreferred COMPLETE");
        }
        #endregion

        #region Business Logic
        /// <summary>
        /// Core processing logic for maintaining preferred address state.
        /// Updates or clears parent entity address based on active/preferred flags,
        /// and ensures only one preferred address exists.
        /// </summary>
        private void Process(CustomerAddress address, CustomerAddress preImage)
        {
            _logger.Trace("Process START");

            var fullAddress = address;

            bool isActive = fullAddress.new_Status ?? false;
            bool isPreferred = fullAddress.new_cmapreferred ?? false;

            bool wasActive = preImage?.new_Status ?? false;
            bool wasPreferred = preImage?.new_cmapreferred ?? false;

            var parentRef = fullAddress.ParentId;
            if (parentRef == null)
            {
                _logger.Warning($"ParentId is null for AddressId: {fullAddress.Id}. Exiting Process.");
                return;
            }


            var parent = GetParent<Contact>(parentRef);
            _logger.Trace($"State => isActive: {isActive}, isPreferred: {isPreferred}, wasActive: {wasActive}, wasPreferred: {wasPreferred}");
            if (isActive && isPreferred)
            {
                _logger.Info("Active + Preferred → Updating parent");
                UpdateParent(parent, fullAddress);

                if (!wasPreferred)
                {
                    _logger.Trace("Resetting other preferred addresses");
                    ResetOtherPreferred(parentRef.Id, fullAddress.Id);
                }

            }
            else if (!isActive && isPreferred)
            {
                _logger.Info("Inactive + Preferred → Clearing parent");
                if (wasActive)
                    ClearParent(parent);

                if (!wasPreferred)
                    ResetOtherPreferred(parentRef.Id, fullAddress.Id);
            }
            else if (!isPreferred)
            {
                _logger.Trace("Address is not preferred");
                bool exists = OtherPreferredExists(parentRef.Id, fullAddress.Id);

                if (wasPreferred && !exists)
                {
                    _logger.Info("No other preferred exists → Clearing parent");
                    ClearParent(parent);
                }
            }
            _logger.Trace("Process COMPLETE");
        }

        /// <summary>
        /// Updates the parent entity (Contact) with address details
        /// from the given CustomerAddress.
        /// Maps address fields and sets preferred address type when applicable.
        /// </summary>
        private void UpdateParent(Entity parent, CustomerAddress addr)
        {
            if (parent.LogicalName == Contact.EntityLogicalName)
            {
                var contact = parent.ToEntity<Contact>();

                _logger.Trace($"Updating Contact address. ContactId: {contact.Id}, AddressId: {addr.Id}");
                contact.Address1_Line1 = addr.Line1;
                contact.Address1_Line2 = addr.Line2;
                contact.Address1_Line3 = addr.Line3;
                contact.Address1_City = addr.City;
                contact.Address1_PostalCode = addr.PostalCode;


                // OptionSet (Choice field)
                contact.Address1_Country = addr.new_Country?.ToString();
                contact.Address1_StateOrProvince = addr.new_StateorProvinceMasterList?.ToString();

                if (addr.AddressTypeCode == null)
                {
                    _logger.Trace("AddressTypeCode is null → clearing preferred address type");
                    contact.Address1_AddressTypeCode = null;
                    contact.new_CMAPreferredAddressType = null;
                }
                else if ((int)addr.AddressTypeCode == (int)customeraddress_addresstypecode.ShipTo)
                {
                    _logger.Trace("AddressTypeCode = ShipTo → setting preferred address type");
                    contact.new_CMAPreferredAddressType = (new_addresstype?)addr.AddressTypeCode;

                }
                _service.Update(contact.ToEntity<Entity>());
                _logger.Info($"Contact updated successfully. ContactId: {contact.Id}");
            }
        }

        /// <summary>
        /// Clears address-related fields on the parent entity (Contact/Account).
        /// Used when no valid preferred address exists.
        /// </summary>
        private void ClearParent(Entity parent)
        {
            if (parent.LogicalName == Contact.EntityLogicalName)
            {
                var contact = parent.ToEntity<Contact>();
                _logger.Trace($"Clearing Contact address. ContactId: {contact.Id}");
                contact.Address1_Line1 = null;
                contact.Address1_Line2 = null;
                contact.Address1_Line3 = null;
                contact.Address1_City = null;
                contact.Address1_PostalCode = null;
                contact.Address1_Country = null;
                contact.Address1_StateOrProvince = null;
                contact.Address1_AddressTypeCode = null;
                contact.new_CMAPreferredAddressType = null;

                _service.Update(contact.ToEntity<Entity>());
                _logger.Info($"Contact address cleared. ContactId: {contact.Id}");
            }
            else if (parent.LogicalName == Account.EntityLogicalName)
            {
                var account = parent.ToEntity<Account>();
                _logger.Trace($"Clearing Account address. AccountId: {account.Id}");
                account.Address1_Line1 = null;
                account.Address1_Line2 = null;
                account.Address1_Line3 = null;
                account.Address1_City = null;
                account.Address1_PostalCode = null;
                account.Address1_Country = null;
                account.Address1_StateOrProvince = null;
                account.Address1_AddressTypeCode = null;

                _service.Update(account);
                _logger.Info($"Account address cleared. AccountId: {account.Id}");
            }
            else
            {
                _logger.Warning($"ClearParent skipped. Unsupported entity: {parent.LogicalName}");
            }
        }

        /// <summary>
        /// Handles scenario where address is marked as NOT CMA Preferred.
        /// Opts out all related subscription (new_opt) records for the given address type.
        /// </summary>
        private void HandleNotPreferred(Guid contactId, CustomerAddress address)
        {
            if (address.AddressTypeCode == null)
            {
                _logger.Trace($"HandleNotPreferred skipped. AddressTypeCode is null. AddressId: {address.Id}");
                return;
            }

            _logger.Info($"Opting out subscriptions. ContactId: {contactId}, AddressType: {address.AddressTypeCode}");
            var opts = FetchOpts(contactId, (int)address.AddressTypeCode);
            _logger.Trace($"Found {opts.Count()} opt records to update");
            foreach (var opt in opts)
            {
                opt.statuscode = new_opt_statuscode.OptOut;
                _service.Update(opt.ToEntity<Entity>());
            }
            _logger.Info($"Opt-out completed. Total updated: {opts.Count()}");
        }

        /// <summary>
        /// Handles switching of CMA Preferred Address.
        /// Sets the new preferred address and updates all subscription (new_opt)
        /// records with the new address details.
        /// </summary>
        private void HandlePreferredSwitch(Guid contactId, Guid addressId, string type, CustomerAddress currentAddress)
        {
            _logger.Info($"Switching CMA Preferred Address. ContactId: {contactId}, Type: {type}");

            var newAddresses = FetchAddresses(contactId, type).ToList();

            if (newAddresses.Count == 0)
            {
                _logger.Warning($"No address found for ContactId: {contactId}, Type: {type}. Skipping.");
                return;
            }

            var newAddress = newAddresses[0].ToEntity<CustomerAddress>();

            // set new preferred
            newAddress.new_cmapreferred = true;
            _service.Update(newAddress);

            _logger.Trace($"New preferred address set. AddressId: {newAddress.Id}");

            var oldAddress = GetById(addressId);

            if (oldAddress.AddressTypeCode == null)
            {
                _logger.Trace($"Old address has no AddressTypeCode. AddressId: {addressId}. Skipping opt update.");
                return;
            }

            var opts = FetchOpts(contactId, (int)oldAddress.AddressTypeCode).ToList();

            _logger.Trace($"Updating {opts.Count} opt records with new address");

            foreach (var opt in opts)
            {
                UpdateOptWithAddress(opt, newAddress);
                _service.Update(opt.ToEntity<Entity>());
            }

            _logger.Info($"Preferred switch completed. Updated {opts.Count} opt records.");
        }

        /// <summary>
        /// Maps CustomerAddress fields to a new_opt entity.
        /// Updates address-related fields including lines, city, postal code,
        /// address type, country, and state/province.
        /// </summary>
        private void UpdateOptWithAddress(new_opt opt, CustomerAddress addr)
        {
            _logger.Trace($"Mapping address to opt. OptId: {opt.Id}, AddressId: {addr.Id}");
            opt.new_AddressID = addr.new_address_id;
            opt.new_Line1 = addr.Line1;
            opt.new_Line2 = addr.Line2;
            opt.new_Line3 = addr.Line3;
            opt.new_City = addr.City;
            opt.new_PostalCode = addr.PostalCode;

            if (addr.AddressTypeCode != null)
                opt.new_AddressType = (new_addresstype)addr.AddressTypeCode;

            if (addr.new_Country != null)
                opt.new_Country = (new_addresscountry)addr.new_Country;

            if (addr.new_StateorProvinceMasterList != null)
                opt.new_stateorprovincemasterlist =
                (new_addressstateofprovince)addr.new_StateorProvinceMasterList;
        }
        #endregion

        #region Data Access
        /// <summary>
        /// Retrieves a CustomerAddress record by its ID.
        /// Returns null if ID is empty or record not found.
        /// </summary>
        private CustomerAddress GetById(Guid id)
        {
            if (id == Guid.Empty) return null;

            var entity = _service.Retrieve(
            CustomerAddress.EntityLogicalName,
            id,
            new ColumnSet(true)
            );

            return entity?.ToEntity<CustomerAddress>();
        }

        /// <summary>
        /// Retrieves the parent entity (Contact) based on the given reference.
        /// Returns the entity as the specified early-bound type.
        /// </summary>
        private T GetParent<T>(EntityReference parentRef) where T : Entity
        {
            if (parentRef == null) return null;

            var entity = _service.Retrieve(
            parentRef.LogicalName,
            parentRef.Id,
            new ColumnSet(true)
            );

            return entity?.ToEntity<T>();
        }

        /// <summary>
        /// Checks if another preferred CustomerAddress exists for the same parent,
        /// excluding the current address.
        /// </summary>
        private bool OtherPreferredExists(Guid parentId, Guid addressId)
        {
            if (parentId == Guid.Empty)
            {
                _logger.Trace("OtherPreferredExists skipped. ParentId is empty.");
                return false;
            }

            var query = new QueryExpression(CustomerAddress.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(AttributeHelper.AttributeName<CustomerAddress>(c => c.CustomerAddressId))
            };

            query.Criteria.AddCondition(
                AttributeHelper.AttributeName<CustomerAddress>(c => c.new_cmapreferred),
                ConditionOperator.Equal,
                true
            );

            query.Criteria.AddCondition(
                AttributeHelper.AttributeName<CustomerAddress>(c => c.ParentId),
                ConditionOperator.Equal,
                parentId
            );

            query.Criteria.AddCondition(
                AttributeHelper.AttributeName<CustomerAddress>(c => c.CustomerAddressId),
                ConditionOperator.NotEqual,
                addressId
            );

            var result = _service.RetrieveMultiple(query);
            bool exists = result.Entities.Any();
            _logger.Trace($"OtherPreferredExists result: {exists} for ParentId: {parentId}");
            return exists;
        }

        /// <summary>
        /// Resets all other preferred CustomerAddress records for a parent
        /// by setting their preferred flag to false.
        /// Ensures only one preferred address remains.
        /// </summary>
        private void ResetOtherPreferred(Guid parentId, Guid addressId)
        {
            if (parentId == Guid.Empty)
            {
                _logger.Trace("ResetOtherPreferred skipped. ParentId is empty.");
                return;
            }

            var query = new QueryExpression(CustomerAddress.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(AttributeHelper.AttributeName<CustomerAddress>(c => c.CustomerAddressId))
            };

            query.Criteria.AddCondition(
                AttributeHelper.AttributeName<CustomerAddress>(c => c.new_cmapreferred),
                ConditionOperator.Equal,
                true
            );

            query.Criteria.AddCondition(
               AttributeHelper.AttributeName<CustomerAddress>(c => c.ParentId),
                ConditionOperator.Equal,
                parentId
            );

            query.Criteria.AddCondition(
                AttributeHelper.AttributeName<CustomerAddress>(c => c.CustomerAddressId),
                ConditionOperator.NotEqual,
            addressId
            );

            var results = _service.RetrieveMultiple(query);

            if (results.Entities == null || results.Entities.Count == 0)
            {
                _logger.Trace($"ResetOtherPreferred: no records found for ParentId: {parentId}");
                return;
            }

            _logger.Info($"Resetting preferred flag for {results.Entities.Count} addresses. ParentId: {parentId}");
            foreach (var entity in results.Entities)
            {
                var addr = new CustomerAddress
                {
                    Id = entity.Id,
                    new_cmapreferred = false
                };

                _service.Update(addr.ToEntity<Entity>());
            }
            _logger.Info($"ResetOtherPreferred completed. Updated: {results.Entities.Count} records.");
        }

        /// <summary>
        /// Clears the CMA Preferred Address trigger field on CustomerAddress.
        /// Used to prevent plugin recursion after processing.
        /// </summary>
        private void ClearCMAPreferredField(Guid addressId)
        {
            var update = new CustomerAddress
            {
                Id = addressId,
                new_AddressTypeForCMAPreferredAddress = null
            };
            _service.Update(update);
        }

        /// <summary>
        /// Retrieves all active new_opt records for a contact filtered by address type.
        /// Used for updating subscription preferences.
        /// </summary>
        private IEnumerable<new_opt> FetchOpts(Guid contactId, int addressType)
        {
            _logger.Trace($"Fetching OPT records. ContactId: {contactId}, AddressType: {addressType}");
            var query = new QueryExpression(new_opt.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(AttributeHelper.AttributeName<Contact>(c => c.new_contact_opt))
            };
            query.Criteria.AddCondition(
                AttributeHelper.AttributeName<Contact>(c => c.new_contact_id),
                ConditionOperator.Equal,
                contactId);
            query.Criteria.AddCondition(AttributeHelper.AttributeName<Contact>(c => c.StatusCode),
                ConditionOperator.Equal,
                (int)new_opt_statuscode.OptIn);
            query.Criteria.AddCondition(AttributeHelper.AttributeName<Contact>(c => c.Address1_AddressTypeCode),
                ConditionOperator.Equal,
                addressType);

            var result = _service.RetrieveMultiple(query);
            var opts = result.Entities.Select(e => e.ToEntity<new_opt>()).ToList();
            _logger.Trace($"FetchOpts result count: {opts.Count}");
            return opts;
        }

        /// <summary>
        /// Retrieves CustomerAddress records for a contact filtered by address type.
        /// Used to identify the new preferred address.
        /// </summary>
        private IEnumerable<CustomerAddress> FetchAddresses(Guid contactId, string addressType)
        {
            _logger.Trace($"Fetching addresses. ContactId: {contactId}, AddressType: {addressType}");
            var query = new QueryExpression(CustomerAddress.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(
                    AttributeHelper.AttributeName<CustomerAddress>(c => c.CustomerAddressId),
                    AttributeHelper.AttributeName<CustomerAddress>(c => c.Line1),
                    AttributeHelper.AttributeName<CustomerAddress>(c => c.Line2),
                    AttributeHelper.AttributeName<CustomerAddress>(c => c.Line3),
                    AttributeHelper.AttributeName<CustomerAddress>(c => c.City),
                    AttributeHelper.AttributeName<CustomerAddress>(c => c.PostalCode),
                    AttributeHelper.AttributeName<CustomerAddress>(c => c.AddressTypeCode),
                    AttributeHelper.AttributeName<CustomerAddress>(c => c.new_Country),
                    AttributeHelper.AttributeName<CustomerAddress>(c => c.new_StateorProvince_100000038)
                    )
            };

            query.Criteria.AddCondition(
                AttributeHelper.AttributeName<CustomerAddress>(c => c.AddressTypeCode),
                ConditionOperator.Equal,
                addressType);

            var link = query.AddLink(
                AttributeHelper.AttributeName<Contact>(c => c.new_account_contact),
                AttributeHelper.AttributeName<Contact>(c => c.ParentContactId),
                AttributeHelper.AttributeName<Contact>(c => c.new_contact_id));

            link.LinkCriteria.AddCondition(AttributeHelper.AttributeName<Contact>(c => c.new_contact_id),
                ConditionOperator.Equal, contactId);

            var result = _service.RetrieveMultiple(query);
            var addresses = result.Entities
                .Select(e => e.ToEntity<CustomerAddress>())
                .ToList();
            _logger.Trace($"FetchAddresses result count: {addresses.Count}");
            return addresses;
        }
        #endregion
    }
}