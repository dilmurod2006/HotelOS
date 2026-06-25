import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { Toaster } from 'react-hot-toast'
import { useAuthStore } from './store/authStore'

import LandingPage from './pages/LandingPage'

// Guest pages
import GuestLayout from './layouts/GuestLayout'
import GuestRegister from './pages/guest/GuestRegister'
import GuestRooms from './pages/guest/GuestRooms'
import GuestBooking from './pages/guest/GuestBooking'
import GuestOrders from './pages/guest/GuestOrders'

// Staff pages
import StaffLayout from './layouts/StaffLayout'
import StaffLogin from './pages/staff/StaffLogin'
import StaffDashboard from './pages/staff/StaffDashboard'
import HousekeepingPage from './pages/staff/HousekeepingPage'
import MaintenancePage from './pages/staff/MaintenancePage'
import OrdersPage from './pages/staff/OrdersPage'

function ProtectedGuest({ children }) {
  const { token, role } = useAuthStore()
  if (!token || role !== 'Guest') return <Navigate to="/" replace />
  return children
}

function ProtectedStaff({ children }) {
  const { token, role } = useAuthStore()
  if (!token || (role !== 'Staff' && role !== 'Admin')) return <Navigate to="/staff/login" replace />
  return children
}

export default function App() {
  return (
    <BrowserRouter>
      <Toaster position="top-right" toastOptions={{ duration: 3000 }} />
      <Routes>
        {/* Landing */}
        <Route path="/" element={<LandingPage />} />

        {/* Guest Portal */}
        <Route path="/guest" element={<ProtectedGuest><GuestLayout /></ProtectedGuest>}>
          <Route index element={<Navigate to="rooms" replace />} />
          <Route path="register" element={<GuestRegister />} />
          <Route path="rooms" element={<GuestRooms />} />
          <Route path="book/:roomId" element={<GuestBooking />} />
          <Route path="orders" element={<GuestOrders />} />
        </Route>

        {/* Staff Portal */}
        <Route path="/staff/login" element={<StaffLogin />} />
        <Route path="/staff" element={<ProtectedStaff><StaffLayout /></ProtectedStaff>}>
          <Route index element={<Navigate to="dashboard" replace />} />
          <Route path="dashboard" element={<StaffDashboard />} />
          <Route path="housekeeping" element={<HousekeepingPage />} />
          <Route path="maintenance" element={<MaintenancePage />} />
          <Route path="orders" element={<OrdersPage />} />
        </Route>

        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  )
}
