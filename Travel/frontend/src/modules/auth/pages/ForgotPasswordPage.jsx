import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { Link } from 'react-router-dom';
import { Mail, ArrowRight, ArrowLeft, ShieldCheck } from 'lucide-react';
import { forgotPassword } from '../../../services/auth';

const ForgotPasswordPage = () => {
  const [emailOrUserName, setEmailOrUserName] = useState('');
  const [submitted, setSubmitted] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [devToken, setDevToken] = useState('');
  const showDevToken = import.meta.env.DEV && !!devToken;

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      const response = await forgotPassword({ emailOrUserName });
      setSubmitted(true);
      setMessage(response?.message || 'Hệ thống đã tiếp nhận yêu cầu khôi phục mật khẩu.');
      setDevToken(response?.devToken || '');
    } catch (err) {
      setError(err.message || 'Không thể gửi yêu cầu khôi phục mật khẩu.');
    } finally {
      setLoading(false);
    }
  };

  const resetLink = `/auth/reset-password?identifier=${encodeURIComponent(emailOrUserName)}${devToken ? `&token=${encodeURIComponent(devToken)}` : ''}`;

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
        className="relative z-10 w-full max-w-[550px] bg-white/95 backdrop-blur-xl rounded-[3rem] shadow-2xl overflow-hidden border border-white/20 p-8 lg:p-12"
      >
        <div className="mb-10 text-center">
          <div className="w-16 h-16 bg-blue-50 text-[#1EB4D4] rounded-2xl flex items-center justify-center mx-auto mb-6 shadow-sm">
            <ShieldCheck size={32} />
          </div>
          <h1 className="text-3xl font-black text-slate-900 mb-2 tracking-tight">Quên mật khẩu?</h1>
          <p className="text-slate-400 font-bold uppercase text-[10px] tracking-widest leading-relaxed">
            Nhập email hoặc tên đăng nhập để nhận hướng dẫn khôi phục mật khẩu truy cập hệ thống
          </p>
        </div>

        {error && (
          <div className="mb-6 rounded-[1.5rem] border border-rose-100 bg-rose-50 px-5 py-4 text-sm font-bold text-rose-600">
            {error}
          </div>
        )}

        {!submitted ? (
          <form className="space-y-6" onSubmit={handleSubmit}>
            <div className="space-y-2">
              <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] ml-1">Email hoặc tên đăng nhập</label>
              <div className="relative group">
                <Mail className="absolute left-5 top-1/2 -translate-y-1/2 text-slate-300 group-focus-within:text-[#1EB4D4] transition-colors" size={20} />
                <input
                  type="text"
                  value={emailOrUserName}
                  onChange={(e) => setEmailOrUserName(e.target.value)}
                  placeholder="name@example.com"
                  required
                  className="w-full bg-slate-50 border-2 border-transparent focus:bg-white focus:border-[#1EB4D4]/20 rounded-[1.5rem] py-5 pl-14 pr-6 font-bold text-slate-900 text-sm focus:ring-0 transition-all shadow-sm"
                />
              </div>
            </div>

            <button
              type="submit"
              disabled={loading}
              className="w-full bg-gradient-to-r from-[#1EB4D4] to-[#002B7F] text-white py-5 rounded-[1.5rem] font-black text-sm uppercase tracking-widest shadow-xl shadow-blue-500/20 hover:shadow-blue-500/40 hover:-translate-y-1 transition-all active:translate-y-0 flex items-center justify-center gap-3 disabled:opacity-70 disabled:hover:translate-y-0"
            >
              {loading ? 'Đang xử lý...' : 'Gửi yêu cầu khôi phục'} <ArrowRight size={18} />
            </button>
          </form>
        ) : (
          <motion.div
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            className="text-center py-2"
          >
            <div className="w-12 h-12 bg-green-50 text-green-500 rounded-full flex items-center justify-center mx-auto mb-4">
              <ShieldCheck size={24} />
            </div>
            <h3 className="text-xl font-black text-slate-900 mb-2">Đã gửi yêu cầu!</h3>
            <p className="text-sm text-slate-500 font-medium mb-6">
              {message || 'Chúng tôi đã gửi hướng dẫn khôi phục mật khẩu tới tài khoản của bạn. Vui lòng kiểm tra hộp thư đến và cả mục spam.'}
            </p>

            {showDevToken && (
              <div className="mb-6 rounded-[1.75rem] border border-sky-100 bg-sky-50 px-5 py-4 text-left">
                <p className="text-[10px] font-black uppercase tracking-[0.2em] text-sky-500 mb-2">Moi truong dev</p>
                <p className="break-all text-sm font-bold text-slate-700">{devToken}</p>
                <Link
                  to={resetLink}
                  className="mt-4 inline-flex items-center gap-2 text-xs font-black uppercase tracking-widest text-[#1EB4D4] hover:underline"
                >
                  Đi tới trang đặt lại mật khẩu <ArrowRight size={14} />
                </Link>
              </div>
            )}

            <button
              type="button"
              onClick={() => {
                setSubmitted(false);
                setDevToken('');
                setMessage('');
              }}
              className="text-[#1EB4D4] font-black text-xs uppercase tracking-widest hover:underline"
            >
              Không nhận được email? Gửi lại
            </button>
          </motion.div>
        )}

        <div className="mt-10 pt-10 border-t border-slate-100 text-center">
          <Link to="/auth/login" className="flex items-center justify-center gap-2 text-sm font-black text-slate-400 hover:text-slate-600 transition-colors uppercase tracking-widest">
            <ArrowLeft size={16} /> Trở về đăng nhập
          </Link>
        </div>
      </motion.div>
    </div>
  );
};

export default ForgotPasswordPage;
