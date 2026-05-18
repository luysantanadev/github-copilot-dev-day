---
description: 'Best practices for Vue 3 development with security as priority and performance efficiency'
applyTo: '**/*.vue, **/*.ts, **/*.js'
---

# Vue 3 Best Practices — Security & Performance

Guidelines for building Vue 3 applications with security as the first priority and maximum rendering/runtime efficiency.

## General Instructions

- Use Vue 3 Composition API with `<script setup>` for all components
- Prefer `ref()` over `reactive()` as the primary reactive state API
- Name composables with the `use` prefix in camelCase (e.g., `useFetch`, `useAuth`)
- Never render user-provided content without sanitization
- Always clean up side effects in `onUnmounted`

---

## Security — Priority #1

### XSS Prevention

**Never use `v-html` with user-provided content without sanitization.**

```vue
<!-- ❌ DANGEROUS — direct user HTML injection -->
<div v-html="userProvidedHtml"></div>

<!-- ✅ SAFE — sanitize with DOMPurify before binding -->
<script setup>
import DOMPurify from 'dompurify'
const safeHtml = computed(() => DOMPurify.sanitize(userProvidedHtml.value))
</script>
<div v-html="safeHtml"></div>
```

### URL Injection Prevention

Always validate and sanitize `href` bindings that come from user input.

```vue
<!-- ❌ DANGEROUS — allows javascript: URLs -->
<a :href="userUrl">link</a>

<!-- ✅ SAFE — validate the protocol before binding -->
<script setup>
import { sanitizeUrl } from '@braintree/sanitize-url'
const safeUrl = computed(() => sanitizeUrl(userUrl.value))
</script>
<a :href="safeUrl">link</a>
```

> Always sanitize URLs on the **backend** before persisting to the database. Frontend sanitization alone is insufficient.

### Style Injection / Clickjacking Prevention

Never bind entire style objects from user input. Allow only specific, safe properties.

```vue
<!-- ❌ DANGEROUS — attacker can overlay/hijack UI elements -->
<div :style="userProvidedStyles"></div>

<!-- ✅ SAFE — only bind specific allowed properties -->
<div :style="{ color: userColor, fontSize: userFontSize }"></div>
```

### JavaScript Injection — Never Bind User Content to Events

```vue
<!-- ❌ NEVER DO THIS -->
<button @click="userProvidedHandler">click</button>

<!-- ❌ NEVER DO THIS -->
<button :onclick="userProvidedJS">click</button>
```

### Dynamic Template Injection

Never compile user-provided strings as Vue templates.

```js
// ❌ CRITICAL VULNERABILITY — user template is arbitrary JS execution
Vue.createApp({ template: `<div>${userString}</div>` }).mount('#app')

// ✅ Use static templates + data bindings
// Template content must be under developer control only
```

### Template Interpolation Is Safe — Use It

Vue automatically escapes `{{ }}` interpolation via `textContent`.

```vue
<!-- ✅ SAFE — Vue escapes this automatically -->
<p>{{ userMessage }}</p>

<!-- ❌ UNSAFE — bypasses Vue's escaping -->
<p v-html="userMessage"></p>
```

### What Vue Protects Automatically

| Vector | Protection |
|--------|-----------|
| `{{ interpolation }}` | Escaped via `textContent` — HTML tags rendered as plain text |
| `:attr` bindings | Escaped via `setAttribute` — injection of new attributes blocked |
| `<style>` in templates | Blocked entirely by Vue |

### CSRF / XSSI

