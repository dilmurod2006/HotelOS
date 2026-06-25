import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { getAvailableRooms } from '../../api/client'
import toast from 'react-hot-toast'

const TYPE_NAMES = { 0: 'Standard', 1: 'Deluxe', 2: 'Suite', 3: 'Presidential' }
const BASE_PRICES = { 0: 89, 1: 149, 2: 249, 3: 499 }
const TYPE_GRADIENTS = {
  0: 'linear-gradient(135deg, #1E293B, #334155)',
  1: 'linear-gradient(135deg, #1E3A5F, #2563EB)',
  2: 'linear-gradient(135deg, #3B1C6E, #7C3AED)',
  3: 'linear-gradient(135deg, #7A4010, #C49A3C)',
}

export default function GuestRooms() {
  const navigate = useNavigate()
  const [rooms, setRooms] = useState([])
  const [loading, setLoading] = useState(true)
  const [filters, setFilters] = useState({ roomType: '', floor: '' })

  const fetchRooms = async () => {
    setLoading(true)
    try {
      const params = {}
      if (filters.roomType !== '') params.roomType = Number(filters.roomType)
      if (filters.floor !== '') params.floor = Number(filters.floor)
      setRooms(await getAvailableRooms(params))
    } catch {
      toast.error('Could not load rooms')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { fetchRooms() }, [])

  return (
    <div>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'flex-end', justifyContent: 'space-between', marginBottom: 28 }}>
        <div>
          <h1 className="page-title">Available Rooms</h1>
          <p className="page-sub">Choose your perfect room for your stay</p>
        </div>
        <span style={{
          padding: '5px 14px',
          background: '#ECFDF5',
          color: '#059669',
          borderRadius: 20,
          fontSize: 12.5,
          fontWeight: 700,
        }}>
          {rooms.length} available
        </span>
      </div>

      {/* Filters */}
      <div style={{
        background: '#fff',
        border: '1px solid #F0EDE7',
        borderRadius: 16,
        padding: '16px 20px',
        display: 'flex',
        gap: 12,
        alignItems: 'flex-end',
        marginBottom: 28,
        boxShadow: '0 1px 4px rgba(11,25,48,0.04)',
      }}>
        <div>
          <label className="lux-label">Room Type</label>
          <select
            className="lux-input"
            style={{ width: 160 }}
            value={filters.roomType}
            onChange={e => setFilters(p => ({ ...p, roomType: e.target.value }))}
          >
            <option value="">All Types</option>
            <option value="0">Standard</option>
            <option value="1">Deluxe</option>
            <option value="2">Suite</option>
            <option value="3">Presidential</option>
          </select>
        </div>
        <div>
          <label className="lux-label">Floor</label>
          <select
            className="lux-input"
            style={{ width: 140 }}
            value={filters.floor}
            onChange={e => setFilters(p => ({ ...p, floor: e.target.value }))}
          >
            <option value="">All Floors</option>
            {[1,2,3,4,5].map(f => <option key={f} value={f}>Floor {f}</option>)}
          </select>
        </div>
        <button onClick={fetchRooms} className="btn-navy" style={{ padding: '11px 22px' }}>
          Search Rooms
        </button>
      </div>

      {/* Loading */}
      {loading && (
        <div style={{ display: 'flex', justifyContent: 'center', padding: '80px 0' }}>
          <div style={{ textAlign: 'center' }}>
            <div style={{
              width: 40, height: 40, border: '3px solid #F0EDE7',
              borderTopColor: '#C49A3C', borderRadius: '50%',
              animation: 'spin 0.8s linear infinite',
              margin: '0 auto 12px',
            }} />
            <p style={{ fontSize: 13.5, color: '#94A3B8' }}>Loading rooms…</p>
          </div>
          <style>{`@keyframes spin { to { transform: rotate(360deg) } }`}</style>
        </div>
      )}

      {/* Empty */}
      {!loading && rooms.length === 0 && (
        <div style={{ textAlign: 'center', padding: '80px 0', color: '#94A3B8' }}>
          <div style={{ fontSize: 40, marginBottom: 12 }}>🛏️</div>
          <p style={{ fontSize: 15, fontWeight: 600, color: '#64748B' }}>No rooms available</p>
          <p style={{ fontSize: 13.5, marginTop: 6 }}>Try changing your filters</p>
        </div>
      )}

      {/* Grid */}
      {!loading && rooms.length > 0 && (
        <div style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))',
          gap: 20,
        }}>
          {rooms.map(room => (
            <div
              key={room.id}
              style={{
                background: '#fff',
                borderRadius: 20,
                border: '1px solid #F0EDE7',
                overflow: 'hidden',
                boxShadow: '0 2px 12px rgba(11,25,48,0.05)',
                transition: 'transform 0.2s, box-shadow 0.2s',
                cursor: 'default',
              }}
              onMouseEnter={e => { e.currentTarget.style.transform = 'translateY(-3px)'; e.currentTarget.style.boxShadow = '0 12px 32px rgba(11,25,48,0.12)' }}
              onMouseLeave={e => { e.currentTarget.style.transform = 'translateY(0)'; e.currentTarget.style.boxShadow = '0 2px 12px rgba(11,25,48,0.05)' }}
            >
              {/* Visual header */}
              <div style={{
                height: 140,
                background: TYPE_GRADIENTS[room.type] ?? TYPE_GRADIENTS[0],
                display: 'flex', alignItems: 'center', justifyContent: 'center',
                position: 'relative',
              }}>
                <span style={{ fontSize: 40, opacity: 0.25 }}>🏨</span>
                <span style={{
                  position: 'absolute', top: 12, right: 12,
                  background: 'rgba(255,255,255,0.15)',
                  color: 'white',
                  fontSize: 11.5, fontWeight: 700,
                  padding: '3px 10px', borderRadius: 12,
                  backdropFilter: 'blur(4px)',
                }}>
                  {TYPE_NAMES[room.type] ?? 'Room'}
                </span>
              </div>

              {/* Info */}
              <div style={{ padding: '20px 22px 22px' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 12 }}>
                  <div>
                    <h3 style={{ fontSize: 20, fontWeight: 800, color: '#0B1930', lineHeight: 1.1 }}>
                      Room {room.roomNumber}
                    </h3>
                    <p style={{ fontSize: 12.5, color: '#94A3B8', marginTop: 3 }}>
                      Floor {room.floor} · {TYPE_NAMES[room.type] ?? 'Standard'}
                    </p>
                  </div>
                  <div style={{ textAlign: 'right' }}>
                    <p style={{ fontSize: 22, fontWeight: 800, color: '#C49A3C', lineHeight: 1 }}>
                      ${BASE_PRICES[room.type] ?? 89}
                    </p>
                    <p style={{ fontSize: 11, color: '#94A3B8', marginTop: 2 }}>per night</p>
                  </div>
                </div>

                {/* Stars */}
                <div style={{ display: 'flex', gap: 3, marginBottom: 18 }}>
                  {[1,2,3,4,5].map(s => (
                    <svg key={s} width="13" height="13" viewBox="0 0 24 24" fill={s <= (room.type + 3) ? '#C49A3C' : '#E8E6E1'}>
                      <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z" />
                    </svg>
                  ))}
                  <span style={{ fontSize: 11.5, color: '#94A3B8', marginLeft: 4 }}>
                    {room.type + 3}-star
                  </span>
                </div>

                <button
                  onClick={() => navigate(`/guest/book/${room.id}`)}
                  className="btn-gold"
                  style={{ width: '100%', fontSize: 14 }}
                >
                  Book This Room
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
