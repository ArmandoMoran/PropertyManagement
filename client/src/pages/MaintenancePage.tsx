import { useEffect, useState, useCallback } from 'react';
import api from '../api';
import type { PropertyListItem, Property, Lender, HoaInfo, Insurance, InsurancePremium } from '../types';

type EntityType = 'property' | 'lender' | 'hoa' | 'insurance' | 'premium';

export default function MaintenancePage() {
  const [properties, setProperties] = useState<PropertyListItem[]>([]);
  const [selectedPropertyId, setSelectedPropertyId] = useState<number | null>(null);
  const [activeEntity, setActiveEntity] = useState<EntityType>('property');
  const [message, setMessage] = useState<{ text: string; type: 'success' | 'error' } | null>(null);

  useEffect(() => {
    api.get<PropertyListItem[]>('/properties').then(({ data }) => setProperties(data));
  }, []);

  const showMsg = (text: string, type: 'success' | 'error' = 'success') => {
    setMessage({ text, type });
    setTimeout(() => setMessage(null), 3000);
  };

  return (
    <div>
      <h1 className="text-2xl font-bold text-gray-800 mb-4">Maintenance</h1>

      {message && (
        <div
          className={`mb-4 px-4 py-3 rounded-lg text-sm ${
            message.type === 'success' ? 'bg-green-50 text-green-700 border border-green-200' : 'bg-red-50 text-red-700 border border-red-200'
          }`}
        >
          {message.text}
        </div>
      )}

      <div className="flex gap-4 mb-6">
        <div>
          <label className="block text-xs text-gray-500 mb-1">Select Property</label>
          <select
            value={selectedPropertyId ?? ''}
            onChange={(e) => setSelectedPropertyId(e.target.value ? Number(e.target.value) : null)}
            className="px-3 py-2 border border-gray-300 rounded-lg text-sm text-gray-800 min-w-[250px]"
          >
            <option value="">— Select a property —</option>
            {properties.map((p) => (
              <option key={p.propertyId} value={p.propertyId}>
                {p.shortName}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Entity tabs */}
      <div className="flex gap-1 mb-4 border-b border-gray-200">
        {(['property', 'lender', 'hoa', 'insurance', 'premium'] as EntityType[]).map((tab) => (
          <button
            key={tab}
            onClick={() => setActiveEntity(tab)}
            className={`px-4 py-2 text-sm font-medium capitalize border-b-2 transition-colors ${
              activeEntity === tab
                ? 'border-blue-600 text-blue-600'
                : 'border-transparent text-gray-500 hover:text-gray-700'
            }`}
          >
            {tab === 'premium' ? 'Ins. Premiums' : tab}
          </button>
        ))}
      </div>

      {activeEntity === 'property' && selectedPropertyId && (
        <PropertyForm propertyId={selectedPropertyId} onSave={() => showMsg('Property updated!')} onError={() => showMsg('Failed to update', 'error')} />
      )}
      {activeEntity === 'lender' && selectedPropertyId && (
        <LenderForm propertyId={selectedPropertyId} onSave={() => showMsg('Lender saved!')} onError={() => showMsg('Failed to save', 'error')} />
      )}
      {activeEntity === 'hoa' && selectedPropertyId && (
        <HoaForm propertyId={selectedPropertyId} onSave={() => showMsg('HOA saved!')} onError={() => showMsg('Failed to save', 'error')} />
      )}
      {activeEntity === 'insurance' && selectedPropertyId && (
        <InsuranceForm propertyId={selectedPropertyId} onSave={() => showMsg('Insurance saved!')} onError={() => showMsg('Failed to save', 'error')} />
      )}
      {activeEntity === 'premium' && selectedPropertyId && (
        <PremiumForm propertyId={selectedPropertyId} onSave={() => showMsg('Premium saved!')} onError={() => showMsg('Failed to save', 'error')} />
      )}

      {!selectedPropertyId && (
        <div className="bg-gray-50 rounded-xl p-12 text-center text-gray-400">
          Select a property above to manage its data
        </div>
      )}
    </div>
  );
}

// --- Property Form ---
function PropertyForm({ propertyId, onSave, onError }: { propertyId: number; onSave: () => void; onError: () => void }) {
  const [form, setForm] = useState<Property | null>(null);

  const load = useCallback(() => {
    api.get<Property>(`/properties/${propertyId}`).then(({ data }) => setForm(data));
  }, [propertyId]);

  useEffect(() => { load(); }, [load]);

  if (!form) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await api.put(`/properties/${propertyId}`, form);
      onSave();
    } catch { onError(); }
  };

  const upd = (field: keyof Property, value: string | number) => setForm({ ...form, [field]: value });

  return (
    <form onSubmit={handleSubmit} className="bg-white rounded-xl shadow-sm border border-gray-200 p-6 max-w-2xl space-y-4">
      <h3 className="font-semibold text-gray-700 mb-2">Edit Property</h3>
      <div className="grid grid-cols-2 gap-4">
        <Field label="Street" value={form.street} onChange={(v) => upd('street', v)} />
        <Field label="City" value={form.city} onChange={(v) => upd('city', v)} />
        <Field label="State" value={form.state} onChange={(v) => upd('state', v)} />
        <Field label="Zip Code" value={form.zipCode} onChange={(v) => upd('zipCode', v)} />
        <Field label="Full Address" value={form.fullAddress} onChange={(v) => upd('fullAddress', v)} />
        <Field label="Owner" value={form.owner || ''} onChange={(v) => upd('owner', v)} />
        <Field label="Property Type" value={form.propertyType || ''} onChange={(v) => upd('propertyType', v)} />
        <Field label="Units" value={form.units?.toString() || ''} onChange={(v) => upd('units', Number(v))} type="number" />
        <Field label="Sq Ft" value={form.sqFt?.toString() || ''} onChange={(v) => upd('sqFt', Number(v))} type="number" />
        <Field label="Zestimate" value={form.zestimate?.toString() || ''} onChange={(v) => upd('zestimate', Number(v))} type="number" />
      </div>
      <button type="submit" className="px-5 py-2 bg-blue-700 hover:bg-blue-800 text-white font-medium rounded-lg text-sm">Save</button>
    </form>
  );
}

