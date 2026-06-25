import { useState, useEffect } from 'react'
import { getCleaningTasks, startCleaning, completeCleaning } from '../../api/client'
import toast from 'react-hot-toast'

// Backend CleaningTaskStatus enum strings: Queued, InProgress, Completed
// Backend CleaningPriority enum strings: Rush, Normal
// DTO fields: id, roomId, roomNumber, floor, status (string), priority (string), assignedCleanerId, startedAt, completedAt, createdAt

const STATUS_DISPLAY = { Queued: 'Pending', InProgress: 'In Progress', Completed: 'Completed' }
const STATUS_STYLE = {
  Queued:     { bg: '#FFFBEB', border: '#FDE68A', color: '#92400E' },
  InProgress: { bg: '#EFF6FF', border: '#BFDBFE', color: '#1E40AF' },
  Completed:  { bg: '#ECFDF5', border: '#A7F3D0', color: '#065F46' },
}
const PRIORITY_COLOR   = { Rush: '#DC2626', Normal: '#2563EB' }
const PRIORITY_DOT     = { Rush: '● ', Normal: '' }

// Filter passes enum string names to the backend query param
const FILTERS = [['', 'All tasks'], ['Queued', 'Pending'], ['InProgress', 'In Progress'], ['Completed', 'Completed']]

