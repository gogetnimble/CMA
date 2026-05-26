using System.Text.Json.Serialization;

namespace Cma.Services.Crm.Models;

public record Category(Guid CategoryId, CategoryName CategoryName);

public record CategoryName([property:JsonPropertyName("en")]string English, [property:JsonPropertyName("fr")]string French);