import React, { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { ArrowRight, ChevronLeft, ChevronRight, MapPin } from 'lucide-react';
import { Link } from 'react-router-dom';

const tours = [
  {
    id: 2,
    location: "Singapore",
    image: "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?q=80&w=800&auto=format&fit=crop",
    title: "Khám phá Moliva xinh đẹp: Thiên đường thiên nhiên",
    price: "$49.00",
  },
  {
    id: 3,
    location: "Hà Lan",
    image: "https://images.unsplash.com/photo-1464822759023-fed622ff2c3b?q=80&w=800&auto=format&fit=crop",
    title: "Thời điểm tuyệt nhất để khám phá thiên nhiên Molina",
    price: "$49.00",
  },
  {
    id: 4,
    location: "Thái Lan",
    image: "https://images.unsplash.com/photo-1528360983277-13d401cdc186?q=80&w=800&auto=format&fit=crop",
    title: "Tắm biển và chèo thuyền Kayak tại bãi biển Nonrival",
    price: "$49.00",
  },
  {
    id: 5,
    location: "Bali",
    image: "https://images.unsplash.com/photo-1537996194471-e657df975ab4?q=80&w=800&auto=format&fit=crop",
    title: "Yoga đón bình minh & Tour đền chùa tại Bali",
    price: "$59.00",
  },
  {
    id: 6,
    location: "Nhật Bản",
    image: "https://images.unsplash.com/photo-1540959733332-eab4deabeeaf?q=80&w=800&auto=format&fit=crop",
    title: "Ngắm hoa anh đào & Trải nghiệm văn hóa Nhật Bản",
    price: "$79.00",
  },
];

const CARDS_PER_PAGE = 4;

const FeaturedTours = () => {
  const [page, setPage] = useState(0);
  const [direction, setDirection] = useState(1);
  const totalPages = Math.ceil(tours.length / CARDS_PER_PAGE);

  const handlePrev = () => {
    if (page > 0) {
      setDirection(-1);
      setPage(p => p - 1);
    }
  };

  const handleNext = () => {
    if (page < totalPages - 1) {
      setDirection(1);
      setPage(p => p + 1);
    }
  };

  const visible = tours.slice(page * CARDS_PER_PAGE, page * CARDS_PER_PAGE + CARDS_PER_PAGE);

  const variants = {
    enter: (dir) => ({ opacity: 0, x: dir > 0 ? 80 : -80 }),
    center: { opacity: 1, x: 0 },
    exit: (dir) => ({ opacity: 0, x: dir > 0 ? -80 : 80 }),
  };

  return (
    <section className="py-24 bg-[#F8FBFB] relative overflow-hidden">

      {/* Decorative dashed path — LEFT */}
      <div className="absolute left-0 top-1/2 -translate-y-1/2 hidden lg:block opacity-30 pointer-events-none">
        <svg width="130" height="160" viewBox="0 0 130 160" fill="none">
          <path d="M130 10 Q60 80 130 150" stroke="#1EB4D4" strokeWidth="2" strokeDasharray="6 6" fill="none" />
        </svg>
        <div className="absolute top-4 right-4 text-[#1EB4D4]">
          <svg width="28" height="28" viewBox="0 0 24 24" fill="currentColor">
            <path d="M21 16v-2l-8-5V3.5A1.5 1.5 0 0 0 11.5 2h0A1.5 1.5 0 0 0 10 3.5V9l-8 5v2l8-2.5V19l-2 1.5V22l3.5-1 3.5 1v-1.5L13 19v-5.5l8 2.5z"/>
          </svg>
        </div>
      </div>

      {/* Decorative dashed path — RIGHT */}
      <div className="absolute right-0 top-1/2 -translate-y-1/2 hidden lg:block opacity-30 pointer-events-none">
        <svg width="130" height="160" viewBox="0 0 130 160" fill="none">
          <path d="M0 10 Q70 80 0 150" stroke="#1EB4D4" strokeWidth="2" strokeDasharray="6 6" fill="none" />
        </svg>
        <div className="absolute top-4 left-4 text-[#1EB4D4]">
          <svg width="28" height="28" viewBox="0 0 24 24" fill="currentColor">
            <path d="M21 16v-2l-8-5V3.5A1.5 1.5 0 0 0 11.5 2h0A1.5 1.5 0 0 0 10 3.5V9l-8 5v2l8-2.5V19l-2 1.5V22l3.5-1 3.5 1v-1.5L13 19v-5.5l8 2.5z"/>
          </svg>
        </div>
      </div>

      <div className="container mx-auto px-4 md:px-12 lg:px-24">
        {/* Header */}
        <div className="flex flex-col md:flex-row items-start md:items-end justify-between gap-6 mb-14">
          <div className="max-w-xl">
            <p
              className="text-[#1EB4D4] text-xl font-medium italic mb-3"
              style={{ fontFamily: "'Kalam', cursive" }}
            >
              Tour nổi bật
            </p>
            <h2 className="text-4xl md:text-5xl font-black text-gray-900 mb-4 leading-tight">
              Khám phá du lịch khó quên
            </h2>
            <p className="text-gray-400 font-medium leading-relaxed max-w-sm">
              Có rất nhiều lựa chọn chuyến đi dành cho bạn, từ thám hiểm mạo hiểm đến nghỉ dưỡng yên bình, giúp bạn tạo nên những kỷ niệm khó quên.
            </p>
          </div>

          {/* Navigation Arrows */}
          <div className="flex gap-3">
            <button
              onClick={handlePrev}
              disabled={page === 0}
              className={`w-12 h-12 rounded-full border-2 flex items-center justify-center transition-all
                ${page === 0
                  ? 'border-gray-100 text-gray-200 cursor-not-allowed'
                  : 'border-gray-200 hover:border-[#1EB4D4] hover:bg-[#1EB4D4] text-gray-400 hover:text-white shadow-sm'}`}
            >
              <ChevronLeft size={20} />
            </button>
            <button
              onClick={handleNext}
              disabled={page === totalPages - 1}
              className={`w-12 h-12 rounded-full flex items-center justify-center transition-all
                ${page === totalPages - 1
                  ? 'bg-gray-100 text-gray-300 cursor-not-allowed'
                  : 'bg-[#1EB4D4] hover:bg-gray-900 text-white shadow-lg shadow-[#1EB4D4]/30'}`}
            >
              <ChevronRight size={20} />
            </button>
          </div>
        </div>

        {/* Cards Grid — animated */}
        <div className="overflow-hidden">
          <AnimatePresence mode="wait" custom={direction}>
            <motion.div
              key={page}
              custom={direction}
              variants={variants}
              initial="enter"
              animate="center"
              exit="exit"
              transition={{ duration: 0.4, ease: 'easeInOut' }}
              className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6"
            >
              {visible.map((tour) => (
                <div
                  key={tour.id}
                  className="bg-white rounded-[2rem] overflow-hidden shadow-md hover:shadow-2xl transition-all duration-300 group"
                >
                  {/* Image */}
                  <div className="relative h-56 overflow-hidden">
                    <img
                      src={tour.image}
                      alt={tour.title}
                      className="w-full h-full object-cover transition-transform duration-500 group-hover:scale-110"
                    />
                    <div className="absolute top-4 left-4 flex items-center gap-1 bg-white/90 backdrop-blur-sm rounded-full px-3 py-1.5 text-gray-700 font-bold text-xs shadow">
                      <MapPin size={12} className="text-[#1EB4D4]" />
                      {tour.location}
                    </div>
                  </div>

                  {/* Content */}
                  <div className="p-6">
                    <Link to="/destinations/details">
                      <h3 className="text-lg font-black text-gray-900 mb-4 leading-snug group-hover:text-[#1EB4D4] transition-colors">
                        {tour.title}
                      </h3>
                    </Link>
                    <div className="flex items-center justify-between">
                      <div>
                        <span className="text-[#1EB4D4] font-black text-xl">{tour.price}</span>
                        <span className="text-gray-400 text-xs font-medium ml-1">/Mỗi ngày</span>
                      </div>
                      <Link to="/destinations/details" className="w-10 h-10 bg-[#1EB4D4]/10 hover:bg-[#1EB4D4] group-hover:bg-[#1EB4D4] rounded-full flex items-center justify-center text-[#1EB4D4] hover:text-white group-hover:text-white transition-all">
                        <ArrowRight size={16} />
                      </Link>
                    </div>
                  </div>
                </div>
              ))}
            </motion.div>
          </AnimatePresence>
        </div>

        {/* Dot Indicators */}
        <div className="flex justify-center gap-2 mt-10">
          {Array.from({ length: totalPages }).map((_, i) => (
            <button
              key={i}
              onClick={() => { setDirection(i > page ? 1 : -1); setPage(i); }}
              className={`h-2.5 rounded-full transition-all duration-300
                ${i === page ? 'w-8 bg-[#1EB4D4]' : 'w-2.5 bg-gray-300 hover:bg-[#1EB4D4]/50'}`}
            />
          ))}
        </div>
      </div>
    </section>
  );
};

export default FeaturedTours;
