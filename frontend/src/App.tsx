import { useState } from 'react';
import { api } from './api';
import type { PlayerDto } from './api';
import { IslandDashboard } from './IslandDashboard';
import './App.css';

const inputCls =
  'w-full rounded-lg bg-slate-700 border border-slate-600 text-white placeholder-slate-500 ' +
  'px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-amber-400 focus:border-transparent transition';

type Mode = 'login' | 'register';
type FormState = 'idle' | 'loading' | 'error';

function AuthForm({ onSuccess }: { onSuccess: (player: PlayerDto) => void }) {
  const [mode, setMode] = useState<Mode>('login');
  const [username, setUsername] = useState('');
  const [email, setEmail] = useState('');
  const [formState, setFormState] = useState<FormState>('idle');
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormState('loading');
    setError(null);

    try {
      let player: PlayerDto;
      if (mode === 'login') {
        player = await api.getPlayer(username.trim());
      } else {
        player = await api.registerPlayer(username.trim(), email.trim());
      }
      onSuccess(player);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Something went wrong');
      setFormState('error');
    }
  };

  const switchMode = (next: Mode) => {
    setMode(next);
    setError(null);
    setFormState('idle');
  };

  return (
    <div className="min-h-screen bg-slate-900 flex items-center justify-center p-4">
      <div
        className="fixed inset-0 bg-gradient-to-b from-slate-900 via-blue-950 to-slate-900"
        style={{ zIndex: -1 }}
      />

      <div className="w-full max-w-md">
        <div className="text-center mb-8">
          <div className="text-5xl mb-3">💀</div>
          <h1 className="text-4xl font-bold text-amber-400 tracking-wide">Corsair Tide</h1>
          <p className="text-slate-400 mt-2 text-sm">
            Claim your island. Build your fleet. Rule the seas.
          </p>
        </div>

        <div className="bg-slate-800 border border-slate-700 rounded-2xl shadow-2xl p-8">
          <div className="flex mb-6 bg-slate-900 rounded-lg p-1">
            {(['login', 'register'] as Mode[]).map(m => (
              <button
                key={m}
                type="button"
                onClick={() => switchMode(m)}
                className={`flex-1 py-2 text-sm font-medium rounded-md transition
                  ${mode === m
                    ? 'bg-amber-500 text-slate-900'
                    : 'text-slate-400 hover:text-slate-200'}`}
              >
                {m === 'login' ? 'Log In' : 'Register'}
              </button>
            ))}
          </div>

          <form onSubmit={handleSubmit} className="space-y-5">
            <div>
              <label htmlFor="username" className="block text-sm font-medium text-slate-300 mb-1.5">
                Pirate Name
              </label>
              <input
                id="username"
                type="text"
                required
                value={username}
                onChange={e => setUsername(e.target.value)}
                placeholder="e.g. BlackBart"
                className={inputCls}
              />
            </div>

            {mode === 'register' && (
              <div>
                <label htmlFor="email" className="block text-sm font-medium text-slate-300 mb-1.5">
                  Email
                </label>
                <input
                  id="email"
                  type="email"
                  required
                  value={email}
                  onChange={e => setEmail(e.target.value)}
                  placeholder="you@sea.io"
                  className={inputCls}
                />
              </div>
            )}

            {error && (
              <div className="text-red-400 text-sm bg-red-950/40 border border-red-800 rounded-lg px-4 py-2.5">
                {error}
              </div>
            )}

            <button
              type="submit"
              disabled={formState === 'loading'}
              className="w-full bg-amber-500 hover:bg-amber-400 disabled:bg-amber-800 disabled:cursor-not-allowed
                         text-slate-900 font-bold py-3 rounded-lg transition text-sm tracking-wide"
            >
              {formState === 'loading'
                ? 'Setting sail...'
                : mode === 'login'
                  ? 'Enter the Seas'
                  : 'Claim Your Island'}
            </button>
          </form>
        </div>

        <p className="text-center text-slate-600 text-xs mt-6">
          No treasure required to start.
        </p>
      </div>
    </div>
  );
}

export default function App() {
  const [player, setPlayer] = useState<PlayerDto | null>(null);

  if (player) {
    return (
      <IslandDashboard
        playerId={player.playerId}
        username={player.username}
        onLogout={() => setPlayer(null)}
      />
    );
  }

  return <AuthForm onSuccess={setPlayer} />;
}
