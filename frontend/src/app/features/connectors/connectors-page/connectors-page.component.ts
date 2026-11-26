import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Store } from '@ngrx/store';
import { components } from '../../../core/api/generated/openapi.types';
import { TranslationService } from '../../../core/i18n/services/translation.service';
import { selectCurrentLanguage } from '../../../core/i18n/store/i18n.selectors';
import { UiStateBannerComponent } from '../../../shared/components/ui-state-banner/ui-state-banner.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { HlmBadgeDirective } from '../../../shared/ui/hlm-badge.directive';
import { HlmButtonDirective } from '../../../shared/ui/hlm-button.directive';
import { HlmCardDirective } from '../../../shared/ui/hlm-card.directive';
import { HlmInputDirective } from '../../../shared/ui/hlm-input.directive';
import { HlmLabelDirective } from '../../../shared/ui/hlm-label.directive';
import { AdminUsersActions } from '../../admin-users/store/admin-users.actions';
import { selectAdminUsersList } from '../../admin-users/store/admin-users.selectors';
import { ConnectorsActions } from '../store/connectors.actions';
import {
  selectAssigningTaskId,
  selectConnectorsError,
  selectConnectorsList,
  selectConnectorsLoading,
  selectConnectorsSaving,
  selectConnectorsSuccessKey,
  selectConnectorTestResults,
  selectDeadLettersList,
  selectDeadLettersLoading,
  selectNeedsAssignmentLoading,
  selectNeedsAssignmentTasks,
  selectRetryingOutboxId,
  selectTestingConnectorId,
} from '../store/connectors.selectors';

type ConnectorType = components['schemas']['ConnectorType'];
type ConnectorAuthType = components['schemas']['ConnectorAuthType'];
type ConnectorSummaryResponse = components['schemas']['ConnectorSummaryResponse'];
type ConnectorCredentialsRequest = components['schemas']['ConnectorCredentialsRequest'];

type ConnectorsTab = 'connectors' | 'dead-letters' | 'needs-assignment';

const AUTH_TYPES_BY_CONNECTOR: Readonly<Record<ConnectorType, readonly ConnectorAuthType[]>> = {
  Camunda8: ['None', 'Basic', 'OidcClientCredentials', 'OAuth2ClientCredentials'],
  N8n: ['HmacSha256', 'Bearer', 'ApiKey'],
  UniversalHttp: ['HmacSha256', 'Bearer', 'ApiKey', 'Basic', 'None'],
};

@Component({
  selector: 'app-connectors-page',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    TranslatePipe,
    HlmButtonDirective,
    HlmCardDirective,
    HlmBadgeDirective,
    HlmInputDirective,
    HlmLabelDirective,
    UiStateBannerComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './connectors-page.component.html',
  host: {
    '(document:keydown.escape)': 'onEscapeKey()',
  },
})
export class ConnectorsPageComponent implements OnInit {
  private readonly store = inject(Store);
  private readonly fb = inject(FormBuilder);
  private readonly translationService = inject(TranslationService);
  private readonly document = inject(DOCUMENT);

  protected readonly currentLanguage = this.store.selectSignal(selectCurrentLanguage);
  protected readonly connectors = this.store.selectSignal(selectConnectorsList);
  protected readonly deadLetters = this.store.selectSignal(selectDeadLettersList);
  protected readonly needsAssignmentTasks = this.store.selectSignal(selectNeedsAssignmentTasks);
  protected readonly users = this.store.selectSignal(selectAdminUsersList);

  protected readonly loadingConnectors = this.store.selectSignal(selectConnectorsLoading);
  protected readonly loadingDeadLetters = this.store.selectSignal(selectDeadLettersLoading);
  protected readonly loadingNeedsAssignment = this.store.selectSignal(selectNeedsAssignmentLoading);
  protected readonly saving = this.store.selectSignal(selectConnectorsSaving);
  protected readonly testingConnectorId = this.store.selectSignal(selectTestingConnectorId);
  protected readonly testResults = this.store.selectSignal(selectConnectorTestResults);
  protected readonly retryingOutboxId = this.store.selectSignal(selectRetryingOutboxId);
  protected readonly assigningTaskId = this.store.selectSignal(selectAssigningTaskId);
  protected readonly error = this.store.selectSignal(selectConnectorsError);
  protected readonly successKey = this.store.selectSignal(selectConnectorsSuccessKey);

