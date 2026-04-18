import React from 'react';
import { ArrowRight } from 'lucide-react';
import { motion } from 'framer-motion';
import { Link } from 'react-router-dom';

const Promotions = () => {
  return (
    <section className="py-20 bg-white">
      <div className="container mx-auto px-4 md:px-12 lg:px-24">
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
          
          {/* Left Promotion Banner */}
          <motion.div 
            initial={{ opacity: 0, x: -30 }}
            whileInView={{ opacity: 1, x: 0 }}
            transition={{ duration: 0.6 }}
            className="relative h-[340px] rounded-[2rem] overflow-hidden bg-gradient-to-r from-[#1EB4D4] via-[#1EB4D4] to-[#A3E635] p-10 md:p-14 text-white group"
          >
            <div className="relative z-10 max-w-[60%]">
              <h4 className="text-4xl md:text-5xl font-black mb-4">GIẢM 35%</h4>
              <p className="text-xl md:text-2xl font-bold leading-tight mb-8">
                Khám phá Tour thế giới <br />
                Đặt phòng khách sạn.
              </p>
              <Link to="/tours" className="bg-white text-[#1EB4D4] px-8 py-3 rounded-full font-black flex items-center gap-2 hover:bg-gray-100 transition-colors shadow-lg">
                ĐẶT NGAY <ArrowRight size={20} />
              </Link>
            </div>
            
            {/* Illustrations */}
            <div className="absolute right-0 top-0 bottom-0 w-[50%] pointer-events-none">
              <motion.img 
                animate={{ y: [0, -10, 0], rotate: [0, 2, 0] }}
                transition={{ duration: 4, repeat: Infinity, ease: "easeInOut" }}
                src="https://ex-coders.com/html/turmet/assets/img/bag-shape.png" 
                alt="Promo 1" 
                className="w-full h-full object-contain object-right"
              />
            </div>
          </motion.div>

          {/* Right Promotion Banner */}
          <motion.div 
            initial={{ opacity: 0, x: 30 }}
            whileInView={{ opacity: 1, x: 0 }}
            transition={{ duration: 0.6 }}
            className="relative h-[340px] rounded-[2rem] overflow-hidden bg-gradient-to-r from-[#002B7F] via-[#002B7F] to-[#4F46E5] p-10 md:p-14 text-white group"
          >
            <div className="relative z-10 max-w-[60%]">
              <h4 className="text-4xl md:text-5xl font-black mb-4">GIẢM 35%</h4>
              <p className="text-xl md:text-2xl font-bold leading-tight mb-8">
                Dành cho vé máy bay <br />
                Nhận ngay bây giờ.
              </p>
              <Link to="/tours" className="bg-white text-[#002B7F] px-8 py-3 rounded-full font-black flex items-center gap-2 hover:bg-gray-100 transition-colors shadow-lg">
                ĐẶT NGAY <ArrowRight size={20} />
              </Link>
            </div>

            {/* Illustrations */}
            <div className="absolute right-0 top-0 bottom-0 w-[50%] pointer-events-none">
              <motion.img 
                animate={{ y: [0, -10, 0], scale: [1, 1.05, 1] }}
                transition={{ duration: 5, repeat: Infinity, ease: "easeInOut" }}
                src="https://ex-coders.com/html/turmet/assets/img/plane-shape.png" 
                alt="Promo 2" 
                className="w-full h-full object-contain object-right p-4"
              />
            </div>
          </motion.div>

        </div>
      </div>
    </section>
  );
};

export default Promotions;
