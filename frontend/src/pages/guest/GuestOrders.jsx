import { useState, useEffect } from 'react'
import { placeOrder } from '../../api/client'
import { useAuthStore } from '../../store/authStore'
import { useNavigate } from 'react-router-dom'
import toast from 'react-hot-toast'

const MENU = [
  { id: 1, cat: 'Food', emoji: '🥪', name: 'Club Sandwich', desc: 'Triple-decker with chicken, bacon & avocado', price: 18 },
  { id: 2, cat: 'Food', emoji: '🍕', name: 'Margherita Pizza', desc: 'Fresh tomato, mozzarella and basil', price: 22 },
  { id: 3, cat: 'Food', emoji: '🥗', name: 'Caesar Salad', desc: 'Romaine, croutons, parmesan dressing', price: 15 },
  { id: 4, cat: 'Food', emoji: '🍝', name: 'Pasta Carbonara', desc: 'Creamy egg, guanciale, pecorino', price: 24 },
  { id: 5, cat: 'Drinks', emoji: '☕', name: 'Espresso Doppio', desc: 'Rich double shot, aromatic blend', price: 6 },
  { id: 6, cat: 'Drinks', emoji: '🍊', name: 'Fresh Orange Juice', desc: 'Squeezed to order, 400ml', price: 8 },
  { id: 7, cat: 'Drinks', emoji: '💧', name: 'Sparkling Water', desc: 'San Pellegrino 500ml, chilled', price: 5 },
  { id: 8, cat: 'Drinks', emoji: '🍷', name: 'House Red Wine', desc: 'Italian Chianti, 175ml glass', price: 14 },
  { id: 9, cat: 'Desserts', emoji: '🍰', name: 'Tiramisu', desc: 'Classic Italian, house-made daily', price: 13 },
  { id: 10, cat: 'Desserts', emoji: '🍓', name: 'Seasonal Fruit Plate', desc: 'Fresh fruits with honey & mint', price: 12 },
]

const CATS = ['All', 'Food', 'Drinks', 'Desserts']

