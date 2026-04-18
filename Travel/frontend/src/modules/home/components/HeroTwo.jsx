import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { Search, ChevronDown, Calendar, MapPin, DollarSign, Briefcase } from 'lucide-react';

const HeroTwo = () => {
    const [activeTab, setActiveTab] = useState('Tour');

    const tabs = ['Hotels', 'Tour', 'Flight'];

    return (
        <section className="relative min-h-screen flex flex-col pt-32 pb-40 overflow-hidden bg-gray-900">
            {/* Background Image with Overlay */}
            <div className="absolute inset-0 z-0">
                <img 
                    src="https://images.unsplash.com/photo-1573843981267-be1999ff37cd?q=80&w=2074&auto=format&fit=crop" 
                    alt="Travel Background" 
                    className="w-full h-full object-cover"
                />
                <div className="absolute inset-0 bg-black/40"></div>
            </div>

            {/* Content Container */}
            <div className="container mx-auto px-4 md:px-12 lg:px-24 relative z-10 flex flex-col items-start justify-center text-left mt-20">
                <motion.p 
                    initial={{ opacity: 0, y: 20 }}
                    animate={{ opacity: 1, y: 0 }}
                    className="text-[#1EB4D4] text-xl font-medium mb-6 italic"
                    style={{ fontFamily: "'Kalam', cursive" }}
                >
                    Tận hưởng niềm vui khó quên cùng chúng tôi
                </motion.p>
                
                <motion.h1 
                    initial={{ opacity: 0, y: 30 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ delay: 0.2 }}
                    className="text-5xl md:text-6xl lg:text-7xl font-black text-white mb-8 tracking-tighter max-w-5xl leading-tight"
                >
                    Khám phá hành trình kế tiếp <br /> được chọn riêng cho bạn
                </motion.h1>

                <motion.p 
                    initial={{ opacity: 0, y: 20 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ delay: 0.3 }}
                    className="text-white/80 text-lg md:text-xl max-w-4xl font-medium leading-relaxed mb-16"
                >
                    Chúng tôi mang đến những chuyến đi độc đáo, những trải nghiệm chân thực và những kỷ niệm khó quên tại những điểm đến tuyệt vời nhất trên thế giới.
                </motion.p>
            </div>

            {/* Search Box Section */}
            <div className="container mx-auto px-4 md:px-12 lg:px-24 relative z-20 -mb-24 mt-auto pb-10">
                <motion.div 
                    initial={{ opacity: 0, y: 50 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ delay: 0.4 }}
                    className="bg-white rounded-[3rem] p-10 shadow-2xl shadow-black/20"
                >
                    <div className="flex flex-col md:flex-row justify-between items-center mb-10 gap-6 border-b border-gray-50 pb-8">
                        <h3 className="text-2xl font-black text-gray-900">Tìm kiếm nơi tuyệt nhất</h3>
                        
                        {/* Tabs */}
                        <div className="flex bg-[#F8FBFB] p-1 rounded-full border border-gray-100">
                            {tabs.map((tab) => (
                                <button
                                    key={tab}
                                    onClick={() => setActiveTab(tab)}
                                    className={`px-8 py-2.5 rounded-full text-xs font-black uppercase tracking-wider transition-all ${activeTab === tab ? 'bg-black text-white' : 'text-gray-400 hover:text-gray-900'}`}
                                >
                                    {tab === 'Hotels' ? 'Khách sạn' : tab === 'Tour' ? 'Tour' : 'Chuyến bay'}
                                </button>
                            ))}
                        </div>
                    </div>

                    {/* Search Form */}
                    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-6 items-end">
                        {/* Looking For */}
                        <div className="space-y-3">
                            <label className="text-[11px] font-black text-gray-900 uppercase tracking-widest block">Bạn đang tìm?</label>
                            <div className="relative">
                                <input 
                                    type="text" 
                                    placeholder="Tên Tour" 
                                    className="w-full bg-[#F8FBFB] py-4 px-6 rounded-xl font-bold text-gray-800 focus:outline-none placeholder:text-gray-300 text-sm"
                                />
                            </div>
                        </div>

                        {/* Price */}
                        <div className="space-y-3">
                            <label className="text-[11px] font-black text-gray-900 uppercase tracking-widest block">Giá</label>
                            <div className="relative">
                                <select className="w-full bg-[#F8FBFB] py-4 px-6 rounded-xl font-bold text-gray-400 appearance-none focus:outline-none text-sm cursor-pointer">
                                    <option>Giá</option>
                                    <option>$100 - $500</option>
                                    <option>$500 - $1000</option>
                                    <option>$1000+</option>
                                </select>
                                <ChevronDown className="absolute right-5 top-1/2 -translate-y-1/2 text-gray-400" size={16} />
                            </div>
                        </div>

                        {/* Location */}
                        <div className="space-y-3">
                            <label className="text-[11px] font-black text-gray-900 uppercase tracking-widest block">Địa điểm</label>
                            <div className="relative">
                                <select className="w-full bg-[#F8FBFB] py-4 px-6 rounded-xl font-bold text-gray-400 appearance-none focus:outline-none text-sm cursor-pointer">
                                    <option>Tất cả thành phố</option>
                                    <option>Hà Nội</option>
                                    <option>Đà Nẵng</option>
                                    <option>TP. HCM</option>
                                </select>
                                <MapPin className="absolute right-5 top-1/2 -translate-y-1/2 text-gray-400 opacity-50" size={16} />
                            </div>
                        </div>

                        {/* Departure Date */}
                        <div className="space-y-3">
                            <label className="text-[11px] font-black text-gray-900 uppercase tracking-widest block">Ngày khởi hành</label>
                            <div className="relative">
                                <input 
                                    type="text" 
                                    placeholder="mm/dd/yyyy" 
                                    className="w-full bg-[#F8FBFB] py-4 px-6 rounded-xl font-bold text-gray-800 focus:outline-none placeholder:text-gray-300 text-sm"
                                />
                                <Calendar className="absolute right-5 top-1/2 -translate-y-1/2 text-gray-400 opacity-50" size={16} />
                            </div>
                        </div>

                        {/* Search Button */}
                        <button className="w-full bg-[#1A3D44] hover:bg-gray-900 text-white h-[56px] rounded-xl font-black text-sm uppercase tracking-widest flex items-center justify-center gap-3 transition-all group">
                            Tìm kiếm <Search size={18} className="group-hover:scale-110 transition-transform" />
                        </button>
                    </div>
                </motion.div>
            </div>
        </section>
    );
};

export default HeroTwo;
