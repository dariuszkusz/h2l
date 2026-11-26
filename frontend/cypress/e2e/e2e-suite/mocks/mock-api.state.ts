import type {
  ColorPalette,
  FormAccessRuleItem,
  FormDefinitionResponse,
  GroupResponse,
  LicenseLimits,
  LicensePlan,
  LicenseStatus,
  LicensingStatusResponse,
  ProblemDetails,
  ProjectResponse,
  SupportedLanguage,
  SystemRole,
  ThemeMode,
  UserProfileResponse,
  WorkflowProcessResponse,
} from './mock-api.types';

export type MockPreset =
  | 'uninitialized'
  | 'initialized-free'
  | 'pro-active'
  | 'enterprise-active'
  | 'grace-period'
  | 'expired';

export interface StoredUserRecord extends UserProfileResponse {
  password: string;
  groupIds: string[];
}

export interface ActivationTokenRecord {
  token: string;
  userId: string;
  used: boolean;
  expired: boolean;
}

export interface MockApiState {
  instanceId: string;
  isInitialized: boolean;
  defaultLanguage: SupportedLanguage;
  selectedPalette: ColorPalette;
  smtpConfigured: boolean;
  plan: LicensePlan;
  licenseStatus: LicenseStatus;
  expiresAt: string | null;
  gracePeriodEndsAt: string | null;
  activeConnectors: number;
  users: StoredUserRecord[];
  activationTokens: ActivationTokenRecord[];
  groups: GroupResponse[];
  projects: ProjectResponse[];
  processes: WorkflowProcessResponse[];
  forms: FormDefinitionResponse[];
  accessRulesByForm: Record<string, FormAccessRuleItem[]>;
  activeAccessTokens: Record<string, string>;
  activeRefreshTokens: Record<string, string>;
  expiredAccessTokens: Set<string>;
  expiredRefreshTokens: Set<string>;
  nextSeq: number;
  setupStatusDelayMs: number;
  setupStatusError: ProblemDetails | null;
  preferencesError: ProblemDetails | null;
  simulateSingleAccessTokenExpiry: boolean;
}

export const MOCK_IDS = {
  instanceId: '019363f0-0000-7000-8000-000000000001',
  otherInstanceId: '019363f0-0000-7000-8000-999999999999',
  adminId: '019363f0-0001-7000-8000-000000000010',
  endUserId: '019363f0-0001-7000-8000-000000000011',
  designerId: '019363f0-0001-7000-8000-000000000012',
  groupHrId: '019363f0-0002-7000-8000-000000000020',
  projectHrId: '019363f0-0003-7000-8000-000000000030',
  processPurchaseId: '019363f0-0004-7000-8000-000000000040',
  formRequestId: '019363f0-0005-7000-8000-000000000050',
} as const;

export const VALID_LICENSE_KEYS = {
  pro: `H2L1.PRO.${MOCK_IDS.instanceId}.VALID_ED25519_SIG`,
  enterprise: `H2L1.ENTERPRISE.${MOCK_IDS.instanceId}.VALID_ED25519_SIG`,
  renewedPro: `H2L1.PRO.${MOCK_IDS.instanceId}.RENEWED_ED25519_SIG`,
  wrongInstance: `H2L1.PRO.${MOCK_IDS.otherInstanceId}.VALID_ED25519_SIG`,
  invalidSignature: `H2L1.PRO.${MOCK_IDS.instanceId}.CORRUPT_SIG`,
  expiredRenewal: `H2L1.EXPIRED.${MOCK_IDS.instanceId}.EXPIRED_KEY`,
} as const;

export const DEFAULT_ADMIN_CREDENTIALS = {
  email: 'admin@example.com',
  password: 'StrongAdminPassword!12',
} as const;

export const DEFAULT_END_USER_CREDENTIALS = {
  email: 'user@example.com',
  password: 'StrongUserPassword!12',
} as const;

export const DEFAULT_DESIGNER_CREDENTIALS = {
  email: 'designer@example.com',
  password: 'StrongDesignerPassword!12',
} as const;

export function nextUuid(state: MockApiState, bucket: number): string {
  state.nextSeq += 1;
  const tail = String(state.nextSeq).padStart(12, '0');
  const mid = String(bucket).padStart(4, '0');
  return `019363f0-${mid}-7000-8000-${tail}`;
}

export function buildProblemDetails(
  status: number,
  title: string,
  detail: string,
  errorCode: string,
  instance: string,
  errors?: Record<string, string[]>,
): ProblemDetails {
  return {
    type: `https://h2l.local/errors/${errorCode.toLowerCase().replace(/_/g, '-')}`,
    title,
    status,
    detail,
    instance,
    errorCode,
    ...(errors !== undefined ? { errors } : {}),
  };
}

