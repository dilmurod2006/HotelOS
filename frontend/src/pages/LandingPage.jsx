import { useNavigate } from 'react-router-dom'
import { useAuthStore } from '../store/authStore'
import toast from 'react-hot-toast'

export default function LandingPage() {
  const navigate = useNavigate()
  const { signIn, loading } = useAuthStore()

  const handleGuest = async () => {
    const ok = await signIn('Guest', 'Guest User')
    if (ok) navigate('/guest/register')
    else toast.error('Connection error')
  }

  return (
    <div
      className="min-h-screen flex flex-col"
      style={{ background: 'linear-gradient(160deg, #04101F 0%, #0C2040 45%, #04101F 100%)' }}
    >
      {/* Top bar */}
      <header className="flex justify-between items-center px-8 py-5">
        <div className="flex items-center gap-3">
          <div
            className="w-10 h-10 rounded-xl flex items-center justify-center"
            style={{ background: 'linear-gradient(135deg, #C49A3C, #E8C578)' }}
          >
            <svg width="20" height="20" fill="none" stroke="white" strokeWidth="1.8" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M3.75 21h16.5M4.5 3h15M5.25 3v18m13.5-18v18M9 6.75h1.5m-1.5 3h1.5m-1.5 3h1.5m3-6H15m-1.5 3H15m-1.5 3H15M9 21v-3.375c0-.621.504-1.125 1.125-1.125h3.75c.621 0 1.125.504 1.125 1.125V21" />
            </svg>
          </div>
          <div>
            <p className="font-bold text-white text-base leading-tight">HotelOS</p>
            <p className="text-xs leading-tight" style={{ color: '#C49A3C' }}>GrandStay · Management</p>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={() => navigate('/staff/login')}
            className="px-5 py-2 text-sm font-medium text-white rounded-xl transition-all"
            style={{ border: '1px solid rgba(255,255,255,0.15)' }}
            onMouseEnter={e => e.currentTarget.style.background = 'rgba(255,255,255,0.07)'}
            onMouseLeave={e => e.currentTarget.style.background = 'transparent'}
          >
            Staff Login
          </button>
          <button
            onClick={handleGuest}
            disabled={loading}
            className="px-5 py-2 text-sm font-semibold text-white rounded-xl transition-all disabled:opacity-40"
            style={{ background: 'linear-gradient(135deg, #C49A3C, #A67D2A)' }}
          >
            {loading ? 'Loading…' : 'Guest Portal'}
          </button>
        </div>
      </header>

      {/* Hero */}
      <main className="flex-1 flex flex-col items-center justify-center px-6 text-center pb-12">
        {/* Gold badge */}
        <div
          className="inline-flex items-center gap-2 px-4 py-1.5 rounded-full mb-10"
          style={{ background: 'rgba(196,154,60,0.1)', border: '1px solid rgba(196,154,60,0.25)' }}
        >
          <span className="w-1.5 h-1.5 rounded-full animate-pulse" style={{ background: '#C49A3C' }} />
          <span className="text-xs font-semibold tracking-wide" style={{ color: '#E8C578' }}>
            Real-time Hotel Management System
          </span>
        </div>

        {/* Main heading */}
        <h1 className="font-black text-white mb-5" style={{ fontSize: 'clamp(48px, 8vw, 86px)', lineHeight: 1.05 }}>
          GrandStay
          <br />
          <span style={{ color: '#C49A3C' }}>Hotel</span>
        </h1>

        <p className="text-base mb-14 max-w-md mx-auto leading-relaxed" style={{ color: 'rgba(255,255,255,0.45)' }}>
          One unified platform connecting Reception, Housekeeping, Maintenance and Room Service with live WebSocket updates.
        </p>

        {/* Portal Cards */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 w-full max-w-xl mb-14">
          {/* Guest */}
          <button
            onClick={handleGuest}
            disabled={loading}
            className="p-7 rounded-2xl text-left transition-all duration-200 disabled:opacity-40 group"
            style={{ background: 'rgba(255,255,255,0.03)', border: '1px solid rgba(255,255,255,0.07)' }}
            onMouseEnter={e => { e.currentTarget.style.background = 'rgba(196,154,60,0.08)'; e.currentTarget.style.borderColor = 'rgba(196,154,60,0.3)'; e.currentTarget.style.transform = 'translateY(-2px)' }}
            onMouseLeave={e => { e.currentTarget.style.background = 'rgba(255,255,255,0.03)'; e.currentTarget.style.borderColor = 'rgba(255,255,255,0.07)'; e.currentTarget.style.transform = 'translateY(0)' }}
          >
            <div className="w-11 h-11 rounded-xl flex items-center justify-center mb-4" style={{ background: 'rgba(196,154,60,0.15)' }}>
              <svg className="w-5 h-5" style={{ color: '#C49A3C' }} fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 6a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0zM4.501 20.118a7.5 7.5 0 0114.998 0A17.933 17.933 0 0112 21.75c-2.676 0-5.216-.584-7.499-1.632z" />
              </svg>
            </div>
            <h3 className="font-bold text-white mb-1.5" style={{ fontSize: 16 }}>Guest Portal</h3>
            <p className="text-xs leading-relaxed mb-4" style={{ color: 'rgba(255,255,255,0.4)' }}>
              Register, browse rooms, make reservations and order room service.
            </p>
            <span className="text-xs font-bold" style={{ color: '#C49A3C' }}>Enter as Guest →</span>
          </button>

          {/* Staff */}
          <button
            onClick={() => navigate('/staff/login')}
            className="p-7 rounded-2xl text-left transition-all duration-200 group"
            style={{ background: 'rgba(255,255,255,0.03)', border: '1px solid rgba(255,255,255,0.07)' }}
            onMouseEnter={e => { e.currentTarget.style.background = 'rgba(5,150,105,0.07)'; e.currentTarget.style.borderColor = 'rgba(16,185,129,0.25)'; e.currentTarget.style.transform = 'translateY(-2px)' }}
            onMouseLeave={e => { e.currentTarget.style.background = 'rgba(255,255,255,0.03)'; e.currentTarget.style.borderColor = 'rgba(255,255,255,0.07)'; e.currentTarget.style.transform = 'translateY(0)' }}
          >
            <div className="w-11 h-11 rounded-xl flex items-center justify-center mb-4" style={{ background: 'rgba(16,185,129,0.12)' }}>
              <svg className="w-5 h-5 text-emerald-400" fill="none" stroke="currentColor" strokeWidth="1.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z" />
              </svg>
            </div>
            <h3 className="font-bold text-white mb-1.5" style={{ fontSize: 16 }}>Staff Dashboard</h3>
            <p className="text-xs leading-relaxed mb-4" style={{ color: 'rgba(255,255,255,0.4)' }}>
              Live room grid, housekeeping, maintenance and order management.
            </p>
            <span className="text-xs font-bold text-emerald-400">Staff Login →</span>
          </button>
        </div>

        {/* Tech badges */}
        <div className="flex flex-wrap justify-center gap-2">
          {['.NET 8', 'PostgreSQL 18', 'Redis', 'SignalR', 'React 19', 'Tailwind CSS'].map(t => (
            <span
              key={t}
              className="px-3 py-1 rounded-full text-xs font-medium"
              style={{ background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.07)', color: 'rgba(255,255,255,0.35)' }}
            >
              {t}
            </span>
          ))}
        </div>
      </main>

      <footer
        className="text-center py-5 text-xs"
        style={{ borderTop: '1px solid rgba(255,255,255,0.05)', color: 'rgba(255,255,255,0.2)' }}
      >
        HotelOS · BTEC Level 4 Digital Technologies · GrandStay Hotel Management System
      </footer>
    </div>
  )
}
