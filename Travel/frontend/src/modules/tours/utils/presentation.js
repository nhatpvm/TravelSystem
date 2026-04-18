import { getCustomerCurrency, getCustomerLocale } from '../../../services/customerPreferences';

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

  if (typeof value === 'string' && /^\d{4}-\d{2}-\d{2}$/.test(value)) {
    const [year, month, day] = value.split('-');
    return `${day}/${month}/${year}`;
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

  if (typeof value === 'string' && /^\d{2}:\d{2}/.test(value)) {
    return value.slice(0, 5);
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

export function parseJsonList(value) {
  if (!value) {
    return [];
  }

  if (Array.isArray(value)) {
    return value.filter(Boolean);
  }

  try {
    const parsed = JSON.parse(value);
    if (Array.isArray(parsed)) {
      return parsed.filter(Boolean);
    }
  } catch {
    return String(value)
      .split(',')
      .map((item) => item.trim())
      .filter(Boolean);
  }

  return [];
}

export function getTourStatusLabel(value) {
  switch (Number(value)) {
    case 1:
      return 'Đang bán';
    case 2:
      return 'Tạm ngưng';
    case 3:
      return 'Ẩn';
    case 4:
      return 'Lưu trữ';
    case 0:
    default:
      return 'Nháp';
  }
}

export function getTourStatusClass(value) {
  switch (Number(value)) {
    case 1:
      return 'bg-emerald-100 text-emerald-700';
    case 2:
      return 'bg-amber-100 text-amber-700';
    case 3:
    case 4:
      return 'bg-rose-100 text-rose-700';
    case 0:
    default:
      return 'bg-slate-100 text-slate-600';
  }
}

export function getBookingStatusLabel(value) {
  switch (Number(value)) {
    case 1:
      return 'Đã xác nhận';
    case 2:
      return 'Xác nhận một phần';
    case 3:
      return 'Đã hủy';
    case 4:
      return 'Thất bại';
    case 5:
      return 'Hủy một phần';
    case 0:
    default:
      return 'Chờ xử lý';
  }
}

export function getBookingStatusClass(value) {
  switch (Number(value)) {
    case 1:
      return 'bg-emerald-100 text-emerald-700';
    case 2:
      return 'bg-sky-100 text-sky-700';
    case 3:
    case 5:
      return 'bg-rose-100 text-rose-700';
    case 4:
      return 'bg-amber-100 text-amber-700';
    case 0:
    default:
      return 'bg-slate-100 text-slate-600';
  }
}

export function getReviewStatusLabel(value) {
  switch (Number(value)) {
    case 1:
      return 'Đã duyệt';
    case 2:
      return 'Từ chối';
    case 3:
      return 'Ẩn';
    case 4:
      return 'Đã xóa';
    case 0:
    default:
      return 'Chờ duyệt';
  }
}

export function getReviewStatusClass(value) {
  switch (Number(value)) {
    case 1:
      return 'bg-emerald-100 text-emerald-700';
    case 2:
      return 'bg-rose-100 text-rose-700';
    case 3:
      return 'bg-amber-100 text-amber-700';
    case 4:
      return 'bg-slate-100 text-slate-600';
    case 0:
    default:
      return 'bg-sky-100 text-sky-700';
  }
}

export function getDifficultyLabel(value) {
  switch (Number(value)) {
    case 1:
      return 'Dễ';
    case 2:
      return 'Trung bình';
    case 3:
      return 'Thử thách';
    default:
      return 'Tự do';
  }
}

export function getTourTypeLabel(value) {
  switch (Number(value)) {
    case 1:
      return 'Trong nước';
    case 2:
      return 'Quốc tế';
    case 3:
      return 'Trong ngày';
    case 4:
      return 'Combo';
    case 5:
      return 'Charter';
    default:
      return 'Khác';
  }
}

export function getPriceTypeLabel(value) {
  switch (Number(value)) {
    case 1:
      return 'Người lớn';
    case 2:
      return 'Trẻ em';
    case 3:
      return 'Em bé';
    case 4:
      return 'Người cao tuổi';
    case 5:
      return 'Phụ thu phòng đơn';
    case 6:
      return 'Nhóm riêng';
    case 7:
      return 'Giường phụ';
    default:
      return 'Khác';
  }
}

export function getScheduleStatusLabel(value) {
  switch (Number(value)) {
    case 1:
      return 'Mở bán';
    case 2:
      return 'Đóng bán';
    case 3:
      return 'Hết chỗ';
    case 4:
      return 'Đã hủy';
    case 5:
      return 'Hoàn thành';
    case 0:
    default:
      return 'Nháp';
  }
}

export function getScheduleStatusClass(value) {
  switch (Number(value)) {
    case 1:
      return 'bg-emerald-100 text-emerald-700';
    case 2:
      return 'bg-slate-100 text-slate-600';
    case 3:
      return 'bg-amber-100 text-amber-700';
    case 4:
      return 'bg-rose-100 text-rose-700';
    case 5:
      return 'bg-sky-100 text-sky-700';
    case 0:
    default:
      return 'bg-slate-100 text-slate-600';
  }
}

export function getCapacityStatusLabel(value) {
  switch (Number(value)) {
    case 1:
      return 'Mở bán';
    case 2:
      return 'Sắp đầy';
    case 3:
      return 'Đầy';
    case 4:
      return 'Đóng';
    case 5:
      return 'Đã hủy';
    case 0:
    default:
      return 'Nháp';
  }
}

export function getCapacityStatusClass(value) {
  switch (Number(value)) {
    case 1:
      return 'bg-emerald-100 text-emerald-700';
    case 2:
      return 'bg-amber-100 text-amber-700';
    case 3:
      return 'bg-rose-100 text-rose-700';
    case 4:
      return 'bg-slate-100 text-slate-600';
    case 5:
      return 'bg-rose-100 text-rose-700';
    case 0:
    default:
      return 'bg-slate-100 text-slate-600';
  }
}

export function getPackageStatusLabel(value) {
  switch (Number(value)) {
    case 1:
      return 'Đang bán';
    case 2:
      return 'Tạm ngưng';
    case 3:
      return 'Lưu trữ';
    case 0:
    default:
      return 'Nháp';
  }
}

export function getPackageModeLabel(value) {
  switch (Number(value)) {
    case 1:
      return 'Cố định';
    case 2:
      return 'Tùy chọn';
    case 3:
      return 'Động';
    default:
      return 'Khác';
  }
}

export function getHoldStrategyLabel(value) {
  switch (Number(value)) {
    case 0:
      return 'Không giữ chỗ';
    case 1:
      return 'Giữ chỗ tốt nhất';
    case 2:
    default:
      return 'Giữ toàn bộ hoặc hủy';
  }
}

export function formatDuration(days, nights) {
  if (!days && !nights) {
    return '--';
  }

  if (days && nights >= 0) {
    return `${days}N${nights}Đ`;
  }

  return `${days || 0} ngày`;
}

export function buildSlug(value) {
  return String(value || '')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .replace(/đ/g, 'd')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
}
