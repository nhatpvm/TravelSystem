import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { Search, ArrowRight } from 'lucide-react';

const DestinationSidebar = () => {
  const [searchQuery, setSearchQuery] = useState('');
  const [form, setForm] = useState({ name: '', email: '', comment: '' });

  const handleSubmit = (e) => {
    e.preventDefault();
    // handle form submission
  };

  return (
    <section className="py-20 bg-white">
      <div className="container mx-auto px-4 md:px-12 lg:px-24">
        <div className="flex flex-col lg:flex-row gap-14 items-start">

          {/* Left: Featured Image + Title + Description */}
          <motion.div
            initial={{ opacity: 0, x: -30 }}
            whileInView={{ opacity: 1, x: 0 }}
            transition={{ duration: 0.7 }}
            className="w-full lg:w-[60%]"
          >
            {/* Image */}
            <div className="rounded-[2rem] overflow-hidden mb-8 shadow-xl">
              <img
                src="https://images.unsplash.com/photo-1469474968028-56623f02e42e?q=80&w=1400&auto=format&fit=crop"
                alt="Explore South Africa"
                className="w-full h-[380px] object-cover hover:scale-105 transition-transform duration-700"
              />
            </div>

            {/* Title */}
            <h2 className="text-4xl md:text-5xl font-black text-gray-900 leading-tight mb-5">
              Khám phá vẻ đẹp quyến rũ <br className="hidden md:block" /> của Nam Phi
            </h2>

            {/* Description */}
            <p className="text-gray-400 font-medium leading-[1.9] mb-4">
              <span className="text-[#1EB4D4] font-semibold">Khám phá</span> vẻ đẹp hùng vĩ của thiên nhiên và những trải nghiệm văn hóa độc đáo chỉ có tại vùng đất này. Chúng tôi cam kết mang lại cho bạn một hành trình trọn vẹn và an toàn nhất.
            </p>
            <p className="text-gray-400 font-medium leading-[1.9]">
              Mỗi chuyến đi là một câu chuyện riêng, và chúng tôi ở đây để giúp bạn viết nên câu chuyện đó một cách tuyệt vời nhất với dịch vụ chuyên nghiệp và tận tâm.
            </p>
          </motion.div>

          {/* Right: Search + Contact Form */}
          <motion.div
            initial={{ opacity: 0, x: 30 }}
            whileInView={{ opacity: 1, x: 0 }}
            transition={{ duration: 0.7, delay: 0.1 }}
            className="w-full lg:w-[40%] space-y-8 lg:sticky lg:top-28"
          >

            {/* Search Box */}
            <div className="bg-white border border-gray-100 rounded-2xl p-6 shadow-lg">
              <h3 className="text-xl font-black text-gray-900 mb-5">Tìm kiếm tại đây</h3>
              <div className="relative flex items-center">
                <input
                  type="text"
                  placeholder="Tìm kiếm tại đây..."
                  value={searchQuery}
                  onChange={e => setSearchQuery(e.target.value)}
                  className="w-full pl-5 pr-16 py-4 rounded-xl border border-gray-200 text-gray-700 font-medium focus:outline-none focus:ring-2 focus:ring-[#1EB4D4]/30 placeholder:text-gray-300"
                />
                <button className="absolute right-2 top-1/2 -translate-y-1/2 w-11 h-11 bg-[#1EB4D4] hover:bg-gray-900 rounded-xl flex items-center justify-center text-white transition-all shadow-md">
                  <Search size={18} />
                </button>
              </div>
            </div>

            {/* Contact for Booking Form */}
            <div className="bg-white border border-gray-100 rounded-2xl p-6 shadow-lg">
              <h3 className="text-xl font-black text-gray-900 mb-6">Liên hệ để đặt chỗ</h3>
              <form onSubmit={handleSubmit} className="space-y-4">
                <input
                  type="text"
                  placeholder="Tên của bạn"
                  value={form.name}
                  onChange={e => setForm({ ...form, name: e.target.value })}
                  className="w-full px-5 py-4 rounded-xl border border-gray-200 text-gray-700 font-medium focus:outline-none focus:ring-2 focus:ring-[#1EB4D4]/30 placeholder:text-gray-300"
                />
                <input
                  type="email"
                  placeholder="Email của bạn"
                  value={form.email}
                  onChange={e => setForm({ ...form, email: e.target.value })}
                  className="w-full px-5 py-4 rounded-xl border border-gray-200 text-gray-700 font-medium focus:outline-none focus:ring-2 focus:ring-[#1EB4D4]/30 placeholder:text-gray-300"
                />
                <textarea
                  placeholder="Nhập bình luận tại đây..."
                  rows={5}
                  value={form.comment}
                  onChange={e => setForm({ ...form, comment: e.target.value })}
                  className="w-full px-5 py-4 rounded-xl border border-gray-200 text-gray-700 font-medium focus:outline-none focus:ring-2 focus:ring-[#1EB4D4]/30 placeholder:text-gray-300 resize-none"
                />
                <button
                  type="submit"
                  className="w-full bg-[#1EB4D4] hover:bg-gray-900 text-white py-4 rounded-xl font-black text-lg flex items-center justify-center gap-3 transition-all shadow-xl shadow-[#1EB4D4]/20 group"
                >
                  Gửi ngay
                  <ArrowRight size={20} className="group-hover:translate-x-1 transition-transform" />
                </button>
              </form>
            </div>

          </motion.div>
        </div>
      </div>
    </section>
  );
};

export default DestinationSidebar;
