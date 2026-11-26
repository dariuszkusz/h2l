import { Injectable, OnDestroy } from '@angular/core';
import { Observable, Subject } from 'rxjs';
import { TaskRealtimeEvent } from './task-realtime-event.model';

const SIGNALR_RECORD_SEPARATOR = '\x1e';

@Injectable({
  providedIn: 'root',
})
export class TasksSignalRService implements OnDestroy {
  private readonly eventsSubject = new Subject<TaskRealtimeEvent>();
  private socket: WebSocket | null = null;
  private activeToken: string | null = null;

  readonly taskEvents$: Observable<TaskRealtimeEvent> = this.eventsSubject.asObservable();

  connect(accessToken: string | null): void {
    if (!accessToken || typeof WebSocket === 'undefined' || typeof window === 'undefined') {
      return;
    }

    if (typeof navigator !== 'undefined' && navigator.onLine === false) {
      return;
    }

    if (this.socket && this.activeToken === accessToken && this.socket.readyState <= WebSocket.OPEN) {
      return;
    }

    this.disconnect();
    this.activeToken = accessToken;

    try {
      const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
      const url = `${protocol}//${window.location.host}/hubs/tasks?access_token=${encodeURIComponent(accessToken)}`;
      const ws = new WebSocket(url);

      ws.onopen = () => {
        const handshake = JSON.stringify({ protocol: 'json', version: 1 }) + SIGNALR_RECORD_SEPARATOR;
        ws.send(handshake);
      };

      ws.onmessage = (event: MessageEvent<unknown>) => {
        if (typeof event.data !== 'string') {
          return;
        }
        this.handleRawFrame(event.data);
      };

      ws.onerror = () => {
        // Graceful no-op when offline or mocked in E2E tests
      };

      ws.onclose = () => {
        if (this.socket === ws) {
          this.socket = null;
        }
      };

      this.socket = ws;
    } catch {
      this.socket = null;
    }
  }

  disconnect(): void {
    if (this.socket) {
      try {
        this.socket.close();
      } catch {
        // Ignore close errors
      }
      this.socket = null;
    }
    this.activeToken = null;
  }

  ngOnDestroy(): void {
    this.disconnect();
    this.eventsSubject.complete();
  }

  private handleRawFrame(raw: string): void {
    const chunks = raw.split(SIGNALR_RECORD_SEPARATOR).filter((part) => part.trim().length > 0);
    for (const chunk of chunks) {
      try {
        const parsed = JSON.parse(chunk) as {
          type?: number;
          target?: string;
          arguments?: readonly unknown[];
        };
        if (parsed.type === 1 && typeof parsed.target === 'string') {
          const firstArg = parsed.arguments?.[0];
          const taskId =
            typeof firstArg === 'string'
              ? firstArg
              : firstArg && typeof firstArg === 'object' && 'taskId' in firstArg
                ? String((firstArg as { taskId: unknown }).taskId)
                : null;

          this.eventsSubject.next({
            eventName: parsed.target,
            taskId,
          });
        }
      } catch {
        // Ignore malformed frames
      }
    }
  }
}
