import React from 'react';
import Navbar from '../../home/components/Navbar';
import Footer from '../../home/components/Footer';
import { ChevronRight } from 'lucide-react';
import { motion } from 'framer-motion';
import { Link } from 'react-router-dom';

const activitiesData = [
  { id: 1, title: "California", trips: "05 Chuyến đi", img: "https://images.unsplash.com/photo-1506461883276-594a12b11cf3?q=80&w=800" },
  { id: 2, title: "Trọn gói", trips: "12 Chuyến đi", img: "https://images.unsplash.com/photo-1540541338287-41700207dee6?q=80&w=800" },
  { id: 3, title: "Thái Lan", trips: "25 Chuyến đi", img: "https://images.unsplash.com/photo-1528360983277-13d401cdc186?q=80&w=800" },
  { id: 4, title: "Ấn Độ", trips: "45 Chuyến đi", img: "https://images.unsplash.com/photo-1548013146-72479768bbfd?q=80&w=800" },
  { id: 5, title: "Sugarland", trips: "15 Chuyến đi", img: "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?q=80&w=800" },
  { id: 6, title: "Sugarland", trips: "35 Chuyến đi", img: "https://images.unsplash.com/photo-1499793983690-e29da59ef1c2?q=80&w=800" },
  { id: 7, title: "Ấn Độ", trips: "18 Chuyến đi", img: "https://images.unsplash.com/photo-1464822759023-fed622ff2c3b?q=80&w=800" },
  { id: 8, title: "Sugarland", trips: "10 Chuyến đi", img: "https://images.unsplash.com/photo-1485833077593-4278bba3f11f?q=80&w=800" },
  { id: 9, title: "Sugarland", trips: "20 Chuyến đi", img: "https://images.unsplash.com/photo-1544191714-8024047ddaba?q=80&w=800" },
];

const ActivitiesPage = () => {
  return (
    <div className="min-h-screen bg-white flex flex-col font-sans">
      <Navbar />

      {/* Breadcrumb Header Section — same as About Us */}
      <section className="relative h-[450px] md:h-[550px] flex items-center justify-center overflow-hidden">
        <div className="absolute inset-0 z-0">
          <img
            src="https://images.unsplash.com/photo-1476514525535-07fb3b4ae5f1?q=80&w=2070"
            alt="Hoạt động"
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
            Hoạt động
          </motion.h1>

          <motion.div
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.2 }}
            className="inline-flex items-center gap-4 bg-white/20 backdrop-blur-md px-10 py-4 rounded-full border border-white/30 text-lg font-bold"
          >
            <Link to="/" className="text-white hover:text-[#1EB4D4] transition-colors">Trang chủ</Link>
            <ChevronRight size={20} className="text-[#1EB4D4]" />
            <span className="text-white">Hoạt động</span>
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
