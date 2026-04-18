import React from 'react';
import { Tag, Clock, ArrowRight, Percent, Sparkles, Zap, Flame, Gift, ChevronDown } from 'lucide-react';
import { motion } from 'framer-motion';
import Navbar from '../components/Navbar';
import Footer from '../components/Footer';

const PromotionsPage = () => {
  const deals = [
    {
      id: 1,
      title: 'Hè Rực Rỡ: Phú Quốc Tour 3N2Đ',
      discount: '40% OFF',
      tag: 'Phổ biến nhất',
      price: '2.500.000đ',
      oldPrice: '4.200.000đ',
      image: 'https://images.unsplash.com/photo-1507525428034-b723cf961d3e?auto=format&fit=crop&q=80&w=800',
      expiry: 'Còn 2 ngày',
      color: 'blue'
    },
    {
      id: 2,
      title: 'Vé Máy Bay Khứ Hồi Hà Nội - Đà Lạt',
      discount: 'GIÁ SỐC',
      tag: 'Flash Sale',
      price: '1.200.000đ',
      oldPrice: '2.800.000đ',
      image: 'https://images.unsplash.com/photo-1542314831-068cd1dbfeeb?auto=format&fit=crop&q=80&w=800',
      expiry: 'Còn 5 giờ',
      color: 'rose'
    },
    {
      id: 3,
      title: 'Hội An Memories Show + Buffet',
      discount: 'MUA 1 TẶNG 1',
      tag: 'Bán chạy',
      price: '600.000đ',
      oldPrice: '1.200.000đ',
      image: 'https://images.unsplash.com/photo-1555432329-399fa3b732de?auto=format&fit=crop&q=80&w=800',
      expiry: 'Hết hạn hôm nay',
      color: 'amber'
    },
    {
      id: 4,
      title: 'Combo Kỳ Nghỉ Sang Trọng Nha Trang',
      discount: 'GIẢM 2TR',
      tag: 'Độc quyền',
      price: '5.900.000đ',
      oldPrice: '7.900.000đ',
      image: 'https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?auto=format&fit=crop&q=80&w=800',
      expiry: 'Còn 3 ngày',
      color: 'indigo'
    },
    {
      id: 5,
      title: 'Kỳ Quan Bà Nà Hills - Cầu Vàng',
      discount: 'GIẢM 25%',
      tag: 'Combo Hot',
      price: '1.150.000đ',
      oldPrice: '1.550.000đ',
      image: 'https://images.unsplash.com/photo-1559592413-7cea732639f5?auto=format&fit=crop&q=80&w=800',
      expiry: 'Còn 4 ngày',
      color: 'blue'
    },
    {
      id: 6,
      title: 'Du Thuyền Hạ Long 5 Sao',
      discount: 'FIXED 3TR',
      tag: 'Luxury',
      price: '2.990.000đ',
      oldPrice: '4.500.000đ',
      image: 'https://images.unsplash.com/photo-1559592413-7cea732639f5?auto=format&fit=crop&q=80&w=800',
      expiry: 'Sắp kết thúc',
      color: 'emerald'
    },
    {
      id: 7,
      title: 'Hành Trình Chinh Phục Phan Xi Păng',
      discount: 'ƯU ĐÃI NHÓM',
      tag: 'Adventure',
      price: '3.200.000đ',
      oldPrice: '3.800.000đ',
      image: 'https://images.unsplash.com/photo-1501785888041-af3ef285b470?auto=format&fit=crop&q=80&w=800',
      expiry: 'Còn 1 tuần',
      color: 'orange'
    }
  ];

  return (
    <div className="min-h-screen bg-[#F8FAFC] flex flex-col font-sans">
      <Navbar />
      
      <main className="flex-1 relative overflow-hidden">
        {/* Decorative Background Elements */}
        <div className="absolute top-0 right-0 w-[500px] h-[500px] bg-blue-100/30 rounded-full blur-[120px] -mr-64 -mt-64 pointer-events-none"></div>
        <div className="absolute bottom-0 left-0 w-[400px] h-[400px] bg-rose-50/50 rounded-full blur-[100px] -ml-48 -mb-48 pointer-events-none"></div>

        <div className="relative pt-16 pb-24">
          <div className="container mx-auto px-4 lg:px-12">
            
            {/* Header Section */}
            <div className="max-w-4xl mb-16">
              <motion.div 
                initial={{ opacity: 0, y: 30 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.6 }}
                className="flex items-center gap-2 mb-6"
              >
                  <span className="w-12 h-px bg-[#1EB4D4]"></span>
                  <span className="text-[12px] font-black uppercase tracking-[0.3em] text-[#1EB4D4] italic" style={{ fontFamily: "'Kalam', cursive" }}>Khuyến mãi đặc sắc</span>
              </motion.div>
              <motion.h1 
                initial={{ opacity: 0, y: 30 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.6, delay: 0.1 }}
                className="text-[42px] md:text-[54px] font-black text-slate-900 tracking-tighter leading-[1] mb-8"
              >
                Săn Ưu Đãi <br /><span className="text-[#1EB4D4]">Tour Du Lịch Xịn.</span>
              </motion.h1>
              <motion.p 
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.6, delay: 0.2 }}
                className="text-lg text-slate-500 font-medium max-w-xl leading-relaxed"
              >
                Tổng hợp những mã giảm giá, chương trình ưu đãi độc quyền từ các đối tác hàng đầu Việt Nam.
              </motion.p>
            </div>

            {/* Featured Hero Card */}
            <motion.div 
              initial={{ opacity: 0, scale: 0.95 }}
              animate={{ opacity: 1, scale: 1 }}
              transition={{ duration: 0.8, delay: 0.3 }}
              className="relative group rounded-[2.5rem] overflow-hidden mb-24 bg-slate-900 shadow-2xl min-h-[400px] flex flex-col justify-end p-8 lg:p-12 border border-white/5"
            >
              <img 
                  src="https://images.unsplash.com/photo-1476514525535-07fb3b4ae5f1?auto=format&fit=crop&q=80&w=1920" 
                  alt="Main Deal"
                  className="absolute inset-0 w-full h-full object-cover opacity-60 group-hover:scale-105 transition-transform duration-1000"
              />
              <div className="absolute inset-0 bg-gradient-to-t from-slate-900 via-slate-900/40 to-transparent"></div>
              
              <div className="relative z-10 max-w-2xl">
                  <div className="flex items-center gap-3 mb-6">
                      <div className="bg-[#1EB4D4] text-white p-2 rounded-xl animate-pulse">
                          <Zap size={20} fill="currentColor" />
                      </div>
                      <span className="text-white font-black text-xs uppercase tracking-[0.2em]">WEEKEND FLASH SALE</span>
                  </div>
                  <h2 className="text-3xl md:text-5xl font-black text-white mb-6 leading-tight">Mùa Hè Vẫy Gọi: <br />Giảm Tới 50% Tất Cả Dịch Vụ</h2>
                  <div className="flex flex-wrap items-center gap-6">
                      <button className="bg-[#1EB4D4] text-white px-10 py-3 rounded-full font-black hover:bg-[#19a7c5] transition-all transform hover:-translate-y-1 shadow-lg">Lấy Mã Ngay</button>
                      <p className="text-xs text-white/70 font-bold uppercase tracking-widest italic" style={{ fontFamily: "'Kalam', cursive" }}>Ưu đãi giới hạn cho 50 khách đầu tiên</p>
                  </div>
              </div>
            </motion.div>

            {/* Grid Layout */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-8">
              {deals.map((deal, idx) => (
                <motion.div 
                  key={deal.id}
                  initial={{ opacity: 0, y: 30 }}
                  whileInView={{ opacity: 1, y: 0 }}
                  viewport={{ once: true }}
                  transition={{ duration: 0.5, delay: idx * 0.1 }}
                  className="group flex flex-col h-full cursor-pointer"
                >
                  <div className="relative rounded-[2rem] overflow-hidden aspect-square shadow-xl transition-all duration-500 group-hover:shadow-[#1EB4D4]/20 border border-transparent group-hover:border-[#1EB4D4]/20">
                    <img 
                      src={deal.image} 
                      alt={deal.title} 
                      className="w-full h-full object-cover group-hover:scale-110 transition-transform duration-700" 
                    />
                    <div className="absolute inset-0 bg-gradient-to-t from-slate-900 via-transparent to-transparent opacity-60"></div>
                    
                    {/* Floating Badges */}
                    <div className="absolute top-4 left-4 flex flex-col gap-2">
                      <span className="bg-white/95 backdrop-blur-md text-slate-900 px-3 py-1 rounded-lg text-[8px] font-black uppercase tracking-widest shadow-xl">
                          {deal.tag}
                      </span>
                      <span className={`bg-[#1EB4D4] text-white px-3 py-1 rounded-lg text-[8px] font-black uppercase tracking-widest shadow-xl shadow-blue-500/20`}>
                          {deal.discount}
                      </span>
                    </div>

                    {/* Price Overlay */}
                    <div className="absolute bottom-6 left-6 right-6 flex items-end justify-between translate-y-4 opacity-0 group-hover:translate-y-0 group-hover:opacity-100 transition-all duration-500">
                      <div>
                          <p className="text-white/60 text-[10px] font-bold line-through">{deal.oldPrice}</p>
                          <p className="text-xl font-black text-white mt-0.5">{deal.price}</p>
                      </div>
                      <div className="w-10 h-10 bg-white text-slate-900 rounded-xl flex items-center justify-center shadow-xl group-hover:bg-[#1EB4D4] group-hover:text-white transition-all">
                          <ArrowRight size={18} />
                      </div>
                    </div>
                  </div>
                  
                  <div className="pt-5 px-2 text-center md:text-left">
                    <div className="flex items-center justify-center md:justify-start gap-2 text-rose-500 font-black text-[9px] uppercase tracking-widest mb-2">
                      <Flame size={12} fill="currentColor" /> {deal.expiry}
                    </div>
                    <h3 className="text-md font-bold text-slate-900 leading-[1.2] mb-2 group-hover:text-[#1EB4D4] transition-colors line-clamp-2 uppercase tracking-tight">
                      {deal.title}
                    </h3>
                  </div>
                </motion.div>
              ))}

              {/* Loyalty Trigger Card */}
              <motion.div 
                initial={{ opacity: 0, scale: 0.95 }}
                whileInView={{ opacity: 1, scale: 1 }}
                viewport={{ once: true }}
                className="bg-[#002B7F] rounded-[2rem] p-8 text-white flex flex-col justify-between shadow-2xl relative overflow-hidden group min-h-[300px]"
              >
                  <div className="absolute top-0 right-0 w-48 h-48 bg-white/10 rounded-full -mr-24 -mt-24 group-hover:scale-150 transition-transform duration-1000"></div>
                  <div className="relative z-10">
                      <div className="w-12 h-12 bg-white/20 rounded-2xl flex items-center justify-center mb-6">
                          <Gift size={24} />
                      </div>
                      <h3 className="text-lg font-black leading-[1.2] mb-3 uppercase">Đặc quyền hội viên</h3>
                      <p className="text-blue-100 font-medium text-[11px] leading-relaxed opacity-80">Giảm thêm 10% và tích lũy điểm thưởng không giới hạn.</p>
                  </div>
                  <button className="relative z-10 w-full mt-6 py-3 bg-[#1EB4D4] text-white rounded-xl font-black uppercase tracking-widest text-[9px] hover:bg-white hover:text-[#002B7F] transition-all">ĐĂNG KÝ NGAY</button>
              </motion.div>
            </div>
          </div>
        </div>
      </main>

      <Footer />
    </div>
  );
};

export default PromotionsPage;
