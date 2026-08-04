import { useEffect, useState, type FormEvent } from 'react';
import { LayoutGrid, Pencil, Plus, Trash2 } from 'lucide-react';
import { tableApi, type TableInput } from '../services/api';
import type { DiningTable } from '../types';

const empty: TableInput = { number: 1, label: '', seats: 2 };

export default function TablesManager({
  restaurantId,
  canEdit,
}: {
  restaurantId: string;
  canEdit: boolean;
}) {
  const [tables, setTables] = useState<DiningTable[]>([]);
  const [loading, setLoading] = useState(true);
  const [form, setForm] = useState<TableInput>(empty);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    setLoading(true);
    try {
      setTables(await tableApi.list(restaurantId));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [restaurantId]);

  const resetForm = () => {
    setForm(empty);
    setEditingId(null);
  };

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);
    const payload: TableInput = {
      number: Number(form.number) || 0,
      label: form.label,
      seats: Number(form.seats) || 0,
    };
    try {
      if (editingId) {
        await tableApi.update(restaurantId, editingId, payload);
      } else {
        await tableApi.create(restaurantId, payload);
      }
      resetForm();
      await load();
    } catch (err: any) {
      setError(err.response?.data?.message ?? 'Could not save table.');
    }
  };

  const onEdit = (table: DiningTable) => {
    setEditingId(table.id);
    setForm({ number: table.number, label: table.label ?? '', seats: table.seats });
  };

  const onDelete = async (table: DiningTable) => {
    await tableApi.remove(restaurantId, table.id);
    if (editingId === table.id) resetForm();
    await load();
  };

  return (
    <section className="panel">
      <h2><LayoutGrid size={18} /> Tables</h2>

      {canEdit && (
        <form className="stacked-form" onSubmit={onSubmit}>
          <div className="form-row">
            <input
              type="number"
              min="1"
              placeholder="Table #"
              value={form.number}
              onChange={(e) => setForm({ ...form, number: Number(e.target.value) })}
              required
            />
            <input
              placeholder="Label (optional)"
              value={form.label ?? ''}
              onChange={(e) => setForm({ ...form, label: e.target.value })}
            />
            <input
              type="number"
              min="0"
              placeholder="Seats"
              value={form.seats}
              onChange={(e) => setForm({ ...form, seats: Number(e.target.value) })}
            />
          </div>
          <div className="form-actions">
            <button type="submit">
              <Plus size={16} /> {editingId ? 'Update table' : 'Add table'}
            </button>
            {editingId && (
              <button type="button" className="link-btn" onClick={resetForm}>Cancel</button>
            )}
          </div>
          {error && <p className="error">{error}</p>}
        </form>
      )}

      {loading ? (
        <div className="skeleton-list">
          <div className="skeleton skeleton-row" />
          <div className="skeleton skeleton-row" />
        </div>
      ) : tables.length === 0 ? (
        <p className="muted">No tables yet.</p>
      ) : (
        <ul className="card-list">
          {tables.map((table) => (
            <li key={table.id} className="card">
              <div>
                <h3>Table {table.number}</h3>
                <p className="muted">
                  {table.label ? `${table.label} · ` : ''}
                  {table.seats} seats
                </p>
              </div>
              {canEdit && (
                <div className="card-actions">
                  <button className="link-btn" onClick={() => onEdit(table)}>
                    <Pencil size={15} /> Edit
                  </button>
                  <button className="link-btn danger" onClick={() => onDelete(table)}>
                    <Trash2 size={15} /> Delete
                  </button>
                </div>
              )}
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
