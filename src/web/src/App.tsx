import { type FormEvent, useEffect, useState } from 'react'
import { api } from './api'
import type {
  DecisionOutcome,
  Experiment,
  ExperimentStatus,
  Goal,
  GoalSummary,
  Opportunity,
} from './types'

const decisionOutcomes: DecisionOutcome[] = ['GO', 'WATCH', 'KILL', 'PIVOT', 'SCALE']
const experimentStatuses: ExperimentStatus[] = ['Planned', 'Running', 'Completed', 'Stopped']

function text(form: FormData, name: string) {
  return String(form.get(name) ?? '')
}

function integer(form: FormData, name: string) {
  return Number.parseInt(text(form, name), 10)
}

function optionalInteger(form: FormData, name: string) {
  const value = text(form, name).trim()
  return value === '' ? null : Number.parseInt(value, 10)
}

function money(cents: number) {
  return `$${(cents / 100).toFixed(2)}`
}

function App() {
  const [goals, setGoals] = useState<GoalSummary[]>([])
  const [goal, setGoal] = useState<Goal | null>(null)
  const [error, setError] = useState('')
  const [isSaving, setIsSaving] = useState(false)

  useEffect(() => {
    async function openWorkspace() {
      try {
        const availableGoals = await api.listGoals()
        setGoals(availableGoals)
        if (availableGoals[0]) {
          setGoal(await api.getGoal(availableGoals[0].id))
        }
      } catch (caughtError) {
        showError(caughtError)
      }
    }

    void openWorkspace()
  }, [])

  function showError(caughtError: unknown) {
    setError(caughtError instanceof Error ? caughtError.message : 'Something went wrong.')
  }

  async function refreshGoals(selectedGoalId?: string) {
    const availableGoals = await api.listGoals()
    setGoals(availableGoals)
    const goalId = selectedGoalId ?? goal?.id
    if (goalId) {
      setGoal(await api.getGoal(goalId))
    }
  }

  async function run(action: () => Promise<void>) {
    setError('')
    setIsSaving(true)
    try {
      await action()
    } catch (caughtError) {
      showError(caughtError)
    } finally {
      setIsSaving(false)
    }
  }

  function createGoal(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const formElement = event.currentTarget
    const form = new FormData(formElement)

    void run(async () => {
      const created = await api.createGoal({
        objective: text(form, 'objective'),
        constraints: text(form, 'constraints'),
        successCriteria: text(form, 'successCriteria'),
      })
      formElement.reset()
      await refreshGoals(created.id)
    })
  }

  function createOpportunity(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!goal) return
    const formElement = event.currentTarget
    const form = new FormData(formElement)

    void run(async () => {
      await api.createOpportunity(goal.id, {
        title: text(form, 'title'),
        source: text(form, 'source'),
        whyNow: text(form, 'whyNow'),
        moneyMechanism: text(form, 'moneyMechanism'),
        competition: text(form, 'competition'),
        geography: text(form, 'geography'),
        payoutRules: text(form, 'payoutRules'),
        aiRestrictions: text(form, 'aiRestrictions'),
        friction: text(form, 'friction'),
        probabilityToFirstDollar: integer(form, 'probabilityToFirstDollar'),
        reusableValue: text(form, 'reusableValue'),
      })
      formElement.reset()
      await refreshGoals(goal.id)
    })
  }

  return (
    <main>
      <header className="page-header">
        <p className="eyebrow">Goal-driven learning loop</p>
        <h1>Opportunity OS</h1>
        <p>Turn a real income goal into small experiments, durable evidence, and explicit decisions.</p>
      </header>

      {error && <p className="error" role="alert">{error}</p>}

      <section>
        <div className="section-heading">
          <div>
            <p className="step">01</p>
            <h2>Goal</h2>
          </div>
          {goals.length > 0 && (
            <label className="goal-picker">
              Reopen goal
              <select
                value={goal?.id ?? ''}
                onChange={(event) => void run(async () => setGoal(await api.getGoal(event.target.value)))}
                disabled={isSaving}
              >
                {goals.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.objective} ({item.opportunityCount})
                  </option>
                ))}
              </select>
            </label>
          )}
        </div>

        {goal ? (
          <div className="goal-summary">
            <span className="status">{goal.status}</span>
            <h3>{goal.objective}</h3>
            <dl className="details-grid">
              <div><dt>Constraints</dt><dd>{goal.constraints}</dd></div>
              <div><dt>Success criteria</dt><dd>{goal.successCriteria}</dd></div>
            </dl>
          </div>
        ) : (
          <p className="empty">Create the first goal to begin the loop.</p>
        )}

        <details className="create-panel" open={!goal}>
          <summary>Create a goal</summary>
          <form onSubmit={createGoal}>
            <label>Objective<textarea name="objective" required /></label>
            <label>Constraints<textarea name="constraints" required /></label>
            <label>Success criteria<textarea name="successCriteria" required /></label>
            <button disabled={isSaving}>Create goal</button>
          </form>
        </details>
      </section>

      {goal && (
        <>
          <section>
            <div className="section-heading">
              <div><p className="step">02</p><h2>Opportunities</h2></div>
              <span className="count">{goal.opportunities.length}</span>
            </div>

            {goal.opportunities.length === 0 && (
              <p className="empty">No opportunities recorded yet.</p>
            )}

            <div className="stack">
              {goal.opportunities.map((opportunity) => (
                <OpportunityCard
                  key={opportunity.id}
                  opportunity={opportunity}
                  isSaving={isSaving}
                  run={run}
                  refresh={() => refreshGoals(goal.id)}
                />
              ))}
            </div>

            <details className="create-panel">
              <summary>Add an opportunity</summary>
              <form onSubmit={createOpportunity}>
                <div className="form-grid">
                  <label>Title<input name="title" required /></label>
                  <label>Source<input name="source" type="url" required /></label>
                </div>
                <label>Why now?<textarea name="whyNow" required /></label>
                <label>Money mechanism<textarea name="moneyMechanism" required /></label>
                <div className="form-grid">
                  <label>Competition<textarea name="competition" /></label>
                  <label>Friction<textarea name="friction" /></label>
                </div>
                <div className="form-grid">
                  <label>Geography<textarea name="geography" required /></label>
                  <label>Payout rules<textarea name="payoutRules" required /></label>
                </div>
                <label>AI restrictions<textarea name="aiRestrictions" /></label>
                <label>
                  Estimated probability to first dollar (%)
                  <input name="probabilityToFirstDollar" type="number" min="0" max="100" required />
                </label>
                <label>Reusable value<textarea name="reusableValue" /></label>
                <button disabled={isSaving}>Add opportunity</button>
              </form>
            </details>
          </section>
        </>
      )}
    </main>
  )
}

