using Confeccao.Api.Common.Pipeline;
using Confeccao.Api.Infrastructure;
using Confeccao.Domain.Entities;
using Confeccao.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Confeccao.UnitTests;

public class PipelineFlowServiceTests
{
    [Fact]
    public async Task GetFirstStage_returns_first_stage_in_sequence()
    {
        await using var db = NewDb();
        var modelId = await SeedModel(db,
            StageCode.Cutting, StageCode.Sewing, StageCode.Pressing);

        var svc = new PipelineFlowService(db);
        var first = await svc.GetFirstStageAsync(modelId);

        Assert.Equal(StageCode.Cutting, first);
    }

    [Fact]
    public async Task GetNextStage_returns_following_stage()
    {
        await using var db = NewDb();
        var modelId = await SeedModel(db,
            StageCode.Cutting, StageCode.Sewing, StageCode.Washing, StageCode.Pressing);

        var svc = new PipelineFlowService(db);

        Assert.Equal(StageCode.Sewing, await svc.GetNextStageAsync(modelId, StageCode.Cutting));
        Assert.Equal(StageCode.Washing, await svc.GetNextStageAsync(modelId, StageCode.Sewing));
        Assert.Equal(StageCode.Pressing, await svc.GetNextStageAsync(modelId, StageCode.Washing));
    }

    [Fact]
    public async Task GetNextStage_returns_null_for_final_stage()
    {
        await using var db = NewDb();
        var modelId = await SeedModel(db, StageCode.Cutting, StageCode.Sewing, StageCode.Pressing);

        var svc = new PipelineFlowService(db);
        var next = await svc.GetNextStageAsync(modelId, StageCode.Pressing);

        Assert.Null(next);
    }

    [Fact]
    public async Task GetNextStage_throws_when_stage_not_in_flow()
    {
        await using var db = NewDb();
        var modelId = await SeedModel(db, StageCode.Cutting, StageCode.Sewing);

        var svc = new PipelineFlowService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.GetNextStageAsync(modelId, StageCode.Washing));
    }

    [Fact]
    public async Task Flow_with_interfacing_inserted_advances_correctly()
    {
        await using var db = NewDb();
        // Vestido chemise's flow: Cutting → Interfacing → Sewing → ...
        var modelId = await SeedModel(db,
            StageCode.Cutting, StageCode.Interfacing, StageCode.Sewing,
            StageCode.Washing, StageCode.Buttoning, StageCode.Labeling, StageCode.Pressing);

        var svc = new PipelineFlowService(db);

        Assert.Equal(StageCode.Interfacing, await svc.GetNextStageAsync(modelId, StageCode.Cutting));
        Assert.Equal(StageCode.Sewing, await svc.GetNextStageAsync(modelId, StageCode.Interfacing));
    }

    [Fact]
    public async Task Colete_alfaiataria_flow_skips_washing_and_buttoning()
    {
        await using var db = NewDb();
        // Colete alfaiataria: Cutting → Sewing → Labeling → Pressing.
        var modelId = await SeedModel(db,
            StageCode.Cutting, StageCode.Sewing, StageCode.Labeling, StageCode.Pressing);

        var svc = new PipelineFlowService(db);

        Assert.Equal(StageCode.Labeling, await svc.GetNextStageAsync(modelId, StageCode.Sewing));
        Assert.Equal(StageCode.Pressing, await svc.GetNextStageAsync(modelId, StageCode.Labeling));
        Assert.Null(await svc.GetNextStageAsync(modelId, StageCode.Pressing));
    }

    private static ConfeccaoDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<ConfeccaoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ConfeccaoDbContext(options);
    }

    private static async Task<Guid> SeedModel(ConfeccaoDbContext db, params StageCode[] stages)
    {
        var modelId = Guid.NewGuid();
        db.Models.Add(new Model { Id = modelId, Name = $"Test Model {modelId:N}" });
        for (var i = 0; i < stages.Length; i++)
        {
            db.ModelStages.Add(new ModelStage
            {
                Id = Guid.NewGuid(),
                ModelId = modelId,
                Stage = stages[i],
                Sequence = i,
            });
        }
        await db.SaveChangesAsync();
        return modelId;
    }
}
