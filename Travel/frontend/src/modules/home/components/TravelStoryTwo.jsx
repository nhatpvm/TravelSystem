import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { ArrowRight, Play } from 'lucide-react';
import nav1 from '../../../assets/nav1.png';
import nav3 from '../../../assets/nav3.png';

const TravelStoryTwo = () => {
  const [videoPlaying, setVideoPlaying] = useState(false);

  return (
    <section className="relative bg-white overflow-hidden py-32">
      {/* World Map Background Decoration */}
      <div className="absolute inset-0 flex items-center justify-center opacity-[0.03] pointer-events-none select-none">
        <img 
          src={nav3}
          alt="world map"
          className="w-full h-full object-contain"
        />
      </div>

      <div className="container mx-auto px-4 md:px-12 lg:px-24 relative z-10 text-center">
        {/* Label */}
        <motion.p
          initial={{ opacity: 0, y: -10 }}
          whileInView={{ opacity: 1, y: 0 }}
          className="text-[#1EB4D4] text-xl font-medium italic mb-6"
          style={{ fontFamily: "'Kalam', cursive" }}
        >
          Xem câu chuyện của chúng tôi
        </motion.p>

        {/* Heading */}
        <motion.h2
          initial={{ opacity: 0, y: 20 }}
          whileInView={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.1 }}
          className="text-4xl md:text-6xl font-black text-gray-900 leading-tight mb-12 tracking-tight"
        >
          Trải nghiệm du lịch khó quên <br /> Nhận hướng dẫn của bạn
        </motion.h2>

        {/* Video Block */}
        <motion.div
          initial={{ opacity: 0, scale: 0.95 }}
          whileInView={{ opacity: 1, scale: 1 }}
          transition={{ delay: 0.3, duration: 0.8 }}
          className="relative mx-auto w-full max-w-6xl aspect-video rounded-[3rem] overflow-hidden shadow-[0_20px_50px_rgba(0,0,0,0.15)] cursor-pointer group"
          onClick={() => setVideoPlaying(true)}
        >
          {/* Thumbnail / Video */}
          {videoPlaying ? (
            <iframe
              className="w-full h-full"
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
                alt="Travel experience"
                className="w-full h-full object-cover transition-transform duration-1000 group-hover:scale-110"
              />
              {/* Overlay with subtle gradient */}
              <div className="absolute inset-0 bg-gradient-to-b from-black/20 via-transparent to-black/40 group-hover:from-black/10 group-hover:to-black/30 transition-all duration-300"></div>

              {/* Play Button */}
              <div className="absolute inset-0 flex items-center justify-center">
                <motion.div
                  whileHover={{ scale: 1.1 }}
                  whileTap={{ scale: 0.9 }}
                  className="w-24 h-24 bg-white rounded-full flex items-center justify-center shadow-2xl relative"
                >
                  <div className="absolute inset-[-10px] rounded-full border border-white/30 animate-ping"></div>
                  <Play size={32} className="text-[#1EB4D4] ml-1" fill="#1EB4D4" />
                </motion.div>
              </div>
              
              {/* Bottom Info Bar */}
              <div className="absolute bottom-10 left-10 text-left">
                <p className="text-white/60 text-xs font-black uppercase tracking-[0.2em] mb-2">Video thực tế</p>
                <h4 className="text-white text-2xl font-black">Hành trình khám phá Đông Nam Á</h4>
              </div>
            </>
          )}
        </motion.div>

        {/* Bottom CTA */}
        <motion.div
           initial={{ opacity: 0 }}
           whileInView={{ opacity: 1 }}
           transition={{ delay: 0.5 }}
           className="mt-16 flex justify-center gap-6"
        >
            <button className="bg-[#1A3D44] hover:bg-black text-white px-12 py-5 rounded-full font-black uppercase text-sm tracking-widest transition-all shadow-xl flex items-center gap-3 group">
                Bắt đầu ngay <ArrowRight size={18} className="group-hover:translate-x-2 transition-transform" />
            </button>
        </motion.div>
      </div>
    </section>
  );
};

export default TravelStoryTwo;