type ChildProps = {
  isSaving: boolean
  run: (action: () => Promise<void>) => Promise<void>
  refresh: () => Promise<void>
}

function OpportunityCard({ opportunity, isSaving, run, refresh }: ChildProps & { opportunity: Opportunity }) {
  function createExperiment(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const formElement = event.currentTarget
    const form = new FormData(formElement)

    void run(async () => {
      await api.createExperiment(opportunity.id, {
        hypothesis: text(form, 'hypothesis'),
        action: text(form, 'action'),
        timeBudgetMinutes: integer(form, 'timeBudgetMinutes'),
        budgetCents: 0,
        successCondition: text(form, 'successCondition'),
        stopCondition: text(form, 'stopCondition'),
      })
      formElement.reset()
      await refresh()
    })
  }

  return (
    <article className="opportunity-card">
      <div className="card-heading">
        <div>
          <p className="muted">{opportunity.moneyMechanism}</p>
          <h3>{opportunity.title}</h3>
        </div>
        <strong className="probability">{opportunity.probabilityToFirstDollar}%</strong>
      </div>

      <dl className="details-grid compact">
        <div><dt>Why now</dt><dd>{opportunity.whyNow}</dd></div>
        <div><dt>Geography</dt><dd>{opportunity.geography}</dd></div>
        <div><dt>Payout rules</dt><dd>{opportunity.payoutRules}</dd></div>
        <div><dt>Competition</dt><dd>{opportunity.competition || 'Not recorded'}</dd></div>
        <div><dt>AI restrictions</dt><dd>{opportunity.aiRestrictions || 'Not recorded'}</dd></div>
        <div><dt>Friction</dt><dd>{opportunity.friction || 'Not recorded'}</dd></div>
        <div><dt>Reusable value</dt><dd>{opportunity.reusableValue || 'Not recorded'}</dd></div>
        <div><dt>Source</dt><dd><a href={opportunity.source} target="_blank" rel="noreferrer">Open source</a></dd></div>
      </dl>

      <div className="experiment-list">
        {opportunity.experiments.map((experiment) => (
          <ExperimentCard
            key={experiment.id}
            experiment={experiment}
            isSaving={isSaving}
            run={run}
            refresh={refresh}
          />
        ))}
      </div>

      <details className="create-panel nested">
        <summary>Define a zero-cost experiment</summary>
        <form onSubmit={createExperiment}>
          <label>Hypothesis<textarea name="hypothesis" required /></label>
          <label>Action<textarea name="action" required /></label>
          <label>Time budget (minutes)<input name="timeBudgetMinutes" type="number" min="0" required /></label>
          <p className="budget-note">Budget: $0.00 — enforced for v0.2A</p>
          <div className="form-grid">
            <label>Success condition<textarea name="successCondition" required /></label>
            <label>Stop condition<textarea name="stopCondition" required /></label>
          </div>
          <button disabled={isSaving}>Create experiment</button>
        </form>
      </details>
    </article>
  )
}