- Coordinate with the backend to submit CSRF tokens on all mutating requests
- These are backend concerns — ensure the API layer handles them
- For SSR apps, follow the [Vue SSR security guidelines](https://vuejs.org/guide/scaling-up/ssr)

---

## Performance — Efficiency Guidelines

### Component Update Optimization

Pass pre-computed primitives as props, not raw inputs for re-computation.

```vue
<!-- ❌ INEFFICIENT — every item re-renders when activeId changes -->
<ListItem v-for="item in list" :key="item.id" :id="item.id" :active-id="activeId" />

<!-- ✅ EFFICIENT — only the affected item re-renders -->
<ListItem v-for="item in list" :key="item.id" :id="item.id" :active="item.id === activeId" />
```

### `v-once` — Skip Re-renders for Static Content

```vue
<!-- ✅ Rendered once, never updated — ideal for static/reference data -->
<ExpensiveStaticComponent v-once />
<footer v-once>
  <LegalText />
</footer>
```

### `v-memo` — Conditional Sub-tree Updates

```vue
<!-- ✅ Only re-renders when item.id or selected changes -->
<div v-for="item in list" :key="item.id" v-memo="[item.id, selected]">
  <p>{{ item.name }}</p>
  <p>{{ item.description }}</p>
</div>
```

### Computed Properties — Stable References

```js
// ❌ INEFFICIENT — new object reference on every computation triggers cascading updates
const state = computed(() => ({ isEven: count.value % 2 === 0 }))

// ✅ EFFICIENT — return previous value if nothing changed (Vue 3.4+)
const state = computed((oldValue) => {
  const next = { isEven: count.value % 2 === 0 }
  if (oldValue && oldValue.isEven === next.isEven) return oldValue
  return next
})
```

### Code Splitting and Lazy Loading

Always lazy-load routes and heavy components.

```js
// ✅ Router-level lazy loading — creates separate chunks per route
const routes = [
  { path: '/dashboard', component: () => import('./views/Dashboard.vue') },
  { path: '/settings', component: () => import('./views/Settings.vue') },
]

// ✅ Async component — loaded on demand
import { defineAsyncComponent } from 'vue'
const HeavyChart = defineAsyncComponent(() => import('./components/HeavyChart.vue'))
```

### Virtualize Large Lists

Never render thousands of DOM nodes directly.

```vue
<!-- ✅ Use virtual scrolling for large data sets (>100 items) -->
<script setup>
import { RecycleScroller } from 'vue-virtual-scroller'
import 'vue-virtual-scroller/dist/vue-virtual-scroller.css'
</script>

<RecycleScroller :items="largeList" :item-size="48" key-field="id" v-slot="{ item }">
  <ListRow :data="item" />
</RecycleScroller>
```

### Reduce Reactivity Overhead for Large Data Structures

Use shallow APIs when deep reactivity is not needed.

```js
// ❌ EXPENSIVE — deep reactivity on large array = thousands of proxy traps
const items = ref(largeArray)

// ✅ EFFICIENT — only root-level reactivity
const items = shallowRef(largeArray)
// Must replace root to trigger updates:
items.value = [...items.value, newItem]

// ✅ For objects
const config = shallowReactive({ theme: 'dark', layout: 'grid' })
```

### Bundle Size — Tree-Shaking

- Always use a **build step** (Vite) — Vue APIs are tree-shakable
- Prefer `lodash-es` over `lodash` for ES module tree-shaking
- Audit dependency size at [bundlejs.com](https://bundlejs.com/) before adding packages

---

## Composition API Best Practices

### Prefer `ref()` Over `reactive()`

```js
// ❌ PITFALLS with reactive()
let state = reactive({ count: 0 })
state = reactive({ count: 1 })      // ❌ original tracking LOST
const { count } = state              // ❌ destructuring LOSES reactivity
count++                              // does not update state.count

// ✅ ref() is safe in all contexts
const count = ref(0)
const { data, loading } = useFetch() // composable refs remain reactive
```

### Composables — Structure and Conventions

```js
// ✅ Composable structure: use prefix, return plain refs object
export function useAuth() {
  const user = ref(null)
  const isLoading = ref(false)

  async function login(credentials) {
    isLoading.value = true
    try {
      user.value = await authService.login(credentials)
    } finally {
      isLoading.value = false
    }
  }

  // ✅ Return refs (not reactive object) — safe to destructure
  return { user, isLoading, login }
}

// In component — reactivity preserved after destructure
const { user, isLoading, login } = useAuth()
```

### Accept Refs or Getters as Input with `toValue`

```js
import { toValue } from 'vue'

// ✅ Handles raw value, ref, or getter — flexible and reactive
export function useFetch(urlOrGetter) {
  const url = computed(() => toValue(urlOrGetter))
  // ...
}

// Caller can pass a getter — re-fetches when props.id changes
const { data } = useFetch(() => `/api/posts/${props.id}`)
```

### Always Clean Up Side Effects

```js
export function useEventListener(target, event, handler) {
  onMounted(() => target.addEventListener(event, handler))
  onUnmounted(() => target.removeEventListener(event, handler)) // ✅ required

  // For watchers that should stop with the component:
  // watchEffect returns a stop handle — called automatically on unmount
  // when used inside setup()
}
```

### Never Use Mixins

| Concern | Mixins | Composables |
|---------|--------|-------------|
| Source clarity | ❌ Unclear which mixin added what | ✅ Explicit import and destructure |
| Namespace safety | ❌ Silent key collisions | ✅ Rename on destructure |
| Coupling | ❌ Implicit shared state | ✅ Explicit data flow |

> Vue 3 docs: **"We no longer recommend using mixins."** Replace all mixins with composables.

### DOM Update Timing

```js
import { nextTick } from 'vue'

// ✅ DOM updates are async — use nextTick() to access updated DOM
async function handleSubmit() {
  formData.value = newData
  await nextTick()
  // DOM now reflects new formData
  inputRef.value?.focus()
}
```

---

## Component Design

### Props Validation

Always define prop types in `<script setup>` using TypeScript or `defineProps` validators.

```vue
<script setup lang="ts">
interface Props {
  userId: string
  role: 'admin' | 'editor' | 'viewer'
  isActive?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  isActive: true,
})
</script>
```

### Emits Declaration

Always declare emits explicitly to document component contracts.

```vue
<script setup lang="ts">
const emit = defineEmits<{
  submit: [payload: { id: string; data: FormData }]
  cancel: []
}>()
</script>
```

### Avoid Unnecessary Component Abstractions

Component instances are significantly more expensive than plain DOM elements. In repeated lists, eliminate wrapper components that only add layout.

```vue
<!-- ❌ EXPENSIVE — 3 components per item × 1000 items = 3000 component instances -->
<ListWrapper v-for="item in list" :key="item.id">
  <ItemCard>
    <ItemLabel :text="item.name" />
  </ItemCard>
</ListWrapper>

<!-- ✅ EFFICIENT — 1 component per item, semantic HTML for layout -->
<div v-for="item in list" :key="item.id" class="list-item">
  <ItemCard :name="item.name" />
</div>
```

---

## Common Pitfalls

| Pitfall | Risk | Fix |
|---------|------|-----|
| `v-html` on user content | XSS | Sanitize with DOMPurify first |
| `:href` from user input | URL/JS injection | Validate protocol with `sanitizeUrl` |
| User string as template | Critical RCE-like XSS | Never compile user input |
| `reactive()` destructure | Silent reactivity loss | Use `ref()`, destructure composable returns |
| Raw large array in `ref()` | Slow deep proxy | Use `shallowRef()` |
| No route lazy loading | Large initial bundle | `() => import('./View.vue')` |
| No list virtualization | DOM thrashing | Use `vue-virtual-scroller` |
| Missing `onUnmounted` cleanup | Memory leaks | Always remove listeners and timers |
| Mixins | Namespace collisions, opaque data flow | Replace with composables |

---

## References

- [Vue 3 Security Guide](https://vuejs.org/guide/best-practices/security)
- [Vue 3 Performance Guide](https://vuejs.org/guide/best-practices/performance)
- [Vue 3 Composables Guide](https://vuejs.org/guide/reusability/composables)
- [OWASP XSS Prevention Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Cross_Site_Scripting_Prevention_Cheat_Sheet.html)
- [DOMPurify](https://github.com/cure53/DOMPurify)
- [vue-virtual-scroller](https://github.com/Akryum/vue-virtual-scroller)
