import React from 'react';
import Navbar from '../../home/components/Navbar';
import Footer from '../../home/components/Footer';
import { ChevronRight } from 'lucide-react';
import { motion } from 'framer-motion';
import { Link } from 'react-router-dom';
import nav1 from '../../../assets/nav1.png';
import nav2 from '../../../assets/nav2.png';
import nav3 from '../../../assets/nav3.png';

const activitiesData = [
  { id: 1, title: "California", trips: "05 Chuyáº¿n Ä‘i", img: nav1 },
  { id: 2, title: "Trá»n gÃ³i", trips: "12 Chuyáº¿n Ä‘i", img: nav2 },
  { id: 3, title: "ThÃ¡i Lan", trips: "25 Chuyáº¿n Ä‘i", img: nav3 },
  { id: 4, title: "áº¤n Äá»™", trips: "45 Chuyáº¿n Ä‘i", img: nav1 },
  { id: 5, title: "Sugarland", trips: "15 Chuyáº¿n Ä‘i", img: nav2 },
  { id: 6, title: "Sugarland", trips: "35 Chuyáº¿n Ä‘i", img: nav3 },
  { id: 7, title: "áº¤n Äá»™", trips: "18 Chuyáº¿n Ä‘i", img: nav1 },
  { id: 8, title: "Sugarland", trips: "10 Chuyáº¿n Ä‘i", img: nav2 },
  { id: 9, title: "Sugarland", trips: "20 Chuyáº¿n Ä‘i", img: nav3 },
];

const ActivitiesPage = () => {
  return (
    <div className="min-h-screen bg-white flex flex-col font-sans">
      <Navbar />

      {/* Breadcrumb Header Section â€” same as About Us */}
      <section className="relative h-[450px] md:h-[550px] flex items-center justify-center overflow-hidden">
        <div className="absolute inset-0 z-0">
          <img
            src={nav1}
            alt="Hoáº¡t Ä‘á»™ng"
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
            Hoáº¡t Ä‘á»™ng
          </motion.h1>

          <motion.div
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.2 }}
            className="inline-flex items-center gap-4 bg-white/20 backdrop-blur-md px-10 py-4 rounded-full border border-white/30 text-lg font-bold"
          >
            <Link to="/" className="text-white hover:text-[#1EB4D4] transition-colors">Trang chá»§</Link>
            <ChevronRight size={20} className="text-[#1EB4D4]" />
            <span className="text-white">Hoáº¡t Ä‘á»™ng</span>
          </motion.div>
        </div>
      </section>

      {/* Activities Grid Section */}
      <section className="py-24 bg-white">
        <div className="container mx-auto px-4 md:px-12 lg:px-24">
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
            {activitiesData.map((item, idx) => (
              <motion.div
                key={item.id}
                initial={{ opacity: 0, y: 20 }}
                whileInView={{ opacity: 1, y: 0 }}
                transition={{ delay: idx * 0.1 }}
                className="group cursor-pointer"
              >
                {/* Image Card */}
                <div className="relative aspect-square rounded-[2rem] overflow-hidden mb-6 shadow-lg">
                  <img 
                    src={item.img} 
                    alt={item.title} 
                    className="w-full h-full object-cover transition-transform duration-700 group-hover:scale-110"
                  />
                  {/* Badge */}
                  <div className="absolute top-6 left-6 bg-[#1EB4D4] text-white text-[10px] font-black px-4 py-2 rounded-full uppercase tracking-wider backdrop-blur-sm bg-opacity-90">
                    ({item.trips})
                  </div>
                </div>

                {/* Title Below Image */}
                <h3 className="text-xl font-black text-gray-900 leading-tight transition-colors group-hover:text-[#1EB4D4]">
                  {item.title}
                </h3>
              </motion.div>
            ))}
          </div>
        </div>
      </section>

      <Footer />
    </div>
  );
};

export default ActivitiesPage;

