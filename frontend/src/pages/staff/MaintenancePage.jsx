import { useState, useEffect } from 'react'
import { getMaintenanceQueue, reportIssue, resolveIssue } from '../../api/client'
import toast from 'react-hot-toast'

// Backend IssueUrgency enum: Critical=1, High=2, Normal=3, Low=4 (serialized as strings)
// Backend IssueStatus enum: Open=1, Assigned=2, InProgress=3, Resolved=4 (serialized as strings)
// DTO fields: id, roomId, roomNumber, description, urgency (string), urgencyLevel, status (string),
//             priorityRank, resolutionNotes, createdAt, resolvedAt

const URGENCY_COLOR  = { Critical: '#DC2626', High: '#D97706', Normal: '#2563EB', Low: '#94A3B8' }
const URGENCY_BG     = { Critical: '#FEF2F2', High: '#FFFBEB', Normal: '#EFF6FF', Low: '#F8FAFC' }
const URGENCY_BORDER = { Critical: '#FECACA', High: '#FDE68A', Normal: '#BFDBFE', Low: '#E2E8F0' }

const STAT_DISPLAY = { Open: 'Reported', Assigned: 'Assigned', InProgress: 'In Progress', Resolved: 'Resolved' }
const STAT_COLOR   = { Open: '#DC2626', Assigned: '#D97706', InProgress: '#2563EB', Resolved: '#059669' }
const STAT_BG      = { Open: '#FEF2F2', Assigned: '#FFFBEB', InProgress: '#EFF6FF', Resolved: '#ECFDF5' }

