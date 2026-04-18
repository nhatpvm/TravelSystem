export const CUSTOMER_PRODUCT = {
  BUS: 1,
  TRAIN: 2,
  FLIGHT: 3,
  HOTEL: 4,
  TOUR: 5,
};

export const CUSTOMER_ORDER_STATUS = {
  PENDING_PAYMENT: 1,
  PAID: 2,
  TICKET_ISSUED: 3,
  COMPLETED: 4,
  CANCELLED: 5,
  EXPIRED: 6,
  FAILED: 7,
  REFUND_REQUESTED: 8,
  REFUNDED_PARTIAL: 9,
  REFUNDED_FULL: 10,
};

export const CUSTOMER_PAYMENT_STATUS = {
  PENDING: 1,
  PAID: 2,
  CANCELLED: 3,
  EXPIRED: 4,
  FAILED: 5,
  REFUNDED_PARTIAL: 6,
  REFUNDED_FULL: 7,
};

export const CUSTOMER_TICKET_STATUS = {
  PENDING: 1,
  ISSUED: 2,
  CANCELLED: 3,
  REFUNDED: 4,
};

export const CUSTOMER_REFUND_STATUS = {
  NONE: 0,
  REQUESTED: 1,
  UNDER_REVIEW: 2,
  APPROVED: 3,
  REJECTED: 4,
  PROCESSING: 5,
  REFUNDED_PARTIAL: 6,
  REFUNDED_FULL: 7,
  CANCELLED: 8,
};

export const CUSTOMER_VAT_STATUS = {
  REQUESTED: 1,
  ISSUED: 2,
  REJECTED: 3,
};

export const CUSTOMER_PASSENGER_TYPE = {
  ADULT: 1,
  CHILD: 2,
  INFANT: 3,
};

function normalizeEnum(value) {
  return Number(value || 0);
}

export function formatCustomerProductLabel(value) {
  switch (normalizeEnum(value)) {
    case CUSTOMER_PRODUCT.BUS:
      return 'Xe khách';
    case CUSTOMER_PRODUCT.TRAIN:
      return 'Tàu hỏa';
    case CUSTOMER_PRODUCT.FLIGHT:
      return 'Máy bay';
    case CUSTOMER_PRODUCT.HOTEL:
      return 'Khách sạn';
    case CUSTOMER_PRODUCT.TOUR:
      return 'Tour';
    default:
      return 'Dịch vụ';
  }
}

export function formatCustomerOrderStatusLabel(value) {
  switch (normalizeEnum(value)) {
    case CUSTOMER_ORDER_STATUS.PENDING_PAYMENT:
      return 'Chờ thanh toán';
    case CUSTOMER_ORDER_STATUS.PAID:
      return 'Đã thanh toán';
    case CUSTOMER_ORDER_STATUS.TICKET_ISSUED:
      return 'Đã phát hành vé';
    case CUSTOMER_ORDER_STATUS.COMPLETED:
      return 'Hoàn tất';
    case CUSTOMER_ORDER_STATUS.CANCELLED:
      return 'Đã hủy';
    case CUSTOMER_ORDER_STATUS.EXPIRED:
      return 'Đã hết hạn';
    case CUSTOMER_ORDER_STATUS.FAILED:
      return 'Thất bại';
    case CUSTOMER_ORDER_STATUS.REFUND_REQUESTED:
      return 'Đang chờ hoàn tiền';
    case CUSTOMER_ORDER_STATUS.REFUNDED_PARTIAL:
      return 'Đã hoàn một phần';
    case CUSTOMER_ORDER_STATUS.REFUNDED_FULL:
      return 'Đã hoàn toàn phần';
    default:
      return 'Đang xử lý';
  }
}

export function formatCustomerPaymentStatusLabel(value) {
  switch (normalizeEnum(value)) {
    case CUSTOMER_PAYMENT_STATUS.PENDING:
      return 'Chờ thanh toán';
    case CUSTOMER_PAYMENT_STATUS.PAID:
      return 'Thanh toán thành công';
    case CUSTOMER_PAYMENT_STATUS.CANCELLED:
      return 'Đã hủy thanh toán';
    case CUSTOMER_PAYMENT_STATUS.EXPIRED:
      return 'Phiên thanh toán hết hạn';
    case CUSTOMER_PAYMENT_STATUS.FAILED:
      return 'Thanh toán thất bại';
    case CUSTOMER_PAYMENT_STATUS.REFUNDED_PARTIAL:
      return 'Đã hoàn một phần';
    case CUSTOMER_PAYMENT_STATUS.REFUNDED_FULL:
      return 'Đã hoàn toàn phần';
    default:
      return 'Đang xử lý';
  }
}

