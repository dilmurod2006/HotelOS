import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuthStore } from '../../store/authStore'
import toast from 'react-hot-toast'

export default function StaffLogin() {
  const navigate = useNavigate()
  const { signIn, loading } = useAuthStore()
  const [name, setName] = useState('')
  const [role, setRole] = useState('Staff')

  const handleLogin = async (e) => {
    e.preventDefault()
    const ok = await signIn(role, name || role)
    if (ok) { toast.success(`Welcome, ${name || role}!`); navigate('/staff/dashboard') }
    else toast.error('Authentication failed')
  }

  return (
    <div
      className="min-h-screen flex items-center justify-center px-4"
      style={{ background: 'linear-gradient(160deg, #021015 0%, #0B2A1F 50%, #021015 100%)' }}
    >
      <div style={{ width: '100%', maxWidth: 400 }}>
        {/* Logo */}
        <div style={{ textAlign: 'center', marginBottom: 36 }}>
          <div style={{
            width: 64, height: 64,
            background: 'linear-gradient(135deg, #059669, #10B981)',
            borderRadius: 18,
            display: 'flex', alignItems: 'center', justifyContent: 'center',
            margin: '0 auto 16px',
            boxShadow: '0 12px 32px rgba(5,150,105,0.35)',
          }}>
            <svg width="28" height="28" fill="none" stroke="white" strokeWidth="1.6" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M3.75 21h16.5M4.5 3h15M5.25 3v18m13.5-18v18M9 6.75h1.5m-1.5 3h1.5m-1.5 3h1.5m3-6H15m-1.5 3H15m-1.5 3H15M9 21v-3.375c0-.621.504-1.125 1.125-1.125h3.75c.621 0 1.125.504 1.125 1.125V21" />
            </svg>
          </div>
          <h1 style={{ fontSize: 26, fontWeight: 800, color: 'white', marginBottom: 6 }}>
            Staff Portal
          </h1>
          <p style={{ fontSize: 13.5, color: 'rgba(255,255,255,0.4)' }}>
            GrandStay Hotel Operations
          </p>
        </div>

        {/* Card */}
        <div style={{
          background: 'rgba(255,255,255,0.04)',
          border: '1px solid rgba(255,255,255,0.08)',
          borderRadius: 22,
          padding: 32,
          backdropFilter: 'blur(12px)',
        }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 24 }}>
            <div style={{
              width: 28, height: 28,
              background: 'rgba(5,150,105,0.2)',
              borderRadius: 8,
              display: 'flex', alignItems: 'center', justifyContent: 'center',
            }}>
              <svg width="14" height="14" fill="none" stroke="#10B981" strokeWidth="2" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z" />
              </svg>
            </div>
            <span style={{ fontSize: 14, fontWeight: 700, color: 'white' }}>Secure Staff Login</span>
          </div>

          <form onSubmit={handleLogin}>
            <div style={{ marginBottom: 14 }}>
              <label style={{ display: 'block', fontSize: 11.5, fontWeight: 700, color: 'rgba(255,255,255,0.45)', textTransform: 'uppercase', letterSpacing: '0.07em', marginBottom: 7 }}>
                Your Name
              </label>
              <input
                className="lux-input-dark"
                type="text"
                placeholder="e.g. Sarah Johnson"
                value={name}
                onChange={e => setName(e.target.value)}
              />
            </div>

            <div style={{ marginBottom: 28 }}>
              <label style={{ display: 'block', fontSize: 11.5, fontWeight: 700, color: 'rgba(255,255,255,0.45)', textTransform: 'uppercase', letterSpacing: '0.07em', marginBottom: 7 }}>
                Access Level
              </label>
              <select
                className="lux-input-dark"
                value={role}
                onChange={e => setRole(e.target.value)}
                style={{ cursor: 'pointer' }}
              >
                <option value="Staff" style={{ background: '#0B2A1F', color: 'white' }}>Staff</option>
                <option value="Admin" style={{ background: '#0B2A1F', color: 'white' }}>Admin</option>
              </select>
            </div>

            <button type="submit" disabled={loading} className="btn-emerald" style={{ width: '100%', fontSize: 15 }}>
              {loading ? 'Signing in…' : 'Sign In to Dashboard'}
            </button>
          </form>

          <div style={{ marginTop: 24, paddingTop: 20, borderTop: '1px solid rgba(255,255,255,0.07)', textAlign: 'center' }}>
            <p style={{ fontSize: 13, color: 'rgba(255,255,255,0.3)' }}>
              Guest?{' '}
              <a href="/" style={{ color: '#C49A3C', textDecoration: 'none', fontWeight: 600 }}>
                Go to Guest Portal →
              </a>
            </p>
          </div>
        </div>

        <p style={{ textAlign: 'center', marginTop: 24, fontSize: 12, color: 'rgba(255,255,255,0.15)' }}>
          HotelOS · GrandStay Hotel Management System
        </p>
      </div>
    </div>
  )
}
