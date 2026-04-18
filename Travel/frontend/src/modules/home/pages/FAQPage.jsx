import React, { useState } from 'react';
import Navbar from '../../home/components/Navbar';
import Footer from '../../home/components/Footer';
import { 
  ChevronRight, 
  ChevronDown, 
  ChevronsRight,
  Smartphone,
  Apple
} from 'lucide-react';
import { motion, AnimatePresence } from 'framer-motion';
import { Link } from 'react-router-dom';

const faqData = [
  {
    id: 1,
    question: "Làm thế nào để đặt tour du lịch trực tuyến?",
    answer: "Bạn chỉ cần chọn tour yêu thích, nhấn nút 'Đặt ngay', điền thông tin cá nhân và tiến hành thanh toán. Hệ thống của chúng tôi hỗ trợ nhiều phương thức thanh toán an toàn."
  },
  {
    id: 2,
    question: "Chính sách hủy tour và hoàn tiền như thế nào?",
    answer: "Hủy tour trước 7 ngày sẽ được hoàn tiền 100%. Hủy từ 3-6 ngày hoàn 50%. Rất tiếc chúng tôi không hỗ trợ hoàn tiền nếu hủy trong vòng 48 giờ trước giờ khởi hành."
  },
  {
    id: 3,
    question: "Tôi có cần mua bảo hiểm du lịch không?",
    answer: "Hầu hết các tour của chúng tôi đã bao gồm bảo hiểm du lịch cơ bản. Tuy nhiên, chúng tôi luôn khuyến khích bạn mua thêm bảo hiểm chuyên sâu cho các chuyến đi mạo hiểm."
  },
  {
    id: 4,
    question: "Làm sao để liên hệ với hướng dẫn viên?",
    answer: "Sau khi đặt tour thành công, thông tin liên lạc của hướng dẫn viên sẽ được gửi qua email và hiển thị trong phần 'Lịch trình của tôi' trên ứng dụng."
  },
  {
    id: 5,
    question: "Trẻ em có được giảm giá vé tour không?",
    answer: "Chúng tôi có chính sách giá ưu đãi cho trẻ em dưới 12 tuổi. Trẻ em dưới 2 tuổi thường được miễn phí hoàn toàn (tùy thuộc vào yêu cầu của hãng hàng không)."
  },
  {
    id: 6,
    question: "Tour có bao gồm chi phí ăn uống không?",
    answer: "Tùy thuộc vào gói tour bạn chọn. Thông thường, các bữa sáng và bữa tối sẽ được bao gồm trong tour trọn gói. Chi tiết được ghi rõ trong phần 'Tiện ích'."
  },
  {
    id: 7,
    question: "Tôi có thể yêu cầu thực đơn riêng không?",
    answer: "Bạn hoàn toàn có thể! Vui lòng ghi chú yêu cầu ăn kiêng (ăn chay, dị ứng thực phẩm) khi thực hiện đặt tour để chúng tôi sắp xếp chu đáo nhất."
  },
  {
    id: 8,
    question: "Lịch trình tour có thể thay đổi không?",
    answer: "Lịch trình có thể thay đổi nhẹ tùy theo điều kiện thời tiết hoặc tình trạng giao thông thực tế để đảm bảo an toàn và trải nghiệm tốt nhất cho quý khách."
  }
];

