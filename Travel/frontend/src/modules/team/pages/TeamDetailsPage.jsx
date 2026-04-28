import React from 'react';
import Navbar from '../../home/components/Navbar';
import Footer from '../../home/components/Footer';
import { 
  ChevronRight, 
  Facebook, 
  Twitter, 
  Linkedin, 
  Youtube,
  ArrowLeft,
  ArrowRight
} from 'lucide-react';
import { motion } from 'framer-motion';
import { Link } from 'react-router-dom';

const skillData = [
  { label: "Kỹ năng ngoại ngữ", percentage: 86 },
  { label: "Hướng dẫn viên", percentage: 92 },
  { label: "Lập kế hoạch", percentage: 92 },
];

const relatedGuides = [
  { id: 1, name: "Marvin McKinney", role: "Tourist Guide", img: "https://images.unsplash.com/photo-1544005313-94ddf0286df2?q=80&w=400" },
  { id: 2, name: "Kathryn Murphy", role: "Tourist Guide", img: "https://images.unsplash.com/photo-1438761681033-6461ffad8d80?q=80&w=400" },
  { id: 3, name: "Bessie Cooper", role: "Tourist Guide", img: "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?q=80&w=400" },
  { id: 4, name: "Leslie Alexander", role: "Tourist Guide", img: "https://images.unsplash.com/photo-1494790108377-be9c29b29330?q=80&w=400" },
  { id: 5, name: "Eleanor Pena", role: "Tourist Guide", img: "https://images.unsplash.com/photo-1534528741775-53994a69daeb?q=80&w=400" },
  { id: 6, name: "Guy Hawkins", role: "Tourist Guide", img: "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?q=80&w=400" },
];

