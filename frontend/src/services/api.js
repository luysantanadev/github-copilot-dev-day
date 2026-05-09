/**
 * api.js — Instância do Axios com interceptores de log e autenticação.
 *
 * A URL base é lida de VITE_API_URL (variável de ambiente Vite).
 * O token JWT é lido do localStorage e adicionado automaticamente
 * em cada requisição autenticada.
 */

import axios from 'axios'
import { logger } from '../utils/logger'

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000'

const api = axios.create({
  baseURL: API_URL,
  headers: { 'Content-Type': 'application/json' }
})

// ── Interceptor de requisição: injeta o token JWT ─────────────────────────────
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('auth_token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  logger.debug('Requisição enviada', {
    method: config.method?.toUpperCase(),
    url: config.url
  })
  return config
})

// ── Interceptor de resposta: loga sucesso e erros ─────────────────────────────
api.interceptors.response.use(
  (response) => {
    logger.debug('Resposta recebida', {
      status: response.status,
      url: response.config.url
    })
    return response
  },
  (error) => {
    logger.error('Erro na requisição', {
      status: error.response?.status,
      url: error.config?.url,
      message: error.message
    })
    return Promise.reject(error)
  }
)

// ── Módulos da API ────────────────────────────────────────────────────────────

export const authApi = {
  login: (username, password) =>
    api.post('/auth/login', { username, password })
}

export const todosApi = {
  getAll:  ()              => api.get('/todos'),
  getOne:  (id)            => api.get(`/todos/${id}`),
  create:  (payload)       => api.post('/todos', payload),
  update:  (id, payload)   => api.put(`/todos/${id}`, payload),
  remove:  (id)            => api.delete(`/todos/${id}`)
}
