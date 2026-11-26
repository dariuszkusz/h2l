export type SupportedLanguage = 'en' | 'de' | 'pl';
export type ThemeMode = 'light' | 'dark' | 'high-contrast';
export type ColorPalette = 'slate' | 'ocean' | 'forest';
export type SystemRole = 'Admin' | 'ProcessDesigner' | 'EndUser';
export type InviteDeliveryMethod = 'smtp' | 'manual_link';
export type WorkflowProcessStatus = 'Draft' | 'Active' | 'Archived';
export type FormDefinitionStatus = 'Draft' | 'Published' | 'Archived';
export type FormAccessScope = 'WholeGroup' | 'GroupUserSubset' | 'DirectUser';
export type LicensePlan = 'Free' | 'Pro' | 'Enterprise';
export type LicenseStatus = 'Active' | 'GracePeriod' | 'Expired';

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errorCode?: string;
  errors?: Record<string, string[]>;
}

export interface SetupStatusResponse {
  isInitialized: boolean;
  instanceId: string;
}

export interface SmtpSettingsRequest {
  host: string;
  port: number;
  useSsl: boolean;
  username?: string | null;
  password?: string | null;
  fromAddress: string;
  fromName?: string | null;
}

export interface InitializeSetupRequest {
  adminEmail: string;
  adminPassword: string;
  defaultLanguage: SupportedLanguage;
  selectedPalette: ColorPalette;
  smtp?: SmtpSettingsRequest | null;
}

export interface InitializeSetupResponse {
  instanceId: string;
  adminUserId: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface UserProfileResponse {
  id: string;
  email: string;
  externalId?: string | null;
  systemRole: SystemRole;
  preferredLanguage: SupportedLanguage;
  preferredTheme: ThemeMode;
  isActive: boolean;
  createdAt: string;
}

export interface AuthTokenResponse {
  accessToken: string;
  refreshToken: string;
  expiresInSeconds: number;
  user: UserProfileResponse;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface LogoutRequest {
  refreshToken: string;
}

export interface ActivateAccountRequest {
  token: string;
  password: string;
}

export interface UpdateUserPreferencesRequest {
  preferredLanguage?: SupportedLanguage;
  preferredTheme?: ThemeMode;
}

export interface UserSummaryResponse {
  id: string;
  email: string;
  externalId?: string | null;
  systemRole: SystemRole;
  preferredLanguage: SupportedLanguage;
  preferredTheme: ThemeMode;
  isActive: boolean;
  groupIds: string[];
  createdAt: string;
}

export interface CreateUserRequest {
  email: string;
  externalId?: string | null;
  systemRole: SystemRole;
  groupIds?: string[];
}

export interface CreateUserResponse {
  id: string;
  email: string;
  externalId?: string | null;
  systemRole: SystemRole;
  isActive: boolean;
  deliveryMethod: InviteDeliveryMethod;
  activationLink?: string | null;
  createdAt: string;
}

export interface CreateGroupRequest {
  name: string;
  code: string;
}

export interface GroupResponse {
  id: string;
  name: string;
  code: string;
  memberCount: number;
  createdAt: string;
}

export interface AddGroupMemberRequest {
  userId: string;
}

export interface GroupMemberResponse {
  userId: string;
  email: string;
  externalId?: string | null;
  systemRole: SystemRole;
  isActive: boolean;
}

export interface CreateProjectRequest {
  name: string;
  description?: string | null;
}

export interface ProjectResponse {
  id: string;
  name: string;
  description?: string | null;
  createdAt: string;
}

export interface CreateWorkflowProcessRequest {
  name: string;
  externalProcessKey: string;
  connectorId?: string | null;
  status?: WorkflowProcessStatus;
}

export interface WorkflowProcessResponse {
  id: string;
  projectId: string;
  connectorId?: string | null;
  name: string;
  externalProcessKey: string;
  status: WorkflowProcessStatus;
  createdAt: string;
}

export interface CreateFormDefinitionRequest {
  name: string;
  stepKey: string;
  draftSchemaJsonb?: Record<string, unknown> | null;
}

export interface FormDefinitionResponse {
  id: string;
  workflowProcessId: string;
  name: string;
  stepKey: string;
  status: FormDefinitionStatus;
  draftSchemaJsonb?: Record<string, unknown> | null;
  updatedAt: string;
}

export interface FormAccessRuleItem {
  id: string;
  groupId?: string | null;
  userId?: string | null;
  accessScope: FormAccessScope;
}

export interface FormAccessRulesResponse {
  formId: string;
  rules: FormAccessRuleItem[];
}

export interface FormAccessRuleInput {
  groupId?: string | null;
  userId?: string | null;
}

export interface UpdateFormAccessRulesRequest {
  rules: FormAccessRuleInput[];
}

export interface LicenseUsage {
  activeProcesses: number;
  activeConnectors: number;
  adminUsers: number;
  endUsers: number;
}

export interface LicenseLimits {
  maxActiveProcesses: number | null;
  maxActiveConnectors: number | null;
  maxAdminUsers: number | null;
  maxEndUsers: number | null;
}

export interface LicensingStatusResponse {
  instanceId: string;
  plan: LicensePlan;
  status: LicenseStatus;
  expiresAt?: string | null;
  gracePeriodEndsAt?: string | null;
  inGracePeriod: boolean;
  usage: LicenseUsage;
  limits: LicenseLimits;
}

export interface ActivateLicenseRequest {
  licenseKey: string;
}
