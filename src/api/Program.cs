using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

var projects = new ConcurrentDictionary<Guid, Project>();
var experiments = new ConcurrentDictionary<Guid, Experiment>();

var app = builder.Build();

app.MapPost("/api/projects", (CreateProjectRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new { error = "Project name is required." });
    }

    var project = new Project(
        Guid.NewGuid(),
        request.Name.Trim(),
        request.Description?.Trim() ?? string.Empty);

    projects[project.Id] = project;

    return Results.Created($"/api/projects/{project.Id}", project);
});

app.MapPost("/api/projects/{projectId:guid}/experiments", (Guid projectId, CreateExperimentRequest request) =>
{
    if (!projects.ContainsKey(projectId))
    {
        return Results.NotFound(new { error = "Project not found." });
    }

    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest(new { error = "Experiment title is required." });
    }

    var experiment = new Experiment(
        Guid.NewGuid(),
        projectId,
        request.Title.Trim(),
        request.Notes?.Trim() ?? string.Empty,
        request.Result?.Trim() ?? string.Empty);

    experiments[experiment.Id] = experiment;

    return Results.Created($"/api/experiments/{experiment.Id}", experiment);
});

app.MapGet("/api/experiments/{id:guid}", (Guid id) =>
    experiments.TryGetValue(id, out var experiment)
        ? Results.Ok(experiment)
        : Results.NotFound(new { error = "Experiment not found." }));

app.Run();

record CreateProjectRequest(string? Name, string? Description);

record Project(Guid Id, string Name, string Description);

record CreateExperimentRequest(string? Title, string? Notes, string? Result);

record Experiment(Guid Id, Guid ProjectId, string Title, string Notes, string Result);
