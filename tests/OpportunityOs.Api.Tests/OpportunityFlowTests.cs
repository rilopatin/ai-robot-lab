using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Xunit;

namespace OpportunityOs.Api.Tests;

public sealed class OpportunityFlowTests
{
    [Fact]
    public async Task Full_history_survives_application_restart()
    {
        var databasePath = NewDatabasePath();
        Guid goalId;

        try
        {
            await using (var firstApplication = new TestApplication(databasePath))
            {
                using var client = firstApplication.CreateClient();
                var flow = await CreateFlowThroughExperiment(client);
                goalId = flow.GoalId;

                await PostAndRequireSuccess(client, $"/api/experiments/{flow.ExperimentId}/evidence", new
                {
                    views = 10,
                    clicks = 2,
                    sales = 0,
                    revenueCents = 0,
                    rejectionOrWaitlist = "",
                    platformFriction = "Initial review",
                    feedback = "Some qualified traffic"
                });
                await Task.Delay(10);
                await PostAndRequireSuccess(client, $"/api/experiments/{flow.ExperimentId}/evidence", new
                {
                    views = 35,
                    clicks = 6,
                    sales = 1,
                    revenueCents = 1200,
                    rejectionOrWaitlist = "",
                    platformFriction = "",
                    feedback = "First sale"
                });

                await PutAndRequireSuccess(client, $"/api/experiments/{flow.ExperimentId}/result", new
                {
                    status = "Completed",
                    result = "One real sale"
                });
                await PutAndRequireSuccess(client, $"/api/experiments/{flow.ExperimentId}/decision", new
                {
                    outcome = "WATCH",
                    reason = "Promising, but collect another week of evidence."
                });
                await PutAndRequireSuccess(client, $"/api/experiments/{flow.ExperimentId}/decision", new
                {
                    outcome = "SCALE",
                    reason = "A real sale validated the mechanism."
                });
            }

            await using var restartedApplication = new TestApplication(databasePath);
            using var restartedClient = restartedApplication.CreateClient();
            using var history = await GetJson(restartedClient, $"/api/goals/{goalId}");

            var goal = history.RootElement;
            Assert.Equal("Earn the first $10–50 online.", goal.GetProperty("objective").GetString());
            var opportunity = Assert.Single(goal.GetProperty("opportunities").EnumerateArray());
            var experiment = Assert.Single(opportunity.GetProperty("experiments").EnumerateArray());
            Assert.Equal("Completed", experiment.GetProperty("status").GetString());
            Assert.Equal("One real sale", experiment.GetProperty("result").GetString());

            var evidence = experiment.GetProperty("evidence").EnumerateArray().ToArray();
            Assert.Equal(2, evidence.Length);
            Assert.Equal(10, evidence[0].GetProperty("views").GetInt64());
            Assert.Equal(35, evidence[1].GetProperty("views").GetInt64());
            Assert.Equal(1200, evidence[1].GetProperty("revenueCents").GetInt64());

            var decision = experiment.GetProperty("decision");
            Assert.Equal("SCALE", decision.GetProperty("outcome").GetString());
            Assert.Equal("A real sale validated the mechanism.", decision.GetProperty("reason").GetString());

            using var goalList = await GetJson(restartedClient, "/api/goals");
            Assert.Contains(goalList.RootElement.EnumerateArray(), item =>
                item.GetProperty("id").GetGuid() == goalId
                && item.GetProperty("opportunityCount").GetInt32() == 1);
        }
        finally
        {
            DeleteDatabaseFiles(databasePath);
        }
    }

