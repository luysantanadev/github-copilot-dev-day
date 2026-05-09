# ── Stage 1: Build ────────────────────────────────────────────────────────────
# Usa a imagem oficial do Node.js LTS (tag "lts-alpine" sempre aponta para o
# último release LTS, atualmente Node 22 — sem necessidade de nvm em produção)
FROM node:lts-alpine AS build
WORKDIR /app

# Recebe variáveis de build passadas pelo docker-compose
ARG VITE_API_URL=/api
ARG VITE_LOG_LEVEL=warn

# Injeta como variáveis de ambiente para o Vite (baked-in no bundle)
ENV VITE_API_URL=$VITE_API_URL \
    VITE_LOG_LEVEL=$VITE_LOG_LEVEL

# Instala dependências (camada de cache otimizada)
COPY package*.json .
RUN npm ci

# Copia o código e gera o build de produção
COPY . .
RUN npm run build

# ── Stage 2: Servidor Nginx ────────────────────────────────────────────────────
FROM nginx:alpine AS final

COPY --from=build /app/dist  /usr/share/nginx/html
COPY nginx.conf               /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
