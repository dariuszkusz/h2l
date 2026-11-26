using H2l.Domain.Common;
using H2l.Domain.Tasks;
using Xunit;

namespace H2l.UnitTests.Domain;

public sealed class HumanTaskDomainTests
{
    [Fact]
    public void ClaimUnclaimComplete_EnforcesGroupQueueInvariantsAndClearsResumeUrl()
    {
        var tenantId = Guid.CreateVersion7();
        var formVersionId = Guid.CreateVersion7();
        var userA = Guid.CreateVersion7();
        var userB = Guid.CreateVersion7();

        var task = HumanTask.Create(
            tenantId,
            formVersionId,
            "ext-task-1",
            encryptedResumeUrl: "v1:enc:resume-url").Value;

        Assert.True(task.CanClaim(userA));
        Assert.False(task.CanUnclaim(userA));

        var initialStamp = task.ConcurrencyStamp;
        var claimA = task.Claim(userA);
        Assert.True(claimA.IsSuccess);
        Assert.Equal(HumanTaskStatus.InProgress, task.Status);
        Assert.Equal(userA, task.ClaimedByUserId);
        Assert.NotEqual(initialStamp, task.ConcurrencyStamp);
        Assert.True(task.CanUnclaim(userA));
        Assert.False(task.CanUnclaim(userB));

        var claimB = task.Claim(userB);
        Assert.True(claimB.IsFailure);
        Assert.Equal("TASK_ALREADY_CLAIMED", claimB.Error.Code);

        var unclaimByOther = task.Unclaim(userB);
        Assert.True(unclaimByOther.IsFailure);
        Assert.Equal(ErrorType.Forbidden, unclaimByOther.Error.Type);

        var unclaimByOwner = task.Unclaim(userA);
        Assert.True(unclaimByOwner.IsSuccess);
        Assert.Equal(HumanTaskStatus.Pending, task.Status);
        Assert.Null(task.ClaimedByUserId);

        Assert.True(task.Claim(userB).IsSuccess);
        var complete = task.Complete(userB, DateTimeOffset.UtcNow);
        Assert.True(complete.IsSuccess);
        Assert.Equal(HumanTaskStatus.Completed, task.Status);
        Assert.Null(task.EncryptedResumeUrl);
        Assert.NotNull(task.CompletedAt);
    }

    [Fact]
    public void DirectlyAssignedTask_CannotBeClaimedOrUnclaimed()
    {
        var userA = Guid.CreateVersion7();
        var task = HumanTask.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "ext-direct-1",
            assignedUserId: userA).Value;

        Assert.False(task.CanClaim(userA));
        Assert.False(task.CanUnclaim(userA));
        Assert.Equal("TASK_DIRECTLY_ASSIGNED", task.Claim(userA).Error.Code);
        Assert.Equal("TASK_DIRECTLY_ASSIGNED", task.Unclaim(userA).Error.Code);
    }
}
