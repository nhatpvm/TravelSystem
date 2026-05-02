import React, { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { ChevronRight, ChevronLeft, Quote } from 'lucide-react';
import nav1 from '../../../assets/nav1.png';
import nav2 from '../../../assets/nav2.png';
import nav3 from '../../../assets/nav3.png';

const testimonials = [
  {
    id: 1,
    name: "Kathryn Murphy",
    role: "Nhà thiết kế Web",
    avatar: nav1,
    text: "Mọi thứ đều thật tuyệt vời! Tôi đã có một kỳ nghỉ không thể nào quên cùng gia đình. Cảm ơn đội ngũ đã hỗ trợ tận tình."
  },
  {
    id: 2,
    name: "Robert Johnson",
    role: "Nhiếp ảnh gia",
    avatar: nav2,
    text: "Chuyến du lịch tuyệt vời nhất! Dịch vụ chuyên nghiệp, hướng dẫn viên nhiệt tình. Tôi chắc chắn sẽ quay lại với những chuyến đi tiếp theo."
  },
  {
    id: 3,
    name: "Emily Watson",
    role: "Blogger Du lịch",
    avatar: nav3,
    text: "Một trải nghiệm không thể nào quên! Mọi thứ đều được sắp xếp hoàn hảo từ đầu đến cuối. Rất đáng để giới thiệu cho bạn bè."
  }
];

const TestimonialsThree = () => {
  const [current, setCurrent] = useState(0);

  const next = () => setCurrent((prev) => (prev + 1) % testimonials.length);
  const prev = () => setCurrent((prev) => (prev - 1 + testimonials.length) % testimonials.length);

  return (
    <section className="py-24 bg-[#F8FBFC] overflow-hidden">
      <div className="container mx-auto px-4 md:px-12 lg:px-24">
        <div className="flex flex-col lg:flex-row items-center gap-16 lg:gap-24">

          {/* Left: Image */}
          <motion.div
            initial={{ opacity: 0, x: -40 }}
            whileInView={{ opacity: 1, x: 0 }}
            viewport={{ once: true }}
            className="w-full lg:w-[45%] relative flex justify-center"
          >
            <div className="relative w-[320px] md:w-[400px]">


              {/* Main Image */}
              <div className="relative rounded-[3rem]">
                <img
                  src={nav2}
                  alt="Happy Traveler"
                  className="w-full aspect-[3/4] object-cover"
                />
                {/* Gradient overlay at bottom */}
                <div className="absolute bottom-0 left-0 right-0 h-32 bg-gradient-to-t from-black/30 to-transparent"></div>
              </div>

              {/* Decorative dots */}
              <div className="absolute -bottom-6 -left-6 w-20 h-20 grid grid-cols-4 gap-1.5 opacity-20">
                {[...Array(16)].map((_, i) => (
                  <div key={i} className="w-2 h-2 bg-[#1EB4D4] rounded-full"></div>
                ))}
              </div>
            </div>
          </motion.div>

          {/* Right: Testimonial Content */}
          <motion.div
            initial={{ opacity: 0, x: 40 }}
            whileInView={{ opacity: 1, x: 0 }}
            viewport={{ once: true }}
            className="w-full lg:w-[55%]"
          >
            <p
              className="text-[#1EB4D4] text-lg font-medium mb-3 italic"
              style={{ fontFamily: "'Kalam', cursive" }}
            >
              Đánh giá
            </p>
            <h2 className="text-3xl md:text-4xl lg:text-5xl font-black text-gray-900 leading-tight mb-10">
              Du Khách Yêu Thích Địa Phương Của Chúng Tôi
            </h2>

            {/* Testimonial Card */}
            <div className="relative">
              <AnimatePresence mode="wait">
                <motion.div
                  key={current}
                  initial={{ opacity: 0, x: 30 }}
                  animate={{ opacity: 1, x: 0 }}
                  exit={{ opacity: 0, x: -30 }}
                  transition={{ duration: 0.3 }}
                  className="bg-white rounded-3xl p-8 md:p-10 shadow-[0_10px_40px_rgba(0,0,0,0.05)] border border-gray-50"
                >
                  {/* Author Info & Quote */}
                  <div className="flex items-start justify-between mb-6">
                    <div className="flex items-center gap-4">
                      <img
                        src={testimonials[current].avatar}
                        alt={testimonials[current].name}
                        className="w-14 h-14 rounded-full object-cover border-2 border-[#1EB4D4]/20"
                      />
                      <div>
                        <h4 className="font-black text-gray-900 text-lg">{testimonials[current].name}</h4>
                        <p className="text-gray-400 text-sm font-medium">{testimonials[current].role}</p>
                      </div>
                    </div>
                    <div className="text-[#1EB4D4]">
                      <Quote size={40} className="fill-[#1EB4D4]/10" />
                    </div>
                  </div>

                  {/* Text */}
                  <p className="text-gray-500 leading-relaxed font-medium text-[15px]">
                    {testimonials[current].text}
                  </p>
                </motion.div>
              </AnimatePresence>

              {/* Navigation Buttons */}
              <div className="flex gap-3 mt-8 justify-end">
                <button
                  onClick={prev}
                  className="w-12 h-12 rounded-full bg-[#1EB4D4] text-white flex items-center justify-center hover:bg-[#19a7c5] transition-all shadow-lg shadow-[#1EB4D4]/20"
                >
                  <ChevronLeft size={20} />
                </button>
                <button
                  onClick={next}
                  className="w-12 h-12 rounded-full bg-white border border-gray-200 text-gray-600 flex items-center justify-center hover:bg-[#1EB4D4] hover:text-white hover:border-[#1EB4D4] transition-all"
                >
                  <ChevronRight size={20} />
                </button>
              </div>
            </div>
          </motion.div>
        </div>
      </div>
    </section>
  );
};

export default TestimonialsThree;
