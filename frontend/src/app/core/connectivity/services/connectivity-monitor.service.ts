import { Injectable } from '@angular/core';
import { distinctUntilChanged, fromEvent, map, merge, Observable, startWith } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class ConnectivityMonitorService {
  readonly isOnline$: Observable<boolean> = merge(
    fromEvent(globalThis, 'online').pipe(map(() => true)),
    fromEvent(globalThis, 'offline').pipe(map(() => false)),
  ).pipe(
    startWith(globalThis.navigator?.onLine ?? true),
    distinctUntilChanged(),
  );
}
