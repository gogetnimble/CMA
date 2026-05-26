using Cma.Services.Crm.Models;

namespace Cma.Services.Crm.Contracts;

public record GetAllTopicsResponse(List<Topic> Topics, List<Category> Categories);