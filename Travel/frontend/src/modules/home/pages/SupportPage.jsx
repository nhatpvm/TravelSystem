import React, { useEffect, useMemo, useState } from 'react';
import { motion } from 'framer-motion';
import { Link } from 'react-router-dom';
import {
  AlertCircle,
  ArrowRight,
  CheckCircle2,
  ChevronDown,
  Clock,
  Mail,
  MessageSquare,
  Phone,
  Plus,
  Search,
} from 'lucide-react';
import Navbar from '../components/Navbar';
import Footer from '../components/Footer';
import { useAuthSession } from '../../auth/hooks/useAuthSession';
import {
  createCustomerSupportTicket,
  listCustomerSupportTickets,
} from '../../../services/customerCommerceService';

const CATEGORIES = [
  'Vé xe / tàu / máy bay',
  'Khách sạn / tour',
  'Thanh toán & hoàn tiền',
  'Tài khoản & bảo mật',
  'Khác',
];

const STATUS_CFG = {
  1: { label: 'Mới', color: 'bg-blue-100 text-blue-700', icon: <Plus size={11} /> },
  2: { label: 'Đang xử lý', color: 'bg-amber-100 text-amber-700', icon: <Clock size={11} /> },
  3: { label: 'Đã giải quyết', color: 'bg-emerald-100 text-emerald-700', icon: <CheckCircle2 size={11} /> },
  4: { label: 'Đã đóng', color: 'bg-slate-100 text-slate-600', icon: <CheckCircle2 size={11} /> },
};

const FAQS = [
  {
    q: 'Tôi chưa nhận được vé dù đã thanh toán thành công?',
    a: 'Vui lòng kiểm tra hộp thư rác/spam. Nếu chưa thấy, hãy đợi thêm 5 đến 10 phút rồi mở lại trang đơn hàng. Trường hợp vẫn chưa có vé, bạn có thể gửi yêu cầu hỗ trợ ngay tại đây.',
  },
  {
    q: 'Làm thế nào để hủy hoặc hoàn tiền đơn hàng?',
    a: 'Bạn vào "Đơn hàng của tôi", chọn đơn tương ứng rồi bấm "Hủy / hoàn". Hệ thống sẽ hiển thị trạng thái xử lý trên chi tiết đơn hàng và trong trung tâm thông báo.',
  },
  {
    q: 'Tôi muốn xuất hóa đơn VAT?',
    a: 'Bạn có thể tích chọn xuất hóa đơn VAT ở trang checkout, hoặc gửi yêu cầu sau thanh toán trong khu vực "Hóa đơn VAT" của tài khoản.',
  },
];

const INITIAL_FORM = {
  subject: '',
  category: '',
  content: '',
  orderCode: '',
  contactEmail: '',
  contactPhone: '',
};

function formatTicketTime(item) {
  const value = item.lastActivityAt || item.updatedAt || item.createdAt;
  return value ? new Date(value).toLocaleString('vi-VN') : 'Vừa cập nhật';
}

