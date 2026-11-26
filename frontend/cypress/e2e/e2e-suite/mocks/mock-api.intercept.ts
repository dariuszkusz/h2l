import type { CyHttpMessages } from 'cypress/types/net-stubbing';
import {
  buildProblemDetails,
  computeLicensingStatus,
  isAdminOrDesigner,
  issueSessionTokens,
  nextUuid,
  toUserProfile,
  VALID_LICENSE_KEYS,
} from './mock-api.state';
import type { MockApiState, StoredUserRecord } from './mock-api.state';
import type {
  ActivateAccountRequest,
  ActivateLicenseRequest,
  AddGroupMemberRequest,
  AuthTokenResponse,
  CreateFormDefinitionRequest,
  CreateGroupRequest,
  CreateProjectRequest,
  CreateUserRequest,
  CreateUserResponse,
  CreateWorkflowProcessRequest,
  FormAccessRuleItem,
  FormAccessScope,
  GroupMemberResponse,
  InitializeSetupRequest,
  LoginRequest,
  LogoutRequest,
  ProblemDetails,
  RefreshTokenRequest,
  UpdateFormAccessRulesRequest,
  UpdateUserPreferencesRequest,
} from './mock-api.types';

function sendProblem(
  req: CyHttpMessages.IncomingHttpRequest,
  problem: ProblemDetails,
): void {
  req.reply({
    statusCode: problem.status ?? 400,
    headers: { 'content-type': 'application/problem+json' },
    body: problem,
  });
}

function getHeaderString(
  headers: Record<string, string | string[]>,
  headerName: string,
): string | undefined {
  const direct = headers[headerName] ?? headers[headerName.toLowerCase()];
  if (Array.isArray(direct)) {
    return direct[0];
  }
  return direct;
}

function authenticateRequest(
  req: CyHttpMessages.IncomingHttpRequest,
  state: MockApiState,
): StoredUserRecord | null {
  const authHeader = getHeaderString(req.headers, 'authorization');
  if (authHeader === undefined || !authHeader.startsWith('Bearer ')) {
    sendProblem(
      req,
      buildProblemDetails(401, 'Unauthorized', 'Missing or invalid Bearer token.', 'UNAUTHORIZED', req.url),
    );
    return null;
  }

  const token = authHeader.slice('Bearer '.length).trim();

  if (state.simulateSingleAccessTokenExpiry) {
    state.simulateSingleAccessTokenExpiry = false;
    state.expiredAccessTokens.add(token);
  }

  if (state.expiredAccessTokens.has(token)) {
    sendProblem(
      req,
      buildProblemDetails(401, 'Token expired', 'Access token has expired.', 'TOKEN_EXPIRED', req.url),
    );
    return null;
  }

  const userId = state.activeAccessTokens[token];
  if (userId === undefined) {
    sendProblem(
      req,
      buildProblemDetails(401, 'Unauthorized', 'Invalid access token.', 'INVALID_TOKEN', req.url),
    );
    return null;
  }

  const user = state.users.find((u) => u.id === userId) ?? null;
  if (user === null || !user.isActive) {
    sendProblem(
      req,
      buildProblemDetails(401, 'Unauthorized', 'User account is inactive or missing.', 'INVALID_USER', req.url),
    );
    return null;
  }

  return user;
}

