import React from 'react';

const CmsPostSelectorCard = ({
  posts = [],
  selectedPostId = '',
  onChange = () => {},
  loading = false,
  title = 'Chọn bài viết',
  subtitle = 'Chọn bài viết để xem chi tiết.',
}) => {
  const selectedPost = posts.find((item) => item.id === selectedPostId) || null;

  return (
    <div className="bg-white rounded-[2.5rem] border border-slate-100 shadow-sm p-6 space-y-4">
      <div>
        <h3 className="text-lg font-black text-slate-900">{title}</h3>
        <p className="text-sm text-slate-500 font-medium mt-1">{subtitle}</p>
      </div>

      <select
        value={selectedPostId}
        onChange={(event) => onChange(event.target.value)}
        disabled={loading || posts.length === 0}
        className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-5 py-4 text-sm font-bold text-slate-700 outline-none disabled:opacity-60"
      >
        {posts.length === 0 ? (
          <option value="">Chưa có bài viết nào</option>
        ) : (
          posts.map((post) => (
            <option key={post.id} value={post.id}>
              {post.title}
            </option>
          ))
        )}
      </select>

      <div className="rounded-2xl bg-slate-50 border border-slate-100 px-5 py-4">
        <p className="text-[10px] font-black uppercase tracking-widest text-slate-400">Tóm tắt hiện tại</p>
        <p className="mt-2 text-sm font-medium text-slate-600">
          {selectedPost?.summary || 'Bài viết được chọn sẽ hiển thị thông tin tóm tắt tại đây.'}
        </p>
      </div>
    </div>
  );
};

export default CmsPostSelectorCard;
