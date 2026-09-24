using System.Text.Json.Serialization;

namespace OpportunityOs.Api.Domain;

public enum GoalStatus
{
    Active,
    Achieved,
    Paused,
    Abandoned
}

public enum ExperimentStatus
{
    Planned,
    Running,
    Completed,
    Stopped
}

public enum DecisionOutcome
{
    GO,
    WATCH,
    KILL,
    PIVOT,
    SCALE
}

public sealed class Goal
{
    public Guid Id { get; set; }
    public required string Objective { get; set; }
    public required string Constraints { get; set; }
    public required string SuccessCriteria { get; set; }
    public GoalStatus Status { get; set; } = GoalStatus.Active;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public List<Opportunity> Opportunities { get; set; } = [];
}

public sealed class Opportunity
{
    public Guid Id { get; set; }
    public Guid GoalId { get; set; }
    [JsonIgnore]
    public Goal? Goal { get; set; }
    public required string Title { get; set; }
    public required string Source { get; set; }
    public required string WhyNow { get; set; }
    public required string MoneyMechanism { get; set; }
    public required string Competition { get; set; }
    public required string Geography { get; set; }
    public required string PayoutRules { get; set; }
    public required string AiRestrictions { get; set; }
    public required string Friction { get; set; }
    public int ProbabilityToFirstDollar { get; set; }
    public required string ReusableValue { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public List<Experiment> Experiments { get; set; } = [];
}

public sealed class Experiment
{
    public Guid Id { get; set; }
    public Guid OpportunityId { get; set; }
    [JsonIgnore]
    public Opportunity? Opportunity { get; set; }
    public required string Hypothesis { get; set; }
    public required string Action { get; set; }
    public int TimeBudgetMinutes { get; set; }
    public long BudgetCents { get; set; }
    public required string SuccessCondition { get; set; }
    public required string StopCondition { get; set; }
    public ExperimentStatus Status { get; set; } = ExperimentStatus.Planned;
    public required string Result { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public List<Evidence> Evidence { get; set; } = [];
    public Decision? Decision { get; set; }
}

public sealed class Evidence
{
    public Guid Id { get; set; }
    public Guid ExperimentId { get; set; }
    [JsonIgnore]
    public Experiment? Experiment { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public long? Views { get; set; }
    public long? Installs { get; set; }
    public long? Downloads { get; set; }
    public long? Users { get; set; }
    public long? Clicks { get; set; }
    public long? Sales { get; set; }
    public long RevenueCents { get; set; }
    public required string RejectionOrWaitlist { get; set; }
    public required string PlatformFriction { get; set; }
    public required string Feedback { get; set; }
}

public sealed class Decision
{
    public Guid Id { get; set; }
    public Guid ExperimentId { get; set; }
    [JsonIgnore]
    public Experiment? Experiment { get; set; }
    public DecisionOutcome Outcome { get; set; }
    public required string Reason { get; set; }
    public DateTime DecidedAtUtc { get; set; }
}
