import type { Goal, GoalSummary } from './types'

type JsonValue = Record<string, unknown>

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    ...options,
    headers: options?.body
      ? { 'Content-Type': 'application/json', ...options.headers }
      : options?.headers,
  })
  const body = (await response.json()) as T & { error?: string }

  if (!response.ok) {
    throw new Error(body.error ?? 'The request could not be completed.')
  }

  return body
}

function send<T>(method: 'POST' | 'PUT', path: string, body: JsonValue): Promise<T> {
  return request<T>(path, { method, body: JSON.stringify(body) })
}

export const api = {
  listGoals: () => request<GoalSummary[]>('/api/goals'),
  getGoal: (goalId: string) => request<Goal>(`/api/goals/${goalId}`),
  createGoal: (body: JsonValue) => send<Goal>('POST', '/api/goals', body),
  createOpportunity: (goalId: string, body: JsonValue) =>
    send('POST', `/api/goals/${goalId}/opportunities`, body),
  createExperiment: (opportunityId: string, body: JsonValue) =>
    send('POST', `/api/opportunities/${opportunityId}/experiments`, body),
  addEvidence: (experimentId: string, body: JsonValue) =>
    send('POST', `/api/experiments/${experimentId}/evidence`, body),
  updateExperimentResult: (experimentId: string, body: JsonValue) =>
    send('PUT', `/api/experiments/${experimentId}/result`, body),
  putDecision: (experimentId: string, body: JsonValue) =>
    send('PUT', `/api/experiments/${experimentId}/decision`, body),
}
