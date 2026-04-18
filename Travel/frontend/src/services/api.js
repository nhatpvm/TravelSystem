import {
  buildAuthHeaders,
  clearAuthSession,
  getRefreshToken,
  getStoredAuthState,
  setAuthSession,
} from './interceptor';

const API_PREFIX = import.meta.env.VITE_API_PREFIX || '/api/v1';
let refreshPromise = null;

export class ApiError extends Error {
  constructor(message, status, data) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.data = data;
  }
}

async function request(path, options = {}, isRetry = false) {
  const {
    method = 'GET',
    body,
    headers = {},
    auth = true,
  } = options;

  const isFormData = typeof FormData !== 'undefined' && body instanceof FormData;
  const finalHeaders = {
    ...(!isFormData && body ? { 'Content-Type': 'application/json' } : {}),
    ...(auth ? buildAuthHeaders(headers) : headers),
  };

  const response = await fetch(`${API_PREFIX}${path}`, {
    method,
    headers: finalHeaders,
    body: !body ? undefined : isFormData ? body : JSON.stringify(body),
  });

  const data = await parseResponse(response);

  if (!response.ok) {
    if (response.status === 401 && auth && !isRetry && path !== '/auth/refresh') {
      const refreshed = await refreshAuthSession();
      if (refreshed) {
        return request(path, options, true);
      }
    }

    if (response.status === 401) {
      clearAuthSession();
    }

    throw new ApiError(resolveErrorMessage(data), response.status, data);
  }

  return data;
}

async function refreshAuthSession() {
  const refreshToken = getRefreshToken();
  if (!refreshToken) {
    return false;
  }

  if (!refreshPromise) {
    refreshPromise = performRefresh(refreshToken).finally(() => {
      refreshPromise = null;
    });
  }

  return refreshPromise;
}

async function performRefresh(refreshToken) {
  const response = await fetch(`${API_PREFIX}/auth/refresh`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ refreshToken }),
  });

  const data = await parseResponse(response);
  if (!response.ok) {
    clearAuthSession();
    return false;
  }

  const currentState = getStoredAuthState();
  setAuthSession({
    accessToken: data?.accessToken,
    refreshToken: data?.refreshToken,
    sessionId: data?.sessionId,
    accessTokenExpiresAt: data?.expiresAt,
    refreshTokenExpiresAt: data?.refreshTokenExpiresAt,
    user: data?.user || currentState.user,
    memberships: currentState.memberships,
    currentTenantId: currentState.currentTenantId,
    permissions: currentState.permissions,
  });

  return true;
}

async function parseResponse(response) {
  const contentType = response.headers.get('content-type') || '';

  if (contentType.includes('application/json')) {
    return response.json();
  }

  const text = await response.text();
  return text ? { message: text } : null;
}

function resolveErrorMessage(data) {
  if (!data) {
    return 'Yêu cầu thất bại.';
  }

  if (typeof data === 'string') {
    return data;
  }

  if (data.message) {
    if (Array.isArray(data.errors) && data.errors.length > 0) {
      return `${data.message} ${data.errors.join(' ')}`.trim();
    }

    return data.message;
  }

  if (data.title) {
    return data.title;
  }

  if (Array.isArray(data.errors) && data.errors.length > 0) {
    return data.errors.join(' ');
  }

  return 'Yêu cầu thất bại.';
}

export const api = {
  get: (path, options) => request(path, { ...options, method: 'GET' }),
  post: (path, body, options) => request(path, { ...options, method: 'POST', body }),
  put: (path, body, options) => request(path, { ...options, method: 'PUT', body }),
  patch: (path, body, options) => request(path, { ...options, method: 'PATCH', body }),
  delete: (path, options) => request(path, { ...options, method: 'DELETE' }),
};
