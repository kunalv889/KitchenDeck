import { useEffect, useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { restaurantApi } from '../services/api';
import type { Restaurant } from '../types';

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
        <h1>KitchenDeck</h1>
        <div className="topbar-right">
          <span className="muted">{user?.displayName}</span>
          <button className="link-btn" onClick={logout}>Sign out</button>
        </div>
      </header>

      <section className="panel">
        <h2>Create a restaurant</h2>
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
          <button type="submit" disabled={creating}>{creating ? 'Creating…' : 'Create'}</button>
        </form>
        {error && <p className="error">{error}</p>}
      </section>

      <section className="panel">
        <h2>Your restaurants</h2>
        {loading ? (
          <p className="muted">Loading…</p>
        ) : restaurants.length === 0 ? (
          <p className="muted">You are not part of any restaurant yet. Create one above.</p>
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
                  <Link to={`/restaurants/${r.id}/orders`}>Orders</Link>
                  <Link to={`/restaurants/${r.id}`}>Manage</Link>
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}
