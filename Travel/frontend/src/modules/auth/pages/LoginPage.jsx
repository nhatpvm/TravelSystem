import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { Mail, Lock, Eye, EyeOff, ArrowRight, Plane, Globe, ShieldCheck, KeyRound } from 'lucide-react';
import logo from '../../../assets/logo.png';
import { login, verifyTwoFactorLogin } from '../../../services/auth';
import { canAccessAdmin, canAccessTenant, getPostLoginPath } from '../types';

const LoginPage = () => {
  const [showPassword, setShowPassword] = useState(false);
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [rememberMe, setRememberMe] = useState(true);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [twoFactorChallenge, setTwoFactorChallenge] = useState(null);
  const [twoFactorCode, setTwoFactorCode] = useState('');
  const [recoveryCode, setRecoveryCode] = useState('');
  const [useRecoveryCode, setUseRecoveryCode] = useState(false);
  const navigate = useNavigate();
  const location = useLocation();

  const registeredMessage = location.state?.registeredMessage;
  const resetMessage = location.state?.resetMessage;
  const returnToPath = typeof location.state?.returnTo === 'string' ? location.state.returnTo : '';
  const fromPath = typeof location.state?.from === 'string'
    ? location.state.from
    : typeof location.state?.from?.pathname === 'string'
      ? location.state.from.pathname
      : returnToPath;
  const isTwoFactorStep = !!twoFactorChallenge?.requiresTwoFactor;

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      if (isTwoFactorStep) {
        const response = await verifyTwoFactorLogin({
          challengeToken: twoFactorChallenge.challengeToken,
          code: useRecoveryCode ? undefined : twoFactorCode,
          recoveryCode: useRecoveryCode ? recoveryCode : undefined,
          rememberMe,
        });

        navigate(resolveRedirectPath(response.user, fromPath), { replace: true });
        return;
      }

      const response = await login({
        usernameOrEmail: email,
        password,
        rememberMe,
      });

      if (response?.requiresTwoFactor) {
        setTwoFactorChallenge(response);
        setTwoFactorCode('');
        setRecoveryCode('');
        setUseRecoveryCode(false);
        return;
      }

      navigate(resolveRedirectPath(response.user, fromPath), { replace: true });
    } catch (err) {
      setError(err.message || (isTwoFactorStep ? 'Xác minh 2 lớp thất bại.' : 'Đăng nhập thất bại.'));
    } finally {
      setLoading(false);
    }
  };

  function resetTwoFactorStep() {
    setTwoFactorChallenge(null);
    setTwoFactorCode('');
    setRecoveryCode('');
    setUseRecoveryCode(false);
    setError('');
  }

  return (
    <div className="min-h-screen relative flex items-center justify-center p-4 overflow-hidden">
      <div className="absolute inset-0 z-0">
        <img
          src="https://images.unsplash.com/photo-1436491865332-7a61a109cc05?auto=format&fit=crop&q=80&w=2000"
          alt="Travel Background"
          className="w-full h-full object-cover"
        />
        <div className="absolute inset-0 bg-gradient-to-br from-[#002B7F]/90 via-[#002B7F]/70 to-[#1EB4D4]/80" />
      </div>

      <div className="absolute inset-0 z-0 pointer-events-none overflow-hidden">
        <motion.div
          animate={{ x: ['100%', '-100%'], y: ['-20%', '120%'] }}
          transition={{ duration: 25, repeat: Infinity, ease: 'linear' }}
          className="absolute top-0 right-0 opacity-10"
        >
          <Plane size={300} className="text-white rotate-45" />
        </motion.div>
      </div>

      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        className="relative z-10 w-full max-w-[1100px] flex flex-col md:flex-row bg-white/95 backdrop-blur-xl rounded-[3rem] shadow-2xl overflow-hidden border border-white/20"
      >
        <div className="hidden md:flex flex-col justify-between p-12 md:w-5/12 bg-[#002B7F] text-white relative">
          <div className="absolute inset-0 overflow-hidden opacity-10">
            <div className="grid grid-cols-4 gap-4 rotate-12 scale-150">
              {[...Array(20)].map((_, i) => (
                <Globe key={i} size={48} className="text-white" />
              ))}
            </div>
          </div>

          <div className="relative z-10">
            <Link to="/" className="flex items-center gap-4 mb-12 group">
              <img src={logo} alt="logo" className="h-20 group-hover:rotate-12 transition-transform brightness-0 invert" />
              <span className="text-3xl font-black tracking-tighter">2TMNY</span>
            </Link>

            <h2 className="text-4xl font-black leading-tight mb-6">Bắt đầu hành trình của bạn ngay hôm nay.</h2>
            <p className="text-white/60 font-medium italic" style={{ fontFamily: "'Kalam', cursive" }}>
              "Thế giới là một cuốn sách, và ai không đi du lịch thì chỉ mới đọc được một trang."
            </p>
          </div>

          <div className="relative z-10 space-y-4">
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 rounded-full bg-white/10 flex items-center justify-center">
                <ShieldCheck size={16} className="text-[#1EB4D4]" />
              </div>
              <span className="text-xs font-bold uppercase tracking-widest text-white/80">Bảo mật thông tin 100%</span>
            </div>
            <p className="text-[10px] text-white/40 uppercase font-black tracking-[0.2em]">© 2024 2TMNY Travel Platform</p>
          </div>
        </div>

        <div className="flex-1 p-8 lg:p-16">
          <div className="mb-10 text-center md:text-left">
            <h1 className="text-3xl font-black text-slate-900 mb-2 tracking-tight">
              {isTwoFactorStep ? 'Xác minh 2 lớp' : 'Chào mừng trở lại!'}
            </h1>
            <p className="text-slate-400 font-bold uppercase text-[10px] tracking-widest">
              {isTwoFactorStep ? 'Nhập mã xác minh để hoàn tất đăng nhập' : 'Đăng nhập vào tài khoản lữ khách của bạn'}
            </p>
          </div>

          {registeredMessage && !isTwoFactorStep ? (
            <div className="mb-6 rounded-[1.5rem] border border-emerald-100 bg-emerald-50 px-5 py-4 text-sm font-bold text-emerald-700">
              {registeredMessage}
            </div>
          ) : null}

          {resetMessage && !isTwoFactorStep ? (
            <div className="mb-6 rounded-[1.5rem] border border-emerald-100 bg-emerald-50 px-5 py-4 text-sm font-bold text-emerald-700">
              {resetMessage}
            </div>
          ) : null}

          {isTwoFactorStep ? (
            <div className="mb-6 rounded-[1.5rem] border border-sky-100 bg-sky-50 px-5 py-4 text-sm font-bold text-sky-700">
              Ứng dụng xác thực đã được bật cho tài khoản này. Hãy nhập mã 6 số hiện tại hoặc dùng mã dự phòng.
            </div>
          ) : null}

          {error ? (
            <div className="mb-6 rounded-[1.5rem] border border-rose-100 bg-rose-50 px-5 py-4 text-sm font-bold text-rose-600">
              {error}
            </div>
          ) : null}

          <form className="space-y-6" onSubmit={handleSubmit}>
            {!isTwoFactorStep ? (
              <>
                <div className="space-y-2">
                  <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] ml-1">Email hoặc tên đăng nhập</label>
                  <div className="relative group">
                    <Mail className="absolute left-5 top-1/2 -translate-y-1/2 text-slate-300 group-focus-within:text-[#1EB4D4] transition-colors" size={20} />
                    <input
                      type="text"
                      value={email}
                      onChange={(e) => setEmail(e.target.value)}
                      placeholder="name@example.com"
                      required
                      className="w-full bg-slate-50 border-2 border-transparent focus:bg-white focus:border-[#1EB4D4]/20 rounded-[1.5rem] py-5 pl-14 pr-6 font-bold text-slate-900 text-sm focus:ring-0 transition-all shadow-sm"
                    />
                  </div>
                </div>

                <div className="space-y-2">
                  <div className="flex justify-between items-center ml-1">
                    <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em]">Mật khẩu</label>
                    <Link to="/auth/forgot-password" className="text-[10px] font-black text-[#1EB4D4] uppercase tracking-widest hover:underline">
                      Quên mật khẩu?
                    </Link>
                  </div>
                  <div className="relative group">
                    <Lock className="absolute left-5 top-1/2 -translate-y-1/2 text-slate-300 group-focus-within:text-[#1EB4D4] transition-colors" size={20} />
                    <input
                      type={showPassword ? 'text' : 'password'}
                      value={password}
                      onChange={(e) => setPassword(e.target.value)}
                      placeholder="••••••••"
                      required
                      className="w-full bg-slate-50 border-2 border-transparent focus:bg-white focus:border-[#1EB4D4]/20 rounded-[1.5rem] py-5 pl-14 pr-14 font-bold text-slate-900 text-sm focus:ring-0 transition-all shadow-sm"
                    />
                    <button
                      type="button"
                      onClick={() => setShowPassword(!showPassword)}
                      className="absolute right-5 top-1/2 -translate-y-1/2 text-slate-300 hover:text-slate-600 transition-colors"
                    >
                      {showPassword ? <EyeOff size={20} /> : <Eye size={20} />}
                    </button>
                  </div>
                </div>
              </>
            ) : (
              <>
                <div className="space-y-2">
                  <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] ml-1">
                    {useRecoveryCode ? 'Mã dự phòng' : 'Mã xác thực 6 số'}
                  </label>
                  <div className="relative group">
                    <KeyRound className="absolute left-5 top-1/2 -translate-y-1/2 text-slate-300 group-focus-within:text-[#1EB4D4] transition-colors" size={20} />
                    <input
                      type="text"
                      value={useRecoveryCode ? recoveryCode : twoFactorCode}
                      onChange={(event) => (useRecoveryCode ? setRecoveryCode(event.target.value) : setTwoFactorCode(event.target.value))}
                      placeholder={useRecoveryCode ? 'Nhập mã dự phòng' : '123 456'}
                      required
                      className="w-full bg-slate-50 border-2 border-transparent focus:bg-white focus:border-[#1EB4D4]/20 rounded-[1.5rem] py-5 pl-14 pr-6 font-bold text-slate-900 text-sm focus:ring-0 transition-all shadow-sm"
                    />
                  </div>
                </div>

                <div className="flex flex-wrap items-center gap-3 text-xs font-black uppercase tracking-widest">
                  <button
                    type="button"
                    onClick={() => {
                      setUseRecoveryCode((current) => !current);
                      setTwoFactorCode('');
                      setRecoveryCode('');
                      setError('');
                    }}
                    className="text-[#1EB4D4] hover:underline"
                  >
                    {useRecoveryCode ? 'Dùng mã xác thực' : 'Dùng mã dự phòng'}
                  </button>
                  <button
                    type="button"
                    onClick={resetTwoFactorStep}
                    className="text-slate-400 hover:text-slate-600"
                  >
                    Quay lại bước đăng nhập
                  </button>
                </div>
              </>
            )}

            <div className="flex items-center gap-3 py-2">
              <input
                type="checkbox"
                id="remember"
                checked={rememberMe}
                onChange={(event) => setRememberMe(event.target.checked)}
                className="w-5 h-5 rounded-lg border-slate-200 text-[#1EB4D4] focus:ring-[#1EB4D4] transition-all cursor-pointer"
              />
              <label htmlFor="remember" className="text-xs font-bold text-slate-500 cursor-pointer select-none">
                Ghi nhớ đăng nhập
              </label>
            </div>

            <button
              disabled={loading}
              className="w-full bg-gradient-to-r from-[#1EB4D4] to-[#002B7F] text-white py-5 rounded-[1.5rem] font-black text-sm uppercase tracking-widest shadow-xl shadow-blue-500/20 hover:shadow-blue-500/40 hover:-translate-y-1 transition-all active:translate-y-0 flex items-center justify-center gap-3 disabled:opacity-70 disabled:hover:translate-y-0"
            >
              {loading
                ? (isTwoFactorStep ? 'Đang xác minh...' : 'Đang xử lý...')
                : (isTwoFactorStep ? 'Hoàn tất đăng nhập' : 'Đăng nhập ngay')}
              <ArrowRight size={18} />
            </button>
          </form>

          <div className="mt-10 pt-10 border-t border-slate-100 text-center">
            {!isTwoFactorStep ? (
              <p className="text-sm font-bold text-slate-400">
                Chưa có tài khoản?{' '}
                <Link to="/auth/register" className="text-[#1EB4D4] hover:underline">Đăng ký thành viên mới</Link>
              </p>
            ) : (
              <p className="text-sm font-bold text-slate-400">
                Không dùng được mã xác thực? Bạn có thể quay lại và đăng nhập lại để lấy thử thách mới.
              </p>
            )}
          </div>
        </div>
      </motion.div>
    </div>
  );
};

function resolveRedirectPath(user, fromPath) {
  if (fromPath.startsWith('/admin') && canAccessAdmin(user)) {
    return fromPath;
  }

  if (fromPath.startsWith('/tenant') && canAccessTenant(user)) {
    return fromPath;
  }

  if (fromPath.startsWith('/my-account')) {
    return fromPath;
  }

  if (fromPath.startsWith('/admin') || fromPath.startsWith('/tenant')) {
    return getPostLoginPath(user);
  }

  if (fromPath.startsWith('/') && !fromPath.startsWith('/auth')) {
    return fromPath;
  }

  return getPostLoginPath(user);
}

export default LoginPage;
