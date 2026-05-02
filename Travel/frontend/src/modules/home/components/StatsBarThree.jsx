import React from 'react';
import { motion } from 'framer-motion';
import nav2 from '../../../assets/nav2.png';

const stats = [
  { value: "26+", label: "Năm Kinh Nghiệm" },
  { value: "3.6k+", label: "Khách Hàng Hài Lòng" },
  { value: "46+", label: "Trên Toàn Thế Giới" },
  { value: "56+", label: "Giải Thưởng & Danh Hiệu" }
];

const StatsBarThree = () => {
  return (
    <section className="relative overflow-hidden">
      <div className="bg-[#0F2A3C] relative">
        {/* Decorative image on right */}
        <div className="absolute right-0 top-0 bottom-0 w-[300px] hidden lg:block">
          <img
            src={nav2}
            alt="Traveler"
            className="h-full w-full object-cover object-center"
          />
          {/* Fade overlay */}
          <div className="absolute inset-0 bg-gradient-to-r from-[#0F2A3C] via-[#0F2A3C]/50 to-transparent"></div>
        </div>

        {/* Decorative star */}
        <div className="absolute bottom-4 right-[280px] hidden lg:block">
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none" className="text-white/20">
            <path d="M12 0L14.5 9.5L24 12L14.5 14.5L12 24L9.5 14.5L0 12L9.5 9.5L12 0Z" fill="currentColor"/>
          </svg>
        </div>

        <div className="container mx-auto px-4 md:px-12 lg:px-24">
          <div className="flex items-center py-10 lg:py-14">
            <div className="grid grid-cols-2 md:grid-cols-4 gap-8 lg:gap-16 w-full lg:w-[70%]">
              {stats.map((stat, idx) => (
                <motion.div
                  key={idx}
                  initial={{ opacity: 0, y: 20 }}
                  whileInView={{ opacity: 1, y: 0 }}
                  transition={{ delay: idx * 0.1 }}
                  viewport={{ once: true }}
                  className="text-center md:text-left"
                >
                  <h3 className="text-4xl md:text-5xl font-black text-white mb-2">{stat.value}</h3>
                  <p className="text-white/40 text-sm font-bold tracking-wide">{stat.label}</p>
                </motion.div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </section>
  );
};

export default StatsBarThree;
