import React from 'react';
import { Link } from 'react-router-dom';
import { FileText } from 'lucide-react';
import Navbar from '../components/Navbar';
import Footer from '../components/Footer';

const SECTIONS = [
  { title: '1. Chấp nhận điều khoản', content: 'Bằng việc truy cập và sử dụng nền tảng Turmet, bạn đồng ý bị ràng buộc bởi các điều khoản và điều kiện sử dụng này. Nếu bạn không đồng ý với bất kỳ phần nào, vui lòng không sử dụng dịch vụ của chúng tôi.' },
  { title: '2. Dịch vụ cung cấp', content: 'Turmet là nền tảng kết nối khách hàng với các nhà cung cấp dịch vụ du lịch bao gồm: vé xe khách, vé tàu hỏa, vé máy bay, tour du lịch và đặt phòng khách sạn. Chúng tôi đóng vai trò là trung gian và không trực tiếp cung cấp các dịch vụ vận chuyển hay lưu trú.' },
  { title: '3. Đặt chỗ và thanh toán', content: 'Mọi giao dịch đặt chỗ được xử lý theo thời gian thực. Giá vé và tình trạng chỗ có thể thay đổi tùy theo thời điểm. Thanh toán được xử lý qua cổng thanh toán an toàn SePay. Turmet không lưu trữ thông tin thẻ thanh toán của bạn.' },
  { title: '4. Chính sách hủy và hoàn tiền', content: 'Chính sách hủy và hoàn tiền áp dụng theo quy định của từng nhà cung cấp dịch vụ. Khách hàng cần đọc kỹ chính sách của từng sản phẩm trước khi đặt chỗ. Turmet sẽ hỗ trợ xử lý hoàn tiền trong phạm vi quy định cho phép.' },
  { title: '5. Trách nhiệm hạn chế', content: 'Turmet không chịu trách nhiệm về: sự chậm trễ, hủy chuyến hay thay đổi lịch trình do nhà cung cấp; thiệt hại gián tiếp phát sinh từ việc sử dụng dịch vụ; thông tin không chính xác do nhà cung cấp cung cấp.' },
  { title: '6. Quyền sở hữu trí tuệ', content: 'Toàn bộ nội dung, giao diện và thương hiệu trên nền tảng Turmet đều thuộc quyền sở hữu của Công ty TNHH Turmet Travel. Nghiêm cấm sao chép, phân phối hoặc sử dụng trái phép.' },
  { title: '7. Thay đổi điều khoản', content: 'Turmet có quyền thay đổi các điều khoản này bất kỳ lúc nào. Người dùng sẽ được thông báo qua email khi có thay đổi quan trọng. Việc tiếp tục sử dụng dịch vụ sau thay đổi đồng nghĩa với việc chấp nhận điều khoản mới.' },
];

export default function TermsPage() {
  return (
    <div className="min-h-screen bg-[#F0F4F8]">
      <Navbar />
      <div className="pt-32 pb-24">
        <div className="container mx-auto px-4 lg:px-12 max-w-3xl">
          <div className="text-center mb-10">
            <div className="w-14 h-14 bg-[#1EB4D4]/10 rounded-[1.5rem] flex items-center justify-center mx-auto mb-4">
              <FileText size={28} className="text-[#1EB4D4]" />
            </div>
            <h1 className="text-3xl font-black text-slate-900">Điều khoản Sử dụng</h1>
            <p className="text-slate-400 text-sm mt-2">Cập nhật lần cuối: 01/01/2024</p>
          </div>

          <div className="bg-white rounded-[2.5rem] p-8 shadow-xl shadow-slate-100/60 mb-8">
            <p className="text-slate-600 font-medium leading-relaxed">
              Vui lòng đọc kỹ các điều khoản và điều kiện sử dụng dưới đây trước khi sử dụng nền tảng Turmet Travel Platform. Khi sử dụng dịch vụ của chúng tôi, bạn đồng ý tuân thủ các điều khoản này.
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
              Xem thêm: <Link to="/privacy" className="text-[#1EB4D4] hover:underline">Chính sách Bảo mật</Link>
            </p>
          </div>
        </div>
      </div>
      <Footer />
    </div>
  );
}
