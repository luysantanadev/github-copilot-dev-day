/**
 * logger.js — Logger estruturado para o frontend.
 *
 * Lê o nível de log da variável de ambiente VITE_LOG_LEVEL.
 * Cada entrada é serializada como JSON para facilitar a integração
 * com ferramentas de monitoramento (ex.: Datadog, Elastic).
 */

const LOG_LEVELS = { debug: 0, info: 1, warn: 2, error: 3 }

const configuredLevel = (import.meta.env.VITE_LOG_LEVEL || 'info').toLowerCase()
const currentLevel = LOG_LEVELS[configuredLevel] ?? LOG_LEVELS.info

/**
 * Formata e emite uma entrada de log se o nível for suficiente.
 * @param {'debug'|'info'|'warn'|'error'} level
 * @param {string} message
 * @param {Record<string, unknown>} [data]
 */
function log(level, message, data = {}) {
  if (LOG_LEVELS[level] < currentLevel) return

  const entry = {
    timestamp: new Date().toISOString(),
    level: level.toUpperCase(),
    message,
    ...data
  }

  const output = JSON.stringify(entry)

  switch (level) {
    case 'debug': console.debug(output); break
    case 'warn':  console.warn(output);  break
    case 'error': console.error(output); break
    default:      console.log(output)
  }
}

export const logger = {
  debug: (message, data) => log('debug', message, data),
  info:  (message, data) => log('info',  message, data),
  warn:  (message, data) => log('warn',  message, data),
  error: (message, data) => log('error', message, data)
}