export function getLimitsForPlan(plan: LicensePlan, status: LicenseStatus): LicenseLimits {
  if (status === 'Expired') {
    return {
      maxActiveProcesses: 1,
      maxActiveConnectors: 1,
      maxAdminUsers: 1,
      maxEndUsers: null,
    };
  }
  switch (plan) {
    case 'Free':
      return {
        maxActiveProcesses: 1,
        maxActiveConnectors: 1,
        maxAdminUsers: 1,
        maxEndUsers: null,
      };
    case 'Pro':
      return {
        maxActiveProcesses: 25,
        maxActiveConnectors: 10,
        maxAdminUsers: 10,
        maxEndUsers: null,
      };
    case 'Enterprise':
      return {
        maxActiveProcesses: null,
        maxActiveConnectors: null,
        maxAdminUsers: null,
        maxEndUsers: null,
      };
  }
}

export function computeLicensingStatus(state: MockApiState): LicensingStatusResponse {
  const activeProcesses = state.processes.filter((p) => p.status === 'Active').length;
  const adminUsers = state.users.filter((u) => u.systemRole === 'Admin').length;
  const endUsers = state.users.filter((u) => u.systemRole === 'EndUser').length;

  return {
    instanceId: state.instanceId,
    plan: state.plan,
    status: state.licenseStatus,
    expiresAt: state.expiresAt,
    gracePeriodEndsAt: state.gracePeriodEndsAt,
    inGracePeriod: state.licenseStatus === 'GracePeriod',
    usage: {
      activeProcesses,
      activeConnectors: state.activeConnectors,
      adminUsers,
      endUsers,
    },
    limits: getLimitsForPlan(state.plan, state.licenseStatus),
  };
}

export function toUserProfile(user: StoredUserRecord): UserProfileResponse {
  return {
    id: user.id,
    email: user.email,
    externalId: user.externalId ?? null,
    systemRole: user.systemRole,
    preferredLanguage: user.preferredLanguage,
    preferredTheme: user.preferredTheme,
    isActive: user.isActive,
    createdAt: user.createdAt,
  };
}

export interface CreateMockStateOptions {
  preset?: MockPreset;
  smtpConfigured?: boolean;
  adminLanguage?: SupportedLanguage;
  adminTheme?: ThemeMode;
  endUserLanguage?: SupportedLanguage;
  endUserTheme?: ThemeMode;
  withSeededProcess?: boolean;
  withSeededGroup?: boolean;
  withSeededUsers?: boolean;
  graceHoursRemaining?: number;
}

