import React, { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { BedDouble, Building2, CalendarDays, ChevronRight, Layers3, RefreshCw, WalletCards } from 'lucide-react';
import HotelModeShell from '../../hotel/components/HotelModeShell';
import {
  getAdminHotelOptions,
  getHotelManagerOptions,
  listAdminHotels,
  listAdminRatePlans,
  listAdminRoomTypes,
  listManagedHotels,
  listManagedRatePlans,
  listManagedRoomTypes,
} from '../../../../services/hotelService';
import {
  formatTimeOnly,
  getHotelStatusLabel,
  getRatePlanStatusLabel,
  getRoomTypeStatusLabel,
  getStatusClass,
} from '../../hotel/utils/presentation';
import { getAdminHotelSectionPath, getHotelManagementSectionPath } from '../../hotel/utils/navigation';

function getTargetPath(mode, key) {
  return mode === 'admin' ? getAdminHotelSectionPath(key) : getHotelManagementSectionPath(key);
}

export default function HotelInventoryPage({ mode = 'tenant', adminScope = null }) {
  const isAdmin = mode === 'admin';
  const tenantId = adminScope?.tenantId;
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [hotels, setHotels] = useState([]);
  const [roomTypes, setRoomTypes] = useState([]);
  const [ratePlans, setRatePlans] = useState([]);
  const [extraServices, setExtraServices] = useState([]);

  async function loadData() {
    if (isAdmin && !tenantId) {
      setLoading(false);
      setHotels([]);
      setRoomTypes([]);
      setRatePlans([]);
      setExtraServices([]);
      return;
    }

    setLoading(true);
    setError('');

    try {
      const [optionsResponse, hotelsResponse, roomTypesResponse, ratePlansResponse] = await Promise.all([
        isAdmin ? getAdminHotelOptions(tenantId) : getHotelManagerOptions(),
        isAdmin ? listAdminHotels({ includeDeleted: true, pageSize: 100 }, tenantId) : listManagedHotels({ includeDeleted: true, pageSize: 100 }),
        isAdmin ? listAdminRoomTypes({ includeDeleted: true, pageSize: 100 }, tenantId) : listManagedRoomTypes({ includeDeleted: true, pageSize: 100 }),
        isAdmin ? listAdminRatePlans({ includeDeleted: true, pageSize: 100 }, tenantId) : listManagedRatePlans({ includeDeleted: true, pageSize: 100 }),
      ]);

      setHotels(Array.isArray(hotelsResponse?.items) ? hotelsResponse.items : []);
      setRoomTypes(Array.isArray(roomTypesResponse?.items) ? roomTypesResponse.items : []);
      setRatePlans(Array.isArray(ratePlansResponse?.items) ? ratePlansResponse.items : []);
      setExtraServices(Array.isArray(optionsResponse?.extraServices) ? optionsResponse.extraServices : []);
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải kho khách sạn.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadData();
  }, [isAdmin, tenantId]);

  const activeHotels = useMemo(() => hotels.filter((item) => !item.isDeleted), [hotels]);
  const activeRoomTypes = useMemo(() => roomTypes.filter((item) => !item.isDeleted), [roomTypes]);
  const activeRatePlans = useMemo(() => ratePlans.filter((item) => !item.isDeleted), [ratePlans]);
  const pausedHotels = activeHotels.filter((item) => String(item.status || '').toLowerCase() === 'inactive' || Number(item.status) === 3);

  return (
    <HotelModeShell
      mode={mode}
      adminScope={adminScope}
      pageKey="overview"
      title={isAdmin ? 'Kho khách sạn toàn hệ thống' : 'Kho khách sạn'}
      subtitle={isAdmin
        ? 'Admin rà theo từng tenant khách sạn, theo dõi inventory, giá bán và chất lượng nội dung room type.'
        : 'Theo dõi khách sạn, hạng phòng, gói giá và dịch vụ thêm của tenant khách sạn hiện tại.'}
      notice={notice}
      error={error}
      actions={(
        <button
          type="button"
          onClick={loadData}
          className="px-5 py-3 rounded-2xl border border-slate-200 bg-white text-sm font-black text-slate-600 flex items-center gap-2"
        >
          <RefreshCw size={16} />
          Làm mới
        </button>
      )}
    >
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
        {[
          { label: 'Khách sạn đang quản lý', value: activeHotels.length, icon: Building2 },
          { label: 'Hạng phòng đang bán', value: activeRoomTypes.length, icon: BedDouble },
          { label: 'Gói giá đang vận hành', value: activeRatePlans.length, icon: Layers3 },
          { label: 'Dịch vụ thêm', value: extraServices.filter((item) => !item.isDeleted).length, icon: WalletCards },
        ].map((item) => {
          const Icon = item.icon;
          return (
            <div key={item.label} className="bg-white rounded-[2.5rem] border border-slate-100 p-6 shadow-sm">
              <div className="w-12 h-12 rounded-2xl bg-slate-50 text-[#1EB4D4] flex items-center justify-center mb-5">
                <Icon size={22} />
              </div>
              <p className="text-3xl font-black text-slate-900">{loading ? '--' : item.value}</p>
              <p className="text-[10px] font-black uppercase tracking-widest text-slate-400 mt-2">{item.label}</p>
            </div>
          );
        })}
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-2 gap-8">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100 flex items-center justify-between gap-4">
            <div>
              <p className="text-lg font-black text-slate-900">Khách sạn gần đây</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Theo dõi tình trạng bán, giờ check-in/out và độ phủ inventory theo tenant đang chọn.</p>
            </div>
            <Link to={getTargetPath(mode, 'room-types')} className="text-xs font-black uppercase tracking-widest text-blue-600">
              Mở hạng phòng
            </Link>
          </div>
          <div className="divide-y divide-slate-50">
            {loading ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải khách sạn...</div>
            ) : hotels.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có khách sạn nào.</div>
            ) : hotels.slice(0, 6).map((item) => (
              <div key={item.id} className="px-8 py-6">
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <div className="flex items-center gap-3 flex-wrap">
                      <p className="font-black text-slate-900">{item.name}</p>
                      <span className={`px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest ${getStatusClass(item.status)}`}>
                        {getHotelStatusLabel(item.status)}
                      </span>
                      {item.isDeleted ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">Đã ẩn</span> : null}
                    </div>
                    <p className="text-xs font-bold text-slate-400 mt-2">
                      {[item.city, item.province].filter(Boolean).join(', ') || 'Chưa cập nhật địa chỉ'}
                    </p>
                    <p className="text-xs font-bold text-slate-400 mt-1">
                      Check-in {formatTimeOnly(item.defaultCheckInTime)} • Check-out {formatTimeOnly(item.defaultCheckOutTime)}
                    </p>
                  </div>
                  <Link
                    to={`${getTargetPath(mode, 'rate-plans')}?hotelId=${item.id}`}
                    className="px-4 py-3 rounded-2xl bg-slate-100 text-xs font-black uppercase tracking-widest text-slate-600 inline-flex items-center gap-2"
                  >
                    Gói giá
                    <ChevronRight size={14} />
                  </Link>
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100 flex items-center justify-between gap-4">
            <div>
              <p className="text-lg font-black text-slate-900">Hạng phòng & dịch vụ</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Tổng hợp nhanh số hạng phòng khả dụng, gói giá và dịch vụ upsell của tenant.</p>
            </div>
            <Link to={getTargetPath(mode, 'ari')} className="text-xs font-black uppercase tracking-widest text-blue-600">
              Mở ARI
            </Link>
          </div>
          <div className="px-8 py-6 grid grid-cols-1 md:grid-cols-3 gap-4 border-b border-slate-50">
            <div className="rounded-[2rem] bg-slate-50 p-5">
              <p className="text-[10px] font-black uppercase tracking-widest text-slate-400">Khách sạn tạm ngưng</p>
              <p className="text-3xl font-black text-slate-900 mt-3">{pausedHotels.length}</p>
            </div>
            <div className="rounded-[2rem] bg-slate-50 p-5">
              <p className="text-[10px] font-black uppercase tracking-widest text-slate-400">Hạng phòng có tồn</p>
              <p className="text-3xl font-black text-slate-900 mt-3">
                {activeRoomTypes.filter((item) => Number(item.totalUnits || 0) > 0).length}
              </p>
            </div>
            <div className="rounded-[2rem] bg-slate-50 p-5">
              <p className="text-[10px] font-black uppercase tracking-widest text-slate-400">Gói giá hoàn hủy</p>
              <p className="text-3xl font-black text-slate-900 mt-3">
                {activeRatePlans.filter((item) => item.refundable).length}
              </p>
            </div>
          </div>
          <div className="divide-y divide-slate-50">
            {roomTypes.slice(0, 6).map((item) => (
              <div key={item.id} className="px-8 py-6 flex items-start justify-between gap-4">
                <div>
                  <div className="flex items-center gap-3 flex-wrap">
                    <p className="font-black text-slate-900">{item.name}</p>
                    <span className={`px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest ${getStatusClass(item.status)}`}>
                      {getRoomTypeStatusLabel(item.status)}
                    </span>
                    {item.isDeleted ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">Đã ẩn</span> : null}
                  </div>
                  <p className="text-xs font-bold text-slate-400 mt-2">
                    {item.code} • {item.totalUnits || 0} phòng • Tối đa {item.maxGuests || item.maxAdults || 1} khách
                  </p>
                </div>
                <Link
                  to={`${getTargetPath(mode, 'rate-plans')}?roomTypeId=${item.id}`}
                  className="px-4 py-3 rounded-2xl bg-slate-900 text-xs font-black uppercase tracking-widest text-white inline-flex items-center gap-2"
                >
                  Giá bán
                  <ChevronRight size={14} />
                </Link>
              </div>
            ))}
            {!loading && roomTypes.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có hạng phòng nào.</div>
            ) : null}
          </div>
        </div>
      </div>

      <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
        <div className="px-8 py-6 border-b border-slate-100 flex items-center justify-between gap-4">
          <div>
            <p className="text-lg font-black text-slate-900">Gói giá gần đây</p>
            <p className="text-xs font-bold text-slate-400 mt-1">Đối chiếu nhanh tình trạng refundable, breakfast và policy đang gắn với gói bán.</p>
          </div>
          <Link to={getTargetPath(mode, 'policies')} className="text-xs font-black uppercase tracking-widest text-blue-600">
            Mở chính sách
          </Link>
        </div>
        <div className="divide-y divide-slate-50">
          {loading ? (
            <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải gói giá...</div>
          ) : ratePlans.length === 0 ? (
            <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có gói giá nào.</div>
          ) : ratePlans.slice(0, 6).map((item) => (
            <div key={item.id} className="px-8 py-6 flex items-start justify-between gap-4">
              <div>
                <div className="flex items-center gap-3 flex-wrap">
                  <p className="font-black text-slate-900">{item.name}</p>
                  <span className={`px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest ${getStatusClass(item.status)}`}>
                    {getRatePlanStatusLabel(item.status)}
                  </span>
                  {item.breakfastIncluded ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-sky-100 text-sky-700">Kèm ăn sáng</span> : null}
                </div>
                <p className="text-xs font-bold text-slate-400 mt-2">
                  {item.code} • {item.refundable ? 'Hoàn hủy được' : 'Không hoàn hủy'}
                </p>
              </div>
              <span className="text-xs font-black uppercase tracking-widest text-slate-400">{item.isDeleted ? 'Đã ẩn' : 'Đang dùng'}</span>
            </div>
          ))}
        </div>
      </div>
    </HotelModeShell>
  );
}
