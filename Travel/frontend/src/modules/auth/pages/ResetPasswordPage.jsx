import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { ArrowLeft, ArrowRight, Eye, EyeOff, KeyRound, Lock, Mail, ShieldCheck } from 'lucide-react';
import { resetPassword } from '../../../services/auth';

const ResetPasswordPage = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [identifier, setIdentifier] = useState(
    searchParams.get('identifier')
    || searchParams.get('emailOrUserName')
    || searchParams.get('email')
    || '',
  );
  const [token, setToken] = useState(searchParams.get('token') || '');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');

    if (newPassword !== confirmPassword) {
      setError('Mật khẩu xác nhận chưa khớp.');
      return;
    }

    setLoading(true);

    try {
      await resetPassword({
        emailOrUserName: identifier,
        token,
        newPassword,
      });

      navigate('/auth/login', {
        replace: true,
        state: {
          resetMessage: 'Đặt lại mật khẩu thành công. Bạn có thể đăng nhập bằng mật khẩu mới.',
        },
      });
    } catch (err) {
      setError(err.message || 'Không thể đặt lại mật khẩu.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen relative flex items-center justify-center p-4 overflow-hidden">
      <div className="absolute inset-0 z-0">
        <img
          src="https://images.unsplash.com/photo-1507525428034-b723cf961d3e?auto=format&fit=crop&q=80&w=2000"
          alt="Travel Background"
          className="w-full h-full object-cover"
        />
        <div className="absolute inset-0 bg-gradient-to-br from-[#002B7F]/80 via-[#002B7F]/60 to-[#1EB4D4]/70 backdrop-blur-[2px]" />
      </div>

      <motion.div
        initial={{ opacity: 0, scale: 0.95 }}
        animate={{ opacity: 1, scale: 1 }}
        className="relative z-10 w-full max-w-[600px] bg-white/95 backdrop-blur-xl rounded-[3rem] shadow-2xl overflow-hidden border border-white/20 p-8 lg:p-12"
      >
        <div className="mb-10 text-center">
          <div className="w-16 h-16 bg-blue-50 text-[#1EB4D4] rounded-2xl flex items-center justify-center mx-auto mb-6 shadow-sm">
            <ShieldCheck size={32} />
          </div>
          <h1 className="text-3xl font-black text-slate-900 mb-2 tracking-tight">Đặt lại mật khẩu</h1>
          <p className="text-slate-400 font-bold uppercase text-[10px] tracking-widest leading-relaxed">
            Điền đầy đủ thông tin xác thực để tạo mật khẩu mới cho tài khoản của bạn
          </p>
        </div>

        {error && (
          <div className="mb-6 rounded-[1.5rem] border border-rose-100 bg-rose-50 px-5 py-4 text-sm font-bold text-rose-600">
            {error}
          </div>
        )}

        <form className="space-y-6" onSubmit={handleSubmit}>
          <div className="space-y-2">
            <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] ml-1">Email hoặc tên đăng nhập</label>
            <div className="relative group">
              <Mail className="absolute left-5 top-1/2 -translate-y-1/2 text-slate-300 group-focus-within:text-[#1EB4D4] transition-colors" size={20} />
              <input
                type="text"
                value={identifier}
                onChange={(e) => setIdentifier(e.target.value)}
                placeholder="name@example.com"
                required
                className="w-full bg-slate-50 border-2 border-transparent focus:bg-white focus:border-[#1EB4D4]/20 rounded-[1.5rem] py-5 pl-14 pr-6 font-bold text-slate-900 text-sm focus:ring-0 transition-all shadow-sm"
              />
            </div>
          </div>

          <div className="space-y-2">
            <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] ml-1">Mã xác thực</label>
            <div className="relative group">
              <KeyRound className="absolute left-5 top-1/2 -translate-y-1/2 text-slate-300 group-focus-within:text-[#1EB4D4] transition-colors" size={20} />
              <textarea
                value={token}
                onChange={(e) => setToken(e.target.value)}
                rows={3}
                required
                className="w-full resize-none bg-slate-50 border-2 border-transparent focus:bg-white focus:border-[#1EB4D4]/20 rounded-[1.5rem] py-5 pl-14 pr-6 font-bold text-slate-900 text-sm focus:ring-0 transition-all shadow-sm"
              />
            </div>
          </div>

          <div className="space-y-2">
            <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] ml-1">Mật khẩu mới</label>
            <div className="relative group">
              <Lock className="absolute left-5 top-1/2 -translate-y-1/2 text-slate-300 group-focus-within:text-[#1EB4D4] transition-colors" size={20} />
              <input
                type={showPassword ? 'text' : 'password'}
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                placeholder="••••••••"
                required
                className="w-full bg-slate-50 border-2 border-transparent focus:bg-white focus:border-[#1EB4D4]/20 rounded-[1.5rem] py-5 pl-14 pr-14 font-bold text-slate-900 text-sm focus:ring-0 transition-all shadow-sm"
              />
              <button
                type="button"
                onClick={() => setShowPassword((value) => !value)}
                className="absolute right-5 top-1/2 -translate-y-1/2 text-slate-300 hover:text-slate-600 transition-colors"
              >
                {showPassword ? <EyeOff size={20} /> : <Eye size={20} />}
              </button>
            </div>
          </div>

          <div className="space-y-2">
            <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] ml-1">Xác nhận mật khẩu mới</label>
            <div className="relative group">
              <Lock className="absolute left-5 top-1/2 -translate-y-1/2 text-slate-300 group-focus-within:text-[#1EB4D4] transition-colors" size={20} />
              <input
                type={showConfirmPassword ? 'text' : 'password'}
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                placeholder="••••••••"
                required
                className="w-full bg-slate-50 border-2 border-transparent focus:bg-white focus:border-[#1EB4D4]/20 rounded-[1.5rem] py-5 pl-14 pr-14 font-bold text-slate-900 text-sm focus:ring-0 transition-all shadow-sm"
              />
              <button
                type="button"
                onClick={() => setShowConfirmPassword((value) => !value)}
                className="absolute right-5 top-1/2 -translate-y-1/2 text-slate-300 hover:text-slate-600 transition-colors"
              >
                {showConfirmPassword ? <EyeOff size={20} /> : <Eye size={20} />}
              </button>
            </div>
          </div>

          <button
            type="submit"
            disabled={loading}
            className="w-full bg-gradient-to-r from-[#1EB4D4] to-[#002B7F] text-white py-5 rounded-[1.5rem] font-black text-sm uppercase tracking-widest shadow-xl shadow-blue-500/20 hover:shadow-blue-500/40 hover:-translate-y-1 transition-all active:translate-y-0 flex items-center justify-center gap-3 disabled:opacity-70 disabled:hover:translate-y-0"
          >
            {loading ? 'Đang xử lý...' : 'Lưu mật khẩu mới'} <ArrowRight size={18} />
          </button>
        </form>

        <div className="mt-10 pt-10 border-t border-slate-100 text-center">
          <Link to="/auth/login" className="flex items-center justify-center gap-2 text-sm font-black text-slate-400 hover:text-slate-600 transition-colors uppercase tracking-widest">
            <ArrowLeft size={16} /> Trở về đăng nhập
          </Link>
        </div>
      </motion.div>
    </div>
  );
};

export default ResetPasswordPage;