export default function HousekeepingPage() {
  const [tasks, setTasks] = useState([])
  const [loading, setLoading] = useState(true)
  const [filter, setFilter] = useState('')
  const [busy, setBusy] = useState(null)
  const [modal, setModal] = useState(null)
  const [cleanerId, setCleanerId] = useState('')

  const load = async () => {
    setLoading(true)
    try {
      // Pass filter as string enum name or undefined for all
      setTasks(await getCleaningTasks(filter || undefined) || [])
    } catch { toast.error('Failed to load tasks') }
    finally { setLoading(false) }
  }

  useEffect(() => { load() }, [filter])

  const handleStart = async () => {
    const id = cleanerId.trim() || crypto.randomUUID()
    setBusy(modal)
    try {
      await startCleaning(modal, id)
      toast.success('Task started')
      setModal(null); setCleanerId(''); load()
    } catch (err) { toast.error(err.response?.data?.description || err.response?.data?.Description || 'Failed') }
    finally { setBusy(null) }
  }

  const handleComplete = async (id) => {
    setBusy(id)
    try {
      await completeCleaning(id)
      toast.success('Room marked clean!')
      load()
    } catch (err) { toast.error(err.response?.data?.description || err.response?.data?.Description || 'Failed') }
    finally { setBusy(null) }
  }

  // Use status string keys for counts
  const counts = {
    Queued:     tasks.filter(t => t.status === 'Queued').length,
    InProgress: tasks.filter(t => t.status === 'InProgress').length,
    Completed:  tasks.filter(t => t.status === 'Completed').length,
  }

  return (
    <div>
      <div style={{ display: 'flex', alignItems: 'flex-end', justifyContent: 'space-between', marginBottom: 28 }}>
        <div>
          <h1 className="page-title">Housekeeping</h1>
          <p className="page-sub">Manage room cleaning assignments</p>
        </div>
        <button onClick={load} className="btn-ghost" style={{ padding: '9px 18px', fontSize: 13 }}>↻ Refresh</button>
      </div>

      {/* Stats */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3,1fr)', gap: 14, marginBottom: 24 }}>
        {[
          { label: 'Pending', v: counts.Queued,     color: '#D97706', bg: '#FFFBEB' },
          { label: 'In Progress', v: counts.InProgress, color: '#2563EB', bg: '#EFF6FF' },
          { label: 'Completed', v: counts.Completed,  color: '#059669', bg: '#ECFDF5' },
        ].map(s => (
          <div key={s.label} className="lux-card" style={{ padding: '18px 22px', textAlign: 'center' }}>
            <p style={{ fontSize: 30, fontWeight: 900, color: s.color, lineHeight: 1 }}>{s.v}</p>
            <p style={{ fontSize: 12.5, color: '#64748B', marginTop: 5, fontWeight: 600 }}>{s.label}</p>
          </div>
        ))}
      </div>

      {/* Filter */}
      <div style={{ display: 'flex', gap: 8, marginBottom: 20, flexWrap: 'wrap' }}>
        {FILTERS.map(([v, l]) => (
          <button key={v} onClick={() => setFilter(v)} style={{
            padding: '7px 18px', borderRadius: 20, fontSize: 13, fontWeight: 700, cursor: 'pointer',
            transition: 'all 0.15s', background: filter === v ? '#0B1930' : '#fff',
            color: filter === v ? 'white' : '#64748B',
            boxShadow: filter === v ? '0 4px 12px rgba(11,25,48,0.2)' : 'none',
            border: filter === v ? 'none' : '1px solid #E8E6E1',
          }}>{l}</button>
        ))}
      </div>

      {loading && <div style={{ textAlign: 'center', padding: '60px 0', color: '#94A3B8' }}>Loading tasks…</div>}

      {!loading && tasks.length === 0 && (
        <div style={{ textAlign: 'center', padding: '60px 0' }}>
          <p style={{ fontSize: 32, marginBottom: 10 }}>✨</p>
          <p style={{ fontSize: 15, fontWeight: 600, color: '#64748B' }}>No tasks found</p>
        </div>
      )}

      {!loading && tasks.length > 0 && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
          {tasks.map(task => {
            // task.status is string: "Queued", "InProgress", "Completed"
            // task.priority is string: "Rush", "Normal"
            const ss = STATUS_STYLE[task.status] ?? STATUS_STYLE.Queued
            const priorityColor = PRIORITY_COLOR[task.priority] ?? '#94A3B8'
            const priorityDot   = PRIORITY_DOT[task.priority]  ?? ''
            return (
              <div key={task.id} className="lux-card" style={{ padding: '18px 22px', display: 'flex', alignItems: 'center', gap: 16 }}>
                <div style={{ width: 48, height: 48, background: '#F7F6F3', borderRadius: 12, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 22, flexShrink: 0 }}>✨</div>
                <div style={{ flex: 1, minWidth: 0 }}>
                  <div style={{ display: 'flex', alignItems: 'center', flexWrap: 'wrap', gap: 8, marginBottom: 5 }}>
                    <span style={{ fontSize: 15, fontWeight: 800, color: '#0B1930' }}>Room {task.roomNumber}</span>
                    <span style={{ padding: '2px 10px', borderRadius: 20, fontSize: 11.5, fontWeight: 700, background: ss.bg, border: `1px solid ${ss.border}`, color: ss.color }}>
                      {STATUS_DISPLAY[task.status] ?? task.status}
                    </span>
                    {task.priority && (
                      <span style={{ fontSize: 12, fontWeight: 700, color: priorityColor }}>
                        {priorityDot}{task.priority}
                      </span>
                    )}
                  </div>
                  <p style={{ fontSize: 12, color: '#94A3B8' }}>
                    Floor {task.floor}
                    {task.startedAt   && ` · Started ${new Date(task.startedAt).toLocaleTimeString()}`}
                    {task.completedAt && ` · Done ${new Date(task.completedAt).toLocaleTimeString()}`}
                  </p>
                </div>
                <div style={{ flexShrink: 0 }}>
                  {task.status === 'Queued' && (
                    <button onClick={() => setModal(task.id)} disabled={busy === task.id} className="btn-navy" style={{ padding: '8px 18px', fontSize: 13 }}>
                      ▶ Start
                    </button>
                  )}
                  {task.status === 'InProgress' && (
                    <button onClick={() => handleComplete(task.id)} disabled={busy === task.id} className="btn-emerald" style={{ padding: '8px 18px', fontSize: 13 }}>
                      {busy === task.id ? '…' : '✓ Complete'}
                    </button>
                  )}
                  {task.status === 'Completed' && (
                    <span style={{ fontSize: 13, fontWeight: 700, color: '#059669' }}>✓ Done</span>
                  )}
                </div>
              </div>
            )
          })}
        </div>
      )}

      {/* Start Modal */}
      {modal && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.4)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 50, padding: 16 }}>
          <div className="lux-card" style={{ padding: 28, width: '100%', maxWidth: 400 }}>
            <h3 style={{ fontSize: 17, fontWeight: 800, color: '#0B1930', marginBottom: 6 }}>Assign Cleaner</h3>
            <p style={{ fontSize: 13.5, color: '#64748B', marginBottom: 20 }}>Enter cleaner ID or leave blank to auto-assign</p>
            <label className="lux-label">Cleaner ID (UUID)</label>
            <input className="lux-input" placeholder="Auto-assigned if empty" value={cleanerId} onChange={e => setCleanerId(e.target.value)} style={{ marginBottom: 20 }} />
            <div style={{ display: 'flex', gap: 10 }}>
              <button onClick={() => { setModal(null); setCleanerId('') }} className="btn-ghost" style={{ flex: 1 }}>Cancel</button>
              <button onClick={handleStart} disabled={busy === modal} className="btn-emerald" style={{ flex: 1 }}>
                {busy === modal ? 'Starting…' : 'Start Task'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
