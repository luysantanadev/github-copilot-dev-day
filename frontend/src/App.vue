<script setup>
import { ref, onMounted } from 'vue'
import { useTodos } from './composables/useTodos.js'

const { todos, loading, error, fetchTodos, addTodo, toggleTodo, deleteTodo } = useTodos()

const newTitle = ref('')

onMounted(fetchTodos)

async function handleAdd() {
  const title = newTitle.value.trim()
  if (!title) return
  await addTodo(title)
  newTitle.value = ''
}
</script>

<template>
  <div class="app">
    <h1>📝 Todo List</h1>

    <form class="add-form" @submit.prevent="handleAdd">
      <input
        v-model="newTitle"
        type="text"
        placeholder="What needs to be done?"
        maxlength="200"
        autocomplete="off"
      />
      <button type="submit">Add</button>
    </form>

    <p v-if="error" class="error" role="alert">{{ error }}</p>

    <p v-if="loading" class="loading">Loading…</p>

    <ul v-else-if="todos.length" class="todo-list">
      <li
        v-for="todo in todos"
        :key="todo.id"
        :class="{ completed: todo.isCompleted }"
        class="todo-item"
      >
        <input
          type="checkbox"
          :checked="todo.isCompleted"
          :aria-label="`Mark &quot;${todo.title}&quot; as ${todo.isCompleted ? 'incomplete' : 'complete'}`"
          @change="toggleTodo(todo)"
        />
        <span class="title">{{ todo.title }}</span>
        <button
          class="delete-btn"
          :aria-label="`Delete &quot;${todo.title}&quot;`"
          @click="deleteTodo(todo.id)"
        >✕</button>
      </li>
    </ul>

    <p v-else class="empty">No todos yet. Add one above!</p>

    <footer v-if="todos.length" class="footer">
      {{ todos.filter(t => !t.isCompleted).length }} item(s) left
    </footer>
  </div>
</template>

<style scoped>
.app {
  max-width: 480px;
  margin: 3rem auto;
  padding: 0 1rem;
  font-family: system-ui, sans-serif;
}

h1 {
  text-align: center;
  margin-bottom: 1.5rem;
  font-size: 2rem;
}

.add-form {
  display: flex;
  gap: 0.5rem;
  margin-bottom: 1rem;
}

.add-form input {
  flex: 1;
  padding: 0.5rem 0.75rem;
  font-size: 1rem;
  border: 1px solid #ccc;
  border-radius: 4px;
}

.add-form button {
  padding: 0.5rem 1rem;
  font-size: 1rem;
  background: #42b883;
  color: #fff;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}

.add-form button:hover {
  background: #33a06f;
}

.error {
  color: #e53e3e;
  margin-bottom: 0.75rem;
}

.loading {
  text-align: center;
  color: #888;
}

.todo-list {
  list-style: none;
  padding: 0;
  margin: 0;
  border: 1px solid #e2e8f0;
  border-radius: 6px;
  overflow: hidden;
}

.todo-item {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.65rem 1rem;
  border-bottom: 1px solid #e2e8f0;
  transition: background 0.15s;
}

.todo-item:last-child {
  border-bottom: none;
}

.todo-item:hover {
  background: #f7fafc;
}

.todo-item.completed .title {
  text-decoration: line-through;
  color: #a0aec0;
}

.title {
  flex: 1;
  word-break: break-word;
}

.delete-btn {
  background: none;
  border: none;
  color: #cbd5e0;
  cursor: pointer;
  font-size: 0.9rem;
  padding: 0.2rem 0.4rem;
  border-radius: 4px;
  line-height: 1;
}

.delete-btn:hover {
  color: #e53e3e;
  background: #fff5f5;
}

.empty {
  text-align: center;
  color: #a0aec0;
  margin-top: 1.5rem;
}

.footer {
  margin-top: 0.75rem;
  text-align: right;
  font-size: 0.85rem;
  color: #718096;
}
</style>
