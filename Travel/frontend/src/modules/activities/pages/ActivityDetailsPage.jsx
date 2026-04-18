import React from 'react';
import Navbar from '../../home/components/Navbar';
import Footer from '../../home/components/Footer';
import { 
  ChevronRight, 
  MapPin, 
  Star, 
  Clock, 
  Users, 
  ArrowRight, 
  Check, 
  Calendar,
  Bed,
  Ticket,
  Map as MapIcon,
  Languages,
  Bus,
  Activity,
  UserCheck,
  Share2,
  Plane,
  Heart
} from 'lucide-react';
import { motion } from 'framer-motion';
import { Link } from 'react-router-dom';

const topHighlights = [
  "Duis ultricies sapien a volutpat varius. Maecenas",
  "Blandit enim. Pellentesque ultrices, justo non",
  "Nunc in quam in quam placerat rhoncus quis",
  "Laoreet sagittis posuere, dolor nibh imperdiet",
  "Condimentum lacinia nisl vitae vehicula.",
  "Duis ultricies sapien a volutpat varius. Maecenas",
  "Blandit enim. Pellentesque ultrices, justo non",
  "Nunc in quam in quam placerat rhoncus quis",
  "Laoreet sagittis posuere, dolor nibh imperdiet",
  "Condimentum lacinia nisl vitae vehicula."
];

const activityAmenities = [
  { icon: <Bed size={22} />, label: "Chỗ ở", value: "Khách sạn 5 sao" },
  { icon: <Ticket size={22} />, label: "Phí tham quan", value: "Không" },
  { icon: <MapIcon size={22} />, label: "Thành phố đến", value: "London" },
  { icon: <Languages size={22} />, label: "Ngôn ngữ", value: "Tiếng Anh" },
  { icon: <Bus size={22} />, label: "Đưa đón khách sạn", value: "Có sẵn" },
  { icon: <Activity size={22} />, label: "Hoạt động tiếp theo", value: "Có sẵn" },
  { icon: <UserCheck size={22} />, label: "Hướng dẫn viên tại chỗ", value: "Có hướng dẫn" },
  { icon: <Users size={22} />, label: "Tuổi tối đa", value: "60" }
];