export default function GuestOrders() {
  const navigate = useNavigate()
  const { bookingId, bookedRoomNumber } = useAuthStore()
  const [cat, setCat] = useState('All')
  const [cart, setCart] = useState({})
  const [room, setRoom] = useState(bookedRoomNumber || '')
  const [loading, setLoading] = useState(false)
  const [done, setDone] = useState(false)

  // Sync room input if bookedRoomNumber loads from store
  useEffect(() => { if (bookedRoomNumber) setRoom(bookedRoomNumber) }, [bookedRoomNumber])

  const filtered = cat === 'All' ? MENU : MENU.filter(m => m.cat === cat)
  const cartItems = MENU.filter(m => cart[m.id]).map(m => ({ ...m, qty: cart[m.id] }))
  const total = cartItems.reduce((s, m) => s + m.price * m.qty, 0)
  const count = cartItems.reduce((s, m) => s + m.qty, 0)

  const add = id => setCart(c => ({ ...c, [id]: (c[id] || 0) + 1 }))
  const sub = id => setCart(c => {
    const n = { ...c }
    n[id] <= 1 ? delete n[id] : n[id]--
    return n
  })

  const handleOrder = async () => {
    if (!bookingId) {
      toast.error('Please book a room first')
      navigate('/guest/rooms')
      return
    }
    if (!count) { toast.error('Add items to your order'); return }
    if (!room.trim()) { toast.error('Enter your room number'); return }
    setLoading(true)
    try {
      // Backend PlaceOrderCommand: { bookingId, roomNumber, items: [{ itemName, quantity, unitPrice }] }
      await placeOrder({
        bookingId,
        roomNumber: room,
        items: cartItems.map(m => ({ itemName: m.name, quantity: m.qty, unitPrice: m.price })),
      })
      setDone(true)
      toast.success('Order placed! 20–30 minutes.')
    } catch (err) {
      toast.error(err.response?.data?.description || err.response?.data?.Description || 'Order failed')
    } finally {
      setLoading(false)
    }
  }

  if (done) {
    return (
      <div style={{ maxWidth: 460, margin: '0 auto', textAlign: 'center', paddingTop: 40 }}>
        <div style={{
          width: 72, height: 72,
          background: 'linear-gradient(135deg, #C49A3C, #E8C578)',
          borderRadius: '50%',
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          margin: '0 auto 20px',
          boxShadow: '0 12px 32px rgba(196,154,60,0.3)',
          fontSize: 30,
        }}>✓</div>
        <h2 style={{ fontSize: 24, fontWeight: 800, color: '#0B1930', marginBottom: 8 }}>Order Placed!</h2>
        <p style={{ fontSize: 14, color: '#64748B', marginBottom: 28 }}>
          Your order is being prepared. Estimated delivery: <strong>20–30 minutes</strong> to Room {room}.
        </p>
        <div className="lux-card" style={{ padding: 22, marginBottom: 24, textAlign: 'left' }}>
          {cartItems.map(m => (
            <div key={m.id} style={{ display: 'flex', justifyContent: 'space-between', padding: '7px 0', borderBottom: '1px solid #F7F6F3' }}>
              <span style={{ fontSize: 13.5, color: '#374151' }}>{m.emoji} {m.name} × {m.qty}</span>
              <span style={{ fontSize: 13.5, fontWeight: 600, color: '#0B1930' }}>${m.price * m.qty}</span>
            </div>
          ))}
          <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 10, paddingTop: 10 }}>
            <span style={{ fontWeight: 700, fontSize: 14, color: '#0B1930' }}>Total</span>
            <span style={{ fontWeight: 800, fontSize: 16, color: '#C49A3C' }}>${total}</span>
          </div>
        </div>
        <button onClick={() => { setDone(false); setCart({}) }} className="btn-gold">
          Order Again
        </button>
      </div>
    )
  }

  return (
    <div style={{ display: 'grid', gridTemplateColumns: '1fr 320px', gap: 24, alignItems: 'start' }}>
      {/* Menu */}
      <div>
        <div style={{ marginBottom: 24 }}>
          <h1 className="page-title">Room Service</h1>
          <p className="page-sub">Delivered to your door · 20–30 minutes</p>
        </div>

        {!bookingId && (
          <div style={{ padding: '14px 18px', background: '#FEF2F2', border: '1px solid #FECACA', borderRadius: 12, marginBottom: 20 }}>
            <p style={{ fontSize: 13.5, color: '#991B1B', fontWeight: 600 }}>
              You need to book a room before ordering. <button onClick={() => navigate('/guest/rooms')} style={{ background: 'none', border: 'none', color: '#C49A3C', fontWeight: 700, cursor: 'pointer', textDecoration: 'underline' }}>Book now →</button>
            </p>
          </div>
        )}

        {/* Category tabs */}
        <div style={{ display: 'flex', gap: 8, marginBottom: 22, flexWrap: 'wrap' }}>
          {CATS.map(c => (
            <button key={c} onClick={() => setCat(c)} style={{
              padding: '7px 18px', borderRadius: 20, border: 'none', fontSize: 13, fontWeight: 700, cursor: 'pointer',
              transition: 'all 0.15s', background: cat === c ? '#0B1930' : '#fff', color: cat === c ? 'white' : '#64748B',
              boxShadow: cat === c ? '0 4px 12px rgba(11,25,48,0.2)' : 'none',
              outline: cat !== c ? '1px solid #E8E6E1' : 'none',
            }}>{c}</button>
          ))}
        </div>

        {/* Items */}
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(260px, 1fr))', gap: 14 }}>
          {filtered.map(item => (
            <div key={item.id} className="lux-card" style={{ padding: 18, display: 'flex', gap: 14, alignItems: 'flex-start' }}>
              <div style={{ width: 52, height: 52, background: '#F7F6F3', borderRadius: 12, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 24, flexShrink: 0 }}>
                {item.emoji}
              </div>
              <div style={{ flex: 1, minWidth: 0 }}>
                <p style={{ fontSize: 14, fontWeight: 700, color: '#0B1930', marginBottom: 3 }}>{item.name}</p>
                <p style={{ fontSize: 12, color: '#94A3B8', lineHeight: 1.4, marginBottom: 10 }}>{item.desc}</p>
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                  <span style={{ fontSize: 15, fontWeight: 800, color: '#C49A3C' }}>${item.price}</span>
                  {cart[item.id] ? (
                    <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                      <button onClick={() => sub(item.id)} style={{ width: 26, height: 26, borderRadius: '50%', border: '1.5px solid #E8E6E1', background: '#fff', cursor: 'pointer', fontSize: 14, fontWeight: 700, color: '#64748B', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>−</button>
                      <span style={{ fontSize: 14, fontWeight: 800, color: '#0B1930', minWidth: 18, textAlign: 'center' }}>{cart[item.id]}</span>
                      <button onClick={() => add(item.id)} style={{ width: 26, height: 26, borderRadius: '50%', border: 'none', background: '#C49A3C', cursor: 'pointer', fontSize: 14, fontWeight: 700, color: 'white', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>+</button>
                    </div>
                  ) : (
                    <button onClick={() => add(item.id)} style={{ padding: '5px 14px', background: '#F7F6F3', border: '1px solid #E8E6E1', borderRadius: 8, fontSize: 12, fontWeight: 700, color: '#0B1930', cursor: 'pointer', transition: 'all 0.15s' }}
                      onMouseEnter={e => { e.currentTarget.style.background = '#C49A3C'; e.currentTarget.style.color = 'white'; e.currentTarget.style.borderColor = '#C49A3C' }}
                      onMouseLeave={e => { e.currentTarget.style.background = '#F7F6F3'; e.currentTarget.style.color = '#0B1930'; e.currentTarget.style.borderColor = '#E8E6E1' }}>
                      + Add
                    </button>
                  )}
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Cart */}
      <div style={{ position: 'sticky', top: 90 }}>
        <div className="lux-card" style={{ padding: 24 }}>
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 20 }}>
            <h2 style={{ fontSize: 16, fontWeight: 800, color: '#0B1930' }}>Your Order</h2>
            {count > 0 && (
              <span style={{ background: '#C49A3C', color: 'white', borderRadius: '50%', width: 22, height: 22, fontSize: 11, fontWeight: 800, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>{count}</span>
            )}
          </div>

          {cartItems.length === 0 ? (
            <div style={{ textAlign: 'center', padding: '28px 0', color: '#94A3B8' }}>
              <div style={{ fontSize: 32, marginBottom: 8 }}>🛒</div>
              <p style={{ fontSize: 13.5, fontWeight: 500 }}>Your cart is empty</p>
              <p style={{ fontSize: 12, marginTop: 4 }}>Add items from the menu</p>
            </div>
          ) : (
            <>
              <div style={{ marginBottom: 16 }}>
                {cartItems.map(m => (
                  <div key={m.id} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '8px 0', borderBottom: '1px solid #F7F6F3' }}>
                    <span style={{ fontSize: 13, color: '#374151' }}>{m.emoji} {m.name} ×{m.qty}</span>
                    <span style={{ fontSize: 13, fontWeight: 700, color: '#0B1930' }}>${m.price * m.qty}</span>
                  </div>
                ))}
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 18, paddingTop: 4 }}>
                <span style={{ fontWeight: 700, fontSize: 14, color: '#0B1930' }}>Total</span>
                <span style={{ fontWeight: 800, fontSize: 18, color: '#C49A3C' }}>${total}</span>
              </div>

              <div style={{ marginBottom: 16 }}>
                <label className="lux-label">Your Room Number</label>
                <input className="lux-input" placeholder="e.g. 101" value={room} onChange={e => setRoom(e.target.value)} />
              </div>

              <button onClick={handleOrder} disabled={loading || !bookingId} className="btn-gold" style={{ width: '100%' }}>
                {loading ? 'Placing order…' : 'Place Order'}
              </button>
            </>
          )}

          <p style={{ textAlign: 'center', marginTop: 16, fontSize: 12, color: '#94A3B8' }}>
            🕐 Estimated delivery: 20–30 min
          </p>
        </div>
      </div>
    </div>
  )
}
