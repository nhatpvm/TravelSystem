export const AUTH_STORAGE_KEY = 'auth_token';
export const AUTH_USER_KEY = 'auth_user';
export const AUTH_REFRESH_TOKEN_KEY = 'auth_refresh_token';
export const AUTH_SESSION_ID_KEY = 'auth_session_id';
export const AUTH_EXPIRES_AT_KEY = 'auth_expires_at';
export const AUTH_REFRESH_EXPIRES_AT_KEY = 'auth_refresh_expires_at';
export const AUTH_MEMBERSHIPS_KEY = 'auth_memberships';
export const AUTH_TENANT_KEY = 'auth_current_tenant_id';
export const AUTH_PERMISSIONS_KEY = 'auth_permissions';
export const AUTH_REMEMBER_KEY = 'auth_remember';
export const AUTH_CHANGED_EVENT = 'auth:changed';

export const AUTH_ROLES = {
  ADMIN: 'Admin',
  CUSTOMER: 'Customer',
  TENANT_BUS: 'QLNX',
  TENANT_TRAIN: 'QLVT',
  TENANT_FLIGHT: 'QLVMM',
  TENANT_HOTEL: 'QLKS',
  TENANT_TOUR: 'QLTour',
};

const TENANT_ROLES = new Set([
  AUTH_ROLES.TENANT_BUS,
  AUTH_ROLES.TENANT_TRAIN,
  AUTH_ROLES.TENANT_FLIGHT,
  AUTH_ROLES.TENANT_HOTEL,
  AUTH_ROLES.TENANT_TOUR,
]);

const TENANT_ROLE_ORDER = [
  AUTH_ROLES.TENANT_BUS,
  AUTH_ROLES.TENANT_TRAIN,
  AUTH_ROLES.TENANT_FLIGHT,
  AUTH_ROLES.TENANT_HOTEL,
  AUTH_ROLES.TENANT_TOUR,
];

const TENANT_ROLE_CONFIG = {
  [AUTH_ROLES.TENANT_BUS]: {
    module: 'bus',
    badge: 'BUS OPERATOR',
    defaultPath: '/tenant/inventory/bus',
    permission: 'bus.trips.read',
  },
  [AUTH_ROLES.TENANT_TRAIN]: {
    module: 'train',
    badge: 'TRAIN OPERATOR',
    defaultPath: '/tenant/inventory/train',
    permission: 'train.trips.read',
  },
  [AUTH_ROLES.TENANT_FLIGHT]: {
    module: 'flight',
    badge: 'FLIGHT OPERATOR',
    defaultPath: '/tenant/inventory/flight',
    permission: 'flight.inventory.read',
  },
  [AUTH_ROLES.TENANT_HOTEL]: {
    module: 'hotel',
    badge: 'HOTEL OPERATOR',
    defaultPath: '/tenant/inventory/hotel',
    permission: 'hotel.inventory.read',
  },
  [AUTH_ROLES.TENANT_TOUR]: {
    module: 'tour',
    badge: 'TOUR OPERATOR',
    defaultPath: '/tenant/inventory/tour',
    permission: 'tour.inventory.read',
  },
};

const TENANT_TYPE_CONFIG = {
  bus: TENANT_ROLE_CONFIG[AUTH_ROLES.TENANT_BUS],
  train: TENANT_ROLE_CONFIG[AUTH_ROLES.TENANT_TRAIN],
  flight: TENANT_ROLE_CONFIG[AUTH_ROLES.TENANT_FLIGHT],
  hotel: TENANT_ROLE_CONFIG[AUTH_ROLES.TENANT_HOTEL],
  tour: TENANT_ROLE_CONFIG[AUTH_ROLES.TENANT_TOUR],
};

const TENANT_FALLBACK_PATHS = [
  { path: '/tenant', permission: 'tenant.dashboard.read' },
  { path: '/tenant/cms', permission: 'cms.posts.read' },
  { path: '/tenant/bookings', permission: 'tenant.bookings.read' },
  { path: '/tenant/promos', permission: null },
  { path: '/tenant/reviews', permission: 'tenant.reviews.read' },
  { path: '/tenant/staff', permission: 'tenant.staff.manage' },
  { path: '/tenant/finance', permission: 'tenant.finance.read' },
  { path: '/tenant/reports', permission: 'tenant.reports.read' },
  { path: '/tenant/settings', permission: 'tenant.settings.read' },
];

function normalizeTenantType(value) {
  return String(value || '').trim().toLowerCase();
}

function resolveSessionValue(sessionOrUser) {
  if (sessionOrUser?.user) {
    return sessionOrUser;
  }

  return {
    user: sessionOrUser || null,
    permissions: [],
    memberships: [],
    currentTenant: null,
  };
}

export function getUserRoles(user) {
  return Array.isArray(user?.roles) ? user.roles : [];
}

export function getSessionUser(sessionOrUser) {
  return sessionOrUser?.user || sessionOrUser || null;
}

export function getSessionPermissions(sessionOrUser) {
  if (Array.isArray(sessionOrUser?.permissions)) {
    return sessionOrUser.permissions;
  }

  return [];
}

