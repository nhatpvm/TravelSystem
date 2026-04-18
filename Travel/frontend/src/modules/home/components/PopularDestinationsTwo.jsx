import React from 'react';
import { motion } from 'framer-motion';
import { MapPin, Star, Heart, ArrowRight, Clock, Users } from 'lucide-react';
import { Link } from 'react-router-dom';

const destinations = [
  {
    id: 1,
    title: "Tour nghỉ dưỡng bãi biển Brooklyn",
    location: "Indonesia",
    img: "https://images.unsplash.com/photo-1519046904884-53103b34b206?q=80&w=800",
    price: "$59.00",
    rating: "4.7",
    days: "10 Ngày",
    capacity: "50+"
  },
  {
    id: 2,
    title: "Tour thị trấn Pak Chumphon",
    location: "Indonesia",
    img: "https://images.unsplash.com/photo-1537996194471-e657df975ab4?q=80&w=800",
    price: "$59.00",
    rating: "4.7",
    days: "10 Ngày",
    capacity: "50+"
  },
  {
    id: 3,
    title: "Phiêu lưu Java & Bali cả đời",
    location: "Indonesia",
    img: "https://images.unsplash.com/photo-1528127269322-539801943592?q=80&w=800",
    price: "$59.00",
    rating: "4.7",
    days: "10 Ngày",
    capacity: "50+"
  }
];

const PopularDestinationsTwo = () => {
    return (
        <section className="py-24 bg-[#F8FBFB] overflow-hidden">
            <div className="container mx-auto px-4 md:px-12 lg:px-24">
                {/* Header Side-by-Side */}
                <div className="flex flex-col lg:flex-row justify-between items-end mb-16 gap-8">
                    <div className="text-left">
                        <motion.p 
                            initial={{ opacity: 0, x: -20 }}
                            whileInView={{ opacity: 1, x: 0 }}
                            className="text-[#1EB4D4] text-xl font-medium mb-4 italic"
                            style={{ fontFamily: "'Kalam', cursive" }}
                        >
                            Những địa điểm gợi ý tốt nhất
                        </motion.p>
                        <motion.h2 
                            initial={{ opacity: 0, x: -20 }}
                            whileInView={{ opacity: 1, x: 0 }}
                            transition={{ delay: 0.1 }}
                            className="text-4xl md:text-5xl font-black text-gray-900 leading-tight"
                        >
                            Điểm đến phổ biến <br /> dành cho tất cả mọi người
                        </motion.h2>
                    </div>
                    <motion.div
                        initial={{ opacity: 0, x: 20 }}
                        whileInView={{ opacity: 1, x: 0 }}
                    >
                        <Link to="/tours" className="bg-white hover:bg-gray-900 hover:text-white text-gray-900 px-8 py-4 rounded-full font-black text-sm uppercase tracking-widest flex items-center gap-2 transition-all shadow-md group border border-gray-100">
                            Xem tất cả tour <ArrowRight size={18} className="group-hover:translate-x-1 transition-transform" />
                        </Link>
                    </motion.div>
                </div>

                {/* Vertical Style Cards Grid */}
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-10">
                    {destinations.map((tour, index) => (
                        <motion.div
                            key={tour.id}
                            initial={{ opacity: 0, y: 30 }}
                            whileInView={{ opacity: 1, y: 0 }}
                            transition={{ delay: index * 0.1 }}
                            className="bg-white rounded-[3rem] overflow-hidden shadow-sm hover:shadow-2xl transition-all duration-500 group border border-gray-50 flex flex-col h-full"
                        >
                            {/* Image Part */}
                            <div className="relative h-[320px] overflow-hidden">
                                <img 
                                    src={tour.img} 
                                    alt={tour.title} 
                                    className="w-full h-full object-cover transition-transform duration-1000 group-hover:scale-110"
                                />
                                <div className="absolute top-6 left-6">
                                    <div className="bg-[#1EB4D4] text-white px-4 py-1.5 rounded-full text-xs font-black uppercase tracking-widest shadow-lg">
                                        Nổi bật
                                    </div>
                                </div>
                                <button className="absolute top-6 right-6 w-11 h-11 bg-white/20 hover:bg-white backdrop-blur-md rounded-full flex items-center justify-center text-white hover:text-[#1EB4D4] transition-all shadow-xl">
                                    <Heart size={20} />
                                </button>
                                
                                <div className="absolute bottom-6 left-6 right-6">
                                    <div className="bg-white/90 backdrop-blur-sm p-4 rounded-2xl flex justify-between items-center shadow-lg">
                                        <div className="flex items-center gap-2 text-gray-900">
                                            <Star size={14} className="text-yellow-400" fill="currentColor" />
                                            <span className="text-sm font-black">{tour.rating}</span>
                                        </div>
                                        <div className="flex items-center gap-2 text-gray-900 font-black">
                                            <span className="text-lg leading-none">{tour.price}</span>
                                            <span className="text-[10px] text-gray-400 uppercase tracking-tighter">/ngày</span>
                                        </div>
                                    </div>
                                </div>
                            </div>

                            {/* Info Part */}
                            <div className="p-10 flex flex-col flex-grow">
                                <div className="flex items-center gap-2 text-[#1EB4D4] mb-3">
                                    <MapPin size={16} />
                                    <span className="text-xs font-black uppercase tracking-widest">{tour.location}</span>
                                </div>
                                <h3 className="text-2xl font-black text-gray-900 mb-6 leading-tight group-hover:text-[#1EB4D4] transition-colors line-clamp-2">
                                    {tour.title}
                                </h3>

                                <div className="mt-auto pt-6 border-t border-gray-50 flex items-center justify-between">
                                    <div className="flex items-center gap-4">
                                        <div className="flex items-center gap-1.5 text-gray-400 text-xs font-bold">
                                            <Clock size={16} />
                                            {tour.days}
                                        </div>
                                        <div className="flex items-center gap-1.5 text-gray-400 text-xs font-bold">
                                            <Users size={16} />
                                            {tour.capacity}
                                        </div>
                                    </div>
                                    <Link to="/tours/details" className="w-11 h-11 bg-gray-50 group-hover:bg-[#1EB4D4] rounded-full flex items-center justify-center text-gray-300 group-hover:text-white transition-all shadow-inner group-hover:shadow-lg">
                                        <ArrowRight size={20} />
                                    </Link>
                                </div>
                            </div>
                        </motion.div>
                    ))}
                </div>
            </div>
        </section>
    );
};

export default PopularDestinationsTwo;
