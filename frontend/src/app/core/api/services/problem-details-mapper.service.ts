import { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { components } from '../generated/openapi.types';
import { MappedApiError } from '../models/mapped-api-error.model';

type ProblemDetails = components['schemas']['ProblemDetails'];

const KNOWN_ERROR_CODE_KEYS: Readonly<Record<string, string>> = {
  INVALID_CREDENTIALS: 'errors.codes.INVALID_CREDENTIALS',
  ACCOUNT_INACTIVE: 'errors.codes.ACCOUNT_INACTIVE',
  SETUP_ALREADY_INITIALIZED: 'errors.codes.SETUP_ALREADY_INITIALIZED',
  ALREADY_INITIALIZED: 'errors.codes.SETUP_ALREADY_INITIALIZED',
  PLAN_LIMIT_EXCEEDED: 'errors.codes.PLAN_LIMIT_EXCEEDED',
  FREE_PLAN_LIMIT_EXCEEDED: 'errors.codes.PLAN_LIMIT_EXCEEDED',
  FREE_PLAN_ADMIN_LIMIT_EXCEEDED: 'errors.codes.PLAN_LIMIT_EXCEEDED',
  FREE_PLAN_PROCESS_LIMIT_EXCEEDED: 'errors.codes.PLAN_LIMIT_EXCEEDED',
  FREE_PLAN_CONNECTOR_LIMIT_EXCEEDED: 'errors.codes.PLAN_LIMIT_EXCEEDED',
  CONNECTOR_SSRF_BLOCKED: 'errors.codes.CONNECTOR_SSRF_BLOCKED',
  SSRF_BLOCKED: 'errors.codes.CONNECTOR_SSRF_BLOCKED',
  CONNECTOR_IN_USE: 'errors.codes.CONNECTOR_IN_USE',
  DEAD_LETTER_ALREADY_RETRIED: 'errors.codes.DEAD_LETTER_ALREADY_RETRIED',
  TASK_ASSIGNEE_NOT_ALLOWED: 'errors.codes.TASK_ASSIGNEE_NOT_ALLOWED',
  LICENSE_EXPIRED_LIMIT_EXCEEDED: 'errors.codes.LICENSE_EXPIRED',
  TOKEN_INVALID: 'errors.codes.TOKEN_INVALID',
  INVALID_ACTIVATION_TOKEN: 'errors.codes.TOKEN_INVALID',
  TOKEN_EXPIRED: 'errors.codes.TOKEN_EXPIRED',
  ACTIVATION_TOKEN_USED: 'errors.codes.TOKEN_EXPIRED',
  ACCOUNT_ALREADY_ACTIVE: 'errors.codes.ACCOUNT_ALREADY_ACTIVE',
  DUPLICATE_EMAIL: 'errors.codes.DUPLICATE_EMAIL',
  DUPLICATE_GROUP_CODE: 'errors.codes.DUPLICATE_GROUP_CODE',
  DUPLICATE_GROUP_MEMBER: 'errors.codes.DUPLICATE_GROUP_MEMBER',
  DUPLICATE_EXTERNAL_PROCESS_KEY: 'errors.codes.DUPLICATE_EXTERNAL_PROCESS_KEY',
  INVALID_ACL_RULE: 'errors.codes.INVALID_ACL_RULE',
  VALIDATION_ERROR: 'errors.codes.VALIDATION_ERROR',
  FORBIDDEN: 'errors.codes.FORBIDDEN',
  UNAUTHORIZED: 'errors.codes.UNAUTHORIZED',
  LICENSE_INVALID: 'errors.codes.LICENSE_INVALID',
  LICENSE_INVALID_SIGNATURE: 'errors.codes.LICENSE_INVALID',
  INSTANCE_MISMATCH: 'errors.codes.INSTANCE_MISMATCH',
  LICENSE_INSTANCE_MISMATCH: 'errors.codes.INSTANCE_MISMATCH',
  TASK_ALREADY_CLAIMED: 'errors.codes.TASK_ALREADY_CLAIMED',
  TASK_NOT_CLAIMED_BY_USER: 'errors.codes.TASK_NOT_CLAIMED_BY_USER',
  TASK_NOT_CLAIMABLE: 'errors.codes.TASK_NOT_CLAIMABLE',
  TASK_ALREADY_COMPLETED: 'errors.codes.TASK_ALREADY_COMPLETED',
  FORM_SCHEMA_INVALID: 'errors.codes.FORM_SCHEMA_INVALID',
  ATTACHMENT_TOO_LARGE: 'errors.codes.ATTACHMENT_TOO_LARGE',
  ATTACHMENT_TYPE_NOT_ALLOWED: 'errors.codes.ATTACHMENT_TYPE_NOT_ALLOWED',
};

@Injectable({
  providedIn: 'root',
})
export class ProblemDetailsMapperService {
  map(error: unknown): MappedApiError {
    if (!(error instanceof HttpErrorResponse)) {
      return {
        status: -1,
        errorCode: null,
        messageKey: 'errors.http.unknown',
        detail: error instanceof Error && error.message.trim().length > 0 ? error.message : null,
        fieldErrors: {},
        isOffline: false,
      };
    }

    if (error.status === 0) {
      return {
        status: 0,
        errorCode: null,
        messageKey: 'errors.http.offline',
        detail: null,
        fieldErrors: {},
        isOffline: true,
      };
    }

    const problem = this.extractProblemDetails(error.error);
    const errorCode = this.normalizeString(problem?.errorCode);
    const detail =
      this.normalizeString(problem?.detail) ?? this.normalizeString(problem?.title);
    const fieldErrors = this.extractFieldErrors(problem?.errors);
    const messageKey = this.resolveMessageKey(error.status, errorCode);

    return {
      status: error.status,
      errorCode,
      messageKey,
      detail,
      fieldErrors,
      isOffline: false,
    };
  }

  private extractProblemDetails(rawError: unknown): ProblemDetails | null {
    if (rawError === null || typeof rawError !== 'object' || Array.isArray(rawError)) {
      return null;
    }

    return rawError as ProblemDetails;
  }

  private extractFieldErrors(
    rawFieldErrors: ProblemDetails['errors'] | null | undefined,
  ): Readonly<Record<string, readonly string[]>> {
    if (
      rawFieldErrors === null ||
      rawFieldErrors === undefined ||
      typeof rawFieldErrors !== 'object' ||
      Array.isArray(rawFieldErrors)
    ) {
      return {};
    }

    const normalized: Record<string, readonly string[]> = {};
    for (const [rawKey, rawMessages] of Object.entries(rawFieldErrors)) {
      if (!Array.isArray(rawMessages)) {
        continue;
      }

      const validMessages = rawMessages.filter(
        (item): item is string => typeof item === 'string' && item.trim().length > 0,
      );
      if (validMessages.length === 0) {
        continue;
      }

      const camelKey = rawKey.length > 0
        ? rawKey.charAt(0).toLowerCase() + rawKey.slice(1)
        : rawKey;
      normalized[camelKey] = validMessages;
    }

    return normalized;
  }

  private resolveMessageKey(status: number, errorCode: string | null): string {
    if (errorCode !== null) {
      const mappedCodeKey = KNOWN_ERROR_CODE_KEYS[errorCode];
      if (mappedCodeKey !== undefined) {
        return mappedCodeKey;
      }
    }

    switch (status) {
      case 400:
        return 'errors.http.badRequest';
      case 401:
        return 'errors.http.unauthorized';
      case 402:
        return 'errors.http.paymentRequired';
      case 403:
        return 'errors.http.forbidden';
      case 404:
        return 'errors.http.notFound';
      case 409:
        return 'errors.http.conflict';
      default:
        return status >= 500 ? 'errors.http.serverError' : 'errors.http.unknown';
    }
  }

  private normalizeString(value: unknown): string | null {
    if (typeof value !== 'string') {
      return null;
    }

    const trimmed = value.trim();
    return trimmed.length > 0 ? trimmed : null;
  }
}
