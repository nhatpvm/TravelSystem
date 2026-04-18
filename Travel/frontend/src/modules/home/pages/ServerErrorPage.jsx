import React from 'react';
import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { ServerCrash, Home, RefreshCw } from 'lucide-react';

export default function ServerErrorPage() {
  return (
    <div className="min-h-screen bg-[#F0F4F8] flex items-center justify-center p-6 relative overflow-hidden">
      <div className="absolute inset-0">
        <img src="https://images.unsplash.com/photo-1531306728370-e2ebd9d7bb99?auto=format&fit=crop&q=80&w=1600" alt="" className="w-full h-full object-cover opacity-10" />
        <div className="absolute inset-0 bg-gradient-to-br from-rose-900/80 to-slate-900/80" />
      </div>
      <motion.div initial={{ opacity: 0, y: 30 }} animate={{ opacity: 1, y: 0 }} className="relative z-10 text-center max-w-xl">
        <motion.div animate={{ y: [0, -10, 0] }} transition={{ duration: 3, repeat: Infinity, ease: 'easeInOut' }} className="mb-6">
          <div className="w-28 h-28 bg-white/10 backdrop-blur rounded-[2rem] flex items-center justify-center mx-auto mb-4 border border-white/20">
            <ServerCrash size={56} className="text-rose-400" />
          </div>
          <p className="text-8xl font-black text-white drop-shadow-2xl">500</p>
        </motion.div>
        <h1 className="text-3xl font-black text-white mb-3">Máy chủ gặp sự cố!</h1>
        <p className="text-white/70 font-medium mb-2">Có lỗi xảy ra phía máy chủ. Đội kỹ thuật đang xử lý.</p>
        <p className="text-white/50 text-sm italic mb-10" style={{ fontFamily: "'Kalam', cursive" }}>"Đừng lo — chúng tôi sẽ khắc phục sớm thôi."</p>
        <div className="flex flex-col sm:flex-row gap-4 justify-center">
          <button onClick={() => window.location.reload()} className="flex items-center justify-center gap-2 px-8 py-4 bg-white text-rose-700 rounded-2xl font-black text-sm uppercase tracking-widest hover:bg-rose-50 transition-all shadow-xl">
            <RefreshCw size={16} /> Thử lại
          </button>
          <Link to="/" className="flex items-center justify-center gap-2 px-8 py-4 bg-white/10 backdrop-blur text-white rounded-2xl font-black text-sm uppercase tracking-widest hover:bg-white/20 transition-all border border-white/20">
            <Home size={16} /> Về trang chủ
          </Link>
        </div>
      </motion.div>
    </div>
  );
}
