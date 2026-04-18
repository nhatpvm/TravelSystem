import React, { useEffect, useState } from 'react';
import { motion } from 'framer-motion';
import { Plane, Camera, MapPin, Edit2, User, Mail, Phone, Calendar, Star, Globe, Award, Compass } from 'lucide-react';
import { getMe, updateMe } from '../../../services/auth';
import { useAuthSession } from '../../auth/hooks/useAuthSession';
import { getUserDisplayName, getUserJoinYear } from '../../auth/types';
import { listCustomerOrders, listWishlistItems } from '../../../services/customerCommerceService';

const DEFAULT_AVATAR = 'https://images.unsplash.com/photo-1539571696357-5a69c17a67c6?auto=format&fit=crop&q=80&w=300';

const ProfilePage = () => {
  const { user } = useAuthSession();
  const [editing, setEditing] = useState(false);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [stats, setStats] = useState(() => ({
    trips: 0,
    destinationCount: 0,
    wishlistCount: 0,
    primaryDestination: '',
  }));
  const [form, setForm] = useState(() => buildFormState(user));

  useEffect(() => {
    setForm((prev) => ({
      ...prev,
      ...buildFormState(user),
      birthDate: prev.birthDate || '',
    }));
  }, [user]);

  useEffect(() => {
    let active = true;

    const loadProfile = async () => {
      setLoading(true);
      setError('');

      try {
        const [response, ordersResponse, wishlistResponse] = await Promise.all([
          getMe(),
          listCustomerOrders({ page: 1, pageSize: 100 }),
          listWishlistItems(),
        ]);

        if (!active) {
          return;
        }

        setForm((prev) => ({
          ...prev,
          ...buildFormState(response),
          birthDate: prev.birthDate || '',
        }));

        const orderItems = Array.isArray(ordersResponse?.items) ? ordersResponse.items : [];
        const wishlistItems = Array.isArray(wishlistResponse) ? wishlistResponse : [];
        const destinations = extractDestinations(orderItems);
        setStats({
          trips: orderItems.length,
          destinationCount: destinations.length,
          wishlistCount: wishlistItems.length,
          primaryDestination: destinations[0] || '',
        });
      } catch (err) {
        if (active) {
          setError(err.message || 'Không thể tải hồ sơ cá nhân.');
        }
      } finally {
        if (active) {
          setLoading(false);
        }
      }
    };

    loadProfile();

    return () => {
      active = false;
    };
  }, []);

  const handleChange = (key, value) => {
    setForm((prev) => ({ ...prev, [key]: value }));
  };

  const handleCancel = () => {
    setEditing(false);
    setError('');
    setSuccess('');
    setForm((prev) => ({
      ...prev,
      ...buildFormState(user),
      birthDate: prev.birthDate || '',
    }));
  };

  const handleSave = async () => {
    setError('');
    setSuccess('');
    setSaving(true);

    try {
      const response = await updateMe({
        fullName: form.fullName,
        email: form.email,
        phoneNumber: form.phoneNumber,
        avatarUrl: form.avatarUrl,
      });

      setForm((prev) => ({
        ...prev,
        ...buildFormState(response),
        birthDate: prev.birthDate || '',
      }));
      setEditing(false);
      setSuccess(response.emailConfirmed ? 'Thông tin cá nhân đã được cập nhật.' : 'Thông tin đã được cập nhật. Email mới đang ở trạng thái chưa xác minh.');
    } catch (err) {
      setError(err.message || 'Không thể lưu thông tin cá nhân.');
    } finally {
      setSaving(false);
    }
  };

  const displayName = form.fullName || getUserDisplayName(user);
  const joinYear = getUserJoinYear({ createdAt: form.createdAt || user?.createdAt });

  return (
    <motion.div
      initial={{ opacity: 0, y: 24 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.55, ease: [0.22, 1, 0.36, 1] }}
      className="space-y-6"
    >
      <div className="relative rounded-[2.5rem] overflow-hidden shadow-2xl shadow-sky-200/40">
        <img
          src="https://images.unsplash.com/photo-1476514525535-07fb3b4ae5f1?auto=format&fit=crop&q=80&w=1600"
          alt="travel banner"
          className="absolute inset-0 w-full h-full object-cover"
        />
        <div className="absolute inset-0 bg-gradient-to-b from-slate-900/30 via-slate-900/50 to-slate-900/90" />

        <div className="absolute top-6 right-8 opacity-20 rotate-12">
          <Plane size={56} className="text-white" />
        </div>
        <div className="absolute top-16 right-24 opacity-10 -rotate-6">
          <Plane size={32} className="text-white" />
        </div>

        <div className="relative z-10 p-8 lg:p-12 pt-16">
          <div className="inline-flex items-center gap-2 px-4 py-2 bg-amber-400/20 border border-amber-400/40 rounded-full backdrop-blur-sm mb-8">
            <Star size={14} className="text-amber-300" fill="currentColor" />
            <span className="text-amber-200 text-[10px] font-black uppercase tracking-widest">Hồ sơ khách hàng</span>
          </div>

          <div className="flex flex-col md:flex-row items-start gap-8">
            <div className="relative group shrink-0">
              <div className="w-28 h-28 rounded-3xl overflow-hidden border-4 border-white/30 shadow-2xl ring-4 ring-white/10">
                <img
                  src={form.avatarUrl || user?.avatarUrl || DEFAULT_AVATAR}
                  alt="Avatar"
                  className="w-full h-full object-cover group-hover:scale-110 transition-transform duration-500"
                />
              </div>
              <button type="button" className="absolute -bottom-2 -right-2 w-10 h-10 bg-[#1EB4D4] rounded-xl flex items-center justify-center text-white shadow-lg">
                <Camera size={16} />
              </button>
            </div>

            <div className="flex-1 text-white">
              <h1 className="text-3xl md:text-4xl font-black tracking-tight mb-2">{displayName}</h1>
              <div className="flex flex-wrap items-center gap-4 text-white/70 text-sm font-medium">
                <span className="flex items-center gap-1.5"><MapPin size={14} className="text-[#1EB4D4]" /> {stats.primaryDestination || 'Chưa có điểm đến gần đây'}</span>
                <span className="flex items-center gap-1.5"><Globe size={14} className="text-[#1EB4D4]" /> {joinYear ? `Tham gia từ ${joinYear}` : 'Thành viên mới'}</span>
              </div>
            </div>

            <button
              onClick={() => {
                setEditing((value) => !value);
                setSuccess('');
                setError('');
              }}
              className="shrink-0 flex items-center gap-2 px-6 py-3 bg-white/10 hover:bg-white/20 backdrop-blur-sm text-white border border-white/20 rounded-2xl font-bold text-sm transition-all"
            >
              <Edit2 size={16} /> {editing ? 'Xem hồ sơ' : 'Chỉnh sửa'}
            </button>
          </div>

          <div className="grid grid-cols-3 gap-4 mt-10">
            {[
              { icon: <Plane size={18} />, label: 'Chuyến đi', value: String(stats.trips) },
              { icon: <Award size={18} />, label: 'Yêu thích', value: String(stats.wishlistCount) },
              { icon: <Compass size={18} />, label: 'Điểm đến', value: String(stats.destinationCount) },
            ].map((item, index) => (
              <div key={index} className="bg-white/10 border border-white/15 backdrop-blur-sm rounded-2xl p-4 text-center hover:bg-white/20 transition-all">
                <div className="flex justify-center text-[#1EB4D4] mb-2">{item.icon}</div>
                <p className="text-2xl font-black text-white">{item.value}</p>
                <p className="text-[10px] font-bold text-white/50 uppercase tracking-widest mt-0.5">{item.label}</p>
              </div>
            ))}
          </div>
        </div>
      </div>

      <div className="bg-white rounded-[2.5rem] shadow-xl shadow-slate-100/60 p-8">
        <div className="flex items-center gap-3 mb-8">
          <div className="w-10 h-10 bg-[#1EB4D4]/10 rounded-xl flex items-center justify-center text-[#1EB4D4]">
            <User size={20} />
          </div>
          <div>
            <h2 className="text-xl font-black text-slate-900 tracking-tight">Thông tin cá nhân</h2>
            <p className="text-xs text-slate-400 font-bold uppercase tracking-widest">Dùng cho đặt vé & tour du lịch</p>
          </div>
        </div>

        {loading && (
          <div className="mb-6 rounded-[1.5rem] border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-500">
            Đang đồng bộ hồ sơ...
          </div>
        )}

        {error && (
          <div className="mb-6 rounded-[1.5rem] border border-rose-100 bg-rose-50 px-5 py-4 text-sm font-bold text-rose-600">
            {error}
          </div>
        )}

        {success && (
          <div className="mb-6 rounded-[1.5rem] border border-emerald-100 bg-emerald-50 px-5 py-4 text-sm font-bold text-emerald-700">
            {success}
          </div>
        )}

        {!loading && form.email && form.emailConfirmed === false && (
          <div className="mb-6 rounded-[1.5rem] border border-amber-100 bg-amber-50 px-5 py-4 text-sm font-bold text-amber-700">
            Email hiện tại chưa được xác minh. Bạn vẫn có thể cập nhật hồ sơ, nhưng nên xác nhận địa chỉ email trước khi dùng cho các luồng khôi phục tài khoản.
          </div>
        )}

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {[
            { key: 'fullName', label: 'Họ và tên', value: form.fullName, icon: <User size={18} />, type: 'text', editable: true },
            { key: 'email', label: 'Email', value: form.email, icon: <Mail size={18} />, type: 'email', editable: true },
            { key: 'phoneNumber', label: 'Số điện thoại', value: form.phoneNumber, icon: <Phone size={18} />, type: 'tel', editable: true },
            { key: 'birthDate', label: 'Ngày sinh', value: form.birthDate || 'Chưa cập nhật', icon: <Calendar size={18} />, type: 'text', editable: false },
          ].map((item, index) => (
            <div key={index} className="space-y-2">
              <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em]">{item.label}</label>
              <div className="relative group">
                <span className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-300 group-focus-within:text-[#1EB4D4] transition-colors">{item.icon}</span>
                <input
                  type={item.type}
                  value={item.value}
                  onChange={(event) => handleChange(item.key, event.target.value)}
                  disabled={!editing || !item.editable}
                  className="w-full bg-slate-50 border-2 border-transparent focus:border-[#1EB4D4]/30 focus:bg-white disabled:opacity-70 rounded-2xl py-4 pl-12 pr-4 font-bold text-slate-900 text-sm focus:ring-0 transition-all"
                />
              </div>
            </div>
          ))}
        </div>

        {editing && (
          <motion.div
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            className="flex gap-3 mt-8 pt-6 border-t border-slate-100"
          >
            <button onClick={handleCancel} className="px-8 py-4 rounded-2xl font-bold text-slate-500 hover:bg-slate-50 transition-all text-sm">Hủy</button>
            <button
              onClick={handleSave}
              disabled={saving}
              className="flex-1 py-4 bg-gradient-to-r from-[#1EB4D4] to-[#002B7F] text-white rounded-2xl font-black text-sm shadow-xl shadow-blue-500/25 hover:shadow-blue-500/40 hover:-translate-y-0.5 transition-all active:translate-y-0 disabled:opacity-70 disabled:hover:translate-y-0"
            >
              {saving ? 'Đang lưu...' : 'Lưu thông tin ✓'}
            </button>
          </motion.div>
        )}
      </div>
    </motion.div>
  );
};

function buildFormState(source) {
  return {
    fullName: source?.fullName || '',
    email: source?.email || '',
    emailConfirmed: source?.emailConfirmed ?? true,
    phoneNumber: source?.phoneNumber || '',
    avatarUrl: source?.avatarUrl || '',
    createdAt: source?.createdAt || '',
    birthDate: '',
  };
}

function extractDestinations(orderItems) {
  const destinationSet = new Set();

  orderItems.forEach((item) => {
    const snapshot = item?.snapshot || {};
    const values = [
      snapshot.locationText,
      snapshot.routeTo,
      snapshot.routeFrom,
      snapshot.title,
    ];

    values.forEach((value) => {
      const normalized = String(value || '').trim();
      if (normalized) {
        destinationSet.add(normalized);
      }
    });
  });

  return Array.from(destinationSet).slice(0, 12);
}

export default ProfilePage;
