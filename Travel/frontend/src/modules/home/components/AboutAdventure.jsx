import React from 'react';
import { Play, ShieldCheck, Map, Crown, Plane } from 'lucide-react';
import { motion } from 'framer-motion';
import nav1 from '../../../assets/nav1.png';
import nav2 from '../../../assets/nav2.png';
import nav3 from '../../../assets/nav3.png';

const features = [
  {
    icon: <Crown size={30} />,
    title: "Chuyến đi độc quyền",
    desc: "Chúng tôi cung cấp các hành trình được thiết kế riêng, mang lại trải nghiệm độc đáo cho bạn."
  },
  {
    icon: <ShieldCheck size={30} />,
    title: "An toàn luôn hàng đầu",
    desc: "Sự an toàn của bạn là ưu tiên số một. Chúng tôi tuân thủ các tiêu chuẩn an toàn nghiêm ngặt nhất."
  },
  {
    icon: <Map size={30} />,
    title: "Hướng dẫn chuyên nghiệp",
    desc: "Đội ngũ hướng dẫn viên giàu kinh nghiệm sẽ đồng hành cùng bạn trên mọi nẻo đường."
  }
];

const AboutAdventure = () => {
  return (
    <section className="py-24 bg-gray-50/30 overflow-hidden relative">
      {/* Background patterns could be added here */}
      <div className="absolute top-0 right-0 opacity-10 pointer-events-none">
         <img src={nav3} alt="" className="w-full max-w-lg" />
      </div>

      <div className="container mx-auto px-4 md:px-12 lg:px-24">
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-16 items-center">
          
          {/* LEFT SIDE: OVERLAPPING IMAGES */}
          <div className="relative h-[600px] flex items-center">
            {/* Top Back Image */}
            <motion.div 
              initial={{ opacity: 0, x: -50 }}
              whileInView={{ opacity: 1, x: 0 }}
              className="relative z-10 w-[65%] rounded-[2rem] overflow-hidden shadow-2xl border-8 border-white"
            >
              <img 
                src={nav1}
                alt="Adventure" 
                className="w-full h-full object-cover aspect-[4/5]"
              />
              {/* Watch Video Button */}
              <div className="absolute bottom-10 left-10 bg-white p-6 rounded-2xl shadow-xl flex flex-col items-center">
                <div className="bg-[#1EB4D4] w-14 h-14 rounded-full flex items-center justify-center text-white mb-2 cursor-pointer hover:scale-110 transition-transform">
                  <Play fill="currentColor" size={24} />
                </div>
                <span className="text-xs font-black uppercase tracking-widest text-gray-900">Xem video</span>
              </div>
            </motion.div>

            {/* Front Image (Bottom Right) */}
            <motion.div 
              initial={{ opacity: 0, scale: 0.8, y: 50 }}
              whileInView={{ opacity: 1, scale: 1, y: 0 }}
              transition={{ delay: 0.2 }}
              className="absolute bottom-0 right-0 z-20 w-[55%] rounded-[2rem] overflow-hidden shadow-2xl border-8 border-white"
            >
              <img 
                src={nav2}
                alt="Luxury" 
                className="w-full h-full object-cover aspect-[4/5]"
              />
              {/* Luxury Tour Label */}
              <div className="absolute bottom-6 left-6 right-6 bg-[#1EB4D4] p-5 rounded-2xl text-white">
                <div className="flex items-center gap-4">
                  <div className="border border-white/30 p-2 rounded-lg">
                    <Plane size={24} />
                  </div>
                  <div>
                    <h4 className="font-black text-lg leading-none mb-1">Tour sang trọng</h4>
                    <p className="text-xs opacity-80">25 năm kinh nghiệm</p>
                  </div>
                </div>
              </div>
            </motion.div>

            {/* Floating Plane Decoration */}
            <div className="absolute top-10 right-20 z-30 opacity-80 hidden xl:block">
               <motion.img 
                animate={{ y: [0, -20, 0] }}
                transition={{ duration: 4, repeat: Infinity }}
                src={nav3}
                alt="plane" 
                className="w-40"
               />
            </div>
          </div>

          {/* RIGHT SIDE: CONTENT */}
          <div className="lg:pl-10">
            <motion.p 
              initial={{ opacity: 0, y: 20 }}
              whileInView={{ opacity: 1, y: 0 }}
              className="text-[#1EB4D4] text-xl font-medium mb-4 italic tracking-wider"
              style={{ fontFamily: "'Kalam', cursive" }}
            >
              Hãy cùng đi nào
            </motion.p>
            <motion.h2 
              initial={{ opacity: 0, y: 20 }}
              whileInView={{ opacity: 1, y: 0 }}
              transition={{ delay: 0.1 }}
              className="text-4xl md:text-5xl lg:text-6xl font-black text-gray-900 mb-12 leading-[1.1]"
            >
              Cơ hội tuyệt vời cho <br />
              phiêu lưu và du lịch
            </motion.h2>

            {/* Features List */}
            <div className="space-y-10 relative">
              {/* Vertical Dashed Line and Pulse Point */}
              <div className="absolute left-[34px] top-4 bottom-4 w-[2px] border-l-2 border-dashed border-gray-200 z-0">
                <motion.div 
                  animate={{ scale: [1, 1.5, 1], opacity: [0.5, 1, 0.5] }}
                  transition={{ duration: 2, repeat: Infinity }}
                  className="absolute top-1/2 left-[-6px] w-3 h-3 bg-[#1EB4D4] rounded-full"
                />
              </div>

              {features.map((item, idx) => (
                <motion.div 
                  key={idx}
                  initial={{ opacity: 0, x: 30 }}
                  whileInView={{ opacity: 1, x: 0 }}
                  transition={{ delay: 0.2 + idx * 0.1 }}
                  className="flex items-start gap-6 relative z-10"
                >
                  <div className="w-[70px] h-[70px] flex-shrink-0 bg-[#1EB4D4] rounded-full flex items-center justify-center text-white shadow-lg shadow-[#1EB4D4]/30">
                    {item.icon}
                  </div>
                  <div>
                    <h4 className="text-2xl font-black text-gray-900 mb-2">{item.title}</h4>
                    <p className="text-gray-500 font-medium leading-relaxed max-w-sm">
                      {item.desc}
                    </p>
                  </div>
                </motion.div>
              ))}
            </div>
          </div>
        </div>
      </div>
      
      {/* Woman Illustration Right Side - Floating effect */}
      <motion.div 
         animate={{ 
           x: [-30, 30, -30],
           y: [0, -15, 0]
         }}
         transition={{ 
           duration: 7, 
           repeat: Infinity, 
           ease: "easeInOut" 
         }}
         className="absolute bottom-0 right-0 opacity-100 hidden 2xl:block pointer-events-none z-50"
      >
         <img 
            src={nav2}
            alt="traveller" 
            className="w-full max-w-md translate-y-20 brightness-110"
         />
      </motion.div>

      {/* Decorative Right Shape */}
      <div className="absolute bottom-0 right-0 opacity-80 pointer-events-none z-0">
         <img 
            src={nav3}
            alt="" 
            className="w-full max-w-lg"
         />
      </div>
    </section>
  );
};

export default AboutAdventure;
