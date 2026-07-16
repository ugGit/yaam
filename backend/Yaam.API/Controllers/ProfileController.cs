using MediatR;
using Microsoft.AspNetCore.Mvc;
using Yaam.UseCases.Profile.Queries;

namespace Yaam.API.Controllers;

[ApiController]
[Route("api/profile")]
public class ProfileController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [EndpointName("GetProfile")]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetProfileQuery(),
            cancellationToken);
        return Ok(result);
    }
}
