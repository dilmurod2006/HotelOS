import { useEffect, useState, useRef } from 'react'
import * as signalR from '@microsoft/signalr'
import { useAuthStore } from '../../store/authStore'

const MOCK_ROOMS = [
  { id:'1', roomNumber:'101', floor:1, type:'Standard',     status:'Available' },
  { id:'2', roomNumber:'102', floor:1, type:'Standard',     status:'Occupied' },
  { id:'3', roomNumber:'103', floor:1, type:'Deluxe',       status:'NeedsHousekeeping' },
  { id:'4', roomNumber:'201', floor:2, type:'Deluxe',       status:'Available' },
  { id:'5', roomNumber:'202', floor:2, type:'Suite',        status:'Occupied' },
  { id:'6', roomNumber:'203', floor:2, type:'Standard',     status:'Maintenance' },
  { id:'7', roomNumber:'301', floor:3, type:'Suite',        status:'Available' },
  { id:'8', roomNumber:'302', floor:3, type:'Presidential', status:'Occupied' },
  { id:'9', roomNumber:'401', floor:4, type:'Presidential', status:'Available' },
]

const STATUS_STYLE = {
  Available:         { bg:'#ECFDF5', border:'#A7F3D0', color:'#065F46', dot:'#10B981' },
  Occupied:          { bg:'#EFF6FF', border:'#BFDBFE', color:'#1E40AF', dot:'#3B82F6' },
  NeedsHousekeeping: { bg:'#FFFBEB', border:'#FDE68A', color:'#92400E', dot:'#F59E0B' },
  Maintenance:       { bg:'#FEF2F2', border:'#FECACA', color:'#991B1B', dot:'#EF4444' },
  OutOfOrder:        { bg:'#F8FAFC', border:'#E2E8F0', color:'#475569', dot:'#94A3B8' },
}

const STATS = [
  { key:'Available', label:'Available', icon:'🏠', color:'#059669', bg:'#ECFDF5' },
  { key:'Occupied',  label:'Occupied',  icon:'👤', color:'#2563EB', bg:'#EFF6FF' },
  { key:'NeedsHousekeeping', label:'Housekeeping', icon:'✨', color:'#D97706', bg:'#FFFBEB' },
  { key:'Maintenance', label:'Maintenance', icon:'🔧', color:'#DC2626', bg:'#FEF2F2' },
]

