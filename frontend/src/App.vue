<template>
  <RouterView />
</template>

<script setup>
import { RouterView } from 'vue-router'
</script>

<style>
/* ── Reset global ──────────────────────────────────────────────────────────── */
*, *::before, *::after {
  box-sizing: border-box;
  margin: 0;
  padding: 0;
}

body {
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen,
    Ubuntu, Cantarell, sans-serif;
  background-color: #f0f2f5;
  color: #333;
  line-height: 1.6;
}

ul { list-style: none; }
button { cursor: pointer; font-family: inherit; }
input  { font-family: inherit; }

/* ── Utilitários de botão ──────────────────────────────────────────────────── */
.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0.5rem 1rem;
  border-radius: 8px;
  font-size: 0.875rem;
  font-weight: 600;
  border: none;
  transition: opacity 0.2s, transform 0.15s;
}

.btn:hover:not(:disabled)  { opacity: 0.9; transform: translateY(-1px); }
.btn:active:not(:disabled) { transform: translateY(0); }
.btn:disabled              { opacity: 0.6; cursor: not-allowed; }
</style>
