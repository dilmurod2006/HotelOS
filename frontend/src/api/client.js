import axios from 'axios'

const api = axios.create({ baseURL: '/api' })

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('hotelOS_token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

api.interceptors.response.use(
  (res) => res,
  (err) => {
    if (err.response?.status === 401) {
      localStorage.removeItem('hotelOS_token')
      localStorage.removeItem('hotelOS_role')
      window.location.href = '/'
    }
    return Promise.reject(err)
  }
)

// Auth
export const login = (role, name) =>
  api.post('/auth/token', { role, name }).then((r) => r.data)

// Guest
export const registerGuest = (data) =>
  api.post('/client/register', data).then((r) => r.data)

export const getAvailableRooms = (params) =>
  api.get('/client/rooms', { params }).then((r) => r.data)

export const createBooking = (data) =>
  api.post('/client/bookings', data).then((r) => r.data)

export const confirmPayment = (bookingId, cardLast4Digits) =>
  api.post(`/client/bookings/${bookingId}/confirm-payment`, { cardLast4Digits }).then((r) => r.data)

export const placeOrder = (data) =>
  api.post('/client/orders', data).then((r) => r.data)

// Admin — Maintenance
export const getMaintenanceQueue = () =>
  api.get('/admin/maintenance/queue').then((r) => r.data)

export const reportIssue = (data) =>
  api.post('/admin/maintenance/issues', data).then((r) => r.data)

export const resolveIssue = (issueId, resolutionNotes) =>
  api.put(`/admin/maintenance/issues/${issueId}/resolve`, { resolutionNotes }).then((r) => r.data)

// Admin — Orders
export const getOrders = (status) =>
  api.get('/admin/orders', { params: status !== undefined ? { status } : {} }).then((r) => r.data)

export const advanceOrderStatus = (orderId) =>
  api.put(`/admin/orders/${orderId}/advance`).then((r) => r.data)

export const checkOut = (data) =>
  api.post('/admin/checkout', data).then((r) => r.data)

// Housekeeping
export const getCleaningTasks = (status) =>
  api.get('/housekeeping/tasks', { params: status ? { status } : {} }).then((r) => r.data)

export const startCleaning = (taskId, cleanerId) =>
  api.put(`/housekeeping/tasks/${taskId}/start`, { cleanerId }).then((r) => r.data)

export const completeCleaning = (taskId) =>
  api.put(`/housekeeping/tasks/${taskId}/complete`).then((r) => r.data)

export default api
