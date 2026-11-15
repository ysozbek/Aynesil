# ── Stage 1: Build Vue app ────────────────────────────────────────────────────
FROM node:22-alpine AS build
WORKDIR /app

# Copy package files first (layer cache optimization)
# npm ci: lockfile varsa kullan (CI ortamı); yoksa npm install'a düş.
COPY frontend/aynesil-web/package.json ./
COPY frontend/aynesil-web/package-lock.json* ./

# package-lock.json varsa npm ci (reproducible), yoksa npm install
RUN if [ -f package-lock.json ]; then \
      echo "package-lock.json bulundu → npm ci" && npm ci; \
    else \
      echo "package-lock.json yok → npm install" && npm install; \
    fi

# Copy source and build
COPY frontend/aynesil-web/ ./
RUN npm run build

# ── Stage 2: Serve via nginx ──────────────────────────────────────────────────
FROM nginx:1.27-alpine AS runtime

# Remove default config; our SPA config is a template rendered at startup by the image's entrypoint
# (/docker-entrypoint.d/20-envsubst-on-templates.sh → /etc/nginx/conf.d/default.conf).
# Only AYNESIL_* variables are substituted so nginx's own $variables are left alone.
RUN rm /etc/nginx/conf.d/default.conf
COPY docker/nginx.conf.template /etc/nginx/templates/default.conf.template
ENV NGINX_ENVSUBST_FILTER=^AYNESIL_
# Compose ağı (docker-compose.yml AYNESIL_NETWORK_SUBNET ile aynı varsayılan); compose environment'ı ezer.
ENV AYNESIL_NETWORK_SUBNET=172.30.0.0/24

COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
