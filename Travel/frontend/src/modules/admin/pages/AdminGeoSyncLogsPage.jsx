import React, { useEffect, useState } from 'react';
import { RefreshCw } from 'lucide-react';
import MasterDataPageShell from '../master-data/components/MasterDataPageShell';
import { formatDateTime, getGeoLogStatusClass, getGeoLogStatusLabel } from '../master-data/utils/options';
import { getGeoSyncLog, listGeoSyncLogs } from '../../../services/masterDataService';

const AdminGeoSyncLogsPage = () => {
  const [logs, setLogs] = useState([]);
  const [loading, setLoading] = useState(true);
  const [detailLoading, setDetailLoading] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [selectedId, setSelectedId] = useState('');
  const [selectedLog, setSelectedLog] = useState(null);

  useEffect(() => {
    loadLogs();
  }, [statusFilter]);

  async function loadLogs() {
    setLoading(true);
    setError('');

    try {
      const response = await listGeoSyncLogs({
        page: 1,
        pageSize: 50,
        isSuccess: statusFilter === 'all' ? undefined : statusFilter === 'success',
      });
      setLogs(response.items || []);
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải danh sách nhật ký đồng bộ địa giới.');
      setLogs([]);
    } finally {
      setLoading(false);
    }
  }

  async function loadDetail(id) {
    setSelectedId(id);
    setDetailLoading(true);
    setError('');

    try {
      const response = await getGeoSyncLog(id);
      setSelectedLog(response);
      setNotice('');
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải chi tiết nhật ký đồng bộ.');
      setSelectedLog(null);
    } finally {
      setDetailLoading(false);
    }
  }

  return (
    <MasterDataPageShell
      pageKey="geo-sync-logs"
      title="Nhật ký đồng bộ địa giới"
      subtitle="Đọc chi tiết từng lần đồng bộ để debug và theo dõi vận hành."
      showTenantSelector={false}
      scopeHint="Nhật ký đồng bộ địa giới là log vận hành dùng chung toàn hệ thống."
      error={error}
      notice={notice}
      actions={(
        <button onClick={loadLogs} className="px-5 py-3 bg-white text-slate-700 rounded-2xl font-black text-xs uppercase tracking-widest flex items-center gap-2 border border-slate-100 shadow-sm">
          <RefreshCw size={14} /> Tải lại
        </button>
      )}
    >
      <div className="grid grid-cols-1 xl:grid-cols-[1.15fr,0.85fr] gap-6">
        <div className="space-y-4">
          <div className="bg-white rounded-2xl p-4 shadow-sm border border-slate-100 flex flex-wrap gap-3">
            <select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)} className="bg-slate-50 rounded-xl border border-slate-100 px-4 py-3 text-sm font-bold text-slate-700 outline-none">
              <option value="all">Tất cả trạng thái</option>
              <option value="success">Thành công</option>
              <option value="failed">Thất bại</option>
            </select>
          </div>

          <div className="bg-white rounded-[2.5rem] shadow-sm border border-slate-100 overflow-hidden">
            <div className="hidden md:grid grid-cols-12 gap-4 px-6 py-4 border-b border-slate-100 bg-slate-50/70 text-[10px] font-black uppercase tracking-widest text-slate-400">
              <div className="col-span-3">Thời điểm</div>
              <div className="col-span-2">Trạng thái</div>
              <div className="col-span-2">Độ sâu</div>
              <div className="col-span-3">Kết quả</div>
              <div className="col-span-2">Chi tiết</div>
            </div>
            <div className="divide-y divide-slate-50">
              {loading ? (
                <div className="px-6 py-8 text-sm font-bold text-slate-400">Đang tải nhật ký...</div>
              ) : logs.length === 0 ? (
                <div className="px-6 py-8 text-sm font-bold text-slate-400">Chưa có nhật ký đồng bộ địa giới nào.</div>
              ) : logs.map((item) => (
                <div key={item.id} className="grid grid-cols-2 md:grid-cols-12 gap-4 px-6 py-5 items-center hover:bg-slate-50/70 transition-all">
                  <div className="col-span-2 md:col-span-3">
                    <p className="font-black text-slate-900">{formatDateTime(item.createdAt)}</p>
                    <p className="text-xs font-bold text-slate-400 mt-1">HTTP {item.httpStatus || 'N/A'}</p>
                  </div>
                  <div className="col-span-1 md:col-span-2">
                    <span className={`px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest ${getGeoLogStatusClass(item)}`}>
                      {getGeoLogStatusLabel(item)}
                    </span>
                  </div>
                  <div className="col-span-1 md:col-span-2 text-sm font-bold text-slate-600">Cấp {item.depth}</div>
                  <div className="col-span-1 md:col-span-3 text-xs font-medium text-slate-500">
                    Tỉnh {Number(item.provincesInserted || 0) + Number(item.provincesUpdated || 0)} | Huyện {Number(item.districtsInserted || 0) + Number(item.districtsUpdated || 0)} | Xã {Number(item.wardsInserted || 0) + Number(item.wardsUpdated || 0)}
                  </div>
                  <div className="col-span-2 md:col-span-2">
                    <button onClick={() => loadDetail(item.id)} className="px-4 py-2 rounded-xl bg-slate-100 text-slate-700 text-[10px] font-black uppercase tracking-widest hover:bg-slate-900 hover:text-white transition-all">
                      Xem chi tiết
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>

        <div className="bg-white rounded-[2.5rem] shadow-sm border border-slate-100 p-6">
          <h3 className="text-xl font-black text-slate-900">Chi tiết nhật ký</h3>
          {detailLoading ? (
            <div className="mt-4 text-sm font-bold text-slate-400">Đang tải chi tiết...</div>
          ) : !selectedLog ? (
            <div className="mt-4 text-sm font-bold text-slate-400">Chọn một nhật ký bên trái để xem chi tiết.</div>
          ) : (
            <div className="space-y-4 mt-5">
              <div className="flex items-center justify-between gap-3">
                <div>
                  <p className="font-black text-slate-900">{selectedLog.source}</p>
                  <p className="text-xs font-medium text-slate-400 mt-1">{formatDateTime(selectedLog.createdAt)}</p>
                </div>
                <span className={`px-3 py-1 rounded-xl text-[10px] font-black uppercase tracking-widest ${getGeoLogStatusClass(selectedLog)}`}>
                  {getGeoLogStatusLabel(selectedLog)}
                </span>
              </div>
              <div className="rounded-2xl bg-slate-50 px-5 py-4 border border-slate-100">
                <p className="text-[10px] font-black uppercase tracking-widest text-slate-400">URL</p>
                <p className="text-sm font-medium text-slate-700 mt-2 break-all">{selectedLog.url}</p>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div className="rounded-2xl bg-slate-50 px-5 py-4 border border-slate-100">
                  <p className="text-[10px] font-black uppercase tracking-widest text-slate-400">Độ sâu</p>
                  <p className="text-sm font-bold text-slate-900 mt-2">{selectedLog.depth}</p>
                </div>
                <div className="rounded-2xl bg-slate-50 px-5 py-4 border border-slate-100">
                  <p className="text-[10px] font-black uppercase tracking-widest text-slate-400">Trạng thái HTTP</p>
                  <p className="text-sm font-bold text-slate-900 mt-2">{selectedLog.httpStatus || 'N/A'}</p>
                </div>
              </div>
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <div className="rounded-2xl bg-slate-50 px-5 py-4 border border-slate-100">
                  <p className="text-[10px] font-black uppercase tracking-widest text-slate-400">Tỉnh/thành</p>
                  <p className="text-sm font-bold text-slate-900 mt-2">{Number(selectedLog.provincesInserted || 0) + Number(selectedLog.provincesUpdated || 0)}</p>
                </div>
                <div className="rounded-2xl bg-slate-50 px-5 py-4 border border-slate-100">
                  <p className="text-[10px] font-black uppercase tracking-widest text-slate-400">Quận/huyện</p>
                  <p className="text-sm font-bold text-slate-900 mt-2">{Number(selectedLog.districtsInserted || 0) + Number(selectedLog.districtsUpdated || 0)}</p>
                </div>
                <div className="rounded-2xl bg-slate-50 px-5 py-4 border border-slate-100">
                  <p className="text-[10px] font-black uppercase tracking-widest text-slate-400">Phường/xã</p>
                  <p className="text-sm font-bold text-slate-900 mt-2">{Number(selectedLog.wardsInserted || 0) + Number(selectedLog.wardsUpdated || 0)}</p>
                </div>
              </div>
              <div className="rounded-2xl bg-slate-50 px-5 py-4 border border-slate-100">
                <p className="text-[10px] font-black uppercase tracking-widest text-slate-400">Chi tiết lỗi</p>
                <p className="text-sm font-medium text-slate-700 mt-2 whitespace-pre-wrap break-words">{selectedLog.errorDetail || selectedLog.errorMessage || 'Không có lỗi.'}</p>
              </div>
            </div>
          )}
        </div>
      </div>
    </MasterDataPageShell>
  );
};

export default AdminGeoSyncLogsPage;