const ActivityDetailsPage = () => {
  return (
    <div className="min-h-screen bg-white flex flex-col font-sans">
      <Navbar />

      {/* Breadcrumb Header Section */}
      <section className="relative h-[400px] flex items-center justify-center overflow-hidden">
        <div className="absolute inset-0 z-0">
          <img
            src="https://images.unsplash.com/photo-1533105079780-92b9be482077?q=80&w=2070"
            alt="Activity Details"
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
            Chi Tiết Hoạt Động
          </motion.h1>

          <motion.div
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.2 }}
            className="inline-flex items-center gap-4 bg-white/20 backdrop-blur-md px-10 py-4 rounded-full border border-white/30 text-lg font-bold"
          >
            <Link to="/" className="text-white hover:text-[#1EB4D4] transition-colors">Trang chủ</Link>
            <ChevronRight size={20} className="text-[#1EB4D4]" />
            <Link to="/activities" className="text-white hover:text-[#1EB4D4] transition-colors">Hoạt động</Link>
            <ChevronRight size={20} className="text-[#1EB4D4]" />
            <span className="text-white">Chi tiết</span>
          </motion.div>
        </div>
      </section>

      {/* Main Content Area */}
      <section className="py-24 bg-white">
        <div className="container mx-auto px-4 md:px-12 lg:px-24">
          
          {/* Header Info Section - Titles & Share */}
          <div className="flex flex-col md:flex-row justify-between items-start md:items-center mb-10 gap-6">
            <div>
              <h2 className="text-4xl font-black text-gray-900 mb-4 tracking-tight">
                The Montcalm At Brewery Japan City
              </h2>
              <div className="flex flex-wrap items-center gap-6">
                <div className="flex items-center gap-2">
                   <div className="flex text-orange-400">
                      {[1,2,3,4].map(i => <Star key={i} size={16} fill="currentColor" />)}
                      <Star size={16} fill="none" />
                   </div>
                   <span className="text-sm text-gray-400 font-bold">(16 Review)</span>
                </div>
                <div className="flex items-center gap-2 text-gray-400 font-bold text-sm">
                   <MapPin size={16} className="text-[#1EB4D4]" /> California
                </div>
              </div>
            </div>
            <button className="flex items-center gap-2 border border-gray-100 px-6 py-3 rounded-xl font-bold text-[#1EB4D4] hover:bg-[#F8FBFB] transition-colors shadow-sm">
               Chia sẻ <Share2 size={18} />
            </button>
          </div>

          {/* Quick Info Bar */}
          <div className="bg-white border border-gray-50 rounded-3xl p-8 mb-16 shadow-sm flex flex-wrap items-center justify-between gap-8 md:divide-x divide-gray-100">
             <div className="flex items-center gap-4 px-4">
                <div className="bg-[#1EB4D4]/10 p-3 rounded-xl text-[#1EB4D4]">
                   <MapPin size={24} />
                </div>
                <div>
                   <p className="text-[10px] text-gray-400 font-black uppercase tracking-widest">Địa điểm</p>
                   <p className="text-sm font-black text-gray-900">California</p>
                </div>
             </div>
             <div className="flex items-center gap-4 px-8">
                <div className="bg-[#1EB4D4]/10 p-3 rounded-xl text-[#1EB4D4]">
                   <Activity size={24} />
                </div>
                <div>
                   <p className="text-[10px] text-gray-400 font-black uppercase tracking-widest">Loại hoạt động</p>
                   <p className="text-sm font-black text-gray-900">Phiêu lưu</p>
                </div>
             </div>
             <div className="flex items-center gap-4 px-8">
                <div className="bg-[#1EB4D4]/10 p-3 rounded-xl text-[#1EB4D4]">
                   <Clock size={24} />
                </div>
                <div>
                   <p className="text-[10px] text-gray-400 font-black uppercase tracking-widest">Ngày diễn ra</p>
                   <p className="text-sm font-black text-gray-900">Tháng 2: 5 - 11</p>
                </div>
             </div>
             <div className="flex items-center gap-4 px-8">
                <div className="bg-[#1EB4D4]/10 p-3 rounded-xl text-[#1EB4D4]">
                   <Users size={24} />
                </div>
                <div>
                   <p className="text-[10px] text-gray-400 font-black uppercase tracking-widest">Khách du lịch</p>
                   <p className="text-sm font-black text-gray-900">1</p>
                </div>
             </div>
             <div className="flex-1 flex justify-end">
                <button className="bg-[#1EB4D4] hover:bg-gray-900 text-white px-8 py-4 rounded-full font-black flex items-center gap-3 transition-all shadow-xl shadow-[#1EB4D4]/20 group">
                   Khám phá chuyến bay <ArrowRight size={20} className="group-hover:translate-x-1" />
                </button>
             </div>
          </div>

          {/* Details Content & Booking Sidebar */}
          <div className="flex flex-col lg:flex-row gap-12">
            
            {/* Left Content */}
            <div className="w-full lg:w-[65%]">
               <div className="mb-14">
                  <h3 className="text-2xl font-black text-gray-900 mb-6 underline decoration-[#1EB4D4] decoration-4 underline-offset-8">Overview</h3>
                  <p className="text-gray-400 font-medium leading-[1.8]">
                    Consectetur adipisicing elit sed do eiusmod tempor is incididunt ut labore et dolore of magna aliqua. ut enim ad minim veniam made of owl the quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea dolor commodo consequat duis aute irure and dolor in reprehenderit Nullam semper quam mauris nec mollis felis aliquam eu ut non gravida mi phasellus.
                  </p>
               </div>

               <div className="mb-14">
                  <h3 className="text-2xl font-black text-gray-900 mb-8">Top Highlights</h3>
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-y-4 gap-x-8">
                    {topHighlights.map((text, i) => (
                      <div key={i} className="flex items-center gap-3">
                         <div className="w-5 h-5 bg-[#1EB4D4]/10 border border-[#1EB4D4]/30 rounded-full flex items-center justify-center">
                            <Check size={12} className="text-[#1EB4D4]" strokeWidth={3} />
                         </div>
                         <span className="text-gray-500 font-bold text-sm">{text}</span>
                      </div>
                    ))}
                  </div>
               </div>

               {/* Amenities Section */}
               <div className="grid grid-cols-2 md:grid-cols-4 gap-4 bg-[#F8FBFB] p-10 rounded-[2.5rem] border border-gray-100">
                  {activityAmenities.map((item, idx) => (
                    <div key={idx} className="flex items-center gap-4">
                       <div className="text-[#1EB4D4] bg-white w-12 h-12 rounded-xl flex items-center justify-center shadow-sm">
                          {item.icon}
                       </div>
                       <div>
                          <p className="text-[10px] text-gray-400 font-black uppercase tracking-wider">{item.label}</p>
                          <p className="text-sm font-black text-gray-900 truncate">{item.value}</p>
                       </div>
                    </div>
                  ))}
               </div>
            </div>

            {/* Right Booking Sidebar */}
            <div className="w-full lg:w-[35%]">
               <div className="sticky top-28 space-y-8">
                  <div className="bg-white border border-gray-100 rounded-[2.5rem] p-10 shadow-2xl shadow-gray-200/50">
                     <h3 className="text-2xl font-black text-gray-900 mb-8 pb-4 border-b border-gray-100">Book This Tour</h3>
                  
                     <form className="space-y-6">
                       <div className="space-y-2">
                          <label className="text-sm font-bold text-gray-600 block">Từ:</label>
                          <div className="relative">
                             <input type="text" placeholder="tháng/ngày/năm" className="w-full bg-[#F8FBFB] border-0 py-4 px-6 rounded-xl font-bold text-gray-800 outline-none focus:ring-2 focus:ring-[#1EB4D4]/20" />
                             <Calendar size={18} className="absolute right-4 top-1/2 -translate-y-1/2 text-[#1EB4D4]" />
                          </div>
                       </div>

                       <div className="space-y-2">
                          <label className="text-sm font-bold text-gray-600 block">Thời gian:</label>
                          <input type="text" className="w-full bg-[#F8FBFB] border-0 py-4 px-6 rounded-xl font-bold text-gray-800 outline-none" />
                       </div>

                       <div className="space-y-2">
                          <label className="text-sm font-bold text-gray-600 block">Vé:</label>
                          <input type="text" disabled placeholder="Please, Select Date First" className="w-full bg-[#F8FBFB] border-0 py-4 px-6 rounded-xl font-medium text-gray-400 italic outline-none" />
                       </div>

                       <div className="space-y-4 pt-4">
                          <p className="text-sm font-black text-gray-900">Thêm dịch vụ:</p>
                          <div className="space-y-3">
                            <label className="flex items-center gap-3 cursor-pointer group">
                              <input type="checkbox" className="w-5 h-5 border-2 border-gray-200 rounded accent-[#1EB4D4]" />
                              <span className="text-gray-500 font-bold text-xs uppercase tracking-wider">Services per booking</span>
                            </label>
                            <label className="flex items-center gap-3 cursor-pointer group">
                              <input type="checkbox" className="w-5 h-5 border-2 border-gray-200 rounded accent-[#1EB4D4]" />
                              <span className="text-gray-500 font-bold text-xs uppercase tracking-wider">Services per person</span>
                            </label>
                          </div>
                       </div>

                       <div className="pt-6 border-t border-dashed border-gray-100 space-y-3 text-gray-600 font-bold">
                          <div className="flex justify-between">
                             <span>Người lớn:</span>
                             <span>$20.00</span>
                          </div>
                          <div className="flex justify-between">
                             <span>Trẻ em:</span>
                             <span>$16.00</span>
                          </div>
                          <div className="flex justify-between pt-4 mt-4 border-t border-gray-100 items-center">
                             <span className="text-xl font-black text-gray-900">TỔNG CỘNG:</span>
                             <span className="text-2xl font-black text-[#1EB4D4]">$36.00</span>
                          </div>
                       </div>

                       <button className="w-full bg-[#1EB4D4] hover:bg-gray-900 text-white py-5 rounded-2xl font-black text-lg transition-all shadow-xl shadow-[#1EB4D4]/30 flex items-center justify-center gap-3">
                          Đặt ngay <ArrowRight size={20} />
                       </button>
                     </form>
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

export default ActivityDetailsPage;
