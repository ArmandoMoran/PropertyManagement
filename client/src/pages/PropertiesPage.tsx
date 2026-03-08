import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import api from '../api';
import type { PropertyListItem } from '../types';

export default function PropertiesPage() {
  const [properties, setProperties] = useState<PropertyListItem[]>([]);
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.get<PropertyListItem[]>('/properties').then(({ data }) => {
      setProperties(data);
      setLoading(false);
    });
  }, []);

  const filtered = properties.filter(
    (p) =>
      p.fullAddress.toLowerCase().includes(search.toLowerCase()) ||
      p.shortName.toLowerCase().includes(search.toLowerCase())
  );

  if (loading) return <div className="text-center py-12 text-gray-500">Loading properties...</div>;

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-800">Properties ({filtered.length})</h1>
        <input
          type="text"
          placeholder="Search properties..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="px-4 py-2 border border-gray-300 rounded-lg w-80 focus:ring-2 focus:ring-blue-500 outline-none text-gray-800"
        />
      </div>
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {filtered.map((p) => (
          <Link
            key={p.propertyId}
            to={`/properties/${p.propertyId}`}
            className="bg-white rounded-xl shadow-sm border border-gray-200 p-5 hover:shadow-md hover:border-blue-300 transition-all"
          >
            <h2 className="text-lg font-semibold text-gray-800 mb-1">{p.shortName}</h2>
            <p className="text-sm text-gray-500">{p.fullAddress}</p>
          </Link>
        ))}
      </div>
    </div>
  );
}
