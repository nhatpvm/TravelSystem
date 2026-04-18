import React, { useEffect, useMemo, useState } from 'react';
import {
  Search,
  Filter,
  ChevronRight,
  ShieldAlert,
  ShieldCheck,
  Mail,
  Building2,
  Lock,
  Unlock,
  RefreshCw,
  Clock,
  CheckCircle2,
  XCircle,
} from 'lucide-react';
import {
  listAdminTenantOnboarding,
  listAdminTenants,
  reviewAdminTenantOnboarding,
  updateAdminTenant,
} from '../../../services/adminIdentity';

function formatDate(value) {
  if (!value) {
    return 'Chưa có dữ liệu';
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return 'Chưa có dữ liệu';
  }

  return new Intl.DateTimeFormat('vi-VN').format(date);
}

function mapTenantTypeToValue(type) {
  switch (type) {
    case 'Bus':
      return 1;
    case 'Train':
      return 2;
    case 'Flight':
      return 3;
    case 'Tour':
      return 4;
    case 'Hotel':
      return 5;
    default:
      return 99;
  }
}

function mapTenantStatusToValue(status) {
  switch (status) {
    case 'Active':
      return 1;
    case 'Suspended':
      return 2;
    case 'Closed':
      return 3;
    default:
      return 1;
  }
}

function nextTenantStatus(status) {
  return status === 'Active' ? 'Suspended' : 'Active';
}

function getTenantStatusClass(status) {
  if (status === 'Active') {
    return 'bg-green-100 text-green-600';
  }

  if (status === 'Suspended') {
    return 'bg-red-100 text-red-600';
  }

  return 'bg-amber-100 text-amber-600';
}

function getOnboardingStatusClass(status) {
  if (status === 'Approved') {
    return 'bg-green-100 text-green-600';
  }

  if (status === 'Rejected') {
    return 'bg-red-100 text-red-600';
  }

  if (status === 'NeedsMoreInfo') {
    return 'bg-sky-100 text-sky-600';
  }

  return 'bg-amber-100 text-amber-600';
}

