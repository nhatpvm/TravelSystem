import React from 'react';
import Navbar from '../../home/components/Navbar';
import Footer from '../../home/components/Footer';
import BookingPlatform from '../../home/components/BookingPlatform';
import { ChevronRight, MapPin, Star, Clock, Users, ArrowRight, Check } from 'lucide-react';
import { motion } from 'framer-motion';
import { Link } from 'react-router-dom';
import DestinationSidebar from '../components/DestinationSidebar';
import AddReview from '../components/AddReview';

const highlights = [
  "Hướng dẫn viên du lịch chuyên nghiệp được cấp chứng chỉ",
  "Bao gồm đón và trả tại khách sạn",
  "Đã bao gồm tất cả phí vào cửa",
  "Trải nghiệm nhóm nhỏ (tối đa 12 người)",
  "Hủy bỏ linh hoạt trước 24 giờ",
  "Bảo hiểm du lịch miễn phí",
];

const itinerary = [
  { day: "Ngày 1", title: "Đến nơi & Tour chào mừng thành phố", desc: "Đến điểm đến, nhận phòng khách sạn và tận hưởng chuyến đi bộ ngắm thành phố buổi tối có hướng dẫn." },
  { day: "Ngày 2", title: "Thiên nhiên & Cảnh quan", desc: "Khám phá phong cảnh tự nhiên tuyệt đẹp, các công viên địa phương và điểm quan sát cùng hướng dẫn viên." },
  { day: "Ngày 3", title: "Hòa nhập văn hóa", desc: "Thăm các địa danh lịch sử, chợ địa phương và thưởng thức ẩm thực truyền thống cho bữa trưa." },
  { day: "Ngày 4", title: "Hoạt động phiêu lưu", desc: "Lựa chọn đi bộ đường dài, chèo thuyền kayak hoặc đu dây zip-line – một ngày trọn vẹn với các hoạt động kịch tính." },
  { day: "Ngày 5", title: "Nghỉ ngơi & Khởi hành", desc: "Buổi sáng tự do khám phá theo tốc độ của riêng bạn trước khi chuyển ra sân bay." },
];

const gallery = [
  "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?q=80&w=800&auto=format&fit=crop",
  "https://images.unsplash.com/photo-1464822759023-fed622ff2c3b?q=80&w=800&auto=format&fit=crop",
  "https://images.unsplash.com/photo-1537996194471-e657df975ab4?q=80&w=800&auto=format&fit=crop",
  "https://images.unsplash.com/photo-1528360983277-13d401cdc186?q=80&w=800&auto=format&fit=crop",
];

