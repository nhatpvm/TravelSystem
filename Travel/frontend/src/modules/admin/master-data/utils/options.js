export const LOCATION_TYPE_OPTIONS = [
  { value: 1, label: 'Thành phố' },
  { value: 2, label: 'Bến xe' },
  { value: 3, label: 'Ga tàu' },
  { value: 4, label: 'Sân bay' },
  { value: 5, label: 'Khách sạn' },
  { value: 6, label: 'Điểm tham quan' },
  { value: 7, label: 'Điểm đón' },
  { value: 8, label: 'Điểm trả' },
  { value: 99, label: 'Khác' },
];

export const PROVIDER_TYPE_OPTIONS = [
  { value: 1, label: 'Nhà xe' },
  { value: 2, label: 'Tàu hỏa' },
  { value: 3, label: 'Hãng bay' },
  { value: 4, label: 'Tour' },
  { value: 5, label: 'Khách sạn' },
];

export const VEHICLE_TYPE_OPTIONS = [
  { value: 1, label: 'Xe khách' },
  { value: 2, label: 'Tàu hỏa' },
  { value: 3, label: 'Máy bay' },
  { value: 4, label: 'Xe tour' },
];

export const SEAT_TYPE_OPTIONS = [
  { value: 1, label: 'Tiêu chuẩn' },
  { value: 2, label: 'VIP' },
  { value: 3, label: 'Nằm dưới' },
  { value: 4, label: 'Nằm trên' },
  { value: 5, label: 'Thương gia' },
  { value: 6, label: 'Phổ thông' },
];

export const SEAT_CLASS_OPTIONS = [
  { value: 0, label: 'Tất cả' },
  { value: 1, label: 'Phổ thông' },
  { value: 2, label: 'Phổ thông đặc biệt' },
  { value: 3, label: 'Thương gia' },
  { value: 4, label: 'Hạng nhất' },
];

export const GEO_SYNC_DEPTH_OPTIONS = [
  { value: 1, label: 'Cấp 1 - Tỉnh/thành' },
  { value: 2, label: 'Cấp 2 - Quận/huyện' },
  { value: 3, label: 'Cấp 3 - Phường/xã' },
];

function resolveOptionLabel(options, value, fallback = 'Chưa xác định') {
  const matched = options.find((item) => Number(item.value) === Number(value));
  return matched?.label || fallback;
}

export function getLocationTypeLabel(value) {
  return resolveOptionLabel(LOCATION_TYPE_OPTIONS, value);
}

export function getProviderTypeLabel(value) {
  return resolveOptionLabel(PROVIDER_TYPE_OPTIONS, value);
}

export function getVehicleTypeLabel(value) {
  return resolveOptionLabel(VEHICLE_TYPE_OPTIONS, value);
}

export function getSeatTypeLabel(value) {
  return resolveOptionLabel(SEAT_TYPE_OPTIONS, value);
}

export function getSeatClassLabel(value) {
  return resolveOptionLabel(SEAT_CLASS_OPTIONS, value);
}

export function formatDate(value) {
  if (!value) {
    return 'Chưa có dữ liệu';
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return 'Chưa có dữ liệu';
  }

  return new Intl.DateTimeFormat('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(date);
}

export function formatDateTime(value) {
  if (!value) {
    return 'Chưa có dữ liệu';
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return 'Chưa có dữ liệu';
  }

  return new Intl.DateTimeFormat('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(date);
}

export function buildVehicleModelLabel(item) {
  if (!item) {
    return 'Chưa gán mẫu';
  }

  const year = item.modelYear ? ` ${item.modelYear}` : '';
  return `${item.manufacturer || ''} ${item.modelName || ''}${year}`.trim();
}

export function getActiveBadgeClass(item) {
  if (item?.isDeleted) {
    return 'bg-slate-100 text-slate-500';
  }

  return item?.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-amber-100 text-amber-700';
}

export function getActiveBadgeLabel(item) {
  if (item?.isDeleted) {
    return 'Đã xóa';
  }

  return item?.isActive ? 'Hoạt động' : 'Tạm dừng';
}

export function getGeoLogStatusClass(item) {
  return item?.isSuccess ? 'bg-emerald-100 text-emerald-700' : 'bg-rose-100 text-rose-600';
}

export function getGeoLogStatusLabel(item) {
  return item?.isSuccess ? 'Thành công' : 'Thất bại';
}
