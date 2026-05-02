import React, { useEffect, useMemo, useState } from 'react';
import { motion } from 'framer-motion';
import { Link2, Plus, RotateCcw, Search, Shield, Trash2, Edit3 } from 'lucide-react';
import { Link } from 'react-router-dom';
import useLatestRef from '../../../shared/hooks/useLatestRef';
import {
  createRolePermission,
  deleteRolePermission,
  listPermissions,
  listRolePermissions,
  listRoles,
  restoreRolePermission,
  updateRolePermission,
} from '../../../services/adminIdentity';
import { getPermissionCategoryLabel, getPermissionLabel } from '../utils/identity';

const EMPTY_FORM = {
  roleId: '',
  permissionId: '',
};

export default function AdminRolePermissionsPage() {
  const [roles, setRoles] = useState([]);
  const [permissions, setPermissions] = useState([]);
  const [items, setItems] = useState([]);
  const [search, setSearch] = useState('');
  const [roleId, setRoleId] = useState('all');
  const [includeDeleted, setIncludeDeleted] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [form, setForm] = useState(EMPTY_FORM);
  const [editingId, setEditingId] = useState('');
  const [saving, setSaving] = useState(false);
  const [actionLoadingId, setActionLoadingId] = useState('');

  const loadItemsRef = useLatestRef(loadItems);

  useEffect(() => {
    loadBootstrap();
  }, []);

  useEffect(() => {
    loadItemsRef.current();
  }, [search, roleId, includeDeleted, loadItemsRef]);

  const stats = useMemo(() => ([
    { label: 'Tổng ánh xạ', value: items.length, color: 'bg-slate-900 text-white' },
    { label: 'Vai trò đã dùng', value: new Set(items.map((item) => item.roleId)).size, color: 'bg-white' },
    { label: 'Quyền được cấp', value: new Set(items.map((item) => item.permissionId)).size, color: 'bg-white' },
    { label: 'Đã xoá mềm', value: items.filter((item) => item.isDeleted).length, color: 'bg-rose-50' },
  ]), [items]);

  async function loadBootstrap() {
    try {
      const [rolesResponse, permissionsResponse] = await Promise.all([
        listRoles(),
        listPermissions({ page: 1, pageSize: 200, includeDeleted: false }),
      ]);

      setRoles(rolesResponse.items || []);
      setPermissions(permissionsResponse.items || []);
    } catch (err) {
      setError(err.message || 'Không thể tải dữ liệu ánh xạ vai trò.');
    }
  }

  async function loadItems() {
    setLoading(true);
    setError('');

    try {
      const response = await listRolePermissions({
        q: search,
        roleId: roleId === 'all' ? '' : roleId,
        includeDeleted,
        page: 1,
        pageSize: 300,
      });
      setItems(response.items || []);
    } catch (err) {
      setError(err.message || 'Không thể tải ánh xạ vai trò - quyền.');
    } finally {
      setLoading(false);
    }
  }

  function startCreate() {
    setEditingId('');
    setForm(EMPTY_FORM);
  }

  function startEdit(item) {
    setEditingId(item.id);
    setForm({
      roleId: item.roleId,
      permissionId: item.permissionId,
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
      if (editingId) {
        await updateRolePermission(editingId, form);
        setNotice('Ánh xạ vai trò đã được cập nhật.');
      } else {
        await createRolePermission(form);
        setNotice('Ánh xạ vai trò mới đã được tạo.');
      }

      startCreate();
      await loadItemsRef.current();
    } catch (err) {
      setError(err.message || 'Không thể lưu ánh xạ vai trò.');
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(item) {
    setActionLoadingId(item.id);
    setError('');
    setNotice('');

    try {
      await deleteRolePermission(item.id);
      setNotice('Đã xoá mềm ánh xạ vai trò.');
      await loadItemsRef.current();
    } catch (err) {
      setError(err.message || 'Không thể xoá ánh xạ vai trò.');
    } finally {
      setActionLoadingId('');
    }
  }

  async function handleRestore(item) {
    setActionLoadingId(item.id);
    setError('');
    setNotice('');

    try {
      await restoreRolePermission(item.id);
      setNotice('Ánh xạ vai trò đã được khôi phục.');
      await loadItemsRef.current();
    } catch (err) {
      setError(err.message || 'Không thể khôi phục ánh xạ vai trò.');
    } finally {
      setActionLoadingId('');
    }
  }

  return (
    <div className="space-y-8">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-black text-slate-900">Ánh xạ Vai trò - Quyền</h1>
          <p className="text-slate-500 text-sm mt-1">Quản lý từng liên kết giữa vai trò định danh và danh mục quyền</p>
        </div>
        <div className="flex flex-wrap items-center gap-3">
          <Link to="/admin/roles" className="flex items-center gap-2 px-6 py-3 bg-white text-slate-700 rounded-2xl font-bold text-sm border border-slate-100 hover:border-blue-200 hover:text-blue-600 transition-all shadow-sm">
            <Shield size={16} /> Vai trò
          </Link>
          <Link to="/admin/permissions" className="flex items-center gap-2 px-6 py-3 bg-white text-slate-700 rounded-2xl font-bold text-sm border border-slate-100 hover:border-blue-200 hover:text-blue-600 transition-all shadow-sm">
            <Link2 size={16} /> Danh mục quyền
          </Link>
          <button onClick={startCreate} className="flex items-center gap-2 px-6 py-3 bg-slate-900 text-white rounded-2xl font-bold text-sm hover:bg-blue-600 transition-all shadow-lg">
            <Plus size={16} /> Thêm ánh xạ
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
            <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] mb-2 block">Vai trò</label>
            <select value={form.roleId} onChange={(e) => updateField('roleId', e.target.value)} className="w-full bg-slate-50 border-2 border-transparent focus:border-blue-200 focus:bg-white rounded-2xl px-4 py-3.5 text-sm font-bold text-slate-900 outline-none transition-all">
              <option value="">Chọn vai trò</option>
              {roles.map((role) => (
                <option key={role.id} value={role.id}>{role.name}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] mb-2 block">Quyền</label>
            <select value={form.permissionId} onChange={(e) => updateField('permissionId', e.target.value)} className="w-full bg-slate-50 border-2 border-transparent focus:border-blue-200 focus:bg-white rounded-2xl px-4 py-3.5 text-sm font-bold text-slate-900 outline-none transition-all">
              <option value="">Chọn quyền</option>
              {permissions.map((permission) => (
                <option key={permission.id} value={permission.id}>{getPermissionLabel(permission)}</option>
              ))}
            </select>
          </div>
        </div>

        <div className="flex flex-wrap items-center justify-end gap-3 mt-6 pt-6 border-t border-slate-100">
          {editingId && (
            <button onClick={startCreate} className="px-6 py-3 rounded-2xl text-sm font-bold text-slate-500 hover:bg-slate-50 transition-all">
              Huỷ chỉnh sửa
            </button>
          )}
          <button onClick={handleSave} disabled={saving} className="px-6 py-3 bg-slate-900 text-white rounded-2xl font-bold text-sm hover:bg-blue-600 transition-all shadow-lg disabled:opacity-70">
            {saving ? 'Đang lưu...' : editingId ? 'Cập nhật ánh xạ' : 'Tạo ánh xạ'}
          </button>
        </div>
      </div>

      <div className="bg-white rounded-2xl p-4 shadow-sm border border-slate-100 flex flex-wrap gap-3">
        <div className="flex-1 min-w-40 flex items-center gap-2 bg-slate-50 rounded-xl px-4">
          <Search size={15} className="text-slate-400" />
          <input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Tìm vai trò hoặc quyền..." className="bg-transparent py-3 flex-1 text-sm font-medium outline-none" />
        </div>
        <select value={roleId} onChange={(e) => setRoleId(e.target.value)} className="bg-slate-50 border border-slate-100 rounded-xl px-4 py-3 text-[10px] font-black uppercase tracking-widest text-slate-600 outline-none">
          <option value="all">Tất cả vai trò</option>
          {roles.map((role) => (
            <option key={role.id} value={role.id}>{role.name}</option>
          ))}
        </select>
        <button onClick={() => setIncludeDeleted((value) => !value)} className={`px-4 py-3 rounded-xl text-[10px] font-black uppercase tracking-widest transition-all ${includeDeleted ? 'bg-rose-50 text-rose-700' : 'bg-slate-50 text-slate-500'}`}>
          {includeDeleted ? 'Ẩn bản ghi xoá' : 'Hiện bản ghi xoá'}
        </button>
      </div>

      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
        <div className="hidden md:grid grid-cols-12 gap-4 px-5 py-3 border-b border-slate-50 bg-slate-50 text-[10px] font-black text-slate-400 uppercase tracking-widest">
          <div className="col-span-3">Vai trò</div>
          <div className="col-span-4">Quyền</div>
          <div className="col-span-2">Nhóm</div>
          <div className="col-span-1">Trạng thái</div>
          <div className="col-span-2">Hành động</div>
        </div>
        <div className="divide-y divide-slate-50">
          {loading ? (
            <div className="px-5 py-8 text-sm font-bold text-slate-400">Đang tải ánh xạ vai trò...</div>
          ) : items.length === 0 ? (
            <div className="px-5 py-8 text-sm font-bold text-slate-400">Không có ánh xạ phù hợp.</div>
          ) : items.map((item, idx) => (
            <motion.div key={item.id} initial={{ opacity: 0 }} animate={{ opacity: 1 }} transition={{ delay: idx * 0.04 }}
              className="grid grid-cols-2 md:grid-cols-12 gap-4 px-5 py-4 items-center hover:bg-slate-50 transition-all"
            >
              <div className="col-span-1 md:col-span-3">
                <p className="font-black text-slate-900 text-sm">{item.role?.name || '--'}</p>
                <p className="text-xs text-slate-400 font-bold">{item.roleId}</p>
              </div>
              <div className="col-span-1 md:col-span-4">
                <p className="font-black text-slate-900 text-sm">{item.permission ? getPermissionLabel(item.permission) : '--'}</p>
                <p className="text-xs text-slate-400 font-bold">{item.permission?.code || item.permissionId}</p>
              </div>
              <div className="col-span-1 md:col-span-2">
                <span className="px-2.5 py-1 rounded-xl text-[10px] font-black uppercase bg-slate-100 text-slate-600">{getPermissionCategoryLabel(item.permission?.category || item.permission?.code?.split('.')[0])}</span>
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
