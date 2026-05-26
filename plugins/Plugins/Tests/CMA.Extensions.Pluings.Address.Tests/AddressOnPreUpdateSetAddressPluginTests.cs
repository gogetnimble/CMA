namespace CMA.Extensions.Plugins.Address;

public class AddressOnPreUpdateSetAddressPluginTests: PluginBaseTest
{
    
    private readonly XrmFakedContext _context;

    public AddressOnPreUpdateSetAddressPluginTests()
    {
        _context = Contecxt;
    }
    
    [Fact]
    public void Should_Exit_When_PreImage_Is_Missing()
    {
        _context.ProxyTypesAssembly = typeof(CustomerAddress).Assembly;

        var address = new CustomerAddress
        {
            Id = Guid.NewGuid()
        };

        var pluginCtx = _context.GetDefaultPluginContext();
        pluginCtx.MessageName = "Update";
        pluginCtx.InputParameters["Target"] = address.ToEntity<Entity>();

        // No PreImage added

        var ex = Record.Exception(() =>
            _context.ExecutePluginWith<AddressOnPreUpdateSetAddressPlugin>(pluginCtx)
        );

        Assert.Null(ex); // Plugin should safely exit
    }
    
    [Fact]
    public void Should_Execute_When_PreImage_Is_Present()
    {
        _context.ProxyTypesAssembly = typeof(CustomerAddress).Assembly;

        var contactId = Guid.NewGuid();

        var address = new CustomerAddress
        {
            Id = Guid.NewGuid(),
            ParentId = new EntityReference(Contact.EntityLogicalName, contactId),
            new_Status = true,
            new_cmapreferred = true
        };

        var preImage = new CustomerAddress
        {
            Id = address.Id,
            ParentId = address.ParentId,
            new_Status = false,
            new_cmapreferred = false
        };

        _context.Initialize(new List<Entity> { address });

        var pluginCtx = _context.GetDefaultPluginContext();
        pluginCtx.MessageName = "Update";
        pluginCtx.InputParameters["Target"] = address.ToEntity<Entity>();

        // Add PreImage
        pluginCtx.PreEntityImages["PreImage"] = preImage.ToEntity<Entity>();

        var ex = Record.Exception(() =>
            _context.ExecutePluginWith<AddressOnPreUpdateSetAddressPlugin>(pluginCtx)
        );

        Assert.Null(ex);
    }
    
    [Fact]
    public void Should_Use_PreImage_For_Comparison()
    {
        _context.ProxyTypesAssembly = typeof(CustomerAddress).Assembly;

        var contactId = Guid.NewGuid();

        var address = new CustomerAddress
        {
            Id = Guid.NewGuid(),
            ParentId = new EntityReference(Contact.EntityLogicalName, contactId),
            new_Status = true,
            new_cmapreferred = true
        };

        var preImage = new CustomerAddress
        {
            Id = address.Id,
            ParentId = address.ParentId,
            new_Status = true,
            new_cmapreferred = true
        };

        _context.Initialize(new List<Entity> { address });

        var pluginCtx = _context.GetDefaultPluginContext();
        pluginCtx.MessageName = "Update";
        pluginCtx.InputParameters["Target"] = address.ToEntity<Entity>();
        pluginCtx.PreEntityImages["PreImage"] = preImage.ToEntity<Entity>();

        var ex = Record.Exception(() =>
            _context.ExecutePluginWith<AddressOnPreUpdateSetAddressPlugin>(pluginCtx)
        );

        Assert.Null(ex);
    }
    
    [Fact]
    public void Should_Throw_InvalidPluginExecutionException_On_Error()
    {
        _context.ProxyTypesAssembly = typeof(CustomerAddress).Assembly;

        // Force issue (missing ParentId)
        var address = new CustomerAddress
        {
            Id = Guid.NewGuid()
        };

        var preImage = new CustomerAddress
        {
            Id = address.Id
        };

        var pluginCtx = _context.GetDefaultPluginContext();
        pluginCtx.MessageName = "Update";
        pluginCtx.InputParameters["Target"] = address.ToEntity<Entity>();
        pluginCtx.PreEntityImages["PreImage"] = preImage.ToEntity<Entity>();

        Assert.Throws<InvalidPluginExecutionException>(() =>
            _context.ExecutePluginWith<AddressOnPreUpdateSetAddressPlugin>(pluginCtx)
        );
    }
}