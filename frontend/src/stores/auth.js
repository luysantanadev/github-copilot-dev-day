/**
 * auth.js — Store Pinia para gerenciar o estado de autenticação.
 *
 * Persiste o token e o nome de usuário no localStorage para que
 * a sessão sobreviva a recarregamentos da página.
 */

import { defineStore } from 'pinia'
import { authApi } from '../services/api'
import { logger } from '../utils/logger'

export const useAuthStore = defineStore('auth', {
  state: () => ({
    token:    localStorage.getItem('auth_token')    ?? null,
    username: localStorage.getItem('auth_username') ?? null
  }),

  getters: {
    isAuthenticated: (state) => !!state.token
  },

  actions: {
    /**
     * Realiza login e persiste o token no localStorage.
     * @throws {AxiosError} em caso de credenciais inválidas ou falha de rede.
     */
    async login(username, password) {
      logger.info('Realizando login', { username })

      const { data } = await authApi.login(username, password)

      this.token    = data.token
      this.username = data.username

      localStorage.setItem('auth_token',    this.token)
      localStorage.setItem('auth_username', this.username)

      logger.info('Login realizado com sucesso', { username })
    },

    /** Limpa o estado local e remove os dados do localStorage. */
    logout() {
      logger.info('Sessão encerrada', { username: this.username })

      this.token    = null
      this.username = null

      localStorage.removeItem('auth_token')
      localStorage.removeItem('auth_username')
    }
  }
})
