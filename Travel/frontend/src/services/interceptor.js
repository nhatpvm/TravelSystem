import {
  AUTH_CHANGED_EVENT,
  AUTH_EXPIRES_AT_KEY,
  AUTH_MEMBERSHIPS_KEY,
  AUTH_PERMISSIONS_KEY,
  AUTH_REFRESH_EXPIRES_AT_KEY,
  AUTH_REFRESH_TOKEN_KEY,
  AUTH_REMEMBER_KEY,
  AUTH_SESSION_ID_KEY,
  AUTH_STORAGE_KEY,
  AUTH_TENANT_KEY,
  AUTH_USER_KEY,
} from '../modules/auth/types';

const AUTH_KEYS = [
  AUTH_STORAGE_KEY,
  AUTH_REFRESH_TOKEN_KEY,
  AUTH_SESSION_ID_KEY,
  AUTH_EXPIRES_AT_KEY,
  AUTH_REFRESH_EXPIRES_AT_KEY,
  AUTH_USER_KEY,
  AUTH_MEMBERSHIPS_KEY,
  AUTH_TENANT_KEY,
  AUTH_PERMISSIONS_KEY,
  AUTH_REMEMBER_KEY,
];

function getStorage(type) {
  return type === 'session' ? window.sessionStorage : window.localStorage;
}

function readRaw(key) {
  const sessionValue = window.sessionStorage.getItem(key);
  if (sessionValue !== null) {
    return sessionValue;
  }

  return window.localStorage.getItem(key);
}

function getActiveStorageType() {
  const remembered = readRaw(AUTH_REMEMBER_KEY);
  if (remembered === 'false') {
    return 'session';
  }

  if (remembered === 'true') {
    return 'local';
  }

  if (window.sessionStorage.getItem(AUTH_STORAGE_KEY)) {
    return 'session';
  }

  return 'local';
}

function readJson(key, fallback) {
  const raw = readRaw(key);
  if (!raw) {
    return fallback;
  }

  try {
    return JSON.parse(raw);
  } catch {
    return fallback;
  }
}

function readRememberFlag() {
  return readRaw(AUTH_REMEMBER_KEY) !== 'false';
}

function clearAllStorages() {
  [window.localStorage, window.sessionStorage].forEach((storage) => {
    AUTH_KEYS.forEach((key) => storage.removeItem(key));
  });
}

function writeValue(storage, key, value) {
  if (value === null || value === '') {
    storage.removeItem(key);
    return;
  }

  storage.setItem(key, String(value));
}

function writeJson(storage, key, value) {
  if (value === null) {
    storage.removeItem(key);
    return;
  }

  storage.setItem(key, JSON.stringify(value));
}

function buildPersistedSession(next = {}) {
  const current = getStoredAuthState();
  return {
    accessToken: next.accessToken ?? current.accessToken ?? null,
    refreshToken: next.refreshToken ?? current.refreshToken ?? null,
    sessionId: next.sessionId ?? current.sessionId ?? null,
    accessTokenExpiresAt: next.accessTokenExpiresAt ?? current.accessTokenExpiresAt ?? null,
    refreshTokenExpiresAt: next.refreshTokenExpiresAt ?? current.refreshTokenExpiresAt ?? null,
    user: next.user ?? current.user ?? null,
    memberships: next.memberships ?? current.memberships ?? [],
    currentTenantId: next.currentTenantId ?? current.currentTenantId ?? null,
    permissions: next.permissions ?? current.permissions ?? [],
    remember: next.remember ?? current.remember ?? true,
  };
}

export function getAccessToken() {
  return readRaw(AUTH_STORAGE_KEY);
}

export function getRefreshToken() {
  return readRaw(AUTH_REFRESH_TOKEN_KEY);
}

export function getSessionId() {
  return readRaw(AUTH_SESSION_ID_KEY);
}

export function getStoredUser() {
  return readJson(AUTH_USER_KEY, null);
}

export function getStoredMemberships() {
  const items = readJson(AUTH_MEMBERSHIPS_KEY, []);
  return Array.isArray(items) ? items : [];
}