  protected readonly activeTab = signal<ConnectorsTab>('connectors');
  protected readonly connectorModalOpen = signal(false);
  protected readonly editingConnector = signal<ConnectorSummaryResponse | null>(null);
  protected readonly deletingConnector = signal<ConnectorSummaryResponse | null>(null);
  protected readonly selectedConnectorType = signal<ConnectorType>('Camunda8');
  protected readonly selectedAuthType = signal<ConnectorAuthType>('None');
  protected readonly taskAssigneeSelections = signal<Readonly<Record<string, string>>>({});
  protected readonly liveAnnouncement = signal<string>('');

  protected readonly connectorTypes: readonly ConnectorType[] = [
    'Camunda8',
    'N8n',
    'UniversalHttp',
  ];

  protected readonly availableAuthTypes = computed<readonly ConnectorAuthType[]>(
    () => AUTH_TYPES_BY_CONNECTOR[this.selectedConnectorType()],
  );

  protected readonly activeUsers = computed(() => this.users().filter((u) => u.isActive));

  protected readonly connectorForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    connectorType: ['Camunda8' as ConnectorType, [Validators.required]],
    baseUrl: ['', [Validators.required, Validators.maxLength(2048)]],
    authType: ['None' as ConnectorAuthType, [Validators.required]],
    callbackUrl: ['', [Validators.maxLength(2048)]],
    isActive: [true],
    username: [''],
    password: [''],
    bearerToken: [''],
    apiKeyHeaderName: ['X-N8N-API-KEY'],
    apiKey: [''],
    hmacSecret: [''],
    clientId: [''],
    clientSecret: [''],
    tokenEndpoint: [''],
    audience: [''],
    scope: [''],
    customCaCertificatePem: [''],
    clearCustomCaCertificate: [false],
  });

  private previouslyFocusedElement: HTMLElement | null = null;

  constructor() {
    effect(() => {
      const key = this.successKey();
      if (key) {
        this.liveAnnouncement.set(
          this.translationService.translate(key, this.currentLanguage()),
        );
      }
    });
  }

  ngOnInit(): void {
    this.store.dispatch(ConnectorsActions.clearFeedback());
    this.store.dispatch(ConnectorsActions.loadConnectors());
    this.store.dispatch(ConnectorsActions.loadDeadLetters());
    this.store.dispatch(ConnectorsActions.loadNeedsAssignmentTasks());
    this.store.dispatch(AdminUsersActions.loadUsers());
  }

  protected setActiveTab(tab: ConnectorsTab): void {
    this.activeTab.set(tab);
    this.store.dispatch(ConnectorsActions.clearFeedback());
    if (tab === 'dead-letters') {
      this.store.dispatch(ConnectorsActions.loadDeadLetters());
    } else if (tab === 'needs-assignment') {
      this.store.dispatch(ConnectorsActions.loadNeedsAssignmentTasks());
    }
  }

  protected openCreateModal(): void {
    this.captureFocus();
    this.store.dispatch(ConnectorsActions.clearFeedback());
    this.editingConnector.set(null);
    this.selectedConnectorType.set('Camunda8');
    this.selectedAuthType.set('None');
    this.connectorForm.reset({
      name: '',
      connectorType: 'Camunda8',
      baseUrl: '',
      authType: 'None',
      callbackUrl: '',
      isActive: true,
      username: '',
      password: '',
      bearerToken: '',
      apiKeyHeaderName: 'X-N8N-API-KEY',
      apiKey: '',
      hmacSecret: '',
      clientId: '',
      clientSecret: '',
      tokenEndpoint: '',
      audience: '',
      scope: '',
      customCaCertificatePem: '',
      clearCustomCaCertificate: false,
    });
    this.connectorModalOpen.set(true);
    this.focusElementById('connector-name');
  }

  protected openEditModal(connector: ConnectorSummaryResponse): void {
    this.captureFocus();
    this.store.dispatch(ConnectorsActions.clearFeedback());
    this.editingConnector.set(connector);
    this.selectedConnectorType.set(connector.connectorType);
    this.selectedAuthType.set(connector.authType);
    this.connectorForm.reset({
      name: connector.name,
      connectorType: connector.connectorType,
      baseUrl: connector.baseUrl,
      authType: connector.authType,
      callbackUrl: connector.callbackUrl ?? '',
      isActive: connector.isActive,
      username: '',
      password: '',
      bearerToken: '',
      apiKeyHeaderName: connector.connectorType === 'N8n' ? 'X-N8N-API-KEY' : 'X-API-Key',
      apiKey: '',
      hmacSecret: '',
      clientId: '',
      clientSecret: '',
      tokenEndpoint: '',
      audience: '',
      scope: '',
      customCaCertificatePem: '',
      clearCustomCaCertificate: false,
    });
    this.connectorModalOpen.set(true);
    this.focusElementById('connector-name');
  }

  protected closeConnectorModal(): void {
    this.connectorModalOpen.set(false);
    this.editingConnector.set(null);
    this.restoreFocus();
  }

  protected openDeleteModal(connector: ConnectorSummaryResponse): void {
    this.captureFocus();
    this.store.dispatch(ConnectorsActions.clearFeedback());
    this.deletingConnector.set(connector);
    this.focusElementById('confirm-delete-connector-btn');
  }

  protected closeDeleteModal(): void {
    this.deletingConnector.set(null);
    this.restoreFocus();
  }

  protected onConnectorTypeChange(event: Event): void {
    const nextType = (event.target as HTMLSelectElement).value as ConnectorType;
    this.selectedConnectorType.set(nextType);
    const allowedAuths = AUTH_TYPES_BY_CONNECTOR[nextType];
    const defaultAuth = allowedAuths[0];
    this.selectedAuthType.set(defaultAuth);
    this.connectorForm.patchValue({
      connectorType: nextType,
      authType: defaultAuth,
      apiKeyHeaderName: nextType === 'N8n' ? 'X-N8N-API-KEY' : 'X-API-Key',
    });
  }

  protected onAuthTypeChange(event: Event): void {
    const nextAuth = (event.target as HTMLSelectElement).value as ConnectorAuthType;
    this.selectedAuthType.set(nextAuth);
    this.connectorForm.patchValue({ authType: nextAuth });
  }

  protected onSubmitConnector(): void {
    if (this.connectorForm.invalid) {
      this.connectorForm.markAllAsTouched();
      return;
    }

    const raw = this.connectorForm.getRawValue();
    const credentials = this.buildCredentialsPayload(raw.authType);
    const customCaPem = this.normalizeOptional(raw.customCaCertificatePem);
    const callbackUrl = this.normalizeOptional(raw.callbackUrl);
    const editing = this.editingConnector();

    if (editing) {
      this.store.dispatch(
        ConnectorsActions.updateConnector({
          connectorId: editing.id,
          request: {
            name: raw.name.trim(),
            connectorType: raw.connectorType,
            baseUrl: raw.baseUrl.trim(),
            authType: raw.authType,
            credentials,
            customCaCertificatePem: customCaPem,
            clearCustomCaCertificate: raw.clearCustomCaCertificate,
            callbackUrl,
            isActive: raw.isActive,
          },
        }),
      );
    } else {
      this.store.dispatch(
        ConnectorsActions.createConnector({
          request: {
            name: raw.name.trim(),
            connectorType: raw.connectorType,
            baseUrl: raw.baseUrl.trim(),
            authType: raw.authType,
            credentials,
            customCaCertificatePem: customCaPem,
            callbackUrl,
            isActive: raw.isActive,
          },
        }),
      );
    }

    this.closeConnectorModal();
  }

  protected onConfirmDelete(): void {
    const target = this.deletingConnector();
    if (!target) {
      return;
    }
    this.store.dispatch(ConnectorsActions.deleteConnector({ connectorId: target.id }));
    this.closeDeleteModal();
  }

  protected onTestConnector(connectorId: string): void {
    this.store.dispatch(ConnectorsActions.testConnector({ connectorId }));
  }

  protected onRetryDeadLetter(outboxId: string): void {
    this.store.dispatch(ConnectorsActions.retryDeadLetter({ outboxId }));
  }

  protected onSelectTaskAssignee(taskId: string, event: Event): void {
    const userId = (event.target as HTMLSelectElement).value;
    this.taskAssigneeSelections.set({
      ...this.taskAssigneeSelections(),
      [taskId]: userId,
    });
  }

  protected selectedAssigneeFor(taskId: string): string {
    return this.taskAssigneeSelections()[taskId] ?? '';
  }

  protected onAssignTask(taskId: string): void {
    const userId = this.selectedAssigneeFor(taskId);
    if (!userId) {
      return;
    }
    this.store.dispatch(ConnectorsActions.assignTask({ taskId, userId }));
  }

  protected formatDateTime(isoString: string): string {
    const parsed = new Date(isoString);
    if (Number.isNaN(parsed.getTime())) {
      return isoString;
    }
    return this.translationService.formatDate(parsed, this.currentLanguage());
  }

  protected onEscapeKey(): void {
    if (this.connectorModalOpen()) {
      this.closeConnectorModal();
    } else if (this.deletingConnector()) {
      this.closeDeleteModal();
    }
  }

  protected onDialogKeydown(event: KeyboardEvent, kind: 'connector' | 'delete'): void {
    if (event.key === 'Escape') {
      event.preventDefault();
      if (kind === 'connector') {
        this.closeConnectorModal();
      } else {
        this.closeDeleteModal();
      }
    }
  }

  private buildCredentialsPayload(
    authType: ConnectorAuthType,
  ): ConnectorCredentialsRequest | null {
    const raw = this.connectorForm.getRawValue();
    switch (authType) {
      case 'None':
        return null;
      case 'Basic': {
        const username = this.normalizeOptional(raw.username);
        const password = this.normalizeOptional(raw.password);
        return username || password ? { username, password } : null;
      }
      case 'Bearer': {
        const bearerToken = this.normalizeOptional(raw.bearerToken);
        return bearerToken ? { bearerToken } : null;
      }
      case 'ApiKey': {
        const apiKeyHeaderName = this.normalizeOptional(raw.apiKeyHeaderName);
        const apiKey = this.normalizeOptional(raw.apiKey);
        return apiKey ? { apiKeyHeaderName, apiKey } : null;
      }
      case 'HmacSha256': {
        const hmacSecret = this.normalizeOptional(raw.hmacSecret);
        return hmacSecret ? { hmacSecret } : null;
      }
      case 'OidcClientCredentials':
      case 'OAuth2ClientCredentials': {
        const clientId = this.normalizeOptional(raw.clientId);
        const clientSecret = this.normalizeOptional(raw.clientSecret);
        const tokenEndpoint = this.normalizeOptional(raw.tokenEndpoint);
        const audience = this.normalizeOptional(raw.audience);
        const scope = this.normalizeOptional(raw.scope);
        return clientId || clientSecret || tokenEndpoint || audience || scope
          ? { clientId, clientSecret, tokenEndpoint, audience, scope }
          : null;
      }
    }
  }

  private normalizeOptional(value: string): string | null {
    const trimmed = value.trim();
    return trimmed.length > 0 ? trimmed : null;
  }

  private captureFocus(): void {
    const active = this.document.activeElement;
    this.previouslyFocusedElement = active instanceof HTMLElement ? active : null;
  }

  private restoreFocus(): void {
    const target = this.previouslyFocusedElement;
    this.previouslyFocusedElement = null;
    if (target && typeof target.focus === 'function') {
      queueMicrotask(() => target.focus());
    }
  }

  private focusElementById(id: string): void {
    queueMicrotask(() => {
      const element = this.document.getElementById(id);
      if (element instanceof HTMLElement) {
        element.focus();
      }
    });
  }
}