export function formatCustomerTicketStatusLabel(value) {
  switch (normalizeEnum(value)) {
    case CUSTOMER_TICKET_STATUS.PENDING:
      return 'Chờ phát hành';
    case CUSTOMER_TICKET_STATUS.ISSUED:
      return 'Đã phát hành';
    case CUSTOMER_TICKET_STATUS.CANCELLED:
      return 'Đã hủy';
    case CUSTOMER_TICKET_STATUS.REFUNDED:
      return 'Đã hoàn';
    default:
      return 'Đang xử lý';
  }
}

export function formatCustomerRefundStatusLabel(value) {
  switch (normalizeEnum(value)) {
    case CUSTOMER_REFUND_STATUS.NONE:
      return 'Chưa có';
    case CUSTOMER_REFUND_STATUS.REQUESTED:
      return 'Đã gửi yêu cầu';
    case CUSTOMER_REFUND_STATUS.UNDER_REVIEW:
      return 'Đang xét duyệt';
    case CUSTOMER_REFUND_STATUS.APPROVED:
      return 'Đã duyệt';
    case CUSTOMER_REFUND_STATUS.REJECTED:
      return 'Từ chối';
    case CUSTOMER_REFUND_STATUS.PROCESSING:
      return 'Đang xử lý hoàn';
    case CUSTOMER_REFUND_STATUS.REFUNDED_PARTIAL:
      return 'Đã hoàn một phần';
    case CUSTOMER_REFUND_STATUS.REFUNDED_FULL:
      return 'Đã hoàn toàn phần';
    case CUSTOMER_REFUND_STATUS.CANCELLED:
      return 'Đã hủy yêu cầu';
    default:
      return 'Đang xử lý';
  }
}

export function formatCustomerVatStatusLabel(value) {
  switch (normalizeEnum(value)) {
    case CUSTOMER_VAT_STATUS.REQUESTED:
      return 'Đang xử lý';
    case CUSTOMER_VAT_STATUS.ISSUED:
      return 'Đã xuất';
    case CUSTOMER_VAT_STATUS.REJECTED:
      return 'Từ chối';
    default:
      return 'Đang cập nhật';
  }
}

export function formatCustomerPassengerTypeLabel(value) {
  switch (normalizeEnum(value)) {
    case CUSTOMER_PASSENGER_TYPE.ADULT:
      return 'Người lớn';
    case CUSTOMER_PASSENGER_TYPE.CHILD:
      return 'Trẻ em';
    case CUSTOMER_PASSENGER_TYPE.INFANT:
      return 'Em bé';
    default:
      return 'Hành khách';
  }
}

export function toPassengerTypeInput(value) {
  switch (normalizeEnum(value)) {
    case CUSTOMER_PASSENGER_TYPE.CHILD:
      return 'child';
    case CUSTOMER_PASSENGER_TYPE.INFANT:
      return 'infant';
    default:
      return 'adult';
  }
}

export function getCustomerOrderStatusClass(value) {
  switch (normalizeEnum(value)) {
    case CUSTOMER_ORDER_STATUS.PAID:
    case CUSTOMER_ORDER_STATUS.TICKET_ISSUED:
    case CUSTOMER_ORDER_STATUS.COMPLETED:
      return 'bg-emerald-100 text-emerald-700';
    case CUSTOMER_ORDER_STATUS.PENDING_PAYMENT:
    case CUSTOMER_ORDER_STATUS.REFUND_REQUESTED:
      return 'bg-amber-100 text-amber-700';
    case CUSTOMER_ORDER_STATUS.REFUNDED_PARTIAL:
    case CUSTOMER_ORDER_STATUS.REFUNDED_FULL:
      return 'bg-sky-100 text-sky-700';
    case CUSTOMER_ORDER_STATUS.CANCELLED:
    case CUSTOMER_ORDER_STATUS.EXPIRED:
    case CUSTOMER_ORDER_STATUS.FAILED:
      return 'bg-rose-100 text-rose-700';
    default:
      return 'bg-slate-100 text-slate-600';
  }
}

