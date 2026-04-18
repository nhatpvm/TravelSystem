import React, { useEffect, useMemo, useState } from 'react';
import { motion } from 'framer-motion';
import { Heart, MapPin, Trash2, ArrowRight, Compass, Hotel, Plane, Train, Bus } from 'lucide-react';
import { Link } from 'react-router-dom';
import { deleteWishlistItem, listWishlistItems } from '../../../services/customerCommerceService';
import { getCustomerLocale } from '../../../services/customerPreferences';
import {
  formatCustomerProductLabel,
  getCustomerProductPath,
} from '../../booking/utils/customerCommerce';
import { formatCurrency } from '../../tenant/train/utils/presentation';

function getProductIcon(productType) {
  switch (Number(productType || 0)) {
    case 1:
      return <Bus size={18} />;
    case 2:
      return <Train size={18} />;
    case 3:
      return <Plane size={18} />;
    case 4:
      return <Hotel size={18} />;
    default:
      return <Compass size={18} />;
  }
}

function formatSavedAt(value) {
  if (!value) {
    return 'Vừa cập nhật';
  }

  return new Date(value).toLocaleString(getCustomerLocale());
}

export default function WishlistPage() {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [removingId, setRemovingId] = useState('');

  async function loadWishlist() {
    setLoading(true);
    setError('');

    try {
      const response = await listWishlistItems();
      setItems(Array.isArray(response) ? response : []);
    } catch (requestError) {
      setItems([]);
      setError(requestError.message || 'Không thể tải danh sách yêu thích.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadWishlist();
  }, []);

  const stats = useMemo(() => ({
    total: items.length,
    transport: items.filter((item) => [1, 2, 3].includes(Number(item.productType))).length,
    hotel: items.filter((item) => Number(item.productType) === 4).length,
    tour: items.filter((item) => Number(item.productType) === 5).length,
  }), [items]);

  async function handleRemove(id) {
    setRemovingId(id);
    setError('');
    setSuccess('');

    try {
      await deleteWishlistItem(id);
      setSuccess('Đã xóa khỏi danh sách yêu thích.');
      await loadWishlist();
    } catch (requestError) {
      setError(requestError.message || 'Không thể xóa mục yêu thích.');
    } finally {
      setRemovingId('');
    }
  }

  return (
    <motion.div
      initial={{ opacity: 0, y: 24 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.55, ease: [0.22, 1, 0.36, 1] }}
      className="space-y-6"
    >
      <div className="bg-white rounded-[2.5rem] p-8 shadow-xl shadow-slate-100/60 flex flex-col md:flex-row md:items-center justify-between gap-6">
        <div>
          <div className="flex items-center gap-2 mb-1">
            <Heart size={14} className="text-rose-500" />
            <span className="text-[10px] font-black text-rose-500 uppercase tracking-[0.25em]">Yêu thích</span>
          </div>
          <h1 className="text-3xl font-black text-slate-900 tracking-tight">Danh sách yêu thích</h1>
          <p className="text-slate-400 text-sm font-medium mt-1">
            Lưu lại dịch vụ bạn muốn quay lại so sánh hoặc chốt booking sau.
          </p>
        </div>
        <div className="grid grid-cols-3 gap-3 w-full md:w-auto">
          {[
            { label: 'Tổng mục', value: stats.total },
            { label: 'Khách sạn', value: stats.hotel },
            { label: 'Tour', value: stats.tour },
          ].map((item) => (
            <div key={item.label} className="bg-slate-50 rounded-2xl px-5 py-4 min-w-[110px]">
              <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest">{item.label}</p>
              <p className="text-2xl font-black text-slate-900 mt-2">{item.value}</p>
            </div>
          ))}
        </div>
      </div>

      {error ? (
        <div className="rounded-[1.75rem] border border-rose-100 bg-rose-50 px-6 py-4 text-sm font-bold text-rose-600">
          {error}
        </div>
      ) : null}

      {success ? (
        <div className="rounded-[1.75rem] border border-emerald-100 bg-emerald-50 px-6 py-4 text-sm font-bold text-emerald-700">
          {success}
        </div>
      ) : null}

      {loading ? (
        <div className="bg-white rounded-[2rem] p-12 text-center text-sm font-bold text-slate-400 shadow-xl shadow-slate-100/60">
          Đang tải danh sách yêu thích...
        </div>
      ) : items.length === 0 ? (
        <div className="bg-white rounded-[2rem] p-12 text-center shadow-xl shadow-slate-100/60">
          <div className="w-16 h-16 rounded-full bg-rose-50 flex items-center justify-center mx-auto mb-4">
            <Heart size={28} className="text-rose-300" />
          </div>
          <h3 className="text-xl font-black text-slate-900 mb-2">Chưa có mục yêu thích nào</h3>
          <p className="text-sm font-medium text-slate-400 mb-6">Bạn có thể lưu bus, train, flight, hotel hoặc tour từ trang chi tiết và trang kết quả.</p>
          <Link to="/" className="inline-flex items-center gap-2 px-6 py-3 bg-slate-900 text-white rounded-2xl text-xs font-black uppercase tracking-widest hover:bg-[#1EB4D4] transition-all">
            Khám phá dịch vụ <ArrowRight size={14} />
          </Link>
        </div>
      ) : (
        <div className="grid grid-cols-1 xl:grid-cols-2 gap-5">
          {items.map((item, index) => {
            const targetPath = item.targetUrl || getCustomerProductPath(item.productType, item.targetId, item.targetSlug);

            return (
              <motion.div
                key={item.id}
                initial={{ opacity: 0, y: 16 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: index * 0.04 }}
                className="bg-white rounded-[2.25rem] shadow-xl shadow-slate-100/60 overflow-hidden border border-slate-100"
              >
                <div className="flex flex-col md:flex-row">
                  <div className="md:w-56 h-44 md:h-auto overflow-hidden bg-slate-900">
                    <img
                      src={item.imageUrl || 'https://images.unsplash.com/photo-1528127269322-539801943592?auto=format&fit=crop&q=80&w=1200'}
                      alt={item.title}
                      className="w-full h-full object-cover"
                    />
                  </div>

                  <div className="flex-1 p-6 flex flex-col">
                    <div className="flex items-start justify-between gap-4 mb-4">
                      <div>
                        <div className="inline-flex items-center gap-2 px-3 py-1 bg-slate-50 rounded-full text-[10px] font-black uppercase tracking-widest text-slate-500 mb-3">
                          {getProductIcon(item.productType)}
                          {formatCustomerProductLabel(item.productType)}
                        </div>
                        <h3 className="text-xl font-black text-slate-900">{item.title}</h3>
                        <p className="text-sm font-bold text-slate-500 mt-1">{item.subtitle || 'Mục đã lưu trong tài khoản của bạn'}</p>
                      </div>
                      <button
                        type="button"
                        onClick={() => handleRemove(item.id)}
                        disabled={removingId === item.id}
                        className="w-11 h-11 rounded-2xl bg-rose-50 text-rose-500 hover:bg-rose-500 hover:text-white transition-all flex items-center justify-center disabled:opacity-60"
                      >
                        <Trash2 size={16} />
                      </button>
                    </div>

                    <div className="flex items-center gap-5 text-xs font-bold text-slate-400 flex-wrap">
                      <span className="flex items-center gap-1.5">
                        <MapPin size={13} className="text-slate-300" />
                        {item.locationText || 'Đang cập nhật vị trí'}
                      </span>
                      <span>
                        {item.priceText || (item.priceValue ? formatCurrency(item.priceValue, item.currencyCode || 'VND') : 'Liên hệ để báo giá')}
                      </span>
                    </div>

                    <div className="mt-auto pt-6 flex items-center justify-between gap-4">
                      <p className="text-[11px] font-bold text-slate-400">
                        Đã lưu lúc {formatSavedAt(item.createdAt)}
                      </p>
                      <Link
                        to={targetPath}
                        className="inline-flex items-center gap-2 px-5 py-3 bg-slate-900 text-white rounded-2xl text-[10px] font-black uppercase tracking-widest hover:bg-[#1EB4D4] transition-all"
                      >
                        Xem chi tiết <ArrowRight size={14} />
                      </Link>
                    </div>
                  </div>
                </div>
              </motion.div>
            );
          })}
        </div>
      )}
    </motion.div>
  );
}
