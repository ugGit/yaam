using FluentAssertions;
using MediatR;
using NSubstitute;
using Yaam.Application.Applications.Dtos;
using Yaam.Application.Applications.Queries;
using Yaam.Domain.Enums;
using Yaam.Domain.Repositories;
using DomainApp = Yaam.Domain.Entities.Application;

namespace Yaam.Tests.Unit.Applications;

public class GetApplicationsQueryTests
{
    private readonly IApplicationRepository _repo = Substitute.For<IApplicationRepository>();

    [Fact]
    public async Task Handle_ReturnsAllApplicationsAsSummaryDtos()
    {
        var applications = new List<DomainApp>
        {
            new() { CompanyName = "Acme", Role = "Dev", Status = ApplicationStatus.Applied },
            new() { CompanyName = "Beta", Role = "QA",  Status = ApplicationStatus.Draft  },
        };
        _repo.GetAllAsync(null, "dateApplied", "desc", Arg.Any<CancellationToken>())
            .Returns(applications);

        var handler = new GetApplicationsQueryHandler(_repo);
        var result = await handler.Handle(
            new GetApplicationsQuery(null, "dateApplied", "desc"), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].CompanyName.Should().Be("Acme");
    }

    [Fact]
    public async Task Handle_PassesFilterAndSortToRepository()
    {
        _repo.GetAllAsync(ApplicationStatus.Applied, "companyName", "asc", Arg.Any<CancellationToken>())
            .Returns(new List<DomainApp>());

        var handler = new GetApplicationsQueryHandler(_repo);
        await handler.Handle(
            new GetApplicationsQuery(ApplicationStatus.Applied, "companyName", "asc"),
            CancellationToken.None);

        await _repo.Received(1)
            .GetAllAsync(ApplicationStatus.Applied, "companyName", "asc", Arg.Any<CancellationToken>());
    }
}