// --- Lender Form ---
function LenderForm({ propertyId, onSave, onError }: { propertyId: number; onSave: () => void; onError: () => void }) {
  const [lenders, setLenders] = useState<Lender[]>([]);
  const [form, setForm] = useState<Partial<Lender>>({ propertyId, lenderName: '', monthlyPayment: 0 });
  const [editing, setEditing] = useState(false);

  const load = useCallback(() => {
    api.get<Lender[]>(`/properties/${propertyId}/lenders`).then(({ data }) => setLenders(data));
  }, [propertyId]);

  useEffect(() => { load(); setForm({ propertyId, lenderName: '', monthlyPayment: 0 }); setEditing(false); }, [load, propertyId]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (editing && form.lenderId) {
        await api.put(`/properties/lenders/${form.lenderId}`, form);
      } else {
        await api.post(`/properties/${propertyId}/lenders`, form);
      }
      onSave();
      load();
      setForm({ propertyId, lenderName: '', monthlyPayment: 0 });
      setEditing(false);
    } catch { onError(); }
  };

  const edit = (l: Lender) => { setForm(l); setEditing(true); };

  return (
    <div className="space-y-4">
      <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-gray-50">
            <tr className="text-left text-gray-600">
              <th className="px-4 py-3">Lender</th><th className="px-4 py-3">Mortgage #</th><th className="px-4 py-3">Monthly Payment</th><th className="px-4 py-3">Actions</th>
            </tr>
          </thead>
          <tbody>
            {lenders.map((l) => (
              <tr key={l.lenderId} className="border-t border-gray-100">
                <td className="px-4 py-2">{l.lenderName}</td>
                <td className="px-4 py-2">{l.mortgageNumber || '—'}</td>
                <td className="px-4 py-2">${l.monthlyPayment.toLocaleString()}</td>
                <td className="px-4 py-2">
                  <button onClick={() => edit(l)} className="text-blue-600 hover:underline text-xs">Edit</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <form onSubmit={handleSubmit} className="bg-white rounded-xl shadow-sm border border-gray-200 p-6 max-w-2xl space-y-4">
        <h3 className="font-semibold text-gray-700">{editing ? 'Edit Lender' : 'Add Lender'}</h3>
        <div className="grid grid-cols-2 gap-4">
          <Field label="Lender Name" value={form.lenderName || ''} onChange={(v) => setForm({ ...form, lenderName: v })} />
          <Field label="Mortgage #" value={form.mortgageNumber || ''} onChange={(v) => setForm({ ...form, mortgageNumber: v })} />
          <Field label="Monthly Payment" value={form.monthlyPayment?.toString() || '0'} onChange={(v) => setForm({ ...form, monthlyPayment: Number(v) })} type="number" />
          <Field label="URL" value={form.lenderUrl || ''} onChange={(v) => setForm({ ...form, lenderUrl: v })} />
          <Field label="User ID" value={form.userId || ''} onChange={(v) => setForm({ ...form, userId: v })} />
        </div>
        <div className="flex gap-2">
          <button type="submit" className="px-5 py-2 bg-blue-700 hover:bg-blue-800 text-white font-medium rounded-lg text-sm">
            {editing ? 'Update' : 'Add'}
          </button>
          {editing && (
            <button type="button" onClick={() => { setForm({ propertyId, lenderName: '', monthlyPayment: 0 }); setEditing(false); }}
              className="px-5 py-2 border border-gray-300 rounded-lg text-sm hover:bg-gray-50">Cancel</button>
          )}
        </div>
      </form>
    </div>
  );
}

// --- HOA Form ---
function HoaForm({ propertyId, onSave, onError }: { propertyId: number; onSave: () => void; onError: () => void }) {
  const [hoaList, setHoaList] = useState<HoaInfo[]>([]);
  const [form, setForm] = useState<Partial<HoaInfo>>({ propertyId, hoaName: '', paymentAmount: 0 });
  const [editing, setEditing] = useState(false);

  const load = useCallback(() => {
    api.get<HoaInfo[]>(`/properties/${propertyId}/hoa`).then(({ data }) => setHoaList(data));
  }, [propertyId]);

  useEffect(() => { load(); setForm({ propertyId, hoaName: '', paymentAmount: 0 }); setEditing(false); }, [load, propertyId]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (editing && form.hoaId) {
        await api.put(`/properties/hoa/${form.hoaId}`, form);
      } else {
        await api.post(`/properties/${propertyId}/hoa`, form);
      }
      onSave();
      load();
      setForm({ propertyId, hoaName: '', paymentAmount: 0 });
      setEditing(false);
    } catch { onError(); }
  };

  return (
    <div className="space-y-4">
      <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-gray-50">
            <tr className="text-left text-gray-600">
              <th className="px-4 py-3">HOA Name</th><th className="px-4 py-3">Frequency</th><th className="px-4 py-3">Amount</th><th className="px-4 py-3">Actions</th>
            </tr>
          </thead>
          <tbody>
            {hoaList.map((h) => (
              <tr key={h.hoaId} className="border-t border-gray-100">
                <td className="px-4 py-2">{h.hoaName}</td>
                <td className="px-4 py-2">{h.paymentFrequency || '—'}</td>
                <td className="px-4 py-2">${h.paymentAmount.toLocaleString()}</td>
                <td className="px-4 py-2">
                  <button onClick={() => { setForm(h); setEditing(true); }} className="text-blue-600 hover:underline text-xs">Edit</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <form onSubmit={handleSubmit} className="bg-white rounded-xl shadow-sm border border-gray-200 p-6 max-w-2xl space-y-4">
        <h3 className="font-semibold text-gray-700">{editing ? 'Edit HOA' : 'Add HOA'}</h3>
        <div className="grid grid-cols-2 gap-4">
          <Field label="HOA Name" value={form.hoaName || ''} onChange={(v) => setForm({ ...form, hoaName: v })} />
          <Field label="Account #" value={form.accountNumber || ''} onChange={(v) => setForm({ ...form, accountNumber: v })} />
          <Field label="Mgmt Company" value={form.managementCompany || ''} onChange={(v) => setForm({ ...form, managementCompany: v })} />
          <Field label="Frequency" value={form.paymentFrequency || ''} onChange={(v) => setForm({ ...form, paymentFrequency: v })} />
          <Field label="Amount" value={form.paymentAmount?.toString() || '0'} onChange={(v) => setForm({ ...form, paymentAmount: Number(v) })} type="number" />
          <Field label="Effective Year" value={form.effectiveYear?.toString() || ''} onChange={(v) => setForm({ ...form, effectiveYear: Number(v) })} type="number" />
        </div>
        <div className="flex gap-2">
          <button type="submit" className="px-5 py-2 bg-blue-700 hover:bg-blue-800 text-white font-medium rounded-lg text-sm">{editing ? 'Update' : 'Add'}</button>
          {editing && (
            <button type="button" onClick={() => { setForm({ propertyId, hoaName: '', paymentAmount: 0 }); setEditing(false); }}
              className="px-5 py-2 border border-gray-300 rounded-lg text-sm hover:bg-gray-50">Cancel</button>
          )}
        </div>
      </form>
    </div>
  );
}

// --- Insurance Form ---
function InsuranceForm({ propertyId, onSave, onError }: { propertyId: number; onSave: () => void; onError: () => void }) {
  const [ins, setIns] = useState<Insurance | null>(null);
  const [form, setForm] = useState<Partial<Insurance>>({ propertyId, carrier: '' });

  const load = useCallback(() => {
    api.get(`/properties/${propertyId}/insurance`).then(({ data }) => {
      if (data.insurance) {
        setIns(data.insurance);
        setForm(data.insurance);
      } else {
        setIns(null);
        setForm({ propertyId, carrier: '' });
      }
    });
  }, [propertyId]);

  useEffect(() => { load(); }, [load]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (ins?.insuranceId) {
        await api.put(`/properties/insurance/${ins.insuranceId}`, form);
      } else {
        await api.post(`/properties/${propertyId}/insurance`, form);
      }
      onSave();
      load();
    } catch { onError(); }
  };

  return (
    <form onSubmit={handleSubmit} className="bg-white rounded-xl shadow-sm border border-gray-200 p-6 max-w-2xl space-y-4">
      <h3 className="font-semibold text-gray-700">{ins ? 'Edit Insurance' : 'Add Insurance'}</h3>
      <div className="grid grid-cols-2 gap-4">
        <Field label="Carrier" value={form.carrier || ''} onChange={(v) => setForm({ ...form, carrier: v })} />
        <Field label="Policy #" value={form.policyNumber || ''} onChange={(v) => setForm({ ...form, policyNumber: v })} />
        <Field label="Who Pays" value={form.whoPays || ''} onChange={(v) => setForm({ ...form, whoPays: v })} />
      </div>
      <button type="submit" className="px-5 py-2 bg-blue-700 hover:bg-blue-800 text-white font-medium rounded-lg text-sm">Save</button>
    </form>
  );
}

// --- Insurance Premium Form ---
function PremiumForm({ propertyId, onSave, onError }: { propertyId: number; onSave: () => void; onError: () => void }) {
  const [premiums, setPremiums] = useState<InsurancePremium[]>([]);
  const [insuranceId, setInsuranceId] = useState<number | null>(null);
  const [form, setForm] = useState<Partial<InsurancePremium>>({ policyYear: new Date().getFullYear(), annualPremium: 0 });

  const load = useCallback(() => {
    api.get(`/properties/${propertyId}/insurance`).then(({ data }) => {
      if (data.insurance) {
        setInsuranceId(data.insurance.insuranceId);
        setPremiums(data.premiums || []);
      }
    });
  }, [propertyId]);

  useEffect(() => { load(); }, [load]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!insuranceId) { onError(); return; }
    try {
      await api.post(`/properties/insurance/${insuranceId}/premiums`, { ...form, insuranceId });
      onSave();
      load();
      setForm({ policyYear: new Date().getFullYear(), annualPremium: 0 });
    } catch { onError(); }
  };

  return (
    <div className="space-y-4">
      <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-gray-50">
            <tr className="text-left text-gray-600">
              <th className="px-4 py-3">Year</th><th className="px-4 py-3">Annual Premium</th><th className="px-4 py-3">YoY Change</th>
            </tr>
          </thead>
          <tbody>
            {premiums.map((p) => (
              <tr key={p.premiumId} className="border-t border-gray-100">
                <td className="px-4 py-2">{p.policyYear}</td>
                <td className="px-4 py-2">${p.annualPremium.toLocaleString()}</td>
                <td className="px-4 py-2">{p.yoyPercentChange != null ? `${p.yoyPercentChange.toFixed(1)}%` : '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {insuranceId ? (
        <form onSubmit={handleSubmit} className="bg-white rounded-xl shadow-sm border border-gray-200 p-6 max-w-2xl space-y-4">
          <h3 className="font-semibold text-gray-700">Add Premium Record</h3>
          <div className="grid grid-cols-2 gap-4">
            <Field label="Policy Year" value={form.policyYear?.toString() || ''} onChange={(v) => setForm({ ...form, policyYear: Number(v) })} type="number" />
            <Field label="Annual Premium" value={form.annualPremium?.toString() || '0'} onChange={(v) => setForm({ ...form, annualPremium: Number(v) })} type="number" />
            <Field label="YoY % Change" value={form.yoyPercentChange?.toString() || ''} onChange={(v) => setForm({ ...form, yoyPercentChange: v ? Number(v) : undefined })} type="number" />
          </div>
          <button type="submit" className="px-5 py-2 bg-blue-700 hover:bg-blue-800 text-white font-medium rounded-lg text-sm">Add Premium</button>
        </form>
      ) : (
        <div className="bg-yellow-50 border border-yellow-200 text-yellow-700 px-4 py-3 rounded-lg text-sm">
          No insurance record exists for this property. Add insurance first.
        </div>
      )}
    </div>
  );
}

// --- Shared Field Component ---
function Field({
  label, value, onChange, type = 'text',
}: {
  label: string; value: string; onChange: (v: string) => void; type?: string;
}) {
  return (
    <div>
      <label className="block text-xs text-gray-500 mb-1">{label}</label>
      <input
        type={type}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm text-gray-800 focus:ring-2 focus:ring-blue-500 outline-none"
      />
    </div>
  );
}
