using NSubstitute;
using Yaam.Domain.Common;
using Yaam.Domain.Entities;
using Yaam.Domain.Enums;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Applications.Queries;

namespace Yaam.Tests.Unit.Applications;

public class GetApplicationsQueryTests
{
    private readonly IApplicationRepository _repo = Substitute.For<IApplicationRepository>();

    [Fact]
    public async Task Handle_PassesFilterAndSortToRepository()
    {
        _repo.GetAllAsync(ApplicationStatus.Applied, ApplicationSortField.CompanyName, "asc", Arg.Any<CancellationToken>())
            .Returns(new List<Application>());

        var handler = new GetApplicationsQueryHandler(_repo);
        await handler.Handle(
            new GetApplicationsQuery(ApplicationStatus.Applied, ApplicationSortField.CompanyName, "asc"),
            CancellationToken.None);

        await _repo.Received(1)
            .GetAllAsync(ApplicationStatus.Applied, ApplicationSortField.CompanyName, "asc", Arg.Any<CancellationToken>());
    }
}
