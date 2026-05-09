<template>
  <li class="todo-item" :class="{ completed: todo.isCompleted }">

    <!-- ── Modo de visualização ─────────────────────────────────────────────── -->
    <div v-if="!editing" class="view-mode">
      <label class="checkbox-wrap" :title="todo.isCompleted ? 'Marcar como pendente' : 'Marcar como concluída'">
        <input
          type="checkbox"
          :checked="todo.isCompleted"
          @change="$emit('toggle', todo)"
        />
      </label>

      <div class="text-block">
        <span class="title">{{ todo.title }}</span>
        <span v-if="todo.description" class="description">{{ todo.description }}</span>
        <span class="date">
          Criada em {{ formatDate(todo.createdAt) }}
          <template v-if="todo.completedAt">
            · Concluída em {{ formatDate(todo.completedAt) }}
          </template>
        </span>
      </div>

      <div class="actions">
        <button @click="startEdit" class="icon-btn" title="Editar">✏️</button>
        <button @click="$emit('delete', todo)" class="icon-btn icon-btn--danger" title="Excluir">🗑️</button>
      </div>
    </div>

    <!-- ── Modo de edição inline ──────────────────────────────────────────────── -->
    <div v-else class="edit-mode">
      <input
        ref="titleRef"
        v-model="editForm.title"
        type="text"
        class="edit-field"
        placeholder="Título"
        @keyup.enter="saveEdit"
        @keyup.escape="cancelEdit"
      />
      <input
        v-model="editForm.description"
        type="text"
        class="edit-field edit-field--sm"
        placeholder="Descrição (opcional)"
        @keyup.escape="cancelEdit"
      />
      <div class="edit-actions">
        <button @click="saveEdit"   class="btn-save">Salvar</button>
        <button @click="cancelEdit" class="btn-cancel">Cancelar</button>
      </div>
    </div>

  </li>
</template>

<script setup>
import { ref, nextTick } from 'vue'

const props = defineProps({
  todo: { type: Object, required: true }
})

const emit = defineEmits(['toggle', 'delete', 'update'])

const editing  = ref(false)
const titleRef = ref(null)
const editForm = ref({ title: '', description: '' })

function formatDate(dateStr) {
  if (!dateStr) return ''
  return new Date(dateStr).toLocaleString('pt-BR', {
    day:    '2-digit',
    month:  '2-digit',
    year:   'numeric',
    hour:   '2-digit',
    minute: '2-digit'
  })
}

async function startEdit() {
  editForm.value = {
    title:       props.todo.title,
    description: props.todo.description ?? ''
  }
  editing.value = true
  await nextTick()
  titleRef.value?.focus()
}

function cancelEdit() {
  editing.value = false
}

function saveEdit() {
  if (!editForm.value.title.trim()) return

  emit('update', props.todo, {
    title:       editForm.value.title.trim(),
    description: editForm.value.description?.trim() || null
  })
  editing.value = false
}
</script>

<style scoped>
.todo-item {
  background: white;
  border-radius: 10px;
  padding: 1rem 1.25rem;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
  transition: box-shadow 0.2s, opacity 0.2s;
  border-left: 4px solid #667eea;
}

.todo-item:hover {
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.1);
}

.todo-item.completed {
  opacity: 0.65;
  border-left-color: #a0aec0;
}

/* ── Modo visualização ────────────────────────────────────────────────────── */
.view-mode {
  display: flex;
  align-items: flex-start;
  gap: 0.875rem;
}

.checkbox-wrap {
  display: flex;
  align-items: center;
  cursor: pointer;
  padding-top: 0.15rem;
  flex-shrink: 0;
}

.checkbox-wrap input[type="checkbox"] {
  width: 1.2rem;
  height: 1.2rem;
  accent-color: #667eea;
  cursor: pointer;
}

.text-block {
  flex: 1;
  min-width: 0;
}

.title {
  display: block;
  font-size: 1rem;
  font-weight: 500;
  color: #2d3748;
  word-break: break-word;
}

.completed .title {
  text-decoration: line-through;
  color: #a0aec0;
}

.description {
  display: block;
  font-size: 0.8125rem;
  color: #718096;
  margin-top: 0.2rem;
  word-break: break-word;
}

.date {
  display: block;
  font-size: 0.75rem;
  color: #cbd5e0;
  margin-top: 0.25rem;
}

.actions {
  display: flex;
  gap: 0.25rem;
  flex-shrink: 0;
}

.icon-btn {
  background: none;
  border: none;
  padding: 0.375rem;
  border-radius: 6px;
  font-size: 1rem;
  cursor: pointer;
  transition: background 0.2s;
  line-height: 1;
}

.icon-btn:hover {
  background: #f0f2f5;
}

.icon-btn--danger:hover {
  background: #fff5f5;
}

/* ── Modo edição ─────────────────────────────────────────────────────────── */
.edit-mode {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.edit-field {
  width: 100%;
  padding: 0.625rem 0.875rem;
  border: 2px solid #667eea;
  border-radius: 8px;
  font-size: 0.9375rem;
  outline: none;
  color: #2d3748;
  transition: border-color 0.2s;
}

.edit-field--sm {
  font-size: 0.875rem;
}

.edit-actions {
  display: flex;
  gap: 0.5rem;
}

.btn-save {
  background: #667eea;
  color: white;
  border: none;
  padding: 0.5rem 1.125rem;
  border-radius: 8px;
  font-size: 0.875rem;
  font-weight: 600;
  cursor: pointer;
  transition: opacity 0.2s;
}

.btn-save:hover { opacity: 0.88; }

.btn-cancel {
  background: #e2e8f0;
  color: #4a5568;
  border: none;
  padding: 0.5rem 1.125rem;
  border-radius: 8px;
  font-size: 0.875rem;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.2s;
}

.btn-cancel:hover { background: #cbd5e0; }
</style>
