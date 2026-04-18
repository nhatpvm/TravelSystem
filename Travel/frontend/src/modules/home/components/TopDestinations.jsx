import React, { useRef, useState, useEffect } from 'react';
import { motion, useScroll, useSpring } from 'framer-motion';
import { ArrowRight, ArrowLeft } from 'lucide-react';

const destinations = [
  {
    id: 1,
    name: "New Zealand",
    category: "Khách sạn 1",
    image: "https://images.unsplash.com/photo-1469474968028-56623f02e42e?q=80&w=800",
  },
  {
    id: 2,
    name: "Rừng Amazon",
    category: "Khách sạn 1",
    image: "https://images.unsplash.com/photo-1518709268805-4e9042af9f23?q=80&w=800",
  },
  {
    id: 3,
    name: "Venice, Ý",
    category: "Khách sạn 1",
    image: "https://images.unsplash.com/photo-1514890547357-a9ee2887ad85?q=80&w=800",
  },
  {
    id: 4,
    name: "Vạn Lý Trường Thành",
    category: "Khách sạn 1",
    image: "https://images.unsplash.com/photo-1508804185872-d7badad00f7d?q=80&w=800",
  },
  {
    id: 5,
    name: "Paris, Pháp",
    category: "Khách sạn 2",
    image: "https://images.unsplash.com/photo-1502602898657-3e91760cbb34?q=80&w=800",
  },
  {
    id: 6,
    name: "Dubai, UAE",
    category: "Khách sạn 3",
    image: "https://images.unsplash.com/photo-1512453979798-5ea266f8880c?q=80&w=800",
  },
];

const TopDestinations = () => {
  const scrollRef = useRef(null);
  const [isPaused, setIsPaused] = useState(false);

  const scroll = (direction) => {
    const { current } = scrollRef;
    if (current) {
      const scrollAmount = 400;
      const maxScroll = current.scrollWidth - current.clientWidth;
      
      if (direction === 'right' && current.scrollLeft >= maxScroll - 10) {
        current.scrollTo({ left: 0, behavior: 'smooth' });
      } else {
        current.scrollBy({
          left: direction === 'left' ? -scrollAmount : scrollAmount,
          behavior: 'smooth'
        });
      }
    }
  };

  useEffect(() => {
    let interval;
    if (!isPaused) {
      interval = setInterval(() => {
        scroll('right');
      }, 3000); // Tự động lướt mỗi 3 giây
    }
    return () => clearInterval(interval);
  }, [isPaused]);

  return (
    <section className="py-24 bg-white overflow-hidden">
      <div className="container mx-auto px-4 md:px-12 lg:px-24">
        {/* Header Section */}
        <div className="flex flex-col md:flex-row justify-between items-end mb-16 gap-8 text-black">
          <div>
            <p 
              className="text-[#1EB4D4] text-xl font-medium mb-4 italic"
              style={{ fontFamily: "'Kalam', cursive" }}
            >
              Điểm đến của chúng tôi
            </p>
            <h2 className="text-4xl md:text-5xl font-black text-gray-900 tracking-tight leading-tight max-w-xl">
              Những địa điểm hàng đầu <br /> dành riêng cho bạn
            </h2>
          </div>
          
          <div className="flex gap-4">
            <button 
              onClick={() => scroll('left')}
              className="w-14 h-14 rounded-full border border-gray-100 flex items-center justify-center text-gray-900 hover:bg-[#1EB4D4] hover:text-white transition-all shadow-sm"
            >
              <ArrowLeft size={24} />
            </button>
            <button 
              onClick={() => scroll('right')}
              className="w-14 h-14 rounded-full border border-gray-100 flex items-center justify-center text-gray-900 hover:bg-[#1EB4D4] hover:text-white transition-all shadow-sm"
            >
              <ArrowRight size={24} />
            </button>
          </div>
        </div>

        {/* Categories / Property Slider */}
        <div 
          ref={scrollRef}
          onMouseEnter={() => setIsPaused(true)}
          onMouseLeave={() => setIsPaused(false)}
          className="flex gap-8 overflow-x-auto pb-10 scrollbar-hide no-scrollbar snap-x snap-mandatory"
          style={{ scrollbarWidth: 'none', msOverflowStyle: 'none' }}
        >
          {destinations.map((dest, index) => (
            <motion.div
              key={dest.id}
              initial={{ opacity: 0, scale: 0.9 }}
              whileInView={{ opacity: 1, scale: 1 }}
              viewport={{ once: true }}
              className="min-w-[300px] md:min-w-[350px] lg:min-w-[400px] group relative h-[500px] rounded-[2.5rem] overflow-hidden cursor-pointer snap-start"
            >
              {/* Background Image */}
              <img 
                src={dest.image} 
                alt={dest.name} 
                className="w-full h-full object-cover transition-transform duration-700 group-hover:scale-110"
              />
              
              {/* Dark Overlay */}
              <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-black/20 to-transparent"></div>

              {/* Text Content */}
              <div className="absolute bottom-10 left-10 right-10 flex justify-between items-end transition-all duration-500 transform group-hover:-translate-y-2">
                <div>
                  <h4 className="text-2xl font-black text-white mb-2 leading-tight">
                    {dest.name}
                  </h4>
                  <p className="text-white/70 text-sm font-bold uppercase tracking-widest">
                    {dest.category}
                  </p>
                </div>

                {/* Arrow Button */}
                <div className="w-12 h-12 bg-white rounded-full flex items-center justify-center text-gray-900 transform scale-0 group-hover:scale-100 transition-all duration-300 shadow-xl">
                  <ArrowRight size={20} />
                </div>
              </div>
            </motion.div>
          ))}
        </div>
      </div>
    </section>
  );
};

export default TopDestinations;
