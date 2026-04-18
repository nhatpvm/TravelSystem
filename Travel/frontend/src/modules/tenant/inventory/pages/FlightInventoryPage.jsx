import React, { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { Plane, RefreshCw, Ticket, PlaneTakeoff, Clock, ChevronRight } from 'lucide-react';
import FlightModeShell from '../../flight/components/FlightModeShell';
import {
  getAdminFlightOptions,
  getFlightManagerOptions,
  listAdminFlights,
  listAdminFlightOffers,
  listFlightManagerFlights,
  listFlightManagerOffers,
} from '../../../../services/flightService';
import {
  formatCurrency,
  formatDateTime,
  getAirlineDisplayName,
  getFlightStatusClass,
  getFlightStatusLabel,
  getOfferStatusClass,
  getOfferStatusLabel,
} from '../../flight/utils/presentation';

function getFlightRouteLabel(flight) {
  const from = flight?.fromAirport?.iataCode || flight?.fromAirport?.code || flight?.fromAirport?.name || '---';
  const to = flight?.toAirport?.iataCode || flight?.toAirport?.code || flight?.toAirport?.name || '---';
  return `${from} → ${to}`;
}

export default function FlightInventoryPage({ mode = 'tenant', adminScope = null }) {
  const isAdmin = mode === 'admin';
  const tenantId = adminScope?.tenantId;
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [options, setOptions] = useState({ airlines: [], aircrafts: [], fareClasses: [] });
  const [flights, setFlights] = useState([]);
  const [offers, setOffers] = useState([]);

  async function loadData() {
    if (isAdmin && !tenantId) {
      setLoading(false);
      setFlights([]);
      setOffers([]);
      setOptions({ airlines: [], aircrafts: [], fareClasses: [] });
      return;
    }

    setLoading(true);
    setError('');

    try {
      const [optionsResponse, flightsResponse, offersResponse] = await Promise.all([
        isAdmin ? getAdminFlightOptions(tenantId) : getFlightManagerOptions(),
        isAdmin ? listAdminFlights({ includeDeleted: true, pageSize: 100 }, tenantId) : listFlightManagerFlights({ includeDeleted: true, pageSize: 100 }),
        isAdmin ? listAdminFlightOffers({ includeDeleted: true, pageSize: 100 }, tenantId) : listFlightManagerOffers({ includeDeleted: true, pageSize: 100 }),
      ]);

      setOptions({
        airlines: Array.isArray(optionsResponse?.airlines) ? optionsResponse.airlines : [],
        aircrafts: Array.isArray(optionsResponse?.aircrafts) ? optionsResponse.aircrafts : [],
        fareClasses: Array.isArray(optionsResponse?.fareClasses) ? optionsResponse.fareClasses : [],
      });
      setFlights(Array.isArray(flightsResponse?.items) ? flightsResponse.items : []);
      setOffers(Array.isArray(offersResponse?.items) ? offersResponse.items : []);
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải kho vé máy bay.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadData();
  }, [isAdmin, tenantId]);

  const activeFlights = useMemo(
    () => flights.filter((item) => !item.isDeleted),
    [flights],
  );

  const activeOffers = useMemo(
    () => offers.filter((item) => !item.isDeleted),
    [offers],
  );

  const openFlights = activeFlights.filter((item) => String(item.status || '').toLowerCase() === 'published' || Number(item.status) === 2);
  const sellableOffers = activeOffers.filter((item) => String(item.status || '').toLowerCase() === 'active' || Number(item.status) === 1);

  return (
    <FlightModeShell
      mode={mode}
      adminScope={adminScope}
      pageKey="overview"
      title={isAdmin ? 'Kho vé máy bay toàn hệ thống' : 'Kho vé máy bay'}
      subtitle={isAdmin
        ? 'Admin rà theo từng tenant hàng không, kiểm tra lịch bay, offer và mức giá đang bán trên marketplace.'
        : 'Theo dõi tổng quan lịch bay, fare class và doanh số chỗ bán của tenant hàng không hiện tại.'}
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
          { label: 'Hãng bay hoạt động', value: options.airlines.filter((item) => item.isActive !== false).length, icon: Plane },
          { label: 'Lịch bay đang quản lý', value: activeFlights.length, icon: PlaneTakeoff },
          { label: 'Chuyến đang mở bán', value: openFlights.length, icon: Clock },
          { label: 'Offer đang bán', value: sellableOffers.length, icon: Ticket },
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
              <p className="text-lg font-black text-slate-900">Lịch bay mới nhất</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Các chuyến vừa tạo hoặc vừa cập nhật của tenant đang chọn.</p>
            </div>
            <Link to={isAdmin ? '/admin/flight/flights' : '/tenant/operations/flight/flights'} className="text-xs font-black uppercase tracking-widest text-blue-600">
              Mở lịch bay
            </Link>
          </div>
          <div className="divide-y divide-slate-50">
            {loading ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải lịch bay...</div>
            ) : flights.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có lịch bay nào.</div>
            ) : flights.slice(0, 6).map((flight) => (
              <div key={flight.id} className="px-8 py-6">
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <div className="flex items-center gap-3 flex-wrap">
                      <p className="font-black text-slate-900">{flight.flightNumber}</p>
                      <span className={`px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest ${getFlightStatusClass(flight.status)}`}>
                        {getFlightStatusLabel(flight.status)}
                      </span>
                      {flight.isDeleted ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">Đã ẩn</span> : null}
                    </div>
                    <p className="text-xs font-bold text-slate-400 mt-2">{getFlightRouteLabel(flight)}</p>
                    <p className="text-xs font-bold text-slate-400 mt-1">
                      {formatDateTime(flight.departureAt)} • {getAirlineDisplayName(flight.airline)}
                    </p>
                  </div>
                  <Link
                    to={`${isAdmin ? '/admin/flight/offers' : '/tenant/operations/flight/offers'}?flightId=${flight.id}`}
                    className="px-4 py-3 rounded-2xl bg-slate-100 text-xs font-black uppercase tracking-widest text-slate-600 inline-flex items-center gap-2"
                  >
                    Offer
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
              <p className="text-lg font-black text-slate-900">Offer đang bán</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Mỗi offer gắn với chuyến bay, fare class và giá bán hiện hành.</p>
            </div>
            <Link to={isAdmin ? '/admin/flight/offers' : '/tenant/operations/flight/offers'} className="text-xs font-black uppercase tracking-widest text-blue-600">
              Mở offer
            </Link>
          </div>
          <div className="divide-y divide-slate-50">
            {loading ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Đang tải offer...</div>
            ) : offers.length === 0 ? (
              <div className="px-8 py-10 text-sm font-bold text-slate-500">Chưa có offer nào.</div>
            ) : offers.slice(0, 6).map((offer) => (
              <div key={offer.id} className="px-8 py-6">
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <div className="flex items-center gap-3 flex-wrap">
                      <p className="font-black text-slate-900">{offer.flight?.flightNumber || offer.id.slice(0, 8)}</p>
                      <span className={`px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest ${getOfferStatusClass(offer.status)}`}>
                        {getOfferStatusLabel(offer.status)}
                      </span>
                      {offer.isDeleted ? <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-rose-100 text-rose-700">Đã ẩn</span> : null}
                    </div>
                    <p className="text-xs font-bold text-slate-400 mt-2">
                      {(offer.fareClass?.name || 'Fare class')} • {offer.flight?.flightNumber || 'Chưa gắn chuyến'}
                    </p>
                    <p className="text-xs font-bold text-slate-400 mt-1">
                      {formatCurrency(offer.totalPrice, offer.currencyCode)} • {offer.seatsAvailable || 0} chỗ còn lại
                    </p>
                  </div>
                  <Link
                    to={`${isAdmin ? '/admin/flight/tax-fee-lines' : '/tenant/operations/flight/tax-fee-lines'}?offerId=${offer.id}`}
                    className="px-4 py-3 rounded-2xl bg-slate-900 text-xs font-black uppercase tracking-widest text-white inline-flex items-center gap-2"
                  >
                    Thuế & phí
                    <ChevronRight size={14} />
                  </Link>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </FlightModeShell>
  );
}
