import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { ArrowRight, Star } from 'lucide-react';

const ratingCategories = [
  { id: 'services', label: 'Dịch vụ' },
  { id: 'hotel',    label: 'Khách sạn' },
  { id: 'places',   label: 'Địa điểm' },
  { id: 'safety',   label: 'An toàn' },
  { id: 'foods',    label: 'Món ăn' },
  { id: 'guides',   label: 'Hướng dẫn viên' },
];

const StarRating = ({ value, onChange }) => {
  const [hovered, setHovered] = useState(0);
  return (
    <div className="flex items-center gap-0.5">
      {[1, 2, 3, 4, 5].map((star) => (
        <button
          key={star}
          type="button"
          onMouseEnter={() => setHovered(star)}
          onMouseLeave={() => setHovered(0)}
          onClick={() => onChange(star)}
          className="focus:outline-none"
        >
          <Star
            size={16}
            className={`transition-colors ${
              star <= (hovered || value)
                ? 'text-amber-400 fill-amber-400'
                : 'text-amber-300'
            }`}
          />
        </button>
      ))}
    </div>
  );
};

const AddReview = () => {
  const [ratings, setRatings] = useState({
    services: 0, hotel: 0, places: 0,
    safety: 0,   foods: 0, guides: 0,
  });
  const [form, setForm] = useState({ name: '', phone: '', email: '', comments: '' });

  const handleSubmit = (e) => {
    e.preventDefault();
    // submit logic here
  };

  return (
    <section className="py-20 bg-[#F8FBFB]">
      <div className="container mx-auto px-4 md:px-12 lg:px-24">
        <motion.div
          initial={{ opacity: 0, y: 30 }}
          whileInView={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.6 }}
          className="bg-white rounded-[2.5rem] p-10 md:p-14 shadow-xl border border-gray-100 relative overflow-hidden max-w-4xl mx-auto"
        >
          {/* Pulsing Dot Decoration */}
          <div className="absolute top-6 left-6 w-3 h-3 bg-[#1EB4D4] rounded-full shadow-[0_0_14px_rgba(30,180,212,0.7)] animate-ping opacity-60"></div>

          <h2 className="text-2xl font-black text-gray-900 mb-8">Thêm đánh giá của bạn</h2>

          {/* Star Rating Grid — 3 columns */}
          <div className="grid grid-cols-2 md:grid-cols-3 gap-x-12 gap-y-4 mb-10">
            {ratingCategories.map((cat) => (
              <div key={cat.id} className="flex items-center gap-3">
                <span className="text-gray-700 font-bold text-sm w-16">{cat.label}</span>
                <StarRating
                  value={ratings[cat.id]}
                  onChange={(val) => setRatings(prev => ({ ...prev, [cat.id]: val }))}
                />
              </div>
            ))}
          </div>

          {/* Form */}
          <form onSubmit={handleSubmit} className="space-y-4">
            {/* Name + Phone */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <input
                type="text"
                placeholder="Tên của bạn"
                value={form.name}
                onChange={e => setForm({ ...form, name: e.target.value })}
                className="w-full px-5 py-4 rounded-xl border border-gray-200 text-gray-700 font-medium focus:outline-none focus:ring-2 focus:ring-[#1EB4D4]/30 placeholder:text-gray-300 bg-[#F8FBFB]"
              />
              <input
                type="tel"
                placeholder="Số điện thoại"
                value={form.phone}
                onChange={e => setForm({ ...form, phone: e.target.value })}
                className="w-full px-5 py-4 rounded-xl border border-gray-200 text-gray-700 font-medium focus:outline-none focus:ring-2 focus:ring-[#1EB4D4]/30 placeholder:text-gray-300 bg-[#F8FBFB]"
              />
            </div>

            {/* Email */}
            <input
              type="email"
              placeholder="Email của bạn"
              value={form.email}
              onChange={e => setForm({ ...form, email: e.target.value })}
              className="w-full px-5 py-4 rounded-xl border border-gray-200 text-gray-700 font-medium focus:outline-none focus:ring-2 focus:ring-[#1EB4D4]/30 placeholder:text-gray-300 bg-[#F8FBFB]"
            />

            {/* Comments */}
            <textarea
              placeholder="Bình luận của bạn..."
              rows={6}
              value={form.comments}
              onChange={e => setForm({ ...form, comments: e.target.value })}
              className="w-full px-5 py-4 rounded-xl border border-gray-200 text-gray-700 font-medium focus:outline-none focus:ring-2 focus:ring-[#1EB4D4]/30 placeholder:text-gray-300 bg-[#F8FBFB] resize-none"
            />

            {/* Submit */}
            <div>
              <button
                type="submit"
                className="inline-flex items-center gap-3 bg-[#1EB4D4] hover:bg-gray-900 text-white px-10 py-4 rounded-full font-black text-base transition-all shadow-xl shadow-[#1EB4D4]/25 group"
              >
                Gửi đánh giá
                <ArrowRight size={18} className="group-hover:translate-x-1 transition-transform" />
              </button>
            </div>
          </form>
        </motion.div>
      </div>
    </section>
  );
};

export default AddReview;
