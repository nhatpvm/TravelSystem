import React, { useEffect, useState } from 'react';
import Navbar from '../../home/components/Navbar';
import Footer from '../../home/components/Footer';
import {
  ChevronRight,
  User,
  MessageSquare,
  Search,
  ArrowRight,
  ArrowLeft,
  Calendar,
} from 'lucide-react';
import { motion } from 'framer-motion';
import { Link } from 'react-router-dom';
import {
  listPublicCmsCategories,
  listPublicCmsPosts,
  listPublicCmsTags,
} from '../../../services/cmsService';
import useCmsSeoMeta from '../hooks/useCmsSeoMeta';

function toDateBadge(value) {
  if (!value) {
    return { day: '--', month: '---' };
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return { day: '--', month: '---' };
  }

  return {
    day: new Intl.DateTimeFormat('vi-VN', { day: '2-digit' }).format(date),
    month: `T${new Intl.DateTimeFormat('vi-VN', { month: '2-digit' }).format(date)}`,
  };
}

const BlogClassicPage = () => {
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [payload, setPayload] = useState({ items: [], total: 0, seo: null });
  const [categories, setCategories] = useState([]);
  const [tags, setTags] = useState([]);

  useCmsSeoMeta(payload.seo, 'Blog co dien');

  useEffect(() => {
    let mounted = true;

    async function loadData() {
      const [postsResponse, categoriesResponse, tagsResponse] = await Promise.all([
        listPublicCmsPosts({ page, pageSize: 3, q: search || undefined }).catch(() => ({ items: [], total: 0, seo: null })),
        listPublicCmsCategories().catch(() => ({ items: [] })),
        listPublicCmsTags().catch(() => ({ items: [] })),
      ]);

      if (!mounted) {
        return;
      }

      setPayload({
        items: postsResponse.items || [],
        total: postsResponse.total || 0,
        seo: postsResponse.seo || null,
      });
      setCategories(categoriesResponse.items || []);
      setTags(tagsResponse.items || []);
    }

    loadData();

    return () => {
      mounted = false;
    };
  }, [page, search]);

  const totalPages = Math.max(1, Math.ceil((payload.total || 0) / 3));

  return (
    <div className="min-h-screen bg-white flex flex-col font-sans">
      <Navbar />

      <section className="relative h-[400px] flex items-center justify-center overflow-hidden">
        <div className="absolute inset-0 z-0">
          <img
            src="https://images.unsplash.com/photo-1516483638261-f4dbaf036963?q=80&w=2070"
            alt="Blog Classic"
            className="w-full h-full object-cover"
          />
          <div className="absolute inset-0 bg-black/40"></div>
        </div>

        <div className="container mx-auto px-4 relative z-10 text-center text-white">
          <motion.h1
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            className="text-5xl md:text-7xl font-black mb-8 tracking-tighter"
          >
            Blog Cổ Điển
          </motion.h1>

          <motion.div
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.2 }}
            className="inline-flex items-center gap-4 bg-white/20 backdrop-blur-md px-10 py-4 rounded-full border border-white/30 text-lg font-bold"
          >
            <Link to="/" className="text-white hover:text-[#1EB4D4] transition-colors">Trang chủ</Link>
            <ChevronRight size={20} className="text-[#1EB4D4]" />
            <span className="text-white">Blog cổ điển</span>
          </motion.div>
        </div>
      </section>

      <section className="py-24 bg-white">
        <div className="container mx-auto px-4 md:px-12 lg:px-24">
          <div className="flex flex-col lg:flex-row gap-12">
            <div className="flex-1 space-y-12">
              {payload.items.map((post) => {
                const badge = toDateBadge(post.publishedAt);
                return (
                  <motion.div
                    key={post.id}
                    initial={{ opacity: 0, y: 30 }}
                    whileInView={{ opacity: 1, y: 0 }}
                    className="bg-white rounded-[2rem] border border-gray-100 p-6 md:p-8 shadow-sm hover:shadow-xl transition-all duration-500 group"
                  >
                    <div className="relative rounded-[1.5rem] overflow-hidden mb-8 aspect-[16/9]">
                      <img src={post.coverImageUrl || 'https://images.unsplash.com/photo-1507525428034-b723cf961d3e?q=80&w=2073'} alt={post.title} className="w-full h-full object-cover transition-transform duration-700 group-hover:scale-105" />
                      <div className="absolute top-6 left-6 bg-[#1EB4D4] text-white px-5 py-3 rounded-2xl text-center shadow-xl">
                        <p className="text-xl font-black leading-none">{badge.day}</p>
                        <p className="text-[10px] font-black uppercase tracking-widest mt-1">{badge.month}</p>
                      </div>
                    </div>

                    <div className="flex items-center gap-8 mb-6 text-gray-400 font-bold text-xs uppercase tracking-wider">
                      <div className="flex items-center gap-2">
                        <User size={16} className="text-[#1EB4D4]" />
                        <span>{post.authorName || 'Admin'}</span>
                      </div>
                      <div className="flex items-center gap-2">
                        <MessageSquare size={16} className="text-[#1EB4D4]" />
                        <span>{post.readingTimeMinutes || 1} phút đọc</span>
                      </div>
                    </div>

                    <h2 className="text-3xl font-black text-gray-900 mb-6 group-hover:text-[#1EB4D4] transition-colors">
                      {post.title}
                    </h2>
                    <p className="text-gray-400 font-medium leading-[1.8] mb-8">
                      {post.summary || 'Bài viết đang được xuất bản từ CMS và sẵn sàng cho chiến dịch SEO/marketing.'}
                    </p>

                    <Link to={`/blog/${post.slug}`} className="px-8 py-4 rounded-2xl font-black text-sm uppercase tracking-widest transition-all flex items-center gap-2 bg-[#1EB4D4] text-white hover:bg-gray-900 w-fit">
                      Xem thêm <ArrowRight size={18} />
                    </Link>
                  </motion.div>
                );
              })}

              <div className="pt-10 flex justify-center items-center gap-4">
                <button
                  disabled={page === 1}
                  onClick={() => setPage((current) => Math.max(1, current - 1))}
                  className="w-12 h-12 rounded-full bg-gray-50 flex items-center justify-center text-gray-400 hover:bg-[#1EB4D4] hover:text-white transition-all disabled:opacity-40"
                >
                  <ArrowLeft size={20} />
                </button>
                <div className="flex gap-2">
                  {Array.from({ length: totalPages }).map((_, index) => (
                    <button
                      key={index + 1}
                      onClick={() => setPage(index + 1)}
                      className={`w-12 h-12 rounded-full font-black text-sm transition-all ${
                        page === index + 1
                          ? 'bg-[#1EB4D4] text-white shadow-xl shadow-[#1EB4D4]/30'
                          : 'bg-gray-50 text-gray-400 hover:bg-[#1EB4D4]/10 hover:text-[#1EB4D4]'
                      }`}
                    >
                      {String(index + 1).padStart(2, '0')}
                    </button>
                  ))}
                </div>
                <button
                  disabled={page === totalPages}
                  onClick={() => setPage((current) => Math.min(totalPages, current + 1))}
                  className="w-12 h-12 rounded-full bg-gray-50 flex items-center justify-center text-gray-400 hover:bg-[#1EB4D4] hover:text-white transition-all disabled:opacity-40"
                >
                  <ArrowRight size={20} />
                </button>
              </div>
            </div>

            <div className="w-full lg:w-[380px] space-y-12">
              <div className="bg-white border border-gray-100 rounded-[2rem] p-8 shadow-sm">
                <h3 className="text-xl font-black text-gray-900 mb-6">Tìm kiếm</h3>
                <div className="relative">
                  <input value={search} onChange={(event) => setSearch(event.target.value)} type="text" placeholder="Tìm kiếm tại đây" className="w-full bg-[#F8FBFB] border-0 py-4 px-6 pr-14 rounded-xl font-bold text-gray-800 outline-none focus:ring-2 focus:ring-[#1EB4D4]/20" />
                  <button className="absolute right-2 top-2 bottom-2 w-10 bg-[#1EB4D4] rounded-lg flex items-center justify-center text-white hover:bg-gray-900 transition-colors">
                    <Search size={18} />
                  </button>
                </div>
              </div>

              <div className="bg-white border border-gray-100 rounded-[2rem] p-8 shadow-sm">
                <h3 className="text-xl font-black text-gray-900 mb-8 border-b border-gray-50 pb-4">Danh mục</h3>
                <div className="space-y-4">
                  {categories.map((category, index) => (
                    <Link key={category.id} to={`/blog/category/${category.slug}`} className="flex flex-col">
                      <div className="flex justify-between items-center group cursor-pointer">
                        <span className="text-gray-500 font-bold group-hover:text-[#1EB4D4] transition-colors">{category.name}</span>
                        <span className="text-gray-400 text-sm font-black">({category.postCount})</span>
                      </div>
                      {index !== categories.length - 1 && (
                        <div className="h-px w-full bg-gray-50 mt-4 border-b border-dashed border-gray-100"></div>
                      )}
                    </Link>
                  ))}
                </div>
              </div>

              <div className="bg-white border border-gray-100 rounded-[2rem] p-8 shadow-sm">
                <h3 className="text-xl font-black text-gray-900 mb-8 border-b border-gray-50 pb-4">Bài viết gần đây</h3>
                <div className="space-y-8">
                  {payload.items.slice(0, 3).map((post) => (
                    <Link key={post.id} to={`/blog/${post.slug}`} className="flex gap-4 group cursor-pointer">
                      <div className="w-20 h-20 rounded-2xl overflow-hidden shrink-0">
                        <img src={post.coverImageUrl || 'https://images.unsplash.com/photo-1502680390469-be75c86b636f?q=80&w=150'} alt={post.title} className="w-full h-full object-cover group-hover:scale-110 transition-transform duration-500" />
                      </div>
                      <div>
                        <div className="flex items-center gap-2 text-[#1EB4D4] text-[10px] font-black uppercase mb-1">
                          <Calendar size={12} />
                          <span>{new Intl.DateTimeFormat('vi-VN').format(new Date(post.publishedAt || 0))}</span>
                        </div>
                        <h4 className="text-sm font-black text-gray-900 group-hover:text-[#1EB4D4] transition-colors leading-tight">
                          {post.title}
                        </h4>
                      </div>
                    </Link>
                  ))}
                </div>
              </div>

              <div className="bg-white border border-gray-100 rounded-[2rem] p-8 shadow-sm">
                <h3 className="text-xl font-black text-gray-900 mb-8 border-b border-gray-50 pb-4">Từ khóa</h3>
                <div className="flex flex-wrap gap-3">
                  {tags.map((tag) => (
                    <Link key={tag.id} to={`/blog/tag/${tag.slug}`} className="px-5 py-2 rounded-lg bg-white border border-gray-100 text-gray-500 text-xs font-bold hover:bg-[#1EB4D4] hover:text-white hover:border-[#1EB4D4] transition-all">
                      {tag.name}
                    </Link>
                  ))}
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      <Footer />
    </div>
  );
};

export default BlogClassicPage;
