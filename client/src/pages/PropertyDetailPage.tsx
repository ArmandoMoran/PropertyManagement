import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import api from '../api';
import type { PropertyDetail, Transaction, PagedResult } from '../types';

export default function PropertyDetailPage() {
  const { id } = useParams<{ id: string }>();
  const [property, setProperty] = useState<PropertyDetail | null>(null);
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<'overview' | 'transactions' | 'history'>('overview');

  useEffect(() => {
    if (!id) return;
    api.get<PropertyDetail>(`/properties/${id}`).then(({ data }) => {
      setProperty(data);
      setLoading(false);
    });
  }, [id]);

  useEffect(() => {
    if (!id) return;
    api
      .get<PagedResult<Transaction>>('/transactions', {
        params: { propertyId: id, page, pageSize: 15, sortBy: 'TransactionDate', sortDesc: true },
      })
      .then(({ data }) => {
        setTransactions(data.items);
        setTotalPages(data.totalPages);
        setTotalCount(data.totalCount);
      });
  }, [id, page]);

  if (loading || !property)
    return <div className="text-center py-12 text-gray-500">Loading property...</div>;

  const fmt = (v?: number | null) => (v != null ? `$${v.toLocaleString()}` : '—');
  const fmtDate = (d?: string | null) =>
    d ? new Date(d).toLocaleDateString() : '—';
  const fmtPct = (v?: number | null) => (v != null ? `${v.toFixed(1)}%` : '—');

  return (
    <div>
      <Link to="/properties" className="text-blue-600 hover:text-blue-800 text-sm mb-4 inline-block">
        ← Back to Properties
      </Link>

      <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6 mb-6">
        <h1 className="text-2xl font-bold text-gray-800">{property.street}</h1>
        <p className="text-gray-500">
          {property.city}, {property.state} {property.zipCode}
        </p>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mt-4">
          <InfoCard label="Type" value={property.propertyType || '—'} />
          <InfoCard label="Units" value={property.units?.toString() || '—'} />
          <InfoCard label="Sq Ft" value={property.sqFt?.toLocaleString() || '—'} />
          <InfoCard label="Zestimate" value={fmt(property.zestimate)} />
        </div>
      </div>

      {/* Tabs */}
      <div className="flex gap-1 mb-4 border-b border-gray-200">
        {(['overview', 'transactions', 'history'] as const).map((tab) => (
          <button
            key={tab}
            onClick={() => setActiveTab(tab)}
            className={`px-4 py-2 text-sm font-medium capitalize border-b-2 transition-colors ${
              activeTab === tab
                ? 'border-blue-600 text-blue-600'
                : 'border-transparent text-gray-500 hover:text-gray-700'
            }`}
          >
            {tab}
          </button>
        ))}
      </div>

      {activeTab === 'overview' && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {/* Lender */}
          <Section title="Lender">
            {property.lender ? (
              <div className="space-y-1 text-sm">
                <Row label="Name" value={property.lender.lenderName} />
                <Row label="Mortgage #" value={property.lender.mortgageNumber || '—'} />
                <Row label="Monthly Payment" value={fmt(property.lender.monthlyPayment)} />
                <Row label="Effective Date" value={fmtDate(property.lender.effectiveDate)} />
                {property.lender.lenderUrl && (
                  <Row
                    label="URL"
                    value={
                      <a href={property.lender.lenderUrl} target="_blank" className="text-blue-600 hover:underline">
                        {property.lender.lenderUrl}
                      </a>
                    }
                  />
                )}
              </div>
            ) : (
              <p className="text-gray-400 text-sm">No lender on file</p>
            )}
          </Section>

          {/* HOA */}
          <Section title="HOA">
            {property.hoa ? (
              <div className="space-y-1 text-sm">
                <Row label="Name" value={property.hoa.hoaName} />
                <Row label="Account #" value={property.hoa.accountNumber || '—'} />
                <Row label="Mgmt Company" value={property.hoa.managementCompany || '—'} />
                <Row label="Payment" value={`${fmt(property.hoa.paymentAmount)} / ${property.hoa.paymentFrequency || '—'}`} />
              </div>
            ) : (
              <p className="text-gray-400 text-sm">No HOA on file</p>
            )}
          </Section>

          {/* Insurance */}
          <Section title="Insurance">
            {property.insurance ? (
              <div className="space-y-1 text-sm">
                <Row label="Carrier" value={property.insurance.carrier} />
                <Row label="Policy #" value={property.insurance.policyNumber || '—'} />
                <Row label="Renewal" value={fmtDate(property.insurance.renewalDate)} />
                <Row label="Who Pays" value={property.insurance.whoPays || '—'} />
              </div>
            ) : (
              <p className="text-gray-400 text-sm">No insurance on file</p>
            )}
          </Section>

          {/* Insurance Premiums */}
          <Section title="Insurance Premiums">
            {property.insurancePremiums.length > 0 ? (
              <table className="w-full text-sm">
                <thead>
                  <tr className="text-left text-gray-500 border-b">
                    <th className="py-1">Year</th>
                    <th className="py-1">Premium</th>
                    <th className="py-1">YoY Change</th>
                  </tr>
                </thead>
                <tbody>
                  {property.insurancePremiums.map((p) => (
                    <tr key={p.premiumId} className="border-b border-gray-100">
                      <td className="py-1">{p.policyYear}</td>
                      <td className="py-1">{fmt(p.annualPremium)}</td>
                      <td className="py-1">{fmtPct(p.yoyPercentChange)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            ) : (
              <p className="text-gray-400 text-sm">No premium history</p>
            )}
          </Section>

          {/* Principal Balance History */}
          <Section title="Principal Balance History">
            {property.principalBalanceHistory.length > 0 ? (
              <table className="w-full text-sm">
                <thead>
                  <tr className="text-left text-gray-500 border-b">
                    <th className="py-1">Date</th>
                    <th className="py-1">Balance</th>
                  </tr>
                </thead>
                <tbody>
                  {property.principalBalanceHistory.map((b) => (
                    <tr key={b.balanceId} className="border-b border-gray-100">
                      <td className="py-1">{fmtDate(b.snapshotDate)}</td>
                      <td className="py-1">{fmt(b.principalBalance)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            ) : (
              <p className="text-gray-400 text-sm">No balance history</p>
            )}
          </Section>
        </div>
      )}

      {activeTab === 'transactions' && (
        <div>
          <p className="text-sm text-gray-500 mb-3">
            Showing {transactions.length} of {totalCount} transactions
          </p>
          <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-gray-50">
                <tr className="text-left text-gray-600">
                  <th className="px-4 py-3 font-medium">Date</th>
                  <th className="px-4 py-3 font-medium">Category</th>
                  <th className="px-4 py-3 font-medium">Name</th>
                  <th className="px-4 py-3 font-medium">Notes</th>
                  <th className="px-4 py-3 font-medium text-right">Amount</th>
                </tr>
              </thead>
              <tbody>
                {transactions.map((t) => (
                  <tr key={t.transactionId} className="border-t border-gray-100 hover:bg-gray-50">
                    <td className="px-4 py-2.5">{fmtDate(t.transactionDate)}</td>
                    <td className="px-4 py-2.5">
                      <span className="inline-block px-2 py-0.5 rounded-full text-xs bg-blue-50 text-blue-700">
                        {t.category || '—'}
                      </span>
                    </td>
                    <td className="px-4 py-2.5 text-gray-700">{t.name || '—'}</td>
                    <td className="px-4 py-2.5 text-gray-500 max-w-xs truncate">{t.notes || '—'}</td>
                    <td
                      className={`px-4 py-2.5 text-right font-medium ${
                        t.amount >= 0 ? 'text-green-600' : 'text-red-600'
                      }`}
                    >
                      {fmt(t.amount)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="flex items-center justify-between mt-4">
            <button
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={page <= 1}
              className="px-4 py-2 text-sm bg-white border border-gray-300 rounded-lg disabled:opacity-40 hover:bg-gray-50"
            >
              Previous
            </button>
            <span className="text-sm text-gray-500">
              Page {page} of {totalPages}
            </span>
            <button
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              disabled={page >= totalPages}
              className="px-4 py-2 text-sm bg-white border border-gray-300 rounded-lg disabled:opacity-40 hover:bg-gray-50"
            >
              Next
            </button>
          </div>
        </div>
      )}

      {activeTab === 'history' && (
        <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
          {property.propertyHistory.length > 0 ? (
            <table className="w-full text-sm">
              <thead className="bg-gray-50">
                <tr className="text-left text-gray-600">
                  <th className="px-4 py-3 font-medium">Date</th>
                  <th className="px-4 py-3 font-medium">Property Name</th>
                  <th className="px-4 py-3 font-medium">Description</th>
                  <th className="px-4 py-3 font-medium">Notes</th>
                </tr>
              </thead>
              <tbody>
                {property.propertyHistory.map((h) => (
                  <tr key={h.historyId} className="border-t border-gray-100 hover:bg-gray-50">
                    <td className="px-4 py-2.5 whitespace-nowrap">{fmtDate(h.eventDate)}</td>
                    <td className="px-4 py-2.5">{h.propertyName || '—'}</td>
                    <td className="px-4 py-2.5">{h.description || '—'}</td>
                    <td className="px-4 py-2.5 text-gray-500">{h.notes || '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          ) : (
            <p className="text-gray-400 text-sm p-6">No property history records</p>
          )}
        </div>
      )}
    </div>
  );
}

function InfoCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="bg-gray-50 rounded-lg p-3">
      <p className="text-xs text-gray-500 uppercase tracking-wide">{label}</p>
      <p className="text-lg font-semibold text-gray-800">{value}</p>
    </div>
  );
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-5">
      <h3 className="font-semibold text-gray-700 mb-3">{title}</h3>
      {children}
    </div>
  );
}

function Row({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="flex justify-between">
      <span className="text-gray-500">{label}</span>
      <span className="text-gray-800 font-medium">{value}</span>
    </div>
  );
}
