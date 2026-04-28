import React, { useEffect, useMemo, useState } from 'react';
import { ArrowRight, Database, MapPinned, PlayCircle, RefreshCw, Route, ScanLine, ShieldCheck, Truck } from 'lucide-react';
import { Link } from 'react-router-dom';
import MasterDataPageShell from '../master-data/components/MasterDataPageShell';
import useAdminMasterDataScope from '../master-data/hooks/useAdminMasterDataScope';
import {
  listGeoProvinces,
  listGeoSyncLogs,
  listLocations,
  listProviders,
  listSeatMaps,
  listVehicleModels,
  listVehicles,
  runGeoSync,
} from '../../../services/masterDataService';
import { formatDateTime } from '../master-data/utils/options';
import useLatestRef from '../../../shared/hooks/useLatestRef';

const AdminMasterDataPage = () => {
  const {
    tenantId,
    tenants,
    selectedTenantId,
    setSelectedTenantId,
    selectedTenant,
    scopeError,
  } = useAdminMasterDataScope();
  const [loading, setLoading] = useState(true);
  const [syncing, setSyncing] = useState(false);
  const [notice, setNotice] = useState('');
  const [error, setError] = useState('');
  const [stats, setStats] = useState({
    locations: 0,
    providers: 0,
    vehicleModels: 0,
    vehicles: 0,
    seatMaps: 0,
    seats: 0,
    provinces: 0,
  });
  const [latestLogs, setLatestLogs] = useState([]);

  const loadDashboardRef = useLatestRef(loadDashboard);

  useEffect(() => {
    if (!tenantId) {
      return;
    }

    loadDashboardRef.current();
  }, [loadDashboardRef, tenantId]);

  const cards = useMemo(() => ([
    { label: 'Địa điểm', value: stats.locations, icon: MapPinned, path: '/admin/master-data/locations' },
    { label: 'Đối tác', value: stats.providers, icon: Database, path: '/admin/master-data/providers' },
    { label: 'Mẫu phương tiện', value: stats.vehicleModels, icon: Truck, path: '/admin/master-data/vehicle-models' },
    { label: 'Phương tiện', value: stats.vehicles, icon: Route, path: '/admin/master-data/vehicles' },
    { label: 'Sơ đồ ghế', value: stats.seatMaps, icon: ShieldCheck, path: '/admin/master-data/seat-maps' },
    { label: 'Ghế', value: stats.seats, icon: ScanLine, path: '/admin/master-data/seats' },
  ]), [stats]);

  async function loadDashboard() {
    if (!tenantId) {
      return;
    }

    setLoading(true);
    setError('');

    try {
      const [locationsResponse, providersResponse, modelsResponse, vehiclesResponse, seatMapsResponse, provincesResponse, logsResponse] = await Promise.all([
        listLocations({ includeDeleted: true }, tenantId),
        listProviders({ includeDeleted: true }, tenantId),
        listVehicleModels({ includeDeleted: true }, tenantId),
        listVehicles({ includeDeleted: true }, tenantId),
        listSeatMaps({ includeDeleted: true }, tenantId),
        listGeoProvinces(),
        listGeoSyncLogs({ page: 1, pageSize: 5 }),
      ]);

      const seatMaps = seatMapsResponse.items || [];

      setStats({
        locations: (locationsResponse.items || []).length,
        providers: (providersResponse.items || []).length,
        vehicleModels: (modelsResponse.items || []).length,
        vehicles: (vehiclesResponse.items || []).length,
        seatMaps: seatMaps.length,
        seats: seatMaps.reduce((total, item) => total + Number(item.seatCount || 0), 0),
        provinces: provincesResponse.count || (provincesResponse.items || []).length,
      });
      setLatestLogs(logsResponse.items || []);
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải tổng quan dữ liệu nền.');
    } finally {
      setLoading(false);
    }
  }

  async function handleSyncGeo() {
    setSyncing(true);
    setNotice('');
    setError('');

    try {
      await runGeoSync(3);
      setNotice('Đồng bộ địa giới đã được kích hoạt thành công.');
      await loadDashboardRef.current();
    } catch (requestError) {
      setError(requestError.message || 'Không thể đồng bộ dữ liệu địa giới.');
    } finally {
      setSyncing(false);
    }
  }

  return (
    <MasterDataPageShell
      pageKey="overview"
      title="Dữ liệu nền / Địa giới / Đội xe"
      subtitle="Tổng quan dữ liệu nền, địa lý và đội xe để làm nền cho các phase nghiệp vụ."
      tenants={tenants}
      selectedTenantId={selectedTenantId}
      setSelectedTenantId={setSelectedTenantId}
      selectedTenant={selectedTenant}
      error={scopeError || error}
      notice={notice}
      actions={(
        <div className="flex items-center gap-3">
          <button onClick={loadDashboard} className="px-5 py-3 bg-white text-slate-700 rounded-2xl font-black text-xs uppercase tracking-widest flex items-center gap-2 border border-slate-100 shadow-sm">
            <RefreshCw size={14} /> Tải lại
          </button>
          <button onClick={handleSyncGeo} disabled={syncing} className="px-5 py-3 bg-slate-900 text-white rounded-2xl font-black text-xs uppercase tracking-widest flex items-center gap-2 shadow-sm disabled:opacity-60">
            <PlayCircle size={14} /> {syncing ? 'Đang đồng bộ...' : 'Đồng bộ địa giới'}
          </button>
        </div>
      )}
    >
      <div className="grid grid-cols-1 md:grid-cols-3 lg:grid-cols-6 gap-4">
        {cards.map((item) => {
          const Icon = item.icon;

          return (
            <Link key={item.label} to={item.path} className="bg-white rounded-[2rem] p-5 border border-slate-100 shadow-sm hover:shadow-md transition-all">
              <div className="w-11 h-11 rounded-2xl bg-slate-100 text-slate-700 flex items-center justify-center">
                <Icon size={18} />
              </div>
              <p className="text-3xl font-black text-slate-900 mt-5">{loading ? '...' : item.value}</p>
              <p className="text-[10px] font-black uppercase tracking-widest text-slate-400 mt-1">{item.label}</p>
            </Link>
          );
        })}
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-[1.2fr,0.8fr] gap-6">
        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="p-8 border-b border-slate-100 flex items-center justify-between gap-4">
            <div>
              <h2 className="text-2xl font-black text-slate-900 tracking-tight">Danh mục thao tác</h2>
              <p className="text-slate-500 font-medium mt-1">Tách từng màn hình chi tiết để quản lý CRUD đúng theo Phase 5.</p>
            </div>
            <div className="rounded-2xl bg-slate-50 px-4 py-3 border border-slate-100 text-[11px] font-black uppercase tracking-widest text-slate-400">
              {stats.provinces} tỉnh/thành
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 p-8">
            {[
              { title: 'Địa điểm', desc: 'Tạo và chỉnh sửa điểm đến, bến xe, ga tàu, sân bay.', path: '/admin/master-data/locations' },
              { title: 'Đối tác', desc: 'Quản lý nhà xe, hãng bay, đối tác tour và khách sạn.', path: '/admin/master-data/providers' },
              { title: 'Đồng bộ địa giới', desc: 'Đồng bộ địa giới hành chính và giám sát kết quả.', path: '/admin/master-data/geo-sync' },
              { title: 'Mẫu phương tiện', desc: 'Quản lý hãng, model và năm sản xuất.', path: '/admin/master-data/vehicle-models' },
              { title: 'Phương tiện', desc: 'Gắn đối tác, mẫu phương tiện và sơ đồ ghế cho từng phương tiện.', path: '/admin/master-data/vehicles' },
              { title: 'Sơ đồ ghế & Ghế', desc: 'Tạo layout ghế, sinh ghế và cập nhật hàng loạt.', path: '/admin/master-data/seat-maps' },
            ].map((item) => (
              <Link key={item.title} to={item.path} className="rounded-[2rem] border border-slate-100 bg-slate-50 px-6 py-5 hover:bg-white hover:shadow-sm transition-all">
                <div className="flex items-center justify-between gap-4">
                  <div>
                    <p className="text-lg font-black text-slate-900">{item.title}</p>
                    <p className="text-sm font-medium text-slate-500 mt-1">{item.desc}</p>
                  </div>
                  <ArrowRight size={18} className="text-slate-300" />
                </div>
              </Link>
            ))}
          </div>
        </div>

        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="p-8 border-b border-slate-100">
            <h2 className="text-2xl font-black text-slate-900 tracking-tight">Đồng bộ địa giới gần nhất</h2>
            <p className="text-slate-500 font-medium mt-1">Theo dõi nhanh các lần đồng bộ địa giới hành chính.</p>
          </div>
          <div className="divide-y divide-slate-50">
            {loading ? (
              <div className="px-8 py-6 text-sm font-bold text-slate-400">Đang tải tổng quan...</div>
            ) : latestLogs.length === 0 ? (
              <div className="px-8 py-6 text-sm font-bold text-slate-400">Chưa có nhật ký đồng bộ nào.</div>
            ) : latestLogs.map((item) => (
              <div key={item.id} className="px-8 py-5">
                <div className="flex items-center justify-between gap-3">
                  <div>
                    <p className="font-black text-slate-900">Độ sâu {item.depth} | HTTP {item.httpStatus || 'N/A'}</p>
                    <p className="text-xs font-medium text-slate-400 mt-1">{formatDateTime(item.createdAt)}</p>
                  </div>
                  <span className={`px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest ${item.isSuccess ? 'bg-emerald-100 text-emerald-700' : 'bg-rose-100 text-rose-600'}`}>
                    {item.isSuccess ? 'Thành công' : 'Thất bại'}
                  </span>
                </div>
                <div className="grid grid-cols-3 gap-3 mt-4 text-center">
                  <div className="rounded-2xl bg-slate-50 px-3 py-3">
                    <p className="text-lg font-black text-slate-900">{Number(item.provincesInserted || 0) + Number(item.provincesUpdated || 0)}</p>
                    <p className="text-[10px] font-black uppercase tracking-widest text-slate-400 mt-1">Tỉnh/thành</p>
                  </div>
                  <div className="rounded-2xl bg-slate-50 px-3 py-3">
                    <p className="text-lg font-black text-slate-900">{Number(item.districtsInserted || 0) + Number(item.districtsUpdated || 0)}</p>
                    <p className="text-[10px] font-black uppercase tracking-widest text-slate-400 mt-1">Quận/huyện</p>
                  </div>
                  <div className="rounded-2xl bg-slate-50 px-3 py-3">
                    <p className="text-lg font-black text-slate-900">{Number(item.wardsInserted || 0) + Number(item.wardsUpdated || 0)}</p>
                    <p className="text-[10px] font-black uppercase tracking-widest text-slate-400 mt-1">Phường/xã</p>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </MasterDataPageShell>
  );
};

export default AdminMasterDataPage;
