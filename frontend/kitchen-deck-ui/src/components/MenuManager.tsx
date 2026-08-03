import { useEffect, useState, type FormEvent } from 'react';
import { menuApi, type MenuItemInput } from '../services/api';
import type { MenuItem } from '../types';

const empty: MenuItemInput = {
  name: '',
  description: '',
  category: '',
  price: 0,
  isAvailable: true,
};

export default function MenuManager({
  restaurantId,
  canEdit,
}: {
  restaurantId: string;
  canEdit: boolean;
}) {
  const [items, setItems] = useState<MenuItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [form, setForm] = useState<MenuItemInput>(empty);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    setLoading(true);
    try {
      setItems(await menuApi.list(restaurantId));
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
    const payload: MenuItemInput = {
      ...form,
      name: form.name.trim(),
      price: Number(form.price) || 0,
    };
    try {
      if (editingId) {
        await menuApi.update(restaurantId, editingId, payload);
      } else {
        await menuApi.create(restaurantId, payload);
      }
      resetForm();
      await load();
    } catch (err: any) {
      setError(err.response?.data?.message ?? 'Could not save menu item.');
    }
  };

  const onEdit = (item: MenuItem) => {
    setEditingId(item.id);
    setForm({
      name: item.name,
      description: item.description ?? '',
      category: item.category ?? '',
      price: item.price,
      isAvailable: item.isAvailable,
    });
  };

  const onDelete = async (item: MenuItem) => {
    await menuApi.remove(restaurantId, item.id);
    if (editingId === item.id) resetForm();
    await load();
  };

  return (
    <section className="panel">
      <h2>Menu</h2>

      {canEdit && (
        <form className="stacked-form" onSubmit={onSubmit}>
          <div className="form-row">
            <input
              placeholder="Item name"
              value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })}
              required
            />
            <input
              placeholder="Category"
              value={form.category ?? ''}
              onChange={(e) => setForm({ ...form, category: e.target.value })}
            />
            <input
              type="number"
              step="0.01"
              min="0"
              placeholder="Price"
              value={form.price}
              onChange={(e) => setForm({ ...form, price: Number(e.target.value) })}
              required
            />
          </div>
          <input
            placeholder="Description (optional)"
            value={form.description ?? ''}
            onChange={(e) => setForm({ ...form, description: e.target.value })}
          />
          <label className="checkbox-row">
            <input
              type="checkbox"
              checked={form.isAvailable}
              onChange={(e) => setForm({ ...form, isAvailable: e.target.checked })}
            />
            Available
          </label>
          <div className="form-actions">
            <button type="submit">{editingId ? 'Update item' : 'Add item'}</button>
            {editingId && (
              <button type="button" className="link-btn" onClick={resetForm}>Cancel</button>
            )}
          </div>
          {error && <p className="error">{error}</p>}
        </form>
      )}

      {loading ? (
        <p className="muted">Loading…</p>
      ) : items.length === 0 ? (
        <p className="muted">No menu items yet.</p>
      ) : (
        <ul className="card-list">
          {items.map((item) => (
            <li key={item.id} className="card">
              <div>
                <h3>
                  {item.name}{' '}
                  {!item.isAvailable && <span className="badge">Unavailable</span>}
                </h3>
                <p className="muted">
                  {item.category ? `${item.category} · ` : ''}
                  {item.price.toFixed(2)}
                  {item.description ? ` · ${item.description}` : ''}
                </p>
              </div>
              {canEdit && (
                <div className="card-actions">
                  <button className="link-btn" onClick={() => onEdit(item)}>Edit</button>
                  <button className="link-btn danger" onClick={() => onDelete(item)}>Delete</button>
                </div>
              )}
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
