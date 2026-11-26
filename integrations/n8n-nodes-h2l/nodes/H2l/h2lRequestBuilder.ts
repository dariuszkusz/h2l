import type { IDataObject } from 'n8n-workflow';

export type SensitiveDataMode = 'ClaimCheck' | 'PassThrough';

export interface CreateTaskParams {
  externalTaskId: string;
  formId: string;
  formVersion?: number;
  assignee?: string;
  candidateGroups?: string[] | string;
  prefilledData?: unknown;
  resumeUrl: string;
  timeoutIso8601?: string;
  sensitiveDataMode: SensitiveDataMode;
}

export function normalizeBaseUrl(baseUrl: string): string {
  return baseUrl.trim().replace(/\/+$/, '');
}

export function parseCandidateGroups(input?: string[] | string): string[] | undefined {
  if (!input) {
    return undefined;
  }

  const list = Array.isArray(input)
    ? input
    : input
        .split(',')
        .map((item) => item.trim());

  const cleaned = list.map((item) => item.trim()).filter((item) => item.length > 0);
  return cleaned.length > 0 ? cleaned : undefined;
}

export function parsePrefilledData(input: unknown): IDataObject | undefined {
  if (input === undefined || input === null || input === '') {
    return undefined;
  }

  let parsed: unknown = input;
  if (typeof input === 'string') {
    const trimmed = input.trim();
    if (!trimmed || trimmed === '{}') {
      return undefined;
    }
    try {
      parsed = JSON.parse(trimmed);
    } catch {
      throw new Error('Prefilled Data must be valid JSON');
    }
  }

  if (typeof parsed !== 'object' || parsed === null || Array.isArray(parsed)) {
    throw new Error('Prefilled Data must be a JSON object');
  }

  const record = parsed as IDataObject;
  return Object.keys(record).length > 0 ? record : undefined;
}

export function buildCreateTaskPayload(params: CreateTaskParams): IDataObject {
  const externalTaskId = params.externalTaskId.trim();
  const formId = params.formId.trim();
  const resumeUrl = params.resumeUrl.trim();

  if (!externalTaskId) {
    throw new Error('externalTaskId is required');
  }
  if (!formId) {
    throw new Error('formId is required');
  }
  if (!resumeUrl) {
    throw new Error('resumeUrl is required');
  }

  const payload: IDataObject = {
    externalTaskId,
    formId,
    resumeUrl,
    sensitiveDataMode: params.sensitiveDataMode,
  };

  if (typeof params.formVersion === 'number' && Number.isInteger(params.formVersion) && params.formVersion >= 1) {
    payload.formVersion = params.formVersion;
  }

  const assignee = params.assignee?.trim();
  if (assignee) {
    payload.assignee = assignee;
  }

  const candidateGroups = parseCandidateGroups(params.candidateGroups);
  if (candidateGroups) {
    payload.candidateGroups = candidateGroups;
  }

  const prefilledData = parsePrefilledData(params.prefilledData);
  if (prefilledData) {
    payload.prefilledData = prefilledData;
  }

  const timeoutIso8601 = params.timeoutIso8601?.trim();
  if (timeoutIso8601) {
    payload.timeoutIso8601 = timeoutIso8601;
  }

  return payload;
}
