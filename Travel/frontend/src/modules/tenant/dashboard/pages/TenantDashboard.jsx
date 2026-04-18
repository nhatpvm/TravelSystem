import React from 'react';

const TenantDashboard = () => {
  return (
    <div className="p-6">
      <h1 className="text-3xl font-bold text-slate-800 mb-6">Partner Dashboard</h1>
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        <div className="bg-gradient-to-br from-blue-600 to-blue-700 p-6 rounded-2xl shadow-lg text-white">
          <p className="opacity-80 text-sm font-medium">Revenue (Monthly)</p>
          <p className="text-3xl font-bold mt-1">$42,500</p>
          <div className="mt-4 bg-white/20 rounded-lg p-2 text-xs">
            Daily average: $1,416
          </div>
        </div>
        <div className="bg-white p-6 rounded-2xl shadow-sm border border-slate-100">
          <p className="text-slate-500 text-sm font-medium">Ticket Booking</p>
          <p className="text-2xl font-bold text-slate-900 mt-1">1,240</p>
          <div className="mt-2 flex items-center gap-2">
            <span className="text-xs font-bold px-2 py-0.5 bg-green-100 text-green-700 rounded-full">+18%</span>
            <span className="text-xs text-slate-400">vs last period</span>
          </div>
        </div>
        <div className="bg-white p-6 rounded-2xl shadow-sm border border-slate-100">
          <p className="text-slate-500 text-sm font-medium">Load Factor / Occupancy</p>
          <div className="mt-4 w-full bg-slate-100 h-2 rounded-full overflow-hidden">
             <div className="bg-blue-600 h-full w-[75%] rounded-full"></div>
          </div>
          <p className="text-right text-sm font-bold mt-1 text-slate-700">75%</p>
        </div>
      </div>

      <div className="mt-8 grid grid-cols-1 lg:grid-cols-2 gap-8">
        <div className="bg-white rounded-2xl border border-slate-100 p-6">
          <h2 className="text-lg font-bold text-slate-800 mb-4">Latest Bookings</h2>
          <div className="overflow-x-auto">
            <table className="w-full text-left">
              <thead>
                <tr className="text-slate-400 text-xs font-bold border-b border-slate-50">
                  <th className="pb-3">GUEST</th>
                  <th className="pb-3">SERVICE</th>
                  <th className="pb-3">STATUS</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-50">
                <tr className="text-sm">
                  <td className="py-3 font-medium">Nguyen Van A</td>
                  <td className="py-3">Hanoi - Sapa (Bus)</td>
                  <td className="py-3"><span className="px-2 py-1 bg-green-100 text-green-700 rounded-lg text-xs">PAID</span></td>
                </tr>
                <tr className="text-sm">
                  <td className="py-3 font-medium">Tran Thi B</td>
                  <td className="py-3">Studio Room (Hotel)</td>
                  <td className="py-3"><span className="px-2 py-1 bg-amber-100 text-amber-700 rounded-lg text-xs">PENDING</span></td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  );
};

export default TenantDashboard;
