import { type FormEvent, useState } from 'react'

type Project = {
  id: string
  name: string
  description: string
}

type Experiment = {
  id: string
  projectId: string
  title: string
  notes: string
  result: string
}

function App() {
  const [projectName, setProjectName] = useState('')
  const [projectDescription, setProjectDescription] = useState('')
  const [project, setProject] = useState<Project | null>(null)
  const [experimentTitle, setExperimentTitle] = useState('')
  const [notes, setNotes] = useState('')
  const [result, setResult] = useState('')
  const [savedExperiment, setSavedExperiment] = useState<Experiment | null>(null)
  const [error, setError] = useState('')
  const [isSaving, setIsSaving] = useState(false)

  async function createProject(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError('')
    setIsSaving(true)

    try {
      const response = await fetch('/api/projects', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name: projectName, description: projectDescription }),
      })
      const body = await response.json()

      if (!response.ok) {
        throw new Error(body.error ?? 'Could not create the project.')
      }

      setProject(body as Project)
    } catch (caughtError) {
      setError(caughtError instanceof Error ? caughtError.message : 'Could not create the project.')
    } finally {
      setIsSaving(false)
    }
  }

  async function createExperiment(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!project) return

    setError('')
    setIsSaving(true)

    try {
      const createResponse = await fetch(`/api/projects/${project.id}/experiments`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ title: experimentTitle, notes, result }),
      })
      const createdBody = await createResponse.json()

      if (!createResponse.ok) {
        throw new Error(createdBody.error ?? 'Could not create the experiment.')
      }

      const savedResponse = await fetch(`/api/experiments/${createdBody.id}`)
      const savedBody = await savedResponse.json()

      if (!savedResponse.ok) {
        throw new Error(savedBody.error ?? 'Could not load the saved experiment.')
      }

      setSavedExperiment(savedBody as Experiment)
    } catch (caughtError) {
      setError(caughtError instanceof Error ? caughtError.message : 'Could not create the experiment.')
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <main>
      <h1>AI Robot Lab</h1>

      {!project ? (
        <section>
          <h2>Create a project</h2>
          <form onSubmit={createProject}>
            <label>
              Name
              <input value={projectName} onChange={(event) => setProjectName(event.target.value)} required />
            </label>
            <label>
              Description
              <textarea
                value={projectDescription}
                onChange={(event) => setProjectDescription(event.target.value)}
              />
            </label>
            <button disabled={isSaving}>Create project</button>
          </form>
        </section>
      ) : (
        <>
          <section>
            <h2>{project.name}</h2>
            <p>{project.description}</p>
          </section>

          <section>
            <h2>Create an experiment</h2>
            <form onSubmit={createExperiment}>
              <label>
                Title
                <input
                  value={experimentTitle}
                  onChange={(event) => setExperimentTitle(event.target.value)}
                  required
                />
              </label>
              <label>
                Notes
                <textarea value={notes} onChange={(event) => setNotes(event.target.value)} />
              </label>
              <label>
                Result / status
                <input value={result} onChange={(event) => setResult(event.target.value)} />
              </label>
              <button disabled={isSaving}>Save experiment</button>
            </form>
          </section>
        </>
      )}

      {error && <p className="error">{error}</p>}

      {savedExperiment && (
        <section>
          <h2>Saved experiment</h2>
          <dl>
            <dt>Title</dt>
            <dd>{savedExperiment.title}</dd>
            <dt>Notes</dt>
            <dd>{savedExperiment.notes}</dd>
            <dt>Result / status</dt>
            <dd>{savedExperiment.result}</dd>
          </dl>
        </section>
      )}
    </main>
  )
}

export default App
