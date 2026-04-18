import React, { useEffect, useState } from 'react';
import { PlayCircle, RefreshCw, ShieldCheck, ShieldX } from 'lucide-react';
import MasterDataPageShell from '../master-data/components/MasterDataPageShell';
import { GEO_SYNC_DEPTH_OPTIONS, formatDateTime } from '../master-data/utils/options';
import { listGeoProvinces, listGeoSyncLogs, runGeoSync } from '../../../services/masterDataService';

const AdminGeoSyncPage = () => {
  const [loading, setLoading] = useState(true);
  const [syncing, setSyncing] = useState(false);
  const [depth, setDepth] = useState(3);
  const [notice, setNotice] = useState('');
  const [error, setError] = useState('');
  const [provinces, setProvinces] = useState([]);
  const [logs, setLogs] = useState([]);
  const [lastResult, setLastResult] = useState(null);

  useEffect(() => {
    loadData();
  }, []);

  async function loadData() {
    setLoading(true);
    setError('');

    try {
      const [provinceResponse, logsResponse] = await Promise.all([
        listGeoProvinces(),
        listGeoSyncLogs({ page: 1, pageSize: 5 }),
      ]);

      setProvinces(provinceResponse.items || []);
      setLogs(logsResponse.items || []);
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải dữ liệu địa giới.');
      setProvinces([]);
      setLogs([]);
    } finally {
      setLoading(false);
    }
  }

  async function handleSync() {
    setSyncing(true);
    setNotice('');
    setError('');

    try {
      const response = await runGeoSync(depth);
      setLastResult(response);
      setNotice('Đồng bộ địa giới đã chạy xong. Kiểm tra nhật ký gần nhất ở bên dưới.');
      await loadData();
    } catch (requestError) {
      setError(requestError.message || 'Không thể thực hiện đồng bộ địa giới.');
      setLastResult(null);
    } finally {
      setSyncing(false);
    }
  }

  const latestLog = logs[0] || null;

  return (
    <MasterDataPageShell
      pageKey="geo-sync"
      title="Đồng bộ địa giới"
      subtitle="Đồng bộ dữ liệu địa giới hành chính dùng chung cho toàn hệ thống."
      showTenantSelector={false}
      scopeHint="Đồng bộ địa giới là dữ liệu toàn cục, không cần chọn tenant."
      error={error}
      notice={notice}
      actions={(
        <div className="flex items-center gap-3">
          <button onClick={loadData} className="px-5 py-3 bg-white text-slate-700 rounded-2xl font-black text-xs uppercase tracking-widest flex items-center gap-2 border border-slate-100 shadow-sm">
            <RefreshCw size={14} /> Tải lại
          </button>
          <button onClick={handleSync} disabled={syncing} className="px-5 py-3 bg-slate-900 text-white rounded-2xl font-black text-xs uppercase tracking-widest flex items-center gap-2 shadow-sm disabled:opacity-60">
            <PlayCircle size={14} /> {syncing ? 'Đang đồng bộ...' : 'Chạy đồng bộ'}
          </button>
        </div>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[0.8fr,1.2fr] gap-6">
        <div className="space-y-6">
          <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-6 space-y-5">
            <div>
              <h3 className="text-xl font-black text-slate-900">Chạy đồng bộ dữ liệu địa giới</h3>
              <p className="text-sm font-medium text-slate-500 mt-1">Nguồn dữ liệu: provinces.open-api.vn</p>
            </div>
            <select value={depth} onChange={(event) => setDepth(Number(event.target.value))} className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-700 outline-none">
              {GEO_SYNC_DEPTH_OPTIONS.map((item) => (
                <option key={item.value} value={item.value}>{item.label}</option>
              ))}
            </select>
            <div className="grid grid-cols-2 gap-4">
              <div className="rounded-2xl bg-slate-50 px-5 py-4 border border-slate-100">
                <p className="text-3xl font-black text-slate-900">{loading ? '...' : provinces.length}</p>
                <p className="text-[10px] font-black uppercase tracking-widest text-slate-400 mt-1">Tỉnh/thành trong CSDL</p>
              </div>
              <div className="rounded-2xl bg-slate-50 px-5 py-4 border border-slate-100">
                <p className="text-3xl font-black text-slate-900">{loading ? '...' : logs.length}</p>
                <p className="text-[10px] font-black uppercase tracking-widest text-slate-400 mt-1">Nhật ký gần nhất</p>
              </div>
            </div>
            {lastResult && (
              <div className="rounded-2xl border border-emerald-100 bg-emerald-50 px-5 py-4">
                <p className="text-sm font-black text-emerald-700">Kết quả đồng bộ mới nhất</p>
                <p className="text-xs font-bold text-emerald-600 mt-2">
                  Tỉnh/thành: {Number(lastResult.provinces?.inserted || 0) + Number(lastResult.provinces?.updated || 0)} |
                  Quận/huyện: {Number(lastResult.districts?.inserted || 0) + Number(lastResult.districts?.updated || 0)} |
                  Phường/xã: {Number(lastResult.wards?.inserted || 0) + Number(lastResult.wards?.updated || 0)}
                </p>
              </div>
            )}
          </div>

          <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-6">
            <div className="flex items-center justify-between gap-4">
              <div>
                <h3 className="text-xl font-black text-slate-900">Trạng thái đồng bộ gần nhất</h3>
                <p className="text-sm font-medium text-slate-500 mt-1">Thông tin lần đồng bộ gần nhất.</p>
              </div>
              {latestLog ? (
                <span className={`px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest ${latestLog.isSuccess ? 'bg-emerald-100 text-emerald-700' : 'bg-rose-100 text-rose-600'}`}>
                  {latestLog.isSuccess ? 'Thành công' : 'Thất bại'}
                </span>
              ) : null}
            </div>
            {latestLog ? (
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mt-5">
                <div className="rounded-2xl bg-slate-50 px-5 py-4 border border-slate-100">
                  <p className="text-[10px] font-black uppercase tracking-widest text-slate-400">Thời điểm</p>
                  <p className="text-sm font-bold text-slate-900 mt-2">{formatDateTime(latestLog.createdAt)}</p>
                </div>
                <div className="rounded-2xl bg-slate-50 px-5 py-4 border border-slate-100">
                  <p className="text-[10px] font-black uppercase tracking-widest text-slate-400">HTTP / Độ sâu</p>
                  <p className="text-sm font-bold text-slate-900 mt-2">HTTP {latestLog.httpStatus || 'N/A'} | Cấp {latestLog.depth}</p>
                </div>
                <div className="rounded-2xl bg-slate-50 px-5 py-4 border border-slate-100">
                  <p className="text-[10px] font-black uppercase tracking-widest text-slate-400">Tỉnh/thành</p>
                  <p className="text-sm font-bold text-slate-900 mt-2">{Number(latestLog.provincesInserted || 0) + Number(latestLog.provincesUpdated || 0)}</p>
                </div>
                <div className="rounded-2xl bg-slate-50 px-5 py-4 border border-slate-100">
                  <p className="text-[10px] font-black uppercase tracking-widest text-slate-400">Quận/huyện / Phường/xã</p>
                  <p className="text-sm font-bold text-slate-900 mt-2">
                    {Number(latestLog.districtsInserted || 0) + Number(latestLog.districtsUpdated || 0)} / {Number(latestLog.wardsInserted || 0) + Number(latestLog.wardsUpdated || 0)}
                  </p>
                </div>
              </div>
            ) : (
              <div className="mt-5 text-sm font-bold text-slate-400">Chưa có nhật ký đồng bộ nào.</div>
            )}
          </div>
        </div>

        <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm overflow-hidden">
          <div className="p-8 border-b border-slate-100 flex items-center justify-between gap-4">
            <div>
              <h2 className="text-2xl font-black text-slate-900 tracking-tight">Danh sách tỉnh/thành</h2>
              <p className="text-slate-500 font-medium mt-1">Xem nhanh dữ liệu địa giới hiện có trong cơ sở dữ liệu.</p>
            </div>
            {latestLog?.isSuccess ? <ShieldCheck size={20} className="text-emerald-600" /> : <ShieldX size={20} className="text-rose-500" />}
          </div>
          <div className="divide-y divide-slate-50 max-h-[680px] overflow-y-auto">
            {loading ? (
              <div className="px-8 py-6 text-sm font-bold text-slate-400">Đang tải tỉnh/thành...</div>
            ) : provinces.length === 0 ? (
              <div className="px-8 py-6 text-sm font-bold text-slate-400">Chưa có tỉnh/thành nào.</div>
            ) : provinces.map((item) => (
              <div key={item.id} className="px-8 py-5 flex items-center justify-between gap-4 hover:bg-slate-50/70 transition-all">
                <div>
                  <p className="font-black text-slate-900">{item.name}</p>
                  <p className="text-xs font-bold text-slate-400 mt-1">Mã {item.code} | {item.type || 'Tỉnh/thành'}</p>
                </div>
                <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest bg-slate-100 text-slate-500">
                  Tỉnh/thành
                </span>
              </div>
            ))}
          </div>
        </div>
      </div>
    </MasterDataPageShell>
  );
};

export default AdminGeoSyncPage;
