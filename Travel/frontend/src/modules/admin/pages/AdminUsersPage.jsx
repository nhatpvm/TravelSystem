import React, { useEffect, useMemo, useState } from 'react';
import { Users, Search, Plus, Lock, Unlock, RotateCcw, Eye, Shield, UserPlus } from 'lucide-react';
import { Link, useSearchParams } from 'react-router-dom';
import AdminImageUploadField from '../components/AdminImageUploadField';
import {
  createUser,
  getUser,
  listRoles,
  listUsers,
  lockUser,
  resetUserPassword,
  setUserActive,
  setUserRoles,
  unlockUser,
  updateUser,
} from '../../../services/adminIdentity';
import { uploadAdminImage } from '../../../services/adminUploadService';
import { formatDate, getRoleBadgeClass, getUserRoleType, getUserStatus } from '../utils/identity';
import useLatestRef from '../../../shared/hooks/useLatestRef';

const INITIAL_CREATE_FORM = {
  userName: '',
  email: '',
  password: '',
  fullName: '',
  phoneNumber: '',
  avatarUrl: '',
  isActive: true,
  roles: [],
};

const INITIAL_EDIT_FORM = {
  userName: '',
  fullName: '',
  email: '',
  phoneNumber: '',
  avatarUrl: '',
  emailConfirmed: true,
};

function buildEditForm(user) {
  return {
    userName: user?.userName || '',
    fullName: user?.fullName || '',
    email: user?.email || '',
    phoneNumber: user?.phoneNumber || '',
    avatarUrl: user?.avatarUrl || '',
    emailConfirmed: user?.emailConfirmed ?? true,
  };
}

