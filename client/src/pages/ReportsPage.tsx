import { useEffect, useState } from 'react';
import api from '../api';

export default function ReportsPage() {
  const [years, setYears] = useState<number[]>([]);
  const [selectedYear, setSelectedYear] = useState<number>(0);
  const [downloading, setDownloading] = useState(false);

  useEffect(() => {
    api.get<number[]>('/properties/years').then(({ data }) => {
      setYears(data);
      if (data.length > 0) setSelectedYear(data[data.length - 1]);
    });
  }, []);

  const downloadExcel = async () => {
    if (!selectedYear) return;
    setDownloading(true);
    try {
      const response = await api.get(`/reports/${selectedYear}/excel`, {
        responseType: 'blob',
      });
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', `PropertyReport_All_${selectedYear}.xlsx`);
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
    } catch (err) {
      console.error('Download failed', err);
      alert('Failed to download report');
    } finally {
      setDownloading(false);
    }
  };

  return (
    <div>
      <h1 className="text-2xl font-bold text-gray-800 mb-6">Reports</h1>

      <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6 max-w-lg">
        <h2 className="text-lg font-semibold text-gray-700 mb-4">Download Excel Report</h2>
        <p className="text-sm text-gray-500 mb-4">
          Generate a comprehensive Profit & Loss Excel workbook with a worksheet for each property.
        </p>
        <div className="flex gap-3 items-end">
          <div>
            <label className="block text-xs text-gray-500 mb-1">Year</label>
            <select
              value={selectedYear}
              onChange={(e) => setSelectedYear(Number(e.target.value))}
              className="px-3 py-2 border border-gray-300 rounded-lg text-sm text-gray-800"
            >
              {years.map((y) => (
                <option key={y} value={y}>{y}</option>
              ))}
            </select>
          </div>
          <button
            onClick={downloadExcel}
            disabled={downloading || !selectedYear}
            className="px-5 py-2 bg-blue-700 hover:bg-blue-800 disabled:bg-blue-400 text-white font-medium rounded-lg text-sm transition-colors"
          >
            {downloading ? 'Generating...' : '📥 Download Excel'}
          </button>
        </div>
      </div>
    </div>
  );
}
