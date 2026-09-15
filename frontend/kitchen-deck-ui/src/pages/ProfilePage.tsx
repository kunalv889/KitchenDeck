import { useState } from 'react';
import { Link } from 'react-router-dom';
import { ArrowLeft, KeyRound } from 'lucide-react';
import { useAuth } from '../context/AuthContext';
import ThemeToggle from '../components/ThemeToggle';
import { useNavigate } from 'react-router-dom';

export default function ProfilePage() {
  const { user, updateProfile, changePassword } = useAuth();
  const [displayName, setDisplayName] = useState(user?.displayName ?? '');
  const [saving, setSaving] = useState(false);
  const [pwdStatus, setPwdStatus] = useState<string | null>(null);
  const [pwdError, setPwdError] = useState<string | null>(null);
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const { deleteAccount } = useAuth();
  const navigate = useNavigate();

  const onSave = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setSaving(true);
    try {
      await updateProfile(displayName.trim());
    } catch (err: any) {
      setError(err.response?.data?.message ?? 'Could not update profile.');
    } finally {
      setSaving(false);
    }
  };

  const onChangePassword = async (e: React.FormEvent) => {
    e.preventDefault();
    setPwdError(null);
    setPwdStatus(null);
    try {
      await changePassword(currentPassword, newPassword);
      setPwdStatus('Password updated.');
      setCurrentPassword('');
      setNewPassword('');
    } catch (err: any) {
      setPwdError(err.response?.data?.message ?? 'Could not change password.');
    }
  };

  const onDelete = async () => {
    if (!window.confirm('Delete your account? This will remove your profile and any restaurants you own.')) return;
    try {
      await deleteAccount();
      navigate('/login');
    } catch (err: any) {
      // show generic error
      setError(err.response?.data?.message ?? 'Could not delete account.');
    }
  };

  return (
    <div className="page">
      <header className="topbar">
        <div>
          <Link to="/" className="back-link"><ArrowLeft size={15} /> Home</Link>
          <h1>Profile</h1>
        </div>
        <div className="topbar-right">
          <ThemeToggle />
        </div>
      </header>

      <section className="panel">
        <h2>Update profile</h2>
        <form className="stacked-form" onSubmit={onSave}>
          <input value={displayName} onChange={(e) => setDisplayName(e.target.value)} required />
          <button type="submit" disabled={saving}>{saving ? 'Saving…' : 'Save'}</button>
        </form>
        {error && <p className="error">{error}</p>}
      </section>

      <section className="panel">
        <h2><KeyRound size={18} /> Change password</h2>
        <form className="stacked-form" onSubmit={onChangePassword}>
          <input
            type="password"
            placeholder="Current password"
            value={currentPassword}
            onChange={(e) => setCurrentPassword(e.target.value)}
            required
          />
          <input
            type="password"
            placeholder="New password (min 6 chars)"
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
            required
            minLength={6}
          />
          <button type="submit">Change password</button>
        </form>
        {pwdStatus && <p className="success">{pwdStatus}</p>}
        {pwdError && <p className="error">{pwdError}</p>}
      </section>

      <section className="panel danger-zone">
        <h2>Danger zone</h2>
        <div className="danger-row">
          <div>
            <h3>Delete account</h3>
            <p className="muted">This permanently deletes your profile. If you own restaurants, they will be removed too.</p>
          </div>
          <button className="btn-danger" onClick={onDelete}>Delete account</button>
        </div>
        {error && <p className="error">{error}</p>}
      </section>
    </div>
  );
}
