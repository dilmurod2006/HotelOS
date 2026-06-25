import { create } from 'zustand'
import { login } from '../api/client'

export const useAuthStore = create((set) => ({
  token: localStorage.getItem('hotelOS_token') || null,
  role: localStorage.getItem('hotelOS_role') || null,
  guestId: localStorage.getItem('hotelOS_guestId') || null,
  guestName: localStorage.getItem('hotelOS_guestName') || null,
  bookingId: localStorage.getItem('hotelOS_bookingId') || null,
  bookedRoomNumber: localStorage.getItem('hotelOS_roomNumber') || null,
  loading: false,
  error: null,

  signIn: async (role, name) => {
    set({ loading: true, error: null })
    try {
      const data = await login(role, name)
      localStorage.setItem('hotelOS_token', data.token)
      localStorage.setItem('hotelOS_role', role)
      set({ token: data.token, role, loading: false })
      return true
    } catch {
      set({ error: 'Login failed', loading: false })
      return false
    }
  },

  setGuestProfile: (id, name) => {
    localStorage.setItem('hotelOS_guestId', id)
    localStorage.setItem('hotelOS_guestName', name)
    set({ guestId: id, guestName: name })
  },

  setBookingInfo: (bookingId, roomNumber) => {
    localStorage.setItem('hotelOS_bookingId', bookingId)
    localStorage.setItem('hotelOS_roomNumber', roomNumber)
    set({ bookingId, bookedRoomNumber: roomNumber })
  },

  signOut: () => {
    localStorage.clear()
    set({ token: null, role: null, guestId: null, guestName: null, bookingId: null, bookedRoomNumber: null })
  },
}))
