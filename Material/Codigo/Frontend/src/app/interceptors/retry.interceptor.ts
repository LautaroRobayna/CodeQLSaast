import { Injectable } from '@angular/core';
import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest, HttpErrorResponse } from '@angular/common/http';
import { Observable, timer, throwError } from 'rxjs';
import { retry } from 'rxjs/operators';
import { environment } from '../../environments/environment';

const CONFIG_URL = 'assets/config.json';

interface RetryConfig {
  maxRetries: number;
  baseDelay: number;
}

let fileConfig: RetryConfig | null = null;
let fileConfigPromise: Promise<void> | null = null;

async function loadFileConfig(): Promise<void> {
  try {
    const res = await fetch(CONFIG_URL);
    const json = await res.json();
    fileConfig = json.retry ?? null;
  } catch {
    fileConfig = null;
  }
}

function readConfig(): RetryConfig {
  if (fileConfig) {
    return fileConfig;
  }
  const env = environment.retryConfig ?? {};
  return {
    maxRetries: env.maxRetries ?? 0,
    baseDelay: env.baseDelay ?? 1000,
  };
}

fileConfigPromise = loadFileConfig();

@Injectable()
export class RetryInterceptor implements HttpInterceptor {

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const config = readConfig();
    if (config.maxRetries <= 0) {
      return next.handle(req);
    }

    return next.handle(req).pipe(
      retry({
        count: config.maxRetries,
        delay: (error: HttpErrorResponse, retryCount: number) => {
          if (error.status >= 400 && error.status < 500) {
            return throwError(() => error);
          }
          const delayMs = config.baseDelay * Math.pow(2, retryCount);
          return timer(delayMs);
        }
      })
    );
  }
}
