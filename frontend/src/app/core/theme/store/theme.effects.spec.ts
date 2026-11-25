import { TestBed } from '@angular/core/testing';
import { provideMockActions } from '@ngrx/effects/testing';
import { Action, provideStore } from '@ngrx/store';
import { firstValueFrom, Observable, of } from 'rxjs';
import { beforeEach, describe, expect, it } from 'vitest';
import { ThemeDomService } from '../services/theme-dom.service';
import { ThemeActions } from './theme.actions';
import { ThemeEffects } from './theme.effects';
import { themeReducer } from './theme.reducer';

describe('ThemeEffects', () => {
  let actions$: Observable<Action>;
  let effects: ThemeEffects;
  let themeDom: ThemeDomService;

  beforeEach(() => {
    globalThis.localStorage?.clear();
    TestBed.configureTestingModule({
      providers: [
        ThemeEffects,
        provideStore({ theme: themeReducer }),
        provideMockActions(() => actions$),
      ],
    });
    effects = TestBed.inject(ThemeEffects);
    themeDom = TestBed.inject(ThemeDomService);
  });

  it('loads persisted theme mode and palette on initialize', async () => {
    themeDom.persistThemeMode('high-contrast');
    themeDom.persistColorPalette('forest');
    actions$ = of(ThemeActions.initialize());

    const result = await firstValueFrom(effects.initializeTheme$);
    expect(result).toEqual(
      ThemeActions.themeLoaded({ mode: 'high-contrast', palette: 'forest' }),
    );
  });
});
