import { useState, useEffect } from 'react'
import { getOrders, advanceOrderStatus } from '../../api/client'
import toast from 'react-hot-toast'

// Backend OrderStatus enum: Received=1, Preparing=2, Delivering=3, Delivered=4
// DTO returns: statusStep (int 1-4), lineItems ([{itemName,quantity,unitPrice,lineTotal}]), createdAt, totalPrice
const STATUS = { 1: 'Received', 2: 'Preparing', 3: 'Delivering', 4: 'Delivered' }
const STATUS_STYLE = {
  1: { bg: '#FFFBEB', border: '#FDE68A', color: '#92400E', dot: '#F59E0B' },
  2: { bg: '#EFF6FF', border: '#BFDBFE', color: '#1E40AF', dot: '#3B82F6' },
  3: { bg: '#F5F3FF', border: '#DDD6FE', color: '#5B21B6', dot: '#8B5CF6' },
  4: { bg: '#ECFDF5', border: '#A7F3D0', color: '#065F46', dot: '#10B981' },
}
const PIPE_STEPS = ['Received', 'Preparing', 'Delivering', 'Delivered']

// Filter sends integer enum values matching backend OrderStatus
const FILTERS = [['', 'All'], ['1', 'Received'], ['2', 'Preparing'], ['3', 'Delivering'], ['4', 'Delivered']]

