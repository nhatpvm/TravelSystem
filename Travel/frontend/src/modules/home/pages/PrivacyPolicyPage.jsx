import React from 'react';
import { Link } from 'react-router-dom';
import { Shield, FileText, Lock, ChevronRight } from 'lucide-react';
import Navbar from '../components/Navbar';
import Footer from '../components/Footer';

const SECTIONS = [
  {
    title: '1. Thu thập thông tin',
    content: 'Chúng tôi thu thập các thông tin bạn cung cấp trực tiếp, bao gồm: Họ tên, địa chỉ email, số điện thoại, thông tin thanh toán và thông tin hành khách khi bạn đặt vé hoặc tạo tài khoản trên nền tảng Turmet.'
  },
  {
    title: '2. Sử dụng thông tin',
    content: 'Thông tin của bạn được sử dụng để: xử lý đặt chỗ và thanh toán; gửi xác nhận và thông báo liên quan đến đơn hàng; cải thiện dịch vụ và trải nghiệm người dùng; đáp ứng yêu cầu pháp lý.'
  },
  {
    title: '3. Chia sẻ thông tin',
    content: 'Chúng tôi không bán thông tin cá nhân của bạn cho bên thứ ba. Thông tin chỉ được chia sẻ với: đối tác cung cấp dịch vụ (nhà xe, hàng không, khách sạn, đơn vị tour) để hoàn tất đặt chỗ; nhà cung cấp dịch vụ thanh toán (SePay) để xử lý giao dịch.'
  },
  {
    title: '4. Bảo mật dữ liệu',
    content: 'Turmet áp dụng các biện pháp bảo mật tiêu chuẩn công nghiệp để bảo vệ thông tin của bạn, bao gồm mã hóa SSL/TLS, kiểm soát truy cập nghiêm ngặt và giám sát hệ thống thường xuyên.'
  },
  {
    title: '5. Quyền của bạn',
    content: 'Bạn có quyền: truy cập và chỉnh sửa thông tin cá nhân; yêu cầu xóa tài khoản và toàn bộ dữ liệu; từ chối nhận thông báo tiếp thị; hạn chế xử lý dữ liệu trong một số trường hợp nhất định.'
  },
  {
    title: '6. Cookie',
    content: 'Chúng tôi sử dụng cookie để cải thiện trải nghiệm duyệt web. Bạn có thể tắt cookie trong cài đặt trình duyệt, tuy nhiên một số tính năng có thể không hoạt động đầy đủ.'
  },
  {
    title: '7. Liên hệ',
    content: 'Mọi thắc mắc về chính sách bảo mật, vui lòng liên hệ: Email: privacy@turmet.vn | Điện thoại: 1800 1234 | Địa chỉ: 123 Trần Duy Hưng, Cầu Giấy, Hà Nội.'
  },
];

export default function PrivacyPolicyPage() {
  return (
    <div className="min-h-screen bg-[#F0F4F8]">
      <Navbar />
      <div className="pt-32 pb-24">
        <div className="container mx-auto px-4 lg:px-12 max-w-3xl">
          <div className="text-center mb-10">
            <div className="w-14 h-14 bg-[#1EB4D4]/10 rounded-[1.5rem] flex items-center justify-center mx-auto mb-4">
              <Lock size={28} className="text-[#1EB4D4]" />
            </div>
            <h1 className="text-3xl font-black text-slate-900">Chính sách Bảo mật</h1>
            <p className="text-slate-400 text-sm mt-2">Cập nhật lần cuối: 01/01/2024</p>
          </div>

          <div className="bg-white rounded-[2.5rem] p-8 shadow-xl shadow-slate-100/60 mb-8">
            <p className="text-slate-600 font-medium leading-relaxed">
              Turmet Travel Platform cam kết bảo vệ quyền riêng tư của người dùng. Chính sách này mô tả cách chúng tôi thu thập, sử dụng và bảo vệ thông tin cá nhân của bạn khi sử dụng nền tảng của chúng tôi.
            </p>
          </div>

          <div className="space-y-4">
            {SECTIONS.map((s, i) => (
              <div key={i} className="bg-white rounded-2xl p-6 shadow-sm border border-slate-100">
                <h2 className="font-black text-slate-900 mb-3">{s.title}</h2>
                <p className="text-slate-600 text-sm leading-relaxed font-medium">{s.content}</p>
              </div>
            ))}
          </div>

          <div className="mt-8 text-center">
            <p className="text-sm text-slate-400 font-bold">
              Xem thêm: <Link to="/terms" className="text-[#1EB4D4] hover:underline">Điều khoản sử dụng</Link>
            </p>
          </div>
        </div>
      </div>
      <Footer />
    </div>
  );
}
