using H2l.Domain.Common;
using H2l.Domain.Workflows;
using Xunit;

namespace H2l.UnitTests.Domain;

public sealed class FormDefinitionTests
{
    [Fact]
    public void UpdateDraftSchema_WhenNotDeleted_UpdatesSchemaAndTimestamp()
    {
        var form = FormDefinition.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Approval Form",
            "approve_step",
            "{\"fields\":[]}").Value;
        var updatedAt = DateTimeOffset.UtcNow.AddMinutes(5);

        var result = form.UpdateDraftSchema("{\"fields\":[{\"key\":\"amount\"}]}", updatedAt);

        Assert.True(result.IsSuccess);
        Assert.Equal("{\"fields\":[{\"key\":\"amount\"}]}", form.DraftSchemaJsonb);
        Assert.Equal(updatedAt, form.UpdatedAt);
    }

    [Fact]
    public void UpdateDraftSchema_WhenSoftDeleted_ReturnsValidationFailure()
    {
        var form = FormDefinition.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Approval Form",
            "approve_step").Value;
        form.SoftDelete();

        var result = form.UpdateDraftSchema("{}");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("FORM_DELETED", result.Error.Code);
    }
}
