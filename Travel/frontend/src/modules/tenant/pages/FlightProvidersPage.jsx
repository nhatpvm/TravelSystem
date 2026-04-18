import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Armchair, BriefcaseBusiness, Grid2X2, RefreshCw, ShoppingBag } from 'lucide-react';
import FlightManagementPageShell from '../flight/components/FlightManagementPageShell';
import { getFlightManagerOptions } from '../../../services/flightService';

const FlightProvidersPage = () => {
  const [options, setOptions] = useState({ aircraftModels: [], aircrafts: [], seatMaps: [], ancillaries: [] });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  async function loadData() {
    setLoading(true);
    setError('');

    try {
      const response = await getFlightManagerOptions();
      setOptions({
        aircraftModels: Array.isArray(response?.aircraftModels) ? response.aircraftModels : [],
        aircrafts: Array.isArray(response?.aircrafts) ? response.aircrafts : [],
        seatMaps: Array.isArray(response?.seatMaps) ? response.seatMaps : [],
        ancillaries: Array.isArray(response?.ancillaries) ? response.ancillaries : [],
      });
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải dữ liệu đội bay.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadData();
  }, []);

  return (
    <FlightManagementPageShell
      pageKey="providers"
      title="Đội bay & ghế cabin"
      subtitle="Quản lý mẫu tàu bay, đội bay khai thác, sơ đồ cabin, ghế ngồi và các dịch vụ bổ sung."
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
          { label: 'Mẫu tàu bay', value: options.aircraftModels.length, icon: Grid2X2 },
          { label: 'Tàu bay', value: options.aircrafts.length, icon: BriefcaseBusiness },
          { label: 'Sơ đồ cabin', value: options.seatMaps.length, icon: Armchair },
          { label: 'Dịch vụ thêm', value: options.ancillaries.length, icon: ShoppingBag },
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
          <div className="px-8 py-6 border-b border-slate-100 flex items-center justify-between">
            <div>
              <p className="text-lg font-black text-slate-900">Tàu bay đang khai thác</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Theo dõi registration và cấu hình cabin của từng tàu bay.</p>
            </div>
            <Link to="/tenant/providers/flight/aircrafts" className="text-xs font-black uppercase tracking-widest text-blue-600">
              Mở
            </Link>
          </div>
          <div className="divide-y divide-slate-50">
            {options.aircrafts.slice(0, 5).map((item) => (
              <div key={item.id} className="px-8 py-5">
                <p className="font-black text-slate-900">{item.registration || item.code}</p>
                <p className="text-xs font-bold text-slate-400 mt-1">
                  {item.aircraftModel?.manufacturer} {item.aircraftModel?.model}
                </p>
                <p className="text-xs font-bold text-slate-400 mt-1">{item.airline?.name || 'Chưa gắn hãng bay'}</p>
              </div>
            ))}
            {!loading && options.aircrafts.length === 0 ? (
              <div className="px-8 py-8 text-sm font-bold text-slate-500">Chưa có tàu bay nào.</div>
            ) : null}
          </div>
        </div>

        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-8 py-6 border-b border-slate-100 flex items-center justify-between">
            <div>
              <p className="text-lg font-black text-slate-900">Sơ đồ cabin & dịch vụ thêm</p>
              <p className="text-xs font-bold text-slate-400 mt-1">Các cabin seat map và ancillary đang được dùng trên marketplace.</p>
            </div>
            <div className="flex items-center gap-4">
              <Link to="/tenant/providers/flight/seat-maps" className="text-xs font-black uppercase tracking-widest text-blue-600">
                Seat map
              </Link>
              <Link to="/tenant/providers/flight/ancillaries" className="text-xs font-black uppercase tracking-widest text-blue-600">
                Dịch vụ
              </Link>
            </div>
          </div>
          <div className="divide-y divide-slate-50">
            {options.seatMaps.slice(0, 3).map((item) => (
              <div key={item.id} className="px-8 py-5">
                <p className="font-black text-slate-900">{item.name}</p>
                <p className="text-xs font-bold text-slate-400 mt-1">
                  {item.aircraftModel?.manufacturer} {item.aircraftModel?.model} • {item.seatCount || 0} ghế
                </p>
              </div>
            ))}
            {options.ancillaries.slice(0, 3).map((item) => (
              <div key={item.id} className="px-8 py-5">
                <p className="font-black text-slate-900">{item.name}</p>
                <p className="text-xs font-bold text-slate-400 mt-1">
                  {item.airline?.name || 'Chưa gắn hãng'} • {item.currencyCode} {item.price}
                </p>
              </div>
            ))}
            {!loading && options.seatMaps.length === 0 && options.ancillaries.length === 0 ? (
              <div className="px-8 py-8 text-sm font-bold text-slate-500">Chưa có seat map hoặc dịch vụ thêm nào.</div>
            ) : null}
          </div>
        </div>
      </div>
    </FlightManagementPageShell>
  );
};

export default FlightProvidersPage;
