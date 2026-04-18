import { getCustomerCurrency, getCustomerLocale } from '../../../../services/customerPreferences';

export const TRAIN_STOP_POINT_TYPES = [
  { value: 1, label: 'Ga tàu' },
  { value: 99, label: 'Khác' },
];

export const TRAIN_TRIP_STATUSES = [
  { value: 1, label: 'Nháp' },
  { value: 2, label: 'Đã mở bán' },
  { value: 3, label: 'Tạm dừng' },
  { value: 4, label: 'Đã hủy' },
];

export const TRAIN_CAR_TYPES = [
  { value: 1, label: 'Toa ghế' },
  { value: 2, label: 'Toa giường' },
  { value: 3, label: 'Toa thương gia' },
  { value: 99, label: 'Khác' },
];

export const TRAIN_SEAT_TYPES = [
  { value: 1, label: 'Ghế ngồi' },
  { value: 2, label: 'Giường trên' },
  { value: 3, label: 'Giường dưới' },
];

export function formatCurrency(value, currency = 'VND') {
  const amount = Number(value || 0);
  const locale = getCustomerLocale();
  const resolvedCurrency = currency || getCustomerCurrency(currency);

  try {
    return new Intl.NumberFormat(locale, {
      style: 'currency',
      currency: resolvedCurrency,
      maximumFractionDigits: 0,
    }).format(amount);
  } catch {
    return `${amount.toLocaleString('vi-VN')} đ`;
  }
}

export function formatDate(value) {
  if (!value) {
    return '--';
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return '--';
  }

  return new Intl.DateTimeFormat(getCustomerLocale(), {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(date);
}

export function formatTime(value) {
  if (!value) {
    return '--:--';
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return '--:--';
  }

  return new Intl.DateTimeFormat(getCustomerLocale(), {
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  }).format(date);
}

export function formatDateTime(value) {
  if (!value) {
    return '--';
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return '--';
  }

  return new Intl.DateTimeFormat(getCustomerLocale(), {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  }).format(date);
}

export function toDateTimeInputValue(value) {
  if (!value) {
    return '';
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return '';
  }

  const offset = date.getTimezoneOffset();
  const local = new Date(date.getTime() - offset * 60 * 1000);
  return local.toISOString().slice(0, 16);
}

export function toApiDateTimeValue(value) {
  if (!value) {
    return null;
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return null;
  }

  return date.toISOString();
}

export function formatDurationMinutes(value) {
  const minutes = Number(value || 0);
  if (!minutes) {
    return '--';
  }

  const hours = Math.floor(minutes / 60);
  const remain = minutes % 60;

  if (hours <= 0) {
    return `${remain} phút`;
  }

  if (!remain) {
    return `${hours} giờ`;
  }

  return `${hours} giờ ${remain} phút`;
}

export function getTripStatusLabel(status) {
  return TRAIN_TRIP_STATUSES.find((item) => item.value === Number(status))?.label || 'Không rõ';
}

export function getTripStatusClass(status) {
  switch (Number(status)) {
    case 2:
      return 'bg-emerald-100 text-emerald-700';
    case 3:
      return 'bg-amber-100 text-amber-700';
    case 4:
      return 'bg-rose-100 text-rose-700';
    case 1:
    default:
      return 'bg-slate-100 text-slate-600';
  }
}

export function getStopPointTypeLabel(type) {
  return TRAIN_STOP_POINT_TYPES.find((item) => item.value === Number(type))?.label || 'Khác';
}

export function getCarTypeLabel(type) {
  return TRAIN_CAR_TYPES.find((item) => item.value === Number(type))?.label || 'Khác';
}

export function getSeatTypeLabel(type) {
  return TRAIN_SEAT_TYPES.find((item) => item.value === Number(type))?.label || 'Khác';
}

export function getSeatStatusLabel(status) {
  switch (status) {
    case 'held_by_me':
      return 'Giữ bởi tôi';
    case 'held':
      return 'Đang giữ';
    case 'booked':
      return 'Đã bán';
    case 'inactive':
      return 'Ngưng bán';
    case 'available':
    default:
      return 'Còn trống';
  }
}

export function getSeatStatusClass(status) {
  switch (status) {
    case 'held_by_me':
      return 'bg-blue-100 text-blue-700';
    case 'held':
      return 'bg-amber-100 text-amber-700';
    case 'booked':
      return 'bg-rose-100 text-rose-700';
    case 'inactive':
      return 'bg-slate-100 text-slate-500';
    case 'available':
    default:
      return 'bg-emerald-100 text-emerald-700';
  }
}

export function buildTodayDateValue() {
  return new Date().toISOString().slice(0, 10);
}
