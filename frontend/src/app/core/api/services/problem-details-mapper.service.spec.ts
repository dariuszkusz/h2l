import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { ProblemDetailsMapperService } from './problem-details-mapper.service';

describe('ProblemDetailsMapperService', () => {
  let service: ProblemDetailsMapperService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ProblemDetailsMapperService);
  });

  it('maps status 0 HttpErrorResponse as offline', () => {
    const error = new HttpErrorResponse({ status: 0, statusText: 'Unknown Error' });
    const mapped = service.map(error);

    expect(mapped.isOffline).toBe(true);
    expect(mapped.status).toBe(0);
    expect(mapped.messageKey).toBe('errors.http.offline');
    expect(mapped.fieldErrors).toEqual({});
  });

  it('maps known errorCode and normalizes FluentValidation errors dictionary to camelCase', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: {
        title: 'Validation failed',
        status: 400,
        detail: 'One or more fields are invalid.',
        errorCode: 'VALIDATION_ERROR',
        errors: {
          AdminEmail: ['Email is invalid.'],
          AdminPassword: ['Password must be at least 12 characters.'],
        },
      },
    });

    const mapped = service.map(error);

    expect(mapped.status).toBe(400);
    expect(mapped.errorCode).toBe('VALIDATION_ERROR');
    expect(mapped.messageKey).toBe('errors.codes.VALIDATION_ERROR');
    expect(mapped.detail).toBe('One or more fields are invalid.');
    expect(mapped.fieldErrors['adminEmail']).toEqual(['Email is invalid.']);
    expect(mapped.fieldErrors['adminPassword']).toEqual([
      'Password must be at least 12 characters.',
    ]);
  });

  it('handles null/partial ProblemDetails fields and non-JSON error bodies safely', () => {
    const error = new HttpErrorResponse({
      status: 409,
      error: 'Plain text conflict response',
    });

    const mapped = service.map(error);
    expect(mapped.status).toBe(409);
    expect(mapped.errorCode).toBeNull();
    expect(mapped.detail).toBeNull();
    expect(mapped.messageKey).toBe('errors.http.conflict');
    expect(mapped.fieldErrors).toEqual({});

    const nullFieldsError = new HttpErrorResponse({
      status: 403,
      error: {
        title: null,
        detail: null,
        errorCode: null,
        errors: null,
      },
    });

    const mappedNulls = service.map(nullFieldsError);
    expect(mappedNulls.status).toBe(403);
    expect(mappedNulls.errorCode).toBeNull();
    expect(mappedNulls.detail).toBeNull();
    expect(mappedNulls.messageKey).toBe('errors.http.forbidden');
    expect(mappedNulls.fieldErrors).toEqual({});
  });

  it('maps unknown non-HttpErrorResponse errors safely', () => {
    const mapped = service.map(new Error('Unexpected failure'));
    expect(mapped.status).toBe(-1);
    expect(mapped.messageKey).toBe('errors.http.unknown');
    expect(mapped.detail).toBe('Unexpected failure');
  });
});