export default function StaffDashboard() {
  const { token } = useAuthStore()
  const [rooms, setRooms] = useState(MOCK_ROOMS)
  const [connected, setConnected] = useState(false)
  const [events, setEvents] = useState([])
  const [lastUpdate, setLastUpdate] = useState(null)

  useEffect(() => {
    // Don't attempt SignalR connection without a valid token — would cause 401 loop
    if (!token) return

    const hub = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/dashboard', { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    hub.on('RoomStatusChanged', d => {
      setRooms(prev => prev.map(r => r.id === d.roomId ? { ...r, status: d.newStatus } : r))
      setLastUpdate(new Date())
      setEvents(prev => [{ id: Date.now(), msg: `Room ${d.roomNumber}: ${d.oldStatus} → ${d.newStatus}`, t: new Date() }, ...prev.slice(0, 9)])
    })

    hub.on('BookingCreated', d => {
      setEvents(prev => [{ id: Date.now(), msg: `New booking: Room ${d.roomNumber}`, t: new Date() }, ...prev.slice(0, 9)])
    })

    hub.start().then(() => setConnected(true)).catch(() => setConnected(false))
    return () => hub.stop()
  }, [token])

  const byFloor = rooms.reduce((a, r) => { (a[r.floor] = a[r.floor] || []).push(r); return a }, {})
  const counts = STATS.reduce((a, s) => { a[s.key] = rooms.filter(r => r.status === s.key).length; return a }, {})

  return (
    <div>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'flex-end', justifyContent: 'space-between', marginBottom: 28 }}>
        <div>
          <h1 className="page-title">Live Room Dashboard</h1>
          <p className="page-sub">Real-time status via SignalR WebSocket</p>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '7px 14px', background: connected ? '#ECFDF5' : '#FEF2F2', borderRadius: 20, border: `1px solid ${connected ? '#A7F3D0' : '#FECACA'}` }}>
          <span style={{ width: 7, height: 7, borderRadius: '50%', background: connected ? '#10B981' : '#EF4444', animation: connected ? 'pulse 2s infinite' : 'none' }} />
          <span style={{ fontSize: 12.5, fontWeight: 700, color: connected ? '#065F46' : '#991B1B' }}>
            {connected ? 'Live' : 'Disconnected'}
          </span>
          {lastUpdate && <span style={{ fontSize: 11, color: '#64748B' }}>{lastUpdate.toLocaleTimeString()}</span>}
        </div>
        <style>{`@keyframes pulse { 0%,100%{opacity:1} 50%{opacity:0.4} }`}</style>
      </div>

      {/* Stats */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 14, marginBottom: 28 }}>
        {STATS.map(s => (
          <div key={s.key} className="lux-card" style={{ padding: '20px 22px' }}>
            <div style={{
              width: 42, height: 42, background: s.bg, borderRadius: 12,
              display: 'flex', alignItems: 'center', justifyContent: 'center',
              fontSize: 20, marginBottom: 12,
            }}>{s.icon}</div>
            <p style={{ fontSize: 28, fontWeight: 900, color: s.color, lineHeight: 1 }}>{counts[s.key] ?? 0}</p>
            <p style={{ fontSize: 12.5, color: '#64748B', marginTop: 4, fontWeight: 600 }}>{s.label}</p>
          </div>
        ))}
      </div>

      {/* Room grid + Event feed */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 280px', gap: 20 }}>
        {/* Room Grid */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
          {Object.keys(byFloor).sort().map(floor => (
            <div key={floor} className="lux-card" style={{ padding: '20px 22px' }}>
              <p style={{ fontSize: 12, fontWeight: 700, color: '#94A3B8', textTransform: 'uppercase', letterSpacing: '0.07em', marginBottom: 14 }}>
                Floor {floor}
              </p>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(120px, 1fr))', gap: 10 }}>
                {byFloor[floor].map(room => {
                  const s = STATUS_STYLE[room.status] || STATUS_STYLE.Available
                  return (
                    <div key={room.id} style={{
                      background: s.bg,
                      border: `1px solid ${s.border}`,
                      borderRadius: 12, padding: '12px 10px',
                      textAlign: 'center',
                    }}>
                      <div style={{ display: 'flex', justifyContent: 'center', marginBottom: 6 }}>
                        <span style={{ width: 7, height: 7, borderRadius: '50%', background: s.dot }} />
                      </div>
                      <p style={{ fontSize: 16, fontWeight: 800, color: s.color, lineHeight: 1 }}>{room.roomNumber}</p>
                      <p style={{ fontSize: 10, color: s.color, opacity: 0.7, marginTop: 3 }}>{room.type}</p>
                      <p style={{ fontSize: 10, fontWeight: 700, color: s.color, marginTop: 4 }}>
                        {room.status === 'NeedsHousekeeping' ? 'Cleaning' : room.status}
                      </p>
                    </div>
                  )
                })}
              </div>
            </div>
          ))}
        </div>

        {/* Event Feed */}
        <div className="lux-card" style={{ padding: '20px 20px', height: 'fit-content', position: 'sticky', top: 90 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 16 }}>
            <span style={{ width: 8, height: 8, borderRadius: '50%', background: connected ? '#10B981' : '#94A3B8', animation: connected ? 'pulse 2s infinite' : 'none' }} />
            <h3 style={{ fontSize: 13.5, fontWeight: 800, color: '#0B1930' }}>Live Events</h3>
          </div>

          {events.length === 0 ? (
            <div style={{ textAlign: 'center', padding: '24px 0', color: '#94A3B8' }}>
              <p style={{ fontSize: 22, marginBottom: 8 }}>📡</p>
              <p style={{ fontSize: 13, fontWeight: 500 }}>Awaiting events…</p>
              <p style={{ fontSize: 11.5, marginTop: 4 }}>Room changes appear here</p>
            </div>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
              {events.map(ev => (
                <div key={ev.id} style={{ background: '#F7F6F3', borderRadius: 10, padding: '10px 12px' }}>
                  <p style={{ fontSize: 12.5, color: '#374151', lineHeight: 1.4 }}>{ev.msg}</p>
                  <p style={{ fontSize: 11, color: '#94A3B8', marginTop: 3 }}>{ev.t.toLocaleTimeString()}</p>
                </div>
              ))}
            </div>
          )}

          {/* Legend */}
          <div style={{ marginTop: 20, paddingTop: 16, borderTop: '1px solid #F0EDE7' }}>
            <p style={{ fontSize: 10.5, fontWeight: 700, color: '#94A3B8', textTransform: 'uppercase', letterSpacing: '0.06em', marginBottom: 10 }}>Status Legend</p>
            {Object.entries(STATUS_STYLE).map(([status, s]) => (
              <div key={status} style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 6 }}>
                <span style={{ width: 8, height: 8, borderRadius: '50%', background: s.dot, flexShrink: 0 }} />
                <span style={{ fontSize: 12, color: '#64748B', fontWeight: 500 }}>
                  {status === 'NeedsHousekeeping' ? 'Needs Cleaning' : status}
                </span>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  )
}
