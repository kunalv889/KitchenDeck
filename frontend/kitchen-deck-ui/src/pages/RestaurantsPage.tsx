import { useEffect, useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { ChefHat, ClipboardList, LogOut, Plus, Settings, Store } from 'lucide-react';
import { useAuth } from '../context/AuthContext';
import { restaurantApi } from '../services/api';
import type { Restaurant } from '../types';
import ThemeToggle from '../components/ThemeToggle';

export default function RestaurantsPage() {
  const { user, logout } = useAuth();
  const [restaurants, setRestaurants] = useState<Restaurant[]>([]);
  const [loading, setLoading] = useState(true);
  const [name, setName] = useState('');
  const [passcode, setPasscode] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);

  const load = async () => {
    setLoading(true);
    try {
      setRestaurants(await restaurantApi.list());
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  const onCreate = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);
    if (passcode && !/^\d{6}$/.test(passcode)) {
      setError('Kitchen passcode must be exactly 6 digits.');
      return;
    }
    setCreating(true);
    try {
      await restaurantApi.create(name.trim(), passcode || undefined);
      setName('');
      setPasscode('');
      await load();
    } catch (err: any) {
      setError(err.response?.data?.message ?? 'Could not create restaurant.');
    } finally {
      setCreating(false);
    }
  };

  return (
    <div className="page">
      <header className="topbar">
        <h1 className="brand"><ChefHat size={24} /> KitchenDeck</h1>
        <div className="topbar-right">
          <span className="muted">{user?.displayName}</span>
          <ThemeToggle />
          <button className="btn-secondary" onClick={logout}>
            <LogOut size={16} /> Sign out
          </button>
        </div>
      </header>

      <section className="panel">
        <h2><Plus size={18} /> Create a restaurant</h2>
        <form className="inline-form" onSubmit={onCreate}>
          <input
            placeholder="Restaurant name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
          />
          <input
            placeholder="Kitchen passcode (6 digits, optional)"
            value={passcode}
            inputMode="numeric"
            maxLength={6}
            onChange={(e) => setPasscode(e.target.value.replace(/\D/g, ''))}
          />
          <button type="submit" disabled={creating}>
            <Plus size={16} />
            {creating ? 'Creating…' : 'Create'}
          </button>
        </form>
        {error && <p className="error">{error}</p>}
      </section>

      <section className="panel">
        <h2><Store size={18} /> Your restaurants</h2>
        {loading ? (
          <div className="skeleton-list">
            <div className="skeleton skeleton-row" />
            <div className="skeleton skeleton-row" />
            <div className="skeleton skeleton-row" />
          </div>
        ) : restaurants.length === 0 ? (
          <div className="empty-state">
            <Store size={30} />
            <p className="muted">You are not part of any restaurant yet. Create one above.</p>
          </div>
        ) : (
          <ul className="card-list">
            {restaurants.map((r) => (
              <li key={r.id} className="card">
                <div>
                  <h3>{r.name}</h3>
                  <p className="muted">
                    {r.isOwner ? 'Owner' : r.myRoles.join(', ') || 'Member'}
                  </p>
                </div>
                <div className="card-actions">
                  <Link className="btn btn-secondary" to={`/restaurants/${r.id}/orders`}>
                    <ClipboardList size={15} /> Orders
                  </Link>
                  <Link className="btn btn-secondary" to={`/restaurants/${r.id}`}>
                    <Settings size={15} /> Manage
                  </Link>
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}