export default function SupportPage() {
  const { isAuthenticated, isReady, user } = useAuthSession();
  const [tab, setTab] = useState('create');
  const [form, setForm] = useState(INITIAL_FORM);
  const [submittedTicket, setSubmittedTicket] = useState(null);
  const [tickets, setTickets] = useState([]);
  const [loadingTickets, setLoadingTickets] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [searchCode, setSearchCode] = useState('');
  const [openFaq, setOpenFaq] = useState(null);

  useEffect(() => {
    if (!user) {
      return;
    }

    setForm((current) => ({
      ...current,
      contactEmail: current.contactEmail || user.email || '',
      contactPhone: current.contactPhone || user.phoneNumber || '',
    }));
  }, [user]);

  async function loadTickets(ticketCode = '') {
    if (!isAuthenticated) {
      setTickets([]);
      return;
    }

    setLoadingTickets(true);
    setError('');

    try {
      const response = await listCustomerSupportTickets(ticketCode ? { ticketCode } : {});
      setTickets(Array.isArray(response) ? response : []);
    } catch (requestError) {
      setTickets([]);
      setError(requestError.message || 'Không thể tải danh sách yêu cầu hỗ trợ.');
    } finally {
      setLoadingTickets(false);
    }
  }

  useEffect(() => {
    if (!isReady || !isAuthenticated) {
      return;
    }

    loadTickets();
  }, [isAuthenticated, isReady]);

  const filteredTickets = useMemo(() => {
    if (!searchCode.trim()) {
      return tickets;
    }

    const keyword = searchCode.trim().toLowerCase();
    return tickets.filter((item) =>
      [item.ticketCode, item.subject, item.category, item.orderCode]
        .filter(Boolean)
        .some((value) => String(value).toLowerCase().includes(keyword)),
    );
  }, [searchCode, tickets]);

  async function handleSubmit(event) {
    event.preventDefault();
    setError('');
    setSuccess('');

    if (!isAuthenticated) {
      setError('Vui lòng đăng nhập để gửi yêu cầu hỗ trợ.');
      return;
    }

    setSubmitting(true);

    try {
      const created = await createCustomerSupportTicket({
        subject: form.subject.trim(),
        category: form.category,
        content: form.content.trim(),
        orderCode: form.orderCode.trim() || undefined,
        contactEmail: form.contactEmail.trim() || undefined,
        contactPhone: form.contactPhone.trim() || undefined,
      });

      setSubmittedTicket(created);
      setSuccess('Yêu cầu hỗ trợ đã được ghi nhận thành công.');
      setForm((current) => ({
        ...INITIAL_FORM,
        contactEmail: current.contactEmail,
        contactPhone: current.contactPhone,
      }));
      await loadTickets();
    } catch (requestError) {
      setError(requestError.message || 'Không thể gửi yêu cầu hỗ trợ.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="min-h-screen bg-[#F0F4F8]">
      <Navbar />
      <div className="pt-32 pb-24">
        <div className="container mx-auto px-4 lg:px-12 max-w-4xl">
          <div className="text-center mb-10">
            <div className="w-14 h-14 bg-[#1EB4D4]/10 rounded-[1.5rem] flex items-center justify-center mx-auto mb-4">
              <MessageSquare size={28} className="text-[#1EB4D4]" />
            </div>
            <h1 className="text-3xl font-black text-slate-900 tracking-tight">Trung tâm hỗ trợ</h1>
            <p className="text-slate-400 font-medium mt-1">Gửi yêu cầu thật, theo dõi trạng thái thật và nhận phản hồi ngay trong tài khoản của bạn.</p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-8">
            <a href="tel:18001234" className="flex items-center gap-4 p-5 bg-white rounded-2xl shadow-sm border border-slate-100 hover:shadow-md hover:border-[#1EB4D4]/20 transition-all group">
              <div className="w-11 h-11 bg-emerald-50 text-emerald-600 rounded-xl flex items-center justify-center"><Phone size={20} /></div>
              <div>
                <p className="font-black text-slate-900 text-sm">Hotline</p>
                <p className="text-[#1EB4D4] font-black group-hover:underline">1800 1234</p>
                <p className="text-xs text-slate-400">08:00 - 22:00 hằng ngày</p>
              </div>
            </a>
            <a href="mailto:support@2tmny.vn" className="flex items-center gap-4 p-5 bg-white rounded-2xl shadow-sm border border-slate-100 hover:shadow-md hover:border-[#1EB4D4]/20 transition-all group">
              <div className="w-11 h-11 bg-blue-50 text-blue-600 rounded-xl flex items-center justify-center"><Mail size={20} /></div>
              <div>
                <p className="font-black text-slate-900 text-sm">Email</p>
                <p className="text-[#1EB4D4] font-black group-hover:underline text-sm">support@2tmny.vn</p>
                <p className="text-xs text-slate-400">Phản hồi trong giờ làm việc</p>
              </div>
            </a>
          </div>

          <div className="flex bg-white rounded-2xl p-1 border border-slate-100 shadow-sm mb-6 gap-1">
            {[
              { v: 'create', l: 'Tạo yêu cầu' },
              { v: 'lookup', l: 'Tra cứu yêu cầu' },
              { v: 'faq', l: 'Câu hỏi thường gặp' },
            ].map((item) => (
              <button
                key={item.v}
                type="button"
                onClick={() => {
                  setTab(item.v);
                  setError('');
                  setSuccess('');
                }}
                className={`flex-1 py-3 rounded-xl text-xs font-black uppercase tracking-widest transition-all ${tab === item.v ? 'bg-slate-900 text-white shadow-md' : 'text-slate-400 hover:text-slate-700'}`}
              >
                {item.l}
              </button>
            ))}
          </div>

          {error ? (
            <div className="rounded-[1.75rem] border border-rose-100 bg-rose-50 px-6 py-4 text-sm font-bold text-rose-600 mb-6">
              {error}
            </div>
          ) : null}

          {success ? (
            <div className="rounded-[1.75rem] border border-emerald-100 bg-emerald-50 px-6 py-4 text-sm font-bold text-emerald-700 mb-6">
              {success}
            </div>
          ) : null}

          {tab === 'create' ? (
            <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} className="bg-white rounded-[2.5rem] p-8 shadow-xl shadow-slate-100/60">
              {!isReady ? (
                <div className="rounded-[2rem] bg-slate-50 px-6 py-10 text-center text-sm font-bold text-slate-400">
                  Đang kiểm tra trạng thái đăng nhập...
                </div>
              ) : submittedTicket ? (
                <div className="text-center py-12">
                  <div className="w-20 h-20 bg-emerald-50 rounded-full flex items-center justify-center mx-auto mb-4">
                    <CheckCircle2 size={40} className="text-emerald-500" />
                  </div>
                  <h2 className="text-2xl font-black text-slate-900 mb-2">Yêu cầu đã được gửi</h2>
                  <p className="text-slate-500 font-medium mb-1">
                    Mã yêu cầu: <span className="font-black text-[#1EB4D4]">{submittedTicket.ticketCode}</span>
                  </p>
                  <p className="text-slate-400 text-sm italic mb-8" style={{ fontFamily: "'Kalam', cursive" }}>
                    Hệ thống đã lưu yêu cầu của bạn và sẽ đẩy cập nhật về khu vực thông báo khi có phản hồi mới.
                  </p>
                  <button
                    type="button"
                    onClick={() => setSubmittedTicket(null)}
                    className="px-8 py-4 bg-slate-900 text-white rounded-2xl font-black text-sm uppercase tracking-widest hover:bg-[#1EB4D4] transition-all"
                  >
                    Tạo yêu cầu mới
                  </button>
                </div>
              ) : !isAuthenticated ? (
                <div className="text-center py-12 space-y-4">
                  <div className="w-16 h-16 bg-slate-50 rounded-full flex items-center justify-center mx-auto">
                    <AlertCircle size={28} className="text-slate-300" />
                  </div>
                  <h2 className="text-2xl font-black text-slate-900">Đăng nhập để gửi yêu cầu</h2>
                  <p className="text-slate-500 font-medium max-w-xl mx-auto">
                    Yêu cầu hỗ trợ sẽ được gắn với đơn hàng và tài khoản của bạn để chúng ta theo dõi xuyên suốt từ thanh toán đến hoàn tiền.
                  </p>
                  <Link to="/auth/login" state={{ from: { pathname: '/support' } }} className="inline-flex items-center gap-2 px-8 py-4 bg-slate-900 text-white rounded-2xl font-black text-sm uppercase tracking-widest hover:bg-[#1EB4D4] transition-all">
                    Đăng nhập để tiếp tục <ArrowRight size={16} />
                  </Link>
                </div>
              ) : (
                <form onSubmit={handleSubmit} className="space-y-5">
                  <h2 className="font-black text-slate-900 text-lg mb-4">Gửi yêu cầu hỗ trợ</h2>

                  <div>
                    <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] ml-1 mb-2 block">Chủ đề</label>
                    <input
                      required
                      value={form.subject}
                      onChange={(event) => setForm((current) => ({ ...current, subject: event.target.value }))}
                      placeholder="Mô tả ngắn vấn đề bạn đang gặp phải"
                      className="w-full bg-slate-50 rounded-2xl py-4 px-5 font-bold text-slate-900 text-sm border-2 border-transparent focus:border-[#1EB4D4]/30 focus:bg-white outline-none transition-all"
                    />
                  </div>

                  <div>
                    <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] ml-1 mb-2 block">Danh mục</label>
                    <select
                      required
                      value={form.category}
                      onChange={(event) => setForm((current) => ({ ...current, category: event.target.value }))}
                      className="w-full bg-slate-50 rounded-2xl py-4 px-5 font-bold text-slate-900 text-sm border-2 border-transparent focus:border-[#1EB4D4]/30 outline-none cursor-pointer"
                    >
                      <option value="">-- Chọn danh mục --</option>
                      {CATEGORIES.map((item) => (
                        <option key={item} value={item}>{item}</option>
                      ))}
                    </select>
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div>
                      <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] ml-1 mb-2 block">Mã đơn hàng (nếu có)</label>
                      <input
                        value={form.orderCode}
                        onChange={(event) => setForm((current) => ({ ...current, orderCode: event.target.value.toUpperCase() }))}
                        placeholder="ORD-2026..."
                        className="w-full bg-slate-50 rounded-2xl py-4 px-5 font-bold text-slate-900 text-sm border-2 border-transparent focus:border-[#1EB4D4]/30 focus:bg-white outline-none transition-all"
                      />
                    </div>
                    <div>
                      <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] ml-1 mb-2 block">Email liên hệ</label>
                      <input
                        type="email"
                        value={form.contactEmail}
                        onChange={(event) => setForm((current) => ({ ...current, contactEmail: event.target.value }))}
                        placeholder="ban@example.com"
                        className="w-full bg-slate-50 rounded-2xl py-4 px-5 font-bold text-slate-900 text-sm border-2 border-transparent focus:border-[#1EB4D4]/30 focus:bg-white outline-none transition-all"
                      />
                    </div>
                  </div>

                  <div>
                    <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] ml-1 mb-2 block">Số điện thoại liên hệ</label>
                    <input
                      value={form.contactPhone}
                      onChange={(event) => setForm((current) => ({ ...current, contactPhone: event.target.value }))}
                      placeholder="090..."
                      className="w-full bg-slate-50 rounded-2xl py-4 px-5 font-bold text-slate-900 text-sm border-2 border-transparent focus:border-[#1EB4D4]/30 focus:bg-white outline-none transition-all"
                    />
                  </div>

                  <div>
                    <label className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] ml-1 mb-2 block">Mô tả chi tiết</label>
                    <textarea
                      required
                      rows={5}
                      value={form.content}
                      onChange={(event) => setForm((current) => ({ ...current, content: event.target.value }))}
                      placeholder="Mô tả rõ vấn đề, thời điểm xảy ra lỗi hoặc yêu cầu hỗ trợ cụ thể của bạn"
                      className="w-full bg-slate-50 rounded-2xl py-4 px-5 font-bold text-slate-900 text-sm border-2 border-transparent focus:border-[#1EB4D4]/30 focus:bg-white outline-none transition-all resize-none"
                    />
                  </div>

                  <button
                    type="submit"
                    disabled={submitting}
                    className="w-full py-5 bg-gradient-to-r from-[#1EB4D4] to-[#002B7F] text-white rounded-[1.5rem] font-black text-sm uppercase tracking-widest shadow-xl hover:-translate-y-0.5 transition-all disabled:opacity-70 disabled:hover:translate-y-0 flex items-center justify-center gap-2"
                  >
                    {submitting ? 'Đang gửi yêu cầu...' : 'Gửi yêu cầu'} <ArrowRight size={16} />
                  </button>
                </form>
              )}
            </motion.div>
          ) : null}

          {tab === 'lookup' ? (
            <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} className="space-y-4">
              {!isReady ? (
                <div className="bg-white rounded-[2.5rem] p-12 shadow-xl text-center text-sm font-bold text-slate-400">
                  Đang kiểm tra trạng thái đăng nhập...
                </div>
              ) : !isAuthenticated ? (
                <div className="bg-white rounded-[2.5rem] p-12 shadow-xl text-center space-y-4">
                  <div className="w-16 h-16 bg-slate-50 rounded-full flex items-center justify-center mx-auto">
                    <Search size={28} className="text-slate-300" />
                  </div>
                  <h2 className="text-2xl font-black text-slate-900">Đăng nhập để tra cứu yêu cầu</h2>
                  <p className="text-slate-500 font-medium max-w-xl mx-auto">
                    Khi đăng nhập, bạn sẽ xem được toàn bộ yêu cầu hỗ trợ đã gửi, mã ticket và trạng thái xử lý mới nhất.
                  </p>
                  <Link to="/auth/login" state={{ from: { pathname: '/support' } }} className="inline-flex items-center gap-2 px-8 py-4 bg-slate-900 text-white rounded-2xl font-black text-sm uppercase tracking-widest hover:bg-[#1EB4D4] transition-all">
                    Đăng nhập để tra cứu <ArrowRight size={16} />
                  </Link>
                </div>
              ) : (
                <>
                  <div className="bg-white rounded-[2.5rem] p-6 shadow-xl">
                    <div className="flex gap-3">
                      <div className="flex-1 flex items-center gap-2 bg-slate-50 rounded-2xl px-5">
                        <Search size={16} className="text-slate-400" />
                        <input
                          value={searchCode}
                          onChange={(event) => setSearchCode(event.target.value.toUpperCase())}
                          placeholder="Nhập mã yêu cầu hỗ trợ hoặc từ khóa"
                          className="flex-1 py-4 bg-transparent outline-none font-bold text-slate-900 text-sm"
                        />
                      </div>
                      <button
                        type="button"
                        onClick={() => loadTickets(searchCode.trim())}
                        className="px-6 py-4 bg-slate-900 text-white rounded-2xl font-black text-sm uppercase tracking-widest hover:bg-[#1EB4D4] transition-all"
                      >
                        Tra cứu
                      </button>
                    </div>
                  </div>

                  {loadingTickets ? (
                    <div className="bg-white rounded-[2rem] p-10 text-center text-sm font-bold text-slate-400 shadow-sm border border-slate-100">
                      Đang tải danh sách yêu cầu hỗ trợ...
                    </div>
                  ) : filteredTickets.length === 0 ? (
                    <div className="bg-white rounded-[2rem] p-10 text-center text-sm font-bold text-slate-500 shadow-sm border border-slate-100">
                      Chưa có yêu cầu hỗ trợ nào phù hợp với bộ lọc hiện tại.
                    </div>
                  ) : (
                    <div className="space-y-3">
                      {filteredTickets.map((item) => {
                        const status = STATUS_CFG[item.status] || STATUS_CFG[1];

                        return (
                          <div key={item.ticketCode} className="bg-white rounded-2xl p-5 shadow-sm border border-slate-100 hover:shadow-md transition-all">
                            <div className="flex items-start justify-between gap-4">
                              <div>
                                <p className="font-black text-slate-900 text-sm">{item.subject}</p>
                                <p className="text-xs text-slate-400 font-bold mt-0.5">
                                  {item.ticketCode} • {item.category} • {formatTicketTime(item)}
                                </p>
                                {item.orderCode ? (
                                  <p className="text-[11px] font-bold text-slate-400 mt-1">Đơn hàng liên quan: {item.orderCode}</p>
                                ) : null}
                              </div>
                              <span className={`inline-flex items-center gap-1 px-2.5 py-1.5 rounded-xl text-[10px] font-black uppercase shrink-0 ${status.color}`}>
                                {status.icon} {status.label}
                              </span>
                            </div>

                            <p className="text-sm text-slate-600 font-medium leading-relaxed mt-3">{item.content}</p>

                            {item.hasUnreadStaffReply ? (
                              <p className="text-xs text-[#1EB4D4] font-black mt-3">Có phản hồi mới từ đội ngũ hỗ trợ.</p>
                            ) : null}

                            {item.resolutionNote ? (
                              <div className="mt-3 rounded-xl bg-emerald-50 border border-emerald-100 px-4 py-3 text-sm font-medium text-emerald-700">
                                {item.resolutionNote}
                              </div>
                            ) : null}
                          </div>
                        );
                      })}
                    </div>
                  )}
                </>
              )}
            </motion.div>
          ) : null}

          {tab === 'faq' ? (
            <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} className="space-y-3">
              {FAQS.map((item, index) => (
                <div key={item.q} className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
                  <button type="button" onClick={() => setOpenFaq(openFaq === index ? null : index)} className="w-full flex items-center justify-between p-5 text-left hover:bg-slate-50 transition-all">
                    <p className="font-black text-slate-900 text-sm pr-4">{item.q}</p>
                    <ChevronDown size={18} className={`text-slate-400 shrink-0 transition-transform ${openFaq === index ? 'rotate-180' : ''}`} />
                  </button>
                  {openFaq === index ? (
                    <motion.div initial={{ height: 0, opacity: 0 }} animate={{ height: 'auto', opacity: 1 }} className="px-5 pb-5 text-sm text-slate-600 font-medium border-t border-slate-100 pt-4">
                      {item.a}
                    </motion.div>
                  ) : null}
                </div>
              ))}
            </motion.div>
          ) : null}
        </div>
      </div>
      <Footer />
    </div>
  );
}
