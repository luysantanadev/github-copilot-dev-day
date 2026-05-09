<template>
  <div class="todo-app">

    <!-- ── Header ──────────────────────────────────────────────────────────── -->
    <header class="app-header">
      <div class="header-inner">
        <h1>📝 Todo App</h1>
        <div class="user-bar">
          <span>Olá, <strong>{{ authStore.username }}</strong></span>
          <button @click="handleLogout" class="btn-logout">Sair</button>
        </div>
      </div>
    </header>

    <!-- ── Conteúdo principal ───────────────────────────────────────────────── -->
    <main class="main-content">

      <!-- Formulário de adição -->
      <section class="add-section">
        <form @submit.prevent="handleAdd" class="add-form">
          <div class="add-row">
            <input
              v-model="newTodo.title"
              type="text"
              placeholder="O que precisa ser feito?"
              class="field"
              required
            />
            <button type="submit" class="btn btn-add" :disabled="adding">
              {{ adding ? '...' : '+ Adicionar' }}
            </button>
          </div>
          <input
            v-model="newTodo.description"
            type="text"
            placeholder="Descrição opcional"
            class="field field-sm"
          />
        </form>
      </section>

      <!-- Estatísticas -->
      <div v-if="todos.length" class="stats">
        <span>Total: <strong>{{ todos.length }}</strong></span>
        <span>Pendentes: <strong>{{ pendingCount }}</strong></span>
        <span>Concluídas: <strong>{{ completedCount }}</strong></span>
      </div>

      <!-- Carregando -->
      <div v-if="loading" class="feedback">⏳ Carregando tarefas...</div>

      <!-- Erro -->
      <div v-else-if="fetchError" class="error-banner">
        <span>{{ fetchError }}</span>
        <button @click="load" class="btn-retry">Tentar novamente</button>
      </div>

      <!-- Lista vazia -->
      <div v-else-if="!todos.length" class="empty">
        <span class="empty-icon">✅</span>
        <p>Nenhuma tarefa ainda. Adicione a primeira!</p>
      </div>

      <!-- Lista de tarefas -->
      <ul v-else class="todo-list">
        <TodoItem
          v-for="todo in todos"
          :key="todo.id"
          :todo="todo"
          @toggle="handleToggle"
          @delete="handleDelete"
          @update="handleUpdate"
        />
      </ul>

    </main>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { todosApi } from '../services/api'
import { logger } from '../utils/logger'
import TodoItem from '../components/TodoItem.vue'

const router    = useRouter()
const authStore = useAuthStore()

const todos      = ref([])
const loading    = ref(false)
const fetchError = ref('')
const adding     = ref(false)
const newTodo    = ref({ title: '', description: '' })

const completedCount = computed(() => todos.value.filter(t => t.isCompleted).length)
const pendingCount   = computed(() => todos.value.filter(t => !t.isCompleted).length)

onMounted(load)

// ── Busca todas as tarefas ────────────────────────────────────────────────────
async function load() {
  loading.value    = true
  fetchError.value = ''
  logger.info('Buscando tarefas')

  try {
    const { data } = await todosApi.getAll()
    todos.value = data
    logger.info('Tarefas carregadas', { count: data.length })
  } catch (err) {
    if (err.response?.status === 401) {
      authStore.logout()
      router.push('/login')
    } else {
      fetchError.value = 'Não foi possível carregar as tarefas.'
      logger.error('Falha ao buscar tarefas', { message: err.message })
    }
  } finally {
    loading.value = false
  }
}

// ── Adiciona nova tarefa ──────────────────────────────────────────────────────
async function handleAdd() {
  if (!newTodo.value.title.trim()) return

  adding.value = true
  logger.info('Criando tarefa', { title: newTodo.value.title })

  try {
    const { data } = await todosApi.create({
      title:       newTodo.value.title.trim(),
      description: newTodo.value.description?.trim() || null
    })
    todos.value.unshift(data)
    newTodo.value = { title: '', description: '' }
    logger.info('Tarefa criada', { id: data.id })
  } catch (err) {
    logger.error('Falha ao criar tarefa', { message: err.message })
  } finally {
    adding.value = false
  }
}

// ── Alterna status de conclusão ───────────────────────────────────────────────
async function handleToggle(todo) {
  logger.info('Alternando status', { id: todo.id, isCompleted: !todo.isCompleted })
  try {
    const { data } = await todosApi.update(todo.id, { isCompleted: !todo.isCompleted })
    replaceInList(data)
  } catch (err) {
    logger.error('Falha ao atualizar status', { id: todo.id, message: err.message })
  }
}

