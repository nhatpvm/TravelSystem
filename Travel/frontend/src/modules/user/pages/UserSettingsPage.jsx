import React, { useEffect, useState } from 'react';
import { motion } from 'framer-motion';
import { Bell, Check, Globe, Moon, Palette, Settings, Sun } from 'lucide-react';
import {
  getCustomerAccountPreferences,
  updateCustomerAccountPreferences,
} from '../../../services/customerCommerceService';
import { getStoredCustomerPreferences, saveCustomerPreferences } from '../../../services/customerPreferences';

const LANGUAGES = [
  { code: 'vi', label: 'Tiếng Việt' },
  { code: 'en', label: 'English' },
  { code: 'zh-CN', label: '中文' },
  { code: 'ko', label: '한국어' },
  { code: 'th', label: 'ภาษาไทย' },
];

const CURRENCIES = [
  { code: 'VND', label: 'VND (₫)' },
  { code: 'USD', label: 'USD ($)' },
  { code: 'EUR', label: 'EUR (€)' },
  { code: 'THB', label: 'THB (฿)' },
  { code: 'KRW', label: 'KRW (₩)' },
];

const DEFAULT_FORM = {
  languageCode: 'vi',
  currencyCode: 'VND',
  themeMode: 'light',
  emailNotificationsEnabled: true,
  smsNotificationsEnabled: false,
  pushNotificationsEnabled: true,
};

function Toggle({ checked, onChange }) {
  return (
    <button
      type="button"
      onClick={() => onChange(!checked)}
      className={`relative inline-flex h-6 w-12 rounded-full transition-all ${checked ? 'bg-[#1EB4D4]' : 'bg-slate-200'}`}
    >
      <span
        className={`absolute left-0.5 top-0.5 h-5 w-5 rounded-full bg-white shadow transition-transform ${checked ? 'translate-x-6' : ''}`}
      />
    </button>
  );
}

