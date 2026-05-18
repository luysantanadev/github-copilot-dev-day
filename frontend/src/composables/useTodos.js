import { ref } from 'vue'

const API_BASE = 'http://localhost:5171/api/todos'

export function useTodos() {
  const todos = ref([])
  const loading = ref(false)
  const error = ref(null)

  async function fetchTodos() {
    loading.value = true
    error.value = null
    try {
      const res = await fetch(API_BASE)
      if (!res.ok) throw new Error('Failed to load todos')
      todos.value = await res.json()
    } catch (e) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  async function addTodo(title) {
    error.value = null
    try {
      const res = await fetch(API_BASE, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ title }),
      })
      if (!res.ok) {
        const data = await res.json()
        throw new Error(data.error ?? 'Failed to create todo')
      }
      const created = await res.json()
      todos.value.push(created)
    } catch (e) {
      error.value = e.message
    }
  }

  async function toggleTodo(todo) {
    error.value = null
    try {
      const res = await fetch(`${API_BASE}/${todo.id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ title: todo.title, isCompleted: !todo.isCompleted }),
      })
      if (!res.ok) throw new Error('Failed to update todo')
      const updated = await res.json()
      const index = todos.value.findIndex(t => t.id === todo.id)
      if (index !== -1) todos.value[index] = updated
    } catch (e) {
      error.value = e.message
    }
  }

  async function deleteTodo(id) {
    error.value = null
    try {
      const res = await fetch(`${API_BASE}/${id}`, { method: 'DELETE' })
      if (!res.ok) throw new Error('Failed to delete todo')
      todos.value = todos.value.filter(t => t.id !== id)
    } catch (e) {
      error.value = e.message
    }
  }

  return { todos, loading, error, fetchTodos, addTodo, toggleTodo, deleteTodo }
}
