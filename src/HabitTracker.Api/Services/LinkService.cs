using HabitTracker.Api.DTOs.Common;

namespace HabitTracker.Api.Services;

public class LinkService(LinkGenerator linkGenerator, IHttpContextAccessor contextAccessor)
{
    public LinkDto Create(string endpointName, string rel, string method, object? values = null, string? controller = null)
    {
        string? href = linkGenerator.GetUriByAction(contextAccessor.HttpContext!, endpointName, controller, values);

        return new LinkDto
        {
            Href = href ?? throw new Exception("invalid endpoint name provided"),
            Rel = rel,
            Method = method,
        };
    }
}
