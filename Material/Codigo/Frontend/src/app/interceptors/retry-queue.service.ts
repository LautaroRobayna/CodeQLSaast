import { Injectable } from '@angular/core';
import { HttpBackend, HttpEvent, HttpErrorResponse, HttpRequest } from '@angular/common/http';
import { Observable, Observer, timer } from 'rxjs';
import { concatMap } from 'rxjs/operators';

interface QueuedItem {
  request: HttpRequest<any>;
  config: { maxRetries: number; baseDelay: number };
  attempt: number;
  subscriber: Partial<Observer<HttpEvent<any>>>;
}

@Injectable({ providedIn: 'root' })
export class RetryQueueService {
  private items: QueuedItem[] = [];
  private processing = false;

  constructor(private httpBackend: HttpBackend) {}

  enqueue(request: HttpRequest<any>, config: { maxRetries: number; baseDelay: number }): Observable<HttpEvent<any>> {
    return new Observable<HttpEvent<any>>(subscriber => {
      const item: QueuedItem = { request, config, attempt: 0, subscriber };
      this.items.push(item);
      if (!this.processing) {
        this.drain();
      }
      return () => {
        const idx = this.items.indexOf(item);
        if (idx >= 0) this.items.splice(idx, 1);
      };
    });
  }

  private drain(): void {
    if (this.processing || this.items.length === 0) return;
    this.processing = true;
    this.retryItem(this.items.shift()!);
  }

  private retryItem(item: QueuedItem): void {
    const delayMs = item.config.baseDelay * Math.pow(2, item.attempt);

    timer(delayMs).pipe(
      concatMap(() => this.httpBackend.handle(item.request))
    ).subscribe({
      next: (event) => item.subscriber.next?.(event),
      error: (error: HttpErrorResponse) => {
        item.attempt++;
        if (item.attempt <= item.config.maxRetries) {
          this.retryItem(item);
        } else {
          item.subscriber.error?.(error);
          this.processing = false;
          this.drain();
        }
      },
      complete: () => {
        item.subscriber.complete?.();
        this.processing = false;
        this.drain();
      }
    });
  }
}
