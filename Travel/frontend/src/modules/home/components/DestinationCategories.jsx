import React, { useState, useEffect } from 'react';
import { Plane } from 'lucide-react';
import { motion, useMotionValue, animate } from 'framer-motion';

const categories = [
  { img: "https://images.unsplash.com/photo-1516426122078-c23e76319801?q=80&w=2068&auto=format&fit=crop", title: "Thám hiểm động vật hoang dã" },
  { img: "https://images.unsplash.com/photo-1502602898657-3e91760cbb34?q=80&w=2070&auto=format&fit=crop", title: "Tour thành phố" },
  { img: "https://images.unsplash.com/photo-1504280390367-361c6d9f38f4?q=80&w=2070&auto=format&fit=crop", title: "Leo núi Hiking" },
  { img: "https://tibro.in/blog/wp-content/uploads/2025/01/The-Ritz-Carlton-Yacht-Collection-1024x683.jpg", title: "Du thuyền sang trọng" },
  { img: "https://images.unsplash.com/photo-1476514525535-07fb3b4ae5f1?q=80&w=2070&auto=format&fit=crop", title: "Thám hiểm sa mạc" },
  { img: "https://images.unsplash.com/photo-1511497584788-876760111969?q=80&w=2070&auto=format&fit=crop", title: "Khám phá rừng rậm" },
  { img: "https://images.unsplash.com/photo-1483347756197-71ef80e95f73?q=80&w=2070&auto=format&fit=crop", title: "Cực quang" },
];

const DestinationCategories = () => {
  const loopCategories = [...categories, ...categories, ...categories, ...categories];
  const x = useMotionValue(0);
  const [isInteracting, setIsInteracting] = useState(false);
  
  // Single card total width
  const cardWidth = 320 + 24; // width + gap

  useEffect(() => {
    let controls;
    if (!isInteracting) {
      // Loop drift
      const moveAmount = -cardWidth * categories.length;
      
      controls = animate(x, [x.get(), moveAmount], {
        duration: 40,
        ease: "linear",
        repeat: Infinity,
        repeatType: "loop",
        onRepeat: () => x.set(0)
      });
    }
    return () => controls?.stop();
  }, [isInteracting, cardWidth, x]);

  return (
    <section className="py-24 bg-white overflow-hidden relative">
      {/* Decorative Shape Image with Pulsing/Floating effect */}
      <div className="absolute top-10 left-[10%] opacity-40 hidden lg:block z-0">
        <motion.img 
          src="https://ex-coders.com/html/turmet/assets/img/destination/shape.png" 
          alt="decoration"
          animate={{ 
            y: [0, -15, 0],
            rotate: [0, 2, 0]
          }}
          transition={{ 
            duration: 5, 
            repeat: Infinity, 
            ease: "easeInOut" 
          }}
          className="w-full max-w-[400px]"
        />
      </div>

      {/* Decorative Dashed Line with Plane */}
      <div className="absolute left-[-100px] top-[150px] opacity-20 hidden xl:block">
        <svg width="400" height="150" viewBox="0 0 400 150" fill="none" xmlns="http://www.w3.org/2000/svg">
          <path d="M1 100C50 100 100 50 150 50C200 50 250 100 300 100C350 100 400 50 450 50" stroke="#1EB4D4" strokeWidth="2" strokeDasharray="8 8"/>
          <circle cx="1" cy="100" r="4" fill="#1EB4D4" />
        </svg>
        <div className="absolute right-[-20px] top-[40px] text-[#1EB4D4] rotate-[20deg]">
          <Plane size={32} fill="currentColor" />
        </div>
      </div>

      <div className="w-full text-center relative z-10">
        <div className="px-4 md:px-12">
          <motion.p 
            initial={{ opacity: 0, y: 20 }}
            whileInView={{ opacity: 1, y: 0 }}
            className="text-[#1EB4D4] text-xl font-medium mb-4 italic tracking-wider"
            style={{ fontFamily: "'Kalam', cursive" }}
          >
            Địa điểm tuyệt vời dành cho bạn
          </motion.p>
          <motion.h2 
            initial={{ opacity: 0, scale: 0.9 }}
            whileInView={{ opacity: 1, scale: 1 }}
            className="text-4xl md:text-5xl lg:text-5xl font-black text-gray-900 mb-16 tracking-tight"
          >
            Duyệt theo danh mục điểm đến
          </motion.h2>
        </div>

        {/* Carousel Root Container */}
        <div 
          className="relative overflow-hidden mb-16 cursor-grab active:cursor-grabbing select-none"
          onMouseDown={() => setIsInteracting(true)}
          onMouseUp={() => setIsInteracting(false)}
          onMouseLeave={() => setIsInteracting(false)}
          onTouchStart={() => setIsInteracting(true)}
          onTouchEnd={() => setIsInteracting(false)}
        >
          <motion.div 
            className="flex gap-6 w-max px-10"
            style={{ x }}
            drag="x"
            dragConstraints={{
               left: -(cardWidth * (loopCategories.length - 3)), // Approximate constraints
               right: 0 
            }}
          >
            {loopCategories.map((cat, idx) => (
              <div 
                key={idx}
                className="w-[280px] md:w-[320px] aspect-[3/4.5] rounded-[2.5rem] overflow-hidden relative group shadow-2xl transition-all duration-500 pointer-events-none"
              >
                <img src={cat.img} alt={cat.title} className="absolute inset-0 w-full h-full object-cover transition-transform duration-700 group-hover:scale-110" />
                <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-transparent to-transparent opacity-80 transition-opacity group-hover:opacity-100"></div>
                
                <div className="absolute bottom-6 left-0 right-0 text-center px-4">
                  <h3 className="text-white text-base lg:text-lg font-black uppercase tracking-widest drop-shadow-md">
                     {cat.title.split(' ')[0]} <br />
                     <span className="text-[11px] font-bold text-[#1EB4D4]">{cat.title.split(' ').slice(1).join(' ')}</span>
                  </h3>
                </div>
              </div>
            ))}
          </motion.div>
        </div>

        {/* 5 Dots - Pulse Animation */}
        <div className="flex justify-center items-center space-x-3">
          {[0, 1, 2, 3, 4].map((dot) => (
            <motion.div
              key={dot}
              animate={{ 
                scale: [1, 1.2, 1],
                backgroundColor: ["#E5E7EB", "#1EB4D4", "#E5E7EB"]
              }}
              transition={{ 
                duration: 2, 
                repeat: Infinity, 
                delay: dot * 0.4 
              }}
              className="h-3 w-3 rounded-full"
            />
          ))}
        </div>
      </div>
    </section>
  );
};

export default DestinationCategories;
