import React, { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { Building2, MapPinned, Plane, Ticket, RefreshCw, ChevronRight } from 'lucide-react';
import FlightManagementPageShell from '../flight/components/FlightManagementPageShell';
import { getFlightManagerOptions } from '../../../services/flightService';
import { formatDateTime } from '../flight/utils/presentation';

const FlightOperationsPage = () => {
  const [options, setOptions] = useState({ airlines: [], airports: [], flights: [], offers: [] });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  async function loadData() {
    setLoading(true);
    setError('');

    try {
      const response = await getFlightManagerOptions();
      setOptions({
        airlines: Array.isArray(response?.airlines) ? response.airlines : [],
        airports: Array.isArray(response?.airports) ? response.airports : [],
        flights: Array.isArray(response?.flights) ? response.flights : [],
        offers: Array.isArray(response?.offers) ? response.offers : [],
      });
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải dữ liệu vận hành hàng không.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadData();
  }, []);

  const activeFlights = useMemo(
    () => options.flights.filter((item) => item.isActive),
    [options.flights],
  );

  return (
    <FlightManagementPageShell
      pageKey="operations"
      title="Vận hành hàng không"
      subtitle="Quản lý hãng bay, sân bay, lịch bay, fare class, điều kiện vé và offer đang bán trên nền tảng."
      error={error}
      actions={(
        <button type="button" onClick={loadData} className="px-5 py-3 rounded-2xl border border-slate-200 bg-white text-sm font-black text-slate-600 flex items-center gap-2">
          <RefreshCw size={16} />
          Làm mới
        </button>
      )}
    >
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
        {[
          { label: 'Hãng bay', value: options.airlines.length, icon: Building2 },
          { label: 'Sân bay', value: options.airports.length, icon: MapPinned },
          { label: 'Lịch bay hoạt động', value: activeFlights.length, icon: Plane },
          { label: 'Offer đang bán', value: options.offers.length, icon: Ticket },
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

      <div className="grid grid-cols-1 xl:grid-cols-3 gap-8">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100 flex items-center justify-between">
            <div>
              <p className="text-lg font-black text-slate-900">Hãng bay</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Thông tin thương hiệu và kênh hỗ trợ khách hàng.</p>
            </div>
            <Link to="/tenant/operations/flight/airlines" className="text-xs font-black uppercase tracking-widest text-blue-600">
              Mở
            </Link>
          </div>
          <div className="divide-y divide-slate-50">
            {options.airlines.slice(0, 4).map((item) => (
              <div key={item.id} className="px-8 py-5">
                <p className="font-black text-slate-900">{item.name}</p>
                <p className="text-xs font-bold text-slate-400 mt-1">
                  {item.iataCode || item.code} • {item.supportPhone || 'Chưa có hotline'}
                </p>
              </div>
            ))}
            {!loading && options.airlines.length === 0 ? (
              <div className="px-8 py-8 text-sm font-bold text-slate-500">Chưa có hãng bay nào.</div>
            ) : null}
          </div>
        </div>

        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100 flex items-center justify-between">
            <div>
              <p className="text-lg font-black text-slate-900">Sân bay</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Điểm đi và điểm đến dùng chung cho toàn bộ lịch bay.</p>
            </div>
            <Link to="/tenant/operations/flight/airports" className="text-xs font-black uppercase tracking-widest text-blue-600">
              Mở
            </Link>
          </div>
          <div className="divide-y divide-slate-50">
            {options.airports.slice(0, 4).map((item) => (
              <div key={item.id} className="px-8 py-5">
                <p className="font-black text-slate-900">{item.name}</p>
                <p className="text-xs font-bold text-slate-400 mt-1">
                  {item.iataCode || item.code} • {item.timeZone || 'Asia/Ho_Chi_Minh'}
                </p>
              </div>
            ))}
            {!loading && options.airports.length === 0 ? (
              <div className="px-8 py-8 text-sm font-bold text-slate-500">Chưa có sân bay nào.</div>
            ) : null}
          </div>
        </div>

        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100 flex items-center justify-between">
            <div>
              <p className="text-lg font-black text-slate-900">Lịch bay mới</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Kiểm tra nhanh các chuyến gần nhất đang được cấu hình.</p>
            </div>
            <Link to="/tenant/operations/flight/flights" className="text-xs font-black uppercase tracking-widest text-blue-600 inline-flex items-center gap-2">
              Mở
              <ChevronRight size={14} />
            </Link>
          </div>
          <div className="divide-y divide-slate-50">
            {options.flights.slice(0, 4).map((item) => (
              <div key={item.id} className="px-8 py-5">
                <p className="font-black text-slate-900">{item.flightNumber}</p>
                <p className="text-xs font-bold text-slate-400 mt-1">
                  {(item.fromAirport?.iataCode || item.fromAirport?.code || '---')} → {(item.toAirport?.iataCode || item.toAirport?.code || '---')}
                </p>
                <p className="text-xs font-bold text-slate-400 mt-1">{formatDateTime(item.departureAt)}</p>
              </div>
            ))}
            {!loading && options.flights.length === 0 ? (
              <div className="px-8 py-8 text-sm font-bold text-slate-500">Chưa có lịch bay nào.</div>
            ) : null}
          </div>
        </div>
      </div>
    </FlightManagementPageShell>
  );
};

export default FlightOperationsPage;
