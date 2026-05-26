//using System;
//using System.Collections.Generic;
//using Microsoft.Xrm.Sdk;
//using Xunit;
//using FakeXrmEasy;
//using CMA.Extensions.Model;
//using CMA.Extensions.Constants;

//namespace CMA.Extensions.Plugins.Address;

//public class CustomerAddressServiceTests
//{
//    private readonly XrmFakedContext _context;
//    private readonly IOrganizationService _service;
//    private readonly CustomerAddressService _sut;

//    public CustomerAddressServiceTests()
//    {
//        _context = new XrmFakedContext();
//        _service = _context.GetOrganizationService();
//        _sut = new CustomerAddressService(_service, null);
//    }
    
//    //****************************************HANDLE CREATE / UPDATE TESTS (Process logic)**************************************************//
//    //Active + Preferred → should update parent
//    [Fact]
//    public void HandleCreate_WhenActiveAndPreferred_ShouldUpdateContact()
//    {
//        var contactId = Guid.NewGuid();

//        var contact = new Contact { Id = contactId };

//        var address = new CustomerAddress
//        {
//            Id = Guid.NewGuid(),
//            ParentId = new EntityReference("contact", contactId),
//            new_Status = true,
//            new_cmapreferred = true,
//            Line1 = "Test Line"
//        };

//        _context.Initialize(new List<Entity> { contact, address });

//        _sut.HandleCreate(address);

//        var updated = _context.CreateQuery<Contact>().First();

//        Assert.Equal("Test Line", updated.Address1_Line1);
//    }
    
//    //Active false + Preferred true → should clear parent
//    [Fact]
//    public void HandleUpdate_WhenInactiveButPreferred_ShouldClearParent()
//    {
//        var contactId = Guid.NewGuid();

//        var contact = new Contact
//        {
//            Id = contactId,
//            Address1_Line1 = "Old"
//        };

//        var address = new CustomerAddress
//        {
//            Id = Guid.NewGuid(),
//            ParentId = new EntityReference("contact", contactId),
//            new_Status = false,
//            new_cmapreferred = true
//        };

//        var pre = new CustomerAddress
//        {
//            new_Status = true
//        };

//        _context.Initialize(new List<Entity> { contact, address });

//        _sut.HandleUpdate(address, pre);

//        var updated = _context.CreateQuery<Contact>().First();

//        Assert.Null(updated.Address1_Line1);
//    }
    
    
//    //Not preferred anymore → clear if no other preferred
//    [Fact]
//    public void HandleUpdate_WhenNoPreferredExists_ShouldClearParent()
//    {
//        var contactId = Guid.NewGuid();

//        var contact = new Contact { Id = contactId };

//        var address = new CustomerAddress
//        {
//            Id = Guid.NewGuid(),
//            ParentId = new EntityReference("contact", contactId),
//            new_cmapreferred = false
//        };

//        var pre = new CustomerAddress
//        {
//            new_cmapreferred = true
//        };

//        _context.Initialize(new List<Entity> { contact, address });

//        _sut.HandleUpdate(address, pre);

//        var updated = _context.CreateQuery<Contact>().First();

//        Assert.Null(updated.Address1_Line1);
//    }
    
//    //Preferred changed → should reset others
//    [Fact]
//    public void HandleUpdate_WhenNewPreferred_ShouldResetOthers()
//    {
//        var contactId = Guid.NewGuid();

//        var addr1 = new CustomerAddress
//        {
//            Id = Guid.NewGuid(),
//            ParentId = new EntityReference("contact", contactId),
//            new_cmapreferred = true
//        };

//        var addr2 = new CustomerAddress
//        {
//            Id = Guid.NewGuid(),
//            ParentId = new EntityReference("contact", contactId),
//            new_cmapreferred = true
//        };

//        var pre = new CustomerAddress { new_cmapreferred = false };

//        _context.Initialize(new List<Entity> { addr1, addr2 });

//        _sut.HandleUpdate(addr1, pre);

//        var all = _context.CreateQuery<CustomerAddress>().ToList();

//        Assert.Single(all.Where(a => a.new_cmapreferred == true));
//    }

    
//    //Invalid / Edge Input Combined Test
//    [Fact]
//    public void HandleCreate_Should_Handle_Invalid_Or_Empty_Fields_Gracefully()
//    {
//        var address = new CustomerAddress
//        {
//            Id = Guid.NewGuid(),
//            Line1 = "",                 // empty
//            City = " ",                 // whitespace
//            PostalCode = null,          // null
//            new_Status = true,
//            new_cmapreferred = true,
//            ParentId = new EntityReference(Contact.EntityLogicalName, Guid.NewGuid())
//        };

