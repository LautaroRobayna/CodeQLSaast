FROM node:16 AS builder

ARG API_URL=http://localhost:5000
ARG ENVIRONMENT=development
ARG BUILD_CONFIGURATION=development

WORKDIR /app

LABEL stage=builder

COPY package*.json ./
COPY .npmrc* ./

RUN npm install --frozen-lockfile

COPY . .

RUN if [ "$BUILD_CONFIGURATION" = "production" ]; then \
        npm run build -- --configuration production; \
    elif [ "$BUILD_CONFIGURATION" = "staging" ]; then \
        npm run build -- --configuration staging; \
    else \
        npm run build; \
    fi

FROM nginx:alpine

LABEL maintainer="PharmaGo Team"
LABEL description="PharmaGo Frontend - Angular"

ARG API_URL=http://localhost:5000
ARG ENVIRONMENT=development

COPY --from=builder /app/dist/pharma-go /usr/share/nginx/html

COPY nginx.conf /etc/nginx/conf.d/default.conf

COPY docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh && sed -i 's/\r$//' /docker-entrypoint.sh

EXPOSE 80

ENV API_URL=${API_URL}
ENV ENVIRONMENT=${ENVIRONMENT}
ENV NGINX_WORKER_PROCESSES=auto
ENV NGINX_WORKER_CONNECTIONS=1024

HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
    CMD wget --quiet --tries=1 --spider http://localhost/ || exit 1

ENTRYPOINT ["/docker-entrypoint.sh"]
CMD ["nginx", "-g", "daemon off;"]
