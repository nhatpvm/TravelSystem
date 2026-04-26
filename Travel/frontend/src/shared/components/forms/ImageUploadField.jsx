import React, { useId } from 'react';
import { Loader2, Upload, X } from 'lucide-react';

export default function ImageUploadField({
  label = 'URL ảnh',
  value,
  onChange,
  onUpload,
  uploading = false,
  placeholder = 'URL ảnh',
  helperText = '',
  previewAlt = 'Preview',
  accept = 'image/png,image/jpeg,image/webp',
}) {
  const fileInputId = useId();

  async function handleFileChange(event) {
    const nextFile = event.target.files?.[0];
    event.target.value = '';

    if (!nextFile || !onUpload) {
      return;
    }

    await onUpload(nextFile);
  }

  return (
    <div className="space-y-3">
      {label ? <span className="block text-[11px] font-black uppercase tracking-widest text-slate-400">{label}</span> : null}
      <div className="grid grid-cols-1 md:grid-cols-[1fr_auto_auto] gap-3">
        <input
          value={value}
          onChange={(event) => onChange(event.target.value)}
          placeholder={placeholder}
          className="w-full rounded-2xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-700 outline-none"
        />
        <label
          htmlFor={fileInputId}
          className={`px-4 py-3 rounded-2xl border border-slate-200 bg-white text-sm font-black text-slate-600 flex items-center justify-center gap-2 ${uploading ? 'cursor-wait opacity-70' : 'cursor-pointer'}`}
        >
          {uploading ? <Loader2 size={16} className="animate-spin" /> : <Upload size={16} />}
          {uploading ? 'Đang tải...' : 'Tải từ máy'}
        </label>
        {value ? (
          <button
            type="button"
            onClick={() => onChange('')}
            className="px-4 py-3 rounded-2xl bg-slate-100 text-sm font-black text-slate-600 flex items-center justify-center gap-2"
          >
            <X size={16} />
            Bỏ ảnh
          </button>
        ) : null}
        <input
          id={fileInputId}
          type="file"
          accept={accept}
          onChange={handleFileChange}
          className="hidden"
          disabled={uploading}
        />
      </div>
      {helperText ? <p className="text-xs font-bold text-slate-400">{helperText}</p> : null}
      {value ? (
        <div className="rounded-[2rem] border border-slate-100 bg-slate-50 p-4">
          <img src={value} alt={previewAlt} className="h-52 w-full rounded-[1.5rem] object-cover" />
        </div>
      ) : null}
    </div>
  );
}
