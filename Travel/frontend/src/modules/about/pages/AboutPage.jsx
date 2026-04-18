import React from 'react';
import Navbar from '../../home/components/Navbar';
import Footer from '../../home/components/Footer';
import BookingPlatform from '../../home/components/BookingPlatform';
import AboutStats from '../components/AboutStats';
import Testimonials from '../components/Testimonials';
import InstagramFeed from '../components/InstagramFeed';
import { ChevronRight, ArrowRight, Plane } from 'lucide-react';
import { motion } from 'framer-motion';

const AboutPage = () => {
  return (
    <div className="min-h-screen bg-white flex flex-col font-sans">
      <Navbar />

      {/* Breadcrumb Header Section */}
      <section className="relative h-[450px] md:h-[550px] flex items-center justify-center overflow-hidden">
        {/* Background with high-quality monuments/travel scene */}
        <div className="absolute inset-0 z-0">
          <img 
            src="https://images.unsplash.com/photo-1467269204594-9661b134dd2b?q=80&w=2070" 
            alt="World Destinations" 
            className="w-full h-full object-cover"
          />
          {/* Subtle dark overlay for readability */}
          <div className="absolute inset-0 bg-black/30"></div>
        </div>

        <div className="container mx-auto px-4 relative z-10 text-center text-white">
          <motion.h1 
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            className="text-5xl md:text-7xl lg:text-8xl font-black mb-8 tracking-tighter"
          >
            Về chúng tôi
          </motion.h1>

          <motion.div 
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.2 }}
            className="inline-flex items-center gap-4 bg-white/20 backdrop-blur-md px-10 py-4 rounded-full border border-white/30 text-lg font-bold"
          >
            <span className="text-white hover:text-[#1EB4D4] transition-colors cursor-pointer">Trang chủ</span>
            <ChevronRight size={20} className="text-[#1EB4D4]" />
            <span className="text-white">Về chúng tôi</span>
          </motion.div>
        </div>
      </section>

      {/* Strived For Best Section */}
      <section className="py-32 bg-white overflow-hidden">
         <div className="container mx-auto px-4 md:px-12 lg:px-24">
            <div className="flex flex-col lg:flex-row gap-20 items-center">
               
               {/* Left Side: Image Collage */}
               <div className="w-full lg:w-1/2 relative">
                  <div className="relative">
                     {/* Main Large Image (Jumping people) */}
                     <motion.div 
                       initial={{ opacity: 0, scale: 0.9 }}
                       whileInView={{ opacity: 1, scale: 1 }}
                       transition={{ duration: 0.8 }}
                       className="w-[85%] rounded-[2rem] overflow-hidden"
                     >
                        <img 
                          src="https://ex-coders.com/html/turmet/assets/img/about/04.png" 
                          alt="People jumping" 
                          className="w-full object-cover"
                        />
                     </motion.div>

                     {/* Overlapping Image (Woman on boat) */}
                     <motion.div 
                       initial={{ opacity: 0, x: 50, y: 50 }}
                       whileInView={{ opacity: 1, x: 0, y: 0 }}
                       transition={{ duration: 0.8, delay: 0.2 }}
                       className="absolute -bottom-10 -right-4 w-[55%] rounded-[2rem] overflow-hidden border-[10px] border-white shadow-2xl"
                     >
                        <img 
                          src="https://ex-coders.com/html/turmet/assets/img/about/05.jpg" 
                          alt="Woman on boat" 
                          className="w-full object-cover"
                        />
                     </motion.div>

                     {/* Small Thumbnail (City view) */}
                     <motion.div 
                       initial={{ opacity: 0, x: -30 }}
                       whileInView={{ opacity: 1, x: 0 }}
                       className="absolute bottom-20 -left-12 w-[35%] rounded-[1.5rem] overflow-hidden border-8 border-white shadow-xl hidden md:block"
                     >
                        <img 
                          src="https://ex-coders.com/html/turmet/assets/img/about/03.jpg" 
                          alt="City Thumbnail" 
                          className="w-full object-cover"
                        />
                     </motion.div>

                     {/* 1992 Badge */}
                     <motion.div 
                       initial={{ scale: 0 }}
                       whileInView={{ scale: 1 }}
                       className="absolute top-1/2 left-[45%] -translate-y-1/2 z-20"
                     >
                        <img 
                          src="https://ex-coders.com/html/turmet/assets/img/about/circle.png" 
                          alt="Since 1992" 
                          className="w-32 md:w-40 animate-spin-slow"
                          style={{ animationDuration: '15s' }}
                        />
                     </motion.div>

                     {/* Avatars Row */}
                     <div className="absolute -bottom-16 left-10 flex items-center">
                        <img 
                          src="https://ex-coders.com/html/turmet/assets/img/about/group.png" 
                          alt="Clients" 
                          className="h-16 object-contain"
                        />
                        <div className="w-10 h-10 bg-[#1EB4D4] rounded-full flex items-center justify-center text-white text-xl font-bold border-4 border-white -ml-2">
                          +
                        </div>
                     </div>

                     {/* Dashed Plane Path Decoration */}
                     <div className="absolute -top-10 right-0 opacity-20 hidden md:block">
                        <svg width="200" height="200" viewBox="0 0 200 200" fill="none" className="rotate-12">
                           <path d="M10 180C10 180 30 100 100 80C170 60 190 10" stroke="#1EB4D4" strokeWidth="2" strokeLinecap="round" strokeDasharray="8 8" />
                           <motion.path 
                              animate={{ offset: [0, 100] }}
                              transition={{ duration: 5, repeat: Infinity, ease: "linear" }}
                              d="M10 180C10 180 30 100 100 80C170 60 190 10" stroke="transparent" strokeWidth="2" 
                           />
                        </svg>
                        <div className="absolute top-0 right-0 text-[#1EB4D4]">
                           <Plane size={32} className="rotate-[-45deg]" />
                        </div>
                     </div>
                  </div>
               </div>

               {/* Right Side: Text & Features */}
               <div className="w-full lg:w-1/2">
                  <p 
                    className="text-[#1EB4D4] text-xl font-medium mb-4 italic"
                    style={{ fontFamily: "'Kalam', cursive" }}
                  >
                    Tìm hiểu về chúng tôi
                  </p>
                  <h2 className="text-4xl md:text-6xl font-black text-gray-900 leading-tight mb-8">
                    Chúng tôi luôn nỗ lực vì <br /> những điều tốt đẹp nhất thế giới
                  </h2>
                  <p className="text-gray-500 font-medium leading-relaxed mb-12 text-lg">
                    Có rất nhiều biến thể của các đoạn văn bản sẵn có, nhưng phần lớn đã bị thay đổi dưới một số hình thức, bởi những từ ngữ hài hước được đưa vào mà không có vẻ gì là đáng tin cậy.
                  </p>
                  
                  <div className="space-y-10 mb-14">
                     {/* Feature Item 1 */}
                     <div className="flex items-center gap-6">
                        <div className="flex items-center gap-4 min-w-[200px]">
                           <div className="text-[#1EB4D4]">
                              <svg width="30" height="30" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="4" strokeLinecap="round" strokeLinejoin="round"><polyline points="20 6 9 17 4 12"/></svg>
                           </div>
                           <h4 className="text-xl font-black text-gray-900 leading-tight">Hệ thống đặt <br /> chỗ dễ dàng</h4>
                        </div>
                        <div className="h-10 w-[1px] bg-gray-200 hidden md:block"></div>
                        <p className="text-gray-500 font-medium md:pl-6 max-w-xs">Khách sạn của chúng tôi cũng tự hào cung cấp các dịch vụ đặc biệt.</p>
                     </div>

                     {/* Feature Item 2 */}
                     <div className="flex items-center gap-6">
                        <div className="flex items-center gap-4 min-w-[200px]">
                           <div className="text-[#1EB4D4]">
                              <svg width="30" height="30" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="4" strokeLinecap="round" strokeLinejoin="round"><polyline points="20 6 9 17 4 12"/></svg>
                           </div>
                           <h4 className="text-xl font-black text-gray-900 leading-tight">Dịch vụ <br /> chăm sóc tận tâm</h4>
                        </div>
                        <div className="h-10 w-[1px] bg-gray-200 hidden md:block"></div>
                        <p className="text-gray-500 font-medium md:pl-6 max-w-xs">Khách sạn của chúng tôi cũng tự hào cung cấp các dịch vụ đặc biệt.</p>
                     </div>
                  </div>

                  <button className="bg-gray-900 hover:bg-[#1EB4D4] text-white px-10 py-5 rounded-full font-black flex items-center gap-3 transition-all shadow-2xl group">
                    Khám phá thêm 
                    <ArrowRight size={22} className="group-hover:translate-x-2 transition-transform" />
                  </button>
               </div>

            </div>
         </div>
      </section>

      <BookingPlatform />

      <AboutStats />

      <Testimonials />

      <InstagramFeed />

      <Footer />
    </div>
  );
};

export default AboutPage;
