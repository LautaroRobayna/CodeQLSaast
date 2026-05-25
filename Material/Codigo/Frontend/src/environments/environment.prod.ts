export const environment = {
  production: false,
  logLevel: 'debug',
  apiUrl: 'http://localhost:5000',
  timeout: 30000,
  retryConfig: {
    maxRetries: 3,
    baseDelay: 1000
  },
  features: {
    swagger: true,
    metrics: true,
    logging: true
  }
};
