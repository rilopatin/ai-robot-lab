using OpportunityOs.Api.Domain;

namespace OpportunityOs.Api.Contracts;

public sealed record CreateGoalRequest(
    string? Objective,
    string? Constraints,
    string? SuccessCriteria);

public sealed record CreateOpportunityRequest(
    string? Title,
    string? Source,
    string? WhyNow,
    string? MoneyMechanism,
    string? Competition,
    string? Geography,
    string? PayoutRules,
    string? AiRestrictions,
    string? Friction,
    int ProbabilityToFirstDollar,
    string? ReusableValue);

public sealed record CreateExperimentRequest(
    string? Hypothesis,
    string? Action,
    int TimeBudgetMinutes,
    long BudgetCents,
    string? SuccessCondition,
    string? StopCondition);

public sealed record AddEvidenceRequest(
    long? Views,
    long? Installs,
    long? Downloads,
    long? Users,
    long? Clicks,
    long? Sales,
    long RevenueCents,
    string? RejectionOrWaitlist,
    string? PlatformFriction,
    string? Feedback);

public sealed record UpdateExperimentResultRequest(
    ExperimentStatus Status,
    string? Result);

public sealed record PutDecisionRequest(
    DecisionOutcome Outcome,
    string? Reason);
