import React, { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { MoveRight, Star, Plane } from 'lucide-react';

const tabs = ['TẤT CẢ', 'MỘT CHIỀU', 'KHỨ HỒI'];

const flights = [
  {
    id: 1,
    image: "https://images.unsplash.com/photo-1530841377377-3ff06c0ca713?q=80&w=800",
    airline: "American Airlines",
    type: "Bay một chiều",
    route: "London đến Paris",
    price: "$450.00",
    rating: 4,
    reviews: "450+ Đánh giá",
    category: "MỘT CHIỀU"
  },
  {
    id: 2,
    image: "https://images.unsplash.com/photo-1544551763-46a013bb70d5?q=80&w=800",
    airline: "Dubai Airlines",
    type: "Bay một chiều",
    route: "Paris đến Dubai",
    price: "$450.00",
    rating: 4.5,
    reviews: "450+ Đánh giá",
    category: "MỘT CHIỀU"
  },
  {
    id: 3,
    image: "https://images.unsplash.com/photo-1519451241324-20b4ea2c4220?q=80&w=800",
    airline: "American Airlines",
    type: "Bay một chiều",
    route: "Nepal đến Mỹ",
    price: "$450.00",
    rating: 4,
    reviews: "450+ Đánh giá",
    category: "KHỨ HỒI"
  }
];

const FeaturedFlightDeals = () => {
  const [activeTab, setActiveTab] = useState('TẤT CẢ');

  const filtered = activeTab === 'TẤT CẢ' ? flights : flights.filter(f => f.category === activeTab);

  return (
    <section className="py-24 bg-white">
      <div className="container mx-auto px-4 md:px-12 lg:px-24">
        {/* Header */}
        <div className="flex flex-col md:flex-row justify-between items-start md:items-end mb-14 gap-6">
          <div>
            <p
              className="text-[#1EB4D4] text-lg font-medium mb-3 italic"
              style={{ fontFamily: "'Kalam', cursive" }}
            >
              Chuyến bay
            </p>
            <h2 className="text-4xl md:text-5xl font-black text-gray-900 leading-tight">
              Ưu Đãi Chuyến Bay Nổi Bật
            </h2>
          </div>

          {/* Tabs */}
          <div className="flex gap-2">
            {tabs.map((tab) => (
              <button
                key={tab}
                onClick={() => setActiveTab(tab)}
                className={`px-6 py-2.5 rounded-full text-xs font-bold uppercase tracking-wider transition-all border ${
                  activeTab === tab
                    ? 'bg-[#1EB4D4] text-white border-[#1EB4D4] shadow-lg shadow-[#1EB4D4]/20'
                    : 'bg-white text-gray-500 border-gray-200 hover:border-[#1EB4D4] hover:text-[#1EB4D4]'
                }`}
              >
                {tab}
              </button>
            ))}
          </div>
        </div>

        {/* Flight Cards Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
          <AnimatePresence mode="wait">
            {filtered.map((flight, idx) => (
              <motion.div
                key={flight.id}
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, y: -20 }}
                transition={{ delay: idx * 0.1 }}
                className="bg-white rounded-3xl overflow-hidden border border-gray-100 shadow-[0_10px_40px_rgba(0,0,0,0.04)] hover:shadow-xl transition-shadow duration-500 group"
              >
                {/* Image */}
                <div className="relative h-52 overflow-hidden">
                  <img
                    src={flight.image}
                    alt={flight.route}
                    className="w-full h-full object-cover group-hover:scale-110 transition-transform duration-700"
                  />
                </div>

                {/* Content */}
                <div className="p-6">
                  {/* Airline & Type */}
                  <div className="flex items-center justify-between mb-3">
                    <div className="flex items-center gap-2">
                      <Plane size={14} className="text-[#1EB4D4]" />
                      <span className="text-gray-400 text-xs font-bold">{flight.airline}</span>
                    </div>
                    <span className="text-gray-400 text-xs font-medium">{flight.type}</span>
                  </div>

                  {/* Route & Price */}
                  <div className="flex items-center justify-between mb-5">
                    <h3 className="text-lg font-black text-gray-900">{flight.route}</h3>
                    <div className="text-right">
                      <span className="text-[10px] text-gray-400 font-medium">từ</span>
                      <p className="text-[#1EB4D4] text-lg font-black">{flight.price}</p>
                    </div>
                  </div>

                  {/* Rating & Book */}
                  <div className="flex items-center justify-between pt-5 border-t border-gray-100">
                    <div>
                      <div className="flex gap-1 mb-1">
                        {[...Array(5)].map((_, i) => (
                          <Star
                            key={i}
                            size={14}
                            className={`${i < Math.floor(flight.rating) ? 'fill-amber-400 text-amber-400' : 'fill-gray-200 text-gray-200'}`}
                          />
                        ))}
                      </div>
                      <span className="text-gray-400 text-xs font-medium">{flight.reviews}</span>
                    </div>
                    <button className="flex items-center gap-2 px-5 py-2.5 rounded-full border border-gray-200 text-sm font-bold text-gray-700 hover:bg-[#1EB4D4] hover:text-white hover:border-[#1EB4D4] transition-all group/btn">
                      Đặt ngay <MoveRight size={16} className="group-hover/btn:translate-x-1 transition-transform" />
                    </button>
                  </div>
                </div>
              </motion.div>
            ))}
          </AnimatePresence>
        </div>
      </div>
    </section>
  );
};

export default FeaturedFlightDeals;
