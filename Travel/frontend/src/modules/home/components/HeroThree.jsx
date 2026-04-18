import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { 
  Plane, 
  Search, 
  MapPin, 
  Calendar, 
  Users, 
  MoveRight, 
  ChevronDown,
  RefreshCw,
  Navigation,
  Map
} from 'lucide-react';

const HeroThree = () => {
  const [activeTab, setActiveTab] = useState('one-way');

  const tabs = [
    { id: 'one-way', label: 'Một lượt', icon: <Navigation size={16} /> },
    { id: 'round-trip', label: 'Khứ hồi', icon: <RefreshCw size={16} /> },
    { id: 'multi-city', label: 'Nhiều thành phố', icon: <Map size={16} /> },
    { id: 'random-trip', label: 'Chuyến đi ngẫu nhiên', icon: <MapPin size={16} /> },
  ];

  return (
    <section className="relative min-h-[130vh] flex flex-col items-center pt-32 pb-80 bg-gray-900">
      {/* Background Image with Dark Overlay */}
      <div className="absolute inset-0 z-0 overflow-hidden">
        <img 
          src="https://images.unsplash.com/photo-1542296332-2e4473faf563?q=80&w=2070&auto=format&fit=crop" 
          alt="Hero Background" 
          className="w-full h-full object-cover opacity-60"
        />
        <div className="absolute inset-0 bg-black/40"></div>
      </div>

      {/* Hero Content */}
      <div className="container mx-auto px-4 relative z-10 text-center text-white">
        <motion.div
          initial={{ opacity: 0, y: -20 }}
          animate={{ opacity: 1, y: 0 }}
          className="flex items-center justify-center gap-2 mb-6"
        >
          <div className="relative">
            <div className="w-1.5 h-1.5 bg-[#1EB4D4] rounded-full"></div>
            <div className="absolute -inset-1 border border-[#1EB4D4]/50 rounded-full animate-ping"></div>
          </div>
          <span className="text-[#1EB4D4] text-xl font-black uppercase tracking-[0.3em]" style={{ fontFamily: "'Kalam', cursive" }}>
            Đặt ngay
          </span>
        </motion.div>

        <motion.h1
          initial={{ opacity: 0, y: 30 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.2 }}
          className="text-5xl md:text-7xl lg:text-8xl font-black mb-10 leading-[1.1]"
        >
          Kỷ Niệm Cả Đời <br /> Chỉ Cách Bạn Vài Ngày
        </motion.h1>

        <motion.p
          initial={{ opacity: 0, y: 30 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.3 }}
          className="text-white/80 text-lg md:text-xl font-medium max-w-4xl mx-auto mb-14 leading-relaxed"
        >
          Biến giấc mơ khám phá thế giới của bạn thành hiện thực là một mục tiêu đầy cảm hứng. 
          Du lịch cho phép bạn trải nghiệm những nền văn hóa, ẩm thực và phong cảnh mới.
        </motion.p>

        <motion.div
          initial={{ opacity: 0, y: 30 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.4 }}
          className="flex flex-col sm:flex-row items-center justify-center gap-6"
        >
          <button className="bg-[#1EB4D4] hover:bg-[#19a7c5] text-white px-10 py-5 rounded-full font-black flex items-center gap-3 transition-all shadow-xl shadow-[#1EB4D4]/30 group">
            Khám phá chuyến bay <MoveRight size={20} className="group-hover:translate-x-2 transition-transform" />
          </button>
          <button className="bg-transparent border-2 border-white/30 hover:bg-white/10 text-white px-10 py-5 rounded-full font-black flex items-center gap-3 transition-all">
            Đặt chỗ ở <MoveRight size={20} />
          </button>
        </motion.div>
      </div>

      {/* Bottom Shape Cutout */}
      <div className="absolute bottom-0 left-0 right-0 h-40 bg-white z-20" style={{ clipPath: 'polygon(0 40%, 15% 40%, 20% 0, 80% 0, 85% 40%, 100% 40%, 100% 100%, 0% 100%)' }}></div>

      {/* Floating Search Box */}
      <div className="absolute bottom-[-100px] left-1/2 -translate-x-1/2 w-full max-w-7xl px-4 z-[60]">
        <motion.div
          initial={{ opacity: 0, y: 50 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.5 }}
          className="bg-white rounded-[2rem] shadow-[0_20px_60px_rgba(0,0,0,0.1)] p-8 md:p-10"
        >
          {/* Tabs */}
          <div className="flex flex-wrap items-center gap-8 mb-10 border-b border-gray-100 pb-6">
            {tabs.map((tab) => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                className={`flex items-center gap-2.5 font-black text-sm uppercase tracking-widest transition-all relative pb-4 ${
                  activeTab === tab.id ? 'text-[#1EB4D4]' : 'text-gray-400 hover:text-gray-600'
                }`}
              >
                {tab.icon}
                {tab.label}
                {activeTab === tab.id && (
                  <motion.div 
                    layoutId="activeTab"
                    className="absolute bottom-0 left-0 right-0 h-1 bg-[#1EB4D4] rounded-full"
                  />
                )}
              </button>
            ))}
          </div>

          {/* Search Inputs */}
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-8">
            {/* From */}
            <div className="relative group">
              <label className="block text-[10px] font-black text-gray-400 uppercase tracking-widest mb-3 pl-1">Thành phố đi</label>
              <div className="flex items-center gap-4 p-4 bg-gray-50 rounded-2xl group-hover:bg-white group-hover:ring-2 ring-[#1EB4D4]/20 transition-all cursor-pointer">
                <div className="w-10 h-10 bg-white rounded-xl flex items-center justify-center text-[#1EB4D4] shadow-sm">
                  <Navigation size={18} />
                </div>
                <div>
                   <p className="font-black text-gray-900 leading-none">New York</p>
                   <p className="text-[10px] text-gray-400 font-bold mt-1">Sân bay JFK, Mỹ</p>
                </div>
              </div>
            </div>

            {/* To */}
            <div className="relative group">
              <label className="block text-[10px] font-black text-gray-400 uppercase tracking-widest mb-3 pl-1">Thành phố đến</label>
              <div className="flex items-center gap-4 p-4 bg-gray-50 rounded-2xl group-hover:bg-white group-hover:ring-2 ring-[#1EB4D4]/20 transition-all cursor-pointer">
                <div className="w-10 h-10 bg-white rounded-xl flex items-center justify-center text-[#1EB4D4] shadow-sm">
                  <MapPin size={18} />
                </div>
                <div>
                   <p className="font-black text-gray-900 leading-none">Paris</p>
                   <p className="text-[10px] text-gray-400 font-bold mt-1">Sân bay CDG, Pháp</p>
                </div>
              </div>
            </div>

            {/* Date */}
            <div className="relative group">
              <label className="block text-[10px] font-black text-gray-400 uppercase tracking-widest mb-3 pl-1">Ngày đi / Về</label>
              <div className="flex items-center gap-4 p-4 bg-gray-50 rounded-2xl group-hover:bg-white group-hover:ring-2 ring-[#1EB4D4]/20 transition-all cursor-pointer">
                <div className="w-10 h-10 bg-white rounded-xl flex items-center justify-center text-[#1EB4D4] shadow-sm">
                  <Calendar size={18} />
                </div>
                <div>
                   <p className="font-black text-gray-900 leading-none">12 Th9 - 25 Th9</p>
                   <p className="text-[10px] text-gray-400 font-bold mt-1">Chọn ngày của bạn</p>
                </div>
              </div>
            </div>

            {/* Passengers / Search */}
            <div className="flex items-end gap-3">
              <div className="flex-1 relative group">
                <label className="block text-[10px] font-black text-gray-400 uppercase tracking-widest mb-3 pl-1">Hành khách</label>
                <div className="flex items-center gap-4 p-4 bg-gray-50 rounded-2xl group-hover:bg-white group-hover:ring-2 ring-[#1EB4D4]/20 transition-all cursor-pointer">
                  <div className="w-10 h-10 bg-white rounded-xl flex items-center justify-center text-[#1EB4D4] shadow-sm">
                    <Users size={18} />
                  </div>
                  <div>
                    <p className="font-black text-gray-900 leading-none">02 Người</p>
                    <p className="text-[10px] text-gray-400 font-bold mt-1">Hạng phổ thông</p>
                  </div>
                  <ChevronDown size={14} className="ml-auto text-gray-300" />
                </div>
              </div>
              <button className="w-18 h-18 bg-[#1EB4D4] hover:bg-gray-900 text-white rounded-2xl flex items-center justify-center transition-all shadow-xl shadow-[#1EB4D4]/20 group/btn">
                <Search size={22} className="group-hover/btn:scale-110 transition-transform" />
              </button>
            </div>
          </div>
        </motion.div>
      </div>
    </section>
  );
};

export default HeroThree;
