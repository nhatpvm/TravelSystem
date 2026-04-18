import React, { useEffect, useState, useRef } from 'react';
import { MapPin, Calendar, Search, Clock, ChevronDown, Luggage } from 'lucide-react';
import { motion, useInView, animate } from 'framer-motion';

const Counter = ({ value, label }) => {
  const ref = useRef(null);
  const isInView = useInView(ref, { once: true, amount: 0.5 });
  const [displayValue, setDisplayValue] = useState("0");

  useEffect(() => {
    if (isInView) {
      const numericValue = parseFloat(value);
      const isDecimal = value.includes('.');
      const suffix = value.replace(/[0-9.]/g, '');

      const controls = animate(0, numericValue, {
        duration: 2,
        onUpdate: (latest) => {
          let formatted = isDecimal ? latest.toFixed(1) : Math.floor(latest).toString();
          setDisplayValue(formatted + suffix);
        },
      });
      return () => controls.stop();
    }
  }, [isInView, value]);

  return (
    <div ref={ref} className="flex flex-col">
      <span className="text-5xl md:text-6xl font-black text-white mb-2">{displayValue}</span>
      <span className="text-xl text-gray-200 font-medium tracking-wide">{label}</span>
    </div>
  );
};

const Hero = () => {
  return (
    <div className="relative h-[140vh] lg:h-screen w-full overflow-hidden font-sans">
      {/* Background Image - James Bond Island style */}
      <img 
        src="https://images.unsplash.com/photo-1528127269322-539801943592?ixlib=rb-4.0.3&auto=format&fit=crop&w=2000&q=80" 
        alt="Travel Destination" 
        className="absolute inset-0 w-full h-full object-cover"
      />
      
      {/* Dark Teal/Blue Overlay */}
      <div className="absolute inset-0 bg-slate-900/40 z-10"></div>

      {/* Hero Content */}
      <div className="relative z-20 h-full container mx-auto px-8 md:px-24 flex flex-col justify-center items-start text-white">
        <p className="text-[18px] italic mb-[10px] animate-fade-in-down opacity-90 tracking-wide text-white" style={{ fontFamily: "'Kalam', sans-serif" }}>
          Tận hưởng niềm vui khó quên cùng chúng tôi
        </p>
        
       <h1 className="font-poppins text-[32px] md:text-[60px] font-semibold leading-[1.1] max-w-4xl text-white animate-fade-in drop-shadow-lg pb-18">
          Hãy Cùng Chúng Tôi Tạo Nên <br />
          Chuyến Đi Tuyệt Vời Nhất
        </h1>

        {/* Improved Search Bar - Matching Image Precisely */}
        <div className="w-full max-w-6xl bg-white/10 backdrop-blur-md p-8 rounded-2xl border border-white/20 shadow-2xl flex flex-col md:flex-row items-center gap-2 animate-fade-in-up delay-300">
          
          {/* Location */}
          <div className="flex-1 flex items-center gap-4 px-4">
             <MapPin size={22} className="text-gray-300" />
            <div className="flex flex-col">
              <span className="text-sm font-bold text-white leading-tight">Địa điểm</span>
              <div className="flex items-center text-gray-300 cursor-pointer mt-1">
                <span className="text-xs font-medium italic">Úc</span>
                <ChevronDown size={12} className="ml-2" />
              </div>
            </div>
          </div>

          <div className="w-[1px] h-10 bg-white/20 hidden md:block"></div>

          {/* Activities Type */}
          <div className="flex-1 flex items-center gap-4 px-4">
             <Luggage size={22} className="text-gray-300" />
            <div className="flex flex-col">
              <span className="text-sm font-bold text-white leading-tight">Loại hình hoạt động</span>
              <div className="flex items-center text-gray-300 cursor-pointer mt-1">
                <span className="text-xs font-medium italic">Loại hoạt động</span>
                <ChevronDown size={12} className="ml-2" />
              </div>
            </div>
          </div>

          <div className="w-[1px] h-10 bg-white/20 hidden md:block"></div>

          {/* Activate Day */}
          <div className="flex-1 flex items-center gap-4 px-4">
             <Clock size={22} className="text-gray-300" />
            <div className="flex flex-col">
              <span className="text-sm font-bold text-white leading-tight">Ngày khởi hành</span>
              <div className="flex items-center text-gray-300 cursor-pointer mt-1">
                <span className="text-xs font-medium italic mr-4">tháng/ngày/năm</span>
                <Calendar size={12} className="opacity-80" />
              </div>
            </div>
          </div>

          <div className="w-[1px] h-10 bg-white/20 hidden md:block"></div>

          {/* Traveler */}
          <div className="flex-1 flex items-center gap-4 px-4">
             <Clock size={22} className="text-gray-300" />
            <div className="flex flex-col">
              <span className="text-sm font-bold text-white leading-tight">Số lượng khách</span>
              <div className="flex items-center text-gray-300 cursor-pointer mt-1">
                <span className="text-xs font-medium italic">01</span>
                <ChevronDown size={12} className="ml-2" />
              </div>
            </div>
          </div>

          <button className="bg-[#1EB4D4] hover:bg-[#19a7c5] text-white px-8 py-3.5 rounded-full flex items-center justify-center font-black text-lg transition-all shadow-lg ml-4">
            Tìm kiếm
          </button>
        </div>

        {/* Statistics Section - Counter Animation */}
        <div className="flex flex-col md:flex-row gap-12 md:gap-24 mt-24">
          <Counter value="20.5k" label="Tour nổi bật" />
          <Counter value="100.5k" label="Khách sạn sang trọng" />
          <Counter value="150.5k" label="Khách hàng hài lòng" />
        </div>
      </div>

      {/* Floating Dot element similar to image */}
      <div className="absolute right-[20%] top-[45%] w-3 h-3 bg-[#1EB4D4] rounded-full blur-[2px] opacity-60 z-20"></div>
    </div>
  );
};

export default Hero;
