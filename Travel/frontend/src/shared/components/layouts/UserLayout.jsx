import React from 'react';
import { Outlet, Link, useLocation, useNavigate } from 'react-router-dom';
import {
  User,
  ShoppingBag,
  Users,
  CreditCard,
  Shield,
  LogOut,
  Plane,
  Star,
  MapPin,
  Heart,
  Bell,
  ReceiptText,
  Settings,
  FileText,
} from 'lucide-react';
import { motion } from 'framer-motion';
import Navbar from '../../../modules/home/components/Navbar';
import Footer from '../../../modules/home/components/Footer';
import { logout } from '../../../services/auth';
import { useAuthSession } from '../../../modules/auth/hooks/useAuthSession';
import { getUserDisplayName } from '../../../modules/auth/types';

const menuItems = [
  { icon: User, label: 'Thông tin cá nhân', sub: 'Hồ sơ lữ khách', path: '/my-account/profile' },
  { icon: ShoppingBag, label: 'Đơn hàng của tôi', sub: 'Theo dõi vòng đời đơn', path: '/my-account/bookings' },
  { icon: Users, label: 'Hành khách đã lưu', sub: 'Điền nhanh khi checkout', path: '/my-account/passengers' },
  { icon: Heart, label: 'Danh sách yêu thích', sub: 'Khách sạn và tour đã lưu', path: '/my-account/wishlist' },
  { icon: Bell, label: 'Thông báo', sub: 'Cập nhật thanh toán và hậu mãi', path: '/my-account/notifications' },
  { icon: CreditCard, label: 'Thanh toán', sub: 'Phương thức hỗ trợ và bảo mật', path: '/my-account/payments' },
  { icon: ReceiptText, label: 'Lịch sử giao dịch', sub: 'Theo dõi payment và hoàn tiền', path: '/my-account/payment-history' },
  { icon: FileText, label: 'Hóa đơn VAT', sub: 'Yêu cầu và tra cứu hóa đơn', path: '/my-account/vat-invoice' },
  { icon: Shield, label: 'Bảo mật', sub: 'Mật khẩu và phiên đăng nhập', path: '/my-account/security' },
  { icon: Settings, label: 'Cài đặt', sub: 'Tùy chọn tài khoản', path: '/my-account/settings' },
];

const DEFAULT_AVATAR = 'https://images.unsplash.com/photo-1539571696357-5a69c17a67c6?auto=format&fit=crop&q=80&w=200';

