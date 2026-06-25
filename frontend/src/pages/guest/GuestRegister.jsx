import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuthStore } from '../../store/authStore'
import { registerGuest } from '../../api/client'
import toast from 'react-hot-toast'

export default function GuestRegister() {
  const navigate = useNavigate()
  const { setGuestProfile, guestId } = useAuthStore()
  const [form, setForm] = useState({ firstName: '', lastName: '', email: '', phoneNumber: '' })
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    if (guestId) navigate('/guest/rooms', { replace: true })
  }, [guestId, navigate])

  const set = (key) => (e) => setForm(p => ({ ...p, [key]: e.target.value }))

  const handleSubmit = async (e) => {
    e.preventDefault()
    setLoading(true)
    try {
      const guest = await registerGuest(form)
      setGuestProfile(guest.id, `${form.firstName} ${form.lastName}`)
      toast.success(`Welcome, ${form.firstName}!`)
      navigate('/guest/rooms')
    } catch (err) {
      toast.error(err.response?.data?.description || 'Registration failed')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div style={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'center', paddingTop: 32, width: '100%' }}>
      <div style={{ width: '100%', maxWidth: 480 }}>
        {/* Header */}
        <div style={{ textAlign: 'center', marginBottom: 32 }}>
          <div style={{
            width: 60, height: 60,
            background: 'linear-gradient(135deg, #0B1930, #1A3057)',
            borderRadius: 16,
            display: 'flex', alignItems: 'center', justifyContent: 'center',
            margin: '0 auto 16px',
            boxShadow: '0 8px 24px rgba(11,25,48,0.25)',
          }}>
            <svg width="26" height="26" fill="none" stroke="white" strokeWidth="1.6" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 6a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0zM4.501 20.118a7.5 7.5 0 0114.998 0A17.933 17.933 0 0112 21.75c-2.676 0-5.216-.584-7.499-1.632z" />
            </svg>
          </div>
          <h1 style={{ fontSize: 24, fontWeight: 800, color: '#0B1930', marginBottom: 6 }}>
            Guest Registration
          </h1>
          <p style={{ fontSize: 14, color: '#64748B', lineHeight: 1.5 }}>
            Create your profile to access all GrandStay services
          </p>
        </div>

        {/* Card */}
        <div style={{
          background: '#FFFFFF',
          borderRadius: 20,
          border: '1px solid #F0EDE7',
          boxShadow: '0 4px 32px rgba(11,25,48,0.08)',
          padding: 32,
        }}>
          <form onSubmit={handleSubmit}>
            {/* Name row */}
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14, marginBottom: 14 }}>
              <div>
                <label className="lux-label">First Name</label>
                <input
                  className="lux-input"
                  type="text" required
                  placeholder="Ali"
                  value={form.firstName}
                  onChange={set('firstName')}
                />
              </div>
              <div>
                <label className="lux-label">Last Name</label>
                <input
                  className="lux-input"
                  type="text" required
                  placeholder="Valiyev"
                  value={form.lastName}
                  onChange={set('lastName')}
                />
              </div>
            </div>

            <div style={{ marginBottom: 14 }}>
              <label className="lux-label">Email Address</label>
              <input
                className="lux-input"
                type="email" required
                placeholder="ali@example.com"
                value={form.email}
                onChange={set('email')}
              />
            </div>

            <div style={{ marginBottom: 28 }}>
              <label className="lux-label">Phone Number</label>
              <input
                className="lux-input"
                type="tel" required
                placeholder="+998 90 123 45 67"
                value={form.phoneNumber}
                onChange={set('phoneNumber')}
              />
            </div>

            <button
              type="submit"
              disabled={loading}
              className="btn-gold"
              style={{ width: '100%', fontSize: 15 }}
            >
              {loading ? 'Creating profile…' : 'Continue to Rooms →'}
            </button>
          </form>
        </div>

        {/* Info */}
        <p style={{ textAlign: 'center', marginTop: 20, fontSize: 12.5, color: '#94A3B8' }}>
          Your information is used only for room bookings and service requests.
        </p>
      </div>
    </div>
  )
}
