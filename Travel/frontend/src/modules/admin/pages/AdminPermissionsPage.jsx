import React, { useEffect, useMemo, useState } from 'react';
import { motion } from 'framer-motion';
import { Shield, Plus, Search, Edit3, RotateCcw, Trash2, Link2 } from 'lucide-react';
import { Link } from 'react-router-dom';
import {
  createPermission,
  deletePermission,
  listPermissions,
  restorePermission,
  updatePermission,
} from '../../../services/adminIdentity';
import { getPermissionCategories } from '../utils/identity';

const EMPTY_FORM = {
  code: '',
  name: '',
  description: '',
  category: '',
  sortOrder: 0,
  isActive: true,
};

export default function AdminPermissionsPage() {
  const [items, setItems] = useState([]);
  const [search, setSearch] = useState('');
  const [category, setCategory] = useState('all');
  const [includeDeleted, setIncludeDeleted] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [form, setForm] = useState(EMPTY_FORM);
  const [editingId, setEditingId] = useState('');
  const [saving, setSaving] = useState(false);
  const [actionLoadingId, setActionLoadingId] = useState('');

  useEffect(() => {
    loadPermissions();
  }, [search, category, includeDeleted]);

  const categories = useMemo(() => getPermissionCategories(items), [items]);

  const stats = useMemo(() => ([
    { label: 'Tổng quyền', value: items.length, color: 'bg-slate-900 text-white' },
    { label: 'Đang hoạt động', value: items.filter((item) => item.isActive && !item.isDeleted).length, color: 'bg-white' },
    { label: 'Nhóm quyền', value: categories.length, color: 'bg-white' },
    { label: 'Đã xoá mềm', value: items.filter((item) => item.isDeleted).length, color: 'bg-rose-50' },
  ]), [categories.length, items]);

  async function loadPermissions() {
    setLoading(true);
    setError('');

    try {
      const response = await listPermissions({
        q: search,
        category: category === 'all' ? '' : category,
        includeDeleted,
        page: 1,
        pageSize: 200,
      });
      setItems(response.items || []);
    } catch (err) {
      setError(err.message || 'Không thể tải danh mục quyền.');
    } finally {
      setLoading(false);
    }
  }

  function updateField(key, value) {
    setForm((prev) => ({ ...prev, [key]: value }));
  }

  function startCreate() {
    setEditingId('');
    setForm(EMPTY_FORM);
  }

  function startEdit(item) {
    setEditingId(item.id);
    setForm({
      code: item.code,
      name: item.name,
      description: item.description || '',
      category: item.category || '',
      sortOrder: item.sortOrder || 0,
      isActive: item.isActive,
    });
  }

  async function handleSave() {
    setSaving(true);
    setError('');
    setNotice('');

    try {
      if (editingId) {
        await updatePermission(editingId, form);
        setNotice('Quyền hạn đã được cập nhật.');
      } else {
        await createPermission(form);
        setNotice('Quyền hạn mới đã được tạo.');
      }

      startCreate();
      await loadPermissions();
    } catch (err) {
      setError(err.message || 'Không thể lưu quyền hạn.');
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(item) {
    setActionLoadingId(item.id);
    setError('');
    setNotice('');

    try {
      await deletePermission(item.id);
      setNotice('Quyền hạn đã được xoá mềm.');
      await loadPermissions();
    } catch (err) {
      setError(err.message || 'Không thể xoá quyền hạn.');
    } finally {
      setActionLoadingId('');
    }
  }

  async function handleRestore(item) {
    setActionLoadingId(item.id);
    setError('');
    setNotice('');

    try {
      await restorePermission(item.id);
      setNotice('Quyền hạn đã được khôi phục.');
      await loadPermissions();
    } catch (err) {
      setError(err.message || 'Không thể khôi phục quyền hạn.');
    } finally {
      setActionLoadingId('');
    }
  }

  return (
    <div className="space-y-8">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-black text-slate-900">Danh mục Quyền hạn</h1>
          <p className="text-slate-500 text-sm mt-1">Quản lý permission code, mô tả, nhóm chức năng và trạng thái sử dụng</p>
        </div>
        <div className="flex flex-wrap items-center gap-3">
          <Link to="/admin/role-permissions" className="flex items-center gap-2 px-6 py-3 bg-white text-slate-700 rounded-2xl font-bold text-sm border border-slate-100 hover:border-blue-200 hover:text-blue-600 transition-all shadow-sm">
            <Link2 size={16} /> Ánh xạ vai trò
          </Link>
          <button onClick={startCreate} className="flex items-center gap-2 px-6 py-3 bg-slate-900 text-white rounded-2xl font-bold text-sm hover:bg-blue-600 transition-all shadow-lg">
            <Plus size={16} /> Tạo quyền mới
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
        <div className="flex items-center gap-3 mb-6">
          <div className="w-11 h-11 rounded-2xl bg-slate-900 text-white flex items-center justify-center">
            <Shield size={18} />
          </div>
          <div>
            <h2 className="font-black text-slate-900">{editingId ? 'Cập nhật quyền hạn' : 'Tạo quyền hạn mới'}</h2>
            <p className="text-xs text-slate-400 font-bold uppercase tracking-widest mt-1">Giữ đồng bộ với permission policy backend</p>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] mb-2 block">Permission Code</label>
            <input value={form.code} onChange={(e) => updateField('code', e.target.value)} placeholder="bus.trips.read" className="w-full bg-slate-50 border-2 border-transparent focus:border-blue-200 focus:bg-white rounded-2xl px-4 py-3.5 text-sm font-bold text-slate-900 outline-none transition-all" />
          </div>
          <div>
            <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] mb-2 block">Tên hiển thị</label>
            <input value={form.name} onChange={(e) => updateField('name', e.target.value)} placeholder="Bus Trips Read" className="w-full bg-slate-50 border-2 border-transparent focus:border-blue-200 focus:bg-white rounded-2xl px-4 py-3.5 text-sm font-bold text-slate-900 outline-none transition-all" />
          </div>
          <div>
            <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] mb-2 block">Category</label>
            <input value={form.category} onChange={(e) => updateField('category', e.target.value)} placeholder="bus" className="w-full bg-slate-50 border-2 border-transparent focus:border-blue-200 focus:bg-white rounded-2xl px-4 py-3.5 text-sm font-bold text-slate-900 outline-none transition-all" />
          </div>
          <div>
            <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] mb-2 block">Sort Order</label>
            <input type="number" value={form.sortOrder} onChange={(e) => updateField('sortOrder', Number(e.target.value))} className="w-full bg-slate-50 border-2 border-transparent focus:border-blue-200 focus:bg-white rounded-2xl px-4 py-3.5 text-sm font-bold text-slate-900 outline-none transition-all" />
          </div>
          <div className="md:col-span-2">
            <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] mb-2 block">Mô tả</label>
            <textarea value={form.description} onChange={(e) => updateField('description', e.target.value)} rows={3} className="w-full resize-none bg-slate-50 border-2 border-transparent focus:border-blue-200 focus:bg-white rounded-2xl px-4 py-3.5 text-sm font-bold text-slate-900 outline-none transition-all" />
          </div>
        </div>

        <div className="flex flex-wrap items-center justify-between gap-4 mt-6 pt-6 border-t border-slate-100">
          <button
            onClick={() => updateField('isActive', !form.isActive)}
            className={`px-4 py-2 rounded-xl text-[10px] font-black uppercase tracking-widest transition-all ${form.isActive ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-500'}`}
          >
            {form.isActive ? 'Đang hoạt động' : 'Tạm ngưng'}
          </button>
          <div className="flex flex-wrap gap-3">
            {editingId && (
              <button onClick={startCreate} className="px-6 py-3 rounded-2xl text-sm font-bold text-slate-500 hover:bg-slate-50 transition-all">
                Huỷ chỉnh sửa
              </button>
            )}
            <button onClick={handleSave} disabled={saving} className="px-6 py-3 bg-slate-900 text-white rounded-2xl font-bold text-sm hover:bg-blue-600 transition-all shadow-lg disabled:opacity-70">
              {saving ? 'Đang lưu...' : editingId ? 'Cập nhật quyền' : 'Tạo quyền'}
            </button>
          </div>
        </div>
      </div>

      <div className="bg-white rounded-2xl p-4 shadow-sm border border-slate-100 flex flex-wrap gap-3">
        <div className="flex-1 min-w-40 flex items-center gap-2 bg-slate-50 rounded-xl px-4">
          <Search size={15} className="text-slate-400" />
          <input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Tìm code, tên, category..." className="bg-transparent py-3 flex-1 text-sm font-medium outline-none" />
        </div>
        <div className="flex bg-slate-50 p-1 rounded-xl border border-slate-100 gap-1">
          <button onClick={() => setCategory('all')} className={`px-3 py-2 rounded-lg text-[10px] font-black uppercase tracking-widest transition-all ${category === 'all' ? 'bg-white shadow-md text-blue-600' : 'text-slate-400 hover:text-slate-700'}`}>Tất cả</button>
          {categories.map((item) => (
            <button key={item} onClick={() => setCategory(item)} className={`px-3 py-2 rounded-lg text-[10px] font-black uppercase tracking-widest transition-all ${category === item ? 'bg-white shadow-md text-blue-600' : 'text-slate-400 hover:text-slate-700'}`}>{item}</button>
          ))}
        </div>
        <button onClick={() => setIncludeDeleted((value) => !value)} className={`px-4 py-3 rounded-xl text-[10px] font-black uppercase tracking-widest transition-all ${includeDeleted ? 'bg-rose-50 text-rose-700' : 'bg-slate-50 text-slate-500'}`}>
          {includeDeleted ? 'Ẩn bản ghi xoá' : 'Hiện bản ghi xoá'}
        </button>
      </div>

      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
        <div className="hidden md:grid grid-cols-12 gap-4 px-5 py-3 border-b border-slate-50 bg-slate-50 text-[10px] font-black text-slate-400 uppercase tracking-widest">
          <div className="col-span-3">Permission</div>
          <div className="col-span-3">Mô tả</div>
          <div className="col-span-2">Category</div>
          <div className="col-span-1">Sort</div>
          <div className="col-span-1">Trạng thái</div>
          <div className="col-span-2">Hành động</div>
        </div>
        <div className="divide-y divide-slate-50">
          {loading ? (
            <div className="px-5 py-8 text-sm font-bold text-slate-400">Đang tải quyền hạn...</div>
          ) : items.length === 0 ? (
            <div className="px-5 py-8 text-sm font-bold text-slate-400">Không có quyền hạn phù hợp.</div>
          ) : items.map((item, idx) => (
            <motion.div key={item.id} initial={{ opacity: 0 }} animate={{ opacity: 1 }} transition={{ delay: idx * 0.04 }}
              className="grid grid-cols-2 md:grid-cols-12 gap-4 px-5 py-4 items-center hover:bg-slate-50 transition-all"
            >
              <div className="col-span-2 md:col-span-3">
                <p className="font-black text-slate-900 text-sm">{item.name}</p>
                <p className="text-xs text-slate-400 font-bold">{item.code}</p>
              </div>
              <div className="col-span-2 md:col-span-3 text-sm font-bold text-slate-500">{item.description || '--'}</div>
              <div className="col-span-1 md:col-span-2">
                <span className="px-2.5 py-1 rounded-xl text-[10px] font-black uppercase bg-slate-100 text-slate-600">{item.category || 'uncategorized'}</span>
              </div>
              <div className="col-span-1 md:col-span-1 text-sm font-bold text-slate-700">{item.sortOrder}</div>
              <div className="col-span-1 md:col-span-1">
                <span className={`inline-flex items-center gap-1 px-2.5 py-1 rounded-xl text-[10px] font-black uppercase ${item.isDeleted ? 'bg-rose-100 text-rose-700' : item.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-amber-100 text-amber-700'}`}>
                  {item.isDeleted ? 'Đã xoá' : item.isActive ? 'Hoạt động' : 'Tạm ngưng'}
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
