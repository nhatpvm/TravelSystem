import React, { useState, useEffect } from 'react';
import { ArrowLeft, ArrowRight } from 'lucide-react';
import { motion } from 'framer-motion';
import { Link } from 'react-router-dom';

const offers = [
  {
    id: 1,
    title: "Mishor",
    price: "$160",
    img: "https://images.unsplash.com/photo-1506461883276-594a12b11cf3?q=80&w=2070&auto=format&fit=crop",
    featured: false
  },
  {
    id: 2,
    title: "Thành phố Trung Quốc",
    price: "$160",
    img: "https://images.unsplash.com/photo-1547984609-bc015f606da0?q=80&w=2070&auto=format&fit=crop",
    featured: true
  },
  {
    id: 3,
    title: "Thành phố New York",
    price: "$160",
    img: "https://images.unsplash.com/photo-1496442226666-8d4d0e62e6e9?q=80&w=2070&auto=format&fit=crop",
    featured: false
  },
  {
    id: 4,
    title: "Thành phố Nepal",
    price: "$160",
    img: "https://images.unsplash.com/photo-1544735716-392fe2489ffa?q=80&w=2070&auto=format&fit=crop",
    featured: true
  },
  {
    id: 5,
    title: "Tokyo, Nhật Bản",
    price: "$180",
    img: "https://images.unsplash.com/photo-1540959733332-eab4deabeeaf?q=80&w=2070&auto=format&fit=crop",
    featured: false
  },
  {
    id: 6,
    title: "Paris, Pháp",
    price: "$200",
    img: "https://images.unsplash.com/photo-1502602898657-3e91760cbb34?q=80&w=2070&auto=format&fit=crop",
    featured: true
  }
];