const DestinationDetailsPage = () => {
  return (
    <div className="min-h-screen bg-white flex flex-col font-sans">
      <Navbar />

      {/* Breadcrumb Header Section — same as About Us */}
      <section className="relative h-[450px] md:h-[550px] flex items-center justify-center overflow-hidden">
        <div className="absolute inset-0 z-0">
          <img
            src="https://images.unsplash.com/photo-1476514525535-07fb3b4ae5f1?q=80&w=2070"
            alt="Destination Details"
            className="w-full h-full object-cover"
          />
          <div className="absolute inset-0 bg-black/35"></div>
        </div>

        <div className="container mx-auto px-4 relative z-10 text-center text-white">
          <motion.h1
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            className="text-5xl md:text-7xl lg:text-8xl font-black mb-8 tracking-tighter"
          >
            Chi tiết điểm đến
          </motion.h1>

          <motion.div
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.2 }}
            className="inline-flex items-center gap-4 bg-white/20 backdrop-blur-md px-10 py-4 rounded-full border border-white/30 text-lg font-bold"
          >
            <Link to="/" className="text-white hover:text-[#1EB4D4] transition-colors">Trang chủ</Link>
            <ChevronRight size={20} className="text-[#1EB4D4]" />
            <Link to="/destinations" className="text-white hover:text-[#1EB4D4] transition-colors">Điểm đến của chúng tôi</Link>
            <ChevronRight size={20} className="text-[#1EB4D4]" />
            <span className="text-white">Chi tiết</span>
          </motion.div>
        </div>
      </section>

      {/* Main Content */}
      <section className="py-20 bg-white">
        <div className="container mx-auto px-4 md:px-12 lg:px-24">
          <div className="flex flex-col lg:flex-row gap-16">

            {/* Left Column: Main Info */}
            <div className="w-full lg:w-2/3">

              {/* Hero Image */}
              <motion.div
                initial={{ opacity: 0, scale: 0.98 }}
                whileInView={{ opacity: 1, scale: 1 }}
                className="rounded-[2rem] overflow-hidden mb-10 shadow-xl"
              >
                <img
                  src="https://images.unsplash.com/photo-1476514525535-07fb3b4ae5f1?q=80&w=1400&auto=format&fit=crop"
                  alt="Destination"
                  className="w-full h-[480px] object-cover"
                />
              </motion.div>

              {/* Meta Info Bar */}
              <div className="flex flex-wrap items-center gap-6 mb-10 pb-8 border-b border-gray-100">
                <div className="flex items-center gap-2 text-gray-500 font-medium">
                  <MapPin size={18} className="text-[#1EB4D4]" />
                  <span>Singerland, Thái Lan</span>
                </div>
                <div className="flex items-center gap-2 text-gray-500 font-medium">
                  <Clock size={18} className="text-[#1EB4D4]" />
                  <span>5 Ngày / 4 Đêm</span>
                </div>
                <div className="flex items-center gap-2 text-gray-500 font-medium">
                  <Users size={18} className="text-[#1EB4D4]" />
                  <span>Tối đa 12 người</span>
                </div>
                <div className="flex items-center gap-1">
                  {[1,2,3,4,5].map(s => (
                    <Star key={s} size={16} className="text-[#1EB4D4] fill-[#1EB4D4]" />
                  ))}
                  <span className="ml-2 text-gray-500 font-medium text-sm">(124 đánh giá)</span>
                </div>
              </div>

              {/* Description */}
              <h2 className="text-3xl font-black text-gray-900 mb-4">Khám phá Moliva xinh đẹp: Thiên đường thiên nhiên</h2>
              <p className="text-gray-500 font-medium leading-[1.9] mb-4">
                Có rất nhiều biến thể của các đoạn văn bản Lorem Ipsum sẵn có, nhưng phần lớn đã bị thay đổi dưới một số hình thức, bởi những từ ngữ hài hước hoặc ngẫu nhiên không hề đáng tin. Nếu bạn định sử dụng một đoạn văn bản Lorem Ipsum.
              </p>
              <p className="text-gray-500 font-medium leading-[1.9] mb-12">
                Tất cả các trình tạo Lorem Ipsum trên Internet đều có xu hướng lặp lại các đoạn văn bản đã xác định trước khi cần thiết, biến đây thành trình tạo thực sự đầu tiên trên Internet. Nó sử dụng một từ điển gồm hơn 200 từ Latinh, kết hợp với một số cấu trúc câu mẫu.
              </p>

              {/* Highlights */}
              <h3 className="text-2xl font-black text-gray-900 mb-6">Điểm nổi bật của tour</h3>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-14">
                {highlights.map((h, i) => (
                  <div key={i} className="flex items-center gap-3 text-gray-600 font-medium">
                    <div className="w-6 h-6 bg-[#1EB4D4]/10 rounded-full flex items-center justify-center flex-shrink-0">
                      <Check size={13} className="text-[#1EB4D4]" />
                    </div>
                    {h}
                  </div>
                ))}
              </div>

              {/* Itinerary */}
              <h3 className="text-2xl font-black text-gray-900 mb-6">Lịch trình tour</h3>
              <div className="space-y-4 mb-14">
                {itinerary.map((item, i) => (
                  <motion.div
                    key={i}
                    initial={{ opacity: 0, x: -20 }}
                    whileInView={{ opacity: 1, x: 0 }}
                    transition={{ delay: i * 0.1 }}
                    className="flex gap-5 p-6 bg-[#F8FBFB] rounded-2xl hover:shadow-md transition-all group"
                  >
                    <div className="flex-shrink-0 w-14 h-14 bg-[#1EB4D4] text-white rounded-2xl flex items-center justify-center font-black text-sm leading-tight text-center group-hover:scale-110 transition-transform">
                      {item.day.split(' ').map((w, j) => <div key={j}>{w}</div>)}
                    </div>
                    <div>
                      <h4 className="font-black text-gray-900 mb-1">{item.title}</h4>
                      <p className="text-gray-400 font-medium text-sm leading-relaxed">{item.desc}</p>
                    </div>
                  </motion.div>
                ))}
              </div>

              {/* Photo Gallery */}
              <h3 className="text-2xl font-black text-gray-900 mb-6">Thư viện ảnh</h3>
              <div className="grid grid-cols-2 gap-4">
                {gallery.map((src, i) => (
                  <motion.div
                    key={i}
                    whileHover={{ scale: 1.02 }}
                    className="rounded-2xl overflow-hidden shadow-md"
                  >
                    <img src={src} alt={`Gallery ${i+1}`} className="w-full h-48 object-cover hover:scale-110 transition-transform duration-500" />
                  </motion.div>
                ))}
              </div>
            </div>

            {/* Right Column: Booking Card */}
            <div className="w-full lg:w-1/3">
              <div className="sticky top-28">
                <div className="bg-white rounded-[2rem] shadow-2xl shadow-gray-200/70 border border-gray-100 overflow-hidden">
                  {/* Price Header */}
                  <div className="bg-[#1EB4D4] p-8 text-white text-center">
                    <p className="text-white/70 font-bold mb-1">Giá từ</p>
                    <h3 className="text-5xl font-black tracking-tighter">$49 <span className="text-2xl font-bold">/ngày</span></h3>
                    <div className="flex items-center justify-center gap-1 mt-3">
                      {[1,2,3,4,5].map(s => <Star key={s} size={14} fill="white" className="text-white" />)}
                      <span className="ml-2 text-white/80 text-sm font-bold">4.9 (124)</span>
                    </div>
                  </div>

                  {/* Booking Form */}
                  <div className="p-8 space-y-4">
                    <div>
                      <label className="block text-gray-700 font-bold text-sm mb-2">Họ và tên</label>
                      <input type="text" placeholder="Họ và tên của bạn" className="w-full px-5 py-4 rounded-xl border border-gray-200 text-gray-700 font-medium focus:outline-none focus:ring-2 focus:ring-[#1EB4D4]/30 placeholder:text-gray-300" />
                    </div>
                    <div>
                      <label className="block text-gray-700 font-bold text-sm mb-2">Ngày đi</label>
                      <input type="date" className="w-full px-5 py-4 rounded-xl border border-gray-200 text-gray-500 font-medium focus:outline-none focus:ring-2 focus:ring-[#1EB4D4]/30" />
                    </div>
                    <div>
                      <label className="block text-gray-700 font-bold text-sm mb-2">Số lượng người</label>
                      <select className="w-full px-5 py-4 rounded-xl border border-gray-200 text-gray-700 font-medium focus:outline-none focus:ring-2 focus:ring-[#1EB4D4]/30 bg-white">
                        <option>1 Người</option>
                        <option>2 Người</option>
                        <option>3-5 Người</option>
                        <option>6-12 Người</option>
                      </select>
                    </div>
                    <button className="w-full bg-[#1EB4D4] hover:bg-gray-900 text-white py-5 rounded-xl font-black text-lg flex items-center justify-center gap-3 transition-all shadow-xl shadow-[#1EB4D4]/20 group mt-2">
                      Đặt ngay
                      <ArrowRight size={20} className="group-hover:translate-x-1 transition-transform" />
                    </button>
                    <p className="text-center text-gray-400 text-xs font-medium pt-2">Không cần thanh toán cho đến khi xác nhận</p>
                  </div>
                </div>
              </div>
            </div>

          </div>
        </div>
      </section>

      <DestinationSidebar />

      <AddReview />

      <BookingPlatform />
      <Footer />
    </div>
  );
};

export default DestinationDetailsPage;
