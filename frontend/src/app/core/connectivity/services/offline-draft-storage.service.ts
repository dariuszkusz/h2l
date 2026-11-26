import { Injectable } from '@angular/core';
import { OfflineDraftRecord } from '../models/offline-draft-record.model';

const DB_NAME = 'h2l-offline-drafts';
const DB_VERSION = 1;
const STORE_NAME = 'drafts';

@Injectable({
  providedIn: 'root',
})
export class OfflineDraftStorageService {
  private get indexedDbFactory(): IDBFactory | null {
    return typeof globalThis.indexedDB === 'undefined' ? null : globalThis.indexedDB;
  }

  async saveDraft(
    taskId: string,
    payload: Readonly<Record<string, unknown>>,
    options?: {
      readonly queuedForSubmission?: boolean;
      readonly idempotencyKey?: string | null;
      readonly attachmentIds?: readonly string[];
    },
  ): Promise<void> {
    const db = await this.openDatabase();
    if (!db) {
      return;
    }

    const record: OfflineDraftRecord = {
      taskId,
      payload,
      updatedAt: new Date().toISOString(),
      queuedForSubmission: options?.queuedForSubmission ?? false,
      idempotencyKey: options?.idempotencyKey ?? null,
      attachmentIds: options?.attachmentIds ?? [],
    };

    try {
      await new Promise<void>((resolve, reject) => {
        const transaction = db.transaction(STORE_NAME, 'readwrite');
        const store = transaction.objectStore(STORE_NAME);
        store.put(record);

        transaction.oncomplete = () => resolve();
        transaction.onerror = () =>
          reject(transaction.error ?? new Error('Failed to save offline draft.'));
        transaction.onabort = () =>
          reject(transaction.error ?? new Error('Offline draft save transaction aborted.'));
      });
    } finally {
      db.close();
    }
  }

  async getDraft(taskId: string): Promise<OfflineDraftRecord | null> {
    const db = await this.openDatabase();
    if (!db) {
      return null;
    }

    try {
      return await new Promise<OfflineDraftRecord | null>((resolve, reject) => {
        const transaction = db.transaction(STORE_NAME, 'readonly');
        const store = transaction.objectStore(STORE_NAME);
        const request = store.get(taskId);

        request.onsuccess = () => {
          const result = request.result as OfflineDraftRecord | undefined;
          resolve(result ?? null);
        };
        request.onerror = () =>
          reject(request.error ?? new Error('Failed to read offline draft.'));
      });
    } finally {
      db.close();
    }
  }

  async listDrafts(): Promise<readonly OfflineDraftRecord[]> {
    const db = await this.openDatabase();
    if (!db) {
      return [];
    }

    try {
      return await new Promise<readonly OfflineDraftRecord[]>((resolve, reject) => {
        const transaction = db.transaction(STORE_NAME, 'readonly');
        const store = transaction.objectStore(STORE_NAME);
        const request = store.getAll();

        request.onsuccess = () => {
          const result = request.result as OfflineDraftRecord[] | undefined;
          resolve(result ?? []);
        };
        request.onerror = () =>
          reject(request.error ?? new Error('Failed to list offline drafts.'));
      });
    } finally {
      db.close();
    }
  }

  async listQueuedSubmissions(): Promise<readonly OfflineDraftRecord[]> {
    const all = await this.listDrafts();
    return all.filter((record) => record.queuedForSubmission === true);
  }

  async deleteDraft(taskId: string): Promise<void> {
    const db = await this.openDatabase();
    if (!db) {
      return;
    }

    try {
      await new Promise<void>((resolve, reject) => {
        const transaction = db.transaction(STORE_NAME, 'readwrite');
        const store = transaction.objectStore(STORE_NAME);
        store.delete(taskId);

        transaction.oncomplete = () => resolve();
        transaction.onerror = () =>
          reject(transaction.error ?? new Error('Failed to delete offline draft.'));
        transaction.onabort = () =>
          reject(transaction.error ?? new Error('Offline draft delete transaction aborted.'));
      });
    } finally {
      db.close();
    }
  }

  async clearAllDrafts(): Promise<void> {
    const db = await this.openDatabase();
    if (!db) {
      return;
    }

    try {
      await new Promise<void>((resolve, reject) => {
        const transaction = db.transaction(STORE_NAME, 'readwrite');
        const store = transaction.objectStore(STORE_NAME);
        store.clear();

        transaction.oncomplete = () => resolve();
        transaction.onerror = () =>
          reject(transaction.error ?? new Error('Failed to clear offline drafts.'));
        transaction.onabort = () =>
          reject(transaction.error ?? new Error('Offline draft clear transaction aborted.'));
      });
    } finally {
      db.close();
    }
  }

  private openDatabase(): Promise<IDBDatabase | null> {
    const factory = this.indexedDbFactory;
    if (!factory) {
      return Promise.resolve(null);
    }

    return new Promise<IDBDatabase | null>((resolve) => {
      try {
        const request = factory.open(DB_NAME, DB_VERSION);

        request.onupgradeneeded = () => {
          const db = request.result;
          if (!db.objectStoreNames.contains(STORE_NAME)) {
            db.createObjectStore(STORE_NAME, { keyPath: 'taskId' });
          }
        };

        request.onsuccess = () => resolve(request.result);
        request.onerror = () => resolve(null);
        request.onblocked = () => resolve(null);
      } catch {
        resolve(null);
      }
    });
  }
}
