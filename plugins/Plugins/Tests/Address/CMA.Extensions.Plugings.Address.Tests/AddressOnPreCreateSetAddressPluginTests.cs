//using System;
//using System.Collections.Generic;
//using Microsoft.Xrm.Sdk;
//using Xunit;
//using CMA.Extensions.Model;
//using FakeXrmEasy;

//namespace CMA.Extensions.Plugins.Address;

//public class AddressOnPreCreateSetAddressPluginTests
////{
    
//    private readonly XrmFakedContext _context;
    
//    public AddressOnPreCreateSetAddressPluginTests()
//    {
//        _context = new XrmFakedContext();
//    }
    
//    [Fact]
//    public void AddressOnPostCreate_Should_Execute_Successfully()
//    {
//        _context.ProxyTypesAssembly = typeof(CustomerAddress).Assembly;

//        var contactId = Guid.NewGuid();

//        var address = new CustomerAddress
//        {
//            Id = Guid.NewGuid(),
//            ParentId = new EntityReference(Contact.EntityLogicalName, contactId),
//            new_Status = true,
//            new_cmapreferred = true
//        };

//        _context.Initialize(new List<Entity> { address });

//        var target = address.ToEntity<Entity>();

//        var pluginCtx = _context.GetDefaultPluginContext();
//        pluginCtx.MessageName = "Create";
//        pluginCtx.InputParameters["Target"] = target;

//        // Should not throw
//        var ex = Record.Exception(() =>
//            _context.ExecutePluginWith<AddressOnPreCreateSetAddressPlugin>(pluginCtx)
//        );

//        Assert.Null(ex);
//    }
    
//    [Fact]
//    public void Should_Exit_When_Target_Is_Null()
//    {
//        var pluginCtx = _context.GetDefaultPluginContext();
//        pluginCtx.MessageName = "Create";
//        // No Target

//        var ex = Record.Exception(() =>
//            _context.ExecutePluginWith<AddressOnPreCreateSetAddressPlugin>(pluginCtx)
//        );

//        Assert.Null(ex);
//    }
    
//    [Fact]
//    public void Should_Exit_When_Entity_Is_Not_CustomerAddress()
//    {
//        var account = new Account { Id = Guid.NewGuid() };

//        var pluginCtx = _context.GetDefaultPluginContext();
//        pluginCtx.MessageName = "Create";
//        pluginCtx.InputParameters["Target"] = account.ToEntity<Entity>();

//        var ex = Record.Exception(() =>
//            _context.ExecutePluginWith<AddressOnPreCreateSetAddressPlugin>(pluginCtx)
//        );

//        Assert.Null(ex);
//    }
    
//    [Fact]
//    public void Should_Throw_InvalidPluginExecutionException_On_Error()
//    {
//        _context.ProxyTypesAssembly = typeof(CustomerAddress).Assembly;

//        // Force failure: missing ParentId (your service depends on it)
//        var address = new CustomerAddress
//        {
//            Id = Guid.NewGuid(),
//            new_Status = true,
//            new_cmapreferred = true
//        };

//        var pluginCtx = _context.GetDefaultPluginContext();
//        pluginCtx.MessageName = "Create";
//        pluginCtx.InputParameters["Target"] = address.ToEntity<Entity>();

//        Assert.Throws<InvalidPluginExecutionException>(() =>
//            _context.ExecutePluginWith<AddressOnPreCreateSetAddressPlugin>(pluginCtx)
//        );
//    }
    
//    [Fact]
//    public void Should_Process_Address_And_Update_Data()
//    {
//        _context.ProxyTypesAssembly = typeof(CustomerAddress).Assembly;

//        var contactId = Guid.NewGuid();

//        var contact = new Contact
//        {
//            Id = contactId
//        };

//        var address = new CustomerAddress
//        {
//            Id = Guid.NewGuid(),
//            ParentId = new EntityReference(Contact.EntityLogicalName, contactId),
//            Line1 = "Test Line",
//            City = "Ottawa",
//            new_Status = true,
//            new_cmapreferred = true
//        };

//        _context.Initialize(new List<Entity> { contact, address });

//        var pluginCtx = _context.GetDefaultPluginContext();
//        pluginCtx.MessageName = "Create";
//        pluginCtx.InputParameters["Target"] = address.ToEntity<Entity>();

//        _context.ExecutePluginWith<AddressOnPreCreateSetAddressPlugin>(pluginCtx);

//        var updatedContact = _context.CreateQuery<Contact>().FirstOrDefault();

//        Assert.NotNull(updatedContact);
//    }
    
//}