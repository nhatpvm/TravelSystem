import React, { useEffect, useMemo, useState } from 'react';
import { motion } from 'framer-motion';
import { CheckCircle2, ChevronDown, ChevronUp, Clock, Compass, Loader2, RefreshCw, Search, Ticket, XCircle } from 'lucide-react';
import {
  cancelManagerTourBookingRemaining,
  confirmManagerTourBookingReschedule,
  getManagerTourBooking,
  getManagerTourBookingItinerary,
  getManagerTourBookingRefunds,
  getManagerTourBookingTimeline,
  getManagerTourBookingVoucher,
  holdManagerTourBookingReschedule,
  listManagerTourBookings,
  listManagerTourBookingReschedules,
  listManagerTours,
  markManagerTourRefundReady,
  rejectManagerTourRefund,
  releaseManagerTourBookingReschedule,
} from '../../../services/tourService';
import {
  formatCurrency,
  formatDateTime,
  getBookingStatusClass,
  getBookingStatusLabel,
} from '../../tours/utils/presentation';

const STATUS_OPTIONS = [
  { value: 'all', label: 'Tất cả' },
  { value: '0', label: 'Chờ xử lý' },
  { value: '1', label: 'Đã xác nhận' },
  { value: '2', label: 'Xác nhận một phần' },
  { value: '3', label: 'Đã hủy' },
  { value: '4', label: 'Thất bại' },
  { value: '5', label: 'Hủy một phần' },
];

function getStatusIcon(status) {
  switch (Number(status)) {
    case 1:
      return <CheckCircle2 size={12} />;
    case 3:
    case 5:
      return <XCircle size={12} />;
    default:
      return <Clock size={12} />;
  }
}

function buildBulkCancellationDraft() {
  return JSON.stringify({
    reasonCode: 'MANUAL',
    reasonText: 'Điều chỉnh từ tenant portal',
    overrideNote: '',
  }, null, 2);
}

function buildRescheduleDraft(booking) {
  return JSON.stringify({
    targetScheduleId: '',
    targetPackageId: booking.packageId || null,
    totalPax: booking.requestedPax || 1,
    includeDefaultPackageOptions: true,
    copyExistingSelections: true,
    selectedPackageOptions: [],
    reasonCode: 'MANUAL',
    reasonText: 'Đổi lịch theo yêu cầu khách',
    overrideNote: '',
    notes: '',
  }, null, 2);
}

export default function TenantBookingsPage() {
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [tourFilter, setTourFilter] = useState('all');
  const [tours, setTours] = useState([]);
  const [bookings, setBookings] = useState([]);
  const [expandedId, setExpandedId] = useState('');
  const [details, setDetails] = useState({});
  const [detailLoadingId, setDetailLoadingId] = useState('');
  const [bulkCancelDrafts, setBulkCancelDrafts] = useState({});
  const [refundNoteDrafts, setRefundNoteDrafts] = useState({});
  const [rescheduleDrafts, setRescheduleDrafts] = useState({});
  const [actionLoadingId, setActionLoadingId] = useState('');

  useEffect(() => {
    loadData();
  }, []);

  const filteredBookings = useMemo(() => {
    const keyword = search.trim().toLowerCase();

    return bookings.filter((booking) => {
      const matchesStatus = statusFilter === 'all' || String(booking.status) === statusFilter;
      const matchesTour = tourFilter === 'all' || booking.tourId === tourFilter;
      const matchesSearch = !keyword || [
        booking.code,
        booking.packageName,
        booking.scheduleCode,
        booking.scheduleName,
        booking.tourName,
        booking.reservationCode,
      ].filter(Boolean).some((value) => String(value).toLowerCase().includes(keyword));

      return matchesStatus && matchesTour && matchesSearch;
    });
  }, [bookings, search, statusFilter, tourFilter]);

  const stats = useMemo(() => ({
    total: bookings.length,
    pending: bookings.filter((item) => Number(item.status) === 0).length,
    confirmed: bookings.filter((item) => Number(item.status) === 1).length,
    cancelled: bookings.filter((item) => [3, 5].includes(Number(item.status))).length,
  }), [bookings]);

  async function loadData() {
    setLoading(true);
    setError('');

    try {
      const tourResponse = await listManagerTours({ page: 1, pageSize: 100, includeDeleted: true });
      const tourItems = tourResponse.items || [];
      setTours(tourItems);

      const bookingResponses = await Promise.all(
        tourItems.map(async (tour) => {
          const response = await listManagerTourBookings(tour.id, { page: 1, pageSize: 50, includeDeleted: true });
          return (response.items || []).map((item) => ({
            ...item,
            tourId: tour.id,
            tourName: tour.name,
            tourCode: tour.code,
          }));
        }),
      );

      setBookings(
        bookingResponses
          .flat()
          .sort((left, right) => new Date(right.lastActivityAt || right.createdAt) - new Date(left.lastActivityAt || left.createdAt)),
      );
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải danh sách đơn đặt tour.');
    } finally {
      setLoading(false);
    }
  }

  async function handleRefresh() {
    setRefreshing(true);
    await loadData();
    setRefreshing(false);
  }

  async function toggleExpanded(booking) {
    if (expandedId === booking.id) {
      setExpandedId('');
      return;
    }

    setExpandedId(booking.id);

    if (details[booking.id]) {
      return;
    }

    setDetailLoadingId(booking.id);

    try {
      const [detail, timeline, itinerary, voucher, refunds, reschedules] = await Promise.all([
        getManagerTourBooking(booking.tourId, booking.id),
        getManagerTourBookingTimeline(booking.tourId, booking.id),
        getManagerTourBookingItinerary(booking.tourId, booking.id),
        getManagerTourBookingVoucher(booking.tourId, booking.id),
        getManagerTourBookingRefunds(booking.tourId, booking.id),
        listManagerTourBookingReschedules(booking.tourId, booking.id),
      ]);

      setDetails((current) => ({
        ...current,
        [booking.id]: { detail, timeline, itinerary, voucher, refunds, reschedules },
      }));
      setBulkCancelDrafts((current) => ({ ...current, [booking.id]: current[booking.id] || buildBulkCancellationDraft() }));
      setRescheduleDrafts((current) => ({ ...current, [booking.id]: current[booking.id] || buildRescheduleDraft(booking) }));
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải chi tiết booking.');
    } finally {
      setDetailLoadingId('');
    }
  }

  async function reloadBookingExtras(booking) {
    const [detail, timeline, itinerary, voucher, refunds, reschedules] = await Promise.all([
      getManagerTourBooking(booking.tourId, booking.id),
      getManagerTourBookingTimeline(booking.tourId, booking.id),
      getManagerTourBookingItinerary(booking.tourId, booking.id),
      getManagerTourBookingVoucher(booking.tourId, booking.id),
      getManagerTourBookingRefunds(booking.tourId, booking.id),
      listManagerTourBookingReschedules(booking.tourId, booking.id),
    ]);

    setDetails((current) => ({
      ...current,
      [booking.id]: { detail, timeline, itinerary, voucher, refunds, reschedules },
    }));
  }

  async function handleBulkCancel(booking) {
    setActionLoadingId(booking.id);
    setError('');
    setNotice('');

    try {
      const payload = JSON.parse(bulkCancelDrafts[booking.id] || buildBulkCancellationDraft());
      await cancelManagerTourBookingRemaining(booking.tourId, booking.id, payload);
      await reloadBookingExtras(booking);
      await loadData();
      setNotice('Đã tạo yêu cầu hủy phần còn lại của booking.');
    } catch (requestError) {
      setError(requestError.message || 'Không thể tạo yêu cầu hủy booking.');
    } finally {
      setActionLoadingId('');
    }
  }

  async function handleRefundAction(booking, refundId, action) {
    setActionLoadingId(refundId);
    setError('');
    setNotice('');

    try {
      const payload = { note: (refundNoteDrafts[refundId] || '').trim() || null };
      if (action === 'ready') {
        await markManagerTourRefundReady(booking.tourId, booking.id, refundId, payload);
      } else {
        await rejectManagerTourRefund(booking.tourId, booking.id, refundId, payload);
      }

      await reloadBookingExtras(booking);
      setNotice('Đã cập nhật trạng thái hoàn tiền.');
    } catch (requestError) {
      setError(requestError.message || 'Không thể cập nhật trạng thái hoàn tiền.');
    } finally {
      setActionLoadingId('');
    }
  }

  async function handleRescheduleHold(booking) {
    setActionLoadingId(booking.id);
    setError('');
    setNotice('');

    try {
      const payload = JSON.parse(rescheduleDrafts[booking.id] || buildRescheduleDraft(booking));
      await holdManagerTourBookingReschedule(booking.tourId, booking.id, payload);
      await reloadBookingExtras(booking);
      setNotice('Đã giữ chỗ đổi lịch cho booking.');
    } catch (requestError) {
      setError(requestError.message || 'Không thể giữ chỗ đổi lịch.');
    } finally {
      setActionLoadingId('');
    }
  }

  async function handleRescheduleAction(booking, rescheduleId, action) {
    setActionLoadingId(rescheduleId);
    setError('');
    setNotice('');

    try {
      if (action === 'confirm') {
        await confirmManagerTourBookingReschedule(booking.tourId, booking.id, rescheduleId, {});
      } else {
        await releaseManagerTourBookingReschedule(booking.tourId, booking.id, rescheduleId);
      }

      await reloadBookingExtras(booking);
      await loadData();
      setNotice('Đã cập nhật trạng thái đổi lịch.');
    } catch (requestError) {
      setError(requestError.message || 'Không thể cập nhật trạng thái đổi lịch.');
    } finally {
      setActionLoadingId('');
    }
  }

  return (
    <div className="space-y-8">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-black text-slate-900">Đơn đặt tour</h1>
          <p className="text-slate-500 text-sm mt-1">Theo dõi booking theo từng tour, gói tour và lịch khởi hành của tenant hiện tại.</p>
        </div>
        <button onClick={handleRefresh} disabled={refreshing} className="flex items-center gap-2 px-6 py-3 bg-slate-900 text-white rounded-2xl font-bold text-sm shadow-lg disabled:opacity-60">
          {refreshing ? <Loader2 size={16} className="animate-spin" /> : <RefreshCw size={16} />}
          Tải lại dữ liệu
        </button>
      </div>

      {error && (
        <div className="rounded-2xl border border-rose-100 bg-rose-50 px-5 py-4 text-sm font-bold text-rose-600">
          {error}
        </div>
      )}

      {notice && (
        <div className="rounded-2xl border border-emerald-100 bg-emerald-50 px-5 py-4 text-sm font-bold text-emerald-700">
          {notice}
        </div>
      )}

      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        {[
          { label: 'Tổng booking', value: stats.total, color: 'bg-slate-900 text-white', sub: 'Tất cả đơn tour' },
          { label: 'Chờ xử lý', value: stats.pending, color: 'bg-amber-50', sub: 'Cần kiểm tra' },
          { label: 'Đã xác nhận', value: stats.confirmed, color: 'bg-emerald-50', sub: 'Đã giữ chỗ thành công' },
          { label: 'Đã hủy', value: stats.cancelled, color: 'bg-rose-50', sub: 'Bao gồm hủy một phần' },
        ].map((item) => (
          <motion.div key={item.label} initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} className={`rounded-2xl p-5 shadow-sm border border-slate-100 ${item.color}`}>
            <p className={`text-3xl font-black ${item.color.includes('slate-900') ? 'text-white' : 'text-slate-900'}`}>{item.value}</p>
            <p className={`text-[10px] font-bold uppercase tracking-widest mt-1 ${item.color.includes('slate-900') ? 'text-white/60' : 'text-slate-400'}`}>{item.label}</p>
            <p className={`text-xs font-bold mt-1 ${item.color.includes('slate-900') ? 'text-white/80' : 'text-slate-600'}`}>{item.sub}</p>
          </motion.div>
        ))}
      </div>

      <div className="bg-white rounded-2xl p-4 shadow-sm border border-slate-100 flex flex-wrap gap-3">
        <div className="flex-1 min-w-48 flex items-center gap-2 bg-slate-50 rounded-xl px-4">
          <Search size={15} className="text-slate-400" />
          <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Mã booking, gói tour, lịch khởi hành..." className="bg-transparent py-3 flex-1 text-sm font-medium outline-none" />
        </div>
        <select value={tourFilter} onChange={(event) => setTourFilter(event.target.value)} className="rounded-xl bg-slate-50 px-4 py-3 text-sm font-bold text-slate-700 outline-none border border-slate-100">
          <option value="all">Tất cả tour</option>
          {tours.map((tour) => (
            <option key={tour.id} value={tour.id}>{tour.name}</option>
          ))}
        </select>
        <div className="flex bg-slate-50 p-1 rounded-xl border border-slate-100 gap-1 overflow-x-auto">
          {STATUS_OPTIONS.map((item) => (
            <button
              key={item.value}
              onClick={() => setStatusFilter(item.value)}
              className={`px-3 py-2 rounded-lg text-[10px] font-black uppercase tracking-widest transition-all whitespace-nowrap ${statusFilter === item.value ? 'bg-white shadow-md text-blue-600' : 'text-slate-400 hover:text-slate-700'}`}
            >
              {item.label}
            </button>
          ))}
        </div>
      </div>

      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
        <div className="hidden md:grid grid-cols-12 gap-4 px-5 py-3 border-b border-slate-50 text-[10px] font-black text-slate-400 uppercase tracking-widest bg-slate-50">
          <div className="col-span-4">Booking</div>
          <div className="col-span-3">Tour</div>
          <div className="col-span-2">Tổng tiền</div>
          <div className="col-span-2">Trạng thái</div>
          <div className="col-span-1"></div>
        </div>

        <div className="divide-y divide-slate-50">
          {loading ? (
            <div className="px-5 py-10 text-sm font-bold text-slate-400 flex items-center gap-3">
              <Loader2 size={16} className="animate-spin" />
              Đang tải booking tour...
            </div>
          ) : filteredBookings.length === 0 ? (
            <div className="py-16 text-center text-slate-400 font-bold text-sm">Không tìm thấy booking tour phù hợp.</div>
          ) : filteredBookings.map((booking) => {
            const expanded = expandedId === booking.id;
            const detail = details[booking.id];

            return (
              <div key={booking.id}>
                <button onClick={() => toggleExpanded(booking)} className="w-full grid grid-cols-2 md:grid-cols-12 gap-4 px-5 py-4 hover:bg-slate-50 transition-all text-left items-center">
                  <div className="col-span-1 md:col-span-4">
                    <p className="font-black text-slate-900 text-sm">{booking.code}</p>
                    <p className="text-[10px] text-slate-400 font-bold mt-1">
                      {booking.packageCode} · {booking.packageName}
                    </p>
                    <p className="text-[10px] text-slate-400 font-bold mt-1">
                      Lịch: {booking.scheduleName || booking.scheduleCode}
                    </p>
                  </div>
                  <div className="col-span-1 md:col-span-3">
                    <div className="flex items-center gap-2">
                      <div className="w-8 h-8 rounded-xl bg-amber-50 text-amber-600 flex items-center justify-center">
                        <Compass size={15} />
                      </div>
                      <div>
                        <p className="text-sm font-bold text-slate-800">{booking.tourName}</p>
                        <p className="text-[10px] text-slate-400">{booking.scheduleCode} · {formatDateTime(booking.createdAt)}</p>
                      </div>
                    </div>
                  </div>
                  <div className="col-span-1 md:col-span-2">
                    <p className="font-black text-slate-900 text-sm">{formatCurrency(booking.packageSubtotalAmount, booking.currencyCode)}</p>
                    <p className="text-[10px] text-slate-400">{booking.requestedPax} khách</p>
                  </div>
                  <div className="col-span-1 md:col-span-2">
                    <span className={`inline-flex items-center gap-1 px-2.5 py-1 rounded-xl text-[10px] font-black uppercase ${getBookingStatusClass(booking.status)}`}>
                      {getStatusIcon(booking.status)} {getBookingStatusLabel(booking.status)}
                    </span>
                  </div>
                  <div className="col-span-2 md:col-span-1 flex items-center justify-end gap-2">
                    {expanded ? <ChevronUp size={16} className="text-slate-400" /> : <ChevronDown size={16} className="text-slate-400" />}
                  </div>
                </button>

                {expanded && (
                  <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} className="bg-slate-50 border-t border-slate-100 px-5 py-4">
                    {detailLoadingId === booking.id ? (
                      <div className="text-sm font-bold text-slate-400 flex items-center gap-3">
                        <Loader2 size={16} className="animate-spin" />
                        Đang tải chi tiết booking...
                      </div>
                    ) : !detail ? (
                      <div className="text-sm font-bold text-slate-400">Chưa tải được chi tiết booking.</div>
                    ) : (
                      <div className="space-y-4">
                        <div className="grid grid-cols-1 xl:grid-cols-[0.95fr,1.05fr] gap-4">
                        <div className="bg-white rounded-2xl border border-slate-100 p-5">
                          <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest">Chi tiết đặt chỗ</p>
                          <div className="grid grid-cols-2 gap-3 mt-4 text-sm font-medium text-slate-600">
                            <div>
                              <p className="text-slate-400 text-[10px] font-black uppercase tracking-widest">Mã reservation</p>
                              <p className="mt-1">{detail.booking.reservationCode || '--'}</p>
                            </div>
                            <div>
                              <p className="text-slate-400 text-[10px] font-black uppercase tracking-widest">Xác nhận lúc</p>
                              <p className="mt-1">{formatDateTime(detail.booking.confirmedAt)}</p>
                            </div>
                            <div>
                              <p className="text-slate-400 text-[10px] font-black uppercase tracking-widest">Số slot xác nhận</p>
                              <p className="mt-1">{detail.booking.confirmedCapacitySlots}</p>
                            </div>
                            <div>
                              <p className="text-slate-400 text-[10px] font-black uppercase tracking-widest">Hold strategy</p>
                              <p className="mt-1">{detail.booking.holdStrategy}</p>
                            </div>
                          </div>

                          <div className="mt-5">
                            <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest">Các dòng dịch vụ</p>
                            <div className="mt-3 space-y-3">
                              {(detail.booking.items || []).map((item) => (
                                <div key={item.id} className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3">
                                  <div className="flex items-center justify-between gap-3">
                                    <div>
                                      <p className="font-black text-slate-900 text-sm">{item.componentType} · {item.sourceType}</p>
                                      <p className="text-[10px] text-slate-400 font-bold mt-1">Số lượng: {item.quantity}</p>
                                    </div>
                                    <p className="font-black text-slate-900">{formatCurrency(item.lineAmount, item.currencyCode)}</p>
                                  </div>
                                </div>
                              ))}
                            </div>
                          </div>
                        </div>

                        <div className="bg-white rounded-2xl border border-slate-100 p-5">
                          <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest">Timeline hoạt động</p>
                          <div className="mt-4 space-y-3">
                            {(detail.timeline || []).length === 0 ? (
                              <p className="text-sm font-bold text-slate-400">Chưa có mốc hoạt động chi tiết.</p>
                            ) : detail.timeline.map((event) => (
                              <div key={`${event.eventType}-${event.occurredAt}`} className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3">
                                <div className="flex items-center justify-between gap-3">
                                  <div>
                                    <p className="font-black text-slate-900 text-sm">{event.title}</p>
                                    <p className="text-[10px] text-slate-400 font-bold mt-1">{event.description || event.eventType}</p>
                                  </div>
                                  <div className="text-right">
                                    <p className="text-xs font-black text-slate-700">{formatDateTime(event.occurredAt)}</p>
                                    {event.amount != null && (
                                      <p className="text-[10px] text-slate-400 font-bold mt-1">{formatCurrency(event.amount, booking.currencyCode)}</p>
                                    )}
                                  </div>
                                </div>
                              </div>
                            ))}
                          </div>
                        </div>
                        </div>

                        <div className="grid grid-cols-1 xl:grid-cols-3 gap-4">
                          <div className="bg-white rounded-2xl border border-slate-100 p-5">
                            <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest">Tài liệu khách</p>
                            <div className="mt-4 space-y-3 text-sm font-medium text-slate-600">
                              <div>
                                <p className="text-slate-400 text-[10px] font-black uppercase tracking-widest">Voucher</p>
                                <p className="mt-1">{detail.voucher?.voucherNumber || '--'}</p>
                              </div>
                              <div>
                                <p className="text-slate-400 text-[10px] font-black uppercase tracking-widest">Điểm đón</p>
                                <p className="mt-1">{detail.itinerary?.pickupSummary || detail.voucher?.summary?.pickupSummary || '--'}</p>
                              </div>
                              <div>
                                <p className="text-slate-400 text-[10px] font-black uppercase tracking-widest">Điểm trả</p>
                                <p className="mt-1">{detail.itinerary?.dropoffSummary || detail.voucher?.summary?.dropoffSummary || '--'}</p>
                              </div>
                              <div>
                                <p className="text-slate-400 text-[10px] font-black uppercase tracking-widest">Cảnh báo</p>
                                <p className="mt-1">{(detail.itinerary?.warnings || detail.voucher?.warnings || []).join(' · ') || 'Không có'}</p>
                              </div>
                            </div>
                          </div>

                          <div className="bg-white rounded-2xl border border-slate-100 p-5">
                            <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest">Hoàn tiền</p>
                            <div className="mt-4 space-y-3">
                              {(detail.refunds?.items || []).length === 0 ? (
                                <p className="text-sm font-bold text-slate-400">Chưa có refund nào.</p>
                              ) : detail.refunds.items.map((refund) => (
                                <div key={refund.id} className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3">
                                  <div className="flex items-center justify-between gap-3">
                                    <div>
                                      <p className="font-black text-slate-900 text-sm">{formatCurrency(refund.refundAmount, refund.currencyCode)}</p>
                                      <p className="text-[10px] text-slate-400 font-bold mt-1">{refund.status} · {formatDateTime(refund.updatedAt || refund.createdAt)}</p>
                                    </div>
                                  </div>
                                  <textarea rows={2} value={refundNoteDrafts[refund.id] || ''} onChange={(event) => setRefundNoteDrafts((current) => ({ ...current, [refund.id]: event.target.value }))} placeholder="Ghi chú cho thao tác hoàn tiền..." className="w-full mt-3 rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm font-medium outline-none resize-none" />
                                  <div className="flex flex-wrap gap-2 mt-3">
                                    <button type="button" disabled={actionLoadingId === refund.id} onClick={() => handleRefundAction(booking, refund.id, 'ready')} className="px-4 py-2 rounded-xl bg-emerald-50 text-emerald-700 text-[11px] font-black uppercase tracking-widest disabled:opacity-60">Sẵn sàng hoàn</button>
                                    <button type="button" disabled={actionLoadingId === refund.id} onClick={() => handleRefundAction(booking, refund.id, 'reject')} className="px-4 py-2 rounded-xl bg-rose-50 text-rose-700 text-[11px] font-black uppercase tracking-widest disabled:opacity-60">Từ chối</button>
                                  </div>
                                </div>
                              ))}
                            </div>
                          </div>

                          <div className="bg-white rounded-2xl border border-slate-100 p-5">
                            <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest">Đổi lịch</p>
                            <div className="mt-4 space-y-3">
                              {(detail.reschedules || []).length === 0 ? (
                                <p className="text-sm font-bold text-slate-400">Chưa có yêu cầu đổi lịch.</p>
                              ) : detail.reschedules.map((item) => (
                                <div key={item.id} className="rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3">
                                  <p className="font-black text-slate-900 text-sm">{item.code}</p>
                                  <p className="text-[10px] text-slate-400 font-bold mt-1">{item.status} · {item.sourceScheduleCode} → {item.targetScheduleCode}</p>
                                  <div className="flex flex-wrap gap-2 mt-3">
                                    <button type="button" disabled={actionLoadingId === item.id} onClick={() => handleRescheduleAction(booking, item.id, 'confirm')} className="px-4 py-2 rounded-xl bg-emerald-50 text-emerald-700 text-[11px] font-black uppercase tracking-widest disabled:opacity-60">Xác nhận</button>
                                    <button type="button" disabled={actionLoadingId === item.id} onClick={() => handleRescheduleAction(booking, item.id, 'release')} className="px-4 py-2 rounded-xl bg-slate-100 text-slate-700 text-[11px] font-black uppercase tracking-widest disabled:opacity-60">Giải phóng</button>
                                  </div>
                                </div>
                              ))}
                            </div>
                          </div>
                        </div>

                        <div className="grid grid-cols-1 xl:grid-cols-2 gap-4">
                          <div className="bg-white rounded-2xl border border-slate-100 p-5">
                            <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest">Hậu mãi nhanh</p>
                            <textarea rows={7} value={bulkCancelDrafts[booking.id] || buildBulkCancellationDraft()} onChange={(event) => setBulkCancelDrafts((current) => ({ ...current, [booking.id]: event.target.value }))} className="w-full mt-4 rounded-[1.5rem] border border-slate-200 bg-slate-50 px-4 py-4 text-sm font-medium outline-none resize-y font-mono" />
                            <button type="button" disabled={actionLoadingId === booking.id} onClick={() => handleBulkCancel(booking)} className="mt-3 px-4 py-2 rounded-xl bg-rose-50 text-rose-700 text-[11px] font-black uppercase tracking-widest disabled:opacity-60">Hủy phần còn lại</button>
                          </div>

                          <div className="bg-white rounded-2xl border border-slate-100 p-5">
                            <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest">Tạo yêu cầu đổi lịch</p>
                            <textarea rows={7} value={rescheduleDrafts[booking.id] || buildRescheduleDraft(booking)} onChange={(event) => setRescheduleDrafts((current) => ({ ...current, [booking.id]: event.target.value }))} className="w-full mt-4 rounded-[1.5rem] border border-slate-200 bg-slate-50 px-4 py-4 text-sm font-medium outline-none resize-y font-mono" />
                            <button type="button" disabled={actionLoadingId === booking.id} onClick={() => handleRescheduleHold(booking)} className="mt-3 px-4 py-2 rounded-xl bg-sky-50 text-sky-700 text-[11px] font-black uppercase tracking-widest disabled:opacity-60">Giữ chỗ đổi lịch</button>
                          </div>
                        </div>
                      </div>
                    )}
                  </motion.div>
                )}
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}
