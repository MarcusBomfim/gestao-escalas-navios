FROM node:26-alpine AS build
WORKDIR /app

COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci

COPY frontend/ ./
ARG VITE_API_URL=http://localhost:8080
ENV VITE_API_URL=$VITE_API_URL
RUN npm run build

# Imagem sem privilégios: o processo mestre do nginx roda como usuário comum e
# escuta em 8080, porque portas abaixo de 1024 exigem root.
FROM nginxinc/nginx-unprivileged:1.29-alpine AS runtime
LABEL org.opencontainers.image.title="Port Management Web" \
      org.opencontainers.image.description="Interface do Sistema de Gestão de Escalas de Navios" \
      org.opencontainers.image.source="https://github.com/MarcusBomfim/gestao-escalas-navios"

USER root
COPY infrastructure/nginx/default.conf /etc/nginx/conf.d/default.conf
# Processado por envsubst na inicialização, para que a CSP receba a origem da API.
COPY infrastructure/nginx/security-headers.conf.template /etc/nginx/templates/security-headers.conf.template
COPY --from=build /app/dist /usr/share/nginx/html
RUN chown -R nginx:nginx /usr/share/nginx/html /etc/nginx/conf.d
USER nginx

# Sem valor definido, a CSP permite apenas a própria origem.
ENV API_ORIGIN="" \
    API_WEBSOCKET_ORIGIN=""

EXPOSE 8080
