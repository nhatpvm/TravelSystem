import React from 'react';
import { ArrowRight, CheckCircle2, Plane } from 'lucide-react';
import { motion } from 'framer-motion';

const BookingPlatform = () => {
  return (
    <section className="py-24 bg-gradient-to-br from-white via-[#F0FCFF] to-[#FFF0F0] relative overflow-hidden">
      
      {/* Floating Plane 1 (Top Left) */}
      <motion.div 
        animate={{ 
          x: [-100, 100], 
          y: [0, -20, 0],
          opacity: [0, 0.2, 0]
        }}
        transition={{ duration: 15, repeat: Infinity, ease: "linear" }}
        className="absolute top-20 left-10 pointer-events-none opacity-10 hidden lg:block"
      >
        <div className="relative">
           <img src="https://ex-coders.com/html/turmet/assets/img/plane-shape1.png" alt=""/>
           {/* Dashed line effect */}
           <div className="absolute top-1/2 right-full w-40 h-[1px] border-b border-dashed border-gray-300 -translate-y-1/2 rotate-[10deg]"></div>
        </div>
      </motion.div>

      {/* Floating Plane 2 (Bottom Right) */}
      <motion.div 
        animate={{ 
          x: [100, -100], 
          y: [0, 30, 0],
          opacity: [0, 0.2, 0]
        }}
        transition={{ duration: 20, repeat: Infinity, ease: "linear" }}
        className="absolute bottom-40 right-10 pointer-events-none opacity-10 hidden lg:block"
      >
        <div className="relative">
           <img src="https://ex-coders.com/html/turmet/assets/img/plane-shape2.png" alt="" />
           <div className="absolute top-1/2 left-full w-40 h-[1px] border-b border-dashed border-gray-300 -translate-y-1/2 rotate-[-15deg]"></div>
        </div>
      </motion.div>

      <div className="container mx-auto px-4 md:px-12 lg:px-24">
        <div className="flex flex-col lg:flex-row items-center gap-16">
          
          {/* Left Content Column */}
          <div className="w-full lg:w-1/2 relative z-10">
            <motion.p 
              initial={{ opacity: 0, x: -20 }}
              whileInView={{ opacity: 1, x: 0 }}
              className="text-[#1EB4D4] text-xl font-medium mb-4 italic"
              style={{ fontFamily: "'Kalam', cursive" }}
            >
              Bạn đã sẵn sàng để du lịch?
            </motion.p>
            
            <motion.h2 
              initial={{ opacity: 0, x: -20 }}
              whileInView={{ opacity: 1, x: 0 }}
              transition={{ delay: 0.1 }}
              className="text-4xl md:text-5xl lg:text-6xl font-black text-gray-900 leading-[1.1] mb-8"
            >
              Nền tảng đặt Tour trực tuyến <br className="hidden md:block" />
              hàng đầu thế giới
            </motion.h2>

            <motion.p 
              initial={{ opacity: 0, x: -20 }}
              whileInView={{ opacity: 1, x: 0 }}
              transition={{ delay: 0.2 }}
              className="text-gray-500 font-medium mb-12 max-w-xl leading-relaxed"
            >
              Có nhiều biến thể của các đoạn văn bản Lorem Ipsum có sẵn, nhưng phần lớn đã bị thay đổi dưới một số hình thức, bằng cách thêm vào sự hài hước, hoặc các từ ngẫu nhiên không hề đáng tin cậy.
            </motion.p>

            {/* Features Row */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-10 mb-12 relative">
              {/* Feature 1 */}
              <div className="flex items-center gap-5">
                <div className="w-16 h-16 rounded-full bg-[#1EB4D4] flex items-center justify-center text-white shrink-0 shadow-lg shadow-[#1EB4D4]/30">
                   <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M19 17h2c.6 0 1-.4 1-1v-3c0-.9-.7-1.7-1.5-1.9C18.7 10.6 16 10 16 10s-1.3-1.4-2.2-2.3c-.5-.4-1.1-.7-1.8-.7H5c-1.1 0-2 .9-2 2v7c0 1.1.9 2 2 2h10"/><circle cx="7" cy="17" r="2"/><circle cx="15" cy="17" r="2"/></svg>
                </div>
                <div>
                  <h4 className="font-black text-gray-900 leading-tight">Tour phiêu lưu <br /> kỳ thú nhất</h4>
                </div>
              </div>

              {/* Feature 2 */}
              <div className="flex items-center gap-5">
                <div className="w-16 h-16 rounded-full bg-[#1EB4D4] flex items-center justify-center text-white shrink-0 shadow-lg shadow-[#1EB4D4]/30">
                  <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M22 17H2a2 2 0 0 1-2-2V9a2 2 0 0 1 2-2h19a2 2 0 0 1 2 2v7a2 2 0 0 1-2 2zm-11 5V17m5 5V17M6 22V17"/></svg>
                </div>
                <div>
                  <h4 className="font-black text-gray-900 leading-tight">Tour thực sự <br /> bắt đầu từ đây</h4>
                </div>
              </div>

              {/* Vertical Dashed Line Divider (Hidden on mobile) */}
              <div className="absolute left-[50%] top-0 bottom-0 w-[1px] border-l border-dashed border-gray-200 hidden md:block">
                <div className="absolute top-1/4 -left-1.5 w-3 h-3 rounded-full bg-[#1EB4D4]/20 border border-[#1EB4D4]/50 flex items-center justify-center">
                  <div className="w-1.5 h-1.5 rounded-full bg-[#1EB4D4]"></div>
                </div>
                <div className="absolute top-3/4 -left-1.5 w-3 h-3 rounded-full bg-[#1EB4D4]/20 border border-[#1EB4D4]/50 flex items-center justify-center">
                  <div className="w-1.5 h-1.5 rounded-full bg-[#1EB4D4]"></div>
                </div>
              </div>
            </div>

            <motion.button 
              whileHover={{ scale: 1.05 }}
              whileTap={{ scale: 0.95 }}
              className="bg-[#1EB4D4] hover:bg-[#19a7c5] text-white px-10 py-4 rounded-full font-black flex items-center gap-2 transition-all shadow-xl shadow-[#1EB4D4]/20"
            >
              Liên hệ với chúng tôi <ArrowRight size={20} />
            </motion.button>
          </div>

          {/* Right Image/Illustration Column */}
          <div className="w-full lg:w-1/2 relative h-[500px] md:h-[600px] flex items-center justify-center">
            {/* White brush strokes / background blob */}
            <div className="absolute inset-0 bg-white/40 blur-[50px] rounded-full scale-75"></div>
            
            <motion.div 
              initial={{ opacity: 0, scale: 0.9, y: 30 }}
              whileInView={{ opacity: 1, scale: 1, y: 0 }}
              transition={{ duration: 0.8 }}
              className="relative z-10 w-full h-full"
            >
              <img 
                src="https://ex-coders.com/html/turmet/assets/img/man-image.png" 
                alt="Traveller with luggage" 
                className="w-full h-full object-contain pointer-events-none drop-shadow-2xl"
              />
              
              {/* Floating tags or secondary shapes if needed */}
              <motion.div 
                animate={{ y: [0, -10, 0] }}
                transition={{ duration: 3, repeat: Infinity, ease: "easeInOut" }}
                className="absolute top-1/4 right-0 md:-right-10 bg-white p-4 rounded-2xl shadow-2xl hidden md:block"
              >
                 <CheckCircle2 className="text-[#1EB4D4] mb-2" size={30} />
                 <p className="text-xs font-black text-gray-400 uppercase tracking-widest">Nền tảng đã xác thực</p>
              </motion.div>
            </motion.div>
          </div>

        </div>
      </div>
    </section>
  );
};

export default BookingPlatform;
