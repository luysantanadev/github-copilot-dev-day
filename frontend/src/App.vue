<script setup>
import { ref, onMounted } from 'vue'

const API = `${import.meta.env.VITE_API_URL}/todos`

const todos = ref([])
const newTitle = ref('')

async function fetchTodos() {
  const res = await fetch(API)
  todos.value = await res.json()
}

async function addTodo() {
  const title = newTitle.value.trim()
  if (!title) return
  await fetch(API, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ title })
  })
  newTitle.value = ''
  await fetchTodos()
}

async function toggleTodo(todo) {
  await fetch(`${API}/${todo.id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ done: !todo.done })
  })
  await fetchTodos()
}

async function deleteTodo(id) {
  await fetch(`${API}/${id}`, { method: 'DELETE' })
  await fetchTodos()
}

onMounted(fetchTodos)
</script>

<template>
  <div class="container">
    <h1>Todo List</h1>

    <form class="add-form" @submit.prevent="addTodo">
      <input
        v-model="newTitle"
        type="text"
        placeholder="Nova tarefa..."
        class="input"
      />
      <button type="submit" class="btn-add">Adicionar</button>
    </form>

    <ul class="todo-list">
      <li
        v-for="todo in todos"
        :key="todo.id"
        class="todo-item"
        :class="{ done: todo.done }"
      >
        <input
          type="checkbox"
          :checked="todo.done"
          @change="toggleTodo(todo)"
          class="checkbox"
        />
        <span class="title">{{ todo.title }}</span>
        <button class="btn-delete" @click="deleteTodo(todo.id)">✕</button>
      </li>
    </ul>

    <p v-if="todos.length === 0" class="empty">Nenhuma tarefa ainda.</p>
  </div>
</template>

<style scoped>
.container {
  max-width: 480px;
  margin: 60px auto;
  font-family: 'Segoe UI', sans-serif;
}

h1 {
  text-align: center;
  margin-bottom: 24px;
  font-size: 2rem;
  color: #2c3e50;
}

.add-form {
  display: flex;
  gap: 8px;
  margin-bottom: 20px;
}

.input {
  flex: 1;
  padding: 10px 14px;
  border: 1px solid #ccc;
  border-radius: 6px;
  font-size: 1rem;
}

.btn-add {
  padding: 10px 18px;
  background: #42b883;
  color: white;
  border: none;
  border-radius: 6px;
  font-size: 1rem;
  cursor: pointer;
}

.btn-add:hover {
  background: #369f6e;
}

.todo-list {
  list-style: none;
  padding: 0;
  margin: 0;
}

.todo-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 12px 14px;
  background: white;
  border: 1px solid #e0e0e0;
  border-radius: 6px;
  margin-bottom: 8px;
  transition: opacity 0.2s;
}

.todo-item.done .title {
  text-decoration: line-through;
  color: #aaa;
}

.checkbox {
  width: 18px;
  height: 18px;
  cursor: pointer;
}

.title {
  flex: 1;
  font-size: 1rem;
}

.btn-delete {
  background: none;
  border: none;
  color: #e74c3c;
  font-size: 1rem;
  cursor: pointer;
  padding: 2px 6px;
  border-radius: 4px;
}

.btn-delete:hover {
  background: #fdecea;
}

.empty {
  text-align: center;
  color: #aaa;
  margin-top: 20px;
}
</style>