export function getSessionMemberships(sessionOrUser) {
  if (Array.isArray(sessionOrUser?.memberships)) {
    return sessionOrUser.memberships;
  }

  return [];
}

export function getCurrentTenantMembership(sessionOrUser) {
  const session = resolveSessionValue(sessionOrUser);

  if (session.currentTenant) {
    return session.currentTenant;
  }

  const memberships = getSessionMemberships(session);
  const currentTenantId = session.currentTenantId;
  if (!currentTenantId) {
    return memberships[0] || null;
  }

  return memberships.find((item) => item.tenantId === currentTenantId) || memberships[0] || null;
}

export function isAdminUser(user) {
  return getUserRoles(user).includes(AUTH_ROLES.ADMIN);
}

export function isTenantUser(user) {
  return getUserRoles(user).some((role) => TENANT_ROLES.has(role));
}

export function canAccessAdmin(user) {
  return isAdminUser(user);
}

export function canAccessTenant(user) {
  return isAdminUser(user) || isTenantUser(user);
}

export function getPrimaryTenantRole(user) {
  const roles = getUserRoles(user);
  return TENANT_ROLE_ORDER.find((role) => roles.includes(role)) || null;
}

export function getPrimaryTenantConfig(user) {
  const role = getPrimaryTenantRole(user);
  return role ? TENANT_ROLE_CONFIG[role] : null;
}

export function getCurrentTenantConfig(sessionOrUser) {
  const currentTenant = getCurrentTenantMembership(sessionOrUser);
  const byType = TENANT_TYPE_CONFIG[normalizeTenantType(currentTenant?.type)];
  return byType || getPrimaryTenantConfig(getSessionUser(sessionOrUser));
}

export function hasTenantPermission(sessionOrUser, permissionCode) {
  const user = getSessionUser(sessionOrUser);

  if (isAdminUser(user)) {
    return true;
  }

  if (!permissionCode) {
    return canAccessTenant(user);
  }

  return getSessionPermissions(sessionOrUser).includes(permissionCode);
}

export function canAccessTenantModule(sessionOrUser, module) {
  const user = getSessionUser(sessionOrUser);

  if (isAdminUser(user)) {
    return true;
  }

  if (!module || module === 'generic') {
    return isTenantUser(user);
  }

  const currentTenantConfig = getCurrentTenantConfig(sessionOrUser);
  if (currentTenantConfig) {
    if (currentTenantConfig.module !== module) {
      return false;
    }

    return hasTenantPermission(sessionOrUser, currentTenantConfig.permission);
  }

  const roles = getUserRoles(user);
  return roles.some((role) => {
    const config = TENANT_ROLE_CONFIG[role];
    return config?.module === module;
  });
}

export function getTenantAccessiblePath(sessionOrUser) {
  const user = getSessionUser(sessionOrUser);

  if (isAdminUser(user) && !isTenantUser(user)) {
    return '/tenant';
  }

  const tenantConfig = getCurrentTenantConfig(sessionOrUser);
  if (tenantConfig && canAccessTenantModule(sessionOrUser, tenantConfig.module)) {
    return tenantConfig.defaultPath;
  }

  const allowedPath = TENANT_FALLBACK_PATHS.find((item) => hasTenantPermission(sessionOrUser, item.permission));
  return allowedPath?.path || '/tenant';
}

export function getTenantDefaultPath(sessionOrUser) {
  return getTenantAccessiblePath(sessionOrUser);
}

export function getTenantOperatorBadge(sessionOrUser) {
  const user = getSessionUser(sessionOrUser);

  if (isAdminUser(user) && !isTenantUser(user)) {
    return 'PLATFORM ADMIN';
  }

  return getCurrentTenantConfig(sessionOrUser)?.badge || 'PARTNER OPERATOR';
}

export function getTenantDisplayName(sessionOrUser) {
  const currentTenant = getCurrentTenantMembership(sessionOrUser);
  if (currentTenant?.name) {
    return currentTenant.name;
  }

  return getUserDisplayName(getSessionUser(sessionOrUser));
}

export function getUserDisplayName(user) {
  return user?.fullName?.trim() || user?.userName || user?.email || 'Tài khoản';
}

export function getUserInitials(user) {
  const displayName = getUserDisplayName(user).trim();
  const parts = displayName.split(/\s+/).filter(Boolean);

  if (parts.length === 0) {
    return 'TK';
  }

  if (parts.length === 1) {
    return parts[0].slice(0, 2).toUpperCase();
  }

  return `${parts[0][0]}${parts[parts.length - 1][0]}`.toUpperCase();
}

export function getUserJoinYear(user) {
  if (!user?.createdAt) {
    return null;
  }

  const value = new Date(user.createdAt);
  if (Number.isNaN(value.getTime())) {
    return null;
  }

  return value.getFullYear();
}

export function getPostLoginPath(user) {
  const roles = getUserRoles(user);

  if (roles.includes(AUTH_ROLES.ADMIN)) {
    return '/admin';
  }

  if (roles.some((role) => TENANT_ROLES.has(role))) {
    return '/tenant';
  }

  return '/';
}
