import React, { useEffect, useMemo, useState } from 'react';
import Navbar from '../../home/components/Navbar';
import Footer from '../../home/components/Footer';
import {
  ChevronRight,
  User,
  MessageSquare,
  Search,
  Tag,
  Calendar,
  Facebook,
  Twitter,
  Linkedin,
  Instagram,
  Reply,
  ArrowRight,
} from 'lucide-react';
import { motion } from 'framer-motion';
import { Link, useLocation, useParams } from 'react-router-dom';
import {
  getPublicCmsPost,
  listPublicCmsCategories,
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

const BlogDetailsPage = () => {
  const { slug: routeSlug } = useParams();
  const location = useLocation();
  const slug = useMemo(() => {
    if (routeSlug && routeSlug !== 'details') {
      return routeSlug;
    }

    const query = new URLSearchParams(location.search);
    return query.get('slug') || '';
  }, [location.search, routeSlug]);
  const [payload, setPayload] = useState(null);
  const [categories, setCategories] = useState([]);
  const activePayload = slug ? payload : null;

  useCmsSeoMeta(activePayload?.seo, activePayload?.post?.title || 'Chi tiết bài viết');

  useEffect(() => {
    if (!slug) {
      return undefined;
    }

    let mounted = true;

    async function loadData() {
      const [postResponse, categoriesResponse] = await Promise.all([
        getPublicCmsPost(slug, { bumpView: true }).catch(() => null),
        listPublicCmsCategories().catch(() => ({ items: [] })),
      ]);

      if (!mounted) {
        return;
      }

      setPayload(postResponse);
      setCategories(categoriesResponse.items || []);
    }

    loadData();

    return () => {
      mounted = false;
    };
  }, [slug]);

  const post = activePayload?.post;
  const badge = toDateBadge(post?.publishedAt);

  return (
    <div className="min-h-screen bg-white flex flex-col font-sans">
      <Navbar />

      <section className="relative h-[400px] flex items-center justify-center overflow-hidden">
        <div className="absolute inset-0 z-0">
          <img
            src={post?.coverImageUrl || 'https://images.unsplash.com/photo-1488646953014-85cb44e25828?q=80&w=2070'}
            alt="Blog Details"
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
            Chi Tiết Bài Viết
          </motion.h1>

          <motion.div
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.2 }}
            className="inline-flex items-center gap-4 bg-white/20 backdrop-blur-md px-10 py-4 rounded-full border border-white/30 text-lg font-bold"
          >
            <Link to="/" className="text-white hover:text-[#1EB4D4] transition-colors">Trang chủ</Link>
            <ChevronRight size={20} className="text-[#1EB4D4]" />
            <Link to="/blog" className="text-white hover:text-[#1EB4D4] transition-colors">Blog</Link>
            <ChevronRight size={20} className="text-[#1EB4D4]" />
            <span className="text-white">Chi tiết</span>
          </motion.div>
        </div>
      </section>

      <section className="py-24 bg-white">
        <div className="container mx-auto px-4 md:px-12 lg:px-24">
          <div className="flex flex-col lg:flex-row gap-12">
            <div className="flex-1">
              {!post ? (
                <div className="rounded-[2rem] border border-gray-100 bg-gray-50 px-8 py-10 text-center text-sm font-bold text-gray-400">
                  Không tìm thấy bài viết hoặc bài chưa được publish.
                </div>
              ) : (
                <article>
                  <div className="relative rounded-[2.5rem] overflow-hidden mb-12">
                    <img src={post.coverImageUrl || 'https://images.unsplash.com/photo-1469474968028-56623f02e42e?q=80&w=2074'} alt={post.title} className="w-full aspect-[16/9] object-cover" />
                    <div className="absolute top-8 left-8 bg-[#1EB4D4] text-white px-6 py-4 rounded-2xl text-center shadow-xl">
                      <p className="text-2xl font-black leading-none">{badge.day}</p>
                      <p className="text-xs font-black uppercase tracking-widest mt-1">{badge.month}</p>
                    </div>
                  </div>

                  <div className="flex flex-wrap items-center gap-8 mb-8 text-gray-400 font-bold text-xs uppercase tracking-wider border-b border-gray-100 pb-8">
                    <div className="flex items-center gap-2">
                      <User size={16} className="text-[#1EB4D4]" />
                      <span>{post.authorName || 'Admin'}</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <MessageSquare size={16} className="text-[#1EB4D4]" />
                      <span>{post.readingTimeMinutes || 1} phút đọc</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <Tag size={16} className="text-[#1EB4D4]" />
                      <span>{post.primaryCategoryName || 'Tin tức'}</span>
                    </div>
                  </div>

                  <h2 className="text-4xl md:text-5xl font-black text-gray-900 mb-8 leading-tight">
                    {post.title}
                  </h2>

                  <div className="text-gray-500 font-medium leading-[1.8] space-y-8 text-lg">
                    {post.summary && <p>{post.summary}</p>}
                    <div dangerouslySetInnerHTML={{ __html: post.contentHtml || `<p>${post.contentMarkdown || ''}</p>` }} />
                  </div>

                  <div className="flex flex-col md:flex-row justify-between items-center gap-8 border-y border-gray-100 py-10 mt-16">
                    <div className="flex gap-4 flex-wrap">
                      {(activePayload.tags || []).map((item) => (
                        <Link key={item.id} to={`/blog/tag/${item.slug}`} className="px-8 py-3 rounded-xl border border-gray-100 text-gray-500 text-xs font-black uppercase tracking-widest hover:bg-[#1EB4D4] hover:text-white transition-all">
                          {item.name}
                        </Link>
                      ))}
                    </div>
                    <div className="flex items-center gap-6">
                      <span className="text-gray-900 font-black text-sm uppercase tracking-widest">Chia sẻ:</span>
                      <div className="flex gap-4">
                        {[Facebook, Twitter, Linkedin, Instagram].map((Icon, index) => (
                          <button key={index} className="text-gray-900 hover:text-[#1EB4D4] transition-colors">
                            <Icon size={18} />
                          </button>
                        ))}
                      </div>
                    </div>
                  </div>
                </article>
              )}

              <div className="mt-24">
                <h3 className="text-3xl font-black text-gray-900 mb-16">02 Bình luận</h3>
                <div className="space-y-12">
                  {[
                    { name: 'Leslie Alexander', img: 'https://images.unsplash.com/photo-1544005313-94ddf0286df2?q=80&w=150' },
                    { name: 'Ralph Edwards', img: 'https://images.unsplash.com/photo-1554151228-14d9def656e4?q=80&w=150' },
                  ].map((comment, index) => (
                    <div key={index} className="flex flex-col md:flex-row gap-8 pb-12 border-b border-gray-50 last:border-0">
                      <div className="w-24 h-24 rounded-full overflow-hidden shrink-0 shadow-lg">
                        <img src={comment.img} alt={comment.name} className="w-full h-full object-cover" />
                      </div>
                      <div className="flex-1">
                        <div className="flex justify-between items-center mb-4">
                          <div>
                            <h4 className="text-xl font-black text-gray-900">{comment.name}</h4>
                            <p className="text-gray-400 text-xs font-bold uppercase tracking-widest mt-1">10 tháng 2, 2024 lúc 19:30</p>
                          </div>
                          <button className="bg-[#1EB4D4] text-white px-5 py-2 rounded-xl text-xs font-black uppercase tracking-widest flex items-center gap-2 hover:bg-gray-900 transition-colors">
                            <Reply size={14} /> Trả lời
                          </button>
                        </div>
                        <p className="text-gray-500 font-medium leading-relaxed">
                          Bài viết rất tuyệt vời và bổ ích. Đây đang là vùng comment tĩnh để giữ nguyên layout chi tiết hiện có.
                        </p>
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              <div className="mt-24">
                <h3 className="text-3xl font-black text-gray-900 mb-12">Để lại bình luận</h3>
                <form className="space-y-8">
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
                    <div className="space-y-3">
                      <label className="text-sm font-black text-gray-900">Tên của bạn*</label>
                      <input type="text" placeholder="Tên của bạn" className="w-full bg-white border border-gray-100 py-5 px-8 rounded-2xl font-bold text-gray-800 outline-none focus:ring-2 focus:ring-[#1EB4D4]/20" />
                    </div>
                    <div className="space-y-3">
                      <label className="text-sm font-black text-gray-900">Email của bạn*</label>
                      <input type="email" placeholder="Email của bạn" className="w-full bg-white border border-gray-100 py-5 px-8 rounded-2xl font-bold text-gray-800 outline-none focus:ring-2 focus:ring-[#1EB4D4]/20" />
                    </div>
                  </div>
                  <div className="space-y-3">
                    <label className="text-sm font-black text-gray-900">Tin nhắn*</label>
                    <textarea rows="6" placeholder="Viết tin nhắn" className="w-full bg-white border border-gray-100 py-5 px-8 rounded-2xl font-bold text-gray-800 outline-none focus:ring-2 focus:ring-[#1EB4D4]/20 resize-none"></textarea>
                  </div>
                  <button className="bg-[#1EB4D4] text-white px-10 py-5 rounded-2xl font-black text-lg shadow-xl shadow-[#1EB4D4]/30 hover:bg-gray-900 transition-all flex items-center gap-3">
                    Gửi bình luận <ArrowRight size={20} />
                  </button>
                </form>
              </div>
            </div>

            <div className="w-full lg:w-[380px] space-y-12">
              <div className="bg-white border border-gray-100 rounded-[2rem] p-8 shadow-sm">
                <h3 className="text-xl font-black text-gray-900 mb-6 text-center md:text-left">Tìm kiếm</h3>
                <div className="relative">
                  <input type="text" placeholder="Tìm kiếm tại đây" className="w-full bg-[#F8FBFB] border-0 py-4 px-6 pr-14 rounded-xl font-bold text-gray-800 outline-none focus:ring-2 focus:ring-[#1EB4D4]/20" />
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
                <h3 className="text-xl font-black text-gray-900 mb-8 border-b border-gray-50 pb-4">Bài viết mới nhất</h3>
                <div className="space-y-8">
                  {(activePayload?.latestPosts || []).map((latest) => (
                    <Link key={latest.id} to={`/blog/${latest.slug}`} className="flex gap-4 group cursor-pointer">
                      <div className="w-20 h-20 rounded-2xl overflow-hidden shrink-0">
                        <img src={latest.coverImageUrl || 'https://images.unsplash.com/photo-1501785888041-af3ef285b470?q=80&w=150'} alt={latest.title} className="w-full h-full object-cover group-hover:scale-110 transition-transform duration-500" />
                      </div>
                      <div>
                        <div className="flex items-center gap-2 text-[#1EB4D4] text-[10px] font-black uppercase mb-1">
                          <Calendar size={12} />
                          <span>{new Intl.DateTimeFormat('vi-VN').format(new Date(latest.publishedAt || 0))}</span>
                        </div>
                        <h4 className="text-sm font-black text-gray-900 group-hover:text-[#1EB4D4] transition-colors leading-tight">
                          {latest.title}
                        </h4>
                      </div>
                    </Link>
                  ))}
                </div>
              </div>

              <div className="bg-white border border-gray-100 rounded-[2rem] p-8 shadow-sm">
                <h3 className="text-xl font-black text-gray-900 mb-8 border-b border-gray-50 pb-4">Từ khóa</h3>
                <div className="flex flex-wrap gap-3">
                  {(activePayload?.tags || []).map((item) => (
                    <Link key={item.id} to={`/blog/tag/${item.slug}`} className="px-5 py-2 rounded-lg bg-white border border-gray-100 text-gray-500 text-xs font-bold hover:bg-[#1EB4D4] hover:text-white hover:border-[#1EB4D4] transition-all">
                      {item.name}
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

export default BlogDetailsPage;
