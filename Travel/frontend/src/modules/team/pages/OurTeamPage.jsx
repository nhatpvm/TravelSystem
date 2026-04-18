import React from 'react';
import Navbar from '../../home/components/Navbar';
import Footer from '../../home/components/Footer';
import TourGuides from '../../home/components/TourGuides';
import { ChevronRight } from 'lucide-react';
import { motion } from 'framer-motion';
import { Link } from 'react-router-dom';

const OurTeamPage = () => {
  return (
    <div className="min-h-screen bg-white flex flex-col font-sans">
      <Navbar />

      {/* Breadcrumb Header Section — same as About Us */}
      <section className="relative h-[450px] md:h-[550px] flex items-center justify-center overflow-hidden">
        <div className="absolute inset-0 z-0">
          <img
            src="https://images.unsplash.com/photo-1522202176988-66273c2fd55f?q=80&w=2071"
            alt="Đội ngũ của chúng tôi"
            className="w-full h-full object-cover"
          />
          <div className="absolute inset-0 bg-black/40"></div>
        </div>

        <div className="container mx-auto px-4 relative z-10 text-center text-white">
          <motion.h1
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            className="text-5xl md:text-7xl lg:text-8xl font-black mb-8 tracking-tighter"
          >
            Đội ngũ của chúng tôi
          </motion.h1>

          <motion.div
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.2 }}
            className="inline-flex items-center gap-4 bg-white/20 backdrop-blur-md px-10 py-4 rounded-full border border-white/30 text-lg font-bold"
          >
            <Link to="/" className="text-white hover:text-[#1EB4D4] transition-colors">Trang chủ</Link>
            <ChevronRight size={20} className="text-[#1EB4D4]" />
            <span className="text-white">Đội ngũ của chúng tôi</span>
          </motion.div>
        </div>
      </section>

      {/* Team Content - Importing TourGuides component */}
      <div className="py-20">
        <TourGuides />
        
        {/* Additional Team Section if needed */}
        <section className="container mx-auto px-4 md:px-12 lg:px-24 mb-24">
            <div className="bg-[#1EB4D4] rounded-[3rem] p-12 md:p-20 text-white relative overflow-hidden">
                <div className="relative z-10 flex flex-col lg:flex-row items-center justify-between gap-12">
                    <div className="max-w-2xl text-center lg:text-left">
                        <h2 className="text-4xl md:text-5xl font-black mb-6">Bạn muốn gia nhập đội ngũ của chúng tôi?</h2>
                        <p className="text-lg opacity-90 font-medium">Chúng tôi luôn tìm kiếm những tài năng đam mê du lịch và muốn mang lại trải nghiệm tốt nhất cho khách hàng.</p>
                    </div>
                    <button className="bg-white text-[#1EB4D4] px-10 py-5 rounded-full font-black text-xl hover:bg-gray-900 hover:text-white transition-all shadow-2xl">
                        Ứng tuyển ngay
                    </button>
                </div>
                {/* Decorative Pattern */}
                <div className="absolute top-0 right-0 w-64 h-64 bg-white/10 rounded-full -mr-32 -mt-32 blur-3xl"></div>
            </div>
        </section>
      </div>

      <Footer />
    </div>
  );
};

export default OurTeamPage;
