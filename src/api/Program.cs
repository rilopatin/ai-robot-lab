using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OpportunityOs.Api.Contracts;
using OpportunityOs.Api.Data;
using OpportunityOs.Api.Domain;

var builder = WebApplication.CreateBuilder(args);

var configuredConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=App_Data/opportunity-os.db";
var connectionStringBuilder = new SqliteConnectionStringBuilder(configuredConnectionString);

if (!Path.IsPathRooted(connectionStringBuilder.DataSource))
{
    connectionStringBuilder.DataSource = Path.Combine(
        builder.Environment.ContentRootPath,
        connectionStringBuilder.DataSource);
}

var databaseDirectory = Path.GetDirectoryName(connectionStringBuilder.DataSource);
if (!string.IsNullOrEmpty(databaseDirectory))
{
    Directory.CreateDirectory(databaseDirectory);
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionStringBuilder.ConnectionString));
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await database.Database.MigrateAsync();
}

app.MapGet("/api/goals", async (AppDbContext database) =>
    Results.Ok(await database.Goals
        .AsNoTracking()
        .OrderByDescending(goal => goal.UpdatedAtUtc)
        .Select(goal => new
        {
            goal.Id,
            goal.Objective,
            goal.Status,
            goal.CreatedAtUtc,
            goal.UpdatedAtUtc,
            OpportunityCount = goal.Opportunities.Count
        })
        .ToListAsync()));

app.MapPost("/api/goals", async (CreateGoalRequest request, AppDbContext database) =>
{
    if (IsBlank(request.Objective) || IsBlank(request.Constraints) || IsBlank(request.SuccessCriteria))
    {
        return Results.BadRequest(new
        {
            error = "Objective, constraints, and success criteria are required."
        });
    }

    var now = DateTime.UtcNow;
    var goal = new Goal
    {
        Id = Guid.NewGuid(),
        Objective = request.Objective!.Trim(),
        Constraints = request.Constraints!.Trim(),
        SuccessCriteria = request.SuccessCriteria!.Trim(),
        Status = GoalStatus.Active,
        CreatedAtUtc = now,
        UpdatedAtUtc = now
    };

    database.Goals.Add(goal);
    await database.SaveChangesAsync();

    return Results.Created($"/api/goals/{goal.Id}", goal);
});

app.MapGet("/api/goals/{goalId:guid}", async (Guid goalId, AppDbContext database) =>
{
    var goal = await database.Goals
        .AsNoTracking()
        .Include(item => item.Opportunities.OrderBy(opportunity => opportunity.CreatedAtUtc))
            .ThenInclude(opportunity => opportunity.Experiments.OrderBy(experiment => experiment.CreatedAtUtc))
                .ThenInclude(experiment => experiment.Evidence.OrderBy(evidence => evidence.RecordedAtUtc))
        .Include(item => item.Opportunities)
            .ThenInclude(opportunity => opportunity.Experiments)
                .ThenInclude(experiment => experiment.Decision)
        .AsSplitQuery()
        .SingleOrDefaultAsync(item => item.Id == goalId);

    return goal is null
        ? Results.NotFound(new { error = "Goal not found." })
        : Results.Ok(goal);
});

app.MapPost("/api/goals/{goalId:guid}/opportunities", async (
    Guid goalId,
    CreateOpportunityRequest request,
    AppDbContext database) =>
{
    var goal = await database.Goals.FindAsync(goalId);
    if (goal is null)
    {
        return Results.NotFound(new { error = "Goal not found." });
    }

    if (IsBlank(request.Title)
        || IsBlank(request.Source)
        || IsBlank(request.WhyNow)
        || IsBlank(request.MoneyMechanism)
        || IsBlank(request.Geography)
        || IsBlank(request.PayoutRules))
    {
        return Results.BadRequest(new
        {
            error = "Title, source, why now, money mechanism, geography, and payout rules are required."
        });
    }

    if (request.ProbabilityToFirstDollar is < 0 or > 100)
    {
        return Results.BadRequest(new { error = "Probability to first dollar must be between 0 and 100." });
    }

    var opportunity = new Opportunity
    {
        Id = Guid.NewGuid(),
        GoalId = goalId,
        Title = request.Title!.Trim(),
        Source = request.Source!.Trim(),
        WhyNow = request.WhyNow!.Trim(),
        MoneyMechanism = request.MoneyMechanism!.Trim(),
        Competition = Clean(request.Competition),
        Geography = request.Geography!.Trim(),
        PayoutRules = request.PayoutRules!.Trim(),
        AiRestrictions = Clean(request.AiRestrictions),
        Friction = Clean(request.Friction),
        ProbabilityToFirstDollar = request.ProbabilityToFirstDollar,
        ReusableValue = Clean(request.ReusableValue),
        CreatedAtUtc = DateTime.UtcNow
    };

    goal.UpdatedAtUtc = DateTime.UtcNow;
    database.Opportunities.Add(opportunity);
    await database.SaveChangesAsync();

    return Results.Created($"/api/goals/{goalId}", opportunity);
});

