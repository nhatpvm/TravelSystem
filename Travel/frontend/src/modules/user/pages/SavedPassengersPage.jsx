import React, { useEffect, useMemo, useState } from 'react';
import { motion } from 'framer-motion';
import {
  Baby,
  Calendar,
  Edit2,
  IdCard,
  Mail,
  Phone,
  Plus,
  Save,
  Star,
  Trash2,
  User,
  UserPlus,
  Users,
  X,
} from 'lucide-react';
import {
  createSavedPassenger,
  deleteSavedPassenger,
  listSavedPassengers,
  updateSavedPassenger,
} from '../../../services/customerCommerceService';
import {
  CUSTOMER_PASSENGER_TYPE,
  formatCustomerPassengerTypeLabel,
  toPassengerTypeInput,
} from '../../booking/utils/customerCommerce';

const BOARD_COLORS = [
  'from-[#1EB4D4] to-[#002B7F]',
  'from-emerald-500 to-teal-700',
  'from-rose-500 to-orange-600',
];

function buildInitialForm() {
  return {
    id: '',
    fullName: '',
    passengerType: 'adult',
    gender: '',
    dateOfBirth: '',
    nationalityCode: 'VN',
    idNumber: '',
    passportNumber: '',
    email: '',
    phoneNumber: '',
    isDefault: false,
    notes: '',
  };
}

function mapPassengerToForm(passenger) {
  return {
    id: passenger.id,
    fullName: passenger.fullName || '',
    passengerType: toPassengerTypeInput(passenger.passengerType),
    gender: passenger.gender || '',
    dateOfBirth: passenger.dateOfBirth || '',
    nationalityCode: passenger.nationalityCode || 'VN',
    idNumber: passenger.idNumber || '',
    passportNumber: passenger.passportNumber || '',
    email: passenger.email || '',
    phoneNumber: passenger.phoneNumber || '',
    isDefault: !!passenger.isDefault,
    notes: passenger.notes || '',
  };
}

function getTripCountHint(passengerType) {
  switch (Number(passengerType || 0)) {
    case CUSTOMER_PASSENGER_TYPE.CHILD:
      return 'Hồ sơ trẻ em';
    case CUSTOMER_PASSENGER_TYPE.INFANT:
      return 'Hồ sơ em bé';
    default:
      return 'Hồ sơ người lớn';
  }
}

