import React, { useEffect, useMemo, useState } from 'react';
import { motion } from 'framer-motion';
import { Shield, Plus, Check, X, Trash2, Link2 } from 'lucide-react';
import { Link } from 'react-router-dom';
import {
  createRole,
  createRolePermission,
  deleteRole,
  deleteRolePermission,
  listPermissions,
  listRolePermissions,
  listRoles,
} from '../../../services/adminIdentity';
import { getPermissionCategories } from '../utils/identity';

export default function AdminRolesPage() {
  const [roles, setRoles] = useState([]);
  const [permissions, setPermissions] = useState([]);
  const [selectedRoleId, setSelectedRoleId] = useState('');
  const [rolePermissions, setRolePermissions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [showCreate, setShowCreate] = useState(false);
  const [createRoleName, setCreateRoleName] = useState('');
  const [createLoading, setCreateLoading] = useState(false);
  const [permissionLoading, setPermissionLoading] = useState(false);
  const [bulkLoading, setBulkLoading] = useState(false);
  const [deleteLoading, setDeleteLoading] = useState(false);

  useEffect(() => {
    loadBootstrap();
  }, []);

  useEffect(() => {
    if (!selectedRoleId) {
      setRolePermissions([]);
      return;
    }

    loadRolePermissions(selectedRoleId);
  }, [selectedRoleId]);

  const selectedRole = useMemo(
    () => roles.find((role) => role.id === selectedRoleId) || null,
    [roles, selectedRoleId],
  );

  const groupedPermissions = useMemo(() => {
    const categories = getPermissionCategories(permissions);

    return categories.map((category) => ({
      category,
      items: permissions.filter((permission) => permission.category === category),
    }));
  }, [permissions]);

  const assignedPermissionMap = useMemo(() => {
    const map = new Map();
    rolePermissions.forEach((item) => {
      map.set(item.permissionId, item);
    });
    return map;
  }, [rolePermissions]);

  async function loadBootstrap() {
    setLoading(true);
    setError('');

    try {
      const [rolesResponse, permissionsResponse] = await Promise.all([
        listRoles(),
        listPermissions({ page: 1, pageSize: 200, includeDeleted: false }),
      ]);

      const nextRoles = rolesResponse.items || [];
      setRoles(nextRoles);
      setPermissions(permissionsResponse.items || []);
      setSelectedRoleId((current) => current || nextRoles[0]?.id || '');
    } catch (err) {
      setError(err.message || 'Không thể tải vai trò và quyền hạn.');
    } finally {
      setLoading(false);
    }
  }

  async function loadRolePermissions(roleId) {
    setPermissionLoading(true);
    setError('');

    try {
      const response = await listRolePermissions({
        roleId,
        page: 1,
        pageSize: 300,
        includeDeleted: false,
      });
      setRolePermissions(response.items || []);
    } catch (err) {
      setError(err.message || 'Không thể tải ánh xạ quyền của vai trò.');
    } finally {
      setPermissionLoading(false);
    }
  }

  async function refreshRoles() {
    const response = await listRoles();
    const nextRoles = response.items || [];
    setRoles(nextRoles);
    setSelectedRoleId((current) => current && nextRoles.some((role) => role.id === current) ? current : nextRoles[0]?.id || '');
  }

  async function handleCreateRole() {
    if (!createRoleName) {
      return;
    }

    setCreateLoading(true);
    setError('');
    setNotice('');

    try {
      await createRole({ name: createRoleName });
      setCreateRoleName('');
      setShowCreate(false);
      setNotice('Vai trò mới đã được tạo thành công.');
      await refreshRoles();
    } catch (err) {
      setError(err.message || 'Không thể tạo vai trò mới.');
    } finally {
      setCreateLoading(false);
    }
  }

  async function handleTogglePermission(permissionId) {
    if (!selectedRoleId) {
      return;
    }

    const current = assignedPermissionMap.get(permissionId);
    setPermissionLoading(true);
    setError('');
    setNotice('');

    try {
      if (current) {
        await deleteRolePermission(current.id);
        setNotice('Đã thu hồi quyền khỏi vai trò.');
      } else {
        await createRolePermission({ roleId: selectedRoleId, permissionId });
        setNotice('Đã cấp quyền cho vai trò.');
      }

      await Promise.all([refreshRoles(), loadRolePermissions(selectedRoleId)]);
    } catch (err) {
      setError(err.message || 'Không thể cập nhật quyền của vai trò.');
    } finally {
      setPermissionLoading(false);
    }
  }

  async function handleGrantAll() {
    if (!selectedRoleId) {
      return;
    }

    setBulkLoading(true);
    setError('');
    setNotice('');

    try {
      for (const permission of permissions) {
        if (!assignedPermissionMap.has(permission.id)) {
          await createRolePermission({ roleId: selectedRoleId, permissionId: permission.id });
        }
      }

      setNotice('Đã cấp toàn bộ quyền cho vai trò.');
      await Promise.all([refreshRoles(), loadRolePermissions(selectedRoleId)]);
    } catch (err) {
      setError(err.message || 'Không thể cấp toàn bộ quyền.');
    } finally {
      setBulkLoading(false);
    }
  }

  async function handleRevokeAll() {
    if (!selectedRoleId) {
      return;
    }

    setBulkLoading(true);
    setError('');
    setNotice('');

    try {
      for (const item of rolePermissions) {
        await deleteRolePermission(item.id);
      }

      setNotice('Đã thu hồi toàn bộ quyền khỏi vai trò.');
      await Promise.all([refreshRoles(), loadRolePermissions(selectedRoleId)]);
    } catch (err) {
      setError(err.message || 'Không thể thu hồi toàn bộ quyền.');
    } finally {
      setBulkLoading(false);
    }
  }

  async function handleDeleteRole() {
    if (!selectedRole || selectedRole.isProtected) {
      return;
    }

    setDeleteLoading(true);
    setError('');
    setNotice('');

    try {
      await deleteRole(selectedRole.id);
      setNotice('Vai trò đã được xoá.');
      await refreshRoles();
    } catch (err) {
      setError(err.message || 'Không thể xoá vai trò.');
    } finally {
      setDeleteLoading(false);
    }
  }

  return (
    <div>
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-8">
        <div>
          <h1 className="text-2xl font-black text-slate-900">Vai trò & Quyền hạn</h1>
          <p className="text-slate-500 text-sm mt-1">Quản lý danh mục vai trò và ánh xạ quyền hạn hệ thống</p>
        </div>
        <div className="flex flex-wrap items-center gap-3">
          <Link to="/admin/permissions" className="flex items-center gap-2 px-6 py-3 bg-white text-slate-700 rounded-2xl font-bold text-sm border border-slate-100 hover:border-blue-200 hover:text-blue-600 transition-all shadow-sm">
            <Shield size={16} /> Danh mục quyền
          </Link>
          <Link to="/admin/role-permissions" className="flex items-center gap-2 px-6 py-3 bg-white text-slate-700 rounded-2xl font-bold text-sm border border-slate-100 hover:border-blue-200 hover:text-blue-600 transition-all shadow-sm">
            <Link2 size={16} /> Ánh xạ chi tiết
          </Link>
          <button onClick={() => setShowCreate((value) => !value)} className="flex items-center gap-2 px-6 py-3 bg-slate-900 text-white rounded-2xl font-bold text-sm hover:bg-blue-600 transition-all shadow-lg">
            <Plus size={16} /> Thêm vai trò
          </button>
        </div>
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

      {showCreate && (
        <div className="bg-white rounded-2xl shadow-sm border border-slate-100 p-6 mb-6">
          <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] mb-2 block">Tên vai trò</label>
          <div className="flex flex-wrap gap-3">
            <input
              type="text"
              value={createRoleName}
              onChange={(e) => setCreateRoleName(e.target.value)}
              placeholder="Ví dụ: Support Supervisor"
              className="flex-1 min-w-60 bg-slate-50 border-2 border-transparent focus:border-blue-200 focus:bg-white rounded-2xl px-4 py-3.5 text-sm font-bold text-slate-900 outline-none transition-all"
            />
            <button onClick={handleCreateRole} disabled={createLoading} className="px-6 py-3 bg-slate-900 text-white rounded-2xl font-bold text-sm hover:bg-blue-600 transition-all shadow-lg disabled:opacity-70">
              {createLoading ? 'Đang tạo...' : 'Lưu vai trò'}
            </button>
          </div>
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
        <div className="bg-white rounded-2xl shadow-sm border border-slate-100 p-4">
          <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-4 px-2">Danh sách vai trò</p>
          {loading ? (
            <div className="px-2 py-3 text-sm font-bold text-slate-400">Đang tải vai trò...</div>
          ) : (
            <div className="space-y-1">
              {roles.map((role) => (
                <button key={role.id} onClick={() => setSelectedRoleId(role.id)}
                  className={`w-full flex items-center justify-between px-4 py-3 rounded-xl text-sm font-bold transition-all ${selectedRoleId === role.id ? 'bg-slate-900 text-white' : 'text-slate-600 hover:bg-slate-50'}`}
                >
                  <span className="flex items-center gap-2">
                    <Shield size={14} /> {role.name}
                  </span>
                  <span className={`text-[10px] font-black px-2 py-0.5 rounded-lg ${selectedRoleId === role.id ? 'bg-white/20' : 'bg-slate-100'}`}>{role.permissionsCount} quyền</span>
                </button>
              ))}
            </div>
          )}
        </div>

        <div className="lg:col-span-3 bg-white rounded-2xl shadow-sm border border-slate-100 p-8">
          {!selectedRole ? (
            <div className="text-sm font-bold text-slate-400">Chưa có vai trò để hiển thị.</div>
          ) : (
            <>
              <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-6">
                <div>
                  <h2 className="font-black text-slate-900">Quyền hạn: {selectedRole.name}</h2>
                  <p className="text-xs text-slate-400 font-bold mt-0.5">{selectedRole.permissionsCount} / {permissions.length} quyền được cấp · {selectedRole.usersCount} người dùng</p>
                </div>
                <div className="flex flex-wrap gap-2">
                  <button onClick={handleGrantAll} disabled={bulkLoading || permissionLoading} className="px-4 py-2 text-emerald-600 bg-emerald-50 rounded-xl text-[10px] font-black uppercase tracking-widest hover:bg-emerald-100 transition-all disabled:opacity-70">
                    Cấp tất cả
                  </button>
                  <button onClick={handleRevokeAll} disabled={bulkLoading || permissionLoading} className="px-4 py-2 text-rose-600 bg-rose-50 rounded-xl text-[10px] font-black uppercase tracking-widest hover:bg-rose-100 transition-all disabled:opacity-70">
                    Thu hồi tất cả
                  </button>
                  {!selectedRole.isProtected && (
                    <button onClick={handleDeleteRole} disabled={deleteLoading} className="px-4 py-2 text-slate-600 bg-slate-100 rounded-xl text-[10px] font-black uppercase tracking-widest hover:bg-slate-200 transition-all disabled:opacity-70">
                      <Trash2 size={12} className="inline mr-1" /> Xoá vai trò
                    </button>
                  )}
                </div>
              </div>

              {permissionLoading ? (
                <div className="text-sm font-bold text-slate-400">Đang đồng bộ quyền hạn...</div>
              ) : (
                <div className="space-y-6">
                  {groupedPermissions.map((group) => (
                    <div key={group.category}>
                      <p className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] mb-3 border-b border-slate-100 pb-2">{group.category}</p>
                      <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
                        {group.items.map((perm) => {
                          const has = assignedPermissionMap.has(perm.id);
                          return (
                            <button key={perm.id} onClick={() => handleTogglePermission(perm.id)}
                              className={`flex items-center justify-between px-4 py-3 rounded-xl border transition-all ${has ? 'border-emerald-200 bg-emerald-50' : 'border-slate-100 bg-slate-50 hover:border-slate-200'}`}
                            >
                              <div className="text-left">
                                <span className={`text-sm font-bold ${has ? 'text-emerald-700' : 'text-slate-500'}`}>{perm.name}</span>
                                <p className={`text-[10px] font-black uppercase tracking-widest mt-1 ${has ? 'text-emerald-500' : 'text-slate-300'}`}>{perm.code}</p>
                              </div>
                              <span className={`w-5 h-5 rounded-md flex items-center justify-center ${has ? 'bg-emerald-500' : 'bg-slate-200'}`}>
                                {has ? <Check size={12} className="text-white" /> : <X size={12} className="text-slate-400" />}
                              </span>
                            </button>
                          );
                        })}
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </>
          )}
        </div>
      </div>
    </div>
  );
}
