import React from 'react';
import { Star, Quote } from 'lucide-react';
import { motion } from 'framer-motion';
import nav3 from '../../../assets/nav3.png';

const testimonials = [
  {
    id: 1,
    name: "Kristin Watson",
    role: "Thiết kế Web",
    image: "https://images.unsplash.com/photo-1494790108377-be9c29b29330?q=80&w=1974&auto=format&fit=crop",
    feedback: "Praesent ut lacus a velit tincidunt aliquam a eget urna. Sed ullamcorper tristique nisl at pharetra turpis accumsan et etiam eu sollicitudin eros. In imperdiet accumsan.",
    stars: 4
  },
  {
    id: 2,
    name: "Wade Warren",
    role: "Trưởng phòng Kinh doanh",
    image: "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?q=80&w=1974&auto=format&fit=crop",
    feedback: "Praesent ut lacus a velit tincidunt aliquam a eget urna. Sed ullamcorper tristique nisl at pharetra turpis accumsan et etiam eu sollicitudin eros. In imperdiet accumsan.",
    stars: 5
  },
  {
    id: 3,
    name: "Brooklyn Simmons",
    role: "President Of Sales",
    image: "https://images.unsplash.com/photo-1534528741775-53994a69daeb?q=80&w=1964&auto=format&fit=crop",
    feedback: "Praesent ut lacus a velit tincidunt aliquam a eget urna. Sed ullamcorper tristique nisl at pharetra turpis accumsan et etiam eu sollicitudin eros. In imperdiet accumsan.",
    stars: 4
  }
];

const Testimonials = () => {
  return (
    <section className="py-24 bg-white relative overflow-hidden">
      {/* Background Decorative Element */}
      <div className="absolute bottom-0 right-0 opacity-100 pointer-events-none z-10 hidden xl:block translate-x-20 translate-y-20">
         <img 
           src={nav3}
           alt="decor" 
           className="w-full max-w-[500px]"
         />
      </div>

      <div className="container mx-auto px-4 md:px-12 lg:px-24 relative z-20">
        
        {/* Header */}
        <div className="text-center mb-16 relative">
          <motion.p 
            initial={{ opacity: 0, y: 10 }}
            whileInView={{ opacity: 1, y: 0 }}
            className="text-[#1EB4D4] text-xl font-medium mb-3 italic tracking-wider"
            style={{ fontFamily: "'Kalam', cursive" }}
          >
            Đánh giá
          </motion.p>
          <motion.h2 
            initial={{ opacity: 0, y: 10 }}
            whileInView={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.1 }}
            className="text-4xl md:text-5xl lg:text-5xl font-black text-gray-900"
          >
            Phản hồi từ khách hàng
          </motion.h2>
          
          {/* Pulsing dot line decoration above first card (approximate position) */}
          <div className="absolute left-[20%] top-[140px] hidden lg:block w-32 h-[1px] bg-gray-100">
             <motion.div 
               animate={{ scale: [1, 1.5, 1], opacity: [1, 0.5, 1] }}
               transition={{ duration: 2, repeat: Infinity }}
               className="absolute -top-1 left-0 w-2 h-2 bg-[#1EB4D4] rounded-full"
             />
          </div>
        </div>

        {/* Testimonials Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8 mb-16">
          {testimonials.map((t, idx) => (
            <motion.div 
              key={t.id}
              initial={{ opacity: 0, y: 30 }}
              whileInView={{ opacity: 1, y: 0 }}
              transition={{ delay: idx * 0.1 }}
              className="bg-white border border-gray-100 rounded-[2.5rem] p-10 shadow-xl hover:shadow-2xl transition-all duration-500 group relative"
            >
              {/* Stars */}
              <div className="flex gap-1 mb-6">
                {[...Array(5)].map((_, i) => (
                  <Star 
                    key={i} 
                    size={16} 
                    fill={i < t.stars ? "#FFB700" : "none"} 
                    className={i < t.stars ? "text-[#FFB700]" : "text-gray-200"} 
                  />
                ))}
              </div>

              {/* Feedback */}
              <p className="text-gray-500 font-medium leading-[1.8] mb-10 text-[15px]">
                "{t.feedback}"
              </p>

              {/* User Info */}
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-4">
                  <div className="w-14 h-14 rounded-full overflow-hidden border-2 border-white shadow-md">
                    <img src={t.image} alt={t.name} className="w-full h-full object-cover" />
                  </div>
                  <div>
                    <h4 className="font-black text-gray-900 text-lg leading-none mb-1">{t.name}</h4>
                    <p className="text-xs font-bold text-gray-400 uppercase tracking-widest">{t.role}</p>
                  </div>
                </div>
                
                {/* Quote Icon */}
                <div className="text-[#1EB4D4]/20 group-hover:text-[#1EB4D4]/40 transition-colors">
                  <Quote size={40} fill="currentColor" />
                </div>
              </div>
            </motion.div>
          ))}
        </div>

        {/* Navigation Buttons */}
        <div className="flex items-center justify-center gap-12 mt-8">
           <button className="text-xs font-black uppercase tracking-[0.2em] text-gray-400 hover:text-[#1EB4D4] transition-colors border-r border-gray-100 pr-12">
             Trước
           </button>
           <button className="text-xs font-black uppercase tracking-[0.2em] text-[#1EB4D4] hover:text-gray-900 transition-colors">
             Tiếp theo
           </button>
        </div>

      </div>
    </section>
  );
};

export default Testimonials;
