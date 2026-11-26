import { Injectable } from '@angular/core';
import { finalize, Observable, shareReplay } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class JwtRefreshCoordinatorService {
  private activeRefresh$: Observable<string> | null = null;

  refreshOrJoin(refreshFn: () => Observable<string>): Observable<string> {
    if (!this.activeRefresh$) {
      this.activeRefresh$ = refreshFn().pipe(
        finalize(() => {
          this.activeRefresh$ = null;
        }),
        shareReplay(1),
      );
    }

    return this.activeRefresh$;
  }
}
