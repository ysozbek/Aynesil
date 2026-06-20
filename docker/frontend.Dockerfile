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

# Remove default config and copy our SPA config
RUN rm /etc/nginx/conf.d/default.conf
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf

COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
