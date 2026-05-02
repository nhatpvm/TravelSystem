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

export const PERMISSION_CATEGORY_LABELS = {
  bus: 'Xe khách',
  train: 'Tàu hỏa',
  flight: 'Máy bay',
  hotel: 'Khách sạn',
  tour: 'Tour',
  cms: 'CMS',
  tenant: 'Đối tác',
  tenants: 'Quản lý đối tác',
  ticketing: 'Vé',
};

export const PERMISSION_LABELS = {
  'bus.trips.read': 'Xem chuyến xe',
  'bus.trips.write': 'Quản lý chuyến xe',
  'train.trips.read': 'Xem chuyến tàu',
  'flight.inventory.read': 'Xem tồn chỗ máy bay',
  'flight.offers.read': 'Xem giá vé máy bay',
  'hotel.inventory.read': 'Xem tồn phòng khách sạn',
  'tour.inventory.read': 'Xem tồn chỗ tour',
  'tenants.manage': 'Quản lý đối tác',
  'ticket.scan': 'Quét vé',
  'cms.posts.read': 'Xem bài viết CMS',
  'cms.posts.write': 'Quản lý bài viết CMS',
  'cms.posts.publish': 'Xuất bản bài viết CMS',
  'cms.media.manage': 'Quản lý thư viện media CMS',
  'cms.taxonomy.manage': 'Quản lý danh mục và thẻ CMS',
  'cms.redirects.manage': 'Quản lý chuyển hướng CMS',
  'cms.site-settings.manage': 'Quản lý cấu hình website',
  'cms.seo.audit': 'Kiểm tra SEO CMS',
  'tenant.dashboard.read': 'Xem bảng điều khiển đối tác',
  'tenant.bookings.read': 'Xem đơn đặt của đối tác',
  'tenant.reviews.read': 'Xem đánh giá của đối tác',
  'tenant.staff.manage': 'Quản lý nhân sự đối tác',
  'tenant.finance.read': 'Xem tài chính đối tác',
  'tenant.reports.read': 'Xem báo cáo đối tác',
  'tenant.settings.read': 'Xem thiết lập đối tác',
  'phase012.permission.1775664619': 'Quyền kiểm thử Phase 012',
  'phase2.test.1775663130': 'Quyền kiểm thử Phase 2',
  'phase2.test.1775663302': 'Quyền kiểm thử Phase 2',
  'phase2.test.1775663414': 'Quyền kiểm thử Phase 2',
};

const PERMISSION_SUBJECT_LABELS = {
  bookings: 'đơn đặt',
  dashboard: 'bảng điều khiển',
  finance: 'tài chính',
  inventory: 'tồn chỗ',
  media: 'thư viện media',
  offers: 'giá vé',
  permission: 'quyền',
  posts: 'bài viết',
  redirects: 'chuyển hướng',
  reports: 'báo cáo',
  reviews: 'đánh giá',
  seo: 'SEO',
  settings: 'thiết lập',
  'site-settings': 'cấu hình website',
  staff: 'nhân sự',
  taxonomy: 'danh mục và thẻ',
  test: 'kiểm thử',
  trips: 'chuyến',
};

const PERMISSION_ACTION_LABELS = {
  audit: 'Kiểm tra',
  manage: 'Quản lý',
  publish: 'Xuất bản',
  read: 'Xem',
  scan: 'Quét',
  write: 'Quản lý',
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

export function getPermissionCategoryLabel(category) {
  if (!category) {
    return 'Chưa phân nhóm';
  }

  return PERMISSION_CATEGORY_LABELS[category] || category;
}

export function getPermissionCode(permissionOrCode) {
  return typeof permissionOrCode === 'string'
    ? permissionOrCode
    : permissionOrCode?.code || permissionOrCode?.Code || '';
}

export function getPermissionName(permission) {
  return permission?.name || permission?.Name || '';
}

export function getPermissionLabel(permissionOrCode) {
  const code = getPermissionCode(permissionOrCode);

  if (code && PERMISSION_LABELS[code]) {
    return PERMISSION_LABELS[code];
  }

  const generatedLabel = buildPermissionLabelFromCode(code);
  if (generatedLabel) {
    return generatedLabel;
  }

  return getPermissionName(permissionOrCode) || code || 'Quyền';
}

function buildPermissionLabelFromCode(code) {
  if (!code) {
    return '';
  }

  const parts = code.split('.').filter(Boolean);
  if (parts.length === 0) {
    return '';
  }

  if (parts[0]?.toLowerCase().startsWith('phase')) {
    return `Quyền kiểm thử ${parts[0].replace(/phase/i, 'Phase ')}`.replace(/\s+/g, ' ').trim();
  }

  const action = PERMISSION_ACTION_LABELS[parts[parts.length - 1]];
  const subject = parts.slice(1, -1).map((part) => PERMISSION_SUBJECT_LABELS[part] || part).join(' ');
  const category = getPermissionCategoryLabel(parts[0]).toLowerCase();

  if (action && subject) {
    return `${action} ${subject} ${category}`;
  }

  if (action) {
    return `${action} ${category}`;
  }

  return '';
}
