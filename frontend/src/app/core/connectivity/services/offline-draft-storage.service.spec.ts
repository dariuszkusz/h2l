import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { OfflineDraftStorageService } from './offline-draft-storage.service';

describe('OfflineDraftStorageService', () => {
  let service: OfflineDraftStorageService;
  const originalIndexedDb = globalThis.indexedDB;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(OfflineDraftStorageService);
  });

  afterEach(() => {
    Object.defineProperty(globalThis, 'indexedDB', {
      value: originalIndexedDb,
      configurable: true,
      writable: true,
    });
  });

  it('gracefully no-ops when indexedDB is unavailable', async () => {
    Object.defineProperty(globalThis, 'indexedDB', {
      value: undefined,
      configurable: true,
      writable: true,
    });

    await expect(service.saveDraft('task-1', { approved: true })).resolves.toBeUndefined();
    await expect(service.getDraft('task-1')).resolves.toBeNull();
    await expect(service.deleteDraft('task-1')).resolves.toBeUndefined();
    await expect(service.clearAllDrafts()).resolves.toBeUndefined();
  });

  it('saves, reads, deletes, and clears drafts when indexedDB is available', async () => {
    const records = new Map<string, unknown>();

    const createTx = (mode: string) => {
      const tx: Record<string, unknown> = {
        error: null,
        oncomplete: null as (() => void) | null,
        onerror: null as (() => void) | null,
        onabort: null as (() => void) | null,
        objectStore: () => ({
          put: (value: { taskId: string }) => {
            records.set(value.taskId, value);
            queueMicrotask(() => (tx['oncomplete'] as (() => void) | null)?.());
          },
          get: (key: string) => {
            const req: Record<string, unknown> = {
              result: records.get(key),
              error: null,
              onsuccess: null as (() => void) | null,
              onerror: null as (() => void) | null,
            };
            queueMicrotask(() => (req['onsuccess'] as (() => void) | null)?.());
            return req;
          },
          delete: (key: string) => {
            records.delete(key);
            queueMicrotask(() => (tx['oncomplete'] as (() => void) | null)?.());
          },
          clear: () => {
            records.clear();
            queueMicrotask(() => (tx['oncomplete'] as (() => void) | null)?.());
          },
        }),
      };
      expect(['readonly', 'readwrite']).toContain(mode);
      return tx;
    };

    const fakeDb = {
      objectStoreNames: { contains: () => true },
      createObjectStore: vi.fn(),
      transaction: (_store: string, mode: string) => createTx(mode),
      close: vi.fn(),
    };

    Object.defineProperty(globalThis, 'indexedDB', {
      value: {
        open: () => {
          const req: Record<string, unknown> = {
            result: fakeDb,
            onupgradeneeded: null,
            onsuccess: null as (() => void) | null,
            onerror: null,
            onblocked: null,
          };
          queueMicrotask(() => (req['onsuccess'] as (() => void) | null)?.());
          return req;
        },
      },
      configurable: true,
      writable: true,
    });

    await service.saveDraft('task-42', { comment: 'Looks good' });
    const loaded = await service.getDraft('task-42');
    expect(loaded?.taskId).toBe('task-42');
    expect(loaded?.payload).toEqual({ comment: 'Looks good' });

    await service.deleteDraft('task-42');
    expect(await service.getDraft('task-42')).toBeNull();

    await service.saveDraft('task-99', { ok: true });
    await service.clearAllDrafts();
    expect(await service.getDraft('task-99')).toBeNull();
    expect(fakeDb.close).toHaveBeenCalled();
  });
});
