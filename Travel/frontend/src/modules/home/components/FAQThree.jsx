import React, { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { ChevronRight } from 'lucide-react';
import nav3 from '../../../assets/nav3.png';

const faqs = [
  {
    question: "Làm cách nào để tôi đặt một chuyến đi?",
    answer: "Bạn có thể đặt tour trực tiếp trên website của chúng tôi hoặc liên hệ qua hotline. Chỉ cần chọn tour yêu thích, điền thông tin và thanh toán đơn giản."
  },
  {
    question: "Có những phương thức thanh toán nào?",
    answer: "Chúng tôi chấp nhận thanh toán qua thẻ tín dụng (Visa, Mastercard), chuyển khoản ngân hàng, ví điện tử (MoMo, ZaloPay) và thanh toán trực tiếp tại văn phòng."
  },
  {
    question: "Tôi có thể tùy chỉnh lịch trình du lịch không?",
    answer: "Hoàn toàn có thể! Chúng tôi cung cấp dịch vụ thiết kế lộ trình riêng theo nhu cầu và sở thích của bạn. Liên hệ đội ngũ tư vấn để được hỗ trợ."
  },
  {
    question: "Chính sách hủy bỏ của các bạn là gì?",
    answer: "Bạn có thể hủy miễn phí trước 7 ngày khởi hành. Hủy trong vòng 3-7 ngày sẽ chịu phí 50%. Hủy dưới 3 ngày sẽ không được hoàn tiền."
  }
];

const FAQThree = () => {
  const [openIndex, setOpenIndex] = useState(0);

  return (
    <section className="py-24 bg-white relative overflow-hidden">
      {/* Decorative bg */}
      <div className="absolute top-0 left-0 opacity-10 pointer-events-none">
        <img 
          src={nav3}
          alt="" 
          className="w-40"
        />
      </div>

      <div className="container mx-auto px-4 md:px-12 lg:px-24">
        <div className="flex flex-col lg:flex-row gap-16 lg:gap-24">

          {/* Left: Title & Description */}
          <motion.div 
            initial={{ opacity: 0, x: -30 }}
            whileInView={{ opacity: 1, x: 0 }}
            viewport={{ once: true }}
            className="w-full lg:w-[45%]"
          >
            <p
              className="text-[#1EB4D4] text-lg font-medium mb-3 italic"
              style={{ fontFamily: "'Kalam', cursive" }}
            >
              Tìm hiểu thêm từ
            </p>
            <h2 className="text-3xl md:text-4xl lg:text-5xl font-black text-gray-900 leading-tight mb-6">
              Câu Hỏi Thường Gặp
            </h2>
            <p className="text-gray-400 text-[15px] leading-relaxed font-medium mb-8 max-w-md">
              Dưới đây là những câu hỏi phổ biến nhất từ khách hàng. Nếu bạn cần thêm thông tin, đừng ngần ngại liên hệ với chúng tôi.
            </p>

            <div className="flex items-center gap-8">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-full bg-[#1EB4D4]/10 flex items-center justify-center">
                  <span className="text-[#1EB4D4] text-lg">📞</span>
                </div>
                <div>
                  <p className="text-xs text-gray-400 font-medium">Gọi bất cứ lúc nào</p>
                  <p className="text-sm font-black text-gray-900">+1 234 567 890</p>
                </div>
              </div>
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-full bg-[#1EB4D4]/10 flex items-center justify-center">
                  <span className="text-[#1EB4D4] text-lg">✉️</span>
                </div>
                <div>
                  <p className="text-xs text-gray-400 font-medium">Địa chỉ email</p>
                  <p className="text-sm font-black text-gray-900">hello@turmet.com</p>
                </div>
              </div>
            </div>
          </motion.div>

          {/* Right: Accordion */}
          <motion.div 
            initial={{ opacity: 0, x: 30 }}
            whileInView={{ opacity: 1, x: 0 }}
            viewport={{ once: true }}
            className="w-full lg:w-[55%]"
          >
            <div className="space-y-4">
              {faqs.map((faq, idx) => (
                <div
                  key={idx}
                  className={`rounded-2xl border transition-all duration-300 overflow-hidden ${
                    openIndex === idx 
                      ? 'border-[#1EB4D4]/30 bg-white shadow-lg shadow-[#1EB4D4]/5' 
                      : 'border-gray-100 bg-white hover:border-gray-200'
                  }`}
                >
                  <button
                    onClick={() => setOpenIndex(openIndex === idx ? -1 : idx)}
                    className="w-full flex items-center justify-between p-6 text-left"
                  >
                    <span className={`font-bold text-[15px] pr-4 transition-colors ${
                      openIndex === idx ? 'text-[#1EB4D4]' : 'text-gray-900'
                    }`}>
                      {faq.question}
                    </span>
                    <div className={`w-8 h-8 rounded-full flex items-center justify-center shrink-0 transition-all duration-300 ${
                      openIndex === idx 
                        ? 'bg-[#1EB4D4] text-white rotate-90' 
                        : 'bg-gray-100 text-gray-500'
                    }`}>
                      <ChevronRight size={16} />
                    </div>
                  </button>
                  <AnimatePresence>
                    {openIndex === idx && (
                      <motion.div
                        initial={{ height: 0, opacity: 0 }}
                        animate={{ height: 'auto', opacity: 1 }}
                        exit={{ height: 0, opacity: 0 }}
                        transition={{ duration: 0.3 }}
                      >
                        <p className="px-6 pb-6 text-gray-400 text-sm leading-relaxed font-medium">
                          {faq.answer}
                        </p>
                      </motion.div>
                    )}
                  </AnimatePresence>
                </div>
              ))}
            </div>
          </motion.div>

        </div>
      </div>
    </section>
  );
};

export default FAQThree;
