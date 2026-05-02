export const environment = {
  production: false,
  logLevel: 'debug',
  apiUrl: '${API_URL:=http://localhost:5000}' || 'http://localhost:5000',
  timeout: 30000,
  features: {
    swagger: true,
    metrics: true,
    logging: true
  }
};
