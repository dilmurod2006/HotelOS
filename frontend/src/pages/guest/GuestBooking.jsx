import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuthStore } from '../../store/authStore'
import { createBooking, confirmPayment } from '../../api/client'
import toast from 'react-hot-toast'

// Backend CreateBookingRequest fields:
// { guestId, roomType (enum 0-3), checkIn, checkOut, preferredFloor, proximityPreference (enum 0-2) }

export default function GuestBooking() {
  const navigate = useNavigate()
  const { guestId, setBookingInfo } = useAuthStore()
  const [step, setStep] = useState(0)
  const [booking, setBooking] = useState(null)
  const [form, setForm] = useState({ checkIn: '', checkOut: '', card: '' })
  const [loading, setLoading] = useState(false)

  const nights = (() => {
    if (!form.checkIn || !form.checkOut) return 0
    return Math.max(0, Math.round((new Date(form.checkOut) - new Date(form.checkIn)) / 86400000))
  })()

  const total = nights * 149

  const handleBook = async () => {
    if (!guestId) { toast.error('Please register first'); navigate('/guest/register'); return }
    if (nights <= 0) { toast.error('Check-out must be after check-in'); return }
    setLoading(true)
    try {
      const result = await createBooking({
        guestId,
        roomType: 0,
        checkIn: form.checkIn,
        checkOut: form.checkOut,
        preferredFloor: null,
        proximityPreference: 0,
      })
      setBooking(result)
      setBookingInfo(result.id, result.roomNumber)
      setStep(1)
      toast.success('Room reserved!')
    } catch (err) {
      const msg = err.response?.data?.description || err.response?.data || 'Booking failed'
      toast.error(typeof msg === 'string' ? msg : 'Booking failed')
    } finally {
      setLoading(false)
    }
  }

  const handlePay = async () => {
    if (form.card.length !== 4) { toast.error('Enter 4-digit card suffix'); return }
    setLoading(true)
    try {
      await confirmPayment(booking.id, form.card)
      setStep(2)
    } catch (err) {
      toast.error(err.response?.data?.description || 'Payment failed')
    } finally {
      setLoading(false)
    }
  }

  const Step = ({ n, label }) => (
    <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
      <div style={{
        width: 30, height: 30, borderRadius: '50%',
        background: n < step ? '#059669' : n === step ? '#C49A3C' : '#E8E6E1',
        display: 'flex', alignItems: 'center', justifyContent: 'center',
        fontSize: 12, fontWeight: 800,
        color: n <= step ? 'white' : '#94A3B8',
        flexShrink: 0,
      }}>
        {n < step ? '✓' : n + 1}
      </div>
      <span style={{ fontSize: 13, fontWeight: 600, color: n === step ? '#0B1930' : '#94A3B8' }}>{label}</span>
    </div>
  )

  const Divider = () => <div style={{ flex: 1, height: 1, background: '#E8E6E1', margin: '0 8px' }} />

  // Success screen
  if (step === 2) {
    return (
      <div style={{ maxWidth: 460, margin: '0 auto', textAlign: 'center', paddingTop: 40 }}>
        <div style={{
          width: 72, height: 72,
          background: 'linear-gradient(135deg, #059669, #10B981)',
          borderRadius: '50%',
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          margin: '0 auto 20px',
          boxShadow: '0 12px 32px rgba(5,150,105,0.3)',
        }}>
          <svg width="32" height="32" fill="none" stroke="white" strokeWidth="2.5" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" />
          </svg>
        </div>
        <h2 style={{ fontSize: 26, fontWeight: 800, color: '#0B1930', marginBottom: 8 }}>Booking Confirmed!</h2>
        <p style={{ fontSize: 14, color: '#64748B', marginBottom: 28, lineHeight: 1.6 }}>
          Your reservation is confirmed and payment has been processed successfully.
        </p>
        {booking && (
          <div className="lux-card" style={{ padding: 24, marginBottom: 24, textAlign: 'left' }}>
            {[
              ['Booking ID', booking.id?.slice(0, 8) + '…'],
              ['Room', booking.roomNumber || 'Assigned'],
              ['Check-in', form.checkIn],
              ['Check-out', form.checkOut],
              ['Total Paid', `$${total}`],
            ].map(([k, v]) => (
              <div key={k} style={{ display: 'flex', justifyContent: 'space-between', padding: '8px 0', borderBottom: '1px solid #F7F6F3' }}>
                <span style={{ fontSize: 13, color: '#64748B' }}>{k}</span>
                <span style={{ fontSize: 13, fontWeight: 600, color: '#0B1930' }}>{v}</span>
              </div>
            ))}
          </div>
        )}
        <div style={{ display: 'flex', gap: 12 }}>
          <button onClick={() => navigate('/guest/orders')} className="btn-gold" style={{ flex: 1 }}>
            Order Room Service
          </button>
          <button onClick={() => navigate('/guest/rooms')} className="btn-ghost" style={{ flex: 1 }}>
            Back to Rooms
          </button>
        </div>
      </div>
    )
  }

  return (
    <div style={{ maxWidth: 520, margin: '0 auto' }}>
      {/* Back */}
      <button
        onClick={() => navigate('/guest/rooms')}
        className="btn-ghost"
        style={{ marginBottom: 24, padding: '8px 16px', fontSize: 13 }}
      >
        ← Back to Rooms
      </button>

      {/* Step indicator */}
      <div style={{ display: 'flex', alignItems: 'center', marginBottom: 32 }}>
        <Step n={0} label="Select Dates" />
        <Divider />
        <Step n={1} label="Payment" />
        <Divider />
        <Step n={2} label="Confirmed" />
      </div>

      {/* Card */}
      <div className="lux-card" style={{ padding: 36 }}>
        {/* Step 0: Dates */}
        {step === 0 && (
          <>
            <h2 style={{ fontSize: 20, fontWeight: 800, color: '#0B1930', marginBottom: 6 }}>Select Your Dates</h2>
            <p style={{ fontSize: 13.5, color: '#64748B', marginBottom: 28 }}>Choose your check-in and check-out dates</p>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14, marginBottom: 20 }}>
              <div>
                <label className="lux-label">Check-in</label>
                <input
                  className="lux-input"
                  type="date"
                  min={new Date().toISOString().split('T')[0]}
                  value={form.checkIn}
                  onChange={e => setForm(p => ({ ...p, checkIn: e.target.value }))}
                />
              </div>
              <div>
                <label className="lux-label">Check-out</label>
                <input
                  className="lux-input"
                  type="date"
                  min={form.checkIn || new Date().toISOString().split('T')[0]}
                  value={form.checkOut}
                  onChange={e => setForm(p => ({ ...p, checkOut: e.target.value }))}
                />
              </div>
            </div>

            {nights > 0 && (
              <div style={{
                background: 'linear-gradient(135deg, #FDF8EE, #FAF3D8)',
                border: '1px solid rgba(196,154,60,0.25)',
                borderRadius: 12,
                padding: '14px 18px',
                display: 'flex', justifyContent: 'space-between', alignItems: 'center',
                marginBottom: 28,
              }}>
                <span style={{ fontSize: 14, color: '#92400E', fontWeight: 600 }}>
                  {nights} night{nights > 1 ? 's' : ''} stay
                </span>
                <span style={{ fontSize: 18, fontWeight: 800, color: '#C49A3C' }}>
                  ${total.toLocaleString()}
                </span>
              </div>
            )}

            <button
              onClick={handleBook}
              disabled={loading || nights <= 0}
              className="btn-gold"
              style={{ width: '100%', fontSize: 15 }}
            >
              {loading ? 'Reserving…' : 'Reserve Room'}
            </button>
          </>
        )}

        {/* Step 1: Payment */}
        {step === 1 && booking && (
          <>
            <h2 style={{ fontSize: 20, fontWeight: 800, color: '#0B1930', marginBottom: 6 }}>Confirm Payment</h2>
            <p style={{ fontSize: 13.5, color: '#64748B', marginBottom: 24 }}>
              Room {booking.roomNumber || 'assigned'} has been reserved for you
            </p>

            {/* Summary */}
            <div style={{ background: '#F7F6F3', borderRadius: 14, padding: '18px 20px', marginBottom: 24 }}>
              {[
                ['Check-in', form.checkIn],
                ['Check-out', form.checkOut],
                ['Nights', nights],
              ].map(([k, v]) => (
                <div key={k} style={{ display: 'flex', justifyContent: 'space-between', padding: '6px 0' }}>
                  <span style={{ fontSize: 13, color: '#64748B' }}>{k}</span>
                  <span style={{ fontSize: 13, fontWeight: 600, color: '#0B1930' }}>{v}</span>
                </div>
              ))}
              <div style={{
                display: 'flex', justifyContent: 'space-between',
                borderTop: '1px solid #E8E6E1',
                marginTop: 10, paddingTop: 12,
              }}>
                <span style={{ fontSize: 14, fontWeight: 700, color: '#0B1930' }}>Total</span>
                <span style={{ fontSize: 20, fontWeight: 800, color: '#C49A3C' }}>${total}</span>
              </div>
            </div>

            <div style={{ marginBottom: 28 }}>
              <label className="lux-label">Card Last 4 Digits</label>
              <input
                className="lux-input"
                type="text"
                maxLength={4}
                placeholder="1234"
                value={form.card}
                onChange={e => setForm(p => ({ ...p, card: e.target.value.replace(/\D/g, '') }))}
                style={{ textAlign: 'center', letterSpacing: '0.3em', fontSize: 20, fontWeight: 800 }}
              />
            </div>

            <button
              onClick={handlePay}
              disabled={loading || form.card.length !== 4}
              className="btn-emerald"
              style={{ width: '100%', fontSize: 15 }}
            >
              {loading ? 'Processing…' : 'Confirm & Pay'}
            </button>
          </>
        )}
      </div>
    </div>
  )
}