export default function SavedPassengersPage() {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [deletingId, setDeletingId] = useState('');
  const [showEditor, setShowEditor] = useState(false);
  const [form, setForm] = useState(buildInitialForm);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  async function loadPassengers() {
    setLoading(true);
    setError('');

    try {
      const response = await listSavedPassengers();
      setItems(Array.isArray(response) ? response : []);
    } catch (requestError) {
      setItems([]);
      setError(requestError.message || 'Không thể tải danh sách hành khách đã lưu.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadPassengers();
  }, []);

  const stats = useMemo(() => ({
    total: items.length,
    defaultCount: items.filter((item) => item.isDefault).length,
    childCount: items.filter((item) => Number(item.passengerType) === CUSTOMER_PASSENGER_TYPE.CHILD).length,
  }), [items]);

  function resetEditor() {
    setForm(buildInitialForm());
    setShowEditor(false);
  }

  function handleCreate() {
    setError('');
    setSuccess('');
    setForm(buildInitialForm());
    setShowEditor(true);
  }

  function handleEdit(passenger) {
    setError('');
    setSuccess('');
    setForm(mapPassengerToForm(passenger));
    setShowEditor(true);
  }

  async function handleSubmit(event) {
    event.preventDefault();

    if (!form.fullName.trim()) {
      setError('Vui lòng nhập họ và tên hành khách.');
      return;
    }

    setSaving(true);
    setError('');
    setSuccess('');

    const payload = {
      fullName: form.fullName.trim(),
      passengerType: form.passengerType,
      gender: form.gender || undefined,
      dateOfBirth: form.dateOfBirth || undefined,
      nationalityCode: form.nationalityCode || undefined,
      idNumber: form.idNumber || undefined,
      passportNumber: form.passportNumber || undefined,
      email: form.email || undefined,
      phoneNumber: form.phoneNumber || undefined,
      isDefault: form.isDefault,
      notes: form.notes || undefined,
    };

    try {
      if (form.id) {
        await updateSavedPassenger(form.id, payload);
        setSuccess('Thông tin hành khách đã được cập nhật.');
      } else {
        await createSavedPassenger(payload);
        setSuccess('Đã thêm hành khách mới vào danh sách lưu.');
      }

      resetEditor();
      await loadPassengers();
    } catch (requestError) {
      setError(requestError.message || 'Không thể lưu hành khách.');
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(id) {
    setDeletingId(id);
    setError('');
    setSuccess('');

    try {
      await deleteSavedPassenger(id);
      setSuccess('Đã xóa hành khách khỏi danh sách lưu.');
      await loadPassengers();
    } catch (requestError) {
      setError(requestError.message || 'Không thể xóa hành khách.');
    } finally {
      setDeletingId('');
    }
  }

  return (
    <motion.div
      initial={{ opacity: 0, y: 24 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.55, ease: [0.22, 1, 0.36, 1] }}
      className="space-y-6"
    >
      <div className="bg-white rounded-[2.5rem] p-8 shadow-xl shadow-slate-100/60 flex flex-col md:flex-row md:items-center justify-between gap-6">
        <div>
          <div className="flex items-center gap-2 mb-1">
            <Users size={14} className="text-[#1EB4D4]" />
            <span className="text-[10px] font-black text-[#1EB4D4] uppercase tracking-[0.25em]">Checkout Nhanh</span>
          </div>
          <h1 className="text-3xl font-black text-slate-900 tracking-tight">Hành khách đã lưu</h1>
          <p className="text-slate-400 text-sm font-medium mt-1">
            Lưu sẵn thông tin để điền nhanh ở checkout cho bus, train, flight, hotel và tour.
          </p>
        </div>
        <button
          type="button"
          onClick={handleCreate}
          className="group flex items-center gap-2 px-7 py-4 bg-[#1EB4D4] text-white rounded-2xl font-black text-xs uppercase tracking-widest shadow-xl shadow-sky-400/25 hover:bg-[#19a7c5] hover:-translate-y-0.5 transition-all active:translate-y-0"
        >
          <UserPlus size={16} className="group-hover:rotate-12 transition-transform" /> Thêm hành khách
        </button>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {[
          { label: 'Tổng hồ sơ', value: stats.total, accent: 'from-[#1EB4D4] to-[#002B7F]', dark: true },
          { label: 'Hồ sơ mặc định', value: stats.defaultCount, accent: 'from-emerald-500 to-teal-700', dark: false },
          { label: 'Hồ sơ trẻ em', value: stats.childCount, accent: 'from-rose-500 to-orange-600', dark: false },
        ].map((item) => (
          <div key={item.label} className={`rounded-[2rem] p-6 shadow-xl ${item.dark ? 'text-white bg-gradient-to-br' : 'bg-white'} ${item.dark ? item.accent : ''}`}>
            <p className={`text-[10px] font-black uppercase tracking-widest mb-2 ${item.dark ? 'text-white/60' : 'text-slate-400'}`}>{item.label}</p>
            <p className={`text-3xl font-black ${item.dark ? 'text-white' : 'text-slate-900'}`}>{item.value}</p>
          </div>
        ))}
      </div>

      {error ? (
        <div className="rounded-[1.75rem] border border-rose-100 bg-rose-50 px-6 py-4 text-sm font-bold text-rose-600">
          {error}
        </div>
      ) : null}

      {success ? (
        <div className="rounded-[1.75rem] border border-emerald-100 bg-emerald-50 px-6 py-4 text-sm font-bold text-emerald-700">
          {success}
        </div>
      ) : null}

      {showEditor ? (
        <form onSubmit={handleSubmit} className="bg-white rounded-[2.5rem] p-8 shadow-xl shadow-slate-100/60 space-y-6">
          <div className="flex items-center justify-between gap-4">
            <div>
              <p className="text-[10px] font-black text-[#1EB4D4] uppercase tracking-[0.25em] mb-1">Passenger Form</p>
              <h2 className="text-2xl font-black text-slate-900">{form.id ? 'Cập nhật hồ sơ hành khách' : 'Thêm hồ sơ hành khách'}</h2>
            </div>
            <button
              type="button"
              onClick={resetEditor}
              className="w-11 h-11 rounded-2xl bg-slate-50 text-slate-400 hover:bg-slate-100 hover:text-slate-700 transition-all flex items-center justify-center"
            >
              <X size={18} />
            </button>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
            <label className="space-y-2">
              <span className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em]">Họ và tên</span>
              <input value={form.fullName} onChange={(event) => setForm((current) => ({ ...current, fullName: event.target.value }))} className="w-full bg-slate-50 rounded-2xl py-4 px-5 font-bold text-slate-900 text-sm border-2 border-transparent focus:border-[#1EB4D4]/30 focus:bg-white outline-none transition-all" placeholder="Ví dụ: Nguyễn Văn A" />
            </label>
            <label className="space-y-2">
              <span className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em]">Loại hành khách</span>
              <select value={form.passengerType} onChange={(event) => setForm((current) => ({ ...current, passengerType: event.target.value }))} className="w-full bg-slate-50 rounded-2xl py-4 px-5 font-bold text-slate-900 text-sm border-2 border-transparent focus:border-[#1EB4D4]/30 focus:bg-white outline-none transition-all">
                <option value="adult">Người lớn</option>
                <option value="child">Trẻ em</option>
                <option value="infant">Em bé</option>
              </select>
            </label>
            <label className="space-y-2">
              <span className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em]">Giới tính</span>
              <select value={form.gender} onChange={(event) => setForm((current) => ({ ...current, gender: event.target.value }))} className="w-full bg-slate-50 rounded-2xl py-4 px-5 font-bold text-slate-900 text-sm border-2 border-transparent focus:border-[#1EB4D4]/30 focus:bg-white outline-none transition-all">
                <option value="">Chưa chọn</option>
                <option value="Nam">Nam</option>
                <option value="Nữ">Nữ</option>
                <option value="Khác">Khác</option>
              </select>
            </label>
            <label className="space-y-2">
              <span className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em]">Ngày sinh</span>
              <input type="date" value={form.dateOfBirth} onChange={(event) => setForm((current) => ({ ...current, dateOfBirth: event.target.value }))} className="w-full bg-slate-50 rounded-2xl py-4 px-5 font-bold text-slate-900 text-sm border-2 border-transparent focus:border-[#1EB4D4]/30 focus:bg-white outline-none transition-all" />
            </label>
            <label className="space-y-2">
              <span className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em]">CCCD / CMND</span>
              <input value={form.idNumber} onChange={(event) => setForm((current) => ({ ...current, idNumber: event.target.value }))} className="w-full bg-slate-50 rounded-2xl py-4 px-5 font-bold text-slate-900 text-sm border-2 border-transparent focus:border-[#1EB4D4]/30 focus:bg-white outline-none transition-all" placeholder="Số giấy tờ tùy thân" />
            </label>
            <label className="space-y-2">
              <span className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em]">Hộ chiếu</span>
              <input value={form.passportNumber} onChange={(event) => setForm((current) => ({ ...current, passportNumber: event.target.value }))} className="w-full bg-slate-50 rounded-2xl py-4 px-5 font-bold text-slate-900 text-sm border-2 border-transparent focus:border-[#1EB4D4]/30 focus:bg-white outline-none transition-all" placeholder="Nhập số hộ chiếu" />
            </label>
            <label className="space-y-2">
              <span className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em]">Email</span>
              <input type="email" value={form.email} onChange={(event) => setForm((current) => ({ ...current, email: event.target.value }))} className="w-full bg-slate-50 rounded-2xl py-4 px-5 font-bold text-slate-900 text-sm border-2 border-transparent focus:border-[#1EB4D4]/30 focus:bg-white outline-none transition-all" placeholder="email@example.com" />
            </label>
            <label className="space-y-2">
              <span className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em]">Số điện thoại</span>
              <input value={form.phoneNumber} onChange={(event) => setForm((current) => ({ ...current, phoneNumber: event.target.value }))} className="w-full bg-slate-50 rounded-2xl py-4 px-5 font-bold text-slate-900 text-sm border-2 border-transparent focus:border-[#1EB4D4]/30 focus:bg-white outline-none transition-all" placeholder="090..." />
            </label>
            <label className="space-y-2">
              <span className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em]">Quốc tịch</span>
              <input value={form.nationalityCode} onChange={(event) => setForm((current) => ({ ...current, nationalityCode: event.target.value.toUpperCase() }))} className="w-full bg-slate-50 rounded-2xl py-4 px-5 font-bold text-slate-900 text-sm border-2 border-transparent focus:border-[#1EB4D4]/30 focus:bg-white outline-none transition-all" placeholder="VN" />
            </label>
            <label className="space-y-2">
              <span className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em]">Ghi chú</span>
              <input value={form.notes} onChange={(event) => setForm((current) => ({ ...current, notes: event.target.value }))} className="w-full bg-slate-50 rounded-2xl py-4 px-5 font-bold text-slate-900 text-sm border-2 border-transparent focus:border-[#1EB4D4]/30 focus:bg-white outline-none transition-all" placeholder="Ví dụ: ưu tiên ghế gần cửa sổ" />
            </label>
          </div>

          <label className="flex items-center gap-3 rounded-2xl bg-slate-50 px-5 py-4 cursor-pointer">
            <input type="checkbox" checked={form.isDefault} onChange={(event) => setForm((current) => ({ ...current, isDefault: event.target.checked }))} className="w-4 h-4 rounded border-slate-300 text-[#1EB4D4] focus:ring-[#1EB4D4]" />
            <div>
              <p className="text-sm font-black text-slate-900">Đặt làm hành khách mặc định</p>
              <p className="text-xs font-bold text-slate-400">Hồ sơ này sẽ được ưu tiên điền vào checkout.</p>
            </div>
          </label>

          <div className="flex gap-3">
            <button type="button" onClick={resetEditor} className="px-6 py-4 rounded-2xl font-black text-xs uppercase tracking-widest text-slate-500 hover:bg-slate-50 transition-all">
              Hủy
            </button>
            <button type="submit" disabled={saving} className="flex-1 flex items-center justify-center gap-2 px-6 py-4 bg-gradient-to-r from-[#1EB4D4] to-[#002B7F] text-white rounded-2xl font-black text-xs uppercase tracking-widest shadow-xl shadow-sky-400/25 hover:-translate-y-0.5 transition-all disabled:opacity-70 disabled:hover:translate-y-0">
              <Save size={16} /> {saving ? 'Đang lưu...' : form.id ? 'Lưu cập nhật' : 'Tạo hồ sơ'}
            </button>
          </div>
        </form>
      ) : null}

      {loading ? (
        <div className="bg-white rounded-[2rem] p-12 text-center text-sm font-bold text-slate-400 shadow-xl shadow-slate-100/60">
          Đang tải danh sách hành khách...
        </div>
      ) : items.length === 0 ? (
        <div className="bg-white rounded-[2rem] p-12 text-center shadow-xl shadow-slate-100/60">
          <div className="w-16 h-16 rounded-full bg-slate-50 flex items-center justify-center mx-auto mb-4">
            <Users size={28} className="text-slate-300" />
          </div>
          <h3 className="text-xl font-black text-slate-900 mb-2">Chưa có hành khách nào được lưu</h3>
          <p className="text-sm font-medium text-slate-400 mb-6">Tạo hồ sơ để checkout nhanh hơn cho những lần đặt dịch vụ tiếp theo.</p>
          <button type="button" onClick={handleCreate} className="inline-flex items-center gap-2 px-6 py-3 bg-slate-900 text-white rounded-2xl text-xs font-black uppercase tracking-widest hover:bg-[#1EB4D4] transition-all">
            <Plus size={14} /> Tạo hồ sơ đầu tiên
          </button>
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-5">
          {items.map((passenger, index) => (
            <motion.div
              key={passenger.id}
              initial={{ opacity: 0, x: -20 }}
              animate={{ opacity: 1, x: 0 }}
              transition={{ delay: index * 0.05 }}
              className="group flex overflow-hidden rounded-[2rem] shadow-xl shadow-slate-100/60 bg-white hover:shadow-2xl hover:shadow-sky-100/40 transition-all duration-500"
            >
              <div className={`w-2 shrink-0 bg-gradient-to-b ${BOARD_COLORS[index % BOARD_COLORS.length]}`} />

              <div className={`hidden md:flex w-40 bg-gradient-to-br ${BOARD_COLORS[index % BOARD_COLORS.length]} items-center justify-center shrink-0`}>
                <div className="flex flex-col items-center gap-3 p-6 text-white">
                  <div className="w-16 h-16 bg-white/20 backdrop-blur rounded-2xl flex items-center justify-center">
                    {Number(passenger.passengerType) === CUSTOMER_PASSENGER_TYPE.CHILD || Number(passenger.passengerType) === CUSTOMER_PASSENGER_TYPE.INFANT
                      ? <Baby size={28} />
                      : <User size={28} />}
                  </div>
                  <span className="text-[9px] font-black uppercase tracking-widest opacity-80">
                    {formatCustomerPassengerTypeLabel(passenger.passengerType)}
                  </span>
                  <span className="text-xs font-black opacity-60">{getTripCountHint(passenger.passengerType)}</span>
                </div>
              </div>

              <div className="flex-1 p-6 md:p-8">
                <div className="flex flex-col md:flex-row md:items-start justify-between gap-4">
                  <div>
                    <div className="flex items-center gap-2 flex-wrap">
                      <h3 className="text-xl font-black text-slate-900 tracking-tight group-hover:text-[#1EB4D4] transition-colors">{passenger.fullName}</h3>
                      {passenger.isDefault ? (
                        <span className="inline-flex items-center gap-1 px-3 py-1 bg-amber-50 text-amber-600 rounded-full text-[9px] font-black uppercase tracking-widest">
                          <Star size={10} fill="currentColor" /> Mặc định
                        </span>
                      ) : null}
                    </div>
                    <span className="inline-block px-3 py-1 bg-slate-100 text-slate-500 rounded-full text-[9px] font-black uppercase tracking-widest mt-2">
                      {passenger.gender || formatCustomerPassengerTypeLabel(passenger.passengerType)}
                    </span>
                  </div>

                  <div className="flex items-center gap-2 opacity-0 group-hover:opacity-100 transition-all">
                    <button type="button" onClick={() => handleEdit(passenger)} className="p-2.5 bg-slate-50 hover:bg-[#1EB4D4] hover:text-white text-slate-400 rounded-xl transition-all shadow-sm">
                      <Edit2 size={15} />
                    </button>
                    <button type="button" disabled={deletingId === passenger.id} onClick={() => handleDelete(passenger.id)} className="p-2.5 bg-slate-50 hover:bg-rose-50 hover:text-rose-500 text-slate-400 rounded-xl transition-all shadow-sm disabled:opacity-60">
                      <Trash2 size={15} />
                    </button>
                  </div>
                </div>

                <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mt-5">
                  {[
                    { icon: <IdCard size={13} />, label: 'CCCD / CMND', val: passenger.idNumber || 'Chưa cập nhật' },
                    { icon: <Calendar size={13} />, label: 'Ngày sinh', val: passenger.dateOfBirth || 'Chưa cập nhật' },
                    { icon: <Mail size={13} />, label: 'Email', val: passenger.email || 'Chưa cập nhật' },
                    { icon: <Phone size={13} />, label: 'Điện thoại', val: passenger.phoneNumber || 'Chưa cập nhật' },
                  ].map((field) => (
                    <div key={`${passenger.id}-${field.label}`} className="bg-slate-50 rounded-2xl p-3">
                      <div className="flex items-center gap-1 text-slate-400 mb-1">
                        {field.icon}
                        <span className="text-[9px] font-black uppercase tracking-widest">{field.label}</span>
                      </div>
                      <p className="text-sm font-black text-slate-900 truncate">{field.val}</p>
                    </div>
                  ))}
                </div>
              </div>

              <div className="hidden lg:flex flex-col items-center justify-center w-24 border-l border-dashed border-slate-200 p-4 gap-2">
                <div className="w-10 h-10 rounded-xl bg-slate-100 flex items-center justify-center text-slate-400 text-xs font-black">#{index + 1}</div>
                <div className="text-[8px] font-black text-slate-400 uppercase tracking-widest text-center leading-tight">Hành khách đã lưu</div>
              </div>
            </motion.div>
          ))}
        </div>
      )}
    </motion.div>
  );
}