export function createMockState(options: CreateMockStateOptions = {}): MockApiState {
  const preset = options.preset ?? 'initialized-free';
  const nowIso = '2026-11-25T21:00:00Z';

  if (preset === 'uninitialized') {
    return {
      instanceId: MOCK_IDS.instanceId,
      isInitialized: false,
      defaultLanguage: 'en',
      selectedPalette: 'slate',
      smtpConfigured: options.smtpConfigured ?? false,
      plan: 'Free',
      licenseStatus: 'Active',
      expiresAt: null,
      gracePeriodEndsAt: null,
      activeConnectors: 0,
      users: [],
      activationTokens: [],
      groups: [],
      projects: [],
      processes: [],
      forms: [],
      accessRulesByForm: {},
      activeAccessTokens: {},
      activeRefreshTokens: {},
      expiredAccessTokens: new Set<string>(),
      expiredRefreshTokens: new Set<string>(),
      nextSeq: 100,
      setupStatusDelayMs: 0,
      setupStatusError: null,
      preferencesError: null,
      simulateSingleAccessTokenExpiry: false,
    };
  }

  const users: StoredUserRecord[] = [
    {
      id: MOCK_IDS.adminId,
      email: DEFAULT_ADMIN_CREDENTIALS.email,
      externalId: null,
      systemRole: 'Admin',
      preferredLanguage: options.adminLanguage ?? 'en',
      preferredTheme: options.adminTheme ?? 'light',
      isActive: true,
      createdAt: nowIso,
      password: DEFAULT_ADMIN_CREDENTIALS.password,
      groupIds: [],
    },
  ];

  const groups: GroupResponse[] = [];
  if (options.withSeededGroup) {
    groups.push({
      id: MOCK_IDS.groupHrId,
      name: 'Human Resources',
      code: 'HR',
      memberCount: options.withSeededUsers ? 1 : 0,
      createdAt: nowIso,
    });
  }

  if (options.withSeededUsers) {
    const endUserGroupIds = options.withSeededGroup ? [MOCK_IDS.groupHrId] : [];
    users.push(
      {
        id: MOCK_IDS.endUserId,
        email: DEFAULT_END_USER_CREDENTIALS.email,
        externalId: 'EMP-101',
        systemRole: 'EndUser',
        preferredLanguage: options.endUserLanguage ?? 'pl',
        preferredTheme: options.endUserTheme ?? 'light',
        isActive: true,
        createdAt: nowIso,
        password: DEFAULT_END_USER_CREDENTIALS.password,
        groupIds: endUserGroupIds,
      },
      {
        id: MOCK_IDS.designerId,
        email: DEFAULT_DESIGNER_CREDENTIALS.email,
        externalId: 'EMP-102',
        systemRole: 'ProcessDesigner',
        preferredLanguage: 'de',
        preferredTheme: 'light',
        isActive: true,
        createdAt: nowIso,
        password: DEFAULT_DESIGNER_CREDENTIALS.password,
        groupIds: [],
      },
    );
  }

  const projects: ProjectResponse[] = [];
  const processes: WorkflowProcessResponse[] = [];
  const forms: FormDefinitionResponse[] = [];
  const accessRulesByForm: Record<string, FormAccessRuleItem[]> = {};

  if (options.withSeededProcess) {
    projects.push({
      id: MOCK_IDS.projectHrId,
      name: 'HR',
      description: 'HR onboarding & approvals',
      createdAt: nowIso,
    });
    processes.push({
      id: MOCK_IDS.processPurchaseId,
      projectId: MOCK_IDS.projectHrId,
      connectorId: null,
      name: 'Software Purchase',
      externalProcessKey: 'software-purchase',
      status: 'Active',
      createdAt: nowIso,
    });
    forms.push({
      id: MOCK_IDS.formRequestId,
      workflowProcessId: MOCK_IDS.processPurchaseId,
      name: 'Initial Request',
      stepKey: 'step-request',
      status: 'Draft',
      draftSchemaJsonb: {},
      updatedAt: nowIso,
    });
    accessRulesByForm[MOCK_IDS.formRequestId] = [];
  }

  let plan: LicensePlan = 'Free';
  let licenseStatus: LicenseStatus = 'Active';
  let expiresAt: string | null = null;
  let gracePeriodEndsAt: string | null = null;

  if (preset === 'pro-active') {
    plan = 'Pro';
    expiresAt = '2027-11-25T21:00:00Z';
  } else if (preset === 'enterprise-active') {
    plan = 'Enterprise';
    expiresAt = '2027-11-25T21:00:00Z';
  } else if (preset === 'grace-period') {
    plan = 'Pro';
    licenseStatus = 'GracePeriod';
    expiresAt = '2026-11-20T21:00:00Z';
    const hours = options.graceHoursRemaining ?? 168;
    gracePeriodEndsAt = new Date(Date.parse('2026-11-25T21:00:00Z') + hours * 3600_000).toISOString();
  } else if (preset === 'expired') {
    plan = 'Pro';
    licenseStatus = 'Expired';
    expiresAt = '2026-11-01T21:00:00Z';
    gracePeriodEndsAt = '2026-11-15T21:00:00Z';
  }

  return {
    instanceId: MOCK_IDS.instanceId,
    isInitialized: true,
    defaultLanguage: options.adminLanguage ?? 'en',
    selectedPalette: 'slate',
    smtpConfigured: options.smtpConfigured ?? false,
    plan,
    licenseStatus,
    expiresAt,
    gracePeriodEndsAt,
    activeConnectors: 0,
    users,
    activationTokens: [
      {
        token: 'one-time-token-123',
        userId: MOCK_IDS.endUserId,
        used: false,
        expired: false,
      },
      {
        token: 'used-tok',
        userId: MOCK_IDS.endUserId,
        used: true,
        expired: false,
      },
    ],
    groups,
    projects,
    processes,
    forms,
    accessRulesByForm,
    activeAccessTokens: {},
    activeRefreshTokens: {},
    expiredAccessTokens: new Set<string>(),
    expiredRefreshTokens: new Set<string>(),
    nextSeq: 200,
    setupStatusDelayMs: 0,
    setupStatusError: null,
    preferencesError: null,
    simulateSingleAccessTokenExpiry: false,
  };
}

export function issueSessionTokens(
  state: MockApiState,
  user: StoredUserRecord,
): { accessToken: string; refreshToken: string } {
  state.nextSeq += 1;
  const accessToken = `eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.${user.systemRole}.${state.nextSeq}`;
  const refreshToken = `rt_${state.nextSeq}_${user.id}`;
  state.activeAccessTokens[accessToken] = user.id;
  state.activeRefreshTokens[refreshToken] = user.id;
  return { accessToken, refreshToken };
}

export function isAdminOrDesigner(role: SystemRole): boolean {
  return role === 'Admin' || role === 'ProcessDesigner';
}
