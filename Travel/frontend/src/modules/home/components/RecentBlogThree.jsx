import React from 'react';
import { motion } from 'framer-motion';
import { MoveRight, Calendar, User } from 'lucide-react';

const posts = [
  {
    id: 1,
    image: "https://images.unsplash.com/photo-1506929562872-bb421503ef21?q=80&w=800",
    category: "Du lịch",
    author: "Bởi Admin",
    date: "15 Tháng 3",
    title: "Người Lướt Sóng Đổi Mới Tại Úc",
    excerpt: "Xem thêm"
  },
  {
    id: 2,
    image: "https://images.unsplash.com/photo-1530789253388-582c481c54b0?q=80&w=800",
    category: "Phiêu lưu",
    author: "Bởi Admin",
    date: "18 Tháng 3",
    title: "Trải Nghiệm Thất Vọng Với Xu Hướng Mới",
    excerpt: "Xem thêm"
  },
  {
    id: 3,
    image: "https://images.unsplash.com/photo-1501785888041-af3ef285b470?q=80&w=800",
    category: "Thiên nhiên",
    author: "Bởi Editor",
    date: "19 Tháng 3",
    title: "Lời Khuyên Tốt Nhất Cho Du Lịch Một Mình",
    excerpt: "Xem thêm"
  }
];

const RecentBlogThree = () => {
  return (
    <section className="py-24 bg-white">
      <div className="container mx-auto px-4 md:px-12 lg:px-24">
        {/* Header */}
        <div className="text-center mb-14">
          <p
            className="text-[#1EB4D4] text-lg font-medium mb-3 italic"
            style={{ fontFamily: "'Kalam', cursive" }}
          >
            Blog & Bài viết
          </p>
          <h2 className="text-3xl md:text-4xl lg:text-5xl font-black text-gray-900 leading-tight">
            Bài Viết Blog Gần Đây
          </h2>
        </div>

        {/* Blog Cards */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
          {posts.map((post, idx) => (
            <motion.div
              key={post.id}
              initial={{ opacity: 0, y: 30 }}
              whileInView={{ opacity: 1, y: 0 }}
              transition={{ delay: idx * 0.12 }}
              viewport={{ once: true }}
              className="bg-white rounded-3xl overflow-hidden border border-gray-100 shadow-[0_8px_30px_rgba(0,0,0,0.04)] hover:shadow-xl transition-shadow duration-500 group"
            >
              {/* Image */}
              <div className="relative h-56 overflow-hidden">
                <img
                  src={post.image}
                  alt={post.title}
                  className="w-full h-full object-cover group-hover:scale-110 transition-transform duration-700"
                />
                {/* Category badge */}
                <div className="absolute top-4 left-4">
                  <span className="bg-[#1EB4D4] text-white text-[10px] font-bold uppercase tracking-wider px-4 py-1.5 rounded-full">
                    {post.category}
                  </span>
                </div>
                {/* Author & Date overlay */}
                <div className="absolute bottom-4 left-4 right-4 flex items-center gap-4">
                  <div className="flex items-center gap-1.5 bg-white/90 backdrop-blur-sm rounded-full px-3 py-1.5">
                    <User size={12} className="text-[#1EB4D4]" />
                    <span className="text-xs font-bold text-gray-700">{post.author}</span>
                  </div>
                  <div className="flex items-center gap-1.5 bg-white/90 backdrop-blur-sm rounded-full px-3 py-1.5">
                    <Calendar size={12} className="text-[#1EB4D4]" />
                    <span className="text-xs font-bold text-gray-700">{post.date}</span>
                  </div>
                </div>
              </div>

              {/* Content */}
              <div className="p-6">
                <h3 className="text-lg font-black text-gray-900 mb-4 leading-snug group-hover:text-[#1EB4D4] transition-colors line-clamp-2">
                  {post.title}
                </h3>
                <button className="flex items-center gap-2 text-gray-900 font-bold text-sm hover:text-[#1EB4D4] transition-colors group/btn">
                  Đọc thêm <MoveRight size={16} className="group-hover/btn:translate-x-1 transition-transform" />
                </button>
              </div>
            </motion.div>
          ))}
        </div>
      </div>
    </section>
  );
};

export default RecentBlogThree;
