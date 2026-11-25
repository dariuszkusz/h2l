import { inject, Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Store } from '@ngrx/store';
import { map, tap, withLatestFrom } from 'rxjs';
import { ThemeDomService } from '../services/theme-dom.service';
import { ThemeActions } from './theme.actions';
import { selectColorPalette, selectThemeMode } from './theme.selectors';

@Injectable()
export class ThemeEffects {
  private readonly actions$ = inject(Actions);
  private readonly store = inject(Store);
  private readonly themeDom = inject(ThemeDomService);

  readonly initializeTheme$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ThemeActions.initialize),
      map(() => {
        const mode = this.themeDom.getInitialThemeMode();
        const palette = this.themeDom.getInitialColorPalette();
        return ThemeActions.themeLoaded({ mode, palette });
      }),
    ),
  );

  readonly applyLoadedTheme$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(ThemeActions.themeLoaded),
        tap(({ mode, palette }) => {
          this.themeDom.applyTheme(mode, palette);
        }),
      ),
    { dispatch: false },
  );

  readonly persistThemeMode$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(ThemeActions.setThemeMode),
        withLatestFrom(this.store.select(selectColorPalette)),
        tap(([{ mode }, palette]) => {
          this.themeDom.persistThemeMode(mode);
          this.themeDom.applyTheme(mode, palette);
        }),
      ),
    { dispatch: false },
  );

  readonly persistColorPalette$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(ThemeActions.setColorPalette),
        withLatestFrom(this.store.select(selectThemeMode)),
        tap(([{ palette }, mode]) => {
          this.themeDom.persistColorPalette(palette);
          this.themeDom.applyTheme(mode, palette);
        }),
      ),
    { dispatch: false },
  );
}
