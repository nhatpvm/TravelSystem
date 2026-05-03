import React from 'react';
import PromoCampaignManager from '../../admin/components/PromoCampaignManager';
import { useAuthSession } from '../../auth/hooks/useAuthSession';
import { getTenantDisplayName, getTenantOperatorBadge } from '../../auth/types';

export default function TenantPromosPage() {
  const session = useAuthSession();

  return (
    <div className="space-y-6">
      <div>
        <div className="mb-2 flex flex-wrap items-center gap-2">
          <span className="rounded-lg bg-blue-50 px-2.5 py-1 text-[10px] font-black uppercase tracking-widest text-blue-700">
            {getTenantOperatorBadge(session)}
          </span>
          <span className="rounded-lg bg-slate-100 px-2.5 py-1 text-[10px] font-black uppercase tracking-widest text-slate-500">
            Tenant promotion
          </span>
        </div>
        <h1 className="text-2xl font-black text-slate-900">Khuyến mãi của {getTenantDisplayName(session)}</h1>
        <p className="mt-1 text-sm font-bold text-slate-500">
          Tạo mã giảm giá riêng cho tenant hiện tại. Scope sản phẩm được backend khóa theo loại tenant.
        </p>
      </div>

      <PromoCampaignManager mode="tenant" />
    </div>
  );
}
