import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideMockActions } from '@ngrx/effects/testing';
import { Action } from '@ngrx/store';
import { firstValueFrom, Observable, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ConnectorsApiService } from '../../../core/api/connectors-api.service';
import { TasksApiService } from '../../../core/api/tasks-api.service';
import { ConnectorsActions } from './connectors.actions';
import { ConnectorsEffects } from './connectors.effects';

describe('ConnectorsEffects', () => {
  let actions$: Observable<Action>;
  let effects: ConnectorsEffects;
  let connectorsApi: {
    listConnectors: ReturnType<typeof vi.fn>;
    createConnector: ReturnType<typeof vi.fn>;
    testConnectorConnection: ReturnType<typeof vi.fn>;
    listDeadLetters: ReturnType<typeof vi.fn>;
    retryDeadLetter: ReturnType<typeof vi.fn>;
  };
  let tasksApi: {
    listTasks: ReturnType<typeof vi.fn>;
    assignTask: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    connectorsApi = {
      listConnectors: vi.fn(),
      createConnector: vi.fn(),
      testConnectorConnection: vi.fn(),
      listDeadLetters: vi.fn(),
      retryDeadLetter: vi.fn(),
    };
    tasksApi = {
      listTasks: vi.fn(),
      assignTask: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        ConnectorsEffects,
        provideMockActions(() => actions$),
        { provide: ConnectorsApiService, useValue: connectorsApi },
        { provide: TasksApiService, useValue: tasksApi },
      ],
    });
    effects = TestBed.inject(ConnectorsEffects);
  });

  it('loads connectors on loadConnectors action', async () => {
    connectorsApi.listConnectors.mockReturnValue(
      of({
        items: [
          {
            id: 'c-1',
            name: 'Camunda 8',
            connectorType: 'Camunda8',
            baseUrl: 'https://camunda.local',
            authType: 'None',
            hasCredentials: false,
            hasCustomCaCertificate: false,
            isActive: true,
            createdAt: '2026-11-26T00:00:00Z',
          },
        ],
      }),
    );

    actions$ = of(ConnectorsActions.loadConnectors());
    const result = await firstValueFrom(effects.loadConnectors$);
    expect(result.type).toBe(ConnectorsActions.loadConnectorsSuccess.type);
  });

  it('maps 402 Free plan error on createConnector failure', async () => {
    connectorsApi.createConnector.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 402,
            error: {
              errorCode: 'FREE_PLAN_CONNECTOR_LIMIT_EXCEEDED',
              detail: 'Free plan allows 1 active connector.',
            },
          }),
      ),
    );

    actions$ = of(
      ConnectorsActions.createConnector({
        request: {
          name: 'Second',
          connectorType: 'N8n',
          baseUrl: 'https://n8n.local',
          authType: 'HmacSha256',
          isActive: true,
        },
      }),
    );
    const result = await firstValueFrom(effects.createConnector$);
    expect(result).toEqual(
      ConnectorsActions.createConnectorFailure({
        error: expect.objectContaining({
          status: 402,
          messageKey: 'errors.codes.PLAN_LIMIT_EXCEEDED',
        }),
      }),
    );
  });

  it('tests connector connection and retries dead-letter', async () => {
    connectorsApi.testConnectorConnection.mockReturnValue(
      of({ isReachable: true, detectedEngine: 'n8n', detectedVersion: '1.80.0', latencyMs: 25 }),
    );
    actions$ = of(ConnectorsActions.testConnector({ connectorId: 'c-1' }));
    const testResult = await firstValueFrom(effects.testConnector$);
    expect(testResult.type).toBe(ConnectorsActions.testConnectorSuccess.type);

    connectorsApi.retryDeadLetter.mockReturnValue(of(void 0));
    actions$ = of(ConnectorsActions.retryDeadLetter({ outboxId: 'dl-1' }));
    const retryResult = await firstValueFrom(effects.retryDeadLetter$);
    expect(retryResult).toEqual(ConnectorsActions.retryDeadLetterSuccess({ outboxId: 'dl-1' }));
  });

  it('loads NeedsAssignment tasks and assigns task to user', async () => {
    tasksApi.listTasks.mockReturnValue(
      of({ items: [], page: 1, pageSize: 50, totalCount: 0, totalPages: 0 }),
    );
    actions$ = of(ConnectorsActions.loadNeedsAssignmentTasks());
    const loadResult = await firstValueFrom(effects.loadNeedsAssignmentTasks$);
    expect(tasksApi.listTasks).toHaveBeenCalledWith({
      status: 'NeedsAssignment',
      page: 1,
      pageSize: 50,
    });
    expect(loadResult.type).toBe(ConnectorsActions.loadNeedsAssignmentTasksSuccess.type);

    tasksApi.assignTask.mockReturnValue(of({ id: 't-1' }));
    actions$ = of(ConnectorsActions.assignTask({ taskId: 't-1', userId: 'u-1' }));
    const assignResult = await firstValueFrom(effects.assignTask$);
    expect(assignResult).toEqual(ConnectorsActions.assignTaskSuccess({ taskId: 't-1' }));
  });
});
