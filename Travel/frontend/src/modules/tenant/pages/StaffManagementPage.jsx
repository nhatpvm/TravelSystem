import React, { useEffect, useMemo, useState } from 'react';
import { motion } from 'framer-motion';
import { Users, Plus, Search, Edit2, ShieldCheck, Award, Phone, Mail, Lock, Unlock } from 'lucide-react';
import {
  createTenantStaff,
  deactivateTenantStaff,
  listTenantStaff,
  restoreTenantStaff,
  updateTenantStaff,
} from '../../../services/tenancyService';

const ROLE_STYLES = {
  manager: { label: 'Quản lý', color: 'bg-purple-100 text-purple-700' },
  accountant: { label: 'Kế toán', color: 'bg-blue-100 text-blue-700' },
  ops: { label: 'Vận hành', color: 'bg-emerald-100 text-emerald-700' },
  support: { label: 'CSKH', color: 'bg-amber-100 text-amber-700' },
  ticket: { label: 'Đại lý vé', color: 'bg-rose-100 text-rose-700' },
};

const EMPTY_FORM = {
  fullName: '',
  email: '',
  phoneNumber: '',
  password: '',
  roleCode: 'manager',
  isOwner: false,
};

function getRoleMeta(roleCode, roleOptions) {
  const style = ROLE_STYLES[roleCode] || ROLE_STYLES.manager;
  const option = roleOptions.find((item) => item.code === roleCode);

  return {
    label: option?.label || style.label,
    color: style.color,
    permissions: option?.permissions || [],
  };
}

function getInitial(name) {
  const displayName = (name || '').trim();
  if (!displayName) {
    return 'N';
  }

  const parts = displayName.split(/\s+/);
  return parts[parts.length - 1][0]?.toUpperCase() || 'N';
}

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

function formatRelative(value) {
  if (!value) {
    return 'Chưa có dữ liệu';
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return 'Chưa có dữ liệu';
  }

  const diffMinutes = Math.max(0, Math.round((Date.now() - date.getTime()) / 60000));

  if (diffMinutes < 1) {
    return 'Vừa xong';
  }

  if (diffMinutes < 60) {
    return `${diffMinutes} phút trước`;
  }

  const diffHours = Math.round(diffMinutes / 60);
  if (diffHours < 24) {
    return `${diffHours} giờ trước`;
  }

  const diffDays = Math.round(diffHours / 24);
  if (diffDays < 7) {
    return `${diffDays} ngày trước`;
  }

  return formatDate(value);
}

function buildFormFromStaff(staff) {
  return {
    fullName: staff?.name || '',
    email: staff?.email || '',
    phoneNumber: staff?.phone || '',
    password: '',
    roleCode: staff?.roleCode || 'manager',
    isOwner: !!staff?.isOwner,
  };
}

