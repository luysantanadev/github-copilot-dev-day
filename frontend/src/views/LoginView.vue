<template>
  <div class="login-container">
    <div class="login-card">
      <div class="login-header">
        <span class="login-icon">📝</span>
        <h1>Todo App</h1>
        <p>Faça login para continuar</p>
      </div>

      <form @submit.prevent="handleLogin" class="login-form">
        <div class="form-group">
          <label for="username">Usuário</label>
          <input
            id="username"
            v-model="form.username"
            type="text"
            placeholder="admin"
            required
            autocomplete="username"
          />
        </div>

        <div class="form-group">
          <label for="password">Senha</label>
          <input
            id="password"
            v-model="form.password"
            type="password"
            placeholder="••••••••"
            required
            autocomplete="current-password"
          />
        </div>

        <div v-if="error" class="error-message" role="alert">
          {{ error }}
        </div>

        <button type="submit" class="btn btn-primary" :disabled="loading">
          {{ loading ? 'Entrando...' : 'Entrar' }}
        </button>
      </form>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { logger } from '../utils/logger'

const router    = useRouter()
const authStore = useAuthStore()

const form    = ref({ username: '', password: '' })
const error   = ref('')
const loading = ref(false)

async function handleLogin() {
  error.value   = ''
  loading.value = true
  logger.info('Iniciando autenticação', { username: form.value.username })

  try {
    await authStore.login(form.value.username, form.value.password)
    logger.info('Autenticação concluída, redirecionando...')
    router.push('/')
  } catch (err) {
    const status = err.response?.status
    error.value  = status === 401
      ? 'Usuário ou senha inválidos.'
      : 'Erro ao conectar com o servidor. Tente novamente.'
    logger.warn('Falha na autenticação', { status, message: err.message })
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.login-container {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  padding: 1rem;
}

.login-card {
  background: white;
  border-radius: 16px;
  padding: 2.5rem;
  width: 100%;
  max-width: 420px;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.25);
}

.login-header {
  text-align: center;
  margin-bottom: 2rem;
}

.login-icon {
  font-size: 3rem;
  display: block;
  margin-bottom: 0.5rem;
}

.login-header h1 {
  font-size: 1.75rem;
  color: #2d3748;
  margin-bottom: 0.375rem;
}

.login-header p {
  color: #718096;
  font-size: 0.9375rem;
}

.login-form {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.375rem;
}

.form-group label {
  font-size: 0.875rem;
  font-weight: 600;
  color: #4a5568;
}

.form-group input {
  padding: 0.75rem 1rem;
  border: 2px solid #e2e8f0;
  border-radius: 8px;
  font-size: 1rem;
  transition: border-color 0.2s;
  outline: none;
  color: #2d3748;
}

.form-group input:focus {
  border-color: #667eea;
}

.error-message {
  background: #fff5f5;
  border: 1px solid #fc8181;
  color: #c53030;
  padding: 0.75rem 1rem;
  border-radius: 8px;
  font-size: 0.875rem;
}

.btn-primary {
  padding: 0.875rem;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border: none;
  border-radius: 8px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: opacity 0.2s, transform 0.15s;
}

.btn-primary:hover:not(:disabled) {
  opacity: 0.9;
  transform: translateY(-1px);
}

.btn-primary:active:not(:disabled) {
  transform: translateY(0);
}

.btn-primary:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
</style>
