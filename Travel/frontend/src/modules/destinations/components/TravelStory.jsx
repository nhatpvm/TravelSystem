import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { ArrowRight, Play } from 'lucide-react';
import nav1 from '../../../assets/nav1.png';
import nav3 from '../../../assets/nav3.png';

const TravelStory = () => {
  const [videoPlaying, setVideoPlaying] = useState(false);

  return (
    <section className="relative bg-white overflow-hidden py-20">
      {/* World Map Background */}
      <div className="absolute inset-0 flex items-center justify-center opacity-[0.04] pointer-events-none select-none">
        <svg viewBox="0 0 1440 700" xmlns="http://www.w3.org/2000/svg" className="w-full h-full">
          <text x="50%" y="50%" dominantBaseline="middle" textAnchor="middle" fontSize="700" fill="#1EB4D4" fontFamily="sans-serif" opacity="0.3">🌍</text>
        </svg>
        <img 
          src={nav3}
          alt="world map"
          className="absolute inset-0 w-full h-full object-contain opacity-100"
        />
      </div>

      <div className="container mx-auto px-4 relative z-10 text-center">
        {/* Label */}
        <motion.p
          initial={{ opacity: 0, y: -10 }}
          whileInView={{ opacity: 1, y: 0 }}
          className="text-[#1EB4D4] text-lg font-medium italic mb-4"
          style={{ fontFamily: "'Kalam', cursive" }}
        >
          Xem câu chuyện của chúng tôi
        </motion.p>

        {/* Heading */}
        <motion.h2
          initial={{ opacity: 0, y: 20 }}
          whileInView={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.1 }}
          className="text-4xl md:text-5xl lg:text-6xl font-black text-gray-900 leading-tight mb-12"
        >
          Trải nghiệm du lịch khó quên <br /> Nhận hướng dẫn của bạn
        </motion.h2>

        {/* Pulsing dot */}
        <div className="absolute top-24 left-1/4 w-3 h-3 bg-[#1EB4D4] rounded-full shadow-[0_0_16px_rgba(30,180,212,0.8)] animate-ping opacity-60"></div>

        {/* CTA Buttons */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          whileInView={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.2 }}
          className="flex items-center justify-center gap-5 mb-16"
        >
          <button className="flex items-center gap-2 bg-[#1EB4D4] hover:bg-[#17a0be] text-white px-8 py-4 rounded-full font-bold transition-all shadow-xl shadow-[#1EB4D4]/30 group">
            Liên hệ
            <ArrowRight size={18} className="group-hover:translate-x-1 transition-transform" />
          </button>
          <button className="flex items-center gap-2 bg-gray-900 hover:bg-[#1EB4D4] text-white px-8 py-4 rounded-full font-bold transition-all shadow-xl group">
            Đặt ngay
            <ArrowRight size={18} className="group-hover:translate-x-1 transition-transform" />
          </button>
        </motion.div>

        {/* Video Block */}
        <motion.div
          initial={{ opacity: 0, scale: 0.95 }}
          whileInView={{ opacity: 1, scale: 1 }}
          transition={{ delay: 0.3, duration: 0.7 }}
          className="relative mx-auto w-[90%] rounded-[2.5rem] overflow-hidden shadow-2xl cursor-pointer group"
          onClick={() => setVideoPlaying(true)}
        >
          {/* Thumbnail / Video */}
          {videoPlaying ? (
            <iframe
              className="w-full h-[680px]"
              src="https://www.youtube.com/embed/WpuqbPF6yH0?autoplay=1"
              title="Travel Story"
              frameBorder="0"
              allow="autoplay; encrypted-media"
              allowFullScreen
            />
          ) : (
            <>
              <img
                src={nav1}
                alt="Travel group"
                className="w-full h-[680px] object-cover transition-transform duration-700 group-hover:scale-105"
              />
              {/* Dark Overlay */}
              <div className="absolute inset-0 bg-black/40 group-hover:bg-black/30 transition-all duration-300"></div>

              {/* Play Button */}
              <div className="absolute inset-0 flex items-center justify-center">
                <motion.div
                  whileHover={{ scale: 1.15 }}
                  whileTap={{ scale: 0.95 }}
                  className="w-20 h-20 bg-[#1EB4D4] rounded-full flex items-center justify-center shadow-2xl shadow-[#1EB4D4]/50 relative"
                >
                  {/* Outer ring pulse */}
                  <div className="absolute inset-0 rounded-full bg-[#1EB4D4] animate-ping opacity-30"></div>
                  <Play size={30} className="text-white ml-1" fill="white" />
                </motion.div>
              </div>
            </>
          )}
        </motion.div>
      </div>
    </section>
  );
};

export default TravelStory;
