import { Outlet, NavLink, useNavigate } from 'react-router-dom'
import { useAuthStore } from '../store/authStore'

export default function GuestLayout() {
  const { signOut, guestName } = useAuthStore()
  const navigate = useNavigate()

  const handleLogout = () => { signOut(); navigate('/') }

  const navLink = ({ isActive }) => ({
    display: 'inline-flex',
    alignItems: 'center',
    gap: 6,
    padding: '6px 14px',
    borderRadius: 9,
    fontSize: 13.5,
    fontWeight: 600,
    textDecoration: 'none',
    transition: 'all 0.15s ease',
    background: isActive ? 'rgba(196,154,60,0.1)' : 'transparent',
    color: isActive ? '#C49A3C' : '#64748B',
  })

  return (
    <div style={{ minHeight: '100vh', background: '#F7F6F3', display: 'flex', flexDirection: 'column' }}>
      {/* Header */}
      <header style={{
        background: '#fff',
        borderBottom: '1px solid #F0EDE7',
        position: 'sticky',
        top: 0,
        zIndex: 40,
      }}>
        <div style={{
          maxWidth: 1200,
          margin: '0 auto',
          padding: '0 24px',
          height: 64,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
        }}>
          {/* Logo */}
          <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <div style={{
              width: 38, height: 38,
              background: 'linear-gradient(135deg, #0B1930, #1A3057)',
              borderRadius: 10,
              display: 'flex', alignItems: 'center', justifyContent: 'center',
            }}>
              <svg width="18" height="18" fill="none" stroke="white" strokeWidth="1.8" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M3.75 21h16.5M4.5 3h15M5.25 3v18m13.5-18v18M9 6.75h1.5m-1.5 3h1.5m-1.5 3h1.5m3-6H15m-1.5 3H15m-1.5 3H15M9 21v-3.375c0-.621.504-1.125 1.125-1.125h3.75c.621 0 1.125.504 1.125 1.125V21" />
              </svg>
            </div>
            <div>
              <p style={{ fontWeight: 800, fontSize: 15, color: '#0B1930', lineHeight: 1.1 }}>GrandStay</p>
              <p style={{ fontSize: 11, color: '#C49A3C', fontWeight: 600, lineHeight: 1.1 }}>Guest Portal</p>
            </div>
          </div>

          {/* Nav */}
          <nav style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
            <NavLink to="/guest/rooms" style={navLink}>
              <svg width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M2.25 12l8.954-8.955c.44-.439 1.152-.439 1.591 0L21.75 12M4.5 9.75v10.125c0 .621.504 1.125 1.125 1.125H9.75v-4.875c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125V21h4.125c.621 0 1.125-.504 1.125-1.125V9.75M8.25 21h8.25" />
              </svg>
              Rooms
            </NavLink>
            <NavLink to="/guest/orders" style={navLink}>
              <svg width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M2.25 3h1.386c.51 0 .955.343 1.087.835l.383 1.437M7.5 14.25a3 3 0 00-3 3h15.75m-12.75-3h11.218c1.121-2.3 2.1-4.684 2.924-7.138a60.114 60.114 0 00-16.536-1.84M7.5 14.25L5.106 5.272M6 20.25a.75.75 0 11-1.5 0 .75.75 0 011.5 0zm12.75 0a.75.75 0 11-1.5 0 .75.75 0 011.5 0z" />
              </svg>
              Room Service
            </NavLink>
          </nav>

          {/* Right */}
          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            {guestName && (
              <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                <div style={{
                  width: 34, height: 34,
                  background: 'linear-gradient(135deg, #C49A3C, #E8C578)',
                  borderRadius: '50%',
                  display: 'flex', alignItems: 'center', justifyContent: 'center',
                  fontSize: 13, fontWeight: 700, color: 'white',
                }}>
                  {guestName[0].toUpperCase()}
                </div>
                <span style={{ fontSize: 13.5, fontWeight: 600, color: '#0B1930' }}>{guestName}</span>
              </div>
            )}
            <button
              onClick={handleLogout}
              style={{
                display: 'flex', alignItems: 'center', gap: 5,
                padding: '7px 14px',
                background: 'transparent',
                border: '1.5px solid #E8E6E1',
                borderRadius: 9,
                fontSize: 13,
                fontWeight: 600,
                color: '#64748B',
                cursor: 'pointer',
                transition: 'all 0.15s',
              }}
              onMouseEnter={e => { e.currentTarget.style.borderColor = '#EF4444'; e.currentTarget.style.color = '#EF4444'; e.currentTarget.style.background = '#FEF2F2' }}
              onMouseLeave={e => { e.currentTarget.style.borderColor = '#E8E6E1'; e.currentTarget.style.color = '#64748B'; e.currentTarget.style.background = 'transparent' }}
            >
              <svg width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 9V5.25A2.25 2.25 0 0013.5 3h-6a2.25 2.25 0 00-2.25 2.25v13.5A2.25 2.25 0 007.5 21h6a2.25 2.25 0 002.25-2.25V15M12 9l-3 3m0 0l3 3m-3-3h12.75" />
              </svg>
              Logout
            </button>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main style={{
        flex: 1,
        maxWidth: 1200,
        width: '100%',
        margin: '0 auto',
        padding: '36px 24px',
      }}>
        <Outlet />
      </main>
    </div>
  )
}
