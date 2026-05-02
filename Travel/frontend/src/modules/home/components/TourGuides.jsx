import React from 'react';
import { Share2 } from 'lucide-react';
import { motion } from 'framer-motion';
import { Link } from 'react-router-dom';
import nav2 from '../../../assets/nav2.png';

const guides = [
  {
    id: 1,
    name: "Darlene Robertson",
    role: "Hướng dẫn viên du lịch",
    img: "https://images.unsplash.com/photo-1544005313-94ddf0286df2?q=80&w=1976&auto=format&fit=crop",
    bgColor: "bg-[#1EB4D4]/10"
  },
  {
    id: 2,
    name: "Leslie Alexander",
    role: "Hướng dẫn viên du lịch",
    img: "https://images.unsplash.com/photo-1506794778202-cad84cf45f1d?q=80&w=1974&auto=format&fit=crop",
    bgColor: "bg-red-50"
  },
  {
    id: 3,
    name: "Ralph Edwards",
    role: "Hướng dẫn viên du lịch",
    img: "https://images.unsplash.com/photo-1554151228-14d9def656e4?q=80&w=1972&auto=format&fit=crop",
    bgColor: "bg-orange-50"
  },
  {
    id: 4,
    name: "Kathryn Murphy",
    role: "Hướng dẫn viên du lịch",
    img: "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?q=80&w=1974&auto=format&fit=crop",
    bgColor: "bg-blue-50"
  }
];

const TourGuides = () => {
  return (
    <section className="py-24 bg-white relative overflow-hidden">
      
      {/* Decorative Pulsing Dot */}
      <div className="absolute top-10 left-[10%] hidden lg:block">
        <div className="relative">
          <div className="w-1 h-1 bg-[#1EB4D4] rounded-full"></div>
          <motion.div 
            animate={{ scale: [1, 2, 1], opacity: [0.5, 0, 0.5] }}
            transition={{ duration: 2, repeat: Infinity }}
            className="absolute -top-1.5 -left-1.5 w-4 h-4 border border-[#1EB4D4] rounded-full"
          />
        </div>
      </div>

      {/* Decorative Van Image (Bottom Right) */}
      <div className="absolute bottom-[-50px] right-[-50px] opacity-100 pointer-events-none z-10 hidden xl:block">
         <motion.img 
           animate={{ rotate: [0, 2, 0], y: [0, -5, 0] }}
           transition={{ duration: 5, repeat: Infinity, ease: "easeInOut" }}
           src={nav2}
           alt="decor van" 
           className="w-full max-w-sm"
         />
      </div>

      <div className="container mx-auto px-4 md:px-12 lg:px-24 relative z-20">
        
        {/* Header */}
        <div className="text-center mb-16">
          <p className="text-[#1EB4D4] text-xl font-medium mb-3 italic tracking-wider" style={{ fontFamily: "'Kalam', cursive" }}>
            Gặp gỡ hướng dẫn viên
          </p>
          <h2 className="text-4xl md:text-5xl font-black text-gray-900">
            Hướng dẫn viên du lịch
          </h2>
        </div>

        {/* Guides Grid */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-8">
          {guides.map((guide, idx) => (
            <motion.div 
              key={guide.id}
              initial={{ opacity: 0, y: 30 }}
              whileInView={{ opacity: 1, y: 0 }}
              transition={{ delay: idx * 0.1 }}
              className="relative group h-[500px]"
            >
              {/* Main Card Container */}
              <div className={`w-full h-[400px] ${guide.bgColor} rounded-[2.5rem] overflow-hidden relative transition-all duration-500 group-hover:-translate-y-2`}>
                <img 
                  src={guide.img} 
                  alt={guide.name} 
                  className="w-full h-full object-cover mix-blend-multiply opacity-90 group-hover:scale-110 transition-transform duration-700" 
                />
              </div>

              {/* Info Box */}
              <div className="absolute bottom-10 left-6 right-6 bg-white rounded-3xl p-6 shadow-xl border border-gray-50 z-20 transition-all duration-300 group-hover:shadow-2xl">
                 <Link to="/team/details">
                    <h4 className="text-xl font-black text-gray-900 mb-1 hover:text-[#1EB4D4] transition-colors">{guide.name}</h4>
                 </Link>
                 <p className="text-sm font-bold text-gray-400">{guide.role}</p>
                 
                 {/* Share Button */}
                 <button className="absolute -top-4 right-6 w-10 h-10 bg-[#1EB4D4] text-white rounded-full flex items-center justify-center shadow-lg hover:bg-gray-900 transition-colors">
                   <Share2 size={16} />
                 </button>
              </div>

              {/* Decorative accent behind info box */}
              <div className="absolute bottom-6 left-10 right-10 h-10 bg-[#1EB4D4]/5 rounded-full blur-xl -z-10"></div>
            </motion.div>
          ))}
        </div>

      </div>
    </section>
  );
};

export default TourGuides;