export default function AdminUsersPage() {
  const [searchParams] = useSearchParams();
  const [users, setUsers] = useState([]);
  const [roles, setRoles] = useState([]);
  const [search, setSearch] = useState(() => searchParams.get('q') || '');
  const [roleFilter, setRoleFilter] = useState('all');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [showCreate, setShowCreate] = useState(false);
  const [createForm, setCreateForm] = useState(INITIAL_CREATE_FORM);
  const [createLoading, setCreateLoading] = useState(false);
  const [selectedUserId, setSelectedUserId] = useState('');
  const [selectedUser, setSelectedUser] = useState(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [editingUser, setEditingUser] = useState(false);
  const [editForm, setEditForm] = useState(INITIAL_EDIT_FORM);
  const [savingProfile, setSavingProfile] = useState(false);
  const [savingRoles, setSavingRoles] = useState(false);
  const [resetPasswordValue, setResetPasswordValue] = useState('');
  const [resetLoading, setResetLoading] = useState(false);
  const [actionLoading, setActionLoading] = useState(false);
  const [createAvatarUploading, setCreateAvatarUploading] = useState(false);
  const [editAvatarUploading, setEditAvatarUploading] = useState(false);

  const loadUsersRef = useLatestRef(loadUsers);

  useEffect(() => {
    loadRoles();
  }, []);

  useEffect(() => {
    setSearch(searchParams.get('q') || '');
  }, [searchParams]);

  useEffect(() => {
    loadUsersRef.current();
  }, [loadUsersRef, search]);

  useEffect(() => {
    if (!selectedUserId) {
      setSelectedUser(null);
      setEditingUser(false);
      setEditForm(INITIAL_EDIT_FORM);
      return;
    }

    loadUserDetail(selectedUserId);
  }, [selectedUserId]);

  const filteredUsers = useMemo(() => {
    if (roleFilter === 'all') {
      return users;
    }

    return users.filter((user) => getUserRoleType(user) === roleFilter);
  }, [roleFilter, users]);

  const stats = useMemo(() => ([
    { label: 'Tổng người dùng', value: users.length, color: 'bg-slate-900 text-white' },
    { label: 'Khách hàng', value: users.filter((user) => getUserRoleType(user) === 'customer').length, color: 'bg-white' },
    { label: 'Tenant', value: users.filter((user) => getUserRoleType(user) === 'tenant').length, color: 'bg-white' },
    { label: 'Đang khoá', value: users.filter((user) => getUserStatus(user) === 'locked').length, color: 'bg-rose-50' },
  ]), [users]);

  async function loadUsers() {
    setLoading(true);
    setError('');

    try {
      const response = await listUsers({
        q: search,
        page: 1,
        pageSize: 100,
        includeInactive: true,
      });
      setUsers(response.items || []);
    } catch (err) {
      setError(err.message || 'Không thể tải danh sách người dùng.');
    } finally {
      setLoading(false);
    }
  }

  async function loadRoles() {
    try {
      const response = await listRoles();
      setRoles(response.items || []);
    } catch (err) {
      setError(err.message || 'Không thể tải danh sách vai trò.');
    }
  }

  async function loadUserDetail(userId) {
    setDetailLoading(true);
    setError('');

    try {
      const response = await getUser(userId);
      const normalizedUser = {
        ...response,
        roles: response.roles || [],
      };
      setSelectedUser(normalizedUser);
      setEditForm(buildEditForm(normalizedUser));
      setEditingUser(false);
    } catch (err) {
      setError(err.message || 'Không thể tải chi tiết người dùng.');
    } finally {
      setDetailLoading(false);
    }
  }

  function updateCreateField(key, value) {
    setCreateForm((prev) => ({ ...prev, [key]: value }));
  }

  function updateEditField(key, value) {
    setEditForm((prev) => ({ ...prev, [key]: value }));
  }

  function toggleCreateRole(roleName) {
    setCreateForm((prev) => ({
      ...prev,
      roles: prev.roles.includes(roleName)
        ? prev.roles.filter((item) => item !== roleName)
        : [...prev.roles, roleName],
    }));
  }

  function toggleSelectedRole(roleName) {
    setSelectedUser((prev) => ({
      ...prev,
      roles: prev.roles.includes(roleName)
        ? prev.roles.filter((item) => item !== roleName)
        : [...prev.roles, roleName],
    }));
  }

  async function handleCreateUser() {
    setCreateLoading(true);
    setError('');
    setNotice('');

    try {
      await createUser(createForm);
      setNotice('Người dùng mới đã được tạo thành công.');
      setCreateForm(INITIAL_CREATE_FORM);
      setShowCreate(false);
      await loadUsersRef.current();
    } catch (err) {
      setError(err.message || 'Không thể tạo người dùng.');
    } finally {
      setCreateLoading(false);
    }
  }

  async function handleSaveProfile() {
    if (!selectedUser) {
      return;
    }

    setSavingProfile(true);
    setError('');
    setNotice('');

    try {
      const response = await updateUser(selectedUser.id, {
        userName: editForm.userName,
        fullName: editForm.fullName,
        email: editForm.email,
        phoneNumber: editForm.phoneNumber,
        avatarUrl: editForm.avatarUrl || null,
        emailConfirmed: editForm.emailConfirmed,
        isActive: selectedUser.isActive,
      });

      const normalizedUser = {
        ...selectedUser,
        ...response,
        roles: response.roles || selectedUser.roles || [],
      };

      setSelectedUser(normalizedUser);
      setEditForm(buildEditForm(normalizedUser));
      setEditingUser(false);
      setNotice('Thông tin người dùng đã được cập nhật.');
      await loadUsersRef.current();
    } catch (err) {
      setError(err.message || 'Không thể cập nhật thông tin người dùng.');
    } finally {
      setSavingProfile(false);
    }
  }

  async function handleSaveRoles() {
    if (!selectedUser) {
      return;
    }

    setSavingRoles(true);
    setError('');
    setNotice('');

    try {
      const response = await setUserRoles(selectedUser.id, selectedUser.roles);
      setSelectedUser((prev) => ({ ...prev, roles: response.roles || prev.roles }));
      setNotice('Vai trò người dùng đã được cập nhật.');
      await loadUsersRef.current();
    } catch (err) {
      setError(err.message || 'Không thể cập nhật vai trò.');
    } finally {
      setSavingRoles(false);
    }
  }

  async function handleLockToggle(user) {
    setActionLoading(true);
    setError('');
    setNotice('');

    try {
      if (getUserStatus(user) === 'locked') {
        await unlockUser(user.id);
        setNotice('Đã mở khoá người dùng.');
      } else {
        await lockUser(user.id, 15);
        setNotice('Đã khoá người dùng trong 15 phút.');
      }

      await loadUsersRef.current();
      if (selectedUserId === user.id) {
        await loadUserDetail(selectedUserId);
      }
    } catch (err) {
      setError(err.message || 'Không thể cập nhật trạng thái khoá.');
    } finally {
      setActionLoading(false);
    }
  }

  async function handleActiveToggle() {
    if (!selectedUser) {
      return;
    }

    setActionLoading(true);
    setError('');
    setNotice('');

    try {
      await setUserActive(selectedUser.id, !selectedUser.isActive);
      setNotice(selectedUser.isActive ? 'Người dùng đã bị vô hiệu hoá.' : 'Người dùng đã được kích hoạt lại.');
      await loadUsersRef.current();
      await loadUserDetail(selectedUser.id);
    } catch (err) {
      setError(err.message || 'Không thể cập nhật trạng thái hoạt động.');
    } finally {
      setActionLoading(false);
    }
  }

  async function handleResetPassword() {
    if (!selectedUser || !resetPasswordValue) {
      return;
    }

    setResetLoading(true);
    setError('');
    setNotice('');

    try {
      await resetUserPassword(selectedUser.id, resetPasswordValue);
      setResetPasswordValue('');
      setNotice('Mật khẩu đã được đặt lại thành công.');
    } catch (err) {
      setError(err.message || 'Không thể đặt lại mật khẩu.');
    } finally {
      setResetLoading(false);
    }
  }

  async function handleUploadCreateAvatar(file) {
    setCreateAvatarUploading(true);
    setError('');
    setNotice('');

    try {
      const response = await uploadAdminImage(file, { scope: 'user-avatar' });
      updateCreateField('avatarUrl', response?.url || '');
      setNotice('Đã tải ảnh đại diện từ máy.');
    } catch (err) {
      setError(err.message || 'Không thể tải ảnh đại diện lên.');
    } finally {
      setCreateAvatarUploading(false);
    }
  }

  async function handleUploadEditAvatar(file) {
    setEditAvatarUploading(true);
    setError('');
    setNotice('');

    try {
      const response = await uploadAdminImage(file, { scope: 'user-avatar' });
      updateEditField('avatarUrl', response?.url || '');
      setNotice('Đã tải ảnh đại diện từ máy.');
    } catch (err) {
      setError(err.message || 'Không thể tải ảnh đại diện lên.');
    } finally {
      setEditAvatarUploading(false);
    }
  }

  return (
    <div>
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-8">
        <div>
          <h1 className="text-2xl font-black text-slate-900">Quản lý Người dùng</h1>
          <p className="text-slate-500 text-sm mt-1">Khoá/mở, đặt lại mật khẩu và quản lý vai trò người dùng hệ thống</p>
        </div>
        <div className="flex flex-wrap items-center gap-3">
          <Link to="/admin/user-permissions" className="flex items-center gap-2 px-6 py-3 bg-white text-slate-700 rounded-2xl font-bold text-sm border border-slate-100 hover:border-blue-200 hover:text-blue-600 transition-all shadow-sm">
            <Shield size={16} /> Quyền người dùng
          </Link>
          <button onClick={() => setShowCreate((value) => !value)} className="flex items-center gap-2 px-6 py-3 bg-slate-900 text-white rounded-2xl font-bold text-sm hover:bg-blue-600 transition-all shadow-lg">
            <Plus size={16} /> Thêm người dùng
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
        <div className="bg-white rounded-[2rem] border border-slate-100 shadow-sm p-6 mb-8">
          <div className="flex items-center gap-3 mb-6">
            <div className="w-11 h-11 rounded-2xl bg-slate-900 text-white flex items-center justify-center">
              <UserPlus size={18} />
            </div>
            <div>
              <h2 className="font-black text-slate-900">Tạo người dùng mới</h2>
              <p className="text-xs text-slate-400 font-bold uppercase tracking-widest mt-1">Dùng cho quản trị hệ thống</p>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {[
              ['userName', 'Tên đăng nhập', 'admin_demo'],
              ['fullName', 'Họ và tên', 'Nguyễn Văn A'],
              ['email', 'Email', 'name@example.com'],
              ['phoneNumber', 'Số điện thoại', '0901 000 001'],
              ['password', 'Mật khẩu', 'Phase2@12345'],
            ].map(([key, label, placeholder]) => (
              <div key={key} className={key === 'password' ? 'md:col-span-2' : ''}>
                <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] mb-2 block">{label}</label>
                <input
                  type={key === 'password' ? 'password' : 'text'}
                  value={createForm[key]}
                  onChange={(event) => updateCreateField(key, event.target.value)}
                  placeholder={placeholder}
                  className="w-full bg-slate-50 border-2 border-transparent focus:border-blue-200 focus:bg-white rounded-2xl px-4 py-3.5 text-sm font-bold text-slate-900 outline-none transition-all"
                />
              </div>
            ))}
          </div>

          <div className="mt-5">
            <AdminImageUploadField
              label="Ảnh đại diện"
              value={createForm.avatarUrl}
              onChange={(value) => updateCreateField('avatarUrl', value)}
              onUpload={handleUploadCreateAvatar}
              uploading={createAvatarUploading}
              placeholder="URL ảnh đại diện"
              helperText="Có thể dán URL sẵn có hoặc tải ảnh trực tiếp từ máy."
              previewAlt={createForm.fullName || createForm.userName || 'Ảnh đại diện'}
            />
          </div>

          <div className="mt-5">
            <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] mb-3 block">Vai trò</label>
            <div className="flex flex-wrap gap-2">
              {roles.map((role) => (
                <button
                  key={role.id}
                  onClick={() => toggleCreateRole(role.name)}
                  className={`px-3 py-2 rounded-xl text-[10px] font-black uppercase tracking-widest transition-all ${
                    createForm.roles.includes(role.name)
                      ? 'bg-slate-900 text-white'
                      : 'bg-slate-50 text-slate-500 hover:bg-slate-100'
                  }`}
                >
                  {role.name}
                </button>
              ))}
            </div>
          </div>

          <div className="mt-5 flex items-center gap-3">
            <button
              onClick={() => updateCreateField('isActive', !createForm.isActive)}
              className={`px-4 py-2 rounded-xl text-[10px] font-black uppercase tracking-widest transition-all ${
                createForm.isActive ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-500'
              }`}
            >
              {createForm.isActive ? 'Đang kích hoạt' : 'Tạm vô hiệu'}
            </button>
          </div>

          <div className="flex flex-wrap gap-3 mt-6 pt-6 border-t border-slate-100">
            <button onClick={() => setShowCreate(false)} className="px-6 py-3 rounded-2xl text-sm font-bold text-slate-500 hover:bg-slate-50 transition-all">
              Huỷ
            </button>
            <button onClick={handleCreateUser} disabled={createLoading} className="px-6 py-3 bg-slate-900 text-white rounded-2xl font-bold text-sm hover:bg-blue-600 transition-all shadow-lg disabled:opacity-70">
              {createLoading ? 'Đang tạo...' : 'Tạo người dùng'}
            </button>
          </div>
        </div>
      )}

      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        {stats.map((item, index) => (
          <motion.div
            key={item.label}
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: index * 0.07 }}
            className={`rounded-2xl p-5 shadow-sm border border-slate-100 ${item.color}`}
          >
            <p className={`text-3xl font-black ${item.color === 'bg-slate-900 text-white' ? 'text-white' : 'text-slate-900'}`}>{item.value}</p>
            <p className={`text-[10px] font-bold uppercase tracking-widest mt-1 ${item.color === 'bg-slate-900 text-white' ? 'text-white/60' : 'text-slate-400'}`}>{item.label}</p>
          </motion.div>
        ))}
      </div>

      <div className="bg-white rounded-2xl p-4 shadow-sm border border-slate-100 mb-5 flex flex-wrap gap-3">
        <div className="flex-1 min-w-40 flex items-center gap-2 bg-slate-50 rounded-xl px-4">
          <Search size={15} className="text-slate-400" />
          <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Tên, email..." className="bg-transparent py-3 flex-1 text-sm font-medium outline-none" />
        </div>
        <div className="flex bg-slate-50 p-1 rounded-xl border border-slate-100 gap-1">
          {[
            { v: 'all', l: 'Tất cả' },
            { v: 'customer', l: 'Khách hàng' },
            { v: 'tenant', l: 'Tenant' },
            { v: 'admin', l: 'Admin' },
          ].map((item) => (
            <button
              key={item.v}
              onClick={() => setRoleFilter(item.v)}
              className={`px-3 py-2 rounded-lg text-[10px] font-black uppercase tracking-widest transition-all ${roleFilter === item.v ? 'bg-white shadow-md text-blue-600' : 'text-slate-400 hover:text-slate-700'}`}
            >
              {item.l}
            </button>
          ))}
        </div>
      </div>

      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
        <div className="hidden md:grid grid-cols-12 gap-4 px-5 py-3 border-b border-slate-50 bg-slate-50 text-[10px] font-black text-slate-400 uppercase tracking-widest">
          <div className="col-span-4">Người dùng</div>
          <div className="col-span-3">Vai trò</div>
          <div className="col-span-2">Ngày tạo</div>
          <div className="col-span-1">Trạng thái</div>
          <div className="col-span-2">Hành động</div>
        </div>
        <div className="divide-y divide-slate-50">
          {loading ? (
            <div className="px-5 py-8 text-sm font-bold text-slate-400">Đang tải người dùng...</div>
          ) : filteredUsers.length === 0 ? (
            <div className="px-5 py-8 text-sm font-bold text-slate-400">Không có người dùng phù hợp.</div>
          ) : filteredUsers.map((user, index) => {
            const userId = user.id;
            const status = getUserStatus(user);
            return (
              <motion.div
                key={userId}
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                transition={{ delay: index * 0.04 }}
                className="grid grid-cols-2 md:grid-cols-12 gap-4 px-5 py-4 items-center hover:bg-slate-50 transition-all"
              >
                <div className="col-span-2 md:col-span-4 flex items-center gap-3">
                  <div className="w-9 h-9 rounded-xl bg-gradient-to-br from-slate-200 to-slate-300 flex items-center justify-center font-black text-slate-700 shrink-0 overflow-hidden">
                    {user.avatarUrl ? (
                      <img src={user.avatarUrl} alt={user.fullName || user.userName || 'Avatar'} className="w-full h-full object-cover" />
                    ) : (
                      (user.fullName || user.userName || user.email || 'U').charAt(0).toUpperCase()
                    )}
                  </div>
                  <div>
                    <p className="font-black text-slate-900 text-sm">{user.fullName || user.userName}</p>
                    <p className="text-xs text-slate-400 font-bold">{user.email}</p>
                    <p className="text-[10px] text-slate-300 font-black uppercase tracking-widest mt-1">{user.phoneNumber || user.userName}</p>
                  </div>
                </div>
                <div className="col-span-1 md:col-span-3 flex flex-wrap gap-2">
                  {(user.roles || []).map((role) => (
                    <span key={role} className={`px-2.5 py-1 rounded-xl text-[10px] font-black uppercase ${getRoleBadgeClass(role)}`}>{role}</span>
                  ))}
                </div>
                <div className="col-span-1 md:col-span-2 text-sm font-bold text-slate-700">{formatDate(user.createdAt)}</div>
                <div className="col-span-1 md:col-span-1">
                  <span className={`inline-flex items-center gap-1 px-2.5 py-1 rounded-xl text-[10px] font-black uppercase ${
                    status === 'active'
                      ? 'bg-emerald-100 text-emerald-700'
                      : status === 'locked'
                        ? 'bg-rose-100 text-rose-700'
                        : 'bg-amber-100 text-amber-700'
                  }`}>
                    {status === 'active' ? 'Hoạt động' : status === 'locked' ? 'Bị khoá' : 'Tạm dừng'}
                  </span>
                </div>
                <div className="col-span-2 md:col-span-2 flex gap-2">
                  <button onClick={() => setSelectedUserId(userId)} className="p-2 text-slate-400 hover:text-blue-600 hover:bg-blue-50 rounded-xl transition-all" title="Xem thông tin"><Eye size={14} /></button>
                  <Link to={`/admin/user-permissions?userId=${userId}`} className="p-2 text-slate-400 hover:text-purple-600 hover:bg-purple-50 rounded-xl transition-all" title="Quyền người dùng"><Shield size={14} /></Link>
                  <button onClick={() => setSelectedUserId(userId)} className="p-2 text-slate-400 hover:text-amber-600 hover:bg-amber-50 rounded-xl transition-all" title="Đặt lại mật khẩu"><RotateCcw size={14} /></button>
                  <button onClick={() => handleLockToggle(user)} disabled={actionLoading} className={`p-2 rounded-xl transition-all ${status === 'locked' ? 'text-emerald-600 bg-emerald-50 hover:bg-emerald-100' : 'text-slate-400 hover:text-rose-600 hover:bg-rose-50'}`} title={status === 'locked' ? 'Mở khoá' : 'Khoá'}>
                    {status === 'locked' ? <Unlock size={14} /> : <Lock size={14} />}
                  </button>
                </div>
              </motion.div>
            );
          })}
        </div>
      </div>

      {selectedUserId && (
        <div className="bg-white rounded-[2rem] shadow-sm border border-slate-100 p-6 mt-6">
          {detailLoading || !selectedUser ? (
            <div className="text-sm font-bold text-slate-400">Đang tải chi tiết người dùng...</div>
          ) : (
            <>
              <div className="flex flex-col lg:flex-row justify-between gap-4 mb-6">
                <div className="flex items-start gap-4">
                  <div className="w-16 h-16 rounded-[1.5rem] bg-slate-100 overflow-hidden flex items-center justify-center text-xl font-black text-slate-700 shrink-0">
                    {selectedUser.avatarUrl ? (
                      <img src={selectedUser.avatarUrl} alt={selectedUser.fullName || selectedUser.userName || 'Avatar'} className="w-full h-full object-cover" />
                    ) : (
                      (selectedUser.fullName || selectedUser.userName || selectedUser.email || 'U').charAt(0).toUpperCase()
                    )}
                  </div>
                  <div>
                    <h2 className="text-xl font-black text-slate-900">{selectedUser.fullName || selectedUser.userName}</h2>
                    <p className="text-sm font-bold text-slate-400 mt-1">{selectedUser.email}</p>
                    <div className="flex flex-wrap gap-2 mt-3">
                      <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase bg-slate-100 text-slate-600">{selectedUser.userName}</span>
                      {selectedUser.phoneNumber && (
                        <span className="px-3 py-1 rounded-xl text-[10px] font-black uppercase bg-slate-100 text-slate-600">{selectedUser.phoneNumber}</span>
                      )}
                      <span className={`px-3 py-1 rounded-xl text-[10px] font-black uppercase ${selectedUser.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-amber-100 text-amber-700'}`}>
                        {selectedUser.isActive ? 'Đang hoạt động' : 'Đã vô hiệu'}
                      </span>
                      <span className={`px-3 py-1 rounded-xl text-[10px] font-black uppercase ${selectedUser.emailConfirmed ? 'bg-sky-100 text-sky-700' : 'bg-amber-100 text-amber-700'}`}>
                        {selectedUser.emailConfirmed ? 'Email đã xác minh' : 'Email chưa xác minh'}
                      </span>
                    </div>
                  </div>
                </div>
                <div className="flex flex-wrap gap-3">
                  <button onClick={() => setEditingUser((value) => !value)} className="px-5 py-3 rounded-2xl bg-white text-slate-700 border border-slate-100 text-sm font-bold hover:border-blue-200 hover:text-blue-600 transition-all shadow-sm">
                    {editingUser ? 'Thu gọn chỉnh sửa' : 'Chỉnh sửa thông tin'}
                  </button>
                  <button onClick={handleActiveToggle} disabled={actionLoading} className="px-5 py-3 rounded-2xl bg-slate-900 text-white text-sm font-bold hover:bg-blue-600 transition-all shadow-lg disabled:opacity-70">
                    {selectedUser.isActive ? 'Vô hiệu người dùng' : 'Kích hoạt lại'}
                  </button>
                  <button onClick={() => handleLockToggle(selectedUser)} disabled={actionLoading} className="px-5 py-3 rounded-2xl bg-white text-slate-700 border border-slate-100 text-sm font-bold hover:border-blue-200 hover:text-blue-600 transition-all shadow-sm disabled:opacity-70">
                    {getUserStatus(selectedUser) === 'locked' ? 'Mở khoá tài khoản' : 'Khoá 15 phút'}
                  </button>
                </div>
              </div>

              {editingUser && (
                <div className="rounded-2xl bg-slate-50 border border-slate-100 p-5 mb-6">
                  <div className="flex items-center justify-between gap-3 mb-4">
                    <p className="text-[10px] font-black uppercase tracking-widest text-slate-400">Thông tin cơ bản</p>
                    <button
                      onClick={() => {
                        setEditingUser(false);
                        setEditForm(buildEditForm(selectedUser));
                      }}
                      className="text-xs font-black uppercase tracking-widest text-slate-400 hover:text-slate-600"
                    >
                      Hoàn tác
                    </button>
                  </div>

                  <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
                    {[
                      ['userName', 'Tên đăng nhập', 'admin_demo'],
                      ['fullName', 'Họ và tên', 'Nguyễn Văn A'],
                      ['email', 'Email', 'name@example.com'],
                      ['phoneNumber', 'Số điện thoại', '0901 000 001'],
                    ].map(([key, label, placeholder]) => (
                      <div key={key}>
                        <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] mb-2 block">{label}</label>
                        <input
                          type={key === 'email' ? 'email' : 'text'}
                          value={editForm[key]}
                          onChange={(event) => updateEditField(key, event.target.value)}
                          placeholder={placeholder}
                          className="w-full bg-white border-2 border-transparent focus:border-blue-200 rounded-2xl px-4 py-3.5 text-sm font-bold text-slate-900 outline-none transition-all"
                        />
                      </div>
                    ))}
                  </div>

                  <div className="mt-5">
                    <AdminImageUploadField
                      label="Ảnh đại diện"
                      value={editForm.avatarUrl}
                      onChange={(value) => updateEditField('avatarUrl', value)}
                      onUpload={handleUploadEditAvatar}
                      uploading={editAvatarUploading}
                      placeholder="URL ảnh đại diện"
                      helperText="Ảnh tải lên sẽ dùng lại đúng field avatarUrl hiện có của user."
                      previewAlt={editForm.fullName || editForm.userName || 'Ảnh đại diện'}
                    />
                  </div>

                  <div className="mt-5 flex flex-wrap items-center gap-3">
                    <button
                      onClick={() => updateEditField('emailConfirmed', !editForm.emailConfirmed)}
                      className={`px-4 py-2 rounded-xl text-[10px] font-black uppercase tracking-widest transition-all ${
                        editForm.emailConfirmed ? 'bg-emerald-50 text-emerald-700' : 'bg-amber-50 text-amber-700'
                      }`}
                    >
                      {editForm.emailConfirmed ? 'Email đã xác minh' : 'Email chưa xác minh'}
                    </button>
                    <button onClick={handleSaveProfile} disabled={savingProfile} className="px-5 py-3 rounded-2xl bg-blue-600 text-white text-sm font-bold hover:bg-blue-700 transition-all shadow-lg disabled:opacity-70">
                      {savingProfile ? 'Đang lưu...' : 'Lưu thông tin'}
                    </button>
                  </div>
                </div>
              )}

              <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                <div className="rounded-2xl bg-slate-50 border border-slate-100 p-5">
                  <div className="flex items-center gap-2 mb-4">
                    <Shield size={16} className="text-blue-600" />
                    <p className="text-[10px] font-black uppercase tracking-widest text-slate-400">Vai trò người dùng</p>
                  </div>
                  <div className="flex flex-wrap gap-2">
                    {roles.map((role) => (
                      <button
                        key={role.id}
                        onClick={() => toggleSelectedRole(role.name)}
                        className={`px-3 py-2 rounded-xl text-[10px] font-black uppercase tracking-widest transition-all ${
                          selectedUser.roles.includes(role.name)
                            ? 'bg-slate-900 text-white'
                            : 'bg-white text-slate-500 border border-slate-100 hover:border-blue-200'
                        }`}
                      >
                        {role.name}
                      </button>
                    ))}
                  </div>
                  <button onClick={handleSaveRoles} disabled={savingRoles} className="mt-4 px-5 py-3 rounded-2xl bg-blue-600 text-white text-sm font-bold hover:bg-blue-700 transition-all shadow-lg disabled:opacity-70">
                    {savingRoles ? 'Đang lưu...' : 'Lưu vai trò'}
                  </button>
                </div>

                <div className="rounded-2xl bg-slate-50 border border-slate-100 p-5">
                  <div className="flex items-center gap-2 mb-4">
                    <RotateCcw size={16} className="text-amber-600" />
                    <p className="text-[10px] font-black uppercase tracking-widest text-slate-400">Đặt lại mật khẩu</p>
                  </div>
                  <input
                    type="password"
                    value={resetPasswordValue}
                    onChange={(event) => setResetPasswordValue(event.target.value)}
                    placeholder="Nhập mật khẩu mới"
                    className="w-full bg-white border-2 border-transparent focus:border-blue-200 rounded-2xl px-4 py-3.5 text-sm font-bold text-slate-900 outline-none transition-all"
                  />
                  <div className="flex flex-wrap items-center justify-between gap-3 mt-4">
                    <Link to={`/admin/user-permissions?userId=${selectedUser.id}`} className="text-xs font-black uppercase tracking-widest text-blue-600 hover:underline">
                      Quản lý quyền trực tiếp
                    </Link>
                    <button onClick={handleResetPassword} disabled={resetLoading || !resetPasswordValue} className="px-5 py-3 rounded-2xl bg-amber-500 text-white text-sm font-bold hover:bg-amber-600 transition-all shadow-lg disabled:opacity-70">
                      {resetLoading ? 'Đang đặt lại...' : 'Đặt lại mật khẩu'}
                    </button>
                  </div>
                </div>
              </div>
            </>
          )}
        </div>
      )}
    </div>
  );
}
