import React from 'react';
import Navbar from '../../home/components/Navbar';
import Footer from '../../home/components/Footer';
import { 
    ChevronRight, 
    MapPin, 
    Mail, 
    Phone,
    ArrowRight
} from 'lucide-react';
import { motion } from 'framer-motion';
import { Link } from 'react-router-dom';

const ContactPage = () => {
    return (
        <div className="min-h-screen bg-white flex flex-col font-sans">
            <Navbar />

            {/* Breadcrumb Header Section */}
            <section className="relative h-[400px] flex items-center justify-center overflow-hidden">
                <div className="absolute inset-0 z-0">
                    <img
                        src="https://images.unsplash.com/photo-1516738901171-8eb4fc13bd20?q=80&w=2070"
                        alt="Contact Us"
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
                        Liên hệ
                    </motion.h1>

                    <motion.div
                        initial={{ opacity: 0, y: 10 }}
                        animate={{ opacity: 1, y: 0 }}
                        transition={{ delay: 0.2 }}
                        className="inline-flex items-center gap-4 bg-white/20 backdrop-blur-md px-10 py-4 rounded-full border border-white/30 text-lg font-bold"
                    >
                        <Link to="/" className="text-white hover:text-[#1EB4D4] transition-colors">Trang chủ</Link>
                        <ChevronRight size={20} className="text-[#1EB4D4]" />
                        <span className="text-white">Liên hệ</span>
                    </motion.div>
                </div>
            </section>

            {/* Contact Cards Section */}
            <section className="py-24 bg-[#F8FBFB]">
                <div className="container mx-auto px-4 md:px-12 lg:px-24">
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-8 mb-20">
                        {/* Address Card */}
                        <motion.div 
                            initial={{ opacity: 0, y: 20 }}
                            whileInView={{ opacity: 1, y: 0 }}
                            className="bg-white p-12 rounded-[2.5rem] shadow-sm border border-gray-100 flex flex-col items-center text-center group hover:shadow-xl transition-all duration-500"
                        >
                            <div className="w-20 h-20 bg-[#F0F9FB] text-[#1EB4D4] rounded-full flex items-center justify-center mb-8 group-hover:bg-[#1EB4D4] group-hover:text-white transition-colors">
                                <MapPin size={32} />
                            </div>
                            <h3 className="text-2xl font-black text-gray-900 mb-4">Địa chỉ của chúng tôi</h3>
                            <p className="text-gray-500 font-bold leading-loose">
                                2464 Royal Ln. Mesa, New Jersey <br /> 45463
                            </p>
                        </motion.div>

                        {/* Email Card */}
                        <motion.div 
                            initial={{ opacity: 0, y: 20 }}
                            whileInView={{ opacity: 1, y: 0 }}
                            transition={{ delay: 0.1 }}
                            className="bg-[#1EB4D4] p-12 rounded-[2.5rem] shadow-xl text-white flex flex-col items-center text-center scale-105 z-10"
                        >
                            <div className="w-20 h-20 bg-white/20 text-white rounded-full flex items-center justify-center mb-8">
                                <Mail size={32} />
                            </div>
                            <h3 className="text-2xl font-black mb-4 uppercase tracking-tighter">Info@Tripco.Com</h3>
                            <p className="text-white/80 font-bold leading-loose">
                                Gửi email cho chúng tôi <br /> bất cứ lúc nào cho bất kỳ thắc mắc nào.
                            </p>
                        </motion.div>

                        {/* Phone Card */}
                        <motion.div 
                            initial={{ opacity: 0, y: 20 }}
                            whileInView={{ opacity: 1, y: 0 }}
                            transition={{ delay: 0.2 }}
                            className="bg-[#D9F0F4] p-12 rounded-[2.5rem] shadow-sm border border-[#C2E7ED] flex flex-col items-center text-center group hover:shadow-xl transition-all duration-500"
                        >
                            <div className="w-20 h-20 bg-[#1EB4D4] text-white rounded-full flex items-center justify-center mb-8 group-hover:bg-gray-900 transition-colors">
                                <Phone size={32} />
                            </div>
                            <h3 className="text-2xl font-black text-gray-900 mb-4">Hot: +208-666-0112</h3>
                            <p className="text-gray-600 font-bold leading-loose">
                                Gọi cho chúng tôi để được tư vấn <br /> phục vụ 24/7 cho bạn.
                            </p>
                        </motion.div>
                    </div>
                </div>
            </section>

            {/* Form & Map Section */}
            <section className="bg-gray-900 py-24 relative overflow-hidden">
                <div className="container mx-auto px-4 md:px-12 lg:px-24">
                    <div className="flex flex-col lg:flex-row gap-16 items-center">
                        
                        {/* Form Side */}
                        <div className="w-full lg:w-1/2">
                            <div className="mb-12">
                                <span className="text-[#1EB4D4] font-black uppercase tracking-widest text-sm mb-4 block">Liên hệ với chúng tôi</span>
                                <h2 className="text-4xl md:text-5xl font-black text-white leading-tight">
                                    Gửi tin nhắn <br /> Bất cứ khi nào
                                </h2>
                            </div>

                            <form className="space-y-6">
                                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                                    <div className="relative">
                                        <input 
                                            type="text" 
                                            placeholder="Tên của bạn" 
                                            className="w-full bg-[#1A1D1F] border border-white/10 py-5 px-8 rounded-2xl font-bold text-white outline-none focus:ring-2 focus:ring-[#1EB4D4]/30 placeholder:text-gray-500" 
                                        />
                                    </div>
                                    <div className="relative">
                                        <input 
                                            type="email" 
                                            placeholder="Email của bạn" 
                                            className="w-full bg-[#1A1D1F] border border-white/10 py-5 px-8 rounded-2xl font-bold text-white outline-none focus:ring-2 focus:ring-[#1EB4D4]/30 placeholder:text-gray-500" 
                                        />
                                    </div>
                                </div>
                                <div className="relative">
                                    <textarea 
                                        rows="6" 
                                        placeholder="Tin nhắn của bạn" 
                                        className="w-full bg-[#1A1D1F] border border-white/10 py-5 px-8 rounded-2xl font-bold text-white outline-none focus:ring-2 focus:ring-[#1EB4D4]/30 placeholder:text-gray-500 resize-none"
                                    ></textarea>
                                </div>
                                <button className="bg-[#1EB4D4] hover:bg-white hover:text-gray-900 text-white px-10 py-5 rounded-2xl font-black text-lg transition-all flex items-center gap-3 group">
                                    Gửi tin nhắn <ArrowRight size={20} className="group-hover:translate-x-1 transition-transform" />
                                </button>
                            </form>
                        </div>

                        {/* Map Side */}
                        <div className="w-full lg:w-1/2 h-[600px] rounded-[2.5rem] overflow-hidden shadow-2xl relative">
                            <iframe 
                                title="Google Map"
                                src="https://www.google.com/maps/embed?pb=!1m18!1m12!1m3!1d157397.64724213706!2d144.96328!3d-37.813627!2m3!1f0!2f0!3f0!3m2!1i1024!2i768!4f13.1!3m3!1m2!1s0x6ad642af0f11fd81%3A0x5045675218ce7e33!2sMelbourne%20VIC%2C%20Australia!5e0!3m2!1sen!2s!4v1709425000000!5m2!1sen!2s" 
                                width="100%" 
                                height="100%" 
                                style={{ border: 0 }} 
                                allowFullScreen="" 
                                loading="lazy" 
                                referrerPolicy="no-referrer-when-downgrade"
                                className="grayscale opacity-80"
                            ></iframe>
                            <div className="absolute inset-x-0 bottom-0 h-24 bg-gradient-to-t from-gray-900 to-transparent pointer-events-none"></div>
                        </div>

                    </div>
                </div>
            </section>

            <Footer />
        </div>
    );
};

export default ContactPage;
