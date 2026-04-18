import React from 'react';
import { Plane, Heart, Star, Clock, Users, ArrowRight, MapPin } from 'lucide-react';
import { motion } from 'framer-motion';
import { Link } from 'react-router-dom';

const destinations = [
  {
    id: 1,
    title: "Tour nghỉ dưỡng bãi biển Brooklyn",
    location: "Indonesia",
    img: "https://images.unsplash.com/photo-1519046904884-53103b34b206?q=80&w=2070&auto=format&fit=crop",
    price: "$59.00",
    rating: "4.7",
    days: "10 Ngày",
    capacity: "50+"
  },
  {
    id: 2,
    title: "Tour thị trấn Pak Chumphon",
    location: "Indonesia",
    img: "https://images.unsplash.com/photo-1537996194471-e657df975ab4?q=80&w=2138&auto=format&fit=crop",
    price: "$59.00",
    rating: "4.7",
    days: "10 Ngày",
    capacity: "50+"
  },
  {
    id: 3,
    title: "Phiêu lưu Java & Bali cả đời",
    location: "Indonesia",
    img: "https://images.unsplash.com/photo-1528127269322-539801943592?q=80&w=2000&auto=format&fit=crop",
    price: "$59.00",
    rating: "4.7",
    days: "10 Ngày",
    capacity: "50+"
  },
  {
    id: 4,
    title: "Những địa điểm du lịch tháng 11",
    location: "Indonesia",
    img: "https://images.unsplash.com/photo-1493976040374-85c8e12f0c0e?q=80&w=2070&auto=format&fit=crop",
    price: "$59.00",
    rating: "4.7",
    days: "10 Ngày",
    capacity: "50+"
  },
  {
    id: 5,
    title: "Khám phá đảo Phuket",
    location: "Thái Lan",
    img: "https://images.unsplash.com/photo-1589308078059-be1415eab4c3?q=80&w=2070&auto=format&fit=crop",
    price: "$59.00",
    rating: "4.7",
    days: "10 Ngày",
    capacity: "50+"
  },
  {
    id: 6,
    title: "Phiêu lưu dãy núi Alps Thụy Sĩ",
    location: "Thụy Sĩ",
    img: "https://images.unsplash.com/photo-1531366936337-7c912a4589a7?q=80&w=2070&auto=format&fit=crop",
    price: "$59.00",
    rating: "4.7",
    days: "10 Ngày",
    capacity: "50+"
  },
  {
    id: 7,
    title: "Khám phá Grand Canyon",
    location: "Mỹ",
    img: "https://images.unsplash.com/photo-1474044159687-1ee9f3a51722?q=80&w=2070&auto=format&fit=crop",
    price: "$59.00",
    rating: "4.7",
    days: "10 Ngày",
    capacity: "50+"
  },
  {
    id: 8,
    title: "Tour ánh sáng thành phố Tokyo",
    location: "Nhật Bản",
    img: "https://images.unsplash.com/photo-1540959733332-eab4deabeeaf?q=80&w=1788&auto=format&fit=crop",
    price: "$59.00",
    rating: "4.7",
    days: "10 Ngày",
    capacity: "50+"
  }
];

const PopularDestinations = () => {
  return (
    <section className="py-24 px-4 md:px-12 lg:px-24 bg-white relative overflow-hidden">
        {/* Decorative Car Illustration - Moved to Bottom Left and made clearer */}
        <div className="absolute bottom-10 left-[-30px] opacity-60 pointer-events-none z-0 hidden xl:block">
           <motion.img 
             animate={{ x: [-25, 25, -25] }}
             transition={{ duration: 10, repeat: Infinity, ease: "easeInOut" }}
             src="https://ex-coders.com/html/turmet/assets/img/destination/car.png" 
             alt="car decoration" 
             className="w-full max-w-md"
           />
        </div>

        {/* Header Section */}
        <div className="flex flex-col md:flex-row justify-between items-end mb-16 gap-6">
          <div className="text-left">
             <p className="text-[#1EB4D4] text-xl font-medium mb-4 italic tracking-wider" style={{ fontFamily: "'Kalam', cursive" }}>
                Những địa điểm gợi ý tốt nhất
             </p>
             <h2 className="text-4xl md:text-5xl lg:text-5xl font-black text-gray-900 leading-tight">
                Điểm đến phổ biến <br />
                dành cho tất cả mọi người
             </h2>
          </div>
          <Link to="/tours" className="bg-[#1EB4D4] hover:bg-[#19a7c5] text-white px-8 py-3 rounded-full font-black flex items-center gap-2 transition-all shadow-lg">
            Xem tất cả tour <ArrowRight size={20} />
          </Link>
        </div>

        {/* Categories Grid */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-8">
          {destinations.map((item, idx) => (
            <motion.div 
              key={item.id}
              initial={{ opacity: 0, y: 20 }}
              whileInView={{ opacity: 1, y: 0 }}
              transition={{ delay: idx * 0.1 }}
              className="bg-white rounded-[2rem] border border-gray-100 shadow-xl overflow-hidden group hover:shadow-2xl transition-all duration-300"
            >
              {/* Image Container */}
              <div className="relative h-[280px] overflow-hidden">
                <img src={item.img} alt={item.title} className="w-full h-full object-cover transition-transform duration-700 group-hover:scale-110" />
                <button className="absolute top-5 right-5 w-10 h-10 bg-black/20 hover:bg-black/40 backdrop-blur-md rounded-full flex items-center justify-center text-white transition-colors">
                   <Heart size={20} />
                </button>
              </div>

              {/* Card Footer Content */}
              <div className="p-8">
                <div className="flex justify-between items-center mb-4">
                  <div className="flex items-center text-gray-400 gap-1 text-sm font-medium">
                    <MapPin size={16} /> {item.location}
                  </div>
                  <div className="bg-[#1EB4D4]/10 text-[#1EB4D4] px-3 py-1 rounded-full flex items-center gap-1 text-xs font-black">
                    <Star size={12} fill="currentColor" /> {item.rating}
                  </div>
                </div>

                <Link to="/destinations/details">
                  <h3 className="text-xl font-bold text-gray-900 mb-6 leading-tight hover:text-[#1EB4D4] cursor-pointer transition-colors">
                    {item.title}
                  </h3>
                </Link>

                <div className="flex items-center gap-4 text-gray-400 text-sm mb-6 pb-6 border-b border-dashed border-gray-100">
                  <div className="flex items-center gap-2">
                    <Clock size={16} /> {item.days}
                  </div>
                  <div className="flex items-center gap-2">
                    <Users size={16} /> {item.capacity}
                  </div>
                </div>

                <div className="flex justify-between items-center">
                   <div>
                     <span className="text-xl font-black text-gray-900">{item.price}</span>
                     <span className="text-xs text-gray-400 font-medium lowercase ml-1">/Mỗi ngày</span>
                   </div>
                   <Link to="/destinations/details" className="bg-[#1EB4D4]/10 hover:bg-[#1EB4D4] text-[#1EB4D4] hover:text-white px-5 py-2.5 rounded-full text-sm font-black transition-all flex items-center gap-2">
                      Đặt ngay <ArrowRight size={16} />
                   </Link>
                </div>
              </div>
            </motion.div>
          ))}
        </div>
    </section>
  );
};

export default PopularDestinations;
