import React, { useState, useEffect } from 'react';
import { motion } from 'framer-motion';
import { MoveRight, Plane } from 'lucide-react';

const DiscountTourThree = () => {
  const [timeLeft, setTimeLeft] = useState({
    days: 0,
    hours: 0,
    minutes: 0,
    seconds: 0
  });

  useEffect(() => {
    const targetDate = new Date();
    targetDate.setDate(targetDate.getDate() + 30);

    const timer = setInterval(() => {
      const now = new Date().getTime();
      const distance = targetDate.getTime() - now;

      if (distance < 0) {
        clearInterval(timer);
        return;
      }

      setTimeLeft({
        days: Math.floor(distance / (1000 * 60 * 60 * 24)),
        hours: Math.floor((distance % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60)),
        minutes: Math.floor((distance % (1000 * 60 * 60)) / (1000 * 60)),
        seconds: Math.floor((distance % (1000 * 60)) / 1000)
      });
    }, 1000);

    return () => clearInterval(timer);
  }, []);

  const timeItems = [
    { label: 'NGÀY', value: timeLeft.days },
    { label: 'GIỜ', value: timeLeft.hours },
    { label: 'PHÚT', value: timeLeft.minutes },
    { label: 'GIÂY', value: timeLeft.seconds }
  ];

  return (
    <section className="relative py-28 md:py-36 overflow-hidden">
      {/* Background Image */}
      <div className="absolute inset-0 z-0">
        <img
          src="https://images.unsplash.com/photo-1507525428034-b723cf961d3e?q=80&w=2070&auto=format&fit=crop"
          alt="Background"
          className="w-full h-full object-cover"
        />
        {/* Gradient overlays for depth */}
        <div className="absolute inset-0 bg-gradient-to-r from-[#0B1D26]/95 via-[#0B1D26]/70 to-[#0B1D26]/30"></div>
        <div className="absolute inset-0 bg-gradient-to-t from-[#0B1D26]/40 to-transparent"></div>
      </div>

      {/* Floating Decorative Plane */}
      <motion.div
        animate={{ x: [0, 80, 0], y: [0, -20, 0] }}
        transition={{ repeat: Infinity, duration: 12, ease: 'easeInOut' }}
        className="absolute top-16 right-[15%] z-[5] opacity-10 hidden lg:block"
      >
        <Plane size={100} className="text-white rotate-12" />
      </motion.div>

      <div className="container mx-auto px-4 md:px-12 lg:px-24 relative z-10">
        <div className="flex flex-col lg:flex-row items-center gap-16 lg:gap-24">

          {/* Left Content */}
          <motion.div
            initial={{ opacity: 0, x: -40 }}
            whileInView={{ opacity: 1, x: 0 }}
            transition={{ duration: 0.7 }}
            viewport={{ once: true }}
            className="w-full lg:w-[55%]"
          >
            <p
              className="text-[#1EB4D4] text-lg font-medium mb-3 italic"
              style={{ fontFamily: "'Kalam', cursive" }}
            >
              Chúng tôi đang cung cấp
            </p>
            <h2 className="text-4xl md:text-5xl lg:text-[4.2rem] font-black text-white leading-[1.15] mb-6">
              Giảm <span className="text-[#1EB4D4]">30%</span> Cho
              <br /> Mọi Chuyến Đi
            </h2>
            <p className="text-white/50 text-base md:text-lg font-medium max-w-md mb-10 leading-relaxed">
              Đừng bỏ lỡ cơ hội giảm giá đặc biệt cho mọi chuyến đi. Ưu đãi có hạn, đặt ngay hôm nay!
            </p>

            {/* Countdown */}
            <div className="flex flex-wrap gap-3 md:gap-5 mb-12">
              {timeItems.map((item, idx) => (
                <motion.div
                  key={item.label}
                  initial={{ opacity: 0, y: 20 }}
                  whileInView={{ opacity: 1, y: 0 }}
                  transition={{ delay: 0.15 + idx * 0.08 }}
                  viewport={{ once: true }}
                  className="group relative w-[72px] md:w-[88px] h-[88px] md:h-[100px]"
                >
                  {/* Glow ring on hover */}
                  <div className="absolute -inset-[2px] rounded-2xl bg-gradient-to-br from-[#1EB4D4]/40 to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-500 blur-[2px]"></div>
                  <div className="relative w-full h-full bg-white/[0.07] backdrop-blur-xl rounded-2xl border border-white/[0.12] flex flex-col items-center justify-center text-white overflow-hidden">
                    {/* Subtle inner shine */}
                    <div className="absolute top-0 left-0 right-0 h-[1px] bg-gradient-to-r from-transparent via-white/20 to-transparent"></div>
                    <span className="text-3xl md:text-[2.5rem] font-black leading-none mb-1 tabular-nums">
                      {item.value < 10 ? `0${item.value}` : item.value}
                    </span>
                    <span className="text-[9px] md:text-[10px] font-bold tracking-[0.2em] text-white/40 uppercase">
                      {item.label}
                    </span>
                  </div>
                </motion.div>
              ))}
            </div>

            {/* CTA Button */}
            <motion.button
              initial={{ opacity: 0, y: 20 }}
              whileInView={{ opacity: 1, y: 0 }}
              transition={{ delay: 0.5 }}
              viewport={{ once: true }}
              className="relative bg-[#1EB4D4] hover:bg-[#17c5e8] text-white px-10 py-[18px] rounded-full font-black text-[15px] flex items-center gap-3 transition-all duration-300 shadow-[0_8px_30px_rgba(30,180,212,0.35)] hover:shadow-[0_12px_40px_rgba(30,180,212,0.5)] group"
            >
              <span>Khám phá chuyến bay</span>
              <MoveRight size={20} className="group-hover:translate-x-2 transition-transform duration-300" />
            </motion.button>
          </motion.div>

          {/* Right: Circular Visual */}
          <motion.div
            initial={{ opacity: 0, x: 40 }}
            whileInView={{ opacity: 1, x: 0 }}
            transition={{ duration: 0.7, delay: 0.15 }}
            viewport={{ once: true }}
            className="w-full lg:w-[45%] flex justify-center lg:justify-end"
          >
            <div className="relative w-[340px] md:w-[420px] lg:w-[460px] aspect-square flex items-center justify-center">

              {/* Outermost subtle ring */}
              <div className="absolute inset-0 rounded-full border border-white/[0.08]"></div>


              {/* Inner glow ring */}
              <div className="absolute inset-10 rounded-full border border-white/[0.06] shadow-[inset_0_0_60px_rgba(30,180,212,0.08)]"></div>

              {/* Main Image Circle - Rotating */}
              <motion.div
                animate={{ rotate: 360 }}
                transition={{ repeat: Infinity, duration: 25, ease: 'linear' }}
                className="relative z-20 w-[65%] aspect-square"
              >
                <motion.div
                  initial={{ scale: 0.6, opacity: 0 }}
                  whileInView={{ scale: 1, opacity: 1 }}
                  transition={{ type: 'spring', damping: 18, delay: 0.3 }}
                  viewport={{ once: true }}
                  className="w-full h-full rounded-full overflow-hidden shadow-[0_20px_60px_rgba(0,0,0,0.5)]"
                >
                  {/* Glowing border */}
                  <div className="absolute -inset-[3px] rounded-full bg-gradient-to-br from-[#1EB4D4]/50 via-transparent to-[#1EB4D4]/20"></div>
                  <div className="absolute inset-[3px] rounded-full overflow-hidden">
                    <motion.img
                      animate={{ rotate: -360 }}
                      transition={{ repeat: Infinity, duration: 25, ease: 'linear' }}
                      src="https://images.unsplash.com/photo-1488646953014-85cb44e25828?q=80&w=800&auto=format&fit=crop"
                      alt="Traveler"
                      className="w-full h-full object-cover"
                    />
                  </div>
                </motion.div>
              </motion.div>

              {/* Accent dots */}
              <div className="absolute top-[18%] right-[8%] w-3.5 h-3.5 bg-[#1EB4D4] rounded-full shadow-[0_0_15px_rgba(30,180,212,0.7)] animate-pulse z-30"></div>
              <div className="absolute bottom-[22%] left-[6%] w-2 h-2 bg-white/30 rounded-full animate-pulse z-30" style={{ animationDelay: '1s' }}></div>

              {/* Floating badge */}
              <motion.div
                animate={{ y: [0, -8, 0] }}
                transition={{ repeat: Infinity, duration: 3, ease: 'easeInOut' }}
                className="absolute bottom-[12%] right-[5%] z-30 bg-white/[0.1] backdrop-blur-xl border border-white/[0.15] rounded-2xl px-5 py-3 flex items-center gap-3"
              >
                <div className="w-10 h-10 rounded-full bg-[#1EB4D4]/20 flex items-center justify-center">
                  <Plane size={18} className="text-[#1EB4D4]" />
                </div>
                <div>
                  <p className="text-white text-sm font-black leading-tight">200+</p>
                  <p className="text-white/40 text-[10px] font-bold">Gói Tour</p>
                </div>
              </motion.div>
            </div>
          </motion.div>

        </div>
      </div>
    </section>
  );
};

export default DiscountTourThree;
