import React, { useRef } from 'react';
import { motion } from 'framer-motion';
import { ArrowRight, ArrowLeft, Plane, Users } from 'lucide-react';

const flights = [
  {
    id: 1,
    airline: "NOVOAIR",
    logo: "https://ex-coders.com/html/turmet/assets/img/feature/ar4.png",
    tag: "Bay nhanh nhất",
    seats: "05 Ghế",
    from: "New Work",
    to: "Nepal",
    depTime: "08.30AM",
    depDate: "25 Nov 2024",
    arrTime: "12.50PM",
    arrDate: "25 Nov 2024",
    duration: "04h 20 phút",
    class: "Thương gia",
    price: "$1520"
  },
  {
    id: 2,
    airline: "Qatar Airways",
    logo: "https://ex-coders.com/html/turmet/assets/img/feature/ar3.png",
    tag: "Bay nhanh nhất",
    seats: "08 Ghế",
    from: "London",
    to: "Dubai",
    depTime: "10.15AM",
    depDate: "26 Nov 2024",
    arrTime: "09.30PM",
    arrDate: "26 Nov 2024",
    duration: "07h 15 phút",
    class: "Hạng nhất",
    price: "$2100"
  },
  {
    id: 3,
    airline: "Etihad Airways",
    logo: "https://ex-coders.com/html/turmet/assets/img/feature/ar1.png",
    tag: "Rẻ nhất",
    seats: "12 Ghế",
    from: "Paris",
    to: "Tokyo",
    depTime: "11.00PM",
    depDate: "27 Nov 2024",
    arrTime: "06.20PM",
    arrDate: "28 Nov 2024",
    duration: "13h 20 phút",
    class: "Phổ thông",
    price: "$850"
  },
  {
    id: 4,
    airline: "US-Bangla",
    logo: "https://ex-coders.com/html/turmet/assets/img/feature/ar4.png",
    tag: "Đề xuất",
    seats: "04 Ghế",
    from: "Sydney",
    to: "Singapore",
    depTime: "07.45AM",
    depDate: "29 Nov 2024",
    arrTime: "01.10PM",
    arrDate: "29 Nov 2024",
    duration: "08h 25 phút",
    class: "Cao cấp",
    price: "$1250"
  }
];

const airlines = [
  { name: "Etihad Airways", logo: "https://ex-coders.com/html/turmet/assets/img/feature/ar1.png" },
  { name: "US-Bangla Airlines", logo: "https://ex-coders.com/html/turmet/assets/img/feature/ar2.png" },
  { name: "Qatar Airways", logo: "https://ex-coders.com/html/turmet/assets/img/feature/ar3.png" },
  { name: "NOVOAIR", logo: "https://ex-coders.com/html/turmet/assets/img/feature/ar4.png" }
];

