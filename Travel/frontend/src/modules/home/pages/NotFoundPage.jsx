import React from 'react';
import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { Compass, Home, ArrowRight } from 'lucide-react';

export default function NotFoundPage() {
  return (
    <div className="min-h-screen bg-[#F0F4F8] flex items-center justify-center p-6 relative overflow-hidden">
      {/* Background */}
      <div className="absolute inset-0">
        <img src="https://images.unsplash.com/photo-1488085061387-422e29b40080?auto=format&fit=crop&q=80&w=1600" alt="" className="w-full h-full object-cover opacity-10" />
        <div className="absolute inset-0 bg-gradient-to-br from-[#002B7F]/80 to-[#1EB4D4]/60" />
      </div>

      <motion.div
        initial={{ opacity: 0, y: 30 }}
        animate={{ opacity: 1, y: 0 }}
        className="relative z-10 text-center max-w-xl"
      >
        {/* Big 404 */}
        <motion.div
          animate={{ y: [0, -12, 0] }}
          transition={{ duration: 4, repeat: Infinity, ease: 'easeInOut' }}
          className="mb-6"
        >
          <div className="w-28 h-28 bg-white/10 backdrop-blur rounded-[2rem] flex items-center justify-center mx-auto mb-4 border border-white/20">
            <Compass size={56} className="text-[#1EB4D4]" />
          </div>
          <p className="text-8xl font-black text-white drop-shadow-2xl">404</p>
        </motion.div>

        <h1 className="text-3xl font-black text-white mb-3">Hành trình lạc đường!</h1>
        <p className="text-white/70 font-medium mb-2">
          Trang bạn đang tìm kiếm không tồn tại hoặc đã bị dời đi nơi khác.
        </p>
        <p className="text-white/50 text-sm italic mb-10" style={{ fontFamily: "'Kalam', cursive" }}>
          "Đôi khi lạc đường mới tìm được điều kỳ diệu."
        </p>

        <div className="flex flex-col sm:flex-row gap-4 justify-center">
          <Link to="/" className="flex items-center justify-center gap-2 px-8 py-4 bg-white text-[#002B7F] rounded-2xl font-black text-sm uppercase tracking-widest hover:bg-[#1EB4D4] hover:text-white transition-all shadow-xl">
            <Home size={16} /> Về trang chủ
          </Link>
          <Link to="/tours" className="flex items-center justify-center gap-2 px-8 py-4 bg-white/10 backdrop-blur text-white rounded-2xl font-black text-sm uppercase tracking-widest hover:bg-white/20 transition-all border border-white/20">
            Khám phá tour <ArrowRight size={16} />
          </Link>
        </div>
      </motion.div>
    </div>
  );
}
