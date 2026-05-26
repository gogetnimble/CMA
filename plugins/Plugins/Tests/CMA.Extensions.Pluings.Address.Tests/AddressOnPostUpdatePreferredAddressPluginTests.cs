namespace CMA.Extensions.Plugins.Address;

public class AddressOnPostUpdatePreferredAddressPluginTests: PluginBaseTest
{
    
    private readonly XrmFakedContext _context;

    public AddressOnPostUpdatePreferredAddressPluginTests()
    {
        _context = Contecxt;
    }
    
    [Fact]
    public void Should_Exit_When_Target_Is_Invalid()
    {
        
        var pluginCtx = _context.GetDefaultPluginContext();
        pluginCtx.MessageName = "Update";

        // No target
        var ex = Record.Exception(() =>
            _context.ExecutePluginWith<AddressOnPostUpdatePreferredAddressPlugin>(pluginCtx)
        );

        Assert.Null(ex);
    }
    
    [Fact]
    public void Should_Exit_When_PostImage_Is_Missing()
    {
        var target = new CustomerAddress { Id = Guid.NewGuid() };

        var pluginCtx = _context.GetDefaultPluginContext();
        pluginCtx.MessageName = "Update";
        pluginCtx.InputParameters["Target"] = target.ToEntity<Entity>();

        // No PostImage

        var ex = Record.Exception(() =>
            _context.ExecutePluginWith<AddressOnPostUpdatePreferredAddressPlugin>(pluginCtx)
        );

        Assert.Null(ex);
    }
    
    [Fact]
    public void Should_Exit_When_Trigger_Field_Is_Empty()
    {
        _context.ProxyTypesAssembly = typeof(CustomerAddress).Assembly;

        var target = new CustomerAddress { Id = Guid.NewGuid() };

        var postImage = new CustomerAddress
        {
            Id = target.Id,
            new_AddressTypeForCMAPreferredAddress = null
        };

        var pluginCtx = _context.GetDefaultPluginContext();
        pluginCtx.MessageName = "Update";
        pluginCtx.InputParameters["Target"] = target.ToEntity<Entity>();
        pluginCtx.PostEntityImages["PostImage"] = postImage.ToEntity<Entity>();

        var ex = Record.Exception(() =>
            _context.ExecutePluginWith<AddressOnPostUpdatePreferredAddressPlugin>(pluginCtx)
        );

        Assert.Null(ex);
    }
    
    [Fact]
    public void Should_Handle_NotPreferred_Flow()
    {
        _context.ProxyTypesAssembly = typeof(CustomerAddress).Assembly;

        var contactId = Guid.NewGuid();

        var target = new CustomerAddress { Id = Guid.NewGuid() };

        var postImage = new CustomerAddress
        {
            Id = target.Id,
            ParentId = new EntityReference(Contact.EntityLogicalName, contactId),
            AddressTypeCode = customeraddress_addresstypecode.ShipTo,
            new_AddressTypeForCMAPreferredAddress = "NOT_CMA"
        };

        _context.Initialize(new List<Entity> { postImage });

        var pluginCtx = _context.GetDefaultPluginContext();
        pluginCtx.MessageName = "Update";
        pluginCtx.InputParameters["Target"] = target.ToEntity<Entity>();
        pluginCtx.PostEntityImages["PostImage"] = postImage.ToEntity<Entity>();

        var ex = Record.Exception(() =>
            _context.ExecutePluginWith<AddressOnPostUpdatePreferredAddressPlugin>(pluginCtx)
        );

        Assert.Null(ex);
    }
    
    [Fact]
    public void Should_Handle_Preferred_Switch_Flow()
    {
        _context.ProxyTypesAssembly = typeof(CustomerAddress).Assembly;

        var contactId = Guid.NewGuid();

        var target = new CustomerAddress { Id = Guid.NewGuid() };

        var postImage = new CustomerAddress
        {
            Id = target.Id,
            ParentId = new EntityReference(Contact.EntityLogicalName, contactId),
            new_AddressTypeForCMAPreferredAddress = "ShipTo"
        };

        _context.Initialize(new List<Entity> { postImage });

        var pluginCtx = _context.GetDefaultPluginContext();
        pluginCtx.MessageName = "Update";
        pluginCtx.InputParameters["Target"] = target.ToEntity<Entity>();
        pluginCtx.PostEntityImages["PostImage"] = postImage.ToEntity<Entity>();

        var ex = Record.Exception(() =>
            _context.ExecutePluginWith<AddressOnPostUpdatePreferredAddressPlugin>(pluginCtx)
        );

        Assert.Null(ex);
    }
    
    [Fact]
    public void Should_Clear_Trigger_Field_After_Execution()
    {
        _context.ProxyTypesAssembly = typeof(CustomerAddress).Assembly;

        var contactId = Guid.NewGuid();
        var addressId = Guid.NewGuid();

        var target = new CustomerAddress { Id = addressId };

        var postImage = new CustomerAddress
        {
            Id = addressId,
            ParentId = new EntityReference(Contact.EntityLogicalName, contactId),
            new_AddressTypeForCMAPreferredAddress = "ShipTo"
        };

        _context.Initialize(new List<Entity> { postImage });

        var pluginCtx = _context.GetDefaultPluginContext();
        pluginCtx.MessageName = "Update";
        pluginCtx.InputParameters["Target"] = target.ToEntity<Entity>();
        pluginCtx.PostEntityImages["PostImage"] = postImage.ToEntity<Entity>();

        _context.ExecutePluginWith<AddressOnPostUpdatePreferredAddressPlugin>(pluginCtx);

        var updated = _context.CreateQuery<CustomerAddress>()
            .FirstOrDefault(a => a.Id == addressId);

        Assert.Null(updated.new_AddressTypeForCMAPreferredAddress);
    }
    
    [Fact]
    public void Should_Throw_InvalidPluginExecutionException_On_Error()
    {
        _context.ProxyTypesAssembly = typeof(CustomerAddress).Assembly;

        var target = new CustomerAddress { Id = Guid.NewGuid() };

        var postImage = new CustomerAddress
        {
            Id = target.Id
            // Missing ParentId → will break service logic
        };

        var pluginCtx = _context.GetDefaultPluginContext();
        pluginCtx.MessageName = "Update";
        pluginCtx.InputParameters["Target"] = target.ToEntity<Entity>();
        pluginCtx.PostEntityImages["PostImage"] = postImage.ToEntity<Entity>();

        Assert.Throws<InvalidPluginExecutionException>(() =>
            _context.ExecutePluginWith<AddressOnPostUpdatePreferredAddressPlugin>(pluginCtx)
        );
    }
    
}