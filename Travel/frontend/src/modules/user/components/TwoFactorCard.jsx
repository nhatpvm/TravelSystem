import React, { useEffect, useState } from 'react';
import { AlertTriangle, CheckCircle, Copy, KeyRound, RefreshCw, ShieldCheck, Smartphone } from 'lucide-react';
import {
  disableTwoFactor,
  enableTwoFactor,
  getTwoFactorStatus,
  regenerateTwoFactorRecoveryCodes,
  setupTwoFactor,
} from '../../../services/auth';

function copyText(value) {
  if (!value || !navigator?.clipboard?.writeText) {
    return Promise.resolve(false);
  }

  return navigator.clipboard.writeText(value).then(() => true).catch(() => false);
}

export default function TwoFactorCard() {
  const [status, setStatus] = useState(null);
  const [setup, setSetup] = useState(null);
  const [code, setCode] = useState('');
  const [recoveryCodes, setRecoveryCodes] = useState([]);
  const [loading, setLoading] = useState(true);
  const [actionLoading, setActionLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  useEffect(() => {
    loadStatus();
  }, []);

  async function loadStatus() {
    setLoading(true);

    try {
      const response = await getTwoFactorStatus();
      setStatus(response);
    } catch (requestError) {
      setError(requestError.message || 'Không thể tải trạng thái xác thực 2 lớp.');
    } finally {
      setLoading(false);
    }
  }

  async function handleSetup() {
    setActionLoading(true);
    setError('');
    setSuccess('');

    try {
      const response = await setupTwoFactor();
      setSetup(response);
      setStatus(response);
      setRecoveryCodes([]);
      setSuccess('Khóa xác thực đã sẵn sàng. Hãy thêm vào ứng dụng Authenticator rồi nhập mã 6 số để bật 2FA.');
    } catch (requestError) {
      setError(requestError.message || 'Không thể khởi tạo Authenticator.');
    } finally {
      setActionLoading(false);
    }
  }

  async function handleEnable() {
    setActionLoading(true);
    setError('');
    setSuccess('');

    try {
      const response = await enableTwoFactor({ code });
      setStatus(response?.status || null);
      setRecoveryCodes(Array.isArray(response?.recoveryCodes) ? response.recoveryCodes : []);
      setCode('');
      setSuccess('Đã bật xác thực 2 lớp. Hãy lưu lại các mã dự phòng ở bên dưới.');
    } catch (requestError) {
      setError(requestError.message || 'Không thể bật xác thực 2 lớp.');
    } finally {
      setActionLoading(false);
    }
  }

  async function handleDisable() {
    if (!window.confirm('Bạn có chắc muốn tắt xác thực 2 lớp cho tài khoản này không?')) {
      return;
    }

    setActionLoading(true);
    setError('');
    setSuccess('');

    try {
      const response = await disableTwoFactor();
      setStatus(response?.status || null);
      setSetup(null);
      setRecoveryCodes([]);
      setCode('');
      setSuccess('Đã tắt xác thực 2 lớp.');
    } catch (requestError) {
      setError(requestError.message || 'Không thể tắt xác thực 2 lớp.');
    } finally {
      setActionLoading(false);
    }
  }

  async function handleRegenerateRecoveryCodes() {
    setActionLoading(true);
    setError('');
    setSuccess('');

    try {
      const response = await regenerateTwoFactorRecoveryCodes();
      setStatus(response?.status || null);
      setRecoveryCodes(Array.isArray(response?.recoveryCodes) ? response.recoveryCodes : []);
      setSuccess('Đã tạo bộ mã dự phòng mới. Các mã cũ sẽ không còn hiệu lực.');
    } catch (requestError) {
      setError(requestError.message || 'Không thể tạo lại mã dự phòng.');
    } finally {
      setActionLoading(false);
    }
  }

  async function handleCopy(value, label) {
    const copied = await copyText(value);
    if (!copied) {
      setError(`Không thể sao chép ${label.toLowerCase()}.`);
      return;
    }

    setSuccess(`Đã sao chép ${label.toLowerCase()}.`);
  }

  const isEnabled = !!status?.isEnabled;

  return (
    <div className="bg-white rounded-[2.5rem] p-8 shadow-xl shadow-slate-100/60">
      <div className="flex flex-col md:flex-row items-start md:items-center justify-between gap-6">
        <div className="flex items-center gap-4">
          <div className="w-11 h-11 bg-rose-50 rounded-2xl flex items-center justify-center text-rose-500">
            <Smartphone size={20} />
          </div>
          <div>
            <div className="flex items-center gap-3 mb-0.5">
              <h2 className="text-xl font-black text-slate-900 tracking-tight">Xác thực 2 lớp (2FA)</h2>
              <span className={`px-2.5 py-1 rounded-full text-[9px] font-black uppercase tracking-widest ${isEnabled ? 'bg-emerald-50 text-emerald-600' : 'bg-amber-50 text-amber-600'}`}>
                {isEnabled ? 'Đang bật' : 'Chưa bật'}
              </span>
            </div>
            <p className="text-xs text-slate-400 font-bold italic" style={{ fontFamily: "'Kalam', cursive" }}>
              Bảo vệ tài khoản bằng mã xác minh từ ứng dụng Authenticator và bộ mã dự phòng dùng khi cần.
            </p>
          </div>
        </div>

        <div className="flex flex-wrap gap-3">
          {!setup ? (
            <button
              type="button"
              onClick={handleSetup}
              disabled={loading || actionLoading}
              className="shrink-0 px-8 py-4 bg-slate-900 text-white rounded-2xl font-black text-xs uppercase tracking-widest hover:bg-[#1EB4D4] transition-all disabled:opacity-70"
            >
              {actionLoading ? 'Đang chuẩn bị...' : 'Thiết lập Authenticator'}
            </button>
          ) : null}

          {isEnabled ? (
            <>
              <button
                type="button"
                onClick={handleRegenerateRecoveryCodes}
                disabled={actionLoading}
                className="shrink-0 px-8 py-4 bg-slate-100 text-slate-700 rounded-2xl font-black text-xs uppercase tracking-widest hover:bg-slate-200 transition-all disabled:opacity-70"
              >
                Tạo lại mã dự phòng
              </button>
              <button
                type="button"
                onClick={handleDisable}
                disabled={actionLoading}
                className="shrink-0 px-8 py-4 bg-rose-50 text-rose-600 rounded-2xl font-black text-xs uppercase tracking-widest hover:bg-rose-100 transition-all disabled:opacity-70"
              >
                Tắt 2FA
              </button>
            </>
          ) : null}
        </div>
      </div>

      {loading ? (
        <div className="mt-6 rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-500 flex items-center gap-2">
          <RefreshCw size={16} className="animate-spin" /> Đang tải trạng thái xác thực 2 lớp...
        </div>
      ) : null}

      {error ? (
        <div className="mt-6 rounded-[1.5rem] border border-rose-100 bg-rose-50 px-5 py-4 text-sm font-bold text-rose-600">
          {error}
        </div>
      ) : null}

      {success ? (
        <div className="mt-6 rounded-[1.5rem] border border-emerald-100 bg-emerald-50 px-5 py-4 text-sm font-bold text-emerald-700">
          {success}
        </div>
      ) : null}

      {status ? (
        <div className="mt-6 grid grid-cols-1 md:grid-cols-3 gap-4">
          {[
            {
              label: 'Trạng thái',
              value: status.isEnabled ? 'Đã bật' : 'Chưa bật',
            },
            {
              label: 'Authenticator',
              value: status.hasAuthenticator ? 'Đã thiết lập' : 'Chưa thiết lập',
            },
            {
              label: 'Mã dự phòng còn lại',
              value: status.recoveryCodesLeft ?? 0,
            },
          ].map((item) => (
            <div key={item.label} className="rounded-2xl bg-slate-50 px-5 py-4">
              <p className="text-[10px] font-black uppercase tracking-widest text-slate-400">{item.label}</p>
              <p className="mt-2 text-lg font-black text-slate-900">{item.value}</p>
            </div>
          ))}
        </div>
      ) : null}

      {setup ? (
        <div className="mt-6 rounded-[2rem] border border-slate-100 bg-slate-50 p-6 space-y-5">
          <div className="flex items-center gap-3">
            <div className="w-11 h-11 rounded-2xl bg-white flex items-center justify-center text-[#1EB4D4] shadow-sm">
              <ShieldCheck size={18} />
            </div>
            <div>
              <p className="text-sm font-black text-slate-900">Bước 1: Thêm khóa vào ứng dụng Authenticator</p>
              <p className="text-xs font-bold text-slate-500">
                Bạn có thể nhập thủ công khóa ở dưới hoặc dùng liên kết otpauth nếu ứng dụng hỗ trợ.
              </p>
            </div>
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
            <div className="rounded-2xl bg-white border border-slate-100 p-5">
              <p className="text-[10px] font-black uppercase tracking-widest text-slate-400 mb-2">Khóa thủ công</p>
              <p className="font-black text-slate-900 break-all">{setup.sharedKey}</p>
              <button
                type="button"
                onClick={() => handleCopy(setup.sharedKey, 'khóa thủ công')}
                className="mt-4 inline-flex items-center gap-2 text-[11px] font-black uppercase tracking-widest text-[#1EB4D4] hover:underline"
              >
                <Copy size={14} /> Sao chép khóa
              </button>
            </div>

            <div className="rounded-2xl bg-white border border-slate-100 p-5">
              <p className="text-[10px] font-black uppercase tracking-widest text-slate-400 mb-2">Liên kết cấu hình</p>
              <p className="text-xs font-bold text-slate-600 break-all">{setup.authenticatorUri}</p>
              <button
                type="button"
                onClick={() => handleCopy(setup.authenticatorUri, 'liên kết cấu hình')}
                className="mt-4 inline-flex items-center gap-2 text-[11px] font-black uppercase tracking-widest text-[#1EB4D4] hover:underline"
              >
                <Copy size={14} /> Sao chép liên kết
              </button>
            </div>
          </div>

          {!isEnabled ? (
            <div className="rounded-2xl bg-white border border-slate-100 p-5">
              <p className="text-sm font-black text-slate-900 mb-3">Bước 2: Nhập mã 6 số để bật 2FA</p>
              <div className="flex flex-col md:flex-row gap-4">
                <div className="relative flex-1">
                  <KeyRound className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-300" size={18} />
                  <input
                    type="text"
                    value={code}
                    onChange={(event) => setCode(event.target.value)}
                    placeholder="123 456"
                    className="w-full bg-slate-50 border-2 border-transparent focus:border-[#1EB4D4]/30 focus:bg-white rounded-2xl py-4 pl-12 pr-4 font-bold text-slate-900 text-sm focus:ring-0 transition-all"
                  />
                </div>
                <button
                  type="button"
                  onClick={handleEnable}
                  disabled={actionLoading || !code.trim()}
                  className="px-8 py-4 bg-gradient-to-r from-[#1EB4D4] to-[#002B7F] text-white rounded-2xl font-black text-xs uppercase tracking-widest shadow-xl shadow-blue-500/25 hover:-translate-y-0.5 transition-all disabled:opacity-70 disabled:hover:translate-y-0"
                >
                  {actionLoading ? 'Đang xác minh...' : 'Bật 2FA'}
                </button>
              </div>
            </div>
          ) : null}
        </div>
      ) : null}

      {recoveryCodes.length > 0 ? (
        <div className="mt-6 rounded-[2rem] border border-amber-100 bg-amber-50 p-6">
          <div className="flex items-center gap-3 mb-4">
            <AlertTriangle size={18} className="text-amber-600" />
            <p className="text-sm font-black text-amber-900">Lưu lại các mã dự phòng này ở nơi an toàn</p>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            {recoveryCodes.map((item) => (
              <div key={item} className="rounded-2xl bg-white border border-amber-100 px-4 py-3 font-black text-slate-900">
                {item}
              </div>
            ))}
          </div>
          <div className="mt-4 flex items-center gap-2 text-xs font-bold text-amber-800">
            <CheckCircle size={14} />
            Mỗi mã chỉ dùng được một lần.
          </div>
        </div>
      ) : null}
    </div>
  );
}