function ExperimentCard({ experiment, isSaving, run, refresh }: ChildProps & { experiment: Experiment }) {
  function addEvidence(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const formElement = event.currentTarget
    const form = new FormData(formElement)

    void run(async () => {
      await api.addEvidence(experiment.id, {
        views: optionalInteger(form, 'views'),
        installs: optionalInteger(form, 'installs'),
        downloads: optionalInteger(form, 'downloads'),
        users: optionalInteger(form, 'users'),
        clicks: optionalInteger(form, 'clicks'),
        sales: optionalInteger(form, 'sales'),
        revenueCents: integer(form, 'revenueCents'),
        rejectionOrWaitlist: text(form, 'rejectionOrWaitlist'),
        platformFriction: text(form, 'platformFriction'),
        feedback: text(form, 'feedback'),
      })
      formElement.reset()
      await refresh()
    })
  }

  function updateResult(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = new FormData(event.currentTarget)
    void run(async () => {
      await api.updateExperimentResult(experiment.id, {
        status: text(form, 'status'),
        result: text(form, 'result'),
      })
      await refresh()
    })
  }

  function putDecision(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = new FormData(event.currentTarget)
    void run(async () => {
      await api.putDecision(experiment.id, {
        outcome: text(form, 'outcome'),
        reason: text(form, 'reason'),
      })
      await refresh()
    })
  }

  return (
    <article className="experiment-card">
      <div className="card-heading">
        <div><p className="muted">Experiment</p><h4>{experiment.hypothesis}</h4></div>
        <span className="status">{experiment.status}</span>
      </div>
      <p><strong>Action:</strong> {experiment.action}</p>
      <div className="limits">
        <span>{experiment.timeBudgetMinutes} minutes</span>
        <span>{money(experiment.budgetCents)} budget</span>
      </div>
      <dl className="details-grid compact">
        <div><dt>Success</dt><dd>{experiment.successCondition}</dd></div>
        <div><dt>Stop</dt><dd>{experiment.stopCondition}</dd></div>
      </dl>

      {experiment.evidence.length > 0 && (
        <div className="evidence-list">
          <h5>Evidence</h5>
          {experiment.evidence.map((item) => (
            <div className="evidence-row" key={item.id}>
              <time>{new Date(item.recordedAtUtc).toLocaleString()}</time>
              <span>Views {item.views ?? '—'}</span>
              <span>Clicks {item.clicks ?? '—'}</span>
              <span>Sales {item.sales ?? '—'}</span>
              <strong>{money(item.revenueCents)}</strong>
              {(item.feedback || item.platformFriction || item.rejectionOrWaitlist) && (
                <p>{[item.feedback, item.platformFriction, item.rejectionOrWaitlist].filter(Boolean).join(' · ')}</p>
              )}
            </div>
          ))}
        </div>
      )}

      <div className="experiment-actions">
        <details className="create-panel nested">
          <summary>Record evidence</summary>
          <form onSubmit={addEvidence}>
            <div className="metrics-grid">
              {['views', 'installs', 'downloads', 'users', 'clicks', 'sales'].map((metric) => (
                <label key={metric}>{metric}<input name={metric} type="number" min="0" /></label>
              ))}
              <label>Revenue (cents)<input name="revenueCents" type="number" min="0" defaultValue="0" required /></label>
            </div>
            <label>Rejection / waitlist<textarea name="rejectionOrWaitlist" /></label>
            <label>Platform friction<textarea name="platformFriction" /></label>
            <label>Feedback<textarea name="feedback" /></label>
            <button disabled={isSaving}>Append evidence</button>
          </form>
        </details>

        <details className="create-panel nested">
          <summary>Update result</summary>
          <form onSubmit={updateResult}>
            <label>Status<select name="status" defaultValue={experiment.status}>{experimentStatuses.map((status) => <option key={status}>{status}</option>)}</select></label>
            <label>Result<textarea name="result" defaultValue={experiment.result} /></label>
            <button disabled={isSaving}>Save result</button>
          </form>
        </details>

        <details className="create-panel nested" open={!experiment.decision}>
          <summary>{experiment.decision ? 'Revise decision' : 'Make decision'}</summary>
          <form onSubmit={putDecision}>
            <label>
              Decision
              <select name="outcome" defaultValue={experiment.decision?.outcome ?? 'WATCH'}>
                {decisionOutcomes.map((outcome) => <option key={outcome}>{outcome}</option>)}
              </select>
            </label>
            <label>Reason<textarea name="reason" defaultValue={experiment.decision?.reason ?? ''} required /></label>
            <button disabled={isSaving}>Save decision</button>
          </form>
        </details>
      </div>

      {experiment.decision && (
        <div className={`decision decision-${experiment.decision.outcome.toLowerCase()}`}>
          <strong>{experiment.decision.outcome}</strong>
          <p>{experiment.decision.reason}</p>
        </div>
      )}
    </article>
  )
}

export default App