export default function OrdersPage() {
  const [orders, setOrders] = useState([])
  const [loading, setLoading] = useState(true)
  const [filter, setFilter] = useState('')
  const [busy, setBusy] = useState(null)

  const load = async () => {
    setLoading(true)
    try {
      setOrders(await getOrders(filter !== '' ? Number(filter) : undefined) || [])
    } catch { toast.error('Failed to load orders') }
    finally { setLoading(false) }
  }

  useEffect(() => { load() }, [filter])

  const handleAdvance = async (id) => {
    setBusy(id)
    try {
      await advanceOrderStatus(id)
      toast.success('Status advanced')
      load()
    } catch (err) { toast.error(err.response?.data?.description || err.response?.data?.Description || 'Failed') }
    finally { setBusy(null) }
  }

  // Use statusStep (1-4) for counts
  const counts = [1, 2, 3, 4].reduce((a, s) => { a[s] = orders.filter(o => o.statusStep === s).length; return a }, {})

  return (
    <div>
      <div style={{ display: 'flex', alignItems: 'flex-end', justifyContent: 'space-between', marginBottom: 28 }}>
        <div>
          <h1 className="page-title">Room Service Orders</h1>
          <p className="page-sub">Manage and advance order pipeline</p>
        </div>
        <button onClick={load} className="btn-ghost" style={{ padding: '9px 18px', fontSize: 13 }}>↻ Refresh</button>
      </div>

      {/* Pipeline Stats */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4,1fr)', gap: 14, marginBottom: 24 }}>
        {[
          { l: 'Received', v: counts[1], c: '#D97706', bg: '#FFFBEB' },
          { l: 'Preparing', v: counts[2], c: '#2563EB', bg: '#EFF6FF' },
          { l: 'Delivering', v: counts[3], c: '#7C3AED', bg: '#F5F3FF' },
          { l: 'Delivered', v: counts[4], c: '#059669', bg: '#ECFDF5' },
        ].map(s => (
          <div key={s.l} className="lux-card" style={{ padding: '18px 22px', textAlign: 'center' }}>
            <p style={{ fontSize: 30, fontWeight: 900, color: s.c, lineHeight: 1 }}>{s.v ?? 0}</p>
            <p style={{ fontSize: 12.5, color: '#64748B', marginTop: 5, fontWeight: 600 }}>{s.l}</p>
          </div>
        ))}
      </div>

      {/* Filters */}
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

      {loading && <div style={{ textAlign: 'center', padding: '60px 0', color: '#94A3B8' }}>Loading orders…</div>}
      {!loading && orders.length === 0 && (
        <div style={{ textAlign: 'center', padding: '60px 0' }}>
          <p style={{ fontSize: 32, marginBottom: 10 }}>🍽️</p>
          <p style={{ fontSize: 15, fontWeight: 600, color: '#64748B' }}>No orders found</p>
        </div>
      )}

      {!loading && orders.length > 0 && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
          {orders.map(order => {
            // Use statusStep (1-4) for display
            const step = order.statusStep ?? 1
            const ss = STATUS_STYLE[step] ?? STATUS_STYLE[1]
            // Progress bar: step 1=25%, 2=50%, 3=75%, 4=100%
            const pct = step * 25
            return (
              <div key={order.id} className="lux-card" style={{ padding: '20px 24px' }}>
                <div style={{ display: 'flex', alignItems: 'flex-start', gap: 16 }}>
                  <div style={{ width: 48, height: 48, background: ss.bg, border: `1px solid ${ss.border}`, borderRadius: 12, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 22, flexShrink: 0 }}>🍽️</div>
                  <div style={{ flex: 1, minWidth: 0 }}>
                    <div style={{ display: 'flex', alignItems: 'center', flexWrap: 'wrap', gap: 8, marginBottom: 8 }}>
                      <span style={{ fontSize: 15, fontWeight: 800, color: '#0B1930' }}>Room {order.roomNumber || '—'}</span>
                      <span style={{ padding: '2px 10px', borderRadius: 20, fontSize: 11.5, fontWeight: 700, background: ss.bg, border: `1px solid ${ss.border}`, color: ss.color }}>{STATUS[step]}</span>
                    </div>
                    {/* Progress bar */}
                    <div style={{ height: 4, background: '#F0EDE7', borderRadius: 4, marginBottom: 10 }}>
                      <div style={{ height: 4, background: ss.dot, borderRadius: 4, width: `${pct}%`, transition: 'width 0.5s ease' }} />
                    </div>
                    {/* Pipeline steps */}
                    <div style={{ display: 'flex', gap: 0, marginBottom: 10 }}>
                      {PIPE_STEPS.map((stepLabel, i) => {
                        const stepNum = i + 1  // 1-4
                        return (
                          <div key={stepLabel} style={{ display: 'flex', alignItems: 'center', flex: i < PIPE_STEPS.length - 1 ? 1 : 'none' }}>
                            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 3 }}>
                              <div style={{ width: 18, height: 18, borderRadius: '50%', background: stepNum <= step ? ss.dot : '#E8E6E1', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                                {stepNum < step && <span style={{ fontSize: 9, color: 'white' }}>✓</span>}
                                {stepNum === step && <span style={{ width: 6, height: 6, borderRadius: '50%', background: 'white', display: 'block' }} />}
                              </div>
                              <span style={{ fontSize: 9.5, color: stepNum <= step ? ss.color : '#94A3B8', fontWeight: 700, whiteSpace: 'nowrap' }}>{stepLabel}</span>
                            </div>
                            {i < PIPE_STEPS.length - 1 && <div style={{ flex: 1, height: 2, background: stepNum < step ? ss.dot : '#E8E6E1', margin: '0 6px', marginBottom: 14 }} />}
                          </div>
                        )
                      })}
                    </div>
                    {/* Order items — backend field is lineItems, each has itemName */}
                    {order.lineItems?.length > 0 && (
                      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6 }}>
                        {order.lineItems.map((item, i) => (
                          <span key={i} style={{ padding: '3px 10px', background: '#F7F6F3', border: '1px solid #E8E6E1', borderRadius: 8, fontSize: 12, color: '#374151', fontWeight: 500 }}>
                            {item.itemName || 'Item'} ×{item.quantity}
                          </span>
                        ))}
                      </div>
                    )}
                    {/* Backend fields: createdAt, totalPrice */}
                    {order.createdAt && (
                      <p style={{ fontSize: 11.5, color: '#94A3B8', marginTop: 8 }}>
                        {new Date(order.createdAt).toLocaleString()}
                        {order.totalPrice ? ` · $${Number(order.totalPrice).toFixed(0)}` : ''}
                      </p>
                    )}
                  </div>
                  <div style={{ flexShrink: 0 }}>
                    {step < 4 ? (
                      <button onClick={() => handleAdvance(order.id)} disabled={busy === order.id} className="btn-navy" style={{ padding: '8px 18px', fontSize: 13 }}>
                        {busy === order.id ? '…' : 'Advance →'}
                      </button>
                    ) : (
                      <span style={{ fontSize: 13, fontWeight: 700, color: '#059669' }}>✓ Delivered</span>
                    )}
                  </div>
                </div>
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}
