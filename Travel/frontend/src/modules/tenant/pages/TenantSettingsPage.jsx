import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { motion } from 'framer-motion';
import {
  AlertTriangle,
  BadgeCheck,
  Bell,
  Building2,
  ChevronRight,
  CreditCard,
  Globe,
  KeyRound,
  Loader2,
  RefreshCw,
  Save,
  Shield,
} from 'lucide-react';
import {
  changePassword,
  disableTwoFactor,
  enableTwoFactor,
  getTwoFactorStatus,
  listMySessions,
  revokeMySession,
  setupTwoFactor,
} from '../../../services/auth';
import {
  getCurrentTenantSettings,
  updateCurrentTenantSettings,
} from '../../../services/tenancyService';
import {
  getTenantCommerceFinance,
  upsertTenantCommercePayoutAccount,
} from '../../../services/commerceBackofficeService';
import { useAuthSession } from '../../auth/hooks/useAuthSession';
import {
  getCurrentTenantConfig,
  getCurrentTenantMembership,
  getTenantOperatorBadge,
  hasTenantPermission,
} from '../../auth/types';

const TABS = [
  { id: 'general', icon: <Building2 size={16} />, label: 'Thông tin chung' },
  { id: 'booking', icon: <Globe size={16} />, label: 'Cài đặt đặt chỗ' },
  { id: 'notify', icon: <Bell size={16} />, label: 'Thông báo' },
  { id: 'security', icon: <Shield size={16} />, label: 'Bảo mật' },
  { id: 'payment', icon: <CreditCard size={16} />, label: 'Thanh toán' },
];

const MODULE_LABEL = {
  bus: 'Nhà xe',
  train: 'Đường sắt',
  flight: 'Hãng bay',
  hotel: 'Khách sạn',
  tour: 'Tour',
};

const LOCAL_DEFAULTS = {
  autoConfirm: true,
  vatEnabled: false,
  cancellationPolicy: 'full_48h',
  notifications: {
    newBooking: true,
    paymentSuccess: true,
    cancellationRequest: true,
    weeklyReport: true,
    lowInventory: true,
  },
};

const NOTIFICATION_OPTIONS = [
  { key: 'newBooking', label: 'Đơn đặt mới', sub: 'Thông báo khi có đặt chỗ thành công' },
  { key: 'paymentSuccess', label: 'Thanh toán thành công', sub: 'Nhận xác nhận khi thanh toán được xử lý' },
  { key: 'cancellationRequest', label: 'Yêu cầu hủy vé', sub: 'Cảnh báo khi khách yêu cầu hủy' },
  { key: 'weeklyReport', label: 'Báo cáo hằng tuần', sub: 'Tổng kết doanh thu mỗi thứ Hai' },
  { key: 'lowInventory', label: 'Cảnh báo tồn kho thấp', sub: 'Nhắc khi còn <= 5 chỗ, phòng hoặc suất' },
];

