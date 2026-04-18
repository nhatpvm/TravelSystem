import React, { useRef, useState } from 'react';
import { Building2, FileCheck, Shield, ChevronRight, Bus, Hotel, Map, ArrowLeft, Upload, CheckCircle2, Train, Plane, Mail, Phone } from 'lucide-react';
import { Link } from 'react-router-dom';
import { submitTenantOnboarding } from '../../../services/tenancyService';

const SERVICE_OPTIONS = [
  { id: 'bus', icon: <Bus size={32} />, label: 'Vận tải', desc: 'Nhà xe, hãng xe khách' },
  { id: 'train', icon: <Train size={32} />, label: 'Đường sắt', desc: 'Đơn vị vận hành tàu' },
  { id: 'flight', icon: <Plane size={32} />, label: 'Hàng không', desc: 'Đại lý, hãng bay' },
  { id: 'hotel', icon: <Hotel size={32} />, label: 'Lưu trú', desc: 'Khách sạn, resort' },
  { id: 'tour', icon: <Map size={32} />, label: 'Lữ hành', desc: 'Công ty du lịch' },
];

const TenantOnboardingPage = () => {
  const [step, setStep] = useState(1);
  const [serviceType, setServiceType] = useState(null);
  const [form, setForm] = useState({
    businessName: '',
    taxCode: '',
    address: '',
    contactEmail: '',
    contactPhone: '',
  });
  const [legalDocument, setLegalDocument] = useState(null);
  const [result, setResult] = useState(null);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const fileInputRef = useRef(null);

  const steps = [
    { id: 1, label: 'Loại hình kinh doanh' },
    { id: 2, label: 'Thông tin cơ bản' },
    { id: 3, label: 'Hồ sơ pháp lý' },
    { id: 4, label: 'Hoàn tất' },
  ];

  function updateField(key, value) {
    setForm((prev) => ({ ...prev, [key]: value }));
  }

  function selectService(nextServiceType) {
    setServiceType(nextServiceType);
    setStep(2);
    setError('');
  }

  function openFileDialog() {
    fileInputRef.current?.click();
  }

  function handleFileChange(event) {
    setLegalDocument(event.target.files?.[0] || null);
    setError('');
  }

  async function handleSubmit() {
    if (!serviceType) {
      setError('Vui lòng chọn loại hình kinh doanh.');
      setStep(1);
      return;
    }

    if (!form.businessName || !form.taxCode || !form.address) {
      setError('Vui lòng nhập đầy đủ thông tin doanh nghiệp.');
      setStep(2);
      return;
    }

    if (!legalDocument) {
      setError('Vui lòng tải lên hồ sơ pháp lý.');
      return;
    }

    setSubmitting(true);
    setError('');

    try {
      const payload = new FormData();
      payload.append('serviceType', serviceType);
      payload.append('businessName', form.businessName);
      payload.append('taxCode', form.taxCode);
      payload.append('address', form.address);
      payload.append('contactEmail', form.contactEmail);
      payload.append('contactPhone', form.contactPhone);
      payload.append('legalDocument', legalDocument);

      const response = await submitTenantOnboarding(payload);
      setResult(response);
      setStep(4);
    } catch (err) {
      setError(err.message || 'Không thể gửi hồ sơ đăng ký.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="min-h-screen bg-slate-50 pt-12 pb-20">
      <div className="container mx-auto px-4 max-w-5xl">
        <div className="flex items-center gap-4 mb-12">
          <Link to="/" className="text-slate-400 hover:text-slate-900 transition-all flex items-center gap-1 font-bold text-sm">
            <ArrowLeft size={16} /> Quay lại trang chủ
          </Link>
        </div>

        <div className="flex items-center justify-between mb-16 relative px-8">
          <div className="absolute top-1/2 left-0 w-full h-1 bg-slate-200 -translate-y-1/2 z-0"></div>
          {steps.map((item) => (
            <div key={item.id} className="relative z-10 flex flex-col items-center">
              <div
                className={`
                  w-12 h-12 rounded-2xl flex items-center justify-center font-black transition-all duration-500
                  ${step >= item.id ? 'bg-blue-600 text-white shadow-xl shadow-blue-500/30 ring-4 ring-white' : 'bg-white text-slate-300 border-2 border-slate-100'}
                `}
              >
                {step > item.id ? <CheckCircle2 size={24} /> : item.id}
              </div>
              <span className={`text-[10px] font-black uppercase tracking-widest mt-3 ${step >= item.id ? 'text-blue-600' : 'text-slate-400'}`}>{item.label}</span>
            </div>
          ))}
        </div>

        <div className="bg-white rounded-[3rem] shadow-2xl shadow-slate-200/50 border border-slate-100 p-12 overflow-hidden relative">
          <div className="absolute top-0 right-0 w-64 h-64 bg-slate-50 rounded-full -mr-32 -mt-32 z-0"></div>

          <div className="relative z-10">
            {error && (
              <div className="mb-8 rounded-[1.75rem] border border-rose-100 bg-rose-50 px-6 py-4 text-sm font-bold text-rose-600">
                {error}
              </div>
            )}

            {step === 1 && (
              <div className="animate-in fade-in slide-in-from-bottom-4 duration-500">
                <h2 className="text-3xl font-black text-slate-900 mb-2">Bạn cung cấp dịch vụ gì?</h2>
                <p className="text-slate-500 font-medium mb-12 text-lg">Chọn loại hình kinh doanh chính của bạn để chúng tôi tối ưu hóa trải nghiệm.</p>

                <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-5 gap-6">
                  {SERVICE_OPTIONS.map((item) => (
                    <button
                      key={item.id}
                      onClick={() => selectService(item.id)}
                      className={`
                        group p-8 rounded-[2.5rem] border-2 text-left transition-all duration-300
                        ${serviceType === item.id ? 'border-blue-600 bg-blue-50/30' : 'border-slate-50 hover:border-slate-100 bg-slate-50/50'}
                      `}
                    >
                      <div className={`w-16 h-16 rounded-2xl flex items-center justify-center mb-6 transition-all ${serviceType === item.id ? 'bg-blue-600 text-white shadow-lg shadow-blue-500/20' : 'bg-white text-slate-400 shadow-sm'}`}>
                        {item.icon}
                      </div>
                      <h3 className="font-black text-slate-900 text-xl mb-2">{item.label}</h3>
                      <p className="text-sm text-slate-500 font-medium leading-relaxed">{item.desc}</p>
                      <div className="mt-8 flex items-center gap-2 text-blue-600 font-black text-xs uppercase tracking-widest opacity-0 group-hover:opacity-100 transition-all">
                        Chọn loại này <ChevronRight size={16} />
                      </div>
                    </button>
                  ))}
                </div>
              </div>
            )}

            {step === 2 && (
              <div className="animate-in fade-in slide-in-from-bottom-4 duration-500">
                <h2 className="text-3xl font-black text-slate-900 mb-2">Thông tin doanh nghiệp</h2>
                <p className="text-slate-500 font-medium mb-12">Vui lòng cung cấp thông tin chính xác để được phê duyệt nhanh nhất.</p>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
                  <div className="space-y-2">
                    <label className="text-[10px] font-black text-slate-400 uppercase tracking-widest pl-1">Tên thương hiệu / Công ty</label>
                    <div className="relative">
                      <Building2 className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-400" size={18} />
                      <input value={form.businessName} onChange={(event) => updateField('businessName', event.target.value)} type="text" placeholder="Ví dụ: Hoàng Long Bus" className="w-full pl-12 pr-6 py-4 bg-slate-50 border-none rounded-2xl focus:ring-2 focus:ring-blue-500 font-medium" />
                    </div>
                  </div>
                  <div className="space-y-2">
                    <label className="text-[10px] font-black text-slate-400 uppercase tracking-widest pl-1">Mã số thuế</label>
                    <div className="relative">
                      <FileCheck className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-400" size={18} />
                      <input value={form.taxCode} onChange={(event) => updateField('taxCode', event.target.value)} type="text" placeholder="Nhập mã số thuế" className="w-full pl-12 pr-6 py-4 bg-slate-50 border-none rounded-2xl focus:ring-2 focus:ring-blue-500 font-medium" />
                    </div>
                  </div>
                  <div className="md:col-span-2 space-y-2">
                    <label className="text-[10px] font-black text-slate-400 uppercase tracking-widest pl-1">Địa chỉ trụ sở chính</label>
                    <input value={form.address} onChange={(event) => updateField('address', event.target.value)} type="text" placeholder="Số nhà, đường, phường/xã, quận/huyện, tỉnh/thành" className="w-full px-6 py-4 bg-slate-50 border-none rounded-2xl focus:ring-2 focus:ring-blue-500 font-medium" />
                  </div>
                  <div className="space-y-2">
                    <label className="text-[10px] font-black text-slate-400 uppercase tracking-widest pl-1">Email liên hệ</label>
                    <div className="relative">
                      <Mail className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-400" size={18} />
                      <input value={form.contactEmail} onChange={(event) => updateField('contactEmail', event.target.value)} type="email" placeholder="partner@example.com" className="w-full pl-12 pr-6 py-4 bg-slate-50 border-none rounded-2xl focus:ring-2 focus:ring-blue-500 font-medium" />
                    </div>
                  </div>
                  <div className="space-y-2">
                    <label className="text-[10px] font-black text-slate-400 uppercase tracking-widest pl-1">Số điện thoại liên hệ</label>
                    <div className="relative">
                      <Phone className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-400" size={18} />
                      <input value={form.contactPhone} onChange={(event) => updateField('contactPhone', event.target.value)} type="text" placeholder="0901 000 001" className="w-full pl-12 pr-6 py-4 bg-slate-50 border-none rounded-2xl focus:ring-2 focus:ring-blue-500 font-medium" />
                    </div>
                  </div>
                </div>

                <div className="mt-12 flex justify-between">
                  <button onClick={() => setStep(1)} className="px-8 py-4 font-black text-slate-400 hover:text-slate-900 uppercase tracking-widest text-xs transition-all">Quay lại</button>
                  <button onClick={() => setStep(3)} className="bg-slate-900 text-white px-10 py-4 rounded-2xl font-black flex items-center gap-2 hover:bg-blue-600 transition-all shadow-xl hover:shadow-blue-500/20">Tiếp tục bước 3</button>
                </div>
              </div>
            )}

            {step === 3 && (
              <div className="animate-in fade-in slide-in-from-bottom-4 duration-500 text-center max-w-2xl mx-auto">
                <div className="w-20 h-20 bg-blue-50 text-blue-600 rounded-[2rem] flex items-center justify-center mx-auto mb-8">
                  <Upload size={32} />
                </div>
                <h2 className="text-3xl font-black text-slate-900 mb-2">Tải lên hồ sơ pháp lý</h2>
                <p className="text-slate-500 font-medium mb-12">Chúng tôi cần giấy phép kinh doanh để xác thực tài khoản của bạn.</p>

                <input ref={fileInputRef} type="file" accept=".pdf,.jpg,.jpeg,.png" onChange={handleFileChange} className="hidden" />
                <div onClick={openFileDialog} className="p-12 border-4 border-dashed border-slate-100 rounded-[3rem] bg-slate-50/30 group hover:border-blue-200 transition-all cursor-pointer">
                  <div className="flex flex-col items-center">
                    <Shield size={48} className="text-slate-200 group-hover:text-blue-100 mb-4 transition-all" />
                    <p className="font-black text-slate-900 mb-1">Click hoặc kéo thả file vào đây</p>
                    <p className="text-xs text-slate-400 font-bold uppercase tracking-widest">Hỗ trợ PDF, JPG, PNG (Tối đa 5MB)</p>
                  </div>
                </div>

                {legalDocument && (
                  <div className="mt-4 rounded-2xl bg-slate-50 border border-slate-100 px-5 py-4 text-left">
                    <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-1">Tệp đã chọn</p>
                    <p className="text-sm font-bold text-slate-900">{legalDocument.name}</p>
                  </div>
                )}

                <div className="mt-12 flex justify-between items-center">
                  <button onClick={() => setStep(2)} className="px-8 py-4 font-black text-slate-400 hover:text-slate-900 uppercase tracking-widest text-xs transition-all">Quay lại</button>
                  <button onClick={handleSubmit} disabled={submitting} className="bg-blue-600 text-white px-12 py-4 rounded-2xl font-black shadow-xl shadow-blue-500/20 hover:scale-105 transition-all disabled:opacity-70 disabled:hover:scale-100">
                    {submitting ? 'Đang gửi hồ sơ...' : 'Gửi hồ sơ đăng ký'}
                  </button>
                </div>
              </div>
            )}

            {step === 4 && (
              <div className="animate-in zoom-in duration-500 text-center py-12">
                <div className="w-24 h-24 bg-green-50 text-green-500 rounded-[2.5rem] flex items-center justify-center mx-auto mb-8 shadow-inner">
                  <CheckCircle2 size={48} />
                </div>
                <h2 className="text-4xl font-black text-slate-900 mb-4">Gửi đăng ký thành công!</h2>
                <p className="text-slate-500 font-medium mb-6 max-w-md mx-auto text-lg leading-relaxed">
                  Cảm ơn bạn đã đăng ký trở thành đối tác. Đội ngũ 2TMNY sẽ xem xét hồ sơ và liên hệ lại trong vòng 24h làm việc.
                </p>
                {result?.trackingCode && (
                  <div className="inline-flex items-center gap-2 px-5 py-3 rounded-2xl bg-slate-50 border border-slate-100 text-sm font-black text-slate-700 mb-10">
                    Mã hồ sơ: <span className="text-blue-600">{result.trackingCode}</span>
                  </div>
                )}
                <div>
                  <Link to="/" className="inline-flex items-center gap-2 bg-slate-900 text-white px-10 py-5 rounded-3xl font-black shadow-2xl hover:bg-blue-600 transition-all uppercase tracking-widest text-sm">
                    Về trang chủ <ChevronRight size={18} />
                  </Link>
                </div>
              </div>
            )}
          </div>
        </div>

        <div className="mt-12 flex flex-col md:flex-row items-center justify-center gap-12 text-center opacity-60">
          <div>
            <p className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] mb-2">Bạn cần hỗ trợ?</p>
            <p className="text-sm font-bold text-slate-900">hotro.partner@travelgsa.vn</p>
          </div>
          <div className="w-px h-8 bg-slate-200 hidden md:block"></div>
          <div>
            <p className="text-[10px] font-black text-slate-400 uppercase tracking-[0.2em] mb-2">Hotline 24/7</p>
            <p className="text-sm font-bold text-slate-900">1900 88xx</p>
          </div>
        </div>
      </div>
    </div>
  );
};

export default TenantOnboardingPage;
