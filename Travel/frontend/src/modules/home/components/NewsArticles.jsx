import React from 'react';
import { Calendar, MapPin, ArrowRight } from 'lucide-react';
import { motion } from 'framer-motion';

const articles = [
  {
    id: 1,
    type: 'image',
    img: 'https://images.unsplash.com/photo-1464822759023-fed622ff2c3b?q=80&w=2070&auto=format&fit=crop'
  },
  {
    id: 2,
    type: 'text',
    date: '02 tháng 12, 2024',
    location: 'Thành phố New York',
    title: 'Bí quyết để có chuyến du lịch tiết kiệm và an toàn nhất',
    link: '#'
  },
  {
    id: 3,
    type: 'image',
    img: 'https://images.unsplash.com/photo-1476514525535-07fb3b4ae5f1?q=80&w=2070&auto=format&fit=crop'
  },
  {
    id: 4,
    type: 'text',
    date: '02 tháng 12, 2024',
    location: 'Thành phố New York',
    title: 'Top 10 địa điểm không thể bỏ qua tại Đông Nam Á năm nay',
    link: '#'
  },
  {
    id: 5,
    type: 'image',
    img: 'https://images.unsplash.com/photo-1506744038136-46273834b3fb?q=80&w=2070&auto=format&fit=crop'
  },
  {
    id: 6,
    type: 'text',
    date: '02 tháng 12, 2024',
    location: 'Thành phố New York',
    title: 'Hành trình khám phá vẻ đẹp hoang sơ của vùng núi phía Bắc',
    link: '#'
  }
];

const NewsArticles = () => {
  return (
    <section className="py-24 bg-white relative">
      <div className="container mx-auto px-4 md:px-12 lg:px-24">
        
        {/* Header */}
        <div className="text-center mb-16 relative">
             {/* Decorative Pulsing Dot from reference */}
             <div className="absolute top-0 left-[15%] hidden xl:block">
                <div className="relative">
                  <div className="w-1.5 h-1.5 bg-[#1EB4D4] rounded-full"></div>
                  <motion.div 
                    animate={{ scale: [1, 2.5, 1], opacity: [0.6, 0, 0.6] }}
                    transition={{ duration: 2, repeat: Infinity }}
                    className="absolute -top-2 -left-2 w-5.5 h-5.5 border border-[#1EB4D4] rounded-full"
                  />
                </div>
             </div>

          <p className="text-[#1EB4D4] text-xl font-medium mb-3 italic tracking-wider" style={{ fontFamily: "'Kalam', cursive" }}>
            Tin tức & Cập nhật
          </p>
          <h2 className="text-4xl md:text-5xl font-black text-gray-900">
            Tin tức & Bài viết mới nhất
          </h2>
        </div>

        {/* Articles Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {articles.map((item, idx) => (
            <motion.div 
              key={item.id}
              initial={{ opacity: 0, y: 20 }}
              whileInView={{ opacity: 1, y: 0 }}
              transition={{ delay: idx * 0.1 }}
              className="h-[320px] rounded-[2rem] overflow-hidden group"
            >
              {item.type === 'image' ? (
                <div className="w-full h-full relative cursor-pointer">
                  <img src={item.img} alt="Article" className="w-full h-full object-cover transition-transform duration-700 group-hover:scale-110" />
                  <div className="absolute inset-0 bg-black/10 group-hover:bg-black/20 transition-colors"></div>
                </div>
              ) : (
                <div className="w-full h-full bg-[#F8FBFB] p-10 flex flex-col justify-center transition-colors group-hover:bg-[#1EB4D4]/5 border border-gray-50">
                  <div className="flex items-center gap-6 text-gray-400 text-sm mb-6">
                    <div className="flex items-center gap-2">
                      <Calendar size={16} className="text-[#1EB4D4]" />
                      <span className="font-bold">{item.date}</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <MapPin size={16} className="text-[#1EB4D4]" />
                      <span className="font-bold">{item.location}</span>
                    </div>
                  </div>
                  
                  <h3 className="text-2xl font-black text-gray-900 mb-8 leading-tight hover:text-[#1EB4D4] cursor-pointer transition-colors">
                    {item.title}
                  </h3>

                  <a href={item.link} className="flex items-center gap-2 text-[#1EB4D4] font-black uppercase text-sm tracking-widest group/link">
                    Xem thêm 
                    <ArrowRight size={18} className="transition-transform group-hover/link:translate-x-2" />
                  </a>
                </div>
              )}
            </motion.div>
          ))}
        </div>
      </div>
    </section>
  );
};

export default NewsArticles;
