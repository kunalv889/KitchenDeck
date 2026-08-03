import { useEffect, useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { menuApi, orderApi, tableApi, type OrderLineInput } from '../services/api';
import type { DiningTable, MenuItem, Order, OrderItemStatus } from '../types';

export default function OrdersPage() {
  const { id: restaurantId = '' } = useParams();
  const [tables, setTables] = useState<DiningTable[]>([]);
  const [menu, setMenu] = useState<MenuItem[]>([]);
  const [orders, setOrders] = useState<Order[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // New-order builder state.
  const [tableId, setTableId] = useState('');
  const [cart, setCart] = useState<Record<string, { quantity: number; notes: string }>>({});

  const loadOrders = async () => {
    setOrders(await orderApi.list(restaurantId, true));
  };

  const loadAll = async () => {
    setLoading(true);
    setError(null);
    try {
      const [t, m] = await Promise.all([
        tableApi.list(restaurantId),
        menuApi.list(restaurantId),
      ]);
      setTables(t);
      setMenu(m);
      await loadOrders();
    } catch (err: any) {
      setError(err.response?.data?.message ?? 'Could not load orders.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadAll();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [restaurantId]);

  const availableMenu = useMemo(() => menu.filter((m) => m.isAvailable), [menu]);
  const menuById = useMemo(() => new Map(menu.map((m) => [m.id, m])), [menu]);

  const addToCart = (menuItemId: string) => {
    setCart((prev) => {
      const existing = prev[menuItemId];
      return {
        ...prev,
        [menuItemId]: { quantity: (existing?.quantity ?? 0) + 1, notes: existing?.notes ?? '' },
      };
    });
  };

  const setCartQty = (menuItemId: string, quantity: number) => {
    setCart((prev) => {
      const next = { ...prev };
      if (quantity <= 0) {
        delete next[menuItemId];
      } else {
        next[menuItemId] = { quantity, notes: prev[menuItemId]?.notes ?? '' };
      }
      return next;
    });
  };

  const setCartNotes = (menuItemId: string, notes: string) => {
    setCart((prev) => ({
      ...prev,
      [menuItemId]: { quantity: prev[menuItemId]?.quantity ?? 1, notes },
    }));
  };

  const cartLines: OrderLineInput[] = Object.entries(cart).map(([menuItemId, v]) => ({
    menuItemId,
    quantity: v.quantity,
    notes: v.notes || null,
  }));

  const submitOrder = async () => {
    if (!tableId || cartLines.length === 0) return;
    setError(null);
    try {
      await orderApi.create(restaurantId, tableId, cartLines);
      setCart({});
      setTableId('');
      await loadOrders();
    } catch (err: any) {
      setError(err.response?.data?.message ?? 'Could not create order.');
    }
  };

  const nextItemStatus = (s: OrderItemStatus): OrderItemStatus | null =>
    s === 'Pending' ? 'Preparing' : s === 'Preparing' ? 'Served' : null;

  const advanceItem = async (order: Order, lineId: string, status: OrderItemStatus) => {
    const updated = await orderApi.setLineStatus(restaurantId, order.id, lineId, status);
    setOrders((prev) => prev.map((o) => (o.id === updated.id ? updated : o)));
  };

  const removeLine = async (order: Order, lineId: string) => {
    const updated = await orderApi.removeLine(restaurantId, order.id, lineId);
    setOrders((prev) => prev.map((o) => (o.id === updated.id ? updated : o)));
  };

  const setOrderStatus = async (order: Order, status: Order['status']) => {
    const updated = await orderApi.setStatus(restaurantId, order.id, status);
    // Served/closed orders drop off the active list on next refresh.
    if (status === 'Served') {
      setOrders((prev) => prev.map((o) => (o.id === updated.id ? updated : o)));
    } else {
      setOrders((prev) => prev.map((o) => (o.id === updated.id ? updated : o)));
    }
  };

  const closeOrder = async (order: Order) => {
    await orderApi.setStatus(restaurantId, order.id, 'Closed');
    await loadOrders();
  };

  const addItemToOrder = async (order: Order, menuItemId: string) => {
    if (!menuItemId) return;
    const updated = await orderApi.addLines(restaurantId, order.id, [
      { menuItemId, quantity: 1, notes: null },
    ]);
    setOrders((prev) => prev.map((o) => (o.id === updated.id ? updated : o)));
  };

  if (loading) {
    return <div className="page"><p className="muted">Loading…</p></div>;
  }

  return (
    <div className="page">
      <header className="topbar">
        <div>
          <Link to={`/restaurants/${restaurantId}`} className="muted">← Restaurant</Link>
          <h1>Orders</h1>
        </div>
        <button className="link-btn" onClick={loadOrders}>Refresh</button>
      </header>

      {error && <p className="error">{error}</p>}

      {/* New order builder */}
      <section className="panel">
        <h2>Take a new order</h2>
        {tables.length === 0 ? (
          <p className="muted">Add tables first.</p>
        ) : availableMenu.length === 0 ? (
          <p className="muted">Add available menu items first.</p>
        ) : (
          <>
            <label>
              Table
              <select value={tableId} onChange={(e) => setTableId(e.target.value)}>
                <option value="">Select a table…</option>
                {tables.map((t) => (
                  <option key={t.id} value={t.id}>
                    Table {t.number}{t.label ? ` (${t.label})` : ''}
                  </option>
                ))}
              </select>
            </label>

            <div className="menu-pick">
              {availableMenu.map((m) => (
                <button
                  key={m.id}
                  type="button"
                  className="pick-btn"
                  onClick={() => addToCart(m.id)}
                >
                  + {m.name} <span className="muted">{m.price.toFixed(2)}</span>
                </button>
              ))}
            </div>

            {cartLines.length > 0 && (
              <ul className="card-list cart">
                {cartLines.map((line) => {
                  const item = menuById.get(line.menuItemId);
                  return (
                    <li key={line.menuItemId} className="card">
                      <div>
                        <h3>{item?.name}</h3>
                        <input
                          className="notes-input"
                          placeholder="Notes (e.g. no onions)"
                          value={cart[line.menuItemId]?.notes ?? ''}
                          onChange={(e) => setCartNotes(line.menuItemId, e.target.value)}
                        />
                      </div>
                      <div className="qty-controls">
                        <button type="button" onClick={() => setCartQty(line.menuItemId, line.quantity - 1)}>−</button>
                        <span>{line.quantity}</span>
                        <button type="button" onClick={() => setCartQty(line.menuItemId, line.quantity + 1)}>+</button>
                      </div>
                    </li>
                  );
                })}
              </ul>
            )}

            <div className="form-actions">
              <button type="button" onClick={submitOrder} disabled={!tableId || cartLines.length === 0}>
                Send order
              </button>
              {cartLines.length > 0 && (
                <button type="button" className="link-btn" onClick={() => setCart({})}>Clear</button>
              )}
            </div>
          </>
        )}
      </section>

      {/* Active orders */}
      <section className="panel">
        <h2>Active orders</h2>
        {orders.length === 0 ? (
          <p className="muted">No active orders.</p>
        ) : (
          <div className="orders-grid">
            {orders.map((order) => (
              <div key={order.id} className={`order-card status-${order.status.toLowerCase()}`}>
                <div className="order-head">
                  <h3>Table {order.tableNumber}</h3>
                  <span className={`status-pill ${order.status.toLowerCase()}`}>{order.status}</span>
                </div>

                <ul className="order-lines">
                  {order.items.map((line) => {
                    const next = nextItemStatus(line.status);
                    return (
                      <li key={line.id} className={`line status-${line.status.toLowerCase()}`}>
                        <span className="line-main">
                          <strong>{line.quantity}×</strong> {line.name}
                          {line.notes ? <em className="muted"> — {line.notes}</em> : null}
                        </span>
                        <span className="line-actions">
                          <span className={`status-dot ${line.status.toLowerCase()}`} title={line.status} />
                          {next && (
                            <button className="link-btn" onClick={() => advanceItem(order, line.id, next)}>
                              {next === 'Preparing' ? 'Prepare' : 'Serve'}
                            </button>
                          )}
                          <button className="link-btn danger" onClick={() => removeLine(order, line.id)}>✕</button>
                        </span>
                      </li>
                    );
                  })}
                </ul>

                <div className="order-add">
                  <select
                    defaultValue=""
                    onChange={(e) => {
                      void addItemToOrder(order, e.target.value);
                      e.target.value = '';
                    }}
                  >
                    <option value="">+ Add item…</option>
                    {availableMenu.map((m) => (
                      <option key={m.id} value={m.id}>{m.name}</option>
                    ))}
                  </select>
                </div>

                <div className="order-actions">
                  {order.status !== 'Preparing' && order.status !== 'Served' && (
                    <button onClick={() => setOrderStatus(order, 'Preparing')}>Mark preparing</button>
                  )}
                  {order.status !== 'Served' && (
                    <button onClick={() => setOrderStatus(order, 'Served')}>Mark served</button>
                  )}
                  <button className="link-btn" onClick={() => closeOrder(order)}>Close</button>
                </div>
              </div>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
