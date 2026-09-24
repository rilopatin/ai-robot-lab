export type GoalStatus = 'Active' | 'Achieved' | 'Paused' | 'Abandoned'
export type ExperimentStatus = 'Planned' | 'Running' | 'Completed' | 'Stopped'
export type DecisionOutcome = 'GO' | 'WATCH' | 'KILL' | 'PIVOT' | 'SCALE'

export type GoalSummary = {
  id: string
  objective: string
  status: GoalStatus
  createdAtUtc: string
  updatedAtUtc: string
  opportunityCount: number
}

export type Goal = {
  id: string
  objective: string
  constraints: string
  successCriteria: string
  status: GoalStatus
  createdAtUtc: string
  updatedAtUtc: string
  opportunities: Opportunity[]
}

export type Opportunity = {
  id: string
  goalId: string
  title: string
  source: string
  whyNow: string
  moneyMechanism: string
  competition: string
  geography: string
  payoutRules: string
  aiRestrictions: string
  friction: string
  probabilityToFirstDollar: number
  reusableValue: string
  createdAtUtc: string
  experiments: Experiment[]
}

export type Experiment = {
  id: string
  opportunityId: string
  hypothesis: string
  action: string
  timeBudgetMinutes: number
  budgetCents: number
  successCondition: string
  stopCondition: string
  status: ExperimentStatus
  result: string
  createdAtUtc: string
  updatedAtUtc: string
  evidence: Evidence[]
  decision: Decision | null
}

export type Evidence = {
  id: string
  experimentId: string
  recordedAtUtc: string
  views: number | null
  installs: number | null
  downloads: number | null
  users: number | null
  clicks: number | null
  sales: number | null
  revenueCents: number
  rejectionOrWaitlist: string
  platformFriction: string
  feedback: string
}

export type Decision = {
  id: string
  experimentId: string
  outcome: DecisionOutcome
  reason: string
  decidedAtUtc: string
}
