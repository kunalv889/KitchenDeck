import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import { ChefHat, Lock, RefreshCw } from 'lucide-react';
import { kitchenApi } from '../services/api';
import type { Order, OrderItem } from '../types';
import ThemeToggle from '../components/ThemeToggle';

const REFRESH_MS = 5000;
const STORAGE_PREFIX = 'kd_kitchen_';

function itemClass(status: OrderItem['status']): string {
  return status.toLowerCase();
}

export default function KitchenPage() {
  const { restaurantId = '' } = useParams();
  const storageKey = STORAGE_PREFIX + restaurantId;

  const [token, setToken] = useState<string | null>(() => localStorage.getItem(STORAGE_PREFIX + restaurantId));
  const [restaurantName, setRestaurantName] = useState('');
  const [passcode, setPasscode] = useState('');
  const [unlocking, setUnlocking] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [orders, setOrders] = useState<Order[]>([]);
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);
  const timerRef = useRef<number | null>(null);

  const unlock = async (e: React.FormEvent) => {
    e.preventDefault();
    setUnlocking(true);
    setError(null);
    try {
      const res = await kitchenApi.access(restaurantId, passcode.trim());
      localStorage.setItem(storageKey, res.token);
      setRestaurantName(res.restaurantName);
      setToken(res.token);
      setPasscode('');
    } catch (err: any) {
      setError(err.response?.data?.message ?? 'Could not unlock the kitchen window.');
    } finally {
      setUnlocking(false);
    }
  };

  const lock = () => {
    localStorage.removeItem(storageKey);
    setToken(null);
    setOrders([]);
    setError(null);
  };

  const loadOrders = useCallback(async () => {
    if (!token) return;
    try {
      const data = await kitchenApi.listOrders(restaurantId, token);
      setOrders(data);
      setLastUpdated(new Date());
      setError(null);
    } catch (err: any) {
      if (err.response?.status === 401) {
        // Kitchen token expired or invalid — force re-entry of the passcode.
        localStorage.removeItem(storageKey);
        setToken(null);
        setError('Kitchen session expired. Enter the passcode again.');
      } else {
        setError('Could not refresh orders.');
      }
    }
  }, [restaurantId, token, storageKey]);

  useEffect(() => {
    if (!token) return;
    void loadOrders();
    timerRef.current = window.setInterval(() => void loadOrders(), REFRESH_MS);
    return () => {
      if (timerRef.current) window.clearInterval(timerRef.current);
    };
  }, [token, loadOrders]);

  // Group active orders by table for the tile board.
  const byTable = useMemo(() => {
    const map = new Map<number, Order[]>();
    for (const o of orders) {
      const list = map.get(o.tableNumber) ?? [];
      list.push(o);
      map.set(o.tableNumber, list);
    }
    return [...map.entries()].sort((a, b) => a[0] - b[0]);
  }, [orders]);

  if (!token) {
    return (
      <div className="kitchen-gate">
        <form className="auth-card" onSubmit={unlock}>
          <div className="auth-head">
            <span className="brand"><ChefHat size={22} /> Kitchen Window</span>
            <ThemeToggle />
          </div>
          <p className="muted">Enter the 6-digit kitchen passcode to open the live order board.</p>
          <input
            type="password"
            inputMode="numeric"
            autoComplete="off"
            placeholder="Passcode"
            value={passcode}
            onChange={(e) => setPasscode(e.target.value)}
            required
          />
          {error && <p className="error">{error}</p>}
          <button type="submit" className="btn-block" disabled={unlocking || !passcode.trim()}>
            <Lock size={16} />
            {unlocking ? 'Unlocking…' : 'Open board'}
          </button>
        </form>
      </div>
    );
  }

  return (
    <div className="kitchen">
      <header className="kitchen-topbar">
        <div>
          <h1><ChefHat size={24} /> Kitchen Window{restaurantName ? ` · ${restaurantName}` : ''}</h1>
          <p className="muted">
            {orders.length} active order{orders.length === 1 ? '' : 's'}
            {lastUpdated && ` · updated ${lastUpdated.toLocaleTimeString()}`}
          </p>
        </div>
        <div className="kitchen-topbar-actions">
          <ThemeToggle />
          <button className="btn-secondary" onClick={() => void loadOrders()}>
            <RefreshCw size={15} /> Refresh
          </button>
          <button className="btn-secondary" onClick={lock}>
            <Lock size={15} /> Lock
          </button>
        </div>
      </header>

      {error && <p className="error">{error}</p>}

      {byTable.length === 0 ? (
        <div className="empty-state kitchen-empty">
          <ChefHat size={34} />
          <p className="muted">No active orders right now.</p>
        </div>
      ) : (
        <div className="kitchen-board">
          {byTable.map(([tableNumber, tableOrders]) => (
            <section className="table-tile" key={tableNumber}>
              <div className="table-tile-head">
                <h2>Table {tableNumber}</h2>
                <span className="badge">
                  {tableOrders.reduce((n, o) => n + o.items.length, 0)} items
                </span>
              </div>
              {tableOrders.map((o) => (
                <div className={`kitchen-order status-${o.status.toLowerCase()}`} key={o.id}>
                  <div className="kitchen-order-head">
                    <span className={`status-pill ${o.status.toLowerCase()}`}>{o.status}</span>
                    <span className="muted">{new Date(o.createdAt).toLocaleTimeString()}</span>
                  </div>
                  <ul className="kitchen-lines">
                    {o.items.map((it) => (
                      <li className={`kitchen-line ${itemClass(it.status)}`} key={it.id}>
                        <span className="qty">{it.quantity}×</span>
                        <span className="name">
                          {it.name}
                          {it.notes && <em className="line-note"> — {it.notes}</em>}
                        </span>
                        <span className={`status-dot ${itemClass(it.status)}`} title={it.status} />
                      </li>
                    ))}
                  </ul>
                </div>
              ))}
            </section>
          ))}
        </div>
      )}
    </div>
  );
}
