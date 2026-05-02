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
  getAdminTenant,
  getAdminTenantOnboarding,
  listAdminTenantOnboarding,
  listAdminTenants,
  provisionAdminTenantOnboarding,
  reviewAdminTenantOnboarding,
  updateAdminTenant,
} from '../../../services/adminIdentity';
import { useSearchParams } from 'react-router-dom';
import useLatestRef from '../../../shared/hooks/useLatestRef';

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

function buildTenantCode(item) {
  const serviceType = String(item?.serviceType || '').toUpperCase();
  const prefix = serviceType === 'BUS'
    ? 'NX'
    : serviceType === 'TRAIN'
      ? 'VT'
      : serviceType === 'FLIGHT'
        ? 'VMM'
        : serviceType === 'HOTEL'
          ? 'KS'
          : serviceType === 'TOUR'
            ? 'TOUR'
            : 'TN';
  const seed = String(item?.taxCode || item?.trackingCode || Date.now())
    .replace(/[^0-9A-Za-z]/g, '')
    .slice(-4)
    .toUpperCase();

  return `${prefix}${seed || '001'}`;
}

function buildInitialOwnerPassword() {
  const suffix = Math.random().toString(36).slice(2, 8).toUpperCase();
  return `Tenant@${suffix}1`;
}

function buildProvisionForm(item) {
  return {
    tenantCode: buildTenantCode(item),
    tenantName: item?.businessName || '',
    serviceType: String(item?.serviceType || 'hotel').toLowerCase(),
    holdMinutes: '5',
    ownerEmail: item?.contactEmail || '',
    ownerFullName: item?.businessName || '',
    ownerPhone: item?.contactPhone || '',
    initialPassword: buildInitialOwnerPassword(),
  };
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

function getTenantStatusLabel(status) {
  if (status === 'Active') return 'Đang hoạt động';
  if (status === 'Suspended') return 'Tạm khóa';
  if (status === 'Closed') return 'Đã đóng';
  return status || 'Chưa xác định';
}

function getOnboardingStatusLabel(status) {
  if (status === 'Approved') return 'Đã duyệt';
  if (status === 'Rejected') return 'Từ chối';
  if (status === 'NeedsMoreInfo') return 'Cần bổ sung';
  if (status === 'PendingReview') return 'Chờ duyệt';
  return status || 'Chưa xác định';
}

function getServiceTypeLabel(value) {
  const normalized = String(value || '').toLowerCase();
  if (normalized === 'bus') return 'Nhà xe';
  if (normalized === 'train') return 'Đường sắt';
  if (normalized === 'flight') return 'Hàng không';
  if (normalized === 'hotel') return 'Khách sạn';
  if (normalized === 'tour') return 'Tour';
  return value || 'Chưa có dữ liệu';
}

const AdminTenantsPage = () => {
  const [searchParams] = useSearchParams();
  const [search, setSearch] = useState(() => searchParams.get('q') || '');
  const [statusFilter, setStatusFilter] = useState('all');
  const [tenants, setTenants] = useState([]);
  const [onboardingItems, setOnboardingItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [detailLoading, setDetailLoading] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [selectedTenant, setSelectedTenant] = useState(null);
  const [selectedOnboarding, setSelectedOnboarding] = useState(null);
  const [reviewForm, setReviewForm] = useState({
    status: 'Approved',
    reviewNote: '',
    rejectReason: '',
    needMoreInfoReason: '',
  });
  const [provisionForm, setProvisionForm] = useState(() => buildProvisionForm(null));

  const loadDataRef = useLatestRef(loadData);

  useEffect(() => {
    loadDataRef.current();
  }, [loadDataRef, search, statusFilter]);

  useEffect(() => {
    setSearch(searchParams.get('q') || '');
  }, [searchParams]);

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
      await loadDataRef.current();
    } catch (err) {
      setError(err.message || 'Không thể cập nhật trạng thái đối tác.');
    } finally {
      setSaving(false);
    }
  }

  async function handleReviewOnboarding(item, status) {
    await handleOpenOnboardingDetail(item);
    setReviewForm((current) => ({ ...current, status }));
  }

  async function handleOpenTenantDetail(tenant) {
    setDetailLoading(true);
    setError('');
    setSelectedOnboarding(null);

    try {
      const detail = await getAdminTenant(tenant.id);
      setSelectedTenant(detail);
    } catch (err) {
      setError(err.message || 'Không thể tải chi tiết đối tác.');
    } finally {
      setDetailLoading(false);
    }
  }

  async function handleOpenOnboardingDetail(item) {
    setDetailLoading(true);
    setError('');
    setSelectedTenant(null);

    try {
      const detail = await getAdminTenantOnboarding(item.trackingCode);
      const nextDetail = detail || item;
      setSelectedOnboarding(nextDetail);
      setReviewForm({
        status: nextDetail.status === 'Rejected' || nextDetail.status === 'NeedsMoreInfo' ? nextDetail.status : 'Approved',
        reviewNote: nextDetail.reviewNote || nextDetail.reviewerNote || '',
        rejectReason: nextDetail.rejectReason || '',
        needMoreInfoReason: nextDetail.needMoreInfoReason || '',
      });
      setProvisionForm(buildProvisionForm(nextDetail));
    } catch (err) {
      setError(err.message || 'Không thể tải chi tiết hồ sơ onboarding.');
    } finally {
      setDetailLoading(false);
    }
  }

  async function handleProvisionTenant(event) {
    event.preventDefault();

    if (!selectedOnboarding) {
      return;
    }

    if (!provisionForm.tenantCode.trim() || !provisionForm.tenantName.trim() || !provisionForm.ownerEmail.trim()) {
      setError('Vui lòng nhập đầy đủ mã tenant, tên tenant và email owner.');
      return;
    }

    setSaving(true);
    setError('');
    setNotice('');

    try {
      if (selectedOnboarding.status !== 'Approved') {
        await reviewAdminTenantOnboarding(selectedOnboarding.trackingCode, {
          status: 'Approved',
          reviewNote: reviewForm.reviewNote || 'Admin duyệt tự động khi cấp tenant.',
          rejectReason: null,
          needMoreInfoReason: null,
        });
      }

      const response = await provisionAdminTenantOnboarding(selectedOnboarding.trackingCode, {
        tenantCode: provisionForm.tenantCode,
        tenantName: provisionForm.tenantName,
        serviceType: provisionForm.serviceType,
        holdMinutes: Number(provisionForm.holdMinutes) || 5,
        ownerEmail: provisionForm.ownerEmail,
        ownerFullName: provisionForm.ownerFullName,
        ownerPhone: provisionForm.ownerPhone,
        initialPassword: provisionForm.initialPassword,
      });

      const refreshedDetail = await getAdminTenantOnboarding(selectedOnboarding.trackingCode);
      setSelectedOnboarding(refreshedDetail || response.onboarding || selectedOnboarding);
      setNotice(response.alreadyProvisioned ? 'Hồ sơ này đã được cấp tenant trước đó.' : 'Đã duyệt hồ sơ, tạo tenant, owner và quyền mặc định cho đối tác.');
      await loadDataRef.current();
    } catch (err) {
      setError(err.message || 'Không thể tạo tenant từ hồ sơ onboarding.');
    } finally {
      setSaving(false);
    }
  }

  async function handleSubmitReview(event) {
    event.preventDefault();

    if (!selectedOnboarding) {
      return;
    }

    if (reviewForm.status === 'Rejected' && !reviewForm.rejectReason.trim()) {
      setError('Vui lòng nhập lý do từ chối hồ sơ.');
      return;
    }

    if (reviewForm.status === 'NeedsMoreInfo' && !reviewForm.needMoreInfoReason.trim()) {
      setError('Vui lòng nhập nội dung cần đối tác bổ sung.');
      return;
    }

    setSaving(true);
    setError('');
    setNotice('');

    try {
      await reviewAdminTenantOnboarding(selectedOnboarding.trackingCode, {
        status: reviewForm.status,
        reviewNote: reviewForm.reviewNote,
        rejectReason: reviewForm.status === 'Rejected' ? reviewForm.rejectReason : null,
        needMoreInfoReason: reviewForm.status === 'NeedsMoreInfo' ? reviewForm.needMoreInfoReason : null,
      });

      const refreshedDetail = await getAdminTenantOnboarding(selectedOnboarding.trackingCode);
      setSelectedOnboarding(refreshedDetail || selectedOnboarding);
      setNotice(reviewForm.status === 'Approved' ? 'Hồ sơ đã được duyệt.' : 'Hồ sơ đã được cập nhật kết quả review.');
      await loadDataRef.current();
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
                    <span className="text-xs font-bold text-slate-700">{getServiceTypeLabel(tenant.type)}</span>
                  </td>
                  <td className="px-8 py-6">
                    <span className="text-xs font-medium text-slate-500">{formatDate(tenant.createdAt)}</span>
                  </td>
                  <td className="px-8 py-6">
                    <span className={`px-3 py-1 rounded-lg text-[10px] font-black uppercase tracking-widest ${getTenantStatusClass(tenant.status)}`}>
                      {getTenantStatusLabel(tenant.status)}
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
                    <button onClick={() => handleOpenTenantDetail(tenant)} disabled={detailLoading} className="p-2 text-slate-200 hover:text-slate-900 disabled:opacity-50" title="Xem chi tiết đối tác">
                      <ChevronRight size={20} />
                    </button>
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
                  <td className="px-8 py-6 text-xs font-bold text-slate-700 uppercase">{getServiceTypeLabel(item.serviceType)}</td>
                  <td className="px-8 py-6 text-xs font-medium text-slate-500">{formatDate(item.submittedAt)}</td>
                  <td className="px-8 py-6">
                    <span className={`px-3 py-1 rounded-lg text-[10px] font-black uppercase tracking-widest ${getOnboardingStatusClass(item.status)}`}>
                      {getOnboardingStatusLabel(item.status)}
                    </span>
                  </td>
                  <td className="px-8 py-6">
                    <div className="flex items-center gap-2">
                      <button onClick={() => handleReviewOnboarding(item, 'Approved')} disabled={saving} className="inline-flex items-center gap-1.5 px-3 py-2 bg-slate-50 text-slate-400 hover:text-green-600 hover:bg-green-50 rounded-xl transition-all" title="Duyệt hồ sơ">
                        <CheckCircle2 size={16} />
                        <span className="text-[10px] font-black">Duyệt</span>
                      </button>
                      <button onClick={() => handleReviewOnboarding(item, 'NeedsMoreInfo')} disabled={saving} className="inline-flex items-center gap-1.5 px-3 py-2 bg-slate-50 text-slate-400 hover:text-sky-600 hover:bg-sky-50 rounded-xl transition-all" title="Yêu cầu bổ sung">
                        <RefreshCw size={16} />
                        <span className="text-[10px] font-black">Bổ sung</span>
                      </button>
                      <button onClick={() => handleReviewOnboarding(item, 'Rejected')} disabled={saving} className="inline-flex items-center gap-1.5 px-3 py-2 bg-slate-50 text-slate-400 hover:text-red-600 hover:bg-red-50 rounded-xl transition-all" title="Từ chối hồ sơ">
                        <XCircle size={16} />
                        <span className="text-[10px] font-black">Từ chối</span>
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {selectedTenant && (
        <div className="bg-white rounded-[3rem] shadow-sm border border-slate-100 overflow-hidden">
          <div className="p-8 border-b border-slate-50 flex flex-col md:flex-row md:items-center justify-between gap-4">
            <div>
              <h2 className="text-2xl font-black text-slate-900 tracking-tight">Chi tiết đối tác</h2>
              <p className="text-slate-500 font-medium mt-1">{selectedTenant.code} - {selectedTenant.name}</p>
            </div>
            <div className="flex items-center gap-3">
              <button onClick={() => handleToggleTenantStatus(selectedTenant)} disabled={saving} className="px-5 py-3 bg-slate-900 text-white rounded-2xl font-black text-xs hover:bg-blue-600 transition-all disabled:opacity-60">
                {selectedTenant.status === 'Active' ? 'Tạm khóa đối tác' : 'Kích hoạt đối tác'}
              </button>
              <button onClick={() => setSelectedTenant(null)} className="px-5 py-3 bg-slate-50 text-slate-600 rounded-2xl font-black text-xs hover:bg-slate-100 transition-all">
                Đóng
              </button>
            </div>
          </div>
          <div className="p-8 grid grid-cols-1 md:grid-cols-3 gap-4">
            <DetailCell label="Mã tenant" value={selectedTenant.code} />
            <DetailCell label="Tên đối tác" value={selectedTenant.name} />
            <DetailCell label="Loại hình" value={getServiceTypeLabel(selectedTenant.type)} />
            <DetailCell label="Trạng thái" value={getTenantStatusLabel(selectedTenant.status)} />
            <DetailCell label="Thời gian giữ chỗ mặc định (phút)" value={selectedTenant.holdMinutes} />
            <DetailCell label="Số user" value={selectedTenant.usersCount} />
            <DetailCell label="Owner" value={selectedTenant.ownerName || 'Chưa có dữ liệu'} />
            <DetailCell label="Owner email" value={selectedTenant.ownerEmail || 'Chưa có dữ liệu'} />
            <DetailCell label="Số role tenant" value={selectedTenant.tenantRolesCount} />
            <DetailCell label="Ngày tạo" value={formatDate(selectedTenant.createdAt)} />
            <DetailCell label="Cập nhật" value={formatDate(selectedTenant.updatedAt)} />
            <DetailCell label="Đã xóa mềm" value={selectedTenant.isDeleted ? 'Có' : 'Không'} />
          </div>
        </div>
      )}

      {selectedOnboarding && (
        <div className="bg-white rounded-[3rem] shadow-sm border border-slate-100 overflow-hidden">
          <div className="p-8 border-b border-slate-50 flex flex-col md:flex-row md:items-center justify-between gap-4">
            <div>
              <h2 className="text-2xl font-black text-slate-900 tracking-tight">Chi tiết hồ sơ onboarding</h2>
              <p className="text-slate-500 font-medium mt-1">{selectedOnboarding.trackingCode} - {selectedOnboarding.businessName}</p>
            </div>
            <button onClick={() => setSelectedOnboarding(null)} className="px-5 py-3 bg-slate-50 text-slate-600 rounded-2xl font-black text-xs hover:bg-slate-100 transition-all">
              Đóng
            </button>
          </div>

          <div className="p-8 grid grid-cols-1 lg:grid-cols-[1fr_1fr] gap-8">
            <div className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <DetailCell label="Đơn vị đăng ký" value={selectedOnboarding.businessName} />
                <DetailCell label="Loại hình" value={getServiceTypeLabel(selectedOnboarding.serviceType)} />
                <DetailCell label="Mã số thuế" value={selectedOnboarding.taxCode} />
                <DetailCell label="Trạng thái" value={getOnboardingStatusLabel(selectedOnboarding.status)} />
                <DetailCell label="Email liên hệ" value={selectedOnboarding.contactEmail || 'Chưa có dữ liệu'} />
                <DetailCell label="Điện thoại" value={selectedOnboarding.contactPhone || 'Chưa có dữ liệu'} />
                <DetailCell label="Ngày gửi" value={formatDate(selectedOnboarding.submittedAt)} />
                <DetailCell label="Ngày review" value={formatDate(selectedOnboarding.reviewedAt)} />
              </div>
              <DetailCell label="Địa chỉ" value={selectedOnboarding.address || 'Chưa có dữ liệu'} />
              <DetailCell label="Tài liệu pháp lý" value={selectedOnboarding.legalDocument?.originalFileName || selectedOnboarding.legalDocument?.storedFileName || 'Chưa có dữ liệu'} />
              <DetailCell label="Người review" value={selectedOnboarding.reviewedBy || 'Chưa có dữ liệu'} />
              <DetailCell label="Ghi chú review nội bộ" value={selectedOnboarding.reviewNote || selectedOnboarding.reviewerNote || 'Chưa có dữ liệu'} />
              <DetailCell label="Lý do từ chối gửi đối tác" value={selectedOnboarding.rejectReason || 'Chưa có dữ liệu'} />
              <DetailCell label="Nội dung cần đối tác bổ sung" value={selectedOnboarding.needMoreInfoReason || 'Chưa có dữ liệu'} />
            </div>

            <form onSubmit={handleSubmitReview} className="space-y-4">
              <div>
                <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] mb-2 block">Kết quả review</label>
                <select
                  value={reviewForm.status}
                  onChange={(event) => setReviewForm((current) => ({ ...current, status: event.target.value }))}
                  className="w-full bg-slate-50 border-2 border-transparent focus:border-blue-200 focus:bg-white rounded-2xl px-4 py-3.5 text-sm font-bold text-slate-900 outline-none transition-all"
                >
                  <option value="Approved">Duyệt hồ sơ</option>
                  <option value="NeedsMoreInfo">Yêu cầu bổ sung thông tin</option>
                  <option value="Rejected">Từ chối hồ sơ</option>
                </select>
              </div>

              <div>
                <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] mb-2 block">Ghi chú review nội bộ</label>
                <textarea
                  value={reviewForm.reviewNote}
                  onChange={(event) => setReviewForm((current) => ({ ...current, reviewNote: event.target.value }))}
                  rows={4}
                  placeholder="Ghi lại căn cứ duyệt/xử lý hồ sơ cho admin nội bộ"
                  className="w-full bg-slate-50 border-2 border-transparent focus:border-blue-200 focus:bg-white rounded-2xl px-4 py-3.5 text-sm font-medium text-slate-900 outline-none transition-all resize-none"
                />
              </div>

              {reviewForm.status === 'Rejected' && (
                <div>
                  <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] mb-2 block">Lý do từ chối gửi đối tác</label>
                  <textarea
                    value={reviewForm.rejectReason}
                    onChange={(event) => setReviewForm((current) => ({ ...current, rejectReason: event.target.value }))}
                    rows={4}
                    placeholder="Nêu rõ hồ sơ sai hoặc chưa đạt điều kiện nào"
                    className="w-full bg-rose-50 border-2 border-transparent focus:border-rose-200 focus:bg-white rounded-2xl px-4 py-3.5 text-sm font-medium text-slate-900 outline-none transition-all resize-none"
                  />
                </div>
              )}

              {reviewForm.status === 'NeedsMoreInfo' && (
                <div>
                  <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] mb-2 block">Nội dung cần đối tác bổ sung</label>
                  <textarea
                    value={reviewForm.needMoreInfoReason}
                    onChange={(event) => setReviewForm((current) => ({ ...current, needMoreInfoReason: event.target.value }))}
                    rows={4}
                    placeholder="VD: bổ sung giấy phép kinh doanh, tài khoản ngân hàng, người đại diện"
                    className="w-full bg-sky-50 border-2 border-transparent focus:border-sky-200 focus:bg-white rounded-2xl px-4 py-3.5 text-sm font-medium text-slate-900 outline-none transition-all resize-none"
                  />
                </div>
              )}

              <button type="submit" disabled={saving} className="w-full px-5 py-4 rounded-2xl bg-slate-900 text-white text-sm font-black uppercase tracking-widest disabled:opacity-60 hover:bg-blue-600 transition-all">
                Lưu kết quả review
              </button>
            </form>

            <form onSubmit={handleProvisionTenant} className="space-y-4 rounded-[2rem] border border-slate-100 bg-slate-50/60 p-5 lg:col-start-2">
              <div>
                <p className="text-sm font-black text-slate-900">Cấp tenant chính thức</p>
                <p className="text-xs font-bold text-slate-400 mt-1">Duyệt hồ sơ, tạo tenant, owner, role và quyền mặc định cho đối tác.</p>
              </div>

              {selectedOnboarding.tenantCode ? (
                <div className="rounded-2xl bg-emerald-50 border border-emerald-100 px-4 py-3 text-sm font-bold text-emerald-700">
                  Hồ sơ đã được cấp tenant {selectedOnboarding.tenantCode}.
                </div>
              ) : null}

              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                <label className="block">
                  <span className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] mb-2 block">Mã tenant dùng trong hệ thống</span>
                  <input value={provisionForm.tenantCode} onChange={(event) => setProvisionForm((current) => ({ ...current, tenantCode: event.target.value }))} placeholder="VD: KS0001, NX0001" className="w-full bg-white border-2 border-transparent focus:border-blue-200 rounded-2xl px-4 py-3 text-sm font-bold text-slate-900 outline-none transition-all" />
                </label>
                <label className="block">
                  <span className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] mb-2 block">Lĩnh vực kinh doanh</span>
                  <select value={provisionForm.serviceType} onChange={(event) => setProvisionForm((current) => ({ ...current, serviceType: event.target.value }))} className="w-full bg-white border-2 border-transparent focus:border-blue-200 rounded-2xl px-4 py-3 text-sm font-bold text-slate-900 outline-none transition-all">
                    <option value="bus">Nhà xe</option>
                    <option value="train">Đường sắt</option>
                    <option value="flight">Hàng không</option>
                    <option value="hotel">Khách sạn</option>
                    <option value="tour">Tour</option>
                  </select>
                </label>
              </div>

              <label className="block">
                <span className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] mb-2 block">Tên hiển thị của đối tác</span>
                <input value={provisionForm.tenantName} onChange={(event) => setProvisionForm((current) => ({ ...current, tenantName: event.target.value }))} placeholder="Tên doanh nghiệp sẽ hiển thị trong admin" className="w-full bg-white border-2 border-transparent focus:border-blue-200 rounded-2xl px-4 py-3 text-sm font-bold text-slate-900 outline-none transition-all" />
              </label>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                <label className="block">
                  <span className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] mb-2 block">Email đăng nhập của owner</span>
                  <input value={provisionForm.ownerEmail} onChange={(event) => setProvisionForm((current) => ({ ...current, ownerEmail: event.target.value }))} placeholder="Email để owner đăng nhập" className="w-full bg-white border-2 border-transparent focus:border-blue-200 rounded-2xl px-4 py-3 text-sm font-medium text-slate-900 outline-none transition-all" />
                </label>
                <label className="block">
                  <span className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] mb-2 block">Số điện thoại owner</span>
                  <input value={provisionForm.ownerPhone} onChange={(event) => setProvisionForm((current) => ({ ...current, ownerPhone: event.target.value }))} placeholder="Dùng để liên hệ vận hành" className="w-full bg-white border-2 border-transparent focus:border-blue-200 rounded-2xl px-4 py-3 text-sm font-medium text-slate-900 outline-none transition-all" />
                </label>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                <label className="block">
                  <span className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] mb-2 block">Tên người quản lý chính</span>
                  <input value={provisionForm.ownerFullName} onChange={(event) => setProvisionForm((current) => ({ ...current, ownerFullName: event.target.value }))} placeholder="Tên owner hoặc người đại diện vận hành" className="w-full bg-white border-2 border-transparent focus:border-blue-200 rounded-2xl px-4 py-3 text-sm font-medium text-slate-900 outline-none transition-all" />
                </label>
                <label className="block">
                  <span className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] mb-2 block">Thời gian giữ chỗ mặc định (phút)</span>
                  <input type="number" min="1" max="60" value={provisionForm.holdMinutes} onChange={(event) => setProvisionForm((current) => ({ ...current, holdMinutes: event.target.value }))} placeholder="VD: 5 nghĩa là giữ chỗ 5 phút trước khi hết hạn" className="w-full bg-white border-2 border-transparent focus:border-blue-200 rounded-2xl px-4 py-3 text-sm font-medium text-slate-900 outline-none transition-all" />
                </label>
              </div>

              <label className="block">
                <span className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] mb-2 block">Mật khẩu tạm cho owner</span>
                <input type="text" value={provisionForm.initialPassword} onChange={(event) => setProvisionForm((current) => ({ ...current, initialPassword: event.target.value }))} placeholder="Owner dùng mật khẩu này để đăng nhập lần đầu" className="w-full bg-white border-2 border-transparent focus:border-blue-200 rounded-2xl px-4 py-3 text-sm font-medium text-slate-900 outline-none transition-all" />
                <span className="text-[11px] font-bold text-slate-400 mt-2 block">Nếu email owner đã có tài khoản, hệ thống sẽ gán tenant và đặt lại mật khẩu theo giá trị này.</span>
              </label>

              <button type="submit" disabled={saving || !!selectedOnboarding.tenantCode} className="w-full px-5 py-4 rounded-2xl bg-blue-600 text-white text-sm font-black uppercase tracking-widest disabled:opacity-60 hover:bg-slate-900 transition-all">
                {saving ? 'Đang xử lý...' : selectedOnboarding.status === 'Approved' ? 'Tạo tenant và owner' : 'Duyệt và tạo tenant'}
              </button>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

function DetailCell({ label, value }) {
  return (
    <div className="rounded-2xl bg-slate-50 px-4 py-3">
      <p className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em]">{label}</p>
      <p className="text-sm font-bold text-slate-900 mt-1 break-words">{value ?? 'Chưa có dữ liệu'}</p>
    </div>
  );
}

export default AdminTenantsPage;
