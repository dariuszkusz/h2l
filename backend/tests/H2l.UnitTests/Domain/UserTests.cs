using H2l.Domain.Common;
using H2l.Domain.Identity;
using Xunit;

namespace H2l.UnitTests.Domain;

public sealed class UserTests
{
    [Fact]
    public void Create_WithValidData_ReturnsUserWithUuidV7AndNormalizedEmail()
    {
        var tenantId = Guid.CreateVersion7();

        var result = User.Create(
            tenantId,
            "  Admin@Example.COM ",
            "hashed-password-value",
            SystemRole.Admin,
            "DE",
            "high-contrast");

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value.Id.Version);
        Assert.Equal("admin@example.com", result.Value.Email);
        Assert.Equal(SystemRole.Admin, result.Value.SystemRole);
        Assert.Equal("de", result.Value.PreferredLanguage);
        Assert.Equal("high-contrast", result.Value.PreferredTheme);
        Assert.True(result.Value.IsActive);
        Assert.NotNull(result.Value.PasswordHash);
        Assert.Null(result.Value.DeletedAt);
    }

    [Fact]
    public void CreateInvited_WithValidData_ReturnsInactiveUserWithNullPasswordHash()
    {
        var tenantId = Guid.CreateVersion7();

        var result = User.CreateInvited(
            tenantId,
            "  Invited@Example.COM ",
            SystemRole.ProcessDesigner,
            "PL",
            "dark",
            "ext-123");

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsActive);
        Assert.Null(result.Value.PasswordHash);
        Assert.Equal("invited@example.com", result.Value.Email);
        Assert.Equal("ext-123", result.Value.ExternalId);
        Assert.Equal(SystemRole.ProcessDesigner, result.Value.SystemRole);
    }

    [Fact]
    public void Activate_InvitedUserWithPasswordHash_SetsActiveAndPasswordHash()
    {
        var user = User.CreateInvited(Guid.CreateVersion7(), "user@example.com", SystemRole.EndUser).Value;

        var result = user.Activate("new-hashed-password");

        Assert.True(result.IsSuccess);
        Assert.True(user.IsActive);
        Assert.Equal("new-hashed-password", user.PasswordHash);
    }

    [Fact]
    public void Activate_AlreadyActiveUser_ReturnsConflictFailure()
    {
        var user = User.Create(Guid.CreateVersion7(), "user@example.com", "hash", SystemRole.EndUser).Value;

        var result = user.Activate("another-hash");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("USER_ALREADY_ACTIVE", result.Error.Code);
    }

    [Fact]
    public void Activate_DeletedUser_ReturnsValidationFailure()
    {
        var user = User.CreateInvited(Guid.CreateVersion7(), "user@example.com", SystemRole.EndUser).Value;
        user.SoftDelete();

        var result = user.Activate("new-hash");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("USER_DELETED", result.Error.Code);
    }

    [Fact]
    public void UpdatePreferences_WithInvalidTheme_ReturnsValidationFailure()
    {
        var user = User.Create(Guid.CreateVersion7(), "user@example.com", "hash", SystemRole.EndUser).Value;

        var result = user.UpdatePreferences("pl", "neon-pink");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("THEME_UNSUPPORTED", result.Error.Code);
    }

    [Fact]
    public void SoftDelete_SetsDeletedAtAndDeactivatesUser()
    {
        var user = User.Create(Guid.CreateVersion7(), "user@example.com", "hash", SystemRole.EndUser).Value;
        var deletedAt = DateTimeOffset.UtcNow;

        user.SoftDelete(deletedAt);

        Assert.False(user.IsActive);
        Assert.Equal(deletedAt, user.DeletedAt);
    }
}
