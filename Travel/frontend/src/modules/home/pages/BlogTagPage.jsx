import React, { useEffect, useMemo, useState } from 'react';
import { motion } from 'framer-motion';
import { Link, useParams } from 'react-router-dom';
import { Tag, Search, ArrowRight, Clock, User } from 'lucide-react';
import Navbar from '../components/Navbar';
import Footer from '../components/Footer';
import {
  getPublicCmsCategoryPage,
  getPublicCmsTagPage,
  listPublicCmsCategories,
  listPublicCmsTags,
} from '../../../services/cmsService';
import useCmsSeoMeta from '../../blog/hooks/useCmsSeoMeta';

export default function BlogTagPage() {
  const { tag, cat } = useParams();
  const pageType = tag ? 'tag' : 'category';
  const activeSlug = tag || cat || '';
  const [search, setSearch] = useState('');
  const [payload, setPayload] = useState({ items: [], seo: null, total: 0, title: '' });
  const [categories, setCategories] = useState([]);
  const [tags, setTags] = useState([]);

  useCmsSeoMeta(payload.seo, payload.title || 'Tin tức du lịch');

  useEffect(() => {
    let mounted = true;

    async function loadData() {
      const [pageResponse, categoriesResponse, tagsResponse] = await Promise.all([
        pageType === 'tag'
          ? getPublicCmsTagPage(activeSlug, { page: 1, pageSize: 24 }).catch(() => null)
          : getPublicCmsCategoryPage(activeSlug, { page: 1, pageSize: 24 }).catch(() => null),
        listPublicCmsCategories().catch(() => ({ items: [] })),
        listPublicCmsTags().catch(() => ({ items: [] })),
      ]);

      if (!mounted) {
        return;
      }

      setCategories(categoriesResponse.items || []);
      setTags(tagsResponse.items || []);

      if (!pageResponse) {
        setPayload({ items: [], seo: null, total: 0, title: '' });
        return;
      }

      setPayload({
        items: pageResponse.items || [],
        seo: pageResponse.seo || null,
        total: pageResponse.total || 0,
        title: pageType === 'tag' ? pageResponse.tag?.name : pageResponse.category?.name,
      });
    }

    loadData();

    return () => {
      mounted = false;
    };
  }, [activeSlug, pageType]);

  const filtered = useMemo(() => {
    const keyword = search.trim().toLowerCase();
    if (!keyword) {
      return payload.items;
    }

    return payload.items.filter((item) => item.title.toLowerCase().includes(keyword) || String(item.summary || '').toLowerCase().includes(keyword));
  }, [payload.items, search]);

  return (
    <div className="min-h-screen bg-[#F0F4F8]">
      <Navbar />
      <div className="pt-32 pb-24">
        <div className="bg-gradient-to-r from-[#002B7F] to-[#1EB4D4] py-10 px-4 mb-10">
          <div className="container mx-auto max-w-4xl text-center">
            <div className="inline-flex items-center gap-2 px-4 py-2 bg-white/20 rounded-full mb-3">
              <Tag size={14} className="text-white" />
              <span className="text-white/80 text-sm font-bold">
                {pageType === 'tag' ? `#${payload.title || activeSlug}` : payload.title || 'Danh mục'}
              </span>
            </div>
            <h1 className="text-white font-black text-3xl mb-4">Tin tức & Cẩm nang du lịch</h1>
            <div className="flex gap-2 bg-white rounded-2xl p-2 shadow-xl max-w-lg mx-auto">
              <div className="flex-1 flex items-center gap-2 px-3">
                <Search size={16} className="text-slate-400" />
                <input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Tìm bài viết…" className="flex-1 py-2 outline-none text-sm font-bold text-slate-900 bg-transparent" />
              </div>
            </div>
          </div>
        </div>

        <div className="container mx-auto px-4 lg:px-12 max-w-6xl">
          <div className="flex flex-col lg:flex-row gap-8">
            <aside className="lg:w-64 shrink-0 space-y-5">
              <div className="bg-white rounded-2xl p-5 shadow-sm border border-slate-100">
                <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-3">Danh mục</p>
                <div className="space-y-1">
                  {categories.map((category) => (
                    <Link key={category.id} to={`/blog/category/${category.slug}`} className={`block w-full text-left px-3 py-2.5 rounded-xl text-sm font-bold transition-all ${cat === category.slug ? 'bg-slate-900 text-white' : 'text-slate-600 hover:bg-slate-50'}`}>
                      {category.name}
                    </Link>
                  ))}
                </div>
              </div>
              <div className="bg-white rounded-2xl p-5 shadow-sm border border-slate-100">
                <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-3">Thẻ phổ biến</p>
                <div className="flex flex-wrap gap-2">
                  {tags.map((item) => (
                    <Link key={item.id} to={`/blog/tag/${item.slug}`} className={`px-3 py-1.5 rounded-xl text-[10px] font-black uppercase tracking-widest transition-all ${tag === item.slug ? 'bg-[#1EB4D4] text-white' : 'bg-slate-50 text-slate-500 hover:bg-slate-100'}`}>
                      {item.name}
                    </Link>
                  ))}
                </div>
              </div>
            </aside>

            <div className="flex-1">
              <p className="text-slate-500 font-bold text-sm mb-5">{filtered.length} bài viết</p>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                {filtered.map((post, index) => (
                  <motion.div key={post.id} initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: index * 0.06 }} className="group bg-white rounded-[2rem] overflow-hidden shadow-xl shadow-slate-100/60 hover:shadow-2xl transition-all">
                    <div className="relative h-44 overflow-hidden">
                      <img src={post.coverImageUrl || 'https://images.unsplash.com/photo-1559592413-7cea732639f5?auto=format&fit=crop&q=80&w=400'} alt={post.title} className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500" />
                      <div className="absolute inset-0 bg-gradient-to-t from-black/50 to-transparent" />
                      <span className="absolute top-4 left-4 px-3 py-1 bg-white/90 backdrop-blur rounded-xl text-[10px] font-black uppercase text-slate-700">
                        {post.primaryCategoryName || payload.title || 'Tin tức'}
                      </span>
                    </div>
                    <div className="p-5">
                      <h2 className="font-black text-slate-900 leading-tight mb-3 group-hover:text-[#1EB4D4] transition-colors line-clamp-2">{post.title}</h2>
                      <div className="flex items-center gap-3 text-[10px] text-slate-400 font-bold mb-3">
                        <span className="flex items-center gap-1"><User size={10} />{post.authorName || 'Admin'}</span>
                        <span className="flex items-center gap-1"><Clock size={10} />{post.readingTimeMinutes || 1} phút đọc</span>
                      </div>
                      <p className="text-sm text-slate-500 font-medium line-clamp-3 mb-4">{post.summary || 'Bài viết được xuất bản từ CMS.'}</p>
                      <Link to={`/blog/${post.slug}`} className="flex items-center gap-1 text-[#1EB4D4] font-black text-xs uppercase tracking-widest hover:gap-2 transition-all">
                        Đọc tiếp <ArrowRight size={13} />
                      </Link>
                    </div>
                  </motion.div>
                ))}
              </div>
              {filtered.length === 0 && (
                <div className="bg-white rounded-2xl p-10 text-center text-slate-300 border border-slate-100">
                  <p className="font-bold">Không tìm thấy bài viết nào.</p>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
      <Footer />
    </div>
  );
}
