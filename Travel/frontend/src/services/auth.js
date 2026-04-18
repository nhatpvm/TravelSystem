import { api } from './api';
import { listTenantMemberships } from './tenancyService';
import {
  clearAuthSession,
  getCurrentTenantId,
  getStoredAuthState,
  setAuthSession,
  updateStoredUser,
  updateTenantContext,
} from './interceptor';
import { canAccessAdmin, canAccessTenant } from '../modules/auth/types';

function normalizePermissionCodes(response) {
  return Array.isArray(response?.items)
    ? response.items.filter((item) => item?.isGranted).map((item) => item.code)
    : [];
}

function selectTenantId(memberships, preferredTenantId) {
  if (preferredTenantId && memberships.some((item) => item.tenantId === preferredTenantId)) {
    return preferredTenantId;
  }

  return memberships[0]?.tenantId || null;
}

async function loadGrantedPermissions(tenantId) {
  const query = tenantId ? `?tenantId=${encodeURIComponent(tenantId)}&grantedOnly=true` : '?grantedOnly=true';
  const response = await api.get(`/auth/me/permissions${query}`);
  return normalizePermissionCodes(response);
}

async function hydrateTenantContext(user, preferredTenantId) {
  let memberships = [];
  let currentTenantId = null;
  let permissions = [];

  if (canAccessTenant(user)) {
    const membershipResponse = await listTenantMemberships();
    memberships = membershipResponse?.items || [];
    currentTenantId = selectTenantId(memberships, preferredTenantId || getCurrentTenantId());
  }

  if (canAccessTenant(user) || canAccessAdmin(user)) {
    permissions = await loadGrantedPermissions(currentTenantId);
  }

  updateTenantContext({
    memberships,
    currentTenantId,
    permissions,
  });

  return { memberships, currentTenantId, permissions };
}

export async function login(payload) {
  const response = await api.post('/auth/login', payload, { auth: false });

  if (response?.requiresTwoFactor) {
    return response;
  }

  setAuthSession({
    accessToken: response.accessToken,
    refreshToken: response.refreshToken,
    sessionId: response.sessionId,
    accessTokenExpiresAt: response.expiresAt,
    refreshTokenExpiresAt: response.refreshTokenExpiresAt,
    user: response.user,
    remember: payload?.rememberMe !== false,
  });

  await hydrateTenantContext(response.user, null);
  return response;
}

export async function verifyTwoFactorLogin(payload) {
  const response = await api.post('/auth/2fa/verify-login', payload, { auth: false });

  setAuthSession({
    accessToken: response.accessToken,
    refreshToken: response.refreshToken,
    sessionId: response.sessionId,
    accessTokenExpiresAt: response.expiresAt,
    refreshTokenExpiresAt: response.refreshTokenExpiresAt,
    user: response.user,
    remember: payload?.rememberMe !== false,
  });

  await hydrateTenantContext(response.user, null);
  return response;
}

export function register(payload) {
  return api.post('/auth/register', payload, { auth: false });
}

export function forgotPassword(payload) {
  return api.post('/auth/forgot-password', payload, { auth: false });
}

export function resetPassword(payload) {
  return api.post('/auth/reset-password', payload, { auth: false });
}

export function getMe() {
  return api.get('/auth/me');
}

export async function updateMe(payload) {
  const response = await api.put('/auth/me', payload);
  updateStoredUser(response);
  return response;
}

export function changePassword(payload) {
  return api.post('/auth/change-password', payload);
}

export function getTwoFactorStatus() {
  return api.get('/auth/2fa/status');
}

export function setupTwoFactor() {
  return api.post('/auth/2fa/setup', {}).then((response) => {
    if (response?.accessToken) {
      const currentState = getStoredAuthState();
      setAuthSession({
        accessToken: response.accessToken,
        refreshToken: response.refreshToken,
        sessionId: response.sessionId,
        accessTokenExpiresAt: response.expiresAt,
        refreshTokenExpiresAt: response.refreshTokenExpiresAt,
        user: response.user || currentState.user,
        remember: currentState.remember,
      });
    }

    return response;
  });
}

export function enableTwoFactor(payload) {
  return api.post('/auth/2fa/enable', payload);
}

export function disableTwoFactor() {
  return api.post('/auth/2fa/disable', {});
}

export function regenerateTwoFactorRecoveryCodes() {
  return api.post('/auth/2fa/recovery-codes', {});
}

export function listMySessions() {
  return api.get('/auth/sessions');
}

export function revokeMySession(sessionId) {
  return api.delete(`/auth/sessions/${sessionId}`);
}

export async function logout() {
  try {
    await api.post('/auth/logout', {});
  } finally {
    clearAuthSession();
  }
}

export async function logoutAllSessions() {
  try {
    await api.post('/auth/logout-all', {});
  } finally {
    clearAuthSession();
  }
}

export async function deactivateAccount(payload = {}) {
  try {
    return await api.post('/auth/deactivate-account', payload);
  } finally {
    clearAuthSession();
  }
}

export async function switchCurrentTenant(tenantId) {
  const currentState = getStoredAuthState();
  const nextTenantId = selectTenantId(currentState.memberships, tenantId);
  const permissions = await loadGrantedPermissions(nextTenantId);

  updateTenantContext({
    memberships: currentState.memberships,
    currentTenantId: nextTenantId,
    permissions,
  });
}

export async function bootstrapStoredSession() {
  const currentState = getStoredAuthState();
  if (!currentState.isAuthenticated) {
    return currentState;
  }

  try {
    const user = currentState.user || await getMe();
    setAuthSession({ user });
    await hydrateTenantContext(user, currentState.currentTenantId);
    return getStoredAuthState();
  } catch (error) {
    clearAuthSession();
    throw error;
  }
}
