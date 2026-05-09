import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import LoginView from '../views/LoginView.vue'
import TodoView  from '../views/TodoView.vue'

const routes = [
  { path: '/login', component: LoginView },
  { path: '/',      component: TodoView, meta: { requiresAuth: true } },
  // Redireciona qualquer rota desconhecida para a raiz
  { path: '/:pathMatch(.*)*', redirect: '/' }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

// Guard global: redireciona para login se não autenticado
router.beforeEach((to) => {
  const authStore = useAuthStore()

  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    return '/login'
  }
  // Evita que um usuário já logado acesse a página de login
  if (to.path === '/login' && authStore.isAuthenticated) {
    return '/'
  }
})

export default router
