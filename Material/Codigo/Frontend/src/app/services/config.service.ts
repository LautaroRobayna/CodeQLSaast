import { Injectable } from '@angular/core';
import { environment } from '../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ConfigService {
  private config = environment;

  constructor() {
    this.initializeConfig();
  }

  private initializeConfig(): void {
    if ((window as any).APP_CONFIG) {
      this.config = { ...this.config, ...(window as any).APP_CONFIG };
    }

    const storedConfig = localStorage.getItem('appConfig');
    if (storedConfig) {
      try {
        const parsedConfig = JSON.parse(storedConfig);
        this.config = { ...this.config, ...parsedConfig };
      } catch (e) {
        console.warn('Error al parsear configuración desde localStorage:', e);
      }
    }

    if (!this.config.production) {
      console.log('Configuración cargada:', this.config);
    }
  }

  /**
   * Obtiene la configuración completa
   */
  getFullConfig(): any {
    return this.config;
  }

  /**
   * Obtiene un valor de configuración específico
   * @param key Clave de configuración
   * @param defaultValue Valor por defecto si no existe
   */
  getConfig(key: string, defaultValue?: any): any {
    const keys = key.split('.');
    let value: any = this.config;

    for (const k of keys) {
      if (value && typeof value === 'object' && k in value) {
        value = value[k];
      } else {
        return defaultValue;
      }
    }

    return value;
  }

  /**
   * Establece un valor de configuración
   * @param key Clave de configuración
   * @param value Valor a establecer
   */
  setConfig(key: string, value: any): void {
    const keys = key.split('.');
    let obj: any = this.config;

    for (let i = 0; i < keys.length - 1; i++) {
      const k = keys[i];
      if (!(k in obj)) {
        obj[k] = {};
      }
      obj = obj[k];
    }

    obj[keys[keys.length - 1]] = value;
  }

  /**
   * Getter convenientes para acceder a configuración común
   */
  get apiUrl(): string {
    return this.getConfig('apiUrl', 'http://localhost:5000');
  }

  get production(): boolean {
    return this.getConfig('production', false);
  }

  get environment(): string {
    return this.production ? 'production' : 'development';
  }

  get logLevel(): string {
    return this.getConfig('logLevel', 'info');
  }

  get timeout(): number {
    return this.getConfig('timeout', 30000);
  }

  get isFeatureEnabled(feature: string): boolean {
    return this.getConfig(`features.${feature}`, false);
  }
}
