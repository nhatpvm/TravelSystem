import React from 'react';
import { 
  TrendingUp, 
  Users, 
  ShoppingBag, 
  Activity, 
  ArrowUpRight, 
  ArrowDownRight, 
  Globe, 
  ShieldCheck, 
  AlertTriangle,
  Zap,
  RefreshCw
} from 'lucide-react';

const AdminDashboard = () => {
  const stats = [
    { label: 'Tổng giá trị giao dịch (GMV)', value: '42.8 tỷ', change: '+18.4%', trend: 'up', icon: <TrendingUp size={24} />, color: 'blue' },
    { label: 'Đối tác hoạt động', value: '156', change: '+12', trend: 'up', icon: <Globe size={24} />, color: 'indigo' },
    { label: 'Lượt đặt chỗ (Bookings)', value: '8,420', change: '+5.2%', trend: 'up', icon: <ShoppingBag size={24} />, color: 'emerald' },
    { label: 'Tỷ lệ thanh toán lỗi', value: '0.42%', change: '-0.05%', trend: 'down', icon: <AlertTriangle size={24} />, color: 'red' },
  ];

  const recentEvents = [
    { id: 1, type: 'payment', title: 'Thanh toán thành công BK0293XT', time: '2 phút trước', amount: '1.250.000đ', status: 'Success' },
    { id: 2, type: 'tenant', title: 'Đối tác mới "Homestay Đà Lạt" đăng ký', time: '15 phút trước', status: 'Pending' },
    { id: 3, type: 'settlement', title: 'Hoàn tất đối soát đợt SET-001', time: '1 giờ trước', amount: '240.000.000đ', status: 'Completed' },
  ];

  return (
    <div className="p-8 space-y-10">
      <div className="flex items-center justify-between">
         <div className="animate-in fade-in slide-in-from-left duration-700">
            <h1 className="text-4xl font-black text-slate-900 tracking-tight">Master Dashboard</h1>
            <p className="text-slate-500 font-medium mt-1 uppercase tracking-widest text-[10px]">Thời gian thực • Toàn hệ thống 2TMNY</p>
         </div>
         <div className="flex items-center gap-3 bg-white p-2 rounded-2xl border border-slate-100 shadow-sm">
            <span className="flex items-center gap-2 px-4 py-2 bg-blue-50 text-blue-600 rounded-xl font-black text-xs">
               <Zap size={14} fill="currentColor" /> LIVE MONITORING
            </span>
         </div>
      </div>

      {/* Global Stats Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-8">
        {stats.map((stat, i) => (
          <div key={i} className="bg-white p-8 rounded-[3rem] shadow-sm border border-slate-100 hover:shadow-2xl hover:shadow-blue-500/10 transition-all duration-500 group relative overflow-hidden">
             <div className={`absolute top-0 right-0 w-32 h-32 bg-${stat.color}-50 rounded-full -mr-16 -mt-16 transition-transform group-hover:scale-110`}></div>
             
             <div className={`w-14 h-14 rounded-2xl flex items-center justify-center bg-slate-900 text-white mb-6 shadow-lg shadow-slate-900/20 relative z-10 transition-all group-hover:bg-blue-600`}>
                {stat.icon}
             </div>
             
             <p className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] mb-2">{stat.label}</p>
             <div className="flex items-end gap-3">
                <p className="text-3xl font-black text-slate-900 tracking-tight">{stat.value}</p>
                <div className={`flex items-center gap-0.5 text-[10px] font-black px-2 py-0.5 rounded-lg mb-1 ${stat.trend === 'up' ? 'bg-green-100 text-green-600' : 'bg-red-100 text-red-600'}`}>
                   {stat.trend === 'up' ? <ArrowUpRight size={10} /> : <ArrowDownRight size={10} />}
                   {stat.change}
                </div>
             </div>
          </div>
        ))}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-10">
         {/* System Revenue Chart */}
         <div className="lg:col-span-2 bg-slate-900 rounded-[4rem] p-10 text-white shadow-2xl relative overflow-hidden group">
            <div className="absolute top-[-20%] right-[-10%] w-64 h-64 bg-blue-500/10 rounded-full blur-[100px] group-hover:bg-blue-500/20 transition-all"></div>
            
            <div className="flex items-center justify-between mb-12 relative z-10">
               <div>
                  <h3 className="text-xl font-black">Biểu đồ Doanh thu (GMV)</h3>
                  <p className="text-xs text-slate-400 font-medium mt-1">Chu kỳ 30 ngày gần nhất</p>
               </div>
               <div className="flex gap-2">
                  <button className="px-4 py-1.5 bg-white/10 rounded-xl text-[10px] font-bold hover:bg-white/20">7 Ngày</button>
                  <button className="px-4 py-1.5 bg-blue-600 rounded-xl text-[10px] font-bold">30 Ngày</button>
               </div>
            </div>

            <div className="h-64 flex items-end gap-3 px-2 relative z-10">
               {[40, 60, 45, 70, 85, 55, 90, 100, 75, 80, 65, 95].map((h, i) => (
                  <div key={i} className="flex-1 bg-gradient-to-t from-blue-600/20 to-blue-500 rounded-xl transition-all hover:to-blue-400 cursor-pointer group/bar relative" style={{ height: `${h}%` }}>
                      <div className="absolute -top-10 left-1/2 -translate-x-1/2 bg-white text-slate-900 px-3 py-1 rounded-xl text-[10px] font-black opacity-0 group-hover/bar:opacity-100 transition-all shadow-xl">{(h * 0.4).toFixed(1)}B</div>
                  </div>
               ))}
            </div>
            
            <div className="mt-8 pt-8 border-t border-white/5 flex items-center justify-between text-[10px] font-black text-slate-500 uppercase tracking-widest px-2 relative z-10">
                <span>01 MAR</span>
                <span>08 MAR</span>
                <span>15 MAR</span>
                <span>22 MAR</span>
                <span>31 MAR</span>
            </div>
         </div>

         {/* Real-time Activity Feed */}
         <div className="bg-white rounded-[4rem] p-10 shadow-sm border border-slate-100">
            <h3 className="text-xl font-black text-slate-900 mb-8 flex items-center gap-3">
               <Activity size={24} className="text-blue-600" /> Hoạt động mới
            </h3>
            
            <div className="space-y-8">
               {recentEvents.map((event) => (
                  <div key={event.id} className="flex items-start gap-4 group cursor-pointer">
                     <div className={`w-10 h-10 rounded-xl flex items-center justify-center shrink-0 transition-transform group-hover:scale-110 ${
                        event.type === 'payment' ? 'bg-green-50 text-green-600' : 
                        event.type === 'tenant' ? 'bg-blue-50 text-blue-600' : 'bg-slate-50 text-slate-600'
                     }`}>
                        {event.type === 'payment' ? <ShieldCheck size={20} /> : <Activity size={20} />}
                     </div>
                     <div className="flex-1 border-b border-slate-50 pb-6 group-last:border-none">
                        <div className="flex justify-between items-start">
                           <p className="text-sm font-bold text-slate-800 leading-tight group-hover:text-blue-600 transition-colors">{event.title}</p>
                           {event.amount && <p className="text-[10px] font-black text-slate-900 ml-2">{event.amount}</p>}
                        </div>
                        <p className="text-[10px] text-slate-400 font-bold uppercase tracking-widest mt-2">{event.time}</p>
                     </div>
                  </div>
               ))}
            </div>

            <button className="w-full mt-6 py-4 border-2 border-slate-50 rounded-2xl text-[10px] font-black text-slate-400 uppercase tracking-widest hover:bg-slate-50 hover:text-slate-900 transition-all">
               Xem tất cả thông báo
            </button>
         </div>
      </div>
    </div>
  );
};

export default AdminDashboard;
