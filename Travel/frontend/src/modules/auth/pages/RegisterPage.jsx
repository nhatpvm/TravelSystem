import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { Link, useNavigate } from 'react-router-dom';
import { User, Mail, Lock, Phone, ArrowRight, ShieldCheck, MapPin } from 'lucide-react';
import logo from '../../../assets/logo.png';
import { register } from '../../../services/auth';

const RegisterPage = () => {
  const navigate = useNavigate();
  const [form, setForm] = useState({
    fullName: '',
    phoneNumber: '',
    email: '',
    password: '',
    confirmPassword: '',
    acceptedTerms: false,
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const updateField = (key, value) => {
    setForm((prev) => ({ ...prev, [key]: value }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');

    if (!form.acceptedTerms) {
      setError('Vui lòng đồng ý với điều khoản dịch vụ.');
      return;
    }

    if (form.password !== form.confirmPassword) {
      setError('Mật khẩu xác nhận chưa khớp.');
      return;
    }

    setLoading(true);

    try {
      await register({
        fullName: form.fullName,
        phoneNumber: form.phoneNumber,
        email: form.email,
        password: form.password,
      });

      navigate('/auth/login', {
        replace: true,
        state: {
          registeredMessage: 'Đăng ký thành công. Bạn có thể đăng nhập ngay bây giờ.',
        },
      });
    } catch (err) {
      setError(err.message || 'Đăng ký thất bại.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen relative flex items-center justify-center p-4 overflow-hidden">
      <div className="absolute inset-0 z-0">
        <img
          src="https://images.unsplash.com/photo-1528360983277-13d401cdc186?auto=format&fit=crop&q=80&w=2000"
          alt="Travel Background"
          className="w-full h-full object-cover"
        />
        <div className="absolute inset-0 bg-gradient-to-br from-[#1EB4D4]/90 via-[#002B7F]/70 to-[#002B7F]/80" />
      </div>

      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        className="relative z-10 w-full max-w-[1100px] flex flex-col md:flex-row-reverse bg-white/95 backdrop-blur-xl rounded-[3rem] shadow-2xl overflow-hidden border border-white/20"
      >
        <div className="hidden md:flex flex-col justify-between p-12 md:w-5/12 bg-[#1EB4D4] text-white relative">
          <div className="absolute inset-0 overflow-hidden opacity-10">
            <div className="grid grid-cols-4 gap-4 rotate-12 scale-150">
              {[...Array(20)].map((_, i) => (
                <MapPin key={i} size={48} className="text-white" />
              ))}
            </div>
          </div>

          <div className="relative z-10">
            <Link to="/" className="flex items-center gap-4 mb-12 group">
              <img src={logo} alt="logo" className="h-20 group-hover:rotate-12 transition-transform brightness-0 invert" />
              <span className="text-3xl font-black tracking-tighter">2TMNY</span>
            </Link>

            <h2 className="text-4xl font-black leading-tight mb-6">Trở thành thành viên của gia đình hành trình.</h2>
            <p className="text-white/60 font-medium italic" style={{ fontFamily: "'Kalam', cursive" }}>
              "Du lịch làm giàu tâm hồn, mở mang trí óc và viết nên những câu chuyện cuộc đời."
            </p>
          </div>

          <div className="relative z-10 space-y-4">
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 rounded-full bg-white/10 flex items-center justify-center">
                <ShieldCheck size={16} className="text-[#002B7F]" />
              </div>
              <span className="text-xs font-bold uppercase tracking-widest text-[#002B7F]">Ưu đãi dành riêng cho hội viên</span>
            </div>
          </div>
        </div>

        <div className="flex-1 p-8 lg:p-16">
          <div className="mb-10 text-center md:text-left">
            <h1 className="text-3xl font-black text-slate-900 mb-2 tracking-tight">Đăng ký thành viên</h1>
            <p className="text-slate-400 font-bold uppercase text-[10px] tracking-widest">Tạo hồ sơ lữ khách mới của bạn</p>
          </div>

          {error && (
            <div className="mb-6 rounded-[1.5rem] border border-rose-100 bg-rose-50 px-5 py-4 text-sm font-bold text-rose-600">
              {error}
            </div>
          )}

          <form className="space-y-6" onSubmit={handleSubmit}>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div className="space-y-2">
                <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] ml-1">Họ và tên</label>
                <div className="relative group">
                  <User className="absolute left-5 top-1/2 -translate-y-1/2 text-slate-300 group-focus-within:text-[#1EB4D4] transition-colors" size={20} />
                  <input
                    type="text"
                    value={form.fullName}
                    onChange={(e) => updateField('fullName', e.target.value)}
                    placeholder="Nguyễn Văn A"
                    required
                    className="w-full bg-slate-50 border-2 border-transparent focus:bg-white focus:border-[#1EB4D4]/20 rounded-[1.5rem] py-5 pl-14 pr-6 font-bold text-slate-900 text-sm focus:ring-0 transition-all shadow-sm"
                  />
                </div>
              </div>

              <div className="space-y-2">
                <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] ml-1">Số điện thoại</label>
                <div className="relative group">
                  <Phone className="absolute left-5 top-1/2 -translate-y-1/2 text-slate-300 group-focus-within:text-[#1EB4D4] transition-colors" size={20} />
                  <input
                    type="tel"
                    value={form.phoneNumber}
                    onChange={(e) => updateField('phoneNumber', e.target.value)}
                    placeholder="09xx xxx xxx"
                    className="w-full bg-slate-50 border-2 border-transparent focus:bg-white focus:border-[#1EB4D4]/20 rounded-[1.5rem] py-5 pl-14 pr-6 font-bold text-slate-900 text-sm focus:ring-0 transition-all shadow-sm"
                  />
                </div>
              </div>
            </div>

            <div className="space-y-2">
              <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] ml-1">Địa chỉ Email</label>
              <div className="relative group">
                <Mail className="absolute left-5 top-1/2 -translate-y-1/2 text-slate-300 group-focus-within:text-[#1EB4D4] transition-colors" size={20} />
                <input
                  type="email"
                  value={form.email}
                  onChange={(e) => updateField('email', e.target.value)}
                  placeholder="name@example.com"
                  required
                  className="w-full bg-slate-50 border-2 border-transparent focus:bg-white focus:border-[#1EB4D4]/20 rounded-[1.5rem] py-5 pl-14 pr-6 font-bold text-slate-900 text-sm focus:ring-0 transition-all shadow-sm"
                />
              </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div className="space-y-2">
                <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] ml-1">Mật khẩu</label>
                <div className="relative group">
                  <Lock className="absolute left-5 top-1/2 -translate-y-1/2 text-slate-300 group-focus-within:text-[#1EB4D4] transition-colors" size={20} />
                  <input
                    type="password"
                    value={form.password}
                    onChange={(e) => updateField('password', e.target.value)}
                    placeholder="••••••••"
                    required
                    className="w-full bg-slate-50 border-2 border-transparent focus:bg-white focus:border-[#1EB4D4]/20 rounded-[1.5rem] py-5 pl-14 pr-6 font-bold text-slate-900 text-sm focus:ring-0 transition-all shadow-sm"
                  />
                </div>
              </div>

              <div className="space-y-2">
                <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] ml-1">Xác nhận mật khẩu</label>
                <div className="relative group">
                  <Lock className="absolute left-5 top-1/2 -translate-y-1/2 text-slate-300 group-focus-within:text-[#1EB4D4] transition-colors" size={20} />
                  <input
                    type="password"
                    value={form.confirmPassword}
                    onChange={(e) => updateField('confirmPassword', e.target.value)}
                    placeholder="••••••••"
                    required
                    className="w-full bg-slate-50 border-2 border-transparent focus:bg-white focus:border-[#1EB4D4]/20 rounded-[1.5rem] py-5 pl-14 pr-6 font-bold text-slate-900 text-sm focus:ring-0 transition-all shadow-sm"
                  />
                </div>
              </div>
            </div>

            <div className="flex items-start gap-3 py-2">
              <input
                type="checkbox"
                id="terms"
                checked={form.acceptedTerms}
                onChange={(e) => updateField('acceptedTerms', e.target.checked)}
                className="mt-1 w-5 h-5 rounded-lg border-slate-200 text-[#1EB4D4] focus:ring-[#1EB4D4] transition-all cursor-pointer"
              />
              <label htmlFor="terms" className="text-xs font-bold text-slate-500 cursor-pointer select-none leading-relaxed">
                Tôi đồng ý với các <Link to="/terms" className="text-[#1EB4D4] hover:underline">Điều khoản dịch vụ</Link> và <Link to="/privacy" className="text-[#1EB4D4] hover:underline">Chính sách bảo mật</Link> của 2TMNY.
              </label>
            </div>

            <button disabled={loading} className="w-full bg-gradient-to-r from-[#1EB4D4] to-[#002B7F] text-white py-5 rounded-[1.5rem] font-black text-sm uppercase tracking-widest shadow-xl shadow-blue-500/20 hover:shadow-blue-500/40 hover:-translate-y-1 transition-all active:translate-y-0 flex items-center justify-center gap-3 disabled:opacity-70 disabled:hover:translate-y-0">
              {loading ? 'Đang xử lý...' : 'Tạo tài khoản'} <ArrowRight size={18} />
            </button>
          </form>

          <div className="mt-10 pt-10 border-t border-slate-100 text-center">
            <p className="text-sm font-bold text-slate-400">
              Đã có tài khoản?{' '}
              <Link to="/auth/login" className="text-[#1EB4D4] hover:underline">Đăng nhập tại đây</Link>
            </p>
          </div>
        </div>
      </motion.div>
    </div>
  );
};

export default RegisterPage;
