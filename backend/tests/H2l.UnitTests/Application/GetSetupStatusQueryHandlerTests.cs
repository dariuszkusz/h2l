using H2l.Application.Setup.Mappings;
using H2l.Application.Setup.Queries;
using H2l.Domain.Identity;
using Mapster;
using MapsterMapper;
using NSubstitute;
using Xunit;

namespace H2l.UnitTests.Application;

public sealed class GetSetupStatusQueryHandlerTests
{
    private static IMapper CreateMapper()
    {
        var config = new TypeAdapterConfig();
        new SetupMappingConfig().Register(config);
        return new Mapper(config);
    }

    [Fact]
    public async Task Handle_WhenNoSettingsExist_ReturnsNotInitializedWithEmptyInstanceId()
    {
        var settingsRepo = Substitute.For<IInstanceSettingsRepository>();
        var userRepo = Substitute.For<IUserRepository>();
        settingsRepo.GetPrimaryAsync(Arg.Any<CancellationToken>()).Returns((InstanceSettings?)null);
        userRepo.AnyAdminExistsAsync(Arg.Any<CancellationToken>()).Returns(false);

        var handler = new GetSetupStatusQueryHandler(settingsRepo, userRepo, CreateMapper());

        var result = await handler.Handle(new GetSetupStatusQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsInitialized);
        Assert.Equal(Guid.Empty, result.Value.InstanceId);
    }

    [Fact]
    public async Task Handle_WhenSettingsExistAndNoAdmin_ReturnsNotInitializedWithInstanceId()
    {
        var settings = InstanceSettings.Create(Guid.CreateVersion7()).Value;
        var settingsRepo = Substitute.For<IInstanceSettingsRepository>();
        var userRepo = Substitute.For<IUserRepository>();
        settingsRepo.GetPrimaryAsync(Arg.Any<CancellationToken>()).Returns(settings);
        userRepo.AnyAdminExistsAsync(Arg.Any<CancellationToken>()).Returns(false);

        var handler = new GetSetupStatusQueryHandler(settingsRepo, userRepo, CreateMapper());

        var result = await handler.Handle(new GetSetupStatusQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsInitialized);
        Assert.Equal(settings.InstanceId, result.Value.InstanceId);
    }

    [Fact]
    public async Task Handle_WhenSettingsAndAdminExist_ReturnsInitializedWithInstanceId()
    {
        var settings = InstanceSettings.Create(Guid.CreateVersion7()).Value;
        var settingsRepo = Substitute.For<IInstanceSettingsRepository>();
        var userRepo = Substitute.For<IUserRepository>();
        settingsRepo.GetPrimaryAsync(Arg.Any<CancellationToken>()).Returns(settings);
        userRepo.AnyAdminExistsAsync(Arg.Any<CancellationToken>()).Returns(true);

        var handler = new GetSetupStatusQueryHandler(settingsRepo, userRepo, CreateMapper());

        var result = await handler.Handle(new GetSetupStatusQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsInitialized);
        Assert.Equal(settings.InstanceId, result.Value.InstanceId);
    }
}