export function getCurrentTenantId() {
  return readRaw(AUTH_TENANT_KEY);
}

export function getStoredPermissions() {
  const items = readJson(AUTH_PERMISSIONS_KEY, []);
  return Array.isArray(items) ? items : [];
}

export function isAuthenticated() {
  return !!getAccessToken();
}

export function getStoredAuthState() {
  const memberships = getStoredMemberships();
  const currentTenantId = getCurrentTenantId();
  const remember = readRememberFlag();

  return {
    accessToken: getAccessToken(),
    refreshToken: getRefreshToken(),
    sessionId: getSessionId(),
    accessTokenExpiresAt: readRaw(AUTH_EXPIRES_AT_KEY),
    refreshTokenExpiresAt: readRaw(AUTH_REFRESH_EXPIRES_AT_KEY),
    user: getStoredUser(),
    memberships,
    currentTenantId,
    currentTenant: memberships.find((item) => item.tenantId === currentTenantId) || memberships[0] || null,
    permissions: getStoredPermissions(),
    remember,
    isAuthenticated: isAuthenticated(),
  };
}

export function setAuthSession(session = {}) {
  const next = buildPersistedSession(session);
  const targetStorage = getStorage(next.remember ? 'local' : 'session');

  clearAllStorages();

  if (next.accessToken) {
    writeValue(targetStorage, AUTH_STORAGE_KEY, next.accessToken);
  }

  if (next.refreshToken) {
    writeValue(targetStorage, AUTH_REFRESH_TOKEN_KEY, next.refreshToken);
  }

  if (next.sessionId) {
    writeValue(targetStorage, AUTH_SESSION_ID_KEY, next.sessionId);
  }

  if (next.accessTokenExpiresAt) {
    writeValue(targetStorage, AUTH_EXPIRES_AT_KEY, next.accessTokenExpiresAt);
  }

  if (next.refreshTokenExpiresAt) {
    writeValue(targetStorage, AUTH_REFRESH_EXPIRES_AT_KEY, next.refreshTokenExpiresAt);
  }

  if (next.user) {
    writeJson(targetStorage, AUTH_USER_KEY, next.user);
  }

  writeJson(targetStorage, AUTH_MEMBERSHIPS_KEY, next.memberships);
  writeValue(targetStorage, AUTH_TENANT_KEY, next.currentTenantId);
  writeJson(targetStorage, AUTH_PERMISSIONS_KEY, next.permissions);
  writeValue(targetStorage, AUTH_REMEMBER_KEY, next.remember ? 'true' : 'false');

  notifyAuthChanged();
}

export function updateStoredUser(user) {
  const next = buildPersistedSession({ user });
  setAuthSession(next);
}

export function updateTenantContext({ memberships, currentTenantId, permissions } = {}) {
  setAuthSession({ memberships, currentTenantId, permissions });
}

export function clearAuthSession() {
  clearAllStorages();
  notifyAuthChanged();
}

export function buildAuthHeaders(headers = {}) {
  const token = getAccessToken();
  const currentTenantId = getCurrentTenantId();

  if (!token) {
    return headers;
  }

  return {
    ...headers,
    Authorization: `Bearer ${token}`,
    ...(currentTenantId ? { 'X-TenantId': currentTenantId } : {}),
  };
}

export function subscribeToAuthChanges(callback) {
  const trackedKeys = new Set(AUTH_KEYS);

  const handleStorage = (event) => {
    if (!event.key || trackedKeys.has(event.key)) {
      callback();
    }
  };

  const handleCustom = () => {
    callback();
  };

  window.addEventListener('storage', handleStorage);
  window.addEventListener(AUTH_CHANGED_EVENT, handleCustom);

  return () => {
    window.removeEventListener('storage', handleStorage);
    window.removeEventListener(AUTH_CHANGED_EVENT, handleCustom);
  };
}

function notifyAuthChanged() {
  window.dispatchEvent(new CustomEvent(AUTH_CHANGED_EVENT));
}

export function getActiveStorage() {
  return getStorage(getActiveStorageType());
}