app.MapPost("/api/opportunities/{opportunityId:guid}/experiments", async (
    Guid opportunityId,
    CreateExperimentRequest request,
    AppDbContext database) =>
{
    var opportunity = await database.Opportunities.FindAsync(opportunityId);
    if (opportunity is null)
    {
        return Results.NotFound(new { error = "Opportunity not found." });
    }

    if (IsBlank(request.Hypothesis)
        || IsBlank(request.Action)
        || IsBlank(request.SuccessCondition)
        || IsBlank(request.StopCondition))
    {
        return Results.BadRequest(new
        {
            error = "Hypothesis, action, success condition, and stop condition are required."
        });
    }

    if (request.TimeBudgetMinutes < 0)
    {
        return Results.BadRequest(new { error = "Time budget cannot be negative." });
    }

    if (request.BudgetCents != 0)
    {
        return Results.BadRequest(new { error = "Experiment budget must be zero in v0.2A." });
    }

    var now = DateTime.UtcNow;
    var experiment = new Experiment
    {
        Id = Guid.NewGuid(),
        OpportunityId = opportunityId,
        Hypothesis = request.Hypothesis!.Trim(),
        Action = request.Action!.Trim(),
        TimeBudgetMinutes = request.TimeBudgetMinutes,
        BudgetCents = 0,
        SuccessCondition = request.SuccessCondition!.Trim(),
        StopCondition = request.StopCondition!.Trim(),
        Status = ExperimentStatus.Planned,
        Result = string.Empty,
        CreatedAtUtc = now,
        UpdatedAtUtc = now
    };

    database.Experiments.Add(experiment);
    await TouchGoalAsync(opportunity.GoalId, database);
    await database.SaveChangesAsync();

    return Results.Created($"/api/goals/{opportunity.GoalId}", experiment);
});

app.MapPut("/api/experiments/{experimentId:guid}/result", async (
    Guid experimentId,
    UpdateExperimentResultRequest request,
    AppDbContext database) =>
{
    var experiment = await database.Experiments
        .Include(item => item.Opportunity)
        .SingleOrDefaultAsync(item => item.Id == experimentId);
    if (experiment is null)
    {
        return Results.NotFound(new { error = "Experiment not found." });
    }

    if (!Enum.IsDefined(request.Status))
    {
        return Results.BadRequest(new { error = "Experiment status is invalid." });
    }

    experiment.Status = request.Status;
    experiment.Result = Clean(request.Result);
    experiment.UpdatedAtUtc = DateTime.UtcNow;
    await TouchGoalAsync(experiment.Opportunity!.GoalId, database);
    await database.SaveChangesAsync();

    return Results.Ok(experiment);
});

app.MapPost("/api/experiments/{experimentId:guid}/evidence", async (
    Guid experimentId,
    AddEvidenceRequest request,
    AppDbContext database) =>
{
    var experiment = await database.Experiments
        .Include(item => item.Opportunity)
        .SingleOrDefaultAsync(item => item.Id == experimentId);
    if (experiment is null)
    {
        return Results.NotFound(new { error = "Experiment not found." });
    }

    if (HasNegativeMetric(request))
    {
        return Results.BadRequest(new { error = "Evidence counts and revenue cannot be negative." });
    }

    var evidence = new Evidence
    {
        Id = Guid.NewGuid(),
        ExperimentId = experimentId,
        RecordedAtUtc = DateTime.UtcNow,
        Views = request.Views,
        Installs = request.Installs,
        Downloads = request.Downloads,
        Users = request.Users,
        Clicks = request.Clicks,
        Sales = request.Sales,
        RevenueCents = request.RevenueCents,
        RejectionOrWaitlist = Clean(request.RejectionOrWaitlist),
        PlatformFriction = Clean(request.PlatformFriction),
        Feedback = Clean(request.Feedback)
    };

    database.Evidence.Add(evidence);
    experiment.UpdatedAtUtc = DateTime.UtcNow;
    await TouchGoalAsync(experiment.Opportunity!.GoalId, database);
    await database.SaveChangesAsync();

    return Results.Created($"/api/goals/{experiment.Opportunity.GoalId}", evidence);
});

app.MapPut("/api/experiments/{experimentId:guid}/decision", async (
    Guid experimentId,
    PutDecisionRequest request,
    AppDbContext database) =>
{
    var experiment = await database.Experiments
        .Include(item => item.Opportunity)
        .Include(item => item.Decision)
        .SingleOrDefaultAsync(item => item.Id == experimentId);
    if (experiment is null)
    {
        return Results.NotFound(new { error = "Experiment not found." });
    }

    if (!Enum.IsDefined(request.Outcome) || IsBlank(request.Reason))
    {
        return Results.BadRequest(new
        {
            error = "A valid outcome and a reason are required."
        });
    }

    var now = DateTime.UtcNow;
    if (experiment.Decision is null)
    {
        var decision = new Decision
        {
            Id = Guid.NewGuid(),
            ExperimentId = experimentId,
            Outcome = request.Outcome,
            Reason = request.Reason!.Trim(),
            DecidedAtUtc = now
        };
        database.Decisions.Add(decision);
        experiment.Decision = decision;
    }
    else
    {
        experiment.Decision.Outcome = request.Outcome;
        experiment.Decision.Reason = request.Reason!.Trim();
        experiment.Decision.DecidedAtUtc = now;
    }

    experiment.UpdatedAtUtc = now;
    await TouchGoalAsync(experiment.Opportunity!.GoalId, database);
    await database.SaveChangesAsync();

    return Results.Ok(experiment.Decision);
});

app.Run();

static bool IsBlank(string? value) => string.IsNullOrWhiteSpace(value);

static string Clean(string? value) => value?.Trim() ?? string.Empty;

static bool HasNegativeMetric(AddEvidenceRequest request) =>
    request.Views < 0
    || request.Installs < 0
    || request.Downloads < 0
    || request.Users < 0
    || request.Clicks < 0
    || request.Sales < 0
    || request.RevenueCents < 0;

static async Task TouchGoalAsync(Guid goalId, AppDbContext database)
{
    var goal = await database.Goals.FindAsync(goalId);
    if (goal is not null)
    {
        goal.UpdatedAtUtc = DateTime.UtcNow;
    }
}

public partial class Program;