// ── Exclui tarefa ─────────────────────────────────────────────────────────────
async function handleDelete(todo) {
  if (!confirm(`Excluir "${todo.title}"?`)) return

  logger.info('Excluindo tarefa', { id: todo.id })
  try {
    await todosApi.remove(todo.id)
    todos.value = todos.value.filter(t => t.id !== todo.id)
    logger.info('Tarefa excluída', { id: todo.id })
  } catch (err) {
    logger.error('Falha ao excluir tarefa', { id: todo.id, message: err.message })
  }
}

// ── Edita título/descrição ────────────────────────────────────────────────────
async function handleUpdate(todo, updates) {
  logger.info('Atualizando tarefa', { id: todo.id, updates })
  try {
    const { data } = await todosApi.update(todo.id, updates)
    replaceInList(data)
  } catch (err) {
    logger.error('Falha ao atualizar tarefa', { id: todo.id, message: err.message })
  }
}

function replaceInList(updated) {
  const idx = todos.value.findIndex(t => t.id === updated.id)
  if (idx !== -1) todos.value[idx] = updated
}

function handleLogout() {
  authStore.logout()
  router.push('/login')
}
</script>

<style scoped>
.todo-app {
  min-height: 100vh;
  background: #f0f2f5;
}

/* ── Header ─────────────────────────────────────────────────────────────────── */
.app-header {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  padding: 1rem 0;
  position: sticky;
  top: 0;
  z-index: 10;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.18);
}

.header-inner {
  max-width: 740px;
  margin: 0 auto;
  padding: 0 1.5rem;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.app-header h1 {
  font-size: 1.5rem;
  font-weight: 700;
}

.user-bar {
  display: flex;
  align-items: center;
  gap: 1rem;
  font-size: 0.9rem;
}

.btn-logout {
  background: rgba(255, 255, 255, 0.2);
  color: white;
  border: 1px solid rgba(255, 255, 255, 0.4);
  padding: 0.375rem 0.875rem;
  border-radius: 6px;
  font-size: 0.875rem;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.2s;
}

.btn-logout:hover {
  background: rgba(255, 255, 255, 0.32);
}

/* ── Layout principal ────────────────────────────────────────────────────────── */
.main-content {
  max-width: 740px;
  margin: 0 auto;
  padding: 2rem 1.5rem;
}

/* ── Formulário de adição ──────────────────────────────────────────────────── */
.add-section {
  background: white;
  border-radius: 12px;
  padding: 1.5rem;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.07);
  margin-bottom: 1.25rem;
}

.add-form {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.add-row {
  display: flex;
  gap: 0.75rem;
}

.field {
  flex: 1;
  padding: 0.75rem 1rem;
  border: 2px solid #e2e8f0;
  border-radius: 8px;
  font-size: 1rem;
  outline: none;
  transition: border-color 0.2s;
  color: #2d3748;
}

.field:focus {
  border-color: #667eea;
}

.field-sm {
  font-size: 0.875rem;
  padding: 0.625rem 1rem;
}

.btn-add {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border: none;
  border-radius: 8px;
  padding: 0.75rem 1.25rem;
  font-size: 0.9375rem;
  font-weight: 600;
  cursor: pointer;
  white-space: nowrap;
  transition: opacity 0.2s, transform 0.15s;
}

.btn-add:hover:not(:disabled) {
  opacity: 0.9;
  transform: translateY(-1px);
}

.btn-add:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

/* ── Estatísticas ─────────────────────────────────────────────────────────── */
.stats {
  display: flex;
  gap: 1.5rem;
  background: white;
  border-radius: 10px;
  padding: 0.875rem 1.25rem;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.07);
  margin-bottom: 1.25rem;
  font-size: 0.875rem;
  color: #718096;
}

.stats strong {
  color: #2d3748;
}

/* ── Estados de feedback ─────────────────────────────────────────────────── */
.feedback {
  text-align: center;
  padding: 3rem;
  color: #a0aec0;
  font-size: 1rem;
}

.error-banner {
  background: #fff5f5;
  border: 1px solid #fc8181;
  color: #c53030;
  padding: 1rem 1.25rem;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 1rem;
}

.btn-retry {
  padding: 0.375rem 0.875rem;
  font-size: 0.8125rem;
  background: #fed7d7;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  color: #c53030;
  font-weight: 600;
  transition: background 0.2s;
}

.btn-retry:hover {
  background: #feb2b2;
}

.empty {
  text-align: center;
  padding: 4rem 2rem;
  color: #a0aec0;
}

.empty-icon {
  font-size: 3rem;
  display: block;
  margin-bottom: 1rem;
}

/* ── Lista ─────────────────────────────────────────────────────────────────── */
.todo-list {
  display: flex;
  flex-direction: column;
  gap: 0.625rem;
}
</style>