export function getCustomerPaymentStatusClass(value) {
  switch (normalizeEnum(value)) {
    case CUSTOMER_PAYMENT_STATUS.PAID:
      return 'bg-emerald-100 text-emerald-700';
    case CUSTOMER_PAYMENT_STATUS.PENDING:
      return 'bg-amber-100 text-amber-700';
    case CUSTOMER_PAYMENT_STATUS.REFUNDED_PARTIAL:
    case CUSTOMER_PAYMENT_STATUS.REFUNDED_FULL:
      return 'bg-sky-100 text-sky-700';
    case CUSTOMER_PAYMENT_STATUS.CANCELLED:
    case CUSTOMER_PAYMENT_STATUS.EXPIRED:
    case CUSTOMER_PAYMENT_STATUS.FAILED:
      return 'bg-rose-100 text-rose-700';
    default:
      return 'bg-slate-100 text-slate-600';
  }
}

export function getCustomerVatStatusClass(value) {
  switch (normalizeEnum(value)) {
    case CUSTOMER_VAT_STATUS.ISSUED:
      return 'bg-emerald-100 text-emerald-700';
    case CUSTOMER_VAT_STATUS.REQUESTED:
      return 'bg-amber-100 text-amber-700';
    case CUSTOMER_VAT_STATUS.REJECTED:
      return 'bg-rose-100 text-rose-700';
    default:
      return 'bg-slate-100 text-slate-600';
  }
}

export function getCustomerProductPath(productType, targetId, targetSlug) {
  switch (normalizeEnum(productType)) {
    case CUSTOMER_PRODUCT.BUS:
      return targetId ? `/bus/trip/${targetId}` : '/bus/results';
    case CUSTOMER_PRODUCT.TRAIN:
      return '/train/results';
    case CUSTOMER_PRODUCT.FLIGHT:
      return '/flight/results';
    case CUSTOMER_PRODUCT.HOTEL:
      return targetId ? `/hotel/${targetId}` : '/hotel/results';
    case CUSTOMER_PRODUCT.TOUR:
      return targetId ? `/tour/${targetId}` : targetSlug ? `/blog/${targetSlug}` : '/tours';
    default:
      return '/';
  }
}

export function getOrderSnapshot(order) {
  return order?.snapshot || {};
}

export function getSnapshotTitle(order) {
  const snapshot = getOrderSnapshot(order);
  return snapshot?.title || order?.orderCode || 'Đơn hàng';
}

export function getSnapshotSubtitle(order) {
  const snapshot = getOrderSnapshot(order);
  return snapshot?.subtitle || snapshot?.providerName || '';
}

export function getSnapshotLines(order) {
  const snapshot = getOrderSnapshot(order);
  return Array.isArray(snapshot?.lines) ? snapshot.lines : [];
}

export function getCountdownText(expiresAt) {
  if (!expiresAt) {
    return '--:--';
  }

  const target = new Date(expiresAt).getTime();
  const diff = Math.max(0, target - Date.now());
  const minutes = Math.floor(diff / 60000);
  const seconds = Math.floor((diff % 60000) / 1000);
  return `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
}

export function isPaidOrder(order) {
  const paymentStatus = normalizeEnum(order?.paymentStatus);
  return paymentStatus === CUSTOMER_PAYMENT_STATUS.PAID
    || paymentStatus === CUSTOMER_PAYMENT_STATUS.REFUNDED_PARTIAL
    || paymentStatus === CUSTOMER_PAYMENT_STATUS.REFUNDED_FULL;
}

export function canRequestRefund(order) {
  if (!order || !isPaidOrder(order)) {
    return false;
  }

  const refundStatus = normalizeEnum(order.refundStatus);
  return refundStatus === CUSTOMER_REFUND_STATUS.NONE
    || refundStatus === CUSTOMER_REFUND_STATUS.REJECTED
    || refundStatus === CUSTOMER_REFUND_STATUS.CANCELLED;
}

export function canCancelPendingOrder(order) {
  return normalizeEnum(order?.paymentStatus) === CUSTOMER_PAYMENT_STATUS.PENDING
    && normalizeEnum(order?.status) === CUSTOMER_ORDER_STATUS.PENDING_PAYMENT;
}