const TeamDetailsPage = () => {
  const [currentIndex, setCurrentIndex] = React.useState(0);
  const itemsPerPage = 3;
  const maxIndex = Math.max(0, relatedGuides.length - itemsPerPage);

  const nextSlide = () => {
    setCurrentIndex(prev => (prev >= maxIndex ? 0 : prev + 1));
  };

  const prevSlide = () => {
    setCurrentIndex(prev => (prev <= 0 ? maxIndex : prev - 1));
  };

  return (
    <div className="min-h-screen bg-white flex flex-col font-sans">
      <Navbar />

      {/* Breadcrumb Header Section */}
      <section className="relative h-[400px] flex items-center justify-center overflow-hidden">
        <div className="absolute inset-0 z-0">
          <img
            src="https://images.unsplash.com/photo-1521737711867-e3b97375f902?q=80&w=2070"
            alt="Chi tiết đội ngũ"
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
            Chi Tiết Đội Ngũ
          </motion.h1>

          <motion.div
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.2 }}
            className="inline-flex items-center gap-4 bg-white/20 backdrop-blur-md px-10 py-4 rounded-full border border-white/30 text-lg font-bold"
          >
            <Link to="/" className="text-white hover:text-[#1EB4D4] transition-colors">Trang chủ</Link>
            <ChevronRight size={20} className="text-[#1EB4D4]" />
            <Link to="/team" className="text-white hover:text-[#1EB4D4] transition-colors">Đội ngũ</Link>
            <ChevronRight size={20} className="text-[#1EB4D4]" />
            <span className="text-white">Chi tiết</span>
          </motion.div>
        </div>
      </section>

      {/* Profile Section */}
      <section className="py-24 bg-white">
        <div className="container mx-auto px-4 md:px-12 lg:px-24">
          
          {/* Main Profile Card */}
          <div className="bg-white border border-gray-100 rounded-[2.5rem] p-10 md:p-16 mb-24 shadow-sm relative overflow-hidden group">
             {/* Decorative pattern in corner */}
             <div className="absolute bottom-0 right-0 p-8 opacity-10">
                <svg width="200" height="200" viewBox="0 0 200 200" fill="none">
                    <circle cx="100" cy="100" r="80" stroke="#1EB4D4" strokeWidth="2" strokeDasharray="10 10" />
                    <circle cx="100" cy="100" r="40" stroke="#1EB4D4" strokeWidth="1" />
                </svg>
             </div>

             <div className="flex flex-col lg:flex-row gap-12 relative z-10">
                {/* Photo */}
                <div className="w-full lg:w-[380px] h-[480px] rounded-[2rem] overflow-hidden shadow-2xl shrink-0">
                   <img 
                      src="https://images.unsplash.com/photo-1540569014015-19a7be504e3a?q=80&w=800" 
                      alt="Savannah Nguyen" 
                      className="w-full h-full object-cover"
                   />
                </div>

                {/* Content */}
                <div className="flex-1 flex flex-col justify-center">
                   <h2 className="text-4xl md:text-5xl font-black text-gray-900 mb-2">Savannah Nguyen</h2>
                   <p className="text-[#1EB4D4] font-black text-sm uppercase tracking-widest mb-8">Hướng dẫn viên du lịch</p>
                   
                   <p className="text-gray-400 font-medium leading-relaxed mb-10 max-w-2xl">
                     Adipiscing elit. Mauris viverra nisl quis mollis laoreet. Ut eget lacus a felis accumsan pharetra in dignissim enim. In amet odio mollis uma aliquet volutpat. Sed bibendum nisl vehicula imperdiet imperdiet, augue massa fringilla.
                   </p>

                   {/* Quick Info Grid */}
                   <div className="grid grid-cols-1 md:grid-cols-3 gap-8 mb-10 pb-10 border-b border-gray-100">
                      <div>
                         <p className="text-[#1EB4D4] text-xs font-black uppercase tracking-wider mb-1">Kinh nghiệm:</p>
                         <p className="text-gray-900 font-black">10 Năm</p>
                      </div>
                      <div>
                         <p className="text-[#1EB4D4] text-xs font-black uppercase tracking-wider mb-1">Vị trí:</p>
                         <p className="text-gray-900 font-black">Người hướng dẫn</p>
                      </div>
                      <div>
                         <p className="text-[#1EB4D4] text-xs font-black uppercase tracking-wider mb-1">Điện thoại:</p>
                         <p className="text-gray-900 font-black">+208-555-0112</p>
                      </div>
                   </div>

                   {/* Social Buttons */}
                   <div className="flex gap-4">
                      {[Facebook, Twitter, Linkedin, Youtube].map((Icon, i) => (
                        <button key={i} className="w-12 h-12 rounded-xl bg-[#F8FBFB] border border-gray-100 flex items-center justify-center text-gray-400 hover:bg-[#1EB4D4] hover:text-white hover:border-[#1EB4D4] transition-all shadow-sm">
                           <Icon size={20} />
                        </button>
                      ))}
                   </div>
                </div>
             </div>
          </div>

          {/* Professional Info Section */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-20 mb-32 items-center">
             <div className="space-y-8">
                <h3 className="text-3xl font-black text-gray-900">Thông tin chuyên môn</h3>
                <p className="text-gray-400 font-medium leading-[1.8]">
                   Consectetur adipisicing elit, sed do eiusmod tempor is incididunt ut labore et dolore of magna aliqua. Ut enim ad minim veniam, made of owl the quis nostrud exercitation ullamco laboris nisi ut aliquip.
                </p>
                <p className="text-gray-400 font-medium leading-[1.8]">
                   The is ipsum dolor sit amet consectetur adipisicing elit. Fusce eleifend porta arcu in hac augu habitasse the is platea augue thelorem turpoidictumst. In lacus libero faucibus.
                </p>
             </div>

             {/* Skills Progress Bars */}
             <div className="space-y-10">
                {skillData.map((skill, idx) => (
                  <div key={idx} className="space-y-4">
                    <div className="flex justify-between items-center font-black">
                       <span className="text-gray-900">{skill.label}</span>
                       <span className="text-[#1EB4D4]">{skill.percentage}%</span>
                    </div>
                    <div className="h-2 w-full bg-gray-100 rounded-full overflow-hidden">
                       <motion.div 
                          initial={{ width: 0 }}
                          whileInView={{ width: `${skill.percentage}%` }}
                          transition={{ duration: 1, ease: "easeOut" }}
                          className="h-full bg-[#1EB4D4]"
                       />
                    </div>
                  </div>
                ))}
             </div>
          </div>

          <div className="h-px bg-gray-100 w-full mb-32"></div>

          {/* Related Guider Section */}
          <div className="overflow-hidden">
             <div className="flex items-center justify-between mb-16">
                <h3 className="text-3xl font-black text-gray-900 text-center md:text-left">Hướng dẫn viên liên quan</h3>
                <div className="flex gap-4">
                   <button 
                    onClick={prevSlide}
                    className="w-12 h-12 rounded-full border border-gray-100 flex items-center justify-center text-gray-400 hover:bg-[#1EB4D4] hover:text-white transition-all shadow-sm"
                   >
                      <ArrowLeft size={20} />
                   </button>
                   <button 
                    onClick={nextSlide}
                    className="w-12 h-12 rounded-full bg-[#1EB4D4] flex items-center justify-center text-white hover:bg-gray-900 transition-all shadow-xl shadow-[#1EB4D4]/30"
                   >
                      <ArrowRight size={20} />
                   </button>
                </div>
             </div>

             <div className="relative">
                <motion.div 
                  animate={{ x: `-${currentIndex * (100 / itemsPerPage)}%` }}
                  transition={{ type: "spring", stiffness: 300, damping: 30 }}
                  className="flex gap-8"
                >
                    {relatedGuides.map((guide) => (
                      <motion.div 
                        key={guide.id}
                        className="min-w-[calc(33.333%-22px)] group"
                      >
                        <div className="relative bg-[#F8FBFB] rounded-[2.5rem] p-4 pb-20 overflow-hidden mb-[-80px] z-0">
                           <div className="aspect-[4/5] rounded-[2rem] overflow-hidden">
                              <img src={guide.img} alt={guide.name} className="w-full h-full object-cover transition-transform duration-700 group-hover:scale-110" />
                           </div>
                        </div>
                        
                        <div className="bg-white mx-8 rounded-[2rem] p-8 shadow-xl relative z-10 text-center border border-gray-50 flex flex-col items-center transition-all duration-500 group-hover:bg-[#1EB4D4] group-hover:-translate-y-4">
                           <h4 className="text-xl font-black text-gray-900 mb-1 group-hover:text-white transition-colors">{guide.name}</h4>
                           <p className="text-[#1EB4D4] font-black text-xs uppercase tracking-wider mb-6 group-hover:text-white opacity-90 transition-colors uppercase">{guide.role}</p>
                           
                           <div className="flex gap-3">
                              {[Facebook, Twitter, Linkedin].map((Icon, i) => (
                                <button key={i} className="w-8 h-8 rounded-lg bg-[#F8FBFB] flex items-center justify-center text-[#1EB4D4] hover:bg-white hover:text-gray-900 transition-all group-hover:bg-white/20 group-hover:text-white">
                                   <Icon size={14} />
                                </button>
                              ))}
                           </div>
                        </div>
                      </motion.div>
                    ))}
                </motion.div>
             </div>
          </div>

        </div>
      </section>

      <Footer />
    </div>
  );
};

export default TeamDetailsPage;
