import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideMockActions } from '@ngrx/effects/testing';
import { Action } from '@ngrx/store';
import { firstValueFrom, Observable, of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';
import { FormsApiService } from '../../../core/api/forms-api.service';
import { FormBuilderActions } from './form-builder.actions';
import { FormBuilderEffects } from './form-builder.effects';

describe('FormBuilderEffects', () => {
  it('dispatches publishVersionFailure with 402 PLAN_LIMIT_EXCEEDED', async () => {
    const actions$: Observable<Action> = of(FormBuilderActions.publishVersion({ formId: 'f1' }));
    const formsApiMock = {
      publishFormVersion: vi.fn(() =>
        throwError(
          () =>
            new HttpErrorResponse({
              status: 402,
              error: { errorCode: 'PLAN_LIMIT_EXCEEDED', detail: 'Free plan limit reached' },
            }),
        ),
      ),
    };

    TestBed.configureTestingModule({
      providers: [
        FormBuilderEffects,
        provideMockActions(() => actions$),
        { provide: FormsApiService, useValue: formsApiMock },
      ],
    });

    const effects = TestBed.inject(FormBuilderEffects);
    const result = await firstValueFrom(effects.publishVersion$);

    expect(result.type).toBe(FormBuilderActions.publishVersionFailure.type);
    if (result.type === FormBuilderActions.publishVersionFailure.type) {
      expect(result.error.status).toBe(402);
      expect(result.error.messageKey).toBe('errors.codes.PLAN_LIMIT_EXCEEDED');
    }
  });
});