export default function StaffManagementPage() {
  const [search, setSearch] = useState('');
  const [roleFilter, setRoleFilter] = useState('all');
  const [staff, setStaff] = useState([]);
  const [roleOptions, setRoleOptions] = useState([]);
  const [stats, setStats] = useState({ total: 0, active: 0, inactive: 0, roles: 0 });
  const [selectedId, setSelectedId] = useState('');
  const [panelMode, setPanelMode] = useState('view');
  const [form, setForm] = useState(EMPTY_FORM);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [actionLoading, setActionLoading] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  useEffect(() => {
    loadStaff();
  }, [search, roleFilter]);

  const selectedStaff = useMemo(
    () => staff.find((item) => item.id === selectedId) || null,
    [selectedId, staff],
  );

  const statCards = useMemo(() => ([
    { label: 'Tổng nhân viên', value: `${stats.total || 0}`, color: 'bg-slate-900 text-white' },
    { label: 'Đang hoạt động', value: `${stats.active || 0}`, color: 'bg-emerald-50 text-emerald-900' },
    { label: 'Tạm khóa', value: `${stats.inactive || 0}`, color: 'bg-rose-50 text-rose-900' },
    { label: 'Vai trò', value: `${stats.roles || roleOptions.length}`, color: 'bg-blue-50 text-blue-900' },
  ]), [roleOptions.length, stats]);

  async function loadStaff() {
    setLoading(true);
    setError('');

    try {
      const response = await listTenantStaff({
        q: search,
        roleCode: roleFilter === 'all' ? undefined : roleFilter,
        includeInactive: true,
      });

      setStaff(response.items || []);
      setRoleOptions(response.roleOptions || []);
      setStats({
        total: response.stats?.total || 0,
        active: response.stats?.active || 0,
        inactive: response.stats?.inactive || 0,
        roles: response.stats?.roles || 0,
      });
    } catch (err) {
      setError(err.message || 'Không thể tải danh sách nhân viên.');
      setStaff([]);
      setRoleOptions([]);
      setStats({ total: 0, active: 0, inactive: 0, roles: 0 });
    } finally {
      setLoading(false);
    }
  }

  function startCreate() {
    const defaultRole = roleOptions[0]?.code || 'manager';
    setPanelMode('create');
    setSelectedId('');
    setForm({ ...EMPTY_FORM, roleCode: defaultRole });
    setError('');
    setNotice('');
  }

  function startEdit() {
    if (!selectedStaff) {
      return;
    }

    setPanelMode('edit');
    setForm(buildFormFromStaff(selectedStaff));
    setError('');
    setNotice('');
  }

  function cancelEditor() {
    setPanelMode('view');
    setForm(EMPTY_FORM);
  }

  function updateField(key, value) {
    setForm((prev) => ({ ...prev, [key]: value }));
  }

  async function handleSubmit() {
    setSaving(true);
    setError('');
    setNotice('');

    try {
      if (panelMode === 'create') {
        const response = await createTenantStaff(form);
        setNotice('Nhân viên mới đã được tạo thành công.');
        setSelectedId(response.id || '');
      } else if (selectedStaff) {
        await updateTenantStaff(selectedStaff.id, form);
        setNotice('Thông tin nhân viên đã được cập nhật.');
      }

      setPanelMode('view');
      setForm(EMPTY_FORM);
      await loadStaff();
    } catch (err) {
      setError(err.message || 'Không thể lưu thông tin nhân viên.');
    } finally {
      setSaving(false);
    }
  }

  async function handleToggleStatus() {
    if (!selectedStaff) {
      return;
    }

    setActionLoading(true);
    setError('');
    setNotice('');

    try {
      if (selectedStaff.active) {
        await deactivateTenantStaff(selectedStaff.id);
        setNotice('Nhân viên đã được tạm khóa.');
      } else {
        await restoreTenantStaff(selectedStaff.id);
        setNotice('Nhân viên đã được mở khóa.');
      }

      await loadStaff();
    } catch (err) {
      setError(err.message || 'Không thể cập nhật trạng thái nhân viên.');
    } finally {
      setActionLoading(false);
    }
  }

  return (
    <div>
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-8">
        <div>
          <h1 className="text-2xl font-black text-slate-900">Quản lý Nhân viên</h1>
          <p className="text-slate-500 text-sm mt-1">Phân công vai trò và quyền hạn trong hệ thống</p>
        </div>
        <button onClick={startCreate} className="flex items-center gap-2 px-6 py-3 bg-slate-900 text-white rounded-2xl font-bold text-sm hover:bg-blue-600 transition-all shadow-lg">
          <Plus size={16} /> Thêm nhân viên
        </button>
      </div>

      {notice && (
        <div className="mb-6 rounded-2xl border border-emerald-100 bg-emerald-50 px-5 py-4 text-sm font-bold text-emerald-700">
          {notice}
        </div>
      )}

      {error && (
        <div className="mb-6 rounded-2xl border border-rose-100 bg-rose-50 px-5 py-4 text-sm font-bold text-rose-600">
          {error}
        </div>
      )}

      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        {statCards.map((item, index) => (
          <motion.div
            key={item.label}
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: index * 0.07 }}
            className={`rounded-2xl p-5 shadow-sm border border-slate-100 ${item.color}`}
          >
            <p className="text-3xl font-black">{item.value}</p>
            <p className="text-[10px] font-bold uppercase tracking-widest mt-1 opacity-60">{item.label}</p>
          </motion.div>
        ))}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2">
          <div className="bg-white rounded-2xl p-4 shadow-sm border border-slate-100 mb-4 flex flex-wrap gap-3">
            <div className="flex-1 min-w-40 flex items-center gap-2 bg-slate-50 rounded-xl px-4">
              <Search size={15} className="text-slate-400" />
              <input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Tìm nhân viên…" className="bg-transparent py-3 flex-1 text-sm font-medium outline-none" />
            </div>
            <select
              value={roleFilter}
              onChange={(e) => setRoleFilter(e.target.value)}
              className="px-4 py-2 bg-slate-50 rounded-xl text-sm font-bold text-slate-700 border-none outline-none cursor-pointer"
            >
              <option value="all">Tất cả vai trò</option>
              {roleOptions.map((item) => (
                <option key={item.code} value={item.code}>{item.label}</option>
              ))}
            </select>
          </div>

          <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
            <div className="divide-y divide-slate-50">
              {loading ? (
                <div className="px-5 py-8 text-sm font-bold text-slate-400">Đang tải danh sách nhân viên...</div>
              ) : staff.length === 0 ? (
                <div className="px-5 py-8 text-sm font-bold text-slate-400">Chưa có nhân viên phù hợp.</div>
              ) : staff.map((item) => {
                const meta = getRoleMeta(item.roleCode, roleOptions);
                return (
                  <div
                    key={item.id}
                    onClick={() => {
                      setPanelMode('view');
                      setSelectedId(selectedId === item.id ? '' : item.id);
                    }}
                    className={`flex items-center gap-4 px-5 py-4 cursor-pointer transition-all hover:bg-slate-50 ${selectedId === item.id && panelMode === 'view' ? 'bg-blue-50 border-l-4 border-blue-500' : ''}`}
                  >
                    <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-slate-200 to-slate-300 flex items-center justify-center font-black text-slate-700 shrink-0">
                      {getInitial(item.name)}
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2">
                        <p className="font-black text-slate-900 text-sm">{item.name}</p>
                        <div className={`w-2 h-2 rounded-full ${item.active ? 'bg-emerald-400' : 'bg-slate-300'}`} />
                      </div>
                      <p className="text-xs text-slate-400 font-bold truncate">{item.email}</p>
                    </div>
                    <div className="flex items-center gap-2 shrink-0">
                      <span className={`px-2.5 py-1 rounded-xl text-[10px] font-black uppercase tracking-wide ${meta.color}`}>{meta.label}</span>
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        </div>

        <div className="lg:col-span-1">
          {(panelMode === 'create' || panelMode === 'edit') ? (
            <motion.div initial={{ opacity: 0, x: 16 }} animate={{ opacity: 1, x: 0 }} className="bg-white rounded-2xl shadow-sm border border-slate-100 p-6 sticky top-6">
              <div className="text-center mb-6">
                <div className="w-16 h-16 rounded-2xl bg-gradient-to-br from-slate-900 to-blue-600 flex items-center justify-center font-black text-white text-2xl mx-auto mb-3">
                  {panelMode === 'create' ? <Plus size={24} /> : <Edit2 size={20} />}
                </div>
                <p className="font-black text-slate-900">{panelMode === 'create' ? 'Thêm nhân viên mới' : 'Cập nhật nhân viên'}</p>
                <span className="inline-block mt-1 px-3 py-1 rounded-xl text-[10px] font-black uppercase bg-slate-100 text-slate-500">
                  {panelMode === 'create' ? 'Tài khoản tenant' : 'Chỉnh sửa hồ sơ'}
                </span>
              </div>

              <div className="space-y-3 mb-6">
                <div>
                  <label className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2 block">Họ và tên</label>
                  <input value={form.fullName} onChange={(e) => updateField('fullName', e.target.value)} className="w-full bg-slate-50 rounded-xl px-4 py-3 text-sm font-medium outline-none" />
                </div>
                <div>
                  <label className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2 block">Email</label>
                  <input value={form.email} onChange={(e) => updateField('email', e.target.value)} className="w-full bg-slate-50 rounded-xl px-4 py-3 text-sm font-medium outline-none" />
                </div>
                <div>
                  <label className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2 block">Số điện thoại</label>
                  <input value={form.phoneNumber} onChange={(e) => updateField('phoneNumber', e.target.value)} className="w-full bg-slate-50 rounded-xl px-4 py-3 text-sm font-medium outline-none" />
                </div>
                {panelMode === 'create' && (
                  <div>
                    <label className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2 block">Mật khẩu khởi tạo</label>
                    <input type="password" value={form.password} onChange={(e) => updateField('password', e.target.value)} className="w-full bg-slate-50 rounded-xl px-4 py-3 text-sm font-medium outline-none" />
                  </div>
                )}
              </div>

              <div className="mb-6">
                <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-3 flex items-center gap-2"><ShieldCheck size={12} />Vai trò</p>
                <div className="flex flex-wrap gap-2">
                  {roleOptions.map((item) => {
                    const meta = getRoleMeta(item.code, roleOptions);
                    const active = form.roleCode === item.code;
                    return (
                      <button
                        key={item.code}
                        onClick={() => updateField('roleCode', item.code)}
                        className={`px-3 py-2 rounded-xl text-[10px] font-black uppercase tracking-widest transition-all ${active ? 'bg-slate-900 text-white' : meta.color}`}
                      >
                        {item.label}
                      </button>
                    );
                  })}
                </div>
              </div>

              <div className="mb-6">
                <button
                  onClick={() => updateField('isOwner', !form.isOwner)}
                  className={`px-4 py-3 rounded-xl text-[10px] font-black uppercase tracking-widest transition-all border ${form.isOwner ? 'bg-amber-50 text-amber-700 border-amber-100' : 'bg-slate-50 text-slate-500 border-slate-100'}`}
                >
                  {form.isOwner ? 'Đang là chủ tenant' : 'Gán quyền chủ tenant'}
                </button>
              </div>

              <div className="grid grid-cols-2 gap-2">
                <button onClick={cancelEditor} className="flex items-center justify-center gap-2 py-3 bg-slate-50 hover:bg-slate-100 text-slate-600 rounded-xl text-[11px] font-black uppercase tracking-widest transition-all border border-slate-100">
                  Hủy
                </button>
                <button onClick={handleSubmit} disabled={saving} className="flex items-center justify-center gap-2 py-3 bg-slate-900 hover:bg-blue-600 text-white rounded-xl text-[11px] font-black uppercase tracking-widest transition-all border border-slate-900 disabled:opacity-70">
                  {saving ? 'Đang lưu' : panelMode === 'create' ? 'Tạo mới' : 'Lưu thay đổi'}
                </button>
              </div>
            </motion.div>
          ) : selectedStaff ? (
            <motion.div initial={{ opacity: 0, x: 16 }} animate={{ opacity: 1, x: 0 }} className="bg-white rounded-2xl shadow-sm border border-slate-100 p-6 sticky top-6">
              <div className="text-center mb-6">
                <div className="w-16 h-16 rounded-2xl bg-gradient-to-br from-blue-400 to-purple-500 flex items-center justify-center font-black text-white text-2xl mx-auto mb-3">
                  {getInitial(selectedStaff.name)}
                </div>
                <p className="font-black text-slate-900">{selectedStaff.name}</p>
                <span className={`inline-block mt-1 px-3 py-1 rounded-xl text-[10px] font-black uppercase ${getRoleMeta(selectedStaff.roleCode, roleOptions).color}`}>
                  {getRoleMeta(selectedStaff.roleCode, roleOptions).label}
                </span>
              </div>

              <div className="space-y-3 mb-6">
                <div className="flex items-center gap-3 text-sm text-slate-600">
                  <Mail size={14} className="text-slate-400" /> {selectedStaff.email}
                </div>
                <div className="flex items-center gap-3 text-sm text-slate-600">
                  <Phone size={14} className="text-slate-400" /> {selectedStaff.phone || 'Chưa cập nhật'}
                </div>
                <div className="flex items-center gap-3 text-xs text-slate-400 font-bold">
                  <Award size={14} /> Từ ngày {formatDate(selectedStaff.joinedAt)}
                </div>
                <div className="text-xs text-slate-400 font-bold ml-5">Online lần cuối: {formatRelative(selectedStaff.lastActivityAt)}</div>
              </div>

              <div className="mb-6">
                <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-3 flex items-center gap-2"><ShieldCheck size={12} />Quyền hạn</p>
                <div className="space-y-1.5">
                  {(selectedStaff.permissions || []).map((permission) => (
                    <div key={permission} className="flex items-center gap-2 text-xs font-bold text-slate-600">
                      <div className="w-1.5 h-1.5 rounded-full bg-emerald-400" />
                      {permission}
                    </div>
                  ))}
                </div>
              </div>

              <div className="grid grid-cols-2 gap-2">
                <button onClick={startEdit} className="flex items-center justify-center gap-2 py-3 bg-slate-50 hover:bg-blue-50 text-slate-600 hover:text-blue-600 rounded-xl text-[11px] font-black uppercase tracking-widest transition-all border border-slate-100 hover:border-blue-100">
                  <Edit2 size={13} /> Sửa
                </button>
                <button onClick={handleToggleStatus} disabled={actionLoading} className={`flex items-center justify-center gap-2 py-3 rounded-xl text-[11px] font-black uppercase tracking-widest transition-all border disabled:opacity-70 ${selectedStaff.active ? 'bg-rose-50 text-rose-600 border-rose-100 hover:bg-rose-100' : 'bg-emerald-50 text-emerald-600 border-emerald-100 hover:bg-emerald-100'}`}>
                  {selectedStaff.active ? <><Lock size={12} /> Khóa</> : <><Unlock size={12} /> Mở khóa</>}
                </button>
              </div>
            </motion.div>
          ) : (
            <div className="bg-white rounded-2xl shadow-sm border border-slate-100 p-8 text-center text-slate-400">
              <Users size={32} className="mx-auto mb-3 opacity-30" />
              <p className="font-bold text-sm">Chọn nhân viên để xem chi tiết</p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
