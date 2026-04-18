import React, { useEffect, useMemo, useState } from 'react';
import { motion } from 'framer-motion';
import { Link2, Plus, RotateCcw, Search, Shield, Trash2, Edit3, UserCog } from 'lucide-react';
import { Link, useSearchParams } from 'react-router-dom';
import {
  createUserPermission,
  deleteUserPermission,
  listAdminTenants,
  listPermissions,
  listUserPermissions,
  listUsers,
  restoreUserPermission,
  updateUserPermission,
} from '../../../services/adminIdentity';
import { EFFECT_BADGE_MAP } from '../utils/identity';

const EMPTY_FORM = {
  userId: '',
  permissionId: '',
  tenantId: '',
  effect: 'Allow',
  reason: '',
};

export default function AdminUserPermissionsPage() {
  const [searchParams] = useSearchParams();
  const initialUserId = searchParams.get('userId') || 'all';
  const [users, setUsers] = useState([]);
  const [permissions, setPermissions] = useState([]);
  const [tenants, setTenants] = useState([]);
  const [items, setItems] = useState([]);
  const [search, setSearch] = useState('');
  const [userFilter, setUserFilter] = useState(initialUserId);
  const [effectFilter, setEffectFilter] = useState('all');
  const [includeDeleted, setIncludeDeleted] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [form, setForm] = useState(() => ({
    ...EMPTY_FORM,
    userId: initialUserId !== 'all' ? initialUserId : '',
  }));
  const [editingId, setEditingId] = useState('');
  const [saving, setSaving] = useState(false);
  const [actionLoadingId, setActionLoadingId] = useState('');

  useEffect(() => {
    loadBootstrap();
  }, []);

  useEffect(() => {
    loadItems();
  }, [search, userFilter, effectFilter, includeDeleted]);

  const stats = useMemo(() => ([
    { label: 'Tổng override', value: items.length, color: 'bg-slate-900 text-white' },
    { label: 'Allow', value: items.filter((item) => item.effect === 'Allow').length, color: 'bg-white' },
    { label: 'Deny', value: items.filter((item) => item.effect === 'Deny').length, color: 'bg-white' },
    { label: 'Theo tenant', value: items.filter((item) => !!item.tenantId).length, color: 'bg-amber-50' },
  ]), [items]);

  async function loadBootstrap() {
    try {
      const [usersResponse, permissionsResponse, tenantsResponse] = await Promise.all([
        listUsers({ page: 1, pageSize: 200, includeInactive: true }),
        listPermissions({ page: 1, pageSize: 200, includeDeleted: false }),
        listAdminTenants({ page: 1, pageSize: 200, includeDeleted: false }),
      ]);

      setUsers(usersResponse.items || []);
      setPermissions(permissionsResponse.items || []);
      setTenants(tenantsResponse.items || []);
    } catch (err) {
      setError(err.message || 'Không thể tải dữ liệu quyền người dùng.');
    }
  }

  async function loadItems() {
    setLoading(true);
    setError('');

    try {
      const response = await listUserPermissions({
        q: search,
        userId: userFilter === 'all' ? '' : userFilter,
        effect: effectFilter === 'all' ? '' : effectFilter,
        includeDeleted,
        page: 1,
        pageSize: 300,
      });
      setItems(response.items || []);
    } catch (err) {
      setError(err.message || 'Không thể tải user permission override.');
    } finally {
      setLoading(false);
    }
  }

  function startCreate() {
    setEditingId('');
    setForm({
      ...EMPTY_FORM,
      userId: userFilter !== 'all' ? userFilter : '',
    });
  }

  function startEdit(item) {
    setEditingId(item.id);
    setForm({
      userId: item.userId,
      permissionId: item.permissionId,
      tenantId: item.tenantId || '',
      effect: item.effect,
      reason: item.reason || '',
    });
  }

  function updateField(key, value) {
    setForm((prev) => ({ ...prev, [key]: value }));
  }

  async function handleSave() {
    setSaving(true);
    setError('');
    setNotice('');

    try {
      const payload = {
        userId: form.userId,
        permissionId: form.permissionId,
        tenantId: form.tenantId || null,
        effect: form.effect,
        reason: form.reason,
      };

      if (editingId) {
        await updateUserPermission(editingId, payload);
        setNotice('Override quyền người dùng đã được cập nhật.');
      } else {
        await createUserPermission(payload);
        setNotice('Override quyền người dùng mới đã được tạo.');
      }

      startCreate();
      await loadItems();
    } catch (err) {
      setError(err.message || 'Không thể lưu quyền người dùng.');
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(item) {
    setActionLoadingId(item.id);
    setError('');
    setNotice('');

    try {
      await deleteUserPermission(item.id);
      setNotice('Override đã được xoá mềm.');
      await loadItems();
    } catch (err) {
      setError(err.message || 'Không thể xoá override.');
    } finally {
      setActionLoadingId('');
    }
  }

  async function handleRestore(item) {
    setActionLoadingId(item.id);
    setError('');
    setNotice('');

    try {
      await restoreUserPermission(item.id);
      setNotice('Override đã được khôi phục.');
      await loadItems();
    } catch (err) {
      setError(err.message || 'Không thể khôi phục override.');
    } finally {
      setActionLoadingId('');
    }
  }

  return (
    <div className="space-y-8">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-black text-slate-900">Quyền Người dùng</h1>
          <p className="text-slate-500 text-sm mt-1">Quản lý override Allow/Deny trực tiếp cho từng tài khoản, có thể theo tenant scope</p>
        </div>
        <div className="flex flex-wrap items-center gap-3">
          <Link to="/admin/users" className="flex items-center gap-2 px-6 py-3 bg-white text-slate-700 rounded-2xl font-bold text-sm border border-slate-100 hover:border-blue-200 hover:text-blue-600 transition-all shadow-sm">
            <UserCog size={16} /> Người dùng
          </Link>
          <Link to="/admin/permissions" className="flex items-center gap-2 px-6 py-3 bg-white text-slate-700 rounded-2xl font-bold text-sm border border-slate-100 hover:border-blue-200 hover:text-blue-600 transition-all shadow-sm">
            <Link2 size={16} /> Permission catalog
          </Link>
          <button onClick={startCreate} className="flex items-center gap-2 px-6 py-3 bg-slate-900 text-white rounded-2xl font-bold text-sm hover:bg-blue-600 transition-all shadow-lg">
            <Plus size={16} /> Thêm override
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

      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        {stats.map((s, i) => (
          <motion.div key={s.label} initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: i * 0.07 }}
            className={`rounded-2xl p-5 shadow-sm border border-slate-100 ${s.color}`}
          >
            <p className={`text-3xl font-black ${s.color === 'bg-slate-900 text-white' ? 'text-white' : 'text-slate-900'}`}>{s.value}</p>
            <p className={`text-[10px] font-bold uppercase tracking-widest mt-1 ${s.color === 'bg-slate-900 text-white' ? 'text-white/60' : 'text-slate-400'}`}>{s.label}</p>
          </motion.div>
        ))}
      </div>

      <div className="bg-white rounded-[2rem] border border-slate-100 shadow-sm p-6">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] mb-2 block">Người dùng</label>
            <select value={form.userId} onChange={(e) => updateField('userId', e.target.value)} className="w-full bg-slate-50 border-2 border-transparent focus:border-blue-200 focus:bg-white rounded-2xl px-4 py-3.5 text-sm font-bold text-slate-900 outline-none transition-all">
              <option value="">Chọn người dùng</option>
              {users.map((user) => (
                <option key={user.id} value={user.id}>{user.fullName || user.userName} - {user.email}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] mb-2 block">Permission</label>
            <select value={form.permissionId} onChange={(e) => updateField('permissionId', e.target.value)} className="w-full bg-slate-50 border-2 border-transparent focus:border-blue-200 focus:bg-white rounded-2xl px-4 py-3.5 text-sm font-bold text-slate-900 outline-none transition-all">
              <option value="">Chọn permission</option>
              {permissions.map((permission) => (
                <option key={permission.id} value={permission.id}>{permission.code}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] mb-2 block">Tenant scope</label>
            <select value={form.tenantId} onChange={(e) => updateField('tenantId', e.target.value)} className="w-full bg-slate-50 border-2 border-transparent focus:border-blue-200 focus:bg-white rounded-2xl px-4 py-3.5 text-sm font-bold text-slate-900 outline-none transition-all">
              <option value="">Global override</option>
              {tenants.map((tenant) => (
                <option key={tenant.id} value={tenant.id}>{tenant.code} - {tenant.name}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] mb-2 block">Effect</label>
            <div className="flex bg-slate-50 p-1 rounded-xl border border-slate-100 gap-1">
              {['Allow', 'Deny'].map((effect) => (
                <button key={effect} onClick={() => updateField('effect', effect)} className={`flex-1 px-3 py-3 rounded-lg text-[10px] font-black uppercase tracking-widest transition-all ${form.effect === effect ? 'bg-white shadow-md text-blue-600' : 'text-slate-400 hover:text-slate-700'}`}>
                  {effect}
                </button>
              ))}
            </div>
          </div>
          <div className="md:col-span-2">
            <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] mb-2 block">Lý do</label>
            <textarea value={form.reason} onChange={(e) => updateField('reason', e.target.value)} rows={3} className="w-full resize-none bg-slate-50 border-2 border-transparent focus:border-blue-200 focus:bg-white rounded-2xl px-4 py-3.5 text-sm font-bold text-slate-900 outline-none transition-all" />
          </div>
        </div>

        <div className="flex flex-wrap items-center justify-end gap-3 mt-6 pt-6 border-t border-slate-100">
          {editingId && (
            <button onClick={startCreate} className="px-6 py-3 rounded-2xl text-sm font-bold text-slate-500 hover:bg-slate-50 transition-all">
              Huỷ chỉnh sửa
            </button>
          )}
          <button onClick={handleSave} disabled={saving} className="px-6 py-3 bg-slate-900 text-white rounded-2xl font-bold text-sm hover:bg-blue-600 transition-all shadow-lg disabled:opacity-70">
            {saving ? 'Đang lưu...' : editingId ? 'Cập nhật override' : 'Tạo override'}
          </button>
        </div>
      </div>

      <div className="bg-white rounded-2xl p-4 shadow-sm border border-slate-100 flex flex-wrap gap-3">
        <div className="flex-1 min-w-40 flex items-center gap-2 bg-slate-50 rounded-xl px-4">
          <Search size={15} className="text-slate-400" />
          <input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Tìm user, permission, tenant..." className="bg-transparent py-3 flex-1 text-sm font-medium outline-none" />
        </div>
        <select value={userFilter} onChange={(e) => setUserFilter(e.target.value)} className="bg-slate-50 border border-slate-100 rounded-xl px-4 py-3 text-[10px] font-black uppercase tracking-widest text-slate-600 outline-none">
          <option value="all">Tất cả user</option>
          {users.map((user) => (
            <option key={user.id} value={user.id}>{user.fullName || user.userName}</option>
          ))}
        </select>
        <div className="flex bg-slate-50 p-1 rounded-xl border border-slate-100 gap-1">
          {['all', 'Allow', 'Deny'].map((effect) => (
            <button key={effect} onClick={() => setEffectFilter(effect)} className={`px-3 py-2 rounded-lg text-[10px] font-black uppercase tracking-widest transition-all ${effectFilter === effect ? 'bg-white shadow-md text-blue-600' : 'text-slate-400 hover:text-slate-700'}`}>
              {effect === 'all' ? 'Tất cả' : effect}
            </button>
          ))}
        </div>
        <button onClick={() => setIncludeDeleted((value) => !value)} className={`px-4 py-3 rounded-xl text-[10px] font-black uppercase tracking-widest transition-all ${includeDeleted ? 'bg-rose-50 text-rose-700' : 'bg-slate-50 text-slate-500'}`}>
          {includeDeleted ? 'Ẩn bản ghi xoá' : 'Hiện bản ghi xoá'}
        </button>
      </div>

      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
        <div className="hidden md:grid grid-cols-12 gap-4 px-5 py-3 border-b border-slate-50 bg-slate-50 text-[10px] font-black text-slate-400 uppercase tracking-widest">
          <div className="col-span-3">Người dùng</div>
          <div className="col-span-3">Permission</div>
          <div className="col-span-2">Scope</div>
          <div className="col-span-1">Effect</div>
          <div className="col-span-1">Trạng thái</div>
          <div className="col-span-2">Hành động</div>
        </div>
        <div className="divide-y divide-slate-50">
          {loading ? (
            <div className="px-5 py-8 text-sm font-bold text-slate-400">Đang tải quyền người dùng...</div>
          ) : items.length === 0 ? (
            <div className="px-5 py-8 text-sm font-bold text-slate-400">Không có override phù hợp.</div>
          ) : items.map((item, idx) => (
            <motion.div key={item.id} initial={{ opacity: 0 }} animate={{ opacity: 1 }} transition={{ delay: idx * 0.04 }}
              className="grid grid-cols-2 md:grid-cols-12 gap-4 px-5 py-4 items-center hover:bg-slate-50 transition-all"
            >
              <div className="col-span-2 md:col-span-3">
                <p className="font-black text-slate-900 text-sm">{item.user?.fullName || item.user?.userName || '--'}</p>
                <p className="text-xs text-slate-400 font-bold">{item.user?.email || item.userId}</p>
              </div>
              <div className="col-span-1 md:col-span-3">
                <p className="font-black text-slate-900 text-sm">{item.permission?.name || '--'}</p>
                <p className="text-xs text-slate-400 font-bold">{item.permission?.code || item.permissionId}</p>
              </div>
              <div className="col-span-1 md:col-span-2">
                <span className="px-2.5 py-1 rounded-xl text-[10px] font-black uppercase bg-slate-100 text-slate-600">{item.tenant?.code || 'global'}</span>
              </div>
              <div className="col-span-1 md:col-span-1">
                <span className={`inline-flex items-center gap-1 px-2.5 py-1 rounded-xl text-[10px] font-black uppercase ${EFFECT_BADGE_MAP[item.effect] || 'bg-slate-100 text-slate-600'}`}>
                  {item.effect}
                </span>
              </div>
              <div className="col-span-1 md:col-span-1">
                <span className={`inline-flex items-center gap-1 px-2.5 py-1 rounded-xl text-[10px] font-black uppercase ${item.isDeleted ? 'bg-rose-100 text-rose-700' : 'bg-emerald-100 text-emerald-700'}`}>
                  {item.isDeleted ? 'Đã xoá' : 'Hiệu lực'}
                </span>
              </div>
              <div className="col-span-2 md:col-span-2 flex gap-2">
                <button onClick={() => startEdit(item)} className="p-2 text-slate-400 hover:text-blue-600 hover:bg-blue-50 rounded-xl transition-all" title="Chỉnh sửa"><Edit3 size={14} /></button>
                {item.isDeleted ? (
                  <button onClick={() => handleRestore(item)} disabled={actionLoadingId === item.id} className="p-2 text-slate-400 hover:text-emerald-600 hover:bg-emerald-50 rounded-xl transition-all" title="Khôi phục"><RotateCcw size={14} /></button>
                ) : (
                  <button onClick={() => handleDelete(item)} disabled={actionLoadingId === item.id} className="p-2 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-xl transition-all" title="Xoá mềm"><Trash2 size={14} /></button>
                )}
              </div>
            </motion.div>
          ))}
        </div>
      </div>
    </div>
  );
}