export default function MaintenancePage() {
  const [issues, setIssues] = useState([])
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(null)
  const [showReport, setShowReport] = useState(false)
  const [resolveId, setResolveId] = useState(null)
  const [notes, setNotes] = useState('')
  // urgency must match IssueUrgency enum string values: Critical, High, Normal, Low
  const [form, setForm] = useState({ roomId: '', roomNumber: '', description: '', urgency: 'Normal' })

  const load = async () => {
    setLoading(true)
    try { setIssues(await getMaintenanceQueue() || []) }
    catch { toast.error('Failed to load') }
    finally { setLoading(false) }
  }

  useEffect(() => { load() }, [])

  const handleReport = async (e) => {
    e.preventDefault()
    setBusy('report')
    try {
      // Backend ReportIssueCommand: { roomId (Guid), roomNumber, description, urgency (IssueUrgency string) }
      await reportIssue({ roomId: form.roomId, roomNumber: form.roomNumber, description: form.description, urgency: form.urgency })
      toast.success('Issue reported')
      setShowReport(false)
      setForm({ roomId: '', roomNumber: '', description: '', urgency: 'Normal' })
      load()
    } catch (err) { toast.error(err.response?.data?.description || err.response?.data?.Description || 'Failed') }
    finally { setBusy(null) }
  }

  const handleResolve = async () => {
    setBusy(resolveId)
    try {
      await resolveIssue(resolveId, notes)
      toast.success('Issue resolved!')
      setResolveId(null); setNotes(''); load()
    } catch (err) { toast.error(err.response?.data?.description || err.response?.data?.Description || 'Failed') }
    finally { setBusy(null) }
  }

  const counts = {
    open: issues.filter(i => i.status === 'Open' || i.status === 'Assigned').length,
    prog: issues.filter(i => i.status === 'InProgress').length,
    done: issues.filter(i => i.status === 'Resolved').length,
  }

  return (
    <div>
      <div style={{ display: 'flex', alignItems: 'flex-end', justifyContent: 'space-between', marginBottom: 28 }}>
        <div>
          <h1 className="page-title">Maintenance</h1>
          <p className="page-sub">Priority queue — critical issues first</p>
        </div>
        <button onClick={() => setShowReport(true)} style={{ padding: '10px 20px', background: '#DC2626', color: 'white', border: 'none', borderRadius: 12, fontSize: 13.5, fontWeight: 700, cursor: 'pointer', display: 'flex', alignItems: 'center', gap: 6, transition: 'all 0.15s' }}
          onMouseEnter={e => e.currentTarget.style.background = '#B91C1C'}
          onMouseLeave={e => e.currentTarget.style.background = '#DC2626'}>
          + Report Issue
        </button>
      </div>

      {/* Stats */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3,1fr)', gap: 14, marginBottom: 24 }}>
        {[{ l: 'Open', v: counts.open, c: '#DC2626', bg: '#FEF2F2' }, { l: 'In Progress', v: counts.prog, c: '#2563EB', bg: '#EFF6FF' }, { l: 'Resolved', v: counts.done, c: '#059669', bg: '#ECFDF5' }].map(s => (
          <div key={s.l} className="lux-card" style={{ padding: '18px 22px', textAlign: 'center' }}>
            <p style={{ fontSize: 30, fontWeight: 900, color: s.c, lineHeight: 1 }}>{s.v}</p>
            <p style={{ fontSize: 12.5, color: '#64748B', marginTop: 5, fontWeight: 600 }}>{s.l}</p>
          </div>
        ))}
      </div>

      {loading && <div style={{ textAlign: 'center', padding: '60px 0', color: '#94A3B8' }}>Loading issues…</div>}
      {!loading && issues.length === 0 && (
        <div style={{ textAlign: 'center', padding: '60px 0' }}>
          <p style={{ fontSize: 32, marginBottom: 10 }}>🔧</p>
          <p style={{ fontSize: 15, fontWeight: 600, color: '#64748B' }}>No maintenance issues</p>
        </div>
      )}

      {!loading && issues.length > 0 && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
          {issues.map(issue => {
            // DTO fields: urgency (string), status (string)
            const urgency = issue.urgency || 'Normal'
            const status  = issue.status  || 'Open'
            return (
              <div key={issue.id} className="lux-card" style={{ padding: '18px 22px', display: 'flex', alignItems: 'flex-start', gap: 16 }}>
                <div style={{ width: 48, height: 48, background: URGENCY_BG[urgency] ?? '#F8FAFC', borderRadius: 12, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 22, flexShrink: 0, border: `1px solid ${URGENCY_BORDER[urgency] ?? '#E2E8F0'}` }}>🔧</div>
                <div style={{ flex: 1, minWidth: 0 }}>
                  <div style={{ display: 'flex', alignItems: 'center', flexWrap: 'wrap', gap: 8, marginBottom: 6 }}>
                    <span style={{ fontSize: 15, fontWeight: 800, color: '#0B1930' }}>Room {issue.roomNumber || '—'}</span>
                    <span style={{ padding: '2px 10px', borderRadius: 20, fontSize: 11.5, fontWeight: 700, background: URGENCY_BG[urgency], border: `1px solid ${URGENCY_BORDER[urgency]}`, color: URGENCY_COLOR[urgency] }}>{urgency}</span>
                    <span style={{ padding: '2px 10px', borderRadius: 20, fontSize: 11.5, fontWeight: 700, background: STAT_BG[status] ?? '#F8FAFC', color: STAT_COLOR[status] ?? '#64748B' }}>{STAT_DISPLAY[status] ?? status}</span>
                  </div>
                  {issue.priorityRank && <p style={{ fontSize: 11, color: '#94A3B8', marginBottom: 4 }}>Queue position #{issue.priorityRank}</p>}
                  <p style={{ fontSize: 13.5, color: '#374151', marginBottom: 4, lineHeight: 1.5 }}>{issue.description}</p>
                  {issue.resolutionNotes && <p style={{ fontSize: 12, color: '#059669', fontWeight: 600 }}>Resolution: {issue.resolutionNotes}</p>}
                  {/* Backend field: createdAt (not reportedAt) */}
                  {issue.createdAt && <p style={{ fontSize: 11.5, color: '#94A3B8', marginTop: 4 }}>{new Date(issue.createdAt).toLocaleString()}</p>}
                </div>
                {status !== 'Resolved' && (
                  <button onClick={() => setResolveId(issue.id)} disabled={busy === issue.id} className="btn-emerald" style={{ padding: '8px 18px', fontSize: 13, flexShrink: 0 }}>✓ Resolve</button>
                )}
              </div>
            )
          })}
        </div>
      )}

      {/* Report Modal */}
      {showReport && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.45)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 50, padding: 16 }}>
          <div className="lux-card" style={{ padding: 30, width: '100%', maxWidth: 480 }}>
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 22 }}>
              <h3 style={{ fontSize: 17, fontWeight: 800, color: '#0B1930' }}>Report Issue</h3>
              <button onClick={() => setShowReport(false)} style={{ border: 'none', background: 'none', cursor: 'pointer', fontSize: 18, color: '#94A3B8' }}>✕</button>
            </div>
            <form onSubmit={handleReport}>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14, marginBottom: 14 }}>
                <div>
                  <label className="lux-label">Room ID (UUID)</label>
                  <input className="lux-input" required placeholder="Room UUID" value={form.roomId} onChange={e => setForm(p => ({ ...p, roomId: e.target.value }))} />
                </div>
                <div>
                  <label className="lux-label">Room Number</label>
                  <input className="lux-input" required placeholder="e.g. 101" value={form.roomNumber} onChange={e => setForm(p => ({ ...p, roomNumber: e.target.value }))} />
                </div>
              </div>
              <div style={{ marginBottom: 14 }}>
                <label className="lux-label">Description</label>
                <textarea className="lux-input" required rows={3} placeholder="Describe the issue…" value={form.description} onChange={e => setForm(p => ({ ...p, description: e.target.value }))} style={{ resize: 'none' }} />
              </div>
              <div style={{ marginBottom: 24 }}>
                <label className="lux-label">Urgency</label>
                <select className="lux-input" value={form.urgency} onChange={e => setForm(p => ({ ...p, urgency: e.target.value }))}>
                  <option value="Low">Low</option>
                  <option value="Normal">Normal</option>
                  <option value="High">High</option>
                  <option value="Critical">Critical</option>
                </select>
              </div>
              <div style={{ display: 'flex', gap: 10 }}>
                <button type="button" onClick={() => setShowReport(false)} className="btn-ghost" style={{ flex: 1 }}>Cancel</button>
                <button type="submit" disabled={busy === 'report'} style={{ flex: 1, padding: '13px', background: '#DC2626', color: 'white', border: 'none', borderRadius: 12, fontSize: 14, fontWeight: 700, cursor: 'pointer' }}>
                  {busy === 'report' ? 'Reporting…' : 'Report Issue'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Resolve Modal */}
      {resolveId && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.45)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 50, padding: 16 }}>
          <div className="lux-card" style={{ padding: 28, width: '100%', maxWidth: 400 }}>
            <h3 style={{ fontSize: 17, fontWeight: 800, color: '#0B1930', marginBottom: 6 }}>Resolve Issue</h3>
            <p style={{ fontSize: 13.5, color: '#64748B', marginBottom: 18 }}>Add notes on what was done (optional)</p>
            <label className="lux-label">Resolution Notes</label>
            <textarea className="lux-input" rows={3} placeholder="What was done to fix it?" value={notes} onChange={e => setNotes(e.target.value)} style={{ resize: 'none', marginBottom: 20 }} />
            <div style={{ display: 'flex', gap: 10 }}>
              <button onClick={() => { setResolveId(null); setNotes('') }} className="btn-ghost" style={{ flex: 1 }}>Cancel</button>
              <button onClick={handleResolve} disabled={busy === resolveId} className="btn-emerald" style={{ flex: 1 }}>
                {busy === resolveId ? 'Resolving…' : 'Mark Resolved'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
