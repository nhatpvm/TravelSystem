import React, { useRef, useState, useEffect } from 'react';
import { motion } from 'framer-motion';
import { MessageCircle, Calendar, ArrowRight, ArrowLeft } from 'lucide-react';
import { Link } from 'react-router-dom';

const articles = [
  {
    id: 1,
    title: "Hướng Dẫn Tuyệt Vời Để Lên Kế Hoạch Cho Kỳ Nghỉ Mơ Ước",
    date: "05 Tháng 9, 2024",
    comments: 0,
    image: "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?q=80&w=800",
    authorImages: [
        "https://randomuser.me/api/portraits/women/1.jpg",
        "https://randomuser.me/api/portraits/men/2.jpg",
        "https://randomuser.me/api/portraits/women/3.jpg"
    ]
  },
  {
    id: 2,
    title: "Những Cuộc Phiêu Lưu Khó Quên: Trải Nghiệm Danh Sách Ước Nguyện",
    date: "05 Tháng 9, 2024",
    comments: 0,
    image: "https://images.unsplash.com/photo-1503220317375-aaad61436b1b?q=80&w=800",
    authorImages: [
        "https://randomuser.me/api/portraits/men/4.jpg",
        "https://randomuser.me/api/portraits/women/5.jpg",
        "https://randomuser.me/api/portraits/men/6.jpg"
    ]
  },
  {
    id: 3,
    title: "Khám Phá Văn Hóa Và Ẩm Thực Cùng Đại Lý Du Lịch",
    date: "05 Tháng 9, 2024",
    comments: 0,
    image: "https://images.unsplash.com/photo-1516483642785-0c47f5990815?q=80&w=800",
    authorImages: [
        "https://randomuser.me/api/portraits/women/7.jpg",
        "https://randomuser.me/api/portraits/men/8.jpg",
        "https://randomuser.me/api/portraits/women/9.jpg"
    ]
  },
  {
    id: 4,
    title: "Những Món Ăn Tốt Nhất Tại Các Điểm Đến Du Lịch",
    date: "05 Tháng 9, 2024",
    comments: 0,
    image: "https://images.unsplash.com/photo-1469854523086-cc02fe5d8800?q=80&w=800",
    authorImages: [
        "https://randomuser.me/api/portraits/men/10.jpg",
        "https://randomuser.me/api/portraits/women/11.jpg",
        "https://randomuser.me/api/portraits/men/12.jpg"
    ]
  },
  {
    id: 5,
    title: "10 Bí Quyết Để Chụp Ảnh Du Lịch Đẹp Như Nhiếp Ảnh Gia",
    date: "08 Tháng 9, 2024",
    comments: 12,
    image: "https://images.unsplash.com/photo-1500530855697-b586d89ba3ee?q=80&w=800",
    authorImages: [
        "https://randomuser.me/api/portraits/women/15.jpg",
        "https://randomuser.me/api/portraits/men/16.jpg"
    ]
  },
  {
    id: 6,
    title: "Hành Trình Khám Phá Những Ngôi Làng Cổ Ở Châu Âu",
    date: "10 Tháng 9, 2024",
    comments: 8,
    image: "https://images.unsplash.com/photo-1467269204594-9661b134dd2b?q=80&w=800",
    authorImages: [
        "https://randomuser.me/api/portraits/men/20.jpg"
    ]
  }
];

const NewsArticlesTwo = () => {
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
      }, 4000); // Tự động trôi mỗi 4 giây
    }
    return () => clearInterval(interval);
  }, [isPaused]);

  return (
    <section className="py-24 bg-white overflow-hidden">
      <div className="container mx-auto px-4 md:px-12 lg:px-24">
        {/* Header with Nav Buttons */}
        <div className="flex flex-col md:flex-row justify-between items-end mb-16 relative">
          <div className="text-left">
            <p className="text-[#1EB4D4] text-xl font-medium mb-3 italic tracking-wider" style={{ fontFamily: "'Kalam', cursive" }}>
                Tin tức & Cập nhật
            </p>
            <h2 className="text-4xl md:text-5xl font-black text-gray-900 tracking-tight">
                Tin Tức & Bài Viết Mới Nhất
            </h2>
          </div>

          <div className="flex gap-4 mt-8 md:mt-0">
            <button 
              onClick={() => scroll('left')}
              className="w-12 h-12 rounded-full border border-gray-100 flex items-center justify-center text-gray-900 hover:bg-[#1EB4D4] hover:text-white hover:border-[#1EB4D4] transition-all shadow-sm"
            >
              <ArrowLeft size={20} />
            </button>
            <button 
              onClick={() => scroll('right')}
              className="w-12 h-12 rounded-full border border-gray-100 flex items-center justify-center text-gray-900 hover:bg-[#1EB4D4] hover:text-white hover:border-[#1EB4D4] transition-all shadow-sm"
            >
              <ArrowRight size={20} />
            </button>
          </div>
        </div>

        {/* Swipeable Container */}
        <div 
          ref={scrollRef}
          onMouseEnter={() => setIsPaused(true)}
          onMouseLeave={() => setIsPaused(false)}
          className="flex gap-8 overflow-x-auto pb-10 scrollbar-hide no-scrollbar snap-x snap-mandatory"
          style={{ scrollbarWidth: 'none', msOverflowStyle: 'none' }}
        >
          {articles.map((item, idx) => (
            <motion.div
              key={item.id}
              initial={{ opacity: 0, scale: 0.9 }}
              whileInView={{ opacity: 1, scale: 1 }}
              viewport={{ once: true }}
              className="min-w-[300px] md:min-w-[350px] lg:min-w-[380px] group bg-white rounded-[2rem] overflow-hidden border border-gray-50 flex flex-col h-full hover:shadow-2xl hover:shadow-[#1EB4D4]/5 transition-all duration-500 snap-start"
            >
              {/* Image Container */}
              <div className="relative h-[240px] overflow-hidden">
                <img 
                  src={item.image} 
                  alt={item.title} 
                  className="w-full h-full object-cover transition-transform duration-1000 group-hover:scale-110"
                />
              </div>

              {/* Content Part */}
              <div className="p-8 flex flex-col flex-grow">
                {/* Meta Info */}
                <div className="flex items-center gap-4 text-gray-400 text-[11px] font-black uppercase tracking-wider mb-4">
                  <div className="flex items-center gap-1.5">
                    <MessageCircle size={14} className="text-[#1EB4D4]" />
                    <span>{item.comments} Bình luận</span>
                  </div>
                  <div className="flex items-center gap-1.5">
                    <Calendar size={14} className="text-[#1EB4D4]" />
                    <span>{item.date}</span>
                  </div>
                </div>

                <h3 className="text-lg font-black text-gray-900 mb-6 leading-snug group-hover:text-[#1EB4D4] transition-colors line-clamp-2">
                  {item.title}
                </h3>

                {/* Footer with Read More and Avatars */}
                <div className="mt-auto pt-6 border-t border-gray-50 flex items-center justify-between">
                  <Link to="#" className="flex items-center gap-2 text-gray-900 group/link">
                    <span className="text-[10px] font-black uppercase tracking-[0.2em]">Xem thêm</span>
                    <ArrowRight size={14} className="text-[#1EB4D4] transition-transform group-hover/link:translate-x-1" />
                  </Link>
                  
                  {/* Avatar Stack */}
                  <div className="flex -space-x-3">
                    {item.authorImages.map((img, i) => (
                        <div key={i} className="w-8 h-8 rounded-full border-2 border-white overflow-hidden shadow-sm">
                            <img src={img} alt="author" className="w-full h-full object-cover" />
                        </div>
                    ))}
                    {item.authorImages.length > 2 && (
                        <div className="w-8 h-8 rounded-full border-2 border-white bg-[#1EB4D4] flex items-center justify-center text-[10px] text-white font-bold shadow-sm">
                            +
                        </div>
                    )}
                  </div>
                </div>
              </div>
            </motion.div>
          ))}
        </div>
      </div>
    </section>
  );
};

export default NewsArticlesTwo;