const AdminTenantsPage = () => {
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [tenants, setTenants] = useState([]);
  const [onboardingItems, setOnboardingItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  useEffect(() => {
    loadData();
  }, [search, statusFilter]);

  const stats = useMemo(() => ({
    totalPartners: tenants.length,
    verifiedPartners: tenants.filter((tenant) => tenant.status === 'Active' && !tenant.isDeleted).length,
    pendingOnboarding: onboardingItems.filter((item) => item.status === 'PendingReview').length,
    lockedPartners: tenants.filter((tenant) => tenant.status !== 'Active' || tenant.isDeleted).length,
  }), [onboardingItems, tenants]);

  async function loadData() {
    setLoading(true);
    setError('');

    try {
      const [tenantResponse, onboardingResponse] = await Promise.all([
        listAdminTenants({
          q: search,
          status: statusFilter === 'all' ? undefined : statusFilter,
          page: 1,
          pageSize: 100,
        }),
        listAdminTenantOnboarding({ q: search }),
      ]);

      setTenants(tenantResponse.items || []);
      setOnboardingItems(onboardingResponse.items || []);
    } catch (err) {
      setError(err.message || 'Không thể tải dữ liệu đối tác.');
      setTenants([]);
      setOnboardingItems([]);
    } finally {
      setLoading(false);
    }
  }

  async function handleToggleTenantStatus(tenant) {
    setSaving(true);
    setError('');
    setNotice('');

    try {
      await updateAdminTenant(tenant.id, {
        code: tenant.code,
        name: tenant.name,
        type: mapTenantTypeToValue(tenant.type),
        status: mapTenantStatusToValue(nextTenantStatus(tenant.status)),
        holdMinutes: tenant.holdMinutes,
      });

      setNotice(tenant.status === 'Active' ? 'Đối tác đã được tạm khóa.' : 'Đối tác đã được kích hoạt lại.');
      await loadData();
    } catch (err) {
      setError(err.message || 'Không thể cập nhật trạng thái đối tác.');
    } finally {
      setSaving(false);
    }
  }

  async function handleReviewOnboarding(item, status) {
    setSaving(true);
    setError('');
    setNotice('');

    try {
      await reviewAdminTenantOnboarding(item.trackingCode, { status });
      setNotice(status === 'Approved' ? 'Hồ sơ đã được duyệt.' : 'Hồ sơ đã được cập nhật trạng thái.');
      await loadData();
    } catch (err) {
      setError(err.message || 'Không thể xử lý hồ sơ onboarding.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="p-8 space-y-8">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-6">
        <div>
          <h1 className="text-3xl font-black text-slate-900 tracking-tight">Quản lý Đối tác & User</h1>
          <p className="text-slate-500 font-medium mt-1">Giám sát tài khoản, phân quyền và phê duyệt đối tác mới</p>
        </div>
        <div className="flex items-center gap-3">
          <button onClick={loadData} className="px-8 py-3 bg-slate-900 text-white rounded-2xl font-black flex items-center gap-2 shadow-xl hover:bg-blue-600 transition-all">
            <RefreshCw size={18} /> Tải lại dữ liệu
          </button>
        </div>
      </div>

      {notice && (
        <div className="rounded-2xl border border-emerald-100 bg-emerald-50 px-5 py-4 text-sm font-bold text-emerald-700">
          {notice}
        </div>
      )}

      {error && (
        <div className="rounded-2xl border border-rose-100 bg-rose-50 px-5 py-4 text-sm font-bold text-rose-600">
          {error}
        </div>
      )}

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div className="bg-blue-600 rounded-[2.5rem] p-8 text-white shadow-xl shadow-blue-500/20 relative overflow-hidden">
          <div className="absolute top-0 right-0 w-32 h-32 bg-white/10 rounded-full -mr-16 -mt-16"></div>
          <p className="text-[10px] font-black uppercase tracking-widest opacity-60">Tổng số đối tác</p>
          <p className="text-4xl font-black mt-2">{stats.totalPartners}</p>
          <div className="mt-8 flex items-center gap-2 text-[10px] font-bold bg-white/10 w-fit px-3 py-1 rounded-full">
            <ShieldCheck size={12} /> {stats.verifiedPartners} đã xác thực
          </div>
        </div>
        <div className="bg-white rounded-[2.5rem] p-8 border border-slate-100 shadow-sm relative overflow-hidden">
          <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest">Đang chờ phê duyệt</p>
          <p className="text-4xl font-black text-slate-900 mt-2">{stats.pendingOnboarding}</p>
          <div className="mt-8 flex items-center gap-2 text-[10px] font-bold text-amber-600 bg-amber-50 w-fit px-3 py-1 rounded-full">
            <Clock size={12} /> Cần xử lý ngay
          </div>
        </div>
        <div className="bg-white rounded-[2.5rem] p-8 border border-slate-100 shadow-sm relative overflow-hidden">
          <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest">Tài khoản bị khóa</p>
          <p className="text-4xl font-black text-slate-900 mt-2">{stats.lockedPartners}</p>
          <div className="mt-8 flex items-center gap-2 text-[10px] font-bold text-red-600 bg-red-50 w-fit px-3 py-1 rounded-full">
            <ShieldAlert size={12} /> Cần theo dõi
          </div>
        </div>
      </div>

      <div className="bg-white rounded-[3rem] shadow-sm border border-slate-100 overflow-hidden">
        <div className="p-8 border-b border-slate-50 flex flex-col md:flex-row gap-6 items-center justify-between">
          <div className="flex items-center gap-4 bg-slate-50 px-5 py-3 rounded-2xl w-full md:w-96 border border-transparent focus-within:border-blue-200 transition-all">
            <Search size={20} className="text-slate-300" />
            <input value={search} onChange={(event) => setSearch(event.target.value)} type="text" placeholder="Tìm tên đối tác, email, mã số thuế..." className="bg-transparent border-none focus:outline-none text-sm w-full font-medium" />
          </div>
          <div className="flex items-center gap-3 w-full md:w-auto">
            <div className="flex items-center justify-center gap-2 px-6 py-3 bg-slate-50 text-slate-600 rounded-2xl font-bold text-xs transition-all">
              <Filter size={16} />
              <select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)} className="bg-transparent outline-none cursor-pointer">
                <option value="all">Tất cả trạng thái</option>
                <option value="Active">Đang hoạt động</option>
                <option value="Suspended">Tạm khóa</option>
                <option value="Closed">Đã đóng</option>
              </select>
            </div>
          </div>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-slate-50/50">
                <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Thông tin Đối tác</th>
                <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Loại hình</th>
                <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Ngày tham gia</th>
                <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Trạng thái</th>
                <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Thao tác nhanh</th>
                <th className="px-8 py-5"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-50">
              {loading ? (
                <tr>
                  <td colSpan={6} className="px-8 py-6 text-sm font-bold text-slate-400">Đang tải danh sách đối tác...</td>
                </tr>
              ) : tenants.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-8 py-6 text-sm font-bold text-slate-400">Chưa có đối tác phù hợp.</td>
                </tr>
              ) : tenants.map((tenant) => (
                <tr key={tenant.id} className="hover:bg-slate-50/30 transition-all group">
                  <td className="px-8 py-6">
                    <div className="flex items-center gap-4">
                      <div className="w-12 h-12 bg-slate-100 rounded-2xl flex items-center justify-center shrink-0">
                        <Building2 size={24} className="text-slate-400" />
                      </div>
                      <div>
                        <p className="font-black text-slate-900">{tenant.name}</p>
                        <div className="flex items-center gap-3 mt-1 flex-wrap">
                          <span className="text-[10px] text-slate-400 font-bold flex items-center gap-1"><Mail size={10} /> {tenant.ownerEmail || tenant.code}</span>
                          <span className="text-[10px] text-blue-600 font-bold bg-blue-50 px-2 py-0.5 rounded-md">{tenant.usersCount} Users</span>
                        </div>
                      </div>
                    </div>
                  </td>
                  <td className="px-8 py-6">
                    <span className="text-xs font-bold text-slate-700">{tenant.type}</span>
                  </td>
                  <td className="px-8 py-6">
                    <span className="text-xs font-medium text-slate-500">{formatDate(tenant.createdAt)}</span>
                  </td>
                  <td className="px-8 py-6">
                    <span className={`px-3 py-1 rounded-lg text-[10px] font-black uppercase tracking-widest ${getTenantStatusClass(tenant.status)}`}>
                      {tenant.status}
                    </span>
                  </td>
                  <td className="px-8 py-6">
                    <div className="flex items-center gap-2">
                      <button onClick={loadData} className="p-2 bg-slate-50 text-slate-400 hover:text-blue-600 hover:bg-blue-50 rounded-xl transition-all" title="Làm mới danh sách"><RefreshCw size={16} /></button>
                      <button onClick={() => handleToggleTenantStatus(tenant)} disabled={saving} className={`p-2 bg-slate-50 rounded-xl transition-all ${tenant.status === 'Active' ? 'text-slate-400 hover:text-red-600 hover:bg-red-50' : 'text-slate-400 hover:text-green-600 hover:bg-green-50'}`} title={tenant.status === 'Active' ? 'Tạm khóa đối tác' : 'Kích hoạt lại đối tác'}>
                        {tenant.status === 'Active' ? <Lock size={16} /> : <Unlock size={16} />}
                      </button>
                    </div>
                  </td>
                  <td className="px-8 py-6 text-right">
                    <button className="p-2 text-slate-200 hover:text-slate-900"><ChevronRight size={20} /></button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      <div className="bg-white rounded-[3rem] shadow-sm border border-slate-100 overflow-hidden">
        <div className="p-8 border-b border-slate-50">
          <h2 className="text-2xl font-black text-slate-900 tracking-tight">Hồ sơ onboarding</h2>
          <p className="text-slate-500 font-medium mt-1">Duyệt hoặc yêu cầu bổ sung hồ sơ đăng ký đối tác mới</p>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-slate-50/50">
                <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Đơn vị đăng ký</th>
                <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Loại hình</th>
                <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Ngày gửi</th>
                <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Trạng thái</th>
                <th className="px-8 py-5 text-[10px] font-black text-slate-400 uppercase tracking-widest">Xử lý</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-50">
              {loading ? (
                <tr>
                  <td colSpan={5} className="px-8 py-6 text-sm font-bold text-slate-400">Đang tải hồ sơ onboarding...</td>
                </tr>
              ) : onboardingItems.length === 0 ? (
                <tr>
                  <td colSpan={5} className="px-8 py-6 text-sm font-bold text-slate-400">Chưa có hồ sơ onboarding cần xử lý.</td>
                </tr>
              ) : onboardingItems.map((item) => (
                <tr key={item.trackingCode} className="hover:bg-slate-50/30 transition-all">
                  <td className="px-8 py-6">
                    <div>
                      <p className="font-black text-slate-900">{item.businessName}</p>
                      <div className="flex flex-wrap gap-3 mt-1">
                        <span className="text-[10px] text-slate-400 font-bold">{item.trackingCode}</span>
                        {item.contactEmail && <span className="text-[10px] text-slate-400 font-bold">{item.contactEmail}</span>}
                      </div>
                    </div>
                  </td>
                  <td className="px-8 py-6 text-xs font-bold text-slate-700 uppercase">{item.serviceType}</td>
                  <td className="px-8 py-6 text-xs font-medium text-slate-500">{formatDate(item.submittedAt)}</td>
                  <td className="px-8 py-6">
                    <span className={`px-3 py-1 rounded-lg text-[10px] font-black uppercase tracking-widest ${getOnboardingStatusClass(item.status)}`}>
                      {item.status}
                    </span>
                  </td>
                  <td className="px-8 py-6">
                    <div className="flex items-center gap-2">
                      <button onClick={() => handleReviewOnboarding(item, 'Approved')} disabled={saving} className="p-2 bg-slate-50 text-slate-400 hover:text-green-600 hover:bg-green-50 rounded-xl transition-all" title="Duyệt hồ sơ">
                        <CheckCircle2 size={16} />
                      </button>
                      <button onClick={() => handleReviewOnboarding(item, 'NeedsMoreInfo')} disabled={saving} className="p-2 bg-slate-50 text-slate-400 hover:text-sky-600 hover:bg-sky-50 rounded-xl transition-all" title="Yêu cầu bổ sung">
                        <RefreshCw size={16} />
                      </button>
                      <button onClick={() => handleReviewOnboarding(item, 'Rejected')} disabled={saving} className="p-2 bg-slate-50 text-slate-400 hover:text-red-600 hover:bg-red-50 rounded-xl transition-all" title="Từ chối hồ sơ">
                        <XCircle size={16} />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};

export default AdminTenantsPage;
