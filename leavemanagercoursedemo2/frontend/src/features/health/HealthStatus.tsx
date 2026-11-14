import { useCallback, useEffect, useState } from 'react'
import { getJson } from '../../shared/api/client'

type SystemHealth = { status: string; version: string }

type HealthState =
  { kind: 'loading' } | { kind: 'healthy'; version: string } | { kind: 'error' }

/** Shows whether the backend is running (spec 0001, User experience). */
export function HealthStatus() {
  const [state, setState] = useState<HealthState>({ kind: 'loading' })
  const [attempt, setAttempt] = useState(0)

  useEffect(() => {
    const controller = new AbortController()
    getJson<SystemHealth>('/health', controller.signal)
      .then((health) => {
        // An unexpected answer counts as unreachable (spec 0001, States: Hata).
        if (health.status === 'healthy' && typeof health.version === 'string') {
          setState({ kind: 'healthy', version: health.version })
        } else {
          setState({ kind: 'error' })
        }
      })
      .catch(() => {
        if (!controller.signal.aborted) {
          setState({ kind: 'error' })
        }
      })
    return () => controller.abort()
  }, [attempt])

  const retry = useCallback(() => {
    setState({ kind: 'loading' })
    setAttempt((n) => n + 1)
  }, [])

  return (
    <section aria-labelledby="health-title">
      <h1 id="health-title">İzin Yönetimi</h1>
      <div role="status">
        {state.kind === 'loading' && <p>Sistem durumu kontrol ediliyor…</p>}
        {state.kind === 'healthy' && (
          <p>
            Sistem çalışıyor <span>(sürüm {state.version})</span>
          </p>
        )}
        {state.kind === 'error' && <p>Sunucuya ulaşılamıyor</p>}
      </div>
      {state.kind === 'error' && (
        <button type="button" onClick={retry}>
          Tekrar dene
        </button>
      )}
    </section>
  )
}
