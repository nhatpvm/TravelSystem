import React, { useEffect, useState } from 'react';
import {
  Facebook,
  Twitter,
  Linkedin,
  Instagram,
  Mail,
  Clock,
  Phone,
  ChevronDown,
  Search,
  MoveRight,
  LogIn,
  User,
  Settings,
} from 'lucide-react';
import { Link, useNavigate } from 'react-router-dom';
import logo from '../../../assets/logo.png';
import nav1 from '../../../assets/nav1.png';
import nav2 from '../../../assets/nav2.png';
import nav3 from '../../../assets/nav3.png';
import { logout } from '../../../services/auth';
import { useAuthSession } from '../../auth/hooks/useAuthSession';
import { canAccessAdmin, canAccessTenant } from '../../auth/types';

const Navbar = () => {
  const navigate = useNavigate();
  const { user, isAuthenticated } = useAuthSession();
  const [isSticky, setIsSticky] = useState(false);

  const handleLogout = async () => {
    await logout();
    navigate('/auth/login', { replace: true });
  };

  useEffect(() => {
    const handleScroll = () => {
      if (window.scrollY > 300) {
        setIsSticky(true);
      } else {
        setIsSticky(false);
      }
    };

    handleScroll();
    window.addEventListener('scroll', handleScroll);

    return () => window.removeEventListener('scroll', handleScroll);
  }, []);

  const settingsTarget = canAccessAdmin(user)
    ? '/admin'
    : canAccessTenant(user)
      ? '/tenant'
      : isAuthenticated
        ? '/my-account/profile'
        : '/auth/login';

  const renderNavContent = (sticky = false) => (
    <nav className={`
      bg-white flex justify-between items-center w-full transition-all duration-300
      ${sticky ? 'fixed top-0 left-0 shadow-2xl animate-slide-down z-[1000] px-4 lg:px-12' : 'relative'}
      ${sticky ? 'py-4 min-h-[90px]' : 'py-0 min-h-[100px]'}
    `}>
      {sticky ? (
        <div className="flex items-center gap-3">
          <img
            src={logo}
            alt="Travel Logo"
            className="h-14 lg:h-18"
          />
          <span className="text-2xl font-black text-gray-900 tracking-tighter">2TMNY</span>
        </div>
      ) : (
        <>
          <div className="hidden lg:flex absolute top-0 left-0 h-full bg-[#1EB4D4] items-center pl-10 pr-12 z-20 transition-all duration-300 w-[18%]" style={{ clipPath: 'polygon(0 0, 85% 0, 100% 100%, 0% 100%)' }}>
            <div className="flex items-center space-x-4 text-white">
              <div className="flex items-center gap-3">
                <img
                  src={logo}
                  alt="Travel Logo"
                  className="h-28 brightness-0 invert"
                />
                <span className="text-3xl font-black tracking-tighter">2TMNY</span>
              </div>
            </div>
          </div>
          <div className="hidden lg:block lg:w-[25%]"></div>
        </>
      )}

      {!sticky && (
        <div className="lg:hidden flex items-center px-8 gap-3">
          <img src={logo} alt="Travel Logo" className="h-14 brightness-0 invert" />
          <span className="text-xl font-black text-gray-900 tracking-tighter">2TMNY</span>
        </div>
      )}

      <div className={`
        hidden lg:flex items-center lg:px-2 space-x-0 text-[#3b3b3b] font-bold text-[13px] xl:text-[14px] h-full
        ${sticky ? 'ml-auto' : 'ml-auto lg:mr-4 xl:mr-8'}
      `}>
        <div className="relative group flex items-center lg:ml-4">
          <Link to="/" className="flex items-center whitespace-nowrap hover:text-[#1EB4D4] transition-colors px-2 xl:px-3 py-2">
            Trang chủ <ChevronDown size={14} className="ml-1" />
          </Link>
          <div className="absolute top-full left-1/2 -translate-x-1/2 w-[820px] bg-white shadow-[0_20px_60px_rgba(0,0,0,0.12)] invisible group-hover:visible opacity-0 group-hover:opacity-100 transition-all duration-300 transform translate-y-4 group-hover:translate-y-0 z-50 rounded-2xl border border-gray-100 mt-2 p-6">
            <div className="grid grid-cols-3 gap-6 text-center">
              <Link to="/" className="group/card">
                <div className="rounded-xl overflow-hidden border-2 border-transparent group-hover/card:border-[#1EB4D4] transition-all duration-300 shadow-sm">
                  <img src={nav1} alt="Tour" className="w-full h-40 object-cover" />
                </div>
                <p className="mt-2 font-bold text-gray-700 group-hover/card:text-[#1EB4D4]">Tour Du Lịch</p>
              </Link>
              <Link to="/home-2" className="group/card">
                <div className="rounded-xl overflow-hidden border-2 border-transparent group-hover/card:border-[#1EB4D4] transition-all duration-300 shadow-sm">
                  <img src={nav2} alt="Hotel" className="w-full h-40 object-cover" />
                </div>
                <p className="mt-2 font-bold text-gray-700 group-hover/card:text-[#1EB4D4]">Khách Sạn</p>
              </Link>
              <Link to="/home-3" className="group/card">
                <div className="rounded-xl overflow-hidden border-2 border-transparent group-hover/card:border-[#1EB4D4] transition-all duration-300 shadow-sm">
                  <img src={nav3} alt="Flight" className="w-full h-40 object-cover" />
                </div>
                <p className="mt-2 font-bold text-gray-700 group-hover/card:text-[#1EB4D4]">Chuyến Bay</p>
              </Link>
            </div>
          </div>
        </div>

        <div className="relative group flex items-center">
          <button className="flex items-center whitespace-nowrap hover:text-[#1EB4D4] transition-colors px-2 xl:px-3 py-2">
            Dịch vụ <ChevronDown size={14} className="ml-1" />
          </button>
          <div className="absolute top-full left-0 w-64 bg-white shadow-2xl invisible group-hover:visible opacity-0 group-hover:opacity-100 transition-all duration-300 transform translate-y-4 group-hover:translate-y-0 z-50 border-t-4 border-[#1EB4D4] mt-2">
            <div className="py-2 flex flex-col font-medium">
              <Link to="/tours" className="px-6 py-3 text-gray-800 hover:text-[#1EB4D4] hover:bg-gray-50 transition-colors">Tour du lịch</Link>
              <Link to="/flight/results" className="px-6 py-3 text-gray-800 hover:text-[#1EB4D4] hover:bg-gray-50 transition-colors">Vé máy bay</Link>
              <Link to="/bus/results" className="px-6 py-3 text-gray-800 hover:text-[#1EB4D4] hover:bg-gray-50 transition-colors">Vé xe khách</Link>
              <Link to="/train/results" className="px-6 py-3 text-gray-800 hover:text-[#1EB4D4] hover:bg-gray-50 transition-colors">Vé tàu hỏa</Link>
              <Link to="/hotel/results" className="px-6 py-3 text-gray-800 hover:text-[#1EB4D4] hover:bg-gray-50 transition-colors">Khách sạn</Link>
            </div>
          </div>
        </div>

        <div className="relative group flex items-center">
          <button className="flex items-center whitespace-nowrap hover:text-[#1EB4D4] transition-colors px-2 xl:px-3 py-2">
            Khám phá <ChevronDown size={14} className="ml-1" />
          </button>
          <div className="absolute top-full left-0 w-64 bg-white shadow-2xl invisible group-hover:visible opacity-0 group-hover:opacity-100 transition-all duration-300 transform translate-y-4 group-hover:translate-y-0 z-50 border-t-4 border-[#1EB4D4] mt-2">
            <div className="py-2 flex flex-col font-medium">
              <Link to="/destinations" className="px-6 py-3 text-gray-800 hover:text-[#1EB4D4] hover:bg-gray-50 transition-colors">Điểm đến</Link>
              <Link to="/activities" className="px-6 py-3 text-gray-800 hover:text-[#1EB4D4] hover:bg-gray-50 transition-colors">Hoạt động trải nghiệm</Link>
              <Link to="/blog" className="px-6 py-3 text-gray-800 hover:text-[#1EB4D4] hover:bg-gray-50 transition-colors">Blog du lịch</Link>
              <Link to="/promotions" className="px-6 py-3 text-gray-800 hover:text-[#1EB4D4] hover:bg-gray-50 transition-colors">Ưu đãi hấp dẫn</Link>
              <div className="mx-6 my-2 h-px bg-gray-100" />
              <Link to="/tenant/onboarding" className="px-6 py-3 font-bold text-[#1EB4D4] hover:bg-cyan-50 transition-colors">Đăng ký đối tác</Link>
            </div>
          </div>
        </div>

        <div className="relative group flex items-center">
          <button className="flex items-center whitespace-nowrap hover:text-[#1EB4D4] transition-colors px-2 xl:px-3 py-2">
            Thông tin <ChevronDown size={14} className="ml-1" />
          </button>
          <div className="absolute top-full left-0 w-64 bg-white shadow-2xl invisible group-hover:visible opacity-0 group-hover:opacity-100 transition-all duration-300 transform translate-y-4 group-hover:translate-y-0 z-50 border-t-4 border-[#1EB4D4] mt-2">
            <div className="py-2 flex flex-col font-medium">
              <Link to="/about" className="px-6 py-3 text-gray-800 hover:text-[#1EB4D4] hover:bg-gray-50 transition-colors">Về 2TMNY</Link>
              <Link to="/team" className="px-6 py-3 text-gray-800 hover:text-[#1EB4D4] hover:bg-gray-50 transition-colors">Đội ngũ</Link>
              <Link to="/faq" className="px-6 py-3 text-gray-800 hover:text-[#1EB4D4] hover:bg-gray-50 transition-colors">Câu hỏi thường gặp</Link>
              <Link to="/contact" className="px-6 py-3 text-gray-800 hover:text-[#1EB4D4] hover:bg-gray-50 transition-colors">Liên hệ hỗ trợ</Link>
            </div>
          </div>
        </div>

        {isAuthenticated ? (
          <div className="relative group flex items-center">
            <Link to="/my-account/profile" className="flex items-center whitespace-nowrap hover:text-[#1EB4D4] transition-colors px-2 xl:px-4 py-2">
              <User size={16} className="mr-1.5" /> Tài khoản <ChevronDown size={14} className="ml-1" />
            </Link>
            <div className="absolute top-full right-0 w-64 bg-white shadow-2xl invisible group-hover:visible opacity-0 group-hover:opacity-100 transition-all duration-300 transform translate-y-4 group-hover:translate-y-0 z-50 border-t-4 border-[#1EB4D4] mt-2">
              <div className="py-4 flex flex-col font-medium text-left">
                <Link to="/my-account/profile" className="px-8 py-3 text-gray-800 hover:text-[#1EB4D4] hover:bg-gray-50 transition-colors">Thông tin cá nhân</Link>
                <Link to="/my-account/bookings" className="px-8 py-3 text-gray-800 hover:text-[#1EB4D4] hover:bg-gray-50 transition-colors">Đơn đặt của tôi</Link>
                {canAccessTenant(user) && (
                  <>
                    <div className="h-px bg-gray-100 my-2" />
                    <Link to="/tenant" className="px-8 py-3 text-purple-600 hover:bg-purple-50 transition-colors font-bold">Tenant Portal</Link>
                  </>
                )}
                {canAccessAdmin(user) && (
                  <>
                    <div className="h-px bg-gray-100 my-2" />
                    <Link to="/admin" className="px-8 py-3 text-slate-900 hover:bg-slate-50 transition-colors font-bold">Admin Portal</Link>
                  </>
                )}
                <div className="h-px bg-gray-100 my-2" />
                <button onClick={handleLogout} className="px-8 py-3 text-rose-500 hover:bg-rose-50 transition-colors text-left font-semibold">Đăng xuất</button>
              </div>
            </div>
          </div>
        ) : (
          <Link
            to="/auth/login"
            className="flex items-center gap-1.5 whitespace-nowrap font-bold text-[#1EB4D4] hover:text-[#002B7F] transition-colors px-2 xl:px-4 py-2"
          >
            <LogIn size={16} /> Đăng nhập
          </Link>
        )}

        <div className="flex items-center space-x-3 ml-3 xl:ml-6 border-l pl-3 xl:pl-6 border-gray-100">
          <button className="text-gray-900 hover:text-[#1EB4D4] transition-colors">
            <Search size={20} />
          </button>
          <Link to="/contact" className="bg-[#1EB4D4] text-white px-4 xl:px-7 py-3 xl:py-4 rounded-full flex items-center space-x-2 font-bold text-[13px] xl:text-[15px] hover:bg-[#19a7c5] transition-all shadow-xl whitespace-nowrap">
            <span className="hidden xl:inline">Yêu cầu báo giá</span>
            <span className="xl:hidden">Báo giá</span>
            <MoveRight size={18} />
          </Link>
          <Link to={settingsTarget} className="bg-slate-900 text-white p-3 xl:p-4 rounded-full flex items-center justify-center hover:bg-[#1EB4D4] transition-all shadow-xl">
            <Settings size={20} />
          </Link>
        </div>
      </div>
    </nav>
  );

  return (
    <div className="w-full flex flex-col relative">
      {!isSticky && (
        <div className="hidden lg:flex absolute top-0 left-0 h-full w-[25%] bg-[#1EB4D4] items-center pl-10 pr-12 z-30 transition-all duration-300" style={{ clipPath: 'polygon(0 0, 85% 0, 100% 100%, 0% 100%)' }}>
          <div className="flex items-center space-x-4 text-white py-12">
            <div className="flex items-center gap-4">
              <img
                src={logo}
                alt="Travel Logo"
                className="h-32 brightness-0 invert"
              />
              <span className="text-4xl font-black tracking-tighter">2TMNY</span>
            </div>
          </div>
        </div>
      )}

      <div className="bg-[#DDEEF0] py-4 px-4 md:px-12 flex flex-col md:flex-row justify-between items-center text-[14px] text-[#444] relative z-20">
        <div className="flex items-center space-x-6 relative z-40 lg:ml-[25%] transition-all duration-300">
          <span className="font-semibold text-gray-600">Theo dõi chúng tôi</span>
          <div className="flex items-center space-x-4">
            <Facebook size={16} className="cursor-pointer hover:text-[#1EB4D4] transition-colors" />
            <Twitter size={16} className="cursor-pointer hover:text-[#1EB4D4] transition-colors" />
            <Linkedin size={16} className="cursor-pointer hover:text-[#1EB4D4] transition-colors" />
            <Instagram size={16} className="cursor-pointer hover:text-[#1EB4D4] transition-colors" />
          </div>
        </div>

        <div className="flex flex-col md:flex-row items-center space-y-2 md:space-y-0 md:space-x-8 mt-2 md:mt-0 font-semibold relative z-40">
          <div className="flex items-center space-x-2">
            <Mail size={16} className="text-[#1EB4D4]" />
            <span>info@touron.com</span>
          </div>
          <div className="flex items-center space-x-2 border-l border-gray-300 pl-8">
            <Clock size={16} className="text-[#1EB4D4]" />
            <span>Chủ nhật đến Thứ sáu: 8:00 sáng - 7:00 tối, Úc</span>
          </div>
          <div className="flex items-center space-x-2 border-l border-gray-300 pl-8">
            <Phone size={16} className="text-[#1EB4D4]" />
            <span>+256 214 203 215</span>
          </div>
        </div>
      </div>

      {renderNavContent()}
      {isSticky && renderNavContent(true)}
    </div>
  );
};

export default Navbar;