const LastMinuteOffers = () => {
  const loopOffers = [...offers, ...offers, ...offers];
  const [timeLeft, setTimeLeft] = useState({
    days: 0,
    hours: 0,
    minutes: 0,
    seconds: 0
  });

  // Mock countdown timer
  useEffect(() => {
    const timer = setInterval(() => {
      setTimeLeft(prev => ({
        ...prev,
        seconds: (prev.seconds + 1) % 60
      }));
    }, 1000);
    return () => clearInterval(timer);
  }, []);

  return (
    <section className="flex flex-col lg:flex-row min-h-[600px] overflow-hidden">
      
      {/* LEFT PORTION: 50% OFF & COUNTDOWN - Larger Version */}
      <div className="w-full lg:w-[45%] bg-[#061e21] py-32 px-12 flex flex-col items-center justify-center text-white relative overflow-hidden group border-r-4 border-[#1EB4D4]/30">
        
        {/* Animated Background Blobs */}
        <motion.div 
          animate={{ 
            scale: [1, 1.3, 1],
            rotate: [0, 90, 0],
            opacity: [0.3, 0.6, 0.3]
          }}
          transition={{ duration: 15, repeat: Infinity, ease: "linear" }}
          className="absolute -top-20 -left-20 w-96 h-96 bg-[#1EB4D4] rounded-full blur-[120px] pointer-events-none"
        />
        <motion.div 
          animate={{ 
            scale: [1.3, 1, 1.3],
            rotate: [90, 0, 90],
            opacity: [0.2, 0.5, 0.2]
          }}
          transition={{ duration: 20, repeat: Infinity, ease: "linear" }}
          className="absolute -bottom-20 -right-20 w-96 h-96 bg-purple-600 rounded-full blur-[120px] pointer-events-none"
        />

        <motion.div 
          initial={{ opacity: 0, scale: 0.8 }}
          whileInView={{ opacity: 1, scale: 1 }}
          transition={{ duration: 0.8, type: "spring" }}
          className="text-center relative z-10 w-full max-w-lg"
        >
          {/* Big Neon 50% Text */}
          <div className="relative inline-block mb-12">
            <motion.h2 
              animate={{ 
                textShadow: [
                  "0 0 20px rgba(30,180,212,0.4)",
                  "0 0 50px rgba(30,180,212,0.7)",
                  "0 0 20px rgba(30,180,212,0.4)"
                ]
              }}
              transition={{ duration: 3, repeat: Infinity }}
              className="text-[160px] md:text-[200px] lg:text-[240px] font-black leading-none tracking-tighter bg-clip-text text-transparent bg-gradient-to-b from-white via-white to-gray-500 drop-shadow-[0_10px_20px_rgba(0,0,0,0.6)]"
            >
              50%
            </motion.h2>
            <motion.p 
              className="text-5xl md:text-6xl font-black tracking-[0.3em] -mt-8 md:-mt-12 text-[#1EB4D4] uppercase italic drop-shadow-[0_4px_10px_rgba(0,0,0,0.3)]"
            >
              GIẢM
            </motion.p>
          </div>
          
          {/* Glassmorphism Countdown - Larger cards */}
          <div className="grid grid-cols-4 gap-6 mt-12">
            {[
              { label: 'Ngày', val: String(timeLeft.days).padStart(2, '0') },
              { label: 'Giờ', val: String(timeLeft.hours).padStart(2, '0') },
              { label: 'Phút', val: String(timeLeft.minutes).padStart(2, '0') },
              { label: 'Giây', val: String(timeLeft.seconds).padStart(2, '0') }
            ].map((t, idx) => (
              <motion.div 
                key={idx}
                whileHover={{ y: -8, backgroundColor: "rgba(255,255,255,0.2)" }}
                className="flex flex-col items-center justify-center bg-white/10 backdrop-blur-xl border border-white/20 rounded-[2rem] py-8 px-4 transition-all shadow-2xl"
              >
                <span className="text-4xl lg:text-5xl font-black mb-2 bg-clip-text text-transparent bg-gradient-to-t from-gray-300 to-white">
                  {t.val}
                </span>
                <span className="text-[11px] uppercase font-bold text-[#1EB4D4] tracking-widest">{t.label}</span>
              </motion.div>
            ))}
          </div>
        </motion.div>
      </div>

      {/* RIGHT PORTION: CAROUSEL (Auto-Drift Slow Version) */}
      <div className="w-full lg:w-[55%] bg-[#1EB4D4] py-24 px-8 md:px-16 flex flex-col relative overflow-hidden">
        {/* Background decorative plane */}
        <div className="absolute top-10 right-10 opacity-10 rotate-[20deg]">
          <svg width="150" height="150" viewBox="0 0 24 24" fill="none" stroke="white" strokeWidth="1">
            <path d="M22 2L11 13M22 2l-7 20-4-9-9-4 20-7z" strokeLinecap="round" strokeLinejoin="round"/>
          </svg>
        </div>

        <div className="flex justify-between items-center mb-12 relative z-10">
          <div>
            <p className="text-white text-2xl font-medium mb-2 italic" style={{ fontFamily: "'Kalam', cursive" }}>
              Ưu đãi & Khuyến mãi
            </p>
            <h3 className="text-4xl md:text-5xl lg:text-5xl font-black text-white leading-tight">
              Những ưu đãi phút chót <br /> khó tin
            </h3>
          </div>
        </div>

        {/* Seamless Drifting Carousel Cards */}
        <div className="relative">
          <motion.div 
            className="flex gap-8 w-max"
            animate={{ 
              x: [0, "-33.33%"] // Moving through one full set of cards
            }}
            transition={{ 
              duration: 45, 
              repeat: Infinity, 
              ease: "linear" 
            }}
          >
            {loopOffers.map((offer, idx) => (
              <Link 
                to="/destinations/details"
                key={idx}
                className="relative w-[300px] md:w-[350px] aspect-[3/4.2] rounded-[2.5rem] overflow-hidden group border-2 border-white/20 hover:border-white shadow-2xl transition-all duration-500 flex-shrink-0 block"
              >
                <img src={offer.img} alt={offer.title} className="absolute inset-0 w-full h-full object-cover transition-transform duration-700 group-hover:scale-110" />
                <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-transparent to-transparent opacity-60 group-hover:opacity-100 transition-opacity"></div>
                
                {/* Badges */}
                <div className="absolute top-6 left-6 flex flex-col gap-3">
                  <span className="bg-[#1EB4D4] text-white text-[12px] font-black px-4 py-2 rounded-xl shadow-xl">
                    Giảm giá 50%
                  </span>
                  {offer.featured && (
                    <span className="bg-gray-900 text-white text-[10px] font-black px-4 py-2 rounded-xl shadow-xl uppercase tracking-wider">
                      Nổi bật
                    </span>
                  )}
                </div>
 
                {/* Card Text */}
                <div className="absolute bottom-8 left-8 text-white">
                  <h4 className="text-3xl font-black mb-1 drop-shadow-lg">{offer.title}</h4>
                  <div className="flex items-baseline gap-2">
                    <span className="text-2xl font-black text-[#1EB4D4]">${offer.price.replace('$', '')}</span>
                    <span className="text-xs opacity-60 line-through font-bold">${parseInt(offer.price.replace('$', '')) * 2}</span>
                  </div>
                </div>
              </Link>
            ))}
          </motion.div>
        </div>
      </div>

    </section>
  );
};

export default LastMinuteOffers;
