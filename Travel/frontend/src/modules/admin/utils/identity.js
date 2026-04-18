import { AUTH_ROLES } from '../../auth/types';

export const ROLE_BADGE_MAP = {
  [AUTH_ROLES.ADMIN]: 'bg-purple-100 text-purple-700',
  [AUTH_ROLES.CUSTOMER]: 'bg-slate-100 text-slate-600',
  [AUTH_ROLES.TENANT_BUS]: 'bg-blue-100 text-blue-700',
  [AUTH_ROLES.TENANT_TRAIN]: 'bg-blue-100 text-blue-700',
  [AUTH_ROLES.TENANT_FLIGHT]: 'bg-blue-100 text-blue-700',
  [AUTH_ROLES.TENANT_HOTEL]: 'bg-blue-100 text-blue-700',
  [AUTH_ROLES.TENANT_TOUR]: 'bg-blue-100 text-blue-700',
};

export const EFFECT_BADGE_MAP = {
  Allow: 'bg-emerald-100 text-emerald-700',
  Deny: 'bg-rose-100 text-rose-700',
};

export function formatDate(value) {
  if (!value) {
    return '--';
  }

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return '--';
  }

  return date.toLocaleDateString('vi-VN');
}

export function getUserRoleType(user) {
  const roles = Array.isArray(user?.Roles) ? user.Roles : Array.isArray(user?.roles) ? user.roles : [];

  if (roles.includes(AUTH_ROLES.ADMIN)) {
    return 'admin';
  }

  if (roles.some((role) => role !== AUTH_ROLES.CUSTOMER)) {
    return 'tenant';
  }

  return 'customer';
}

export function getRoleBadgeClass(roleName) {
  return ROLE_BADGE_MAP[roleName] || 'bg-slate-100 text-slate-600';
}

export function getUserStatus(user) {
  const lockoutEnd = user?.LockoutEnd || user?.lockoutEnd;

  if (lockoutEnd && new Date(lockoutEnd).getTime() > Date.now()) {
    return 'locked';
  }

  if (user?.IsActive === false || user?.isActive === false) {
    return 'inactive';
  }

  return 'active';
}

export function getPermissionCategories(items) {
  return [...new Set((items || []).map((item) => item.Category || item.category).filter(Boolean))];
}