//        _context.Initialize(new List<Entity> { address });
        
//        // Should NOT throw exception
//        var ex = Record.Exception(() => _sut.HandleCreate(address));

//        Assert.Null(ex);
//    }
    
//    //************************************************HANDLE CMA PREFERRED TESTS*******************************************************//
//    //Null target → should do nothing
//    [Fact]
//    public void HandleUpdateCMAPreferred_WhenTargetNull_ShouldDoNothing()
//    {
//        _sut.HandleUpdateCMAPreferred(null, null);

//        // no exception = pass
//        Assert.True(true);
//    }
    
//    //Empty type -> should do nothing
//    [Fact]
//    public void HandleUpdateCMAPreferred_WhenTypeEmpty_ShouldDoNothing()
//    {
//        var addr = new CustomerAddress
//        {
//            new_AddressTypeForCMAPreferredAddress = null
//        };

//        _sut.HandleUpdateCMAPreferred(addr, addr);

//        Assert.True(true);
//    }
    
//    //Not preferred → should opt out
//    [Fact]
//    public void HandleUpdateCMAPreferred_WhenNotPreferred_ShouldOptOut()
//    {
//        var contactId = Guid.NewGuid();

//        var opt = new new_opt
//        {
//            Id = Guid.NewGuid(),
//            new_ContactID = new EntityReference("contact", contactId),
//            statuscode = new_opt_statuscode.OptIn,
//            new_AddressType = new_addresstype.Professional
//        };

//        var address = new CustomerAddress
//        {
//            Id = Guid.NewGuid(),
//            ParentId = new EntityReference("contact", contactId),
//            AddressTypeCode = customeraddress_addresstypecode.ShipTo,
//            new_AddressTypeForCMAPreferredAddress = CustomerAddressConstants.NotCMAPreferred
//        };

//        _context.Initialize(new List<Entity> { opt });

//        _sut.HandleUpdateCMAPreferred(address, address);

//        var updated = _context.CreateQuery<new_opt>().First();

//        Assert.Equal(new_opt_statuscode.OptOut, updated.statuscode);
//    }
    
//    //Preferred switch → should update new address
//    [Fact]
//    public void HandleUpdateCMAPreferred_WhenSwitch_ShouldSetNewPreferred()
//    {
//        var contactId = Guid.NewGuid();

//        var newAddr = new CustomerAddress
//        {
//            Id = Guid.NewGuid(),
//            ParentId = new EntityReference("contact", contactId),
//            AddressTypeCode = customeraddress_addresstypecode.ShipTo
//        };

//        var current = new CustomerAddress
//        {
//            Id = Guid.NewGuid(),
//            ParentId = new EntityReference("contact", contactId),
//            new_AddressTypeForCMAPreferredAddress = "2"
//        };

//        _context.Initialize(new List<Entity> { newAddr });

//        _sut.HandleUpdateCMAPreferred(current, current);

//        var updated = _context.CreateQuery<CustomerAddress>().First();

//        Assert.True(updated.new_cmapreferred == true);
//    }
    
//    //Preferred switch -> should update opt records
//    [Fact]
//    public void HandleUpdateCMAPreferred_ShouldUpdateOpts()
//    {
//        var contactId = Guid.NewGuid();

//        var opt = new new_opt
//        {
//            Id = Guid.NewGuid(),
//            new_ContactID = new EntityReference("contact", contactId),
//            statuscode = new_opt_statuscode.OptIn
//        };

//        var addr = new CustomerAddress
//        {
//            Id = Guid.NewGuid(),
//            ParentId = new EntityReference("contact", contactId),
//            new_AddressTypeForCMAPreferredAddress = "2"
//        };

//        _context.Initialize(new List<Entity> { opt, addr });

//        _sut.HandleUpdateCMAPreferred(addr, addr);

//        var updated = _context.CreateQuery<new_opt>().First();

//        Assert.NotNull(updated.new_AddressID);
//    }
    
//    //Should clear CMA preferred trigger field
//    [Fact]
//    public void HandleUpdateCMAPreferred_ShouldClearTriggerField()
//    {
//        var addr = new CustomerAddress
//        {
//            Id = Guid.NewGuid(),
//            new_AddressTypeForCMAPreferredAddress = "2"
//        };

//        _context.Initialize(new List<Entity> { addr });

//        _sut.HandleUpdateCMAPreferred(addr, addr);

//        var updated = _context.CreateQuery<CustomerAddress>().First();

//        Assert.Null(updated.new_AddressTypeForCMAPreferredAddress);
//    }
//}