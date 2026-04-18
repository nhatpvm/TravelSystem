import React from 'react';
import Navbar from '../../home/components/Navbar';
import Footer from '../../home/components/Footer';
import BookingPlatform from '../../home/components/BookingPlatform';
import PopularDestinations from '../../home/components/PopularDestinations';
import TravelStory from '../components/TravelStory';
import FeaturedTours from '../components/FeaturedTours';
import { ChevronRight } from 'lucide-react';
import { motion } from 'framer-motion';
import { Link } from 'react-router-dom';


const DestinationsPage = () => {
  return (
    <div className="min-h-screen bg-white flex flex-col font-sans">
      <Navbar />

      {/* Breadcrumb Header Section — same as About Us */}
      <section className="relative h-[450px] md:h-[550px] flex items-center justify-center overflow-hidden">
        <div className="absolute inset-0 z-0">
          <img
            src="https://images.unsplash.com/photo-1476514525535-07fb3b4ae5f1?q=80&w=2070"
            alt="Our Destinations"
            className="w-full h-full object-cover"
          />
          <div className="absolute inset-0 bg-black/35"></div>
        </div>

        <div className="container mx-auto px-4 relative z-10 text-center text-white">
          <motion.h1
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            className="text-5xl md:text-7xl lg:text-8xl font-black mb-8 tracking-tighter"
          >
            Điểm đến của chúng tôi
          </motion.h1>

          <motion.div
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.2 }}
            className="inline-flex items-center gap-4 bg-white/20 backdrop-blur-md px-10 py-4 rounded-full border border-white/30 text-lg font-bold"
          >
            <Link to="/" className="text-white hover:text-[#1EB4D4] transition-colors">Trang chủ</Link>
            <ChevronRight size={20} className="text-[#1EB4D4]" />
            <span className="text-white">Điểm đến của chúng tôi</span>
          </motion.div>
        </div>
      </section>

      <PopularDestinations />

      <TravelStory />

      <FeaturedTours />

      <Footer />
    </div>
  );
};

export default DestinationsPage;
