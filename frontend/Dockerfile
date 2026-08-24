FROM node:24-alpine AS build
WORKDIR /app

COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci

COPY frontend/ ./
ARG VITE_API_URL=http://localhost:8080
ENV VITE_API_URL=$VITE_API_URL
RUN npm run build

FROM nginx:1.31-alpine AS runtime
LABEL org.opencontainers.image.title="Port Management Web" \
      org.opencontainers.image.description="Interface do Sistema de Gestão de Escalas de Navios" \
      org.opencontainers.image.source="https://github.com/MarcusBomfim/gestao-escalas-navios"
COPY infrastructure/nginx/default.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
