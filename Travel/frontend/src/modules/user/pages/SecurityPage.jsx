import React, { useEffect, useState } from 'react';
import { motion } from 'framer-motion';
import {
  Shield,
  Key,
  Smartphone,
  Lock,
  AlertTriangle,
  CheckCircle,
  Eye,
  EyeOff,
  MonitorSmartphone,
  RefreshCw,
} from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { changePassword, deactivateAccount, listMySessions, logoutAllSessions, revokeMySession } from '../../../services/auth';
import { clearAuthSession } from '../../../services/interceptor';
import TwoFactorCard from '../components/TwoFactorCard';

function formatRelative(value) {
  if (!value) {
    return 'Vừa cập nhật';
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return 'Vừa cập nhật';
  }

  const diffMinutes = Math.max(1, Math.round((Date.now() - date.getTime()) / 60000));
  if (diffMinutes < 60) {
    return `${diffMinutes} phút trước`;
  }

  const diffHours = Math.round(diffMinutes / 60);
  if (diffHours < 24) {
    return `${diffHours} giờ trước`;
  }

  const diffDays = Math.round(diffHours / 24);
  return `${diffDays} ngày trước`;
}

function formatSessionLabel(session) {
  return session.userAgent || 'Thiết bị đăng nhập';
}

function formatSessionMeta(session) {
  const parts = [];

  if (session.ipAddress) {
    parts.push(session.ipAddress);
  }

  parts.push(session.isCurrent ? 'Hiện tại' : formatRelative(session.lastRefreshedAt || session.createdAt));
  return parts.join(' · ');
}

export default function SecurityPage() {
  const navigate = useNavigate();
  const [showPass, setShowPass] = useState(false);
  const [form, setForm] = useState({
    currentPassword: '',
    newPassword: '',
    confirmPassword: '',
  });
  const [sessions, setSessions] = useState([]);
  const [loadingSessions, setLoadingSessions] = useState(true);
  const [loading, setLoading] = useState(false);
  const [sessionActionLoading, setSessionActionLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  useEffect(() => {
    loadSessions();
  }, []);

  const updateField = (key, value) => {
    setForm((prev) => ({ ...prev, [key]: value }));
  };

  async function loadSessions() {
    setLoadingSessions(true);

    try {
      const response = await listMySessions();
      setSessions((response?.items || []).map((item) => ({
        ...item,
        isCurrent: !!item.isCurrent,
      })));
    } catch (err) {
      setError(err.message || 'Không thể tải danh sách phiên đăng nhập.');
      setSessions([]);
    } finally {
      setLoadingSessions(false);
    }
  }

  const handleSubmit = async () => {
    setError('');
    setSuccess('');

    if (form.newPassword !== form.confirmPassword) {
      setError('Mật khẩu xác nhận chưa khớp.');
      return;
    }

    setLoading(true);

    try {
      await changePassword({
        currentPassword: form.currentPassword,
        newPassword: form.newPassword,
      });

      setForm({
        currentPassword: '',
        newPassword: '',
        confirmPassword: '',
      });
      setSuccess('Mật khẩu đã được cập nhật. Vui lòng đăng nhập lại.');
      clearAuthSession();
      window.setTimeout(() => {
        navigate('/auth/login', {
          replace: true,
          state: { resetMessage: 'Mật khẩu đã được cập nhật. Vui lòng đăng nhập lại.' },
        });
      }, 1200);
    } catch (err) {
      setError(err.message || 'Không thể cập nhật mật khẩu.');
    } finally {
      setLoading(false);
    }
  };

  async function handleRevokeSession(session) {
    setSessionActionLoading(true);
    setError('');
    setSuccess('');

    try {
      await revokeMySession(session.sessionId);

      if (session.isCurrent) {
        clearAuthSession();
        navigate('/auth/login', {
          replace: true,
          state: { resetMessage: 'Phiên hiện tại đã được đăng xuất.' },
        });
        return;
      }

      setSuccess('Phiên đăng nhập đã được đăng xuất.');
      await loadSessions();
    } catch (err) {
      setError(err.message || 'Không thể đăng xuất phiên này.');
    } finally {
      setSessionActionLoading(false);
    }
  }

  async function handleLogoutAll() {
    setSessionActionLoading(true);
    setError('');
    setSuccess('');

    try {
      await logoutAllSessions();
      navigate('/auth/login', {
        replace: true,
        state: { resetMessage: 'Tất cả phiên đăng nhập đã được đăng xuất.' },
      });
    } catch (err) {
      setError(err.message || 'Không thể đăng xuất tất cả phiên.');
    } finally {
      setSessionActionLoading(false);
    }
  }

  async function handleDeactivateAccount() {
    if (!window.confirm('Bạn có chắc muốn vô hiệu hóa tài khoản này không?')) {
      return;
    }

    setSessionActionLoading(true);
    setError('');
    setSuccess('');

    try {
      await deactivateAccount({ reason: 'Self-service deactivation from security page' });
      navigate('/auth/login', {
        replace: true,
        state: { resetMessage: 'Tài khoản đã được vô hiệu hóa.' },
      });
    } catch (err) {
      setError(err.message || 'Không thể vô hiệu hóa tài khoản.');
    } finally {
      setSessionActionLoading(false);
    }
  }

  return (
    <motion.div
      initial={{ opacity: 0, y: 24 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.55, ease: [0.22, 1, 0.36, 1] }}
      className="space-y-6"
    >
      <div className="relative rounded-[2.5rem] overflow-hidden shadow-2xl">
        <img
          src="https://images.unsplash.com/photo-1614064641938-3bbee52942c7?auto=format&fit=crop&q=80&w=1600"
          alt="security"
          className="absolute inset-0 w-full h-full object-cover"
        />
        <div className="absolute inset-0 bg-gradient-to-r from-slate-900/95 to-slate-900/70" />
        <div className="relative z-10 p-10 flex items-center gap-6">
          <div className="w-16 h-16 rounded-2xl bg-white/10 border border-white/20 backdrop-blur flex items-center justify-center text-[#1EB4D4] shrink-0">
            <Shield size={32} strokeWidth={1.5} />
          </div>
          <div className="text-white">
            <div className="flex items-center gap-2 mb-1">
              <span className="text-[10px] font-black text-[#1EB4D4] uppercase tracking-[0.25em]">Lá chắn bảo vệ</span>
            </div>
            <h1 className="text-3xl font-black tracking-tight">Bảo mật tài khoản</h1>
            <p className="text-white/60 text-sm font-medium mt-1">Bảo vệ tài khoản du lịch của bạn trên mọi hành trình</p>
          </div>
          <div className="ml-auto hidden md:flex items-center gap-2 px-5 py-3 bg-emerald-400/20 border border-emerald-400/30 rounded-2xl">
            <CheckCircle size={16} className="text-emerald-400" />
            <span className="text-emerald-300 text-xs font-black uppercase tracking-widest">Tài khoản an toàn</span>
          </div>
        </div>
      </div>

      <div className="bg-white rounded-[2.5rem] p-8 shadow-xl shadow-slate-100/60">
        <div className="flex items-center gap-4 mb-8">
          <div className="w-11 h-11 bg-[#1EB4D4]/10 rounded-2xl flex items-center justify-center text-[#1EB4D4]">
            <Key size={20} />
          </div>
          <div>
            <h2 className="text-xl font-black text-slate-900 tracking-tight">Đổi mật khẩu</h2>
            <p className="text-xs text-slate-400 font-bold italic mt-0.5" style={{ fontFamily: "'Kalam', cursive" }}>Nên thay đổi định kỳ 6 tháng một lần</p>
          </div>
        </div>

        {error ? (
          <div className="mb-6 rounded-[1.5rem] border border-rose-100 bg-rose-50 px-5 py-4 text-sm font-bold text-rose-600">
            {error}
          </div>
        ) : null}

        {success ? (
          <div className="mb-6 rounded-[1.5rem] border border-emerald-100 bg-emerald-50 px-5 py-4 text-sm font-bold text-emerald-700">
            {success}
          </div>
        ) : null}

        <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
          {[
            { key: 'currentPassword', label: 'Mật khẩu hiện tại', value: form.currentPassword, span: 'md:col-span-2' },
            { key: 'newPassword', label: 'Mật khẩu mới', value: form.newPassword, span: '' },
          ].map((field, index) => (
            <div key={field.key} className={field.span}>
              <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] mb-2 block">{field.label}</label>
              <div className="relative group">
                <Lock className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-300 group-focus-within:text-[#1EB4D4] transition-colors" size={18} />
                <input
                  type={showPass ? 'text' : 'password'}
                  value={field.value}
                  onChange={(event) => updateField(field.key, event.target.value)}
                  placeholder="••••••••••"
                  className="w-full bg-slate-50 border-2 border-transparent focus:border-[#1EB4D4]/30 focus:bg-white rounded-2xl py-4 pl-12 pr-12 font-bold text-slate-900 text-sm focus:ring-0 transition-all"
                />
                {index === 0 ? (
                  <button type="button" onClick={() => setShowPass(!showPass)} className="absolute right-4 top-1/2 -translate-y-1/2 text-slate-300 hover:text-slate-600 transition-colors">
                    {showPass ? <EyeOff size={18} /> : <Eye size={18} />}
                  </button>
                ) : null}
              </div>
            </div>
          ))}
          <div>
            <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] mb-2 block">Nhập lại mật khẩu mới</label>
            <div className="relative group">
              <Lock className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-300" size={18} />
              <input
                type="password"
                value={form.confirmPassword}
                onChange={(event) => updateField('confirmPassword', event.target.value)}
                placeholder="••••••••••"
                className="w-full bg-slate-50 border-2 border-transparent focus:border-[#1EB4D4]/30 focus:bg-white rounded-2xl py-4 pl-12 pr-4 font-bold text-slate-900 text-sm focus:ring-0 transition-all"
              />
            </div>
          </div>
        </div>

        <button
          type="button"
          onClick={handleSubmit}
          disabled={loading}
          className="mt-6 px-8 py-4 bg-gradient-to-r from-[#1EB4D4] to-[#002B7F] text-white rounded-2xl font-black text-xs uppercase tracking-widest shadow-xl shadow-blue-500/25 hover:-translate-y-0.5 transition-all disabled:opacity-70 disabled:hover:translate-y-0"
        >
          {loading ? 'Đang cập nhật...' : 'Cập nhật mật khẩu'}
        </button>
      </div>

      <TwoFactorCard />

      <div className="bg-white rounded-[2.5rem] p-8 shadow-xl shadow-slate-100/60">
        <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4 mb-6">
          <div className="flex items-center gap-4">
            <div className="w-11 h-11 bg-slate-100 rounded-2xl flex items-center justify-center text-slate-500">
              <MonitorSmartphone size={20} />
            </div>
            <div>
              <h2 className="text-xl font-black text-slate-900 tracking-tight">Phiên đăng nhập hoạt động</h2>
              <p className="text-xs text-slate-400 font-bold">Thiết bị đang truy cập tài khoản của bạn</p>
            </div>
          </div>
          <button
            type="button"
            onClick={handleLogoutAll}
            disabled={sessionActionLoading || loadingSessions || sessions.length === 0}
            className="px-5 py-3 bg-slate-900 text-white rounded-2xl text-[11px] font-black uppercase tracking-widest hover:bg-blue-600 transition-all disabled:opacity-70"
          >
            Đăng xuất tất cả
          </button>
        </div>

        <div className="space-y-3">
          {loadingSessions ? (
            <div className="rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-500 flex items-center gap-2">
              <RefreshCw size={16} className="animate-spin" /> Đang tải danh sách phiên...
            </div>
          ) : sessions.length === 0 ? (
            <div className="rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-500">
              Chưa có thông tin phiên đăng nhập.
            </div>
          ) : sessions.map((session) => (
            <div key={session.sessionId} className={`flex items-center justify-between p-5 rounded-2xl border transition-all ${session.isCurrent ? 'border-[#1EB4D4]/30 bg-[#1EB4D4]/5' : 'border-slate-100 hover:border-slate-200 hover:bg-slate-50/50'}`}>
              <div className="flex items-center gap-4">
                <div className={`w-10 h-10 rounded-xl flex items-center justify-center ${session.isCurrent ? 'bg-[#1EB4D4]/10 text-[#1EB4D4]' : 'bg-slate-100 text-slate-500'}`}>
                  {session.userAgent?.toLowerCase().includes('iphone') ? <Smartphone size={18} /> : <MonitorSmartphone size={18} />}
                </div>
                <div>
                  <p className="font-black text-slate-900 text-sm">{formatSessionLabel(session)}</p>
                  <p className="text-xs text-slate-400 font-bold">{formatSessionMeta(session)}</p>
                </div>
              </div>
              {session.isCurrent ? (
                <span className="px-3 py-1.5 bg-[#1EB4D4]/10 text-[#1EB4D4] rounded-xl text-[10px] font-black uppercase tracking-widest">Thiết bị này</span>
              ) : (
                <button
                  type="button"
                  onClick={() => handleRevokeSession(session)}
                  disabled={sessionActionLoading}
                  className="flex items-center gap-1 px-4 py-2 text-slate-400 hover:text-rose-500 hover:bg-rose-50 rounded-xl text-[10px] font-black uppercase tracking-widest transition-all disabled:opacity-70"
                >
                  Đăng xuất
                </button>
              )}
            </div>
          ))}
        </div>
      </div>

      <div className="relative rounded-[2rem] bg-rose-50 border border-rose-100 p-8 overflow-hidden">
        <div className="absolute -right-8 -top-8 text-rose-100">
          <AlertTriangle size={120} fill="currentColor" />
        </div>
        <div className="relative z-10">
          <div className="flex items-center gap-3 mb-3 text-rose-600">
            <AlertTriangle size={22} strokeWidth={2.5} />
            <h3 className="font-black text-base uppercase tracking-[0.2em]">Vùng nguy hiểm</h3>
          </div>
          <p className="text-rose-700/80 text-sm font-medium leading-relaxed max-w-2xl mb-6 italic" style={{ fontFamily: "'Kalam', cursive" }}>
            Vô hiệu hóa tài khoản sẽ chặn toàn bộ quyền truy cập hiện tại của bạn. Hành động này cần đăng nhập lại nếu được kích hoạt lại sau này.
          </p>
          <button
            type="button"
            onClick={handleDeactivateAccount}
            disabled={sessionActionLoading}
            className="flex items-center gap-2 px-6 py-3 bg-white border border-rose-200 text-rose-600 rounded-2xl font-black text-xs uppercase tracking-widest hover:bg-rose-600 hover:text-white hover:border-rose-600 transition-all shadow-sm disabled:opacity-70"
          >
            <AlertTriangle size={14} /> Vô hiệu hóa tài khoản
          </button>
        </div>
      </div>
    </motion.div>
  );
}
