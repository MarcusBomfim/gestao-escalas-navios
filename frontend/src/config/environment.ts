const apiUrl = import.meta.env.VITE_API_URL?.trim() || 'http://localhost:8080'

export const environment = Object.freeze({
  apiUrl,
})

