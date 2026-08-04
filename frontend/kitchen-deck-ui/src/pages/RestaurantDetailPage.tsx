import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { AlertTriangle, ArrowLeft, ExternalLink, KeyRound, Trash2, UserPlus, Users } from 'lucide-react';
import { restaurantApi } from '../services/api';
import { ALL_ROLES, type Member, type Restaurant, type StaffRole } from '../types';
import MenuManager from '../components/MenuManager';
import TablesManager from '../components/TablesManager';
import ThemeToggle from '../components/ThemeToggle';

export default function RestaurantDetailPage() {
  const { id = '' } = useParams();
  const navigate = useNavigate();
  const [restaurant, setRestaurant] = useState<Restaurant | null>(null);
  const [members, setMembers] = useState<Member[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [deleting, setDeleting] = useState(false);

  const [email, setEmail] = useState('');
  const [newRoles, setNewRoles] = useState<StaffRole[]>(['Waiter']);
  const [passcode, setPasscode] = useState('');
  const [status, setStatus] = useState<string | null>(null);

  const isAdmin = useMemo(
    () => restaurant?.isOwner || restaurant?.myRoles.includes('Admin'),
    [restaurant],
  );

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const r = await restaurantApi.get(id);
      setRestaurant(r);
      if (r.isOwner || r.myRoles.includes('Admin')) {
        setMembers(await restaurantApi.members(id));
      }
    } catch (err: any) {
      setError(err.response?.data?.message ?? 'Could not load restaurant.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  const toggleRole = (role: StaffRole) => {
    setNewRoles((prev) =>
      prev.includes(role) ? prev.filter((r) => r !== role) : [...prev, role],
    );
  };

  const onAddMember = async (e: FormEvent) => {
    e.preventDefault();
    setStatus(null);
    setError(null);
    try {
      await restaurantApi.addMember(id, email.trim(), newRoles);
      setEmail('');
      setNewRoles(['Waiter']);
      setStatus('Member added.');
      await load();
    } catch (err: any) {
      setError(err.response?.data?.message ?? 'Could not add member.');
    }
  };

  const onChangeRoles = async (member: Member, role: StaffRole) => {
    const roles = member.roles.includes(role)
      ? member.roles.filter((r) => r !== role)
      : [...member.roles, role];
    await restaurantApi.updateRoles(id, member.userId, roles);
    await load();
  };

  const onRemove = async (member: Member) => {
    await restaurantApi.removeMember(id, member.userId);
    await load();
  };

  const onSetPasscode = async (e: FormEvent) => {
    e.preventDefault();
    setStatus(null);
    setError(null);
    if (!/^\d{6}$/.test(passcode)) {
      setError('Passcode must be exactly 6 digits.');
      return;
    }
    try {
      await restaurantApi.setKitchenPasscode(id, passcode);
      setPasscode('');
      setStatus('Kitchen passcode updated.');
      await load();
    } catch (err: any) {
      setError(err.response?.data?.message ?? 'Could not set passcode.');
    }
  };

  const onDeleteRestaurant = async () => {
    if (!restaurant) return;
    const confirmed = window.confirm(
      `Delete "${restaurant.name}"? This permanently removes its menu, tables, orders and staff. This cannot be undone.`,
    );
    if (!confirmed) return;
    setError(null);
    setDeleting(true);
    try {
      await restaurantApi.remove(restaurant.id);
      navigate('/');
    } catch (err: any) {
      setError(err.response?.data?.message ?? 'Could not delete restaurant.');
      setDeleting(false);
    }
  };

  if (loading) {
    return (
      <div className="page">
        <div className="skeleton" style={{ height: '2rem', width: '40%', marginBottom: '1.5rem' }} />
        <div className="skeleton-list">
          <div className="skeleton skeleton-row" />
          <div className="skeleton skeleton-row" />
        </div>
      </div>
    );
  }

  if (!restaurant) {
    return (
      <div className="page">
        <p className="error">{error ?? 'Restaurant not found.'}</p>
        <Link className="back-link" to="/"><ArrowLeft size={15} /> Back</Link>
      </div>
    );
  }

  return (
    <div className="page">
      <header className="topbar">
        <div>
          <Link to="/" className="back-link"><ArrowLeft size={15} /> All restaurants</Link>
          <h1>{restaurant.name}</h1>
        </div>
        <div className="topbar-right">
          <ThemeToggle />
        </div>
      </header>

      {status && <p className="success">{status}</p>}
      {error && <p className="error">{error}</p>}

      {!isAdmin && (
        <section className="panel">
          <p className="muted">
            You are a member with roles: {restaurant.myRoles.join(', ') || 'none'}.
          </p>
        </section>
      )}

      <MenuManager restaurantId={restaurant.id} canEdit={!!isAdmin} />
      <TablesManager restaurantId={restaurant.id} canEdit={!!isAdmin} />

      {isAdmin && (
        <>
          <section className="panel">
            <h2><UserPlus size={18} /> Add staff</h2>
            <form className="stacked-form" onSubmit={onAddMember}>
              <input
                type="email"
                placeholder="Existing user's email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
              />
              <div className="role-chips">
                {ALL_ROLES.map((role) => (
                  <label key={role} className={`chip ${newRoles.includes(role) ? 'chip-on' : ''}`}>
                    <input
                      type="checkbox"
                      checked={newRoles.includes(role)}
                      onChange={() => toggleRole(role)}
                    />
                    {role}
                  </label>
                ))}
              </div>
              <button type="submit"><UserPlus size={16} /> Add member</button>
            </form>
          </section>

          <section className="panel">
            <h2><Users size={18} /> Staff</h2>
            <ul className="card-list">
              {members.map((m) => (
                <li key={m.userId} className="card">
                  <div>
                    <h3>{m.displayName}</h3>
                    <p className="muted">{m.email}</p>
                    <div className="role-chips">
                      {ALL_ROLES.map((role) => (
                        <label key={role} className={`chip ${m.roles.includes(role) ? 'chip-on' : ''}`}>
                          <input
                            type="checkbox"
                            checked={m.roles.includes(role)}
                            disabled={m.userId === restaurant.ownerUserId && role === 'Admin'}
                            onChange={() => onChangeRoles(m, role)}
                          />
                          {role}
                        </label>
                      ))}
                    </div>
                  </div>
                  {m.userId !== restaurant.ownerUserId && (
                    <div className="card-actions">
                      <button className="link-btn danger" onClick={() => onRemove(m)}>
                        <Trash2 size={15} /> Remove
                      </button>
                    </div>
                  )}
                </li>
              ))}
            </ul>
          </section>

          <section className="panel">
            <h2><KeyRound size={18} /> Kitchen window passcode</h2>
            <p className="muted">
              {restaurant.hasKitchenPasscode
                ? 'A passcode is set. Enter a new value to change it.'
                : 'No passcode set yet.'}
            </p>
            <form className="inline-form" onSubmit={onSetPasscode}>
              <input
                placeholder="6-digit passcode"
                value={passcode}
                inputMode="numeric"
                maxLength={6}
                onChange={(e) => setPasscode(e.target.value.replace(/\D/g, ''))}
              />
              <button type="submit">Save passcode</button>
            </form>
            {restaurant.hasKitchenPasscode && (
              <p className="muted">
                Open the live board:{' '}
                <Link className="inline-link" to={`/kitchen/${id}`} target="_blank" rel="noopener">
                  Kitchen window <ExternalLink size={13} />
                </Link>{' '}
                (opens with the passcode above).
              </p>
            )}
          </section>
        </>
      )}

      {restaurant.isOwner && (
        <section className="panel danger-zone">
          <h2><AlertTriangle size={18} /> Danger zone</h2>
          <div className="danger-row">
            <div>
              <h3>Delete this restaurant</h3>
              <p className="muted">
                Permanently removes the restaurant along with its menu, tables, orders and staff.
                This cannot be undone.
              </p>
            </div>
            <button className="btn-danger" onClick={onDeleteRestaurant} disabled={deleting}>
              <Trash2 size={16} /> {deleting ? 'Deleting…' : 'Delete restaurant'}
            </button>
          </div>
        </section>
      )}
    </div>
  );
}