    [Fact]
    public async Task Experiment_budget_must_be_zero()
    {
        var databasePath = NewDatabasePath();

        try
        {
            await using var application = new TestApplication(databasePath);
            using var client = application.CreateClient();
            var goalId = await CreateGoal(client);
            var opportunityId = await CreateOpportunity(client, goalId);

            var response = await client.PostAsJsonAsync($"/api/opportunities/{opportunityId}/experiments", new
            {
                hypothesis = "A paid test might work.",
                action = "Spend money.",
                timeBudgetMinutes = 10,
                budgetCents = 1,
                successCondition = "A sale",
                stopCondition = "No sale"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("must be zero", await response.Content.ReadAsStringAsync());
        }
        finally
        {
            DeleteDatabaseFiles(databasePath);
        }
    }

    [Fact]
    public async Task Invalid_evidence_decisions_and_parents_are_rejected()
    {
        var databasePath = NewDatabasePath();

        try
        {
            await using var application = new TestApplication(databasePath);
            using var client = application.CreateClient();
            var flow = await CreateFlowThroughExperiment(client);

            var negativeEvidence = await client.PostAsJsonAsync($"/api/experiments/{flow.ExperimentId}/evidence", new
            {
                views = -1,
                revenueCents = -10
            });
            Assert.Equal(HttpStatusCode.BadRequest, negativeEvidence.StatusCode);

            var invalidDecision = await client.PutAsJsonAsync($"/api/experiments/{flow.ExperimentId}/decision", new
            {
                outcome = "MAYBE",
                reason = "Not an allowed outcome"
            });
            Assert.Equal(HttpStatusCode.BadRequest, invalidDecision.StatusCode);

            var missingParent = await client.PostAsJsonAsync($"/api/opportunities/{Guid.NewGuid()}/experiments", new
            {
                hypothesis = "Test",
                action = "Act",
                timeBudgetMinutes = 10,
                budgetCents = 0,
                successCondition = "Success",
                stopCondition = "Stop"
            });
            Assert.Equal(HttpStatusCode.NotFound, missingParent.StatusCode);
        }
        finally
        {
            DeleteDatabaseFiles(databasePath);
        }
    }

    private static async Task<(Guid GoalId, Guid ExperimentId)> CreateFlowThroughExperiment(HttpClient client)
    {
        var goalId = await CreateGoal(client);
        var opportunityId = await CreateOpportunity(client, goalId);
        using var experiment = await PostAndRequireSuccess(client, $"/api/opportunities/{opportunityId}/experiments", new
        {
            hypothesis = "One focused listing can produce a sale.",
            action = "Publish one compliant listing.",
            timeBudgetMinutes = 180,
            budgetCents = 0,
            successCondition = "At least one sale",
            stopCondition = "No meaningful traffic after seven days"
        });

        return (goalId, experiment.RootElement.GetProperty("id").GetGuid());
    }

    private static async Task<Guid> CreateGoal(HttpClient client)
    {
        using var goal = await PostAndRequireSuccess(client, "/api/goals", new
        {
            objective = "Earn the first $10–50 online.",
            constraints = "$0 upfront; no clients; minimal live communication.",
            successCriteria = "Receive one real payout."
        });
        return goal.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateOpportunity(HttpClient client, Guid goalId)
    {
        using var opportunity = await PostAndRequireSuccess(client, $"/api/goals/{goalId}/opportunities", new
        {
            title = "Self-service marketplace listing",
            source = "https://example.com/opportunity",
            whyNow = "Visible buyer demand",
            moneyMechanism = "Marketplace sale",
            competition = "Moderate",
            geography = "Israel and Cyprus supported",
            payoutRules = "Payout after threshold",
            aiRestrictions = "Reviewed AI assistance allowed",
            friction = "Listing review",
            probabilityToFirstDollar = 25,
            reusableValue = "Reusable listing workflow"
        });
        return opportunity.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<JsonDocument> PostAndRequireSuccess(HttpClient client, string path, object body)
    {
        var response = await client.PostAsJsonAsync(path, body);
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, content);
        return JsonDocument.Parse(content);
    }

    private static async Task<JsonDocument> PutAndRequireSuccess(HttpClient client, string path, object body)
    {
        var response = await client.PutAsJsonAsync(path, body);
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, content);
        return JsonDocument.Parse(content);
    }

    private static async Task<JsonDocument> GetJson(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    private static string NewDatabasePath() =>
        Path.Combine(Path.GetTempPath(), $"opportunity-os-{Guid.NewGuid():N}.db");

    private static void DeleteDatabaseFiles(string databasePath)
    {
        foreach (var path in new[] { databasePath, $"{databasePath}-shm", $"{databasePath}-wal" })
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private sealed class TestApplication(string databasePath) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.UseSetting(
                "ConnectionStrings:DefaultConnection",
                $"Data Source={databasePath};Pooling=False");
        }
    }
}
