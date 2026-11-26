import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { provideMockActions } from '@ngrx/effects/testing';
import { Action } from '@ngrx/store';
import { firstValueFrom, ReplaySubject } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { I18nActions } from '../../i18n/store/i18n.actions';
import { ThemeActions } from '../../theme/store/theme.actions';
import { SetupActions } from './setup.actions';
import { SetupEffects } from './setup.effects';

describe('SetupEffects', () => {
  let actions$: ReplaySubject<Action>;
  let effects: SetupEffects;
  let httpMock: HttpTestingController;
  let router: Router;

  beforeEach(() => {
    actions$ = new ReplaySubject<Action>(1);

    TestBed.configureTestingModule({
      providers: [
        SetupEffects,
        provideMockActions(() => actions$),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });

    effects = TestBed.inject(SetupEffects);
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('checks setup status via GET /setup/status', async () => {
    const resultPromise = firstValueFrom(effects.checkStatus$);
    actions$.next(SetupActions.checkStatus());

    const req = httpMock.expectOne('/api/v1/setup/status');
    req.flush({ isInitialized: false, instanceId: 'inst-1' });

    expect(await resultPromise).toEqual(
      SetupActions.checkStatusSuccess({
        status: { isInitialized: false, instanceId: 'inst-1' },
      }),
    );
  });

  it('initializes instance, applies language & palette, and redirects to /login', async () => {
    const navSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    const initPromise = firstValueFrom(effects.initialize$);
    actions$.next(
      SetupActions.initialize({
        request: {
          adminEmail: 'admin@h2l.local',
          adminPassword: 'VerySecurePassword123',
          defaultLanguage: 'de',
          selectedPalette: 'forest',
          smtp: null,
        },
      }),
    );

    const req = httpMock.expectOne('/api/v1/setup/initialize');
    expect(req.request.body.smtp).toBeNull();
    req.flush({ instanceId: 'inst-1', adminUserId: 'admin-1' });

    const successAction = await initPromise;
    expect(successAction).toEqual(
      SetupActions.initializeSuccess({
        response: { instanceId: 'inst-1', adminUserId: 'admin-1' },
        language: 'de',
        palette: 'forest',
      }),
    );

    const langPromise = firstValueFrom(effects.applyLanguageOnInitialize$);
    const palettePromise = firstValueFrom(effects.applyPaletteOnInitialize$);
    const redirectPromise = firstValueFrom(effects.redirectAfterSetup$);

    actions$.next(successAction);
    expect(await langPromise).toEqual(I18nActions.changeLanguage({ language: 'de' }));
    expect(await palettePromise).toEqual(ThemeActions.setColorPalette({ palette: 'forest' }));
    await redirectPromise;
    expect(navSpy).toHaveBeenCalledWith(['/login']);
  });

  it('dispatches initializeAlreadyDone on 409 conflict', async () => {
    const initPromise = firstValueFrom(effects.initialize$);
    actions$.next(
      SetupActions.initialize({
        request: {
          adminEmail: 'admin@h2l.local',
          adminPassword: 'VerySecurePassword123',
          defaultLanguage: 'en',
          selectedPalette: 'slate',
          smtp: null,
        },
      }),
    );

    const req = httpMock.expectOne('/api/v1/setup/initialize');
    req.flush(
      {
        status: 409,
        errorCode: 'SETUP_ALREADY_INITIALIZED',
        detail: 'Already initialized.',
      },
      { status: 409, statusText: 'Conflict' },
    );

    const action = await initPromise;
    expect(action.type).toBe(SetupActions.initializeAlreadyDone.type);
  });
});
