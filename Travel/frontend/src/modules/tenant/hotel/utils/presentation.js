import { getCustomerCurrency, getCustomerLocale } from '../../../../services/customerPreferences';

export const HOTEL_STATUS_OPTIONS = [
  { value: 1, label: 'Nháp', name: 'Draft' },
  { value: 2, label: 'Đang bán', name: 'Active' },
  { value: 3, label: 'Tạm ngưng', name: 'Inactive' },
  { value: 4, label: 'Treo bán', name: 'Suspended' },
];

export const ROOM_TYPE_STATUS_OPTIONS = [
  { value: 1, label: 'Nháp', name: 'Draft' },
  { value: 2, label: 'Đang bán', name: 'Active' },
  { value: 3, label: 'Tạm ngưng', name: 'Inactive' },
];

export const RATE_PLAN_STATUS_OPTIONS = [
  { value: 1, label: 'Nháp', name: 'Draft' },
  { value: 2, label: 'Đang bán', name: 'Active' },
  { value: 3, label: 'Tạm ngưng', name: 'Inactive' },
];

export const RATE_PLAN_TYPE_OPTIONS = [
  { value: 1, label: 'Công khai', name: 'Public' },
  { value: 2, label: 'Doanh nghiệp', name: 'Corporate' },
  { value: 3, label: 'Gói combo', name: 'Package' },
  { value: 4, label: 'Khuyến mãi', name: 'Promo' },
];

export const CANCELLATION_POLICY_TYPE_OPTIONS = [
  { value: 1, label: 'Miễn phí hủy', name: 'FreeCancellation' },
  { value: 2, label: 'Không hoàn hủy', name: 'NonRefundable' },
  { value: 3, label: 'Tùy chỉnh', name: 'Custom' },
];

export const PENALTY_CHARGE_TYPE_OPTIONS = [
  { value: 1, label: '% tiền đêm', name: 'PercentOfNight' },
  { value: 2, label: '% tổng hóa đơn', name: 'PercentOfTotal' },
  { value: 3, label: 'Số tiền cố định', name: 'FixedAmount' },
  { value: 4, label: 'Số đêm', name: 'NightCount' },
];

export const EXTRA_SERVICE_TYPE_OPTIONS = [
  { value: 1, label: 'Giường phụ', name: 'ExtraBed' },
  { value: 2, label: 'Bữa sáng', name: 'Breakfast' },
  { value: 3, label: 'Đưa đón sân bay', name: 'AirportPickup' },
  { value: 4, label: 'Trả phòng muộn', name: 'LateCheckout' },
  { value: 5, label: 'Nhận phòng sớm', name: 'EarlyCheckin' },
  { value: 99, label: 'Khác', name: 'Other' },
];

function normalizeEnumName(value) {
  return String(value || '').trim().toLowerCase();
}

function getOption(options, value, fallbackValue) {
  if (typeof value === 'number' && Number.isFinite(value)) {
    return options.find((item) => item.value === value)
      || options.find((item) => item.value === fallbackValue)
      || options[0];
  }

  const normalized = normalizeEnumName(value);
  return options.find((item) => normalizeEnumName(item.name) === normalized || String(item.value) === String(value))
    || options.find((item) => item.value === fallbackValue)
    || options[0];
}

export function parseEnumOptionValue(options, value, fallbackValue) {
  return getOption(options, value, fallbackValue)?.value ?? fallbackValue;
}

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
  } catch (error) {
    return `${amount.toLocaleString('vi-VN')} đ`;
  }
}

export function formatDate(value) {
  if (!value) return '--';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '--';
  return new Intl.DateTimeFormat(getCustomerLocale(), {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(date);
}

export function formatDateOnly(value) {
  if (!value) return '--';
  const date = new Date(`${value}T00:00:00`);
  if (Number.isNaN(date.getTime())) return '--';
  return new Intl.DateTimeFormat(getCustomerLocale(), {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(date);
}

export function formatTimeOnly(value) {
  return value ? String(value).slice(0, 5) : '--';
}

export function getHotelStatusLabel(value) { return getOption(HOTEL_STATUS_OPTIONS, value, 1)?.label || 'Nháp'; }
export function getRoomTypeStatusLabel(value) { return getOption(ROOM_TYPE_STATUS_OPTIONS, value, 1)?.label || 'Nháp'; }
export function getRatePlanStatusLabel(value) { return getOption(RATE_PLAN_STATUS_OPTIONS, value, 1)?.label || 'Nháp'; }
export function getRatePlanTypeLabel(value) { return getOption(RATE_PLAN_TYPE_OPTIONS, value, 1)?.label || 'Công khai'; }
export function getCancellationPolicyTypeLabel(value) { return getOption(CANCELLATION_POLICY_TYPE_OPTIONS, value, 1)?.label || 'Miễn phí hủy'; }
export function getPenaltyChargeTypeLabel(value) { return getOption(PENALTY_CHARGE_TYPE_OPTIONS, value, 1)?.label || '% tiền đêm'; }
export function getExtraServiceTypeLabel(value) { return getOption(EXTRA_SERVICE_TYPE_OPTIONS, value, 99)?.label || 'Khác'; }

export function getStatusClass(value) {
  const label = getHotelStatusLabel(value);
  if (label === 'Đang bán') return 'bg-emerald-100 text-emerald-700';
  if (label === 'Tạm ngưng') return 'bg-amber-100 text-amber-700';
  if (label === 'Treo bán') return 'bg-rose-100 text-rose-700';
  return 'bg-slate-100 text-slate-600';
}

export function readJsonInput(value, fallback = []) {
  if (!value || !String(value).trim()) return fallback;
  try {
    return JSON.parse(value);
  } catch (error) {
    throw new Error('JSON không hợp lệ.');
  }
}

export function toPrettyJson(value) {
  if (!value) return '';
  if (typeof value === 'string') {
    try {
      return JSON.stringify(JSON.parse(value), null, 2);
    } catch (error) {
      return value;
    }
  }
  return JSON.stringify(value, null, 2);
}
