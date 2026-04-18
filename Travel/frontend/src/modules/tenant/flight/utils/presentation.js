import { getCustomerCurrency, getCustomerLocale } from '../../../../services/customerPreferences';

export const FLIGHT_STATUS_OPTIONS = [
  { value: 1, label: 'Nháp', name: 'Draft' },
  { value: 2, label: 'Đã mở bán', name: 'Published' },
  { value: 3, label: 'Tạm dừng', name: 'Suspended' },
  { value: 4, label: 'Đã hủy', name: 'Cancelled' },
];

export const OFFER_STATUS_OPTIONS = [
  { value: 1, label: 'Đang bán', name: 'Active' },
  { value: 2, label: 'Hết hiệu lực', name: 'Expired' },
  { value: 3, label: 'Đã hủy', name: 'Cancelled' },
];

export const CABIN_CLASS_OPTIONS = [
  { value: 1, label: 'Phổ thông', name: 'Economy' },
  { value: 2, label: 'Phổ thông đặc biệt', name: 'PremiumEconomy' },
  { value: 3, label: 'Thương gia', name: 'Business' },
  { value: 4, label: 'Hạng nhất', name: 'First' },
];

export const ANCILLARY_TYPE_OPTIONS = [
  { value: 1, label: 'Hành lý', name: 'Baggage' },
  { value: 2, label: 'Suất ăn', name: 'Meal' },
  { value: 3, label: 'Chỗ ngồi', name: 'Seat' },
  { value: 4, label: 'Bảo hiểm', name: 'Insurance' },
  { value: 5, label: 'Phòng chờ', name: 'Lounge' },
  { value: 6, label: 'Ưu tiên', name: 'Priority' },
  { value: 99, label: 'Khác', name: 'Other' },
];

export const TAX_FEE_LINE_TYPE_OPTIONS = [
  { value: 1, label: 'Giá cơ bản', name: 'BaseFare' },
  { value: 2, label: 'Thuế', name: 'Tax' },
  { value: 3, label: 'Phí', name: 'Fee' },
  { value: 4, label: 'Phụ thu', name: 'Surcharge' },
  { value: 5, label: 'Giảm trừ', name: 'Discount' },
  { value: 99, label: 'Khác', name: 'Other' },
];

function normalizeEnumName(value) {
  return String(value || '').trim().toLowerCase();
}

export function parseEnumOptionValue(options, value, fallbackValue) {
  if (typeof value === 'number' && Number.isFinite(value)) {
    return value;
  }

  const normalized = normalizeEnumName(value);
  const match = options.find((item) => normalizeEnumName(item.name) === normalized || String(item.value) === String(value));
  return match?.value ?? fallbackValue;
}

function getOption(options, value, fallbackValue) {
  const parsed = parseEnumOptionValue(options, value, fallbackValue);
  return options.find((item) => item.value === parsed) || options.find((item) => item.value === fallbackValue) || options[0];
}

export function getFlightStatusLabel(value) {
  return getOption(FLIGHT_STATUS_OPTIONS, value, 1)?.label || 'Nháp';
}

export function getOfferStatusLabel(value) {
  return getOption(OFFER_STATUS_OPTIONS, value, 1)?.label || 'Đang bán';
}

export function getCabinClassLabel(value) {
  return getOption(CABIN_CLASS_OPTIONS, value, 1)?.label || 'Phổ thông';
}

export function getAncillaryTypeLabel(value) {
  return getOption(ANCILLARY_TYPE_OPTIONS, value, 99)?.label || 'Khác';
}

export function getTaxFeeLineTypeLabel(value) {
  return getOption(TAX_FEE_LINE_TYPE_OPTIONS, value, 99)?.label || 'Khác';
}

export function getFlightStatusClass(value) {
  const label = getFlightStatusLabel(value);
  if (label === 'Đã mở bán') return 'bg-emerald-100 text-emerald-700';
  if (label === 'Tạm dừng') return 'bg-amber-100 text-amber-700';
  if (label === 'Đã hủy') return 'bg-rose-100 text-rose-700';
  return 'bg-slate-100 text-slate-600';
}

export function getOfferStatusClass(value) {
  const label = getOfferStatusLabel(value);
  if (label === 'Đang bán') return 'bg-emerald-100 text-emerald-700';
  if (label === 'Hết hiệu lực') return 'bg-amber-100 text-amber-700';
  if (label === 'Đã hủy') return 'bg-rose-100 text-rose-700';
  return 'bg-slate-100 text-slate-600';
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
  }).format(date);
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
    return '--';
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return '--';
  }

  return new Intl.DateTimeFormat(getCustomerLocale(), {
    hour: '2-digit',
    minute: '2-digit',
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

  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  const hours = String(date.getHours()).padStart(2, '0');
  const minutes = String(date.getMinutes()).padStart(2, '0');
  return `${year}-${month}-${day}T${hours}:${minutes}`;
}

export function toApiDateTimeValue(value) {
  if (!value) {
    return null;
  }

  return new Date(value).toISOString();
}

export function getAirportDisplayName(airport) {
  if (!airport) {
    return 'Chưa chọn sân bay';
  }

  return airport.iataCode || airport.code || airport.name || 'Chưa chọn sân bay';
}

export function getAirlineDisplayName(airline) {
  return airline?.name || airline?.code || 'Hãng bay chưa xác định';
}

export function getSeatStatusClass(status) {
  if (status === 'inactive') {
    return 'bg-slate-100 text-slate-500';
  }

  if (status === 'held' || status === 'booked') {
    return 'bg-rose-100 text-rose-700';
  }

  return 'bg-emerald-100 text-emerald-700';
}