const FlightDealsThree = () => {
  const scrollRef = useRef(null);

  const scroll = (direction) => {
    const { current } = scrollRef;
    if (current) {
      const scrollAmount = current.offsetWidth / (window.innerWidth >= 1024 ? 2 : 1);
      current.scrollBy({
        left: direction === 'left' ? -scrollAmount : scrollAmount,
        behavior: 'smooth'
      });
    }
  };

  return (
    <section className="py-24 bg-[#F8FBFC] overflow-hidden">
      <div className="container mx-auto px-4 md:px-12 lg:px-24">
        
        {/* Last Minute Deals Header */}
        <div className="flex flex-col md:flex-row justify-between items-end mb-12 gap-8">
          <div>
            <p 
              className="text-[#1EB4D4] text-xl font-medium mb-4 italic"
              style={{ fontFamily: "'Kalam', cursive" }}
            >
              Ưu đãi & Khuyến mãi
            </p>
            <h2 className="text-4xl md:text-5xl font-black text-gray-900 leading-tight">
              Ưu Đãi Phút Chót
            </h2>
          </div>
          
          <div className="flex gap-4">
            <button 
              onClick={() => scroll('left')}
              className="w-14 h-14 rounded-full bg-white border border-gray-100 flex items-center justify-center text-gray-900 hover:bg-[#1EB4D4] hover:text-white transition-all shadow-sm group"
            >
              <ArrowLeft size={24} className="group-active:scale-90 transition-transform" />
            </button>
            <button 
              onClick={() => scroll('right')}
              className="w-14 h-14 rounded-full bg-[#0F172A] flex items-center justify-center text-white hover:bg-[#1EB4D4] transition-all shadow-xl group"
            >
              <ArrowRight size={24} className="group-active:scale-90 transition-transform" />
            </button>
          </div>
        </div>

        {/* Flight Cards Slider */}
        <div 
          ref={scrollRef}
          className="flex gap-8 mb-32 overflow-x-auto scrollbar-hide no-scrollbar snap-x snap-mandatory pb-10"
          style={{ scrollbarWidth: 'none', msOverflowStyle: 'none' }}
        >
          {flights.map((flight) => (
            <motion.div 
              key={flight.id}
              className="min-w-full lg:min-w-[calc(50%-16px)] snap-start bg-white rounded-[2.5rem] p-8 md:p-12 shadow-[0_15px_50px_rgba(0,0,0,0.05)] border border-gray-50 flex flex-col gap-10"
            >
              {/* Header: Logo, Tag, Seats */}
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-4">
                  <div className="w-12 h-12 rounded-full bg-gray-50 p-2 overflow-hidden flex items-center justify-center">
                    <img src={flight.logo} alt={flight.airline} className="w-full h-auto object-contain" />
                  </div>
                  <span className="text-xl font-black text-gray-900 uppercase tracking-tighter">{flight.airline}</span>
                </div>
                <div className="flex flex-wrap items-center justify-end gap-4 md:gap-10">
                  <span className="bg-[#1EB4D4] text-white px-4 md:px-6 py-2 rounded-full text-[10px] md:text-xs font-black uppercase tracking-wider">
                    {flight.tag}
                  </span>
                  <span className="text-[#1EB4D4] text-sm font-bold">{flight.seats}</span>
                </div>
              </div>

              {/* Center: Route Visualization */}
              <div className="relative flex items-center justify-between px-2">
                <div className="text-center">
                  <p className="text-gray-400 text-sm font-medium mb-1">{flight.from}</p>
                </div>
                <div className="flex-1 flex items-center px-4 md:px-8 relative">
                    <div className="w-3 h-3 bg-[#1EB4D4]/20 rounded-full"></div>
                    <div className="flex-1 h-[1px] border-t-2 border-dashed border-[#1EB4D4]/30 relative flex items-center justify-center">
                        <div className="bg-white px-2 md:px-4">
                            <Plane className="text-[#1EB4D4]" size={20} />
                        </div>
                    </div>
                    <div className="w-3 h-3 bg-[#1EB4D4]/20 rounded-full"></div>
                </div>
                <div className="text-center">
                  <p className="text-gray-400 text-sm font-medium mb-1">{flight.to}</p>
                </div>
              </div>

              {/* Times and Price Row */}
              <div className="grid grid-cols-3 items-center">
                <div className="text-left">
                  <p className="text-xl md:text-2xl font-black text-gray-900">{flight.depTime}</p>
                  <p className="text-gray-400 text-xs md:text-sm font-medium">{flight.depDate}</p>
                </div>
                <div className="text-center">
                   <p className="text-gray-400 text-[10px] md:text-xs font-bold uppercase tracking-widest">{flight.duration}</p>
                </div>
                <div className="text-right">
                  <p className="text-xl md:text-2xl font-black text-gray-900">{flight.arrTime}</p>
                  <p className="text-gray-400 text-xs md:text-sm font-medium">{flight.arrDate}</p>
                </div>
              </div>

              {/* Footer: Class, Price, Link */}
              <div className="pt-8 border-t border-gray-100 flex flex-wrap gap-4 items-center justify-between">
                <div className="flex items-center gap-6 md:gap-10">
                   <div className="flex items-center gap-3">
                      <div className="w-8 h-8 rounded-full bg-[#1EB4D4]/10 flex items-center justify-center text-[#1EB4D4]">
                        <Users size={14} />
                      </div>
                      <span className="text-gray-500 font-bold text-sm whitespace-nowrap">{flight.class}</span>
                   </div>
                   <span className="text-xl md:text-2xl font-black text-[#1EB4D4]">{flight.price}</span>
                </div>
                <button className="flex items-center gap-2 text-gray-900 font-black hover:text-[#1EB4D4] transition-colors group">
                   Chi tiết chuyến bay <ArrowRight size={18} className="group-hover:translate-x-1 transition-transform" />
                </button>
              </div>
            </motion.div>
          ))}
        </div>

        {/* Top Airlines Section */}
        <div className="text-center mb-16">
          <p 
            className="text-[#1EB4D4] text-xl font-medium mb-4 italic"
            style={{ fontFamily: "'Kalam', cursive" }}
          >
            Hãng hàng không
          </p>
          <h2 className="text-4xl md:text-5xl font-black text-gray-900 leading-tight">
            Tìm Hãng Hàng Không Hàng Đầu
          </h2>
        </div>

        {/* Airline Logos Grid */}
        <div className="grid grid-cols-2 md:grid-cols-4 gap-8">
          {airlines.map((airline, idx) => (
            <motion.div 
              key={idx}
              initial={{ opacity: 0, scale: 0.9 }}
              whileInView={{ opacity: 1, scale: 1 }}
              transition={{ delay: idx * 0.1 }}
              className="bg-white rounded-3xl p-8 flex flex-col items-center justify-center gap-4 shadow-[0_10px_30px_rgba(0,0,0,0.03)] border border-gray-50 hover:shadow-xl transition-all group cursor-pointer"
            >
              <div className="w-16 h-16 rounded-full bg-gray-50 p-2 overflow-hidden flex items-center justify-center group-hover:scale-110 transition-transform">
                <img src={airline.logo} alt={airline.name} className="w-full h-auto object-contain" />
              </div>
              <span className="font-extrabold text-gray-900 text-center leading-tight">{airline.name}</span>
            </motion.div>
          ))}
        </div>

      </div>
    </section>
  );
};

export default FlightDealsThree;
