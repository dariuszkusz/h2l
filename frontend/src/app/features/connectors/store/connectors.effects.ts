import { inject, Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, exhaustMap, map, of, switchMap } from 'rxjs';
import { ConnectorsApiService } from '../../../core/api/connectors-api.service';
import { ProblemDetailsMapperService } from '../../../core/api/services/problem-details-mapper.service';
import { TasksApiService } from '../../../core/api/tasks-api.service';
import { ConnectorsActions } from './connectors.actions';

@Injectable()
export class ConnectorsEffects {
  private readonly actions$ = inject(Actions);
  private readonly connectorsApi = inject(ConnectorsApiService);
  private readonly tasksApi = inject(TasksApiService);
  private readonly problemDetailsMapper = inject(ProblemDetailsMapperService);

  readonly loadConnectors$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ConnectorsActions.loadConnectors),
      switchMap(() =>
        this.connectorsApi.listConnectors().pipe(
          map((response) =>
            ConnectorsActions.loadConnectorsSuccess({ connectors: response.items }),
          ),
          catchError((error: unknown) =>
            of(
              ConnectorsActions.loadConnectorsFailure({
                error: this.problemDetailsMapper.map(error),
              }),
            ),
          ),
        ),
      ),
    ),
  );

  readonly createConnector$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ConnectorsActions.createConnector),
      exhaustMap(({ request }) =>
        this.connectorsApi.createConnector(request).pipe(
          map((connector) => ConnectorsActions.createConnectorSuccess({ connector })),
          catchError((error: unknown) =>
            of(
              ConnectorsActions.createConnectorFailure({
                error: this.problemDetailsMapper.map(error),
              }),
            ),
          ),
        ),
      ),
    ),
  );

  readonly updateConnector$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ConnectorsActions.updateConnector),
      exhaustMap(({ connectorId, request }) =>
        this.connectorsApi.updateConnector(connectorId, request).pipe(
          map((connector) => ConnectorsActions.updateConnectorSuccess({ connector })),
          catchError((error: unknown) =>
            of(
              ConnectorsActions.updateConnectorFailure({
                error: this.problemDetailsMapper.map(error),
              }),
            ),
          ),
        ),
      ),
    ),
  );

  readonly deleteConnector$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ConnectorsActions.deleteConnector),
      exhaustMap(({ connectorId }) =>
        this.connectorsApi.deleteConnector(connectorId).pipe(
          map(() => ConnectorsActions.deleteConnectorSuccess({ connectorId })),
          catchError((error: unknown) =>
            of(
              ConnectorsActions.deleteConnectorFailure({
                error: this.problemDetailsMapper.map(error),
              }),
            ),
          ),
        ),
      ),
    ),
  );

  readonly testConnector$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ConnectorsActions.testConnector),
      exhaustMap(({ connectorId }) =>
        this.connectorsApi.testConnectorConnection(connectorId).pipe(
          map((result) => ConnectorsActions.testConnectorSuccess({ connectorId, result })),
          catchError((error: unknown) =>
            of(
              ConnectorsActions.testConnectorFailure({
                connectorId,
                error: this.problemDetailsMapper.map(error),
              }),
            ),
          ),
        ),
      ),
    ),
  );

  readonly loadDeadLetters$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ConnectorsActions.loadDeadLetters),
      switchMap(() =>
        this.connectorsApi.listDeadLetters().pipe(
          map((response) =>
            ConnectorsActions.loadDeadLettersSuccess({ deadLetters: response.items }),
          ),
          catchError((error: unknown) =>
            of(
              ConnectorsActions.loadDeadLettersFailure({
                error: this.problemDetailsMapper.map(error),
              }),
            ),
          ),
        ),
      ),
    ),
  );

  readonly retryDeadLetter$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ConnectorsActions.retryDeadLetter),
      exhaustMap(({ outboxId }) =>
        this.connectorsApi.retryDeadLetter(outboxId).pipe(
          map(() => ConnectorsActions.retryDeadLetterSuccess({ outboxId })),
          catchError((error: unknown) =>
            of(
              ConnectorsActions.retryDeadLetterFailure({
                error: this.problemDetailsMapper.map(error),
              }),
            ),
          ),
        ),
      ),
    ),
  );

  readonly loadNeedsAssignmentTasks$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ConnectorsActions.loadNeedsAssignmentTasks),
      switchMap(() =>
        this.tasksApi.listTasks({ status: 'NeedsAssignment', page: 1, pageSize: 50 }).pipe(
          map((response) =>
            ConnectorsActions.loadNeedsAssignmentTasksSuccess({ tasks: response.items }),
          ),
          catchError((error: unknown) =>
            of(
              ConnectorsActions.loadNeedsAssignmentTasksFailure({
                error: this.problemDetailsMapper.map(error),
              }),
            ),
          ),
        ),
      ),
    ),
  );

  readonly assignTask$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ConnectorsActions.assignTask),
      exhaustMap(({ taskId, userId }) =>
        this.tasksApi.assignTask(taskId, { userId }).pipe(
          map(() => ConnectorsActions.assignTaskSuccess({ taskId })),
          catchError((error: unknown) =>
            of(
              ConnectorsActions.assignTaskFailure({
                error: this.problemDetailsMapper.map(error),
              }),
            ),
          ),
        ),
      ),
    ),
  );
}
