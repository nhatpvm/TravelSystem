import React from 'react';
import { Facebook, Twitter, Linkedin, Instagram, MapPin, Mail, Phone, ArrowRight, Plane } from 'lucide-react';
import { motion } from 'framer-motion';
import { Link } from 'react-router-dom';
import logo from '../../../assets/logo.png';

const Footer = () => {
  return (
    <footer className="relative bg-gray-900 text-white pt-24 pb-8 overflow-hidden">
      {/* Background Image with Overlay */}
      <div className="absolute inset-0 z-0 opacity-20">
        <img 
          src="https://images.unsplash.com/photo-1507525428034-b723cf961d3e?q=80&w=2073&auto=format&fit=crop" 
          alt="Island footer background" 
          className="w-full h-full object-cover"
        />
      </div>

      <div className="container mx-auto px-4 md:px-12 lg:px-24 relative z-10">
        <div className="flex flex-col lg:flex-row gap-12 mb-16">
          
          {/* Newsletter / Profile Column */}
          <div className="w-full lg:w-1/3 bg-[#1A1D1F]/80 backdrop-blur-xl p-10 rounded-[2.5rem] border border-white/5 relative overflow-hidden group text-center">
             {/* Logo */}
             <div className="flex justify-center mb-10">
                <div className="flex items-center gap-4">
                   <img src={logo} alt="logo" className="h-32" />
                   <span className="text-4xl font-black text-white tracking-tighter">2TMNY</span>
                </div>
             </div>

             <h3 className="text-2xl font-black mb-4">Đăng ký bản tin</h3>
             <p className="text-gray-400 text-sm mb-8 font-medium">Nhận các ưu đãi và cập nhật mới nhất từ chúng tôi</p>

             <div className="space-y-4 mb-10">
                <input 
                  type="email" 
                  placeholder="Địa chỉ Email của bạn" 
                  className="w-full bg-white text-gray-900 px-6 py-4 rounded-full font-bold focus:outline-none placeholder:text-gray-300"
                />
                <button className="w-full bg-[#1EB4D4] hover:bg-[#19a7c5] text-white py-4 rounded-full font-black flex items-center justify-center gap-2 transition-all shadow-xl shadow-[#1EB4D4]/20 group/btn">
                  Đăng ký <ArrowRight size={20} className="group-hover/btn:translate-x-1 transition-transform" />
                </button>
             </div>

             {/* Social Links */}
             <div className="flex items-center justify-center gap-4">
                {[Facebook, Twitter, Linkedin, Instagram].map((Icon, i) => (
                  <a key={i} href="#" className="w-10 h-10 bg-white text-gray-900 rounded-full flex items-center justify-center hover:bg-[#1EB4D4] hover:text-white transition-all transform hover:-translate-y-1">
                    <Icon size={18} fill="currentColor" stroke="0" />
                  </a>
                ))}
             </div>
          </div>

          {/* Quick Links & Services Columns */}
          <div className="flex-1 grid grid-cols-1 md:grid-cols-3 gap-12 lg:pl-12 pt-8">
            
            {/* Quick Links */}
            <div>
              <h4 className="text-xl font-black mb-8 relative inline-block">
                Liên kết nhanh
                <div className="absolute -bottom-2 left-0 w-10 h-[2px] bg-[#1EB4D4]"></div>
              </h4>
              <ul className="space-y-4">
                {[
                  { name: 'Trang chủ', path: '/' },
                  { name: 'Về chúng tôi', path: '/about' },
                  { name: 'Blog', path: '/' },
                  { name: 'Dịch vụ', path: '/destinations' },
                  { name: 'Tour', path: '/tours' }
                ].map((item) => (
                  <li key={item.name}>
                    <Link to={item.path} className="text-gray-400 hover:text-[#1EB4D4] font-bold transition-colors">{item.name}</Link>
                  </li>
                ))}
              </ul>
            </div>

            {/* Services */}
            <div>
              <h4 className="text-xl font-black mb-8 relative inline-block">
                Dịch vụ
                <div className="absolute -bottom-2 left-0 w-10 h-[2px] bg-[#1EB4D4]"></div>
              </h4>
              <ul className="space-y-4">
                {['Phiêu lưu lãng du', 'Du lịch xuyên cầu', 'Dịch vụ du lịch Odyssey', 'Hành trình máy bay', 'Du lịch điểm đến trong mơ'].map((item) => (
                  <li key={item}>
                    <a href="#" className="text-gray-400 hover:text-[#1EB4D4] font-bold transition-colors">{item}</a>
                  </li>
                ))}
              </ul>
            </div>

            {/* Contact Us */}
            <div>
              <h4 className="text-xl font-black mb-8 relative inline-block">
                Liên hệ với chúng tôi
                <div className="absolute -bottom-2 left-0 w-10 h-[2px] bg-[#1EB4D4]"></div>
              </h4>
              <div className="space-y-6">
                 <div className="flex gap-4">
                    <div className="w-10 h-10 bg-[#1EB4D4] rounded-lg shrink-0 flex items-center justify-center shadow-lg shadow-[#1EB4D4]/30">
                       <MapPin size={18} />
                    </div>
                    <p className="text-gray-400 font-bold text-sm leading-relaxed">
                       9550 Bolsa Ave #126, <br /> Mỹ
                    </p>
                 </div>
                 <div className="flex gap-4">
                    <div className="w-10 h-10 bg-[#1EB4D4] rounded-lg shrink-0 flex items-center justify-center shadow-lg shadow-[#1EB4D4]/30">
                       <Mail size={18} />
                    </div>
                    <p className="text-gray-400 font-bold text-sm">Info@Touron.Com</p>
                 </div>
                 <div className="flex gap-4">
                    <div className="w-10 h-10 bg-[#1EB4D4] rounded-lg shrink-0 flex items-center justify-center shadow-lg shadow-[#1EB4D4]/30">
                       <Phone size={18} />
                    </div>
                    <p className="text-gray-400 font-bold text-sm">
                       +256 214 203 215 <br /> +1 098 765 4321
                    </p>
                 </div>
              </div>
            </div>

          </div>
        </div>

        {/* Copyright Line */}
        <div className="border-t border-white/5 pt-10 flex flex-col md:flex-row items-center justify-between gap-6">
           <p className="text-gray-500 font-bold text-sm">
             Bản quyền © <span className="text-[#1EB4D4]">Turmet</span>, Bảo lưu mọi quyền.
           </p>
           <div className="flex items-center gap-8">
              <a href="#" className="text-gray-500 hover:text-white text-sm font-bold transition-colors">Điều khoản sử dụng</a>
              <a href="#" className="text-gray-500 hover:text-white text-sm font-bold transition-colors">Chính sách bảo mật & môi trường</a>
           </div>
        </div>
      </div>
    </footer>
  );
};

export default Footer;
