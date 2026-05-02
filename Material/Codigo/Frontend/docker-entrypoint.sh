#!/bin/sh

echo "Starting PharmaGo Frontend with API URL: $API_URL"
echo "Environment: $ENVIRONMENT"

cat > /usr/share/nginx/html/app-config.js <<EOF
window.APP_CONFIG = {
  apiUrl: '${API_URL}',
  environment: '${ENVIRONMENT}',
  production: $([ "$ENVIRONMENT" = "production" ] && echo "true" || echo "false"),
  logLevel: '${LOG_LEVEL:-info}'
};
EOF

if ! grep -q "app-config.js" /usr/share/nginx/html/index.html; then
  sed -i '/<head>/a \  <script src="app-config.js"><\/script>' /usr/share/nginx/html/index.html
fi

exec "$@"