export default function UserSettingsPage() {
  const [form, setForm] = useState(() => ({
    ...DEFAULT_FORM,
    ...getStoredCustomerPreferences(),
  }));
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  useEffect(() => {
    let active = true;

    getCustomerAccountPreferences()
      .then((response) => {
        if (active) {
          setForm(saveCustomerPreferences(response));
        }
      })
      .catch((requestError) => {
        if (active) {
          setError(requestError.message || 'Không thể tải cài đặt tài khoản.');
        }
      })
      .finally(() => {
        if (active) {
          setLoading(false);
        }
      });

    return () => {
      active = false;
    };
  }, []);

  function updateField(key, value) {
    setForm((current) => ({ ...current, [key]: value }));
    setSuccess('');
  }

  async function handleSave() {
    setSaving(true);
    setError('');
    setSuccess('');

    try {
      const response = await updateCustomerAccountPreferences(form);
      setForm(saveCustomerPreferences(response));
      setSuccess('Cài đặt tài khoản đã được lưu.');
    } catch (requestError) {
      setError(requestError.message || 'Không thể lưu cài đặt tài khoản.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <motion.div
      initial={{ opacity: 0, y: 24 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.5 }}
      className="space-y-6"
    >
      <div className="bg-white rounded-[2.5rem] p-8 shadow-xl shadow-slate-100/60">
        <div className="flex items-center gap-2 mb-1">
          <Settings size={14} className="text-[#1EB4D4]" />
          <span className="text-[10px] font-black text-[#1EB4D4] uppercase tracking-[0.25em]">Tùy chỉnh</span>
        </div>
        <h1 className="text-3xl font-black text-slate-900">Cài đặt tài khoản</h1>
        <p className="text-slate-400 text-sm mt-1">
          Đồng bộ ngôn ngữ, tiền tệ và cách bạn nhận thông báo trên toàn bộ trải nghiệm đặt dịch vụ.
        </p>
      </div>

      {loading ? (
        <div className="bg-white rounded-[2.5rem] p-12 shadow-xl shadow-slate-100/60 text-center text-sm font-bold text-slate-400">
          Đang tải cài đặt tài khoản...
        </div>
      ) : null}

      {error ? (
        <div className="rounded-[1.5rem] border border-rose-100 bg-rose-50 px-5 py-4 text-sm font-bold text-rose-600">
          {error}
        </div>
      ) : null}

      {success ? (
        <div className="rounded-[1.5rem] border border-emerald-100 bg-emerald-50 px-5 py-4 text-sm font-bold text-emerald-700">
          {success}
        </div>
      ) : null}

      <div className="bg-white rounded-[2.5rem] p-8 shadow-xl shadow-slate-100/60">
        <div className="flex items-center gap-2 mb-6">
          <Globe size={16} className="text-[#1EB4D4]" />
          <h2 className="font-black text-slate-900 text-sm uppercase tracking-widest">Ngôn ngữ và tiền tệ</h2>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div>
            <label className="mb-3 ml-1 block text-[10px] font-black uppercase tracking-[0.2em] text-slate-400">
              Ngôn ngữ hiển thị
            </label>
            <div className="space-y-2">
              {LANGUAGES.map((item) => (
                <button
                  key={item.code}
                  type="button"
                  onClick={() => updateField('languageCode', item.code)}
                  className={`w-full flex items-center justify-between rounded-2xl border px-5 py-3.5 text-sm font-bold transition-all ${
                    form.languageCode === item.code
                      ? 'border-[#1EB4D4] bg-[#1EB4D4]/5 text-[#1EB4D4]'
                      : 'border-slate-100 bg-slate-50 text-slate-700 hover:border-slate-200'
                  }`}
                >
                  {item.label}
                  {form.languageCode === item.code ? <Check size={16} /> : null}
                </button>
              ))}
            </div>
          </div>

          <div>
            <label className="mb-3 ml-1 block text-[10px] font-black uppercase tracking-[0.2em] text-slate-400">
              Đơn vị tiền tệ
            </label>
            <div className="space-y-2">
              {CURRENCIES.map((item) => (
                <button
                  key={item.code}
                  type="button"
                  onClick={() => updateField('currencyCode', item.code)}
                  className={`w-full flex items-center justify-between rounded-2xl border px-5 py-3.5 text-sm font-bold transition-all ${
                    form.currencyCode === item.code
                      ? 'border-[#1EB4D4] bg-[#1EB4D4]/5 text-[#1EB4D4]'
                      : 'border-slate-100 bg-slate-50 text-slate-700 hover:border-slate-200'
                  }`}
                >
                  {item.label}
                  {form.currencyCode === item.code ? <Check size={16} /> : null}
                </button>
              ))}
            </div>
          </div>
        </div>
      </div>

      <div className="bg-white rounded-[2.5rem] p-8 shadow-xl shadow-slate-100/60">
        <div className="flex items-center gap-2 mb-6">
          <Palette size={16} className="text-[#1EB4D4]" />
          <h2 className="font-black text-slate-900 text-sm uppercase tracking-widest">Giao diện</h2>
        </div>

        <div className="flex items-center justify-between rounded-2xl bg-slate-50 p-5">
          <div className="flex items-center gap-4">
            <div className={`w-11 h-11 rounded-xl flex items-center justify-center ${form.themeMode === 'dark' ? 'bg-slate-800' : 'bg-amber-50'}`}>
              {form.themeMode === 'dark' ? <Moon size={20} className="text-white" /> : <Sun size={20} className="text-amber-500" />}
            </div>
            <div>
              <p className="font-black text-slate-900">{form.themeMode === 'dark' ? 'Chế độ tối' : 'Chế độ sáng'}</p>
              <p className="text-xs font-bold text-slate-400">
                Giao diện hiện tại sẽ được lưu cho các lần truy cập tiếp theo.
              </p>
            </div>
          </div>

          <Toggle
            checked={form.themeMode === 'dark'}
            onChange={(value) => updateField('themeMode', value ? 'dark' : 'light')}
          />
        </div>
      </div>

      <div className="bg-white rounded-[2.5rem] p-8 shadow-xl shadow-slate-100/60">
        <div className="flex items-center gap-2 mb-6">
          <Bell size={16} className="text-[#1EB4D4]" />
          <h2 className="font-black text-slate-900 text-sm uppercase tracking-widest">Thông báo</h2>
        </div>

        <div className="space-y-4">
          {[
            {
              key: 'emailNotificationsEnabled',
              label: 'Thông báo qua email',
              sub: 'Nhận xác nhận đơn hàng, thanh toán và cập nhật hậu mãi qua email.',
            },
            {
              key: 'smsNotificationsEnabled',
              label: 'Thông báo qua SMS',
              sub: 'Nhận nhắc lịch khởi hành hoặc thay đổi quan trọng qua điện thoại.',
            },
            {
              key: 'pushNotificationsEnabled',
              label: 'Thông báo trên thiết bị',
              sub: 'Nhận cập nhật tức thời khi vé, hoàn tiền hoặc thanh toán thay đổi trạng thái.',
            },
          ].map((item) => (
            <div key={item.key} className="flex items-center justify-between rounded-2xl bg-slate-50 p-5">
              <div>
                <p className="text-sm font-black text-slate-900">{item.label}</p>
                <p className="mt-0.5 text-xs text-slate-500">{item.sub}</p>
              </div>
              <Toggle
                checked={form[item.key]}
                onChange={(value) => updateField(item.key, value)}
              />
            </div>
          ))}
        </div>
      </div>

      <div className="flex justify-end">
        <button
          type="button"
          onClick={handleSave}
          disabled={saving || loading}
          className="px-10 py-5 rounded-[1.5rem] bg-gradient-to-r from-[#1EB4D4] to-[#002B7F] text-white font-black text-sm uppercase tracking-widest shadow-xl transition-all hover:-translate-y-0.5 disabled:opacity-70 disabled:hover:translate-y-0"
        >
          {saving ? 'Đang lưu cài đặt...' : 'Lưu thay đổi'}
        </button>
      </div>
    </motion.div>
  );
}
