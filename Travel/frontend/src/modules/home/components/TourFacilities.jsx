import React from 'react';
import { Play, ArrowRight } from 'lucide-react';
import { motion } from 'framer-motion';

const TourFacilities = () => {
  return (
    <section className="relative h-[600px] flex items-center justify-center overflow-hidden">
      {/* Background Image with Overlay */}
      <div className="absolute inset-0 z-0">
        <img 
          src="https://images.unsplash.com/photo-1528127269322-539801943592?q=80&w=2070&auto=format&fit=crop" 
          alt="Scenic tour background" 
          className="w-full h-full object-cover"
        />
        {/* Dark Overlay for readability */}
        <div className="absolute inset-0 bg-black/40"></div>
      </div>

      <div className="container mx-auto px-4 relative z-10 text-center text-white">
        <motion.div
           initial={{ opacity: 0, y: 30 }}
           whileInView={{ opacity: 1, y: 0 }}
           transition={{ duration: 0.8 }}
        >
          <p className="text-xl md:text-2xl font-medium mb-4 italic" style={{ fontFamily: "'Kalam', cursive" }}>
            Xem câu chuyện của chúng tôi
          </p>
          
          <h2 className="text-4xl md:text-6xl font-black mb-12 max-w-4xl mx-auto leading-tight">
            Chúng tôi cung cấp các <br /> cơ sở vật chất tour tốt nhất
          </h2>

          <div className="flex flex-col sm:flex-row items-center justify-center gap-6">
            <motion.button 
              whileHover={{ scale: 1.05 }}
              whileTap={{ scale: 0.95 }}
              className="bg-[#1EB4D4] hover:bg-[#19a7c5] text-white px-8 py-4 rounded-full font-black flex items-center gap-2 transition-all shadow-xl shadow-[#1EB4D4]/30"
            >
              Tìm hiểu thêm <ArrowRight size={20} />
            </motion.button>

            <motion.button 
              whileHover={{ scale: 1.05 }}
              whileTap={{ scale: 0.95 }}
              className="group flex items-center gap-4 text-white font-black"
            >
              <div className="w-14 h-14 rounded-full border-2 border-white/50 flex items-center justify-center group-hover:bg-white group-hover:text-[#1EB4D4] transition-all">
                <Play fill="currentColor" size={20} className="ml-1" />
              </div>
              <span className="text-lg">Xem Video</span>
            </motion.button>
          </div>
        </motion.div>
      </div>

      {/* Decorative pulse line (if needed for consistency) */}
      <div className="absolute bottom-0 left-0 right-0 h-24 bg-gradient-to-t from-black/20 to-transparent"></div>
    </section>
  );
};

export default TourFacilities;