export function registerMockApi(state: MockApiState): void {
  cy.intercept('GET', '/api/v1/setup/status', (req) => {
    if (state.setupStatusError !== null) {
      const err = state.setupStatusError;
      req.reply({
        statusCode: err.status ?? 503,
        delay: state.setupStatusDelayMs,
        headers: { 'content-type': 'application/problem+json' },
        body: err,
      });
      return;
    }
    req.reply({
      statusCode: 200,
      delay: state.setupStatusDelayMs,
      body: {
        isInitialized: state.isInitialized,
        instanceId: state.instanceId,
      },
    });
  }).as('getSetupStatus');

  cy.intercept('POST', '/api/v1/setup/initialize', (req) => {
    if (state.isInitialized) {
      sendProblem(
        req,
        buildProblemDetails(
          409,
          'Instance already initialized',
          'This h2l instance is already initialized. Please log in.',
          'ALREADY_INITIALIZED',
          req.url,
        ),
      );
      return;
    }

    const body = req.body as InitializeSetupRequest;
    const errors: Record<string, string[]> = {};
    if (!body.adminEmail || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(body.adminEmail)) {
      errors['adminEmail'] = ['Valid admin email is required.'];
    }
    if (!body.adminPassword || body.adminPassword.length < 12 || body.adminPassword.length > 128) {
      errors['adminPassword'] = ['Password must be between 12 and 128 characters.'];
    }
    if (body.smtp !== undefined && body.smtp !== null) {
      if (!body.smtp.host || body.smtp.host.trim().length === 0) {
        errors['smtp.host'] = ['SMTP host is required.'];
      }
      if (!Number.isInteger(body.smtp.port) || body.smtp.port < 1 || body.smtp.port > 65535) {
        errors['smtp.port'] = ['SMTP port must be between 1 and 65535.'];
      }
      if (!body.smtp.fromAddress || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(body.smtp.fromAddress)) {
        errors['smtp.fromAddress'] = ['Valid SMTP fromAddress is required.'];
      }
    }

    if (Object.keys(errors).length > 0) {
      sendProblem(
        req,
        buildProblemDetails(400, 'Validation failed', 'Setup request contains invalid fields.', 'VALIDATION_ERROR', req.url, errors),
      );
      return;
    }

    const adminId = nextUuid(state, 1);
    state.isInitialized = true;
    state.defaultLanguage = body.defaultLanguage;
    state.selectedPalette = body.selectedPalette;
    state.smtpConfigured = body.smtp !== undefined && body.smtp !== null;
    state.users.push({
      id: adminId,
      email: body.adminEmail,
      externalId: null,
      systemRole: 'Admin',
      preferredLanguage: body.defaultLanguage,
      preferredTheme: 'light',
      isActive: true,
      createdAt: '2026-11-25T21:00:00Z',
      password: body.adminPassword,
      groupIds: [],
    });

    req.reply({
      statusCode: 201,
      body: {
        instanceId: state.instanceId,
        adminUserId: adminId,
      },
    });
  }).as('initializeSetup');

  cy.intercept('POST', '/api/v1/auth/login', (req) => {
    const body = req.body as LoginRequest;
    const user = state.users.find(
      (u) => u.email.toLowerCase() === (body.email ?? '').toLowerCase() && u.password === body.password,
    );

    if (user === undefined || !user.isActive) {
      sendProblem(
        req,
        buildProblemDetails(
          401,
          'Invalid credentials',
          'Email or password is invalid, or account is not active.',
          'INVALID_CREDENTIALS',
          req.url,
        ),
      );
      return;
    }

    const tokens = issueSessionTokens(state, user);
    const response: AuthTokenResponse = {
      accessToken: tokens.accessToken,
      refreshToken: tokens.refreshToken,
      expiresInSeconds: 900,
      user: toUserProfile(user),
    };
    req.reply({ statusCode: 200, body: response });
  }).as('login');

  cy.intercept('POST', '/api/v1/auth/refresh', (req) => {
    const body = req.body as RefreshTokenRequest;
    if (!body.refreshToken || state.expiredRefreshTokens.has(body.refreshToken)) {
      sendProblem(
        req,
        buildProblemDetails(401, 'Invalid refresh token', 'Refresh token is expired or revoked.', 'INVALID_REFRESH_TOKEN', req.url),
      );
      return;
    }

    const userId = state.activeRefreshTokens[body.refreshToken];
    const user = state.users.find((u) => u.id === userId);
    if (user === undefined) {
      sendProblem(
        req,
        buildProblemDetails(401, 'Invalid refresh token', 'Refresh token not recognized.', 'INVALID_REFRESH_TOKEN', req.url),
      );
      return;
    }

    delete state.activeRefreshTokens[body.refreshToken];
    const tokens = issueSessionTokens(state, user);
    req.reply({
      statusCode: 200,
      body: {
        accessToken: tokens.accessToken,
        refreshToken: tokens.refreshToken,
        expiresInSeconds: 900,
        user: toUserProfile(user),
      },
    });
  }).as('refreshToken');

  cy.intercept('POST', '/api/v1/auth/logout', (req) => {
    const user = authenticateRequest(req, state);
    if (user === null) {
      return;
    }
    const body = req.body as LogoutRequest;
    const authHeader = getHeaderString(req.headers, 'authorization') ?? '';
    const accessToken = authHeader.slice('Bearer '.length).trim();
    delete state.activeAccessTokens[accessToken];
    if (body.refreshToken) {
      delete state.activeRefreshTokens[body.refreshToken];
      state.expiredRefreshTokens.add(body.refreshToken);
    }
    req.reply({ statusCode: 204, body: '' });
  }).as('logout');

  cy.intercept('POST', '/api/v1/auth/activate', (req) => {
    const body = req.body as ActivateAccountRequest;
    if (!body.token || body.token.trim().length === 0) {
      sendProblem(
        req,
        buildProblemDetails(400, 'Missing token', 'Activation token is required.', 'INVALID_ACTIVATION_TOKEN', req.url, {
          token: ['Activation token is required.'],
        }),
      );
      return;
    }
    if (!body.password || body.password.length < 12 || body.password.length > 128) {
      sendProblem(
        req,
        buildProblemDetails(400, 'Invalid password', 'Password must be between 12 and 128 characters.', 'VALIDATION_ERROR', req.url, {
          password: ['Password must be between 12 and 128 characters.'],
        }),
      );
      return;
    }

    const tokenRecord = state.activationTokens.find((t) => t.token === body.token);
    if (tokenRecord === undefined) {
      sendProblem(
        req,
        buildProblemDetails(400, 'Invalid token', 'Activation token is invalid.', 'INVALID_ACTIVATION_TOKEN', req.url),
      );
      return;
    }
    if (tokenRecord.used || tokenRecord.expired) {
      sendProblem(
        req,
        buildProblemDetails(409, 'Token already used', 'Activation token has already been used or expired.', 'ACTIVATION_TOKEN_USED', req.url),
      );
      return;
    }

    const targetUser = state.users.find((u) => u.id === tokenRecord.userId);
    if (targetUser !== undefined) {
      targetUser.isActive = true;
      targetUser.password = body.password;
    }
    tokenRecord.used = true;
    req.reply({ statusCode: 204, body: '' });
  }).as('activateAccount');

  cy.intercept('GET', '/api/v1/users/me', (req) => {
    const user = authenticateRequest(req, state);
    if (user === null) {
      return;
    }
    req.reply({ statusCode: 200, body: toUserProfile(user) });
  }).as('getMe');

  cy.intercept('PATCH', '/api/v1/users/me/preferences', (req) => {
    const user = authenticateRequest(req, state);
    if (user === null) {
      return;
    }
    if (state.preferencesError !== null) {
      sendProblem(req, state.preferencesError);
      return;
    }
    const body = req.body as UpdateUserPreferencesRequest;
    if (body.preferredLanguage !== undefined) {
      user.preferredLanguage = body.preferredLanguage;
    }
    if (body.preferredTheme !== undefined) {
      user.preferredTheme = body.preferredTheme;
    }
    req.reply({ statusCode: 204, body: '' });
  }).as('updatePreferences');

  cy.intercept('GET', '/api/v1/users', (req) => {
    const user = authenticateRequest(req, state);
    if (user === null) {
      return;
    }
    if (user.systemRole !== 'Admin') {
      sendProblem(
        req,
        buildProblemDetails(403, 'Forbidden', 'Admin role is required to list users.', 'FORBIDDEN', req.url),
      );
      return;
    }
    req.reply({
      statusCode: 200,
      body: state.users.map((u) => ({
        ...toUserProfile(u),
        groupIds: [...u.groupIds],
      })),
    });
  }).as('listUsers');

  cy.intercept('POST', '/api/v1/users', (req) => {
    const caller = authenticateRequest(req, state);
    if (caller === null) {
      return;
    }
    if (caller.systemRole !== 'Admin') {
      sendProblem(
        req,
        buildProblemDetails(403, 'Forbidden', 'Admin role is required to invite users.', 'FORBIDDEN', req.url),
      );
      return;
    }

    const body = req.body as CreateUserRequest;
    const errors: Record<string, string[]> = {};
    if (!body.email || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(body.email)) {
      errors['email'] = ['Valid email address is required.'];
    }
    if (body.externalId !== undefined && body.externalId !== null && body.externalId.length > 200) {
      errors['externalId'] = ['externalId must not exceed 200 characters.'];
    }
    if (Object.keys(errors).length > 0) {
      sendProblem(
        req,
        buildProblemDetails(400, 'Validation failed', 'User invitation payload is invalid.', 'VALIDATION_ERROR', req.url, errors),
      );
      return;
    }

    if (state.users.some((u) => u.email.toLowerCase() === body.email.toLowerCase())) {
      sendProblem(
        req,
        buildProblemDetails(409, 'Duplicate email', `User with email ${body.email} already exists.`, 'DUPLICATE_EMAIL', req.url),
      );
      return;
    }

    const licensing = computeLicensingStatus(state);
    if (
      body.systemRole === 'Admin' &&
      licensing.limits.maxAdminUsers !== null &&
      licensing.usage.adminUsers >= licensing.limits.maxAdminUsers
    ) {
      sendProblem(
        req,
        buildProblemDetails(
          402,
          'Free plan limit exceeded',
          `Free plan allows at most ${licensing.limits.maxAdminUsers} Admin user. Activate a license for instance ${state.instanceId}.`,
          'FREE_PLAN_ADMIN_LIMIT_EXCEEDED',
          req.url,
        ),
      );
      return;
    }

    const newUserId = nextUuid(state, 1);
    const groupIds = body.groupIds ?? [];
    const token = `act_${newUserId}`;
    const newUser: StoredUserRecord = {
      id: newUserId,
      email: body.email,
      externalId: body.externalId ?? null,
      systemRole: body.systemRole,
      preferredLanguage: state.defaultLanguage,
      preferredTheme: 'light',
      isActive: false,
      createdAt: '2026-11-25T21:10:00Z',
      password: '',
      groupIds: [...groupIds],
    };
    state.users.push(newUser);
    state.activationTokens.push({ token, userId: newUserId, used: false, expired: false });

    for (const gid of groupIds) {
      const grp = state.groups.find((g) => g.id === gid);
      if (grp !== undefined) {
        grp.memberCount += 1;
      }
    }

    const response: CreateUserResponse = {
      id: newUser.id,
      email: newUser.email,
      externalId: newUser.externalId ?? null,
      systemRole: newUser.systemRole,
      isActive: false,
      deliveryMethod: state.smtpConfigured ? 'smtp' : 'manual_link',
      activationLink: state.smtpConfigured ? null : `http://localhost:4200/activate?token=${token}`,
      createdAt: newUser.createdAt,
    };
    req.reply({ statusCode: 201, body: response });
  }).as('createUser');

  cy.intercept('GET', '/api/v1/groups', (req) => {
    const caller = authenticateRequest(req, state);
    if (caller === null) {
      return;
    }
    req.reply({ statusCode: 200, body: state.groups });
  }).as('listGroups');

  cy.intercept('POST', '/api/v1/groups', (req) => {
    const caller = authenticateRequest(req, state);
    if (caller === null) {
      return;
    }
    if (caller.systemRole !== 'Admin') {
      sendProblem(req, buildProblemDetails(403, 'Forbidden', 'Admin role required.', 'FORBIDDEN', req.url));
      return;
    }
    const body = req.body as CreateGroupRequest;
    if (state.groups.some((g) => g.code.toLowerCase() === (body.code ?? '').toLowerCase())) {
      sendProblem(
        req,
        buildProblemDetails(409, 'Duplicate group code', `Group code ${body.code} already exists.`, 'DUPLICATE_GROUP_CODE', req.url),
      );
      return;
    }
    const group = {
      id: nextUuid(state, 2),
      name: body.name,
      code: body.code,
      memberCount: 0,
      createdAt: '2026-11-25T21:12:00Z',
    };
    state.groups.push(group);
    req.reply({ statusCode: 201, body: group });
  }).as('createGroup');

  cy.intercept('GET', /\/api\/v1\/groups\/([^/]+)\/members$/, (req) => {
    const caller = authenticateRequest(req, state);
    if (caller === null) {
      return;
    }
    const match = req.url.match(/\/api\/v1\/groups\/([^/]+)\/members$/);
    const groupId = match?.[1] ?? '';
    const group = state.groups.find((g) => g.id === groupId);
    if (group === undefined) {
      sendProblem(req, buildProblemDetails(404, 'Not found', 'Group not found.', 'GROUP_NOT_FOUND', req.url));
      return;
    }
    const members: GroupMemberResponse[] = state.users
      .filter((u) => u.groupIds.includes(groupId))
      .map((u) => ({
        userId: u.id,
        email: u.email,
        externalId: u.externalId ?? null,
        systemRole: u.systemRole,
        isActive: u.isActive,
      }));
    req.reply({ statusCode: 200, body: members });
  }).as('listGroupMembers');

  cy.intercept('POST', /\/api\/v1\/groups\/([^/]+)\/members$/, (req) => {
    const caller = authenticateRequest(req, state);
    if (caller === null) {
      return;
    }
    if (caller.systemRole !== 'Admin') {
      sendProblem(req, buildProblemDetails(403, 'Forbidden', 'Admin role required.', 'FORBIDDEN', req.url));
      return;
    }
    const match = req.url.match(/\/api\/v1\/groups\/([^/]+)\/members$/);
    const groupId = match?.[1] ?? '';
    const group = state.groups.find((g) => g.id === groupId);
    if (group === undefined) {
      sendProblem(req, buildProblemDetails(404, 'Not found', 'Group not found.', 'GROUP_NOT_FOUND', req.url));
      return;
    }
    const body = req.body as AddGroupMemberRequest;
    const targetUser = state.users.find((u) => u.id === body.userId);
    if (targetUser === undefined) {
      sendProblem(req, buildProblemDetails(404, 'Not found', 'User not found.', 'USER_NOT_FOUND', req.url));
      return;
    }
    if (targetUser.groupIds.includes(groupId)) {
      sendProblem(
        req,
        buildProblemDetails(409, 'Already a member', 'User is already a member of this group.', 'DUPLICATE_GROUP_MEMBER', req.url),
      );
      return;
    }
    targetUser.groupIds.push(groupId);
    group.memberCount += 1;
    const member: GroupMemberResponse = {
      userId: targetUser.id,
      email: targetUser.email,
      externalId: targetUser.externalId ?? null,
      systemRole: targetUser.systemRole,
      isActive: targetUser.isActive,
    };
    req.reply({ statusCode: 201, body: member });
  }).as('addGroupMember');

  cy.intercept('DELETE', /\/api\/v1\/groups\/([^/]+)\/members\/([^/]+)$/, (req) => {
    const caller = authenticateRequest(req, state);
    if (caller === null) {
      return;
    }
    if (caller.systemRole !== 'Admin') {
      sendProblem(req, buildProblemDetails(403, 'Forbidden', 'Admin role required.', 'FORBIDDEN', req.url));
      return;
    }
    const match = req.url.match(/\/api\/v1\/groups\/([^/]+)\/members\/([^/]+)$/);
    const groupId = match?.[1] ?? '';
    const userId = match?.[2] ?? '';
    const group = state.groups.find((g) => g.id === groupId);
    const targetUser = state.users.find((u) => u.id === userId);
    if (group === undefined || targetUser === undefined) {
      sendProblem(req, buildProblemDetails(404, 'Not found', 'Group or user not found.', 'NOT_FOUND', req.url));
      return;
    }
    targetUser.groupIds = targetUser.groupIds.filter((id) => id !== groupId);
    group.memberCount = Math.max(0, group.memberCount - 1);
    req.reply({ statusCode: 204, body: '' });
  }).as('removeGroupMember');

  cy.intercept('GET', '/api/v1/projects', (req) => {
    const caller = authenticateRequest(req, state);
    if (caller === null) {
      return;
    }
    req.reply({ statusCode: 200, body: state.projects });
  }).as('listProjects');

  cy.intercept('POST', '/api/v1/projects', (req) => {
    const caller = authenticateRequest(req, state);
    if (caller === null) {
      return;
    }
    if (caller.systemRole !== 'Admin') {
      sendProblem(req, buildProblemDetails(403, 'Forbidden', 'Admin role required.', 'FORBIDDEN', req.url));
      return;
    }
    const body = req.body as CreateProjectRequest;
    const project = {
      id: nextUuid(state, 3),
      name: body.name,
      description: body.description ?? null,
      createdAt: '2026-11-25T21:14:00Z',
    };
    state.projects.push(project);
    req.reply({ statusCode: 201, body: project });
  }).as('createProject');

  cy.intercept('GET', /\/api\/v1\/projects\/([^/]+)\/processes$/, (req) => {
    const caller = authenticateRequest(req, state);
    if (caller === null) {
      return;
    }
    const match = req.url.match(/\/api\/v1\/projects\/([^/]+)\/processes$/);
    const projectId = match?.[1] ?? '';
    if (!state.projects.some((p) => p.id === projectId)) {
      sendProblem(req, buildProblemDetails(404, 'Not found', 'Project not found.', 'PROJECT_NOT_FOUND', req.url));
      return;
    }
    req.reply({
      statusCode: 200,
      body: state.processes.filter((proc) => proc.projectId === projectId),
    });
  }).as('listProcesses');

  cy.intercept('POST', /\/api\/v1\/projects\/([^/]+)\/processes$/, (req) => {
    const caller = authenticateRequest(req, state);
    if (caller === null) {
      return;
    }
    if (!isAdminOrDesigner(caller.systemRole)) {
      sendProblem(req, buildProblemDetails(403, 'Forbidden', 'Admin or ProcessDesigner role required.', 'FORBIDDEN', req.url));
      return;
    }
    const match = req.url.match(/\/api\/v1\/projects\/([^/]+)\/processes$/);
    const projectId = match?.[1] ?? '';
    if (!state.projects.some((p) => p.id === projectId)) {
      sendProblem(req, buildProblemDetails(404, 'Not found', 'Project not found.', 'PROJECT_NOT_FOUND', req.url));
      return;
    }

    const body = req.body as CreateWorkflowProcessRequest;
    if (
      state.processes.some(
        (p) => p.projectId === projectId && p.externalProcessKey.toLowerCase() === body.externalProcessKey.toLowerCase(),
      )
    ) {
      sendProblem(
        req,
        buildProblemDetails(
          409,
          'Duplicate process key',
          `Process key "${body.externalProcessKey}" already exists in this project.`,
          'DUPLICATE_EXTERNAL_PROCESS_KEY',
          req.url,
        ),
      );
      return;
    }

    const desiredStatus = body.status ?? 'Active';
    const licensing = computeLicensingStatus(state);
    if (
      desiredStatus === 'Active' &&
      licensing.limits.maxActiveProcesses !== null &&
      licensing.usage.activeProcesses >= licensing.limits.maxActiveProcesses
    ) {
      sendProblem(
        req,
        buildProblemDetails(
          402,
          'License limit exceeded',
          `Active workflow process limit (${licensing.limits.maxActiveProcesses}) reached. Activate or renew license for instance ${state.instanceId}.`,
          state.licenseStatus === 'Expired' ? 'LICENSE_EXPIRED_LIMIT_EXCEEDED' : 'FREE_PLAN_PROCESS_LIMIT_EXCEEDED',
          req.url,
        ),
      );
      return;
    }

    const process = {
      id: nextUuid(state, 4),
      projectId,
      connectorId: body.connectorId ?? null,
      name: body.name,
      externalProcessKey: body.externalProcessKey,
      status: desiredStatus,
      createdAt: '2026-11-25T21:16:00Z',
    };
    state.processes.push(process);
    req.reply({ statusCode: 201, body: process });
  }).as('createProcess');

  cy.intercept('GET', /\/api\/v1\/processes\/([^/]+)\/forms$/, (req) => {
    const caller = authenticateRequest(req, state);
    if (caller === null) {
      return;
    }
    const match = req.url.match(/\/api\/v1\/processes\/([^/]+)\/forms$/);
    const processId = match?.[1] ?? '';
    if (!state.processes.some((p) => p.id === processId)) {
      sendProblem(req, buildProblemDetails(404, 'Not found', 'Workflow process not found.', 'PROCESS_NOT_FOUND', req.url));
      return;
    }
    req.reply({
      statusCode: 200,
      body: state.forms.filter((f) => f.workflowProcessId === processId),
    });
  }).as('listForms');

  cy.intercept('POST', /\/api\/v1\/processes\/([^/]+)\/forms$/, (req) => {
    const caller = authenticateRequest(req, state);
    if (caller === null) {
      return;
    }
    if (!isAdminOrDesigner(caller.systemRole)) {
      sendProblem(req, buildProblemDetails(403, 'Forbidden', 'Admin or ProcessDesigner role required.', 'FORBIDDEN', req.url));
      return;
    }
    const match = req.url.match(/\/api\/v1\/processes\/([^/]+)\/forms$/);
    const processId = match?.[1] ?? '';
    if (!state.processes.some((p) => p.id === processId)) {
      sendProblem(req, buildProblemDetails(404, 'Not found', 'Workflow process not found.', 'PROCESS_NOT_FOUND', req.url));
      return;
    }
    const body = req.body as CreateFormDefinitionRequest;
    const form = {
      id: nextUuid(state, 5),
      workflowProcessId: processId,
      name: body.name,
      stepKey: body.stepKey,
      status: 'Draft' as const,
      draftSchemaJsonb: body.draftSchemaJsonb ?? {},
      updatedAt: '2026-11-25T21:18:00Z',
    };
    state.forms.push(form);
    state.accessRulesByForm[form.id] = [];
    req.reply({ statusCode: 201, body: form });
  }).as('createForm');

  cy.intercept('GET', /\/api\/v1\/forms\/([^/]+)\/access-rules$/, (req) => {
    const caller = authenticateRequest(req, state);
    if (caller === null) {
      return;
    }
    const match = req.url.match(/\/api\/v1\/forms\/([^/]+)\/access-rules$/);
    const formId = match?.[1] ?? '';
    if (!state.forms.some((f) => f.id === formId)) {
      sendProblem(req, buildProblemDetails(404, 'Not found', 'Form not found.', 'FORM_NOT_FOUND', req.url));
      return;
    }
    req.reply({
      statusCode: 200,
      body: {
        formId,
        rules: state.accessRulesByForm[formId] ?? [],
      },
    });
  }).as('getFormAccessRules');

  cy.intercept('PUT', /\/api\/v1\/forms\/([^/]+)\/access-rules$/, (req) => {
    const caller = authenticateRequest(req, state);
    if (caller === null) {
      return;
    }
    if (!isAdminOrDesigner(caller.systemRole)) {
      sendProblem(req, buildProblemDetails(403, 'Forbidden', 'Admin or ProcessDesigner role required.', 'FORBIDDEN', req.url));
      return;
    }
    const match = req.url.match(/\/api\/v1\/forms\/([^/]+)\/access-rules$/);
    const formId = match?.[1] ?? '';
    if (!state.forms.some((f) => f.id === formId)) {
      sendProblem(req, buildProblemDetails(404, 'Not found', 'Form not found.', 'FORM_NOT_FOUND', req.url));
      return;
    }

    const body = req.body as UpdateFormAccessRulesRequest;
    const savedRules: FormAccessRuleItem[] = [];

    for (const rule of body.rules ?? []) {
      const groupId = rule.groupId ?? null;
      const userId = rule.userId ?? null;

      if (groupId === null && userId === null) {
        sendProblem(
          req,
          buildProblemDetails(
            400,
            'Invalid access rule',
            'Each ACL rule must specify groupId, userId, or both.',
            'INVALID_ACL_RULE',
            req.url,
          ),
        );
        return;
      }

      let scope: FormAccessScope;
      if (groupId !== null && userId !== null) {
        const targetUser = state.users.find((u) => u.id === userId);
        if (targetUser === undefined || !targetUser.groupIds.includes(groupId)) {
          sendProblem(
            req,
            buildProblemDetails(
              400,
              'Invalid group subset user',
              'User selected for GroupUserSubset must belong to the specified group.',
              'USER_NOT_IN_GROUP',
              req.url,
            ),
          );
          return;
        }
        scope = 'GroupUserSubset';
      } else if (groupId !== null) {
        scope = 'WholeGroup';
      } else {
        scope = 'DirectUser';
      }

      savedRules.push({
        id: nextUuid(state, 6),
        groupId,
        userId,
        accessScope: scope,
      });
    }

    state.accessRulesByForm[formId] = savedRules;
    req.reply({ statusCode: 204, body: '' });
  }).as('updateFormAccessRules');

  cy.intercept('GET', '/api/v1/licensing/status', (req) => {
    const caller = authenticateRequest(req, state);
    if (caller === null) {
      return;
    }
    if (caller.systemRole !== 'Admin') {
      sendProblem(
        req,
        buildProblemDetails(403, 'Forbidden', 'Admin role is required to view licensing status.', 'FORBIDDEN', req.url),
      );
      return;
    }
    req.reply({ statusCode: 200, body: computeLicensingStatus(state) });
  }).as('getLicensingStatus');

  cy.intercept('POST', '/api/v1/licensing/activate', (req) => {
    const caller = authenticateRequest(req, state);
    if (caller === null) {
      return;
    }
    if (caller.systemRole !== 'Admin') {
      sendProblem(
        req,
        buildProblemDetails(403, 'Forbidden', 'Admin role is required to activate license keys.', 'FORBIDDEN', req.url),
      );
      return;
    }

    const body = req.body as ActivateLicenseRequest;
    const key = (body.licenseKey ?? '').trim();

    if (key.length === 0) {
      sendProblem(
        req,
        buildProblemDetails(400, 'Missing license key', 'License key must not be empty.', 'EMPTY_LICENSE_KEY', req.url),
      );
      return;
    }
    if (key === VALID_LICENSE_KEYS.wrongInstance || (key.includes(MOCK_INSTANCE_MISMATCH_MARKER) && !key.includes(state.instanceId))) {
      sendProblem(
        req,
        buildProblemDetails(
          400,
          'InstanceId mismatch',
          `License key does not match this instance (${state.instanceId}).`,
          'INSTANCE_ID_MISMATCH',
          req.url,
        ),
      );
      return;
    }
    if (key === VALID_LICENSE_KEYS.expiredRenewal) {
      sendProblem(
        req,
        buildProblemDetails(400, 'License expired', 'Submitted license key has already expired.', 'LICENSE_KEY_EXPIRED', req.url),
      );
      return;
    }
    if (key === VALID_LICENSE_KEYS.pro || key === VALID_LICENSE_KEYS.renewedPro) {
      state.plan = 'Pro';
      state.licenseStatus = 'Active';
      state.expiresAt = '2028-11-25T21:00:00Z';
      state.gracePeriodEndsAt = null;
      req.reply({ statusCode: 200, body: computeLicensingStatus(state) });
      return;
    }
    if (key === VALID_LICENSE_KEYS.enterprise) {
      state.plan = 'Enterprise';
      state.licenseStatus = 'Active';
      state.expiresAt = '2028-11-25T21:00:00Z';
      state.gracePeriodEndsAt = null;
      req.reply({ statusCode: 200, body: computeLicensingStatus(state) });
      return;
    }

    sendProblem(
      req,
      buildProblemDetails(
        400,
        'Invalid Ed25519 signature',
        'License key signature verification failed offline.',
        'INVALID_LICENSE_SIGNATURE',
        req.url,
      ),
    );
  }).as('activateLicense');
}

const MOCK_INSTANCE_MISMATCH_MARKER = '999999999999';