const FAQPage = () => {
  const [activeFaq, setActiveFaq] = useState(null);

  return (
    <div className="min-h-screen bg-white flex flex-col font-sans">
      <Navbar />

      {/* Breadcrumb Header Section */}
      <section className="relative h-[400px] flex items-center justify-center overflow-hidden">
        <div className="absolute inset-0 z-0">
          <img
            src="https://images.unsplash.com/photo-1454165833222-68d69a597721?q=80&w=2070"
            alt="FAQ"
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
            Hỏi & Đáp
          </motion.h1>

          <motion.div
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.2 }}
            className="inline-flex items-center gap-4 bg-white/20 backdrop-blur-md px-10 py-4 rounded-full border border-white/30 text-lg font-bold"
          >
            <Link to="/" className="text-white hover:text-[#1EB4D4] transition-colors">Trang chủ</Link>
            <ChevronRight size={20} className="text-[#1EB4D4]" />
            <span className="text-white">Câu hỏi thường gặp</span>
          </motion.div>
        </div>
      </section>

      {/* FAQ Grid Section */}
      <section className="py-24 bg-white">
        <div className="container mx-auto px-4 md:px-12 lg:px-24">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-x-12 gap-y-6">
            {faqData.map((faq) => (
              <div 
                key={faq.id} 
                className="relative"
                onMouseEnter={() => setActiveFaq(faq.id)}
                onMouseLeave={() => setActiveFaq(null)}
              >
                <div
                  className={`w-full text-left px-8 py-6 rounded-2xl border transition-all duration-300 flex items-center justify-between cursor-pointer ${
                    activeFaq === faq.id 
                    ? 'bg-white border-[#1EB4D4]/30 shadow-xl shadow-[#1EB4D4]/10' 
                    : 'bg-white border-gray-100 hover:border-[#1EB4D4]/30'
                  }`}
                >
                  <span className={`text-lg font-black transition-colors ${activeFaq === faq.id ? 'text-[#1EB4D4]' : 'text-gray-900'}`}>
                    {faq.question}
                  </span>
                  {activeFaq === faq.id 
                    ? <ChevronDown size={22} className="text-[#1EB4D4]" /> 
                    : <ChevronsRight size={22} className="text-gray-400" />
                  }
                </div>
                
                <AnimatePresence>
                  {activeFaq === faq.id && (
                    <motion.div
                      initial={{ height: 0, opacity: 0 }}
                      animate={{ height: 'auto', opacity: 1 }}
                      exit={{ height: 0, opacity: 0 }}
                      className="overflow-hidden"
                    >
                      <div className="px-8 py-6 bg-[#F8FBFB] border-x border-b border-gray-100 rounded-b-2xl -mt-4 pt-10 text-gray-500 font-medium leading-relaxed">
                        {faq.answer}
                      </div>
                    </motion.div>
                  )}
                </AnimatePresence>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* App Promo Banner Section */}
      <section className="container mx-auto px-4 md:px-12 lg:px-24 mb-24">
        <div className="bg-[#1EB4D4] rounded-[2.5rem] relative overflow-hidden h-auto md:h-[500px] flex flex-col md:flex-row items-center">
          {/* Text Content */}
          <div className="w-full md:w-1/2 p-12 md:p-20 relative z-10 text-white leading-tight">
            <p className="font-black italic mb-4 text-xl" style={{ fontFamily: "'Kalam', cursive" }}>Ưu đãi đặc biệt cho bạn</p>
            <h2 className="text-4xl md:text-6xl font-black mb-8">Giảm giá 50% khi đặt qua ứng dụng lần đầu</h2>
            <p className="text-white/80 font-medium mb-12 max-w-md leading-relaxed">
              Trải nghiệm hành trình du lịch trọn vẹn và tiết kiệm hơn với ứng dụng di động của chúng tôi. Đặt tour mọi lúc, mọi nơi.
            </p>
            
            <div className="flex flex-wrap gap-4">
              <button className="bg-black text-white px-8 py-4 rounded-full font-black flex items-center gap-3 hover:bg-gray-900 transition-all shadow-xl group">
                <Apple size={24} className="group-hover:scale-110 transition-transform" />
                <div className="text-left leading-none">
                  <p className="text-[10px] opacity-60 uppercase font-black">Tải trên</p>
                  <p className="text-lg">App Store</p>
                </div>
              </button>
              <button className="bg-white/10 backdrop-blur-md text-white border border-white/30 px-8 py-4 rounded-full font-black flex items-center gap-3 hover:bg-white hover:text-[#1EB4D4] transition-all shadow-xl group">
                <Smartphone size={24} className="group-hover:scale-110 transition-transform" />
                <div className="text-left leading-none">
                  <p className="text-[10px] opacity-60 uppercase font-black">Tải trên</p>
                  <p className="text-lg">Google Play</p>
                </div>
              </button>
            </div>
          </div>

          {/* App Screenshots Mockup */}
          <div className="w-full md:w-1/2 h-full relative flex items-center justify-center p-12 md:p-0">
             <div className="relative">
                {/* Main phone screen */}
                <div className="w-[280px] h-[580px] bg-white rounded-[3rem] shadow-2xl overflow-hidden border-8 border-gray-900 relative z-20 hidden md:block mt-24">
                   <img src="https://images.unsplash.com/photo-1555066931-4365d14bab8c?q=80&w=400" alt="App Screen" className="w-full h-full object-cover" />
                </div>
                {/* Floating card UI element */}
                <div className="absolute top-1/4 -left-32 z-30 bg-white p-4 rounded-2xl shadow-2xl flex items-center gap-4 hidden lg:flex border border-gray-50">
                    <div className="w-12 h-12 bg-orange-100 rounded-xl flex items-center justify-center text-orange-500"><ChevronRight /></div>
                    <div>
                        <p className="text-xs font-black text-gray-900 leading-none mb-1">Câu chuyện thành công</p>
                        <p className="text-[10px] text-gray-400 font-bold">Xem đánh giá thành viên</p>
                    </div>
                </div>
                {/* Background decorative App Screen image */}
                <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[450px] opacity-10 pointer-events-none">
                    <img src="https://ex-coders.com/html/turmet/assets/img/others/app-shape.png" alt="pattern" />
                </div>
             </div>
          </div>
          
          {/* Decorative Pattern Background */}
          <div className="absolute inset-0 opacity-10 pointer-events-none">
            <svg className="w-full h-full" viewBox="0 0 100 100" preserveAspectRatio="none">
              <path d="M0 100 L100 0 V100 Z" fill="white" fillOpacity="0.05" />
            </svg>
          </div>
        </div>
      </section>

      <Footer />
    </div>
  );
};

export default FAQPage;