function formatDateTime(value) {
  if (!value) {
    return '--';
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return '--';
  }

  return new Intl.DateTimeFormat('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(date);
}

function readLocalSettings(tenantId) {
  if (!tenantId) {
    return LOCAL_DEFAULTS;
  }

  try {
    const raw = window.localStorage.getItem(`tenant_settings:${tenantId}`);
    if (!raw) {
      return LOCAL_DEFAULTS;
    }

    const parsed = JSON.parse(raw);
    return {
      ...LOCAL_DEFAULTS,
      ...parsed,
      notifications: {
        ...LOCAL_DEFAULTS.notifications,
        ...(parsed.notifications || {}),
      },
    };
  } catch {
    return LOCAL_DEFAULTS;
  }
}

function writeLocalSettings(tenantId, value) {
  if (!tenantId) {
    return;
  }

  window.localStorage.setItem(`tenant_settings:${tenantId}`, JSON.stringify(value));
}

function Toggle({ checked, onChange, disabled = false }) {
  return (
    <button
      type="button"
      onClick={() => onChange(!checked)}
      disabled={disabled}
      className={`relative inline-flex h-6 w-12 shrink-0 rounded-full transition ${checked ? 'bg-blue-600' : 'bg-slate-200'} disabled:opacity-60`}
    >
      <span className={`absolute left-0.5 top-0.5 h-5 w-5 rounded-full bg-white shadow transition-transform ${checked ? 'translate-x-6' : ''}`} />
    </button>
  );
}

export default function TenantSettingsPage() {
  const session = useAuthSession();
  const tenantMembership = getCurrentTenantMembership(session);
  const tenantConfig = getCurrentTenantConfig(session);
  const tenantId = tenantMembership?.tenantId || session.currentTenantId;
  const canReadFinance = hasTenantPermission(session, 'tenant.finance.read');
  const canWriteFinance = hasTenantPermission(session, 'tenant.finance.write');

  const [activeTab, setActiveTab] = useState('general');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState('');
  const [error, setError] = useState('');
  const [tenant, setTenant] = useState(null);
  const [membership, setMembership] = useState(null);
  const [generalForm, setGeneralForm] = useState({ name: '', code: '', type: '', status: '' });
  const [bookingForm, setBookingForm] = useState({
    holdMinutes: 5,
    autoConfirm: true,
    vatEnabled: false,
    cancellationPolicy: 'full_48h',
  });
  const [notifications, setNotifications] = useState(LOCAL_DEFAULTS.notifications);
  const [paymentForm, setPaymentForm] = useState({
    bankName: '',
    accountNumber: '',
    accountHolder: '',
    bankBranch: '',
    note: '',
    isVerified: false,
    updatedAt: '',
  });
  const [passwordForm, setPasswordForm] = useState({ currentPassword: '', newPassword: '', confirmPassword: '' });
  const [sessions, setSessions] = useState([]);
  const [twoFactor, setTwoFactor] = useState(null);
  const [twoFactorSetup, setTwoFactorSetup] = useState(null);
  const [twoFactorCode, setTwoFactorCode] = useState('');

  const loadSettings = useCallback(async () => {
    if (!session.isReady) {
      return;
    }

    const [tenantResult, financeResult, sessionsResult, twoFactorResult] = await Promise.all([
      getCurrentTenantSettings().then((data) => ({ data })).catch((requestError) => ({ error: requestError })),
      canReadFinance
        ? getTenantCommerceFinance().then((data) => ({ data })).catch((requestError) => ({ error: requestError }))
        : Promise.resolve({ data: null }),
      listMySessions().then((data) => ({ data })).catch(() => ({ data: null })),
      getTwoFactorStatus().then((data) => ({ data })).catch(() => ({ data: null })),
    ]);

    if (tenantResult.error) {
      setError(tenantResult.error.message || 'Không thể tải cài đặt tenant.');
    } else {
      const nextTenant = tenantResult.data?.tenant || {};
      const local = readLocalSettings(nextTenant.id || tenantId);

      setTenant(nextTenant);
      setMembership(tenantResult.data?.membership || null);
      setGeneralForm({
        name: nextTenant.name || tenantMembership?.name || '',
        code: nextTenant.code || tenantMembership?.code || '',
        type: nextTenant.type || tenantMembership?.type || '',
        status: nextTenant.status || tenantMembership?.status || '',
      });
      setBookingForm({
        holdMinutes: Number(nextTenant.holdMinutes || local.holdMinutes || 5),
        autoConfirm: Boolean(local.autoConfirm),
        vatEnabled: Boolean(local.vatEnabled),
        cancellationPolicy: local.cancellationPolicy || 'full_48h',
      });
      setNotifications(local.notifications || LOCAL_DEFAULTS.notifications);
    }

    const payout = financeResult.data?.payoutAccount;
    setPaymentForm({
      bankName: payout?.bankName || '',
      accountNumber: payout?.accountNumber || '',
      accountHolder: payout?.accountHolder || '',
      bankBranch: payout?.bankBranch || '',
      note: payout?.note || '',
      isVerified: !!payout?.isVerified,
      updatedAt: payout?.updatedAt || '',
    });

    setSessions(Array.isArray(sessionsResult.data?.items) ? sessionsResult.data.items : []);
    setTwoFactor(twoFactorResult.data || null);
    setLoading(false);
  }, [canReadFinance, session.isReady, tenantId, tenantMembership]);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      loadSettings();
    }, 0);

    return () => window.clearTimeout(timer);
  }, [loadSettings]);

  const moduleLabel = useMemo(
    () => MODULE_LABEL[tenantConfig?.module] || generalForm.type || 'Tenant',
    [generalForm.type, tenantConfig?.module],
  );

  function flashSaved(message = 'Đã lưu thay đổi.') {
    setSaved(message);
    window.setTimeout(() => setSaved(''), 2500);
  }

  async function handleSave() {
    setSaving(true);
    setError('');

    try {
      if (activeTab === 'general' || activeTab === 'booking') {
        const updatedTenant = await updateCurrentTenantSettings({
          name: generalForm.name,
          holdMinutes: Number(bookingForm.holdMinutes || 5),
        });

        setTenant(updatedTenant);
        setGeneralForm((current) => ({
          ...current,
          name: updatedTenant.name || current.name,
          code: updatedTenant.code || current.code,
          type: updatedTenant.type || current.type,
          status: updatedTenant.status || current.status,
        }));

        const local = {
          ...readLocalSettings(updatedTenant.id || tenant?.id || tenantId),
          autoConfirm: bookingForm.autoConfirm,
          vatEnabled: bookingForm.vatEnabled,
          cancellationPolicy: bookingForm.cancellationPolicy,
          notifications,
        };
        writeLocalSettings(updatedTenant.id || tenant?.id || tenantId, local);
        flashSaved('Đã lưu cài đặt tenant.');
      } else if (activeTab === 'notify') {
        const local = {
          ...readLocalSettings(tenant?.id || tenantId),
          autoConfirm: bookingForm.autoConfirm,
          vatEnabled: bookingForm.vatEnabled,
          cancellationPolicy: bookingForm.cancellationPolicy,
          notifications,
        };
        writeLocalSettings(tenant?.id || tenantId, local);
        flashSaved('Đã lưu tùy chọn thông báo trên trình duyệt này.');
      } else if (activeTab === 'payment') {
        const response = await upsertTenantCommercePayoutAccount({
          bankName: paymentForm.bankName,
          accountNumber: paymentForm.accountNumber,
          accountHolder: paymentForm.accountHolder,
          bankBranch: paymentForm.bankBranch || undefined,
          note: paymentForm.note || undefined,
        });

        setPaymentForm({
          bankName: response?.bankName || '',
          accountNumber: response?.accountNumber || '',
          accountHolder: response?.accountHolder || '',
          bankBranch: response?.bankBranch || '',
          note: response?.note || '',
          isVerified: !!response?.isVerified,
          updatedAt: response?.updatedAt || '',
        });
        flashSaved('Đã lưu tài khoản thanh toán.');
      }
    } catch (requestError) {
      setError(requestError.message || 'Không thể lưu cài đặt.');
    } finally {
      setSaving(false);
    }
  }

  async function handleChangePassword() {
    setSaving(true);
    setError('');

    try {
      if (passwordForm.newPassword !== passwordForm.confirmPassword) {
        throw new Error('Mật khẩu mới và xác nhận mật khẩu không khớp.');
      }

      await changePassword({
        currentPassword: passwordForm.currentPassword,
        newPassword: passwordForm.newPassword,
      });
      setPasswordForm({ currentPassword: '', newPassword: '', confirmPassword: '' });
      flashSaved('Đã đổi mật khẩu. Các phiên khác sẽ cần đăng nhập lại.');
    } catch (requestError) {
      setError(requestError.message || 'Không thể đổi mật khẩu.');
    } finally {
      setSaving(false);
    }
  }

  async function handleSetupTwoFactor() {
    setSaving(true);
    setError('');

    try {
      const response = await setupTwoFactor();
      setTwoFactorSetup(response);
      setTwoFactor(response);
    } catch (requestError) {
      setError(requestError.message || 'Không thể tạo khóa 2FA.');
    } finally {
      setSaving(false);
    }
  }

  async function handleEnableTwoFactor() {
    setSaving(true);
    setError('');

    try {
      const response = await enableTwoFactor({ code: twoFactorCode });
      setTwoFactor(response?.status || null);
      setTwoFactorCode('');
      flashSaved('Đã bật xác thực 2 lớp.');
    } catch (requestError) {
      setError(requestError.message || 'Không thể bật 2FA.');
    } finally {
      setSaving(false);
    }
  }

  async function handleDisableTwoFactor() {
    setSaving(true);
    setError('');

    try {
      const response = await disableTwoFactor();
      setTwoFactor(response?.status || null);
      setTwoFactorSetup(null);
      flashSaved('Đã tắt xác thực 2 lớp.');
    } catch (requestError) {
      setError(requestError.message || 'Không thể tắt 2FA.');
    } finally {
      setSaving(false);
    }
  }

  async function handleRevokeSession(sessionId) {
    setSaving(true);
    setError('');

    try {
      await revokeMySession(sessionId);
      const response = await listMySessions();
      setSessions(Array.isArray(response?.items) ? response.items : []);
      flashSaved('Đã thu hồi phiên đăng nhập.');
    } catch (requestError) {
      setError(requestError.message || 'Không thể thu hồi phiên.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
        <div>
          <div className="mb-2 flex flex-wrap items-center gap-2">
            <span className="inline-flex items-center gap-1 rounded-lg bg-blue-50 px-2.5 py-1 text-[10px] font-black uppercase tracking-widest text-blue-700">
              <BadgeCheck size={12} />
              {getTenantOperatorBadge(session)}
            </span>
            <span className="rounded-lg bg-slate-100 px-2.5 py-1 text-[10px] font-black uppercase tracking-widest text-slate-500">
              {moduleLabel}
            </span>
          </div>
          <h1 className="text-2xl font-black text-slate-900">Cài đặt tenant</h1>
          <p className="mt-1 text-sm font-medium text-slate-500">
            Quản lý thông tin tenant, giữ chỗ, thông báo, bảo mật và tài khoản nhận tiền.
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          <button
            type="button"
            onClick={() => {
              setLoading(true);
              loadSettings();
            }}
            disabled={loading || saving}
            className="inline-flex items-center justify-center gap-2 rounded-xl border border-slate-200 bg-white px-5 py-3 text-sm font-black text-slate-700 transition hover:border-blue-200 hover:text-blue-600 disabled:opacity-60"
          >
            {loading ? <Loader2 size={16} className="animate-spin" /> : <RefreshCw size={16} />}
            Tải lại
          </button>
          {activeTab !== 'security' ? (
            <button
              type="button"
              onClick={handleSave}
              disabled={saving || loading || (activeTab === 'payment' && !canWriteFinance)}
              className={`inline-flex items-center justify-center gap-2 rounded-xl px-5 py-3 text-sm font-black text-white transition disabled:opacity-60 ${
                saved ? 'bg-emerald-500' : 'bg-slate-900 hover:bg-blue-600'
              }`}
            >
              {saving ? <Loader2 size={16} className="animate-spin" /> : <Save size={16} />}
              {saved || 'Lưu thay đổi'}
            </button>
          ) : null}
        </div>
      </div>

      {error ? (
        <div className="rounded-xl border border-rose-100 bg-rose-50 px-5 py-4 text-sm font-bold text-rose-600">
          {error}
        </div>
      ) : null}

      {saved && activeTab === 'security' ? (
        <div className="rounded-xl border border-emerald-100 bg-emerald-50 px-5 py-4 text-sm font-bold text-emerald-700">
          {saved}
        </div>
      ) : null}

      <div className="flex flex-col gap-6 lg:flex-row">
        <div className="shrink-0 lg:w-56">
          <div className="flex gap-1 overflow-x-auto rounded-xl border border-slate-100 bg-white p-2 shadow-sm lg:flex-col lg:overflow-visible">
            {TABS.map((tab) => (
              <button
                key={tab.id}
                type="button"
                onClick={() => setActiveTab(tab.id)}
                className={`flex w-full items-center gap-3 whitespace-nowrap rounded-lg px-4 py-3 text-left text-sm font-bold transition ${
                  activeTab === tab.id ? 'bg-slate-900 text-white shadow-sm' : 'text-slate-500 hover:bg-slate-50 hover:text-slate-800'
                }`}
              >
                <span className={activeTab === tab.id ? 'text-white' : 'text-slate-400'}>{tab.icon}</span>
                {tab.label}
              </button>
            ))}
          </div>
        </div>

        <div className="min-w-0 flex-1">
          <motion.div key={activeTab} initial={{ opacity: 0, x: 8 }} animate={{ opacity: 1, x: 0 }} transition={{ duration: 0.2 }}>
            {loading ? (
              <div className="flex items-center gap-3 rounded-xl border border-slate-100 bg-white p-8 text-sm font-bold text-slate-400 shadow-sm">
                <Loader2 size={16} className="animate-spin" />
                Đang tải cài đặt tenant...
              </div>
            ) : null}

            {!loading && activeTab === 'general' ? (
              <section className="space-y-6 rounded-xl border border-slate-100 bg-white p-6 shadow-sm">
                <h2 className="border-b border-slate-100 pb-4 text-sm font-black uppercase tracking-widest text-slate-900">Thông tin tenant</h2>
                <div className="grid grid-cols-1 gap-5 md:grid-cols-2">
                  <div className="space-y-2">
                    <label className="text-[10px] font-black uppercase tracking-[0.2em] text-slate-400">Tên tenant</label>
                    <input
                      value={generalForm.name}
                      onChange={(event) => setGeneralForm((current) => ({ ...current, name: event.target.value }))}
                      className="w-full rounded-xl border-2 border-transparent bg-slate-50 px-4 py-3 text-sm font-bold text-slate-900 outline-none transition focus:border-blue-200 focus:bg-white"
                    />
                  </div>
                  <div className="space-y-2">
                    <label className="text-[10px] font-black uppercase tracking-[0.2em] text-slate-400">Mã tenant</label>
                    <input value={generalForm.code} disabled className="w-full rounded-xl bg-slate-100 px-4 py-3 text-sm font-bold text-slate-500" />
                  </div>
                  <div className="space-y-2">
                    <label className="text-[10px] font-black uppercase tracking-[0.2em] text-slate-400">Loại tenant</label>
                    <input value={moduleLabel} disabled className="w-full rounded-xl bg-slate-100 px-4 py-3 text-sm font-bold text-slate-500" />
                  </div>
                  <div className="space-y-2">
                    <label className="text-[10px] font-black uppercase tracking-[0.2em] text-slate-400">Trạng thái</label>
                    <input value={generalForm.status || '--'} disabled className="w-full rounded-xl bg-slate-100 px-4 py-3 text-sm font-bold text-slate-500" />
                  </div>
                  <div className="space-y-2">
                    <label className="text-[10px] font-black uppercase tracking-[0.2em] text-slate-400">Vai trò của bạn</label>
                    <input value={membership?.roleName || tenantMembership?.roleName || '--'} disabled className="w-full rounded-xl bg-slate-100 px-4 py-3 text-sm font-bold text-slate-500" />
                  </div>
                  <div className="space-y-2">
                    <label className="text-[10px] font-black uppercase tracking-[0.2em] text-slate-400">Cập nhật gần nhất</label>
                    <input value={formatDateTime(tenant?.updatedAt || tenant?.createdAt)} disabled className="w-full rounded-xl bg-slate-100 px-4 py-3 text-sm font-bold text-slate-500" />
                  </div>
                </div>
              </section>
            ) : null}

            {!loading && activeTab === 'booking' ? (
              <section className="space-y-6 rounded-xl border border-slate-100 bg-white p-6 shadow-sm">
                <h2 className="border-b border-slate-100 pb-4 text-sm font-black uppercase tracking-widest text-slate-900">Cài đặt đặt chỗ</h2>
                <div>
                  <label className="mb-3 block text-[10px] font-black uppercase tracking-[0.2em] text-slate-400">Thời gian giữ chỗ</label>
                  <div className="flex items-center gap-4">
                    <input
                      type="range"
                      min={1}
                      max={60}
                      step={1}
                      value={bookingForm.holdMinutes}
                      onChange={(event) => setBookingForm((current) => ({ ...current, holdMinutes: Number(event.target.value) }))}
                      className="flex-1 accent-blue-600"
                    />
                    <div className="flex h-12 w-20 items-center justify-center rounded-xl bg-slate-900 text-xl font-black text-white">
                      {bookingForm.holdMinutes}'
                    </div>
                  </div>
                  <p className="mt-2 text-xs font-bold text-slate-400">
                    Chỗ sẽ được giữ {bookingForm.holdMinutes} phút trước khi tự động hủy nếu khách chưa thanh toán.
                  </p>
                </div>
                <div className="flex items-center justify-between gap-4 rounded-xl bg-slate-50 p-5">
                  <div>
                    <p className="text-sm font-black text-slate-900">Tự động xác nhận đơn hàng</p>
                    <p className="mt-0.5 text-xs font-medium text-slate-500">Tùy chọn vận hành cục bộ cho tenant portal.</p>
                  </div>
                  <Toggle checked={bookingForm.autoConfirm} onChange={(value) => setBookingForm((current) => ({ ...current, autoConfirm: value }))} />
                </div>
                <div className="flex items-center justify-between gap-4 rounded-xl bg-slate-50 p-5">
                  <div>
                    <p className="text-sm font-black text-slate-900">Hỗ trợ xuất hóa đơn VAT</p>
                    <p className="mt-0.5 text-xs font-medium text-slate-500">Lưu cục bộ đến khi có module VAT chính thức.</p>
                  </div>
                  <Toggle checked={bookingForm.vatEnabled} onChange={(value) => setBookingForm((current) => ({ ...current, vatEnabled: value }))} />
                </div>
                <div className="space-y-2">
                  <label className="text-[10px] font-black uppercase tracking-[0.2em] text-slate-400">Chính sách hủy mặc định</label>
                  <select
                    value={bookingForm.cancellationPolicy}
                    onChange={(event) => setBookingForm((current) => ({ ...current, cancellationPolicy: event.target.value }))}
                    className="w-full cursor-pointer rounded-xl border-2 border-transparent bg-slate-50 px-4 py-3 text-sm font-bold text-slate-900 outline-none focus:border-blue-200"
                  >
                    <option value="full_48h">Hoàn 100% nếu hủy trước 48 giờ</option>
                    <option value="half_24h">Hoàn 50% nếu hủy trước 24 giờ</option>
                    <option value="no_refund">Không hoàn tiền</option>
                  </select>
                </div>
              </section>
            ) : null}

            {!loading && activeTab === 'notify' ? (
              <section className="space-y-4 rounded-xl border border-slate-100 bg-white p-6 shadow-sm">
                <h2 className="border-b border-slate-100 pb-4 text-sm font-black uppercase tracking-widest text-slate-900">Cài đặt thông báo</h2>
                <div className="rounded-xl border border-blue-100 bg-blue-50 px-5 py-4 text-xs font-bold text-blue-700">
                  Các tùy chọn này đang được lưu trên trình duyệt cho tenant hiện tại. Khi backend có module notification preference, trang đã sẵn form để nối API.
                </div>
                {NOTIFICATION_OPTIONS.map((item) => (
                  <div key={item.key} className="flex items-center justify-between gap-4 rounded-xl bg-slate-50 p-5">
                    <div>
                      <p className="text-sm font-black text-slate-900">{item.label}</p>
                      <p className="mt-0.5 text-xs font-medium text-slate-500">{item.sub}</p>
                    </div>
                    <Toggle
                      checked={Boolean(notifications[item.key])}
                      onChange={(value) => setNotifications((current) => ({ ...current, [item.key]: value }))}
                    />
                  </div>
                ))}
              </section>
            ) : null}

            {!loading && activeTab === 'security' ? (
              <section className="space-y-6 rounded-xl border border-slate-100 bg-white p-6 shadow-sm">
                <h2 className="border-b border-slate-100 pb-4 text-sm font-black uppercase tracking-widest text-slate-900">Bảo mật tài khoản</h2>

                <div className="rounded-xl bg-slate-50 p-5">
                  <h3 className="mb-4 text-sm font-black text-slate-900">Đổi mật khẩu đăng nhập</h3>
                  <div className="grid grid-cols-1 gap-3 md:grid-cols-3">
                    <input type="password" value={passwordForm.currentPassword} onChange={(event) => setPasswordForm((current) => ({ ...current, currentPassword: event.target.value }))} placeholder="Mật khẩu hiện tại" className="rounded-xl bg-white px-4 py-3 text-sm font-bold outline-none" />
                    <input type="password" value={passwordForm.newPassword} onChange={(event) => setPasswordForm((current) => ({ ...current, newPassword: event.target.value }))} placeholder="Mật khẩu mới" className="rounded-xl bg-white px-4 py-3 text-sm font-bold outline-none" />
                    <input type="password" value={passwordForm.confirmPassword} onChange={(event) => setPasswordForm((current) => ({ ...current, confirmPassword: event.target.value }))} placeholder="Nhập lại mật khẩu mới" className="rounded-xl bg-white px-4 py-3 text-sm font-bold outline-none" />
                  </div>
                  <button type="button" onClick={handleChangePassword} disabled={saving} className="mt-4 inline-flex items-center gap-2 rounded-xl bg-slate-900 px-5 py-3 text-sm font-black text-white disabled:opacity-60">
                    {saving ? <Loader2 size={16} className="animate-spin" /> : <KeyRound size={16} />}
                    Đổi mật khẩu
                  </button>
                </div>

                <div className="rounded-xl bg-slate-50 p-5">
                  <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
                    <div>
                      <h3 className="text-sm font-black text-slate-900">Xác thực 2 lớp</h3>
                      <p className="mt-1 text-xs font-bold text-slate-500">
                        Trạng thái: {twoFactor?.isEnabled ? 'Đã bật' : 'Chưa bật'} · Recovery codes: {twoFactor?.recoveryCodesLeft ?? 0}
                      </p>
                    </div>
                    {twoFactor?.isEnabled ? (
                      <button type="button" onClick={handleDisableTwoFactor} disabled={saving} className="inline-flex items-center gap-2 rounded-xl bg-rose-50 px-4 py-2 text-xs font-black uppercase text-rose-700 disabled:opacity-60">
                        Tắt 2FA
                      </button>
                    ) : (
                      <button type="button" onClick={handleSetupTwoFactor} disabled={saving} className="inline-flex items-center gap-2 rounded-xl bg-blue-600 px-4 py-2 text-xs font-black uppercase text-white disabled:opacity-60">
                        Tạo khóa 2FA
                      </button>
                    )}
                  </div>
                  {twoFactorSetup?.sharedKey ? (
                    <div className="mt-4 rounded-xl bg-white p-4">
                      <p className="text-xs font-bold text-slate-500">Nhập khóa này vào ứng dụng Authenticator:</p>
                      <p className="mt-1 break-all text-sm font-black text-slate-900">{twoFactorSetup.sharedKey}</p>
                      <div className="mt-3 flex flex-col gap-2 md:flex-row">
                        <input value={twoFactorCode} onChange={(event) => setTwoFactorCode(event.target.value)} placeholder="Mã 6 số" className="rounded-xl bg-slate-50 px-4 py-3 text-sm font-bold outline-none md:w-48" />
                        <button type="button" onClick={handleEnableTwoFactor} disabled={saving || !twoFactorCode.trim()} className="inline-flex items-center justify-center gap-2 rounded-xl bg-slate-900 px-5 py-3 text-sm font-black text-white disabled:opacity-60">
                          Bật 2FA
                        </button>
                      </div>
                    </div>
                  ) : null}
                </div>

                <div className="rounded-xl bg-slate-50 p-5">
                  <h3 className="mb-4 text-sm font-black text-slate-900">Phiên đăng nhập đang hoạt động</h3>
                  <div className="divide-y divide-slate-100 overflow-hidden rounded-xl bg-white">
                    {sessions.length === 0 ? (
                      <div className="px-4 py-5 text-sm font-bold text-slate-400">Chưa có dữ liệu phiên đăng nhập.</div>
                    ) : (
                      sessions.map((item) => (
                        <div key={item.sessionId} className="flex flex-col gap-3 px-4 py-4 md:flex-row md:items-center md:justify-between">
                          <div>
                            <p className="text-sm font-black text-slate-900">{item.isCurrent ? 'Phiên hiện tại' : item.deviceName || item.sessionId}</p>
                            <p className="mt-1 text-xs font-bold text-slate-400">Tạo: {formatDateTime(item.createdAt)} · Hết hạn: {formatDateTime(item.expiresAt)}</p>
                          </div>
                          {!item.isCurrent ? (
                            <button type="button" onClick={() => handleRevokeSession(item.sessionId)} disabled={saving} className="inline-flex items-center gap-1 text-xs font-black uppercase text-rose-600 disabled:opacity-60">
                              Thu hồi
                              <ChevronRight size={14} />
                            </button>
                          ) : null}
                        </div>
                      ))
                    )}
                  </div>
                </div>

                <div className="rounded-xl border border-rose-100 bg-rose-50 p-5">
                  <div className="mb-2 flex items-center gap-2">
                    <AlertTriangle size={16} className="text-rose-500" />
                    <p className="text-sm font-black text-rose-700">Vùng nguy hiểm</p>
                  </div>
                  <p className="text-xs font-medium text-rose-600">
                    Xóa hoặc đóng tenant cần admin xử lý để tránh mất dữ liệu booking, đối soát và lịch sử vận hành.
                  </p>
                </div>
              </section>
            ) : null}

            {!loading && activeTab === 'payment' ? (
              <section className="space-y-6 rounded-xl border border-slate-100 bg-white p-6 shadow-sm">
                <h2 className="border-b border-slate-100 pb-4 text-sm font-black uppercase tracking-widest text-slate-900">Thông tin thanh toán</h2>
                {!canReadFinance ? (
                  <div className="rounded-xl border border-amber-100 bg-amber-50 px-5 py-4 text-sm font-bold text-amber-700">
                    Tài khoản này chưa có quyền xem tài chính tenant.
                  </div>
                ) : null}
                <div className="grid grid-cols-1 gap-5 md:grid-cols-2">
                  {[
                    { key: 'bankName', label: 'Ngân hàng' },
                    { key: 'accountNumber', label: 'Số tài khoản' },
                    { key: 'accountHolder', label: 'Chủ tài khoản' },
                    { key: 'bankBranch', label: 'Chi nhánh' },
                  ].map((field) => (
                    <div key={field.key} className="space-y-2">
                      <label className="text-[10px] font-black uppercase tracking-[0.2em] text-slate-400">{field.label}</label>
                      <input
                        value={paymentForm[field.key]}
                        onChange={(event) => setPaymentForm((current) => ({ ...current, [field.key]: event.target.value }))}
                        disabled={!canWriteFinance}
                        className="w-full rounded-xl border-2 border-transparent bg-slate-50 px-4 py-3 text-sm font-bold text-slate-900 outline-none transition focus:border-blue-200 focus:bg-white disabled:text-slate-400"
                      />
                    </div>
                  ))}
                </div>
                <div className="rounded-xl bg-blue-50 p-5">
                  <p className="mb-2 text-[10px] font-black uppercase tracking-widest text-blue-400">Trạng thái payout account</p>
                  <p className="text-sm font-bold text-blue-800">
                    {paymentForm.isVerified ? 'Tài khoản đã được xác minh.' : 'Tài khoản đang chờ admin xác minh hoặc vừa thay đổi thông tin quan trọng.'}
                  </p>
                  <p className="mt-1 text-xs font-bold text-blue-600">Cập nhật gần nhất: {formatDateTime(paymentForm.updatedAt)}</p>
                </div>
                <div className="space-y-2">
                  <label className="text-[10px] font-black uppercase tracking-[0.2em] text-slate-400">Ghi chú đối soát</label>
                  <textarea
                    rows={3}
                    value={paymentForm.note}
                    onChange={(event) => setPaymentForm((current) => ({ ...current, note: event.target.value }))}
                    disabled={!canWriteFinance}
                    className="w-full resize-none rounded-xl border-2 border-transparent bg-slate-50 px-4 py-3 text-sm font-medium text-slate-900 outline-none transition focus:border-blue-200 focus:bg-white disabled:text-slate-400"
                  />
                </div>
              </section>
            ) : null}
          </motion.div>
        </div>
      </div>
    </div>
  );
}
