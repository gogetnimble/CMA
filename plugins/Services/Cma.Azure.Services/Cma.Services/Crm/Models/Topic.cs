using System.Text.Json.Serialization;

namespace Cma.Services.Crm.Models;

public record Topic(Guid TopicId, TopicName TopicName, string Code, TopicDescription TopicDescription, Guid CategoryId, int DisplayOrder);

public record TopicName([property:JsonPropertyName("en")]string English, [property:JsonPropertyName("fr")]string French);
public record TopicDescription([property:JsonPropertyName("en")]string English, [property:JsonPropertyName("fr")]string French);