import React, { useEffect, useState } from 'react';
import Navbar from '../../home/components/Navbar';
import Footer from '../../home/components/Footer';
import {
  ChevronRight,
  User,
  Tag,
  ArrowRight,
  ArrowLeft,
} from 'lucide-react';
import { motion } from 'framer-motion';
import { Link } from 'react-router-dom';
import { listPublicCmsPosts } from '../../../services/cmsService';
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

const BlogGridPage = () => {
  const [currentPage, setCurrentPage] = useState(1);
  const [payload, setPayload] = useState({ items: [], total: 0, seo: null });
  const [loading, setLoading] = useState(true);

  useCmsSeoMeta(payload.seo, 'Blog du lich');

  useEffect(() => {
    let mounted = true;

    async function loadPosts() {
      setLoading(true);
      try {
        const response = await listPublicCmsPosts({ page: currentPage, pageSize: 6 });
        if (mounted) {
          setPayload({
            items: response.items || [],
            total: response.total || 0,
            seo: response.seo || null,
          });
        }
      } catch {
        if (mounted) {
          setPayload({ items: [], total: 0, seo: null });
        }
      } finally {
        if (mounted) {
          setLoading(false);
        }
      }
    }

    loadPosts();

    return () => {
      mounted = false;
    };
  }, [currentPage]);

  const totalPages = Math.max(1, Math.ceil((payload.total || 0) / 6));

  const paginate = (pageNumber) => {
    setCurrentPage(pageNumber);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  return (
    <div className="min-h-screen bg-white flex flex-col font-sans">
      <Navbar />

      <section className="relative h-[400px] flex items-center justify-center overflow-hidden">
        <div className="absolute inset-0 z-0">
          <img
            src="https://images.unsplash.com/photo-1499750310107-5fef28a66643?q=80&w=2070"
            alt="Blog"
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
            Blog Dạng Lưới
          </motion.h1>

          <motion.div
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.2 }}
            className="inline-flex items-center gap-4 bg-white/20 backdrop-blur-md px-10 py-4 rounded-full border border-white/30 text-lg font-bold"
          >
            <Link to="/" className="text-white hover:text-[#1EB4D4] transition-colors">Trang chủ</Link>
            <ChevronRight size={20} className="text-[#1EB4D4]" />
            <span className="text-white">Blog dạng lưới</span>
          </motion.div>
        </div>
      </section>

      <section className="py-24 bg-white">
        <div className="container mx-auto px-4 md:px-12 lg:px-24">
          <div className="relative min-h-[800px]">
            {loading ? (
              <div className="rounded-[2rem] border border-gray-100 bg-gray-50 px-8 py-10 text-center text-sm font-bold text-gray-400">
                Đang tải bài viết...
              </div>
            ) : payload.items.length === 0 ? (
              <div className="rounded-[2rem] border border-gray-100 bg-gray-50 px-8 py-10 text-center text-sm font-bold text-gray-400">
                Chưa có bài viết nào được publish.
              </div>
            ) : (
              <motion.div
                key={currentPage}
                initial={{ opacity: 0, x: 20 }}
                animate={{ opacity: 1, x: 0 }}
                transition={{ duration: 0.5 }}
                className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-10"
              >
                {payload.items.map((post) => {
                  const badge = toDateBadge(post.publishedAt);
                  return (
                    <div key={post.id} className="group relative">
                      <div className="relative overflow-hidden rounded-[2.5rem] mb-6">
                        <img
                          src={post.coverImageUrl || 'https://images.unsplash.com/photo-1469474968028-56623f02e42e?q=80&w=2074'}
                          alt={post.title}
                          className="w-full h-[300px] object-cover transition-transform duration-700 group-hover:scale-110"
                        />
                        <div className="absolute top-6 left-6 bg-white rounded-2xl overflow-hidden shadow-xl text-center min-w-[65px] z-20">
                          <div className="bg-[#1EB4D4] text-white py-2 px-3 text-xl font-black leading-none">{badge.day}</div>
                          <div className="py-2 px-3 text-[10px] font-black uppercase text-gray-500 tracking-wider">{badge.month}</div>
                        </div>
                      </div>

                      <div className="bg-white rounded-[2rem] p-8 shadow-sm border border-gray-50 group-hover:shadow-2xl group-hover:shadow-gray-200/50 transition-all duration-500 relative -mt-20 mx-6 z-10">
                        <div className="flex items-center gap-6 mb-4 text-gray-400 font-bold text-xs uppercase tracking-wider">
                          <div className="flex items-center gap-2">
                            <User size={14} className="text-[#1EB4D4]" />
                            <span>{post.authorName || 'Admin'}</span>
                          </div>
                          <div className="flex items-center gap-2">
                            <Tag size={14} className="text-[#1EB4D4]" />
                            <span>{post.primaryCategoryName || 'Tin tức'}</span>
                          </div>
                        </div>

                        <h3 className="text-2xl font-black text-gray-900 mb-6 leading-tight group-hover:text-[#1EB4D4] transition-colors line-clamp-2">
                          {post.title}
                        </h3>

                        <Link to={`/blog/${post.slug}`} className="flex items-center gap-2 text-gray-900 font-black uppercase text-xs tracking-widest hover:text-[#1EB4D4] transition-all group/btn">
                          Xem thêm
                          <ArrowRight size={14} className="group-hover/btn:translate-x-2 transition-transform duration-300" />
                        </Link>
                      </div>
                    </div>
                  );
                })}
              </motion.div>
            )}
          </div>

          <div className="mt-20 flex justify-center items-center gap-4">
            <button
              disabled={currentPage === 1}
              onClick={() => paginate(currentPage - 1)}
              className={`w-12 h-12 rounded-full flex items-center justify-center transition-all ${
                currentPage === 1 ? 'bg-gray-50 text-gray-200' : 'bg-gray-50 text-gray-400 hover:bg-[#1EB4D4] hover:text-white'
              }`}
            >
              <ArrowLeft size={20} />
            </button>

            <div className="flex gap-2">
              {Array.from({ length: totalPages }).map((_, index) => (
                <button
                  key={index + 1}
                  onClick={() => paginate(index + 1)}
                  className={`w-12 h-12 rounded-full font-black text-sm transition-all ${
                    currentPage === index + 1
                      ? 'bg-[#1EB4D4] text-white shadow-xl shadow-[#1EB4D4]/30'
                      : 'bg-gray-50 text-gray-400 hover:bg-[#1EB4D4]/10 hover:text-[#1EB4D4]'
                  }`}
                >
                  {String(index + 1).padStart(2, '0')}
                </button>
              ))}
            </div>

            <button
              disabled={currentPage === totalPages}
              onClick={() => paginate(currentPage + 1)}
              className={`w-12 h-12 rounded-full flex items-center justify-center transition-all ${
                currentPage === totalPages ? 'bg-gray-50 text-gray-200' : 'bg-gray-50 text-gray-400 hover:bg-[#1EB4D4] hover:text-white'
              }`}
            >
              <ArrowRight size={20} />
            </button>
          </div>
        </div>
      </section>

      <Footer />
    </div>
  );
};

export default BlogGridPage;