const UserLayout = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const { user } = useAuthSession();

  const handleLogout = async () => {
    await logout();
    navigate('/auth/login', { replace: true });
  };

  return (
    <div className="min-h-screen bg-[#F0F4F8] flex flex-col font-sans">
      <Navbar />

      <main className="flex-1 pt-32 pb-24 relative">
        <div className="pointer-events-none fixed inset-0 overflow-hidden">
          <div className="absolute top-0 right-0 w-[700px] h-[700px] bg-[#1EB4D4]/8 rounded-full blur-[140px] -mr-96 -mt-96" />
          <div className="absolute bottom-0 left-0 w-[500px] h-[500px] bg-[#002B7F]/6 rounded-full blur-[120px] -ml-64 -mb-64" />
        </div>

        <div className="container mx-auto px-4 lg:px-16 xl:px-24 relative z-10">
          <div className="flex flex-col lg:flex-row gap-8 xl:gap-12">
            <aside className="w-full lg:w-80 xl:w-[340px] shrink-0">
              <div className="sticky top-32 space-y-4">
                <div className="relative rounded-[2.5rem] overflow-hidden shadow-2xl shadow-slate-200/50">
                  <img
                    src="https://images.unsplash.com/photo-1501785888041-af3ef285b470?auto=format&fit=crop&q=80&w=600"
                    alt="sidebar bg"
                    className="absolute inset-0 w-full h-full object-cover"
                  />
                  <div className="absolute inset-0 bg-gradient-to-b from-slate-900/70 via-slate-900/80 to-slate-900/95" />

                  <div className="relative z-10 p-8 text-center">
                    <div className="relative inline-block mb-4">
                      <div className="w-20 h-20 rounded-2xl overflow-hidden border-2 border-white/30 shadow-2xl ring-4 ring-white/10 mx-auto">
                        <img
                          src={user?.avatarUrl || DEFAULT_AVATAR}
                          alt="avatar"
                          className="w-full h-full object-cover"
                        />
                      </div>
                      <div className="absolute -bottom-2 -right-2 w-7 h-7 bg-[#1EB4D4] rounded-xl flex items-center justify-center shadow-lg">
                        <Plane size={13} className="text-white" />
                      </div>
                    </div>

                    <h3 className="font-black text-white text-xl tracking-tight">{getUserDisplayName(user)}</h3>
                    <p className="flex items-center justify-center gap-1 text-white/50 text-xs font-bold mt-1">
                      <MapPin size={11} /> {user?.email || 'Tài khoản khách hàng'}
                    </p>

                    <div className="inline-flex items-center gap-1.5 mt-3 px-4 py-1.5 bg-amber-400/20 border border-amber-400/30 rounded-full">
                      <Star size={11} className="text-amber-300" fill="currentColor" />
                      <span className="text-amber-200 text-[10px] font-black uppercase tracking-widest">Thành viên nền tảng</span>
                    </div>

                    <div className="grid grid-cols-3 gap-2 mt-6">
                      {[['Đơn', 'Đang theo dõi'], ['Ví', 'SePay'], ['VAT', 'Hỗ trợ']].map(([v, l], i) => (
                        <div key={i} className="bg-white/8 border border-white/10 rounded-2xl p-2.5">
                          <p className="text-white font-black text-base">{v}</p>
                          <p className="text-white/40 text-[9px] font-bold uppercase tracking-wider">{l}</p>
                        </div>
                      ))}
                    </div>
                  </div>
                </div>

                <div className="bg-white rounded-[2rem] shadow-xl shadow-slate-100/50 p-3">
                  <nav className="space-y-1">
                    {menuItems.map((item) => {
                      const active = location.pathname === item.path;
                      return (
                        <Link
                          key={item.path}
                          to={item.path}
                          className={`flex items-center gap-4 px-5 py-4 rounded-[1.25rem] transition-all duration-300 group ${
                            active
                              ? 'bg-gradient-to-r from-[#1EB4D4] to-[#002B7F] text-white shadow-xl shadow-sky-500/25'
                              : 'text-slate-500 hover:bg-slate-50 hover:text-slate-900'
                          }`}
                        >
                          <div className={`w-9 h-9 rounded-xl flex items-center justify-center shrink-0 transition-all ${
                            active ? 'bg-white/20' : 'bg-slate-100 group-hover:bg-[#1EB4D4]/10 group-hover:text-[#1EB4D4]'
                          }`}
                          >
                            <item.icon size={17} strokeWidth={active ? 2.5 : 2} />
                          </div>
                          <div className="min-w-0 flex-1">
                            <p className="text-[13px] font-black truncate">{item.label}</p>
                            <p className={`text-[10px] font-bold truncate ${active ? 'text-white/60' : 'text-slate-400'}`}>{item.sub}</p>
                          </div>
                        </Link>
                      );
                    })}
                  </nav>

                  <div className="mt-3 pt-3 border-t border-slate-100">
                    <button
                      type="button"
                      onClick={handleLogout}
                      className="flex items-center justify-center gap-2 w-full px-5 py-3.5 rounded-[1.25rem] bg-rose-50 text-rose-500 hover:bg-rose-500 hover:text-white transition-all duration-300 font-black text-xs uppercase tracking-widest"
                    >
                      <LogOut size={16} /> Đăng xuất
                    </button>
                  </div>
                </div>

                <div className="relative rounded-[2rem] overflow-hidden">
                  <img
                    src="https://images.unsplash.com/photo-1507525428034-b723cf961d3e?auto=format&fit=crop&q=80&w=600"
                    alt="travel tip"
                    className="w-full h-32 object-cover"
                  />
                  <div className="absolute inset-0 bg-gradient-to-t from-[#002B7F]/90 to-transparent" />
                  <div className="absolute bottom-0 left-0 p-5">
                    <p className="text-white font-black text-sm leading-tight">Customer Commerce Hub</p>
                    <p className="text-white/70 text-xs italic mt-0.5" style={{ fontFamily: "'Kalam', cursive" }}>
                      Theo dõi đơn, payment, ticket và hậu mãi trên cùng một nơi.
                    </p>
                  </div>
                </div>
              </div>
            </aside>

            <div className="flex-1 min-w-0">
              <motion.div
                key={location.pathname}
                initial={{ opacity: 0, x: 16 }}
                animate={{ opacity: 1, x: 0 }}
                transition={{ duration: 0.4, ease: [0.22, 1, 0.36, 1] }}
              >
                <Outlet />
              </motion.div>
            </div>
          </div>
        </div>
      </main>

      <Footer />
    </div>
  );
};

export default UserLayout;
