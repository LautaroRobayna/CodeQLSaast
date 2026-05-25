import { Injectable } from '@angular/core';
import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { RetryQueueService } from './retry-queue.service';

const CONFIG_URL = 'assets/config.json';
let fileConfig: { maxRetries: number; baseDelay: number } | null = null;

async function loadFileConfig(): Promise<void> {
  try {
    const res = await fetch(CONFIG_URL);
    const json = await res.json();
    fileConfig = json.retry ?? null;
  } catch {
    fileConfig = null;
  }
}

function readConfig() {
  return fileConfig ?? environment.retryConfig ?? { maxRetries: 0, baseDelay: 1000 };
}

loadFileConfig();

@Injectable()
export class RetryInterceptor implements HttpInterceptor {

  constructor(private queueService: RetryQueueService) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const config = readConfig();
    if (config.maxRetries <= 0) {
      return next.handle(req);
    }

    return next.handle(req).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status >= 400 && error.status < 500) {
          return throwError(() => error);
        }
        return this.queueService.enqueue(req, config);
      })
    );
  }
}
