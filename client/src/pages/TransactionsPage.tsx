import { useEffect, useState } from 'react';
import api from '../api';
import type { Transaction, PagedResult, PropertyListItem } from '../types';

export default function TransactionsPage() {
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [properties, setProperties] = useState<PropertyListItem[]>([]);
  const [categories, setCategories] = useState<string[]>([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);

  // Filters
  const [propertyId, setPropertyId] = useState<string>('');
  const [category, setCategory] = useState('');
  const [search, setSearch] = useState('');
  const [sortBy, setSortBy] = useState('TransactionDate');
  const [sortDesc, setSortDesc] = useState(true);

  useEffect(() => {
    Promise.all([
      api.get<PropertyListItem[]>('/properties'),
      api.get<string[]>('/transactions/categories'),
    ]).then(([propRes, catRes]) => {
      setProperties(propRes.data);
      setCategories(catRes.data);
    });
  }, []);

  useEffect(() => {
    setLoading(true);
    const params: Record<string, unknown> = {
      page,
      pageSize: 25,
      sortBy,
      sortDesc,
    };
    if (propertyId) params.propertyId = propertyId;
    if (category) params.category = category;
    if (search) params.search = search;

    api.get<PagedResult<Transaction>>('/transactions', { params }).then(({ data }) => {
      setTransactions(data.items);
      setTotalPages(data.totalPages);
      setTotalCount(data.totalCount);
      setLoading(false);
    });
  }, [page, propertyId, category, search, sortBy, sortDesc]);

  const handleSort = (col: string) => {
    if (sortBy === col) {
      setSortDesc(!sortDesc);
    } else {
      setSortBy(col);
      setSortDesc(true);
    }
    setPage(1);
  };

  const resetFilters = () => {
    setPropertyId('');
    setCategory('');
    setSearch('');
    setPage(1);
  };

  const fmt = (v?: number | null) => (v != null ? `$${v.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}` : '—');
  const fmtDate = (d?: string | null) => (d ? new Date(d).toLocaleDateString() : '—');

  const SortIcon = ({ col }: { col: string }) => {
    if (sortBy !== col) return <span className="text-gray-300 ml-1">↕</span>;
    return <span className="ml-1">{sortDesc ? '↓' : '↑'}</span>;
  };

  return (
    <div>
      <h1 className="text-2xl font-bold text-gray-800 mb-4">Transactions</h1>

      {/* Filters */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-4 mb-4">
        <div className="flex flex-wrap gap-3 items-end">
          <div>
            <label className="block text-xs text-gray-500 mb-1">Property</label>
            <select
              value={propertyId}
              onChange={(e) => { setPropertyId(e.target.value); setPage(1); }}
              className="px-3 py-2 border border-gray-300 rounded-lg text-sm text-gray-800 min-w-[200px]"
            >
              <option value="">All Properties</option>
              {properties.map((p) => (
                <option key={p.propertyId} value={p.propertyId}>
                  {p.shortName}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-xs text-gray-500 mb-1">Category</label>
            <select
              value={category}
              onChange={(e) => { setCategory(e.target.value); setPage(1); }}
              className="px-3 py-2 border border-gray-300 rounded-lg text-sm text-gray-800 min-w-[180px]"
            >
              <option value="">All Categories</option>
              {categories.map((c) => (
                <option key={c} value={c}>{c}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-xs text-gray-500 mb-1">Search</label>
            <input
              type="text"
              placeholder="Name, notes, details..."
              value={search}
              onChange={(e) => { setSearch(e.target.value); setPage(1); }}
              className="px-3 py-2 border border-gray-300 rounded-lg text-sm w-60 text-gray-800"
            />
          </div>
          <button
            onClick={resetFilters}
            className="px-3 py-2 text-sm text-gray-600 hover:text-gray-800 border border-gray-300 rounded-lg hover:bg-gray-50"
          >
            Reset
          </button>
          <span className="text-sm text-gray-500 ml-auto">
            {totalCount.toLocaleString()} total records
          </span>
        </div>
      </div>

      {/* Table */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        {loading ? (
          <div className="text-center py-12 text-gray-500">Loading...</div>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-gray-50">
              <tr className="text-left text-gray-600">
                <th className="px-4 py-3 font-medium cursor-pointer select-none" onClick={() => handleSort('TransactionDate')}>
                  Date <SortIcon col="TransactionDate" />
                </th>
                <th className="px-4 py-3 font-medium cursor-pointer select-none" onClick={() => handleSort('Category')}>
                  Category <SortIcon col="Category" />
                </th>
                <th className="px-4 py-3 font-medium">Sub-Category</th>
                <th className="px-4 py-3 font-medium cursor-pointer select-none" onClick={() => handleSort('Name')}>
                  Name <SortIcon col="Name" />
                </th>
                <th className="px-4 py-3 font-medium">Notes</th>
                <th className="px-4 py-3 font-medium text-right cursor-pointer select-none" onClick={() => handleSort('Amount')}>
                  Amount <SortIcon col="Amount" />
                </th>
              </tr>
            </thead>
            <tbody>
              {transactions.map((t) => (
                <tr key={t.transactionId} className="border-t border-gray-100 hover:bg-gray-50">
                  <td className="px-4 py-2.5 whitespace-nowrap">{fmtDate(t.transactionDate)}</td>
                  <td className="px-4 py-2.5">
                    <span className="inline-block px-2 py-0.5 rounded-full text-xs bg-blue-50 text-blue-700">
                      {t.category || '—'}
                    </span>
                  </td>
                  <td className="px-4 py-2.5 text-gray-600">{t.subCategory || '—'}</td>
                  <td className="px-4 py-2.5 text-gray-700">{t.name || '—'}</td>
                  <td className="px-4 py-2.5 text-gray-500 max-w-xs truncate">{t.notes || '—'}</td>
                  <td
                    className={`px-4 py-2.5 text-right font-medium whitespace-nowrap ${
                      t.amount >= 0 ? 'text-green-600' : 'text-red-600'
                    }`}
                  >
                    {fmt(t.amount)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {/* Pagination */}
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
  );
}
