import { useState, useEffect, useCallback } from 'react';
import { api } from './api';
import type { IslandDto } from './api';

const RESOURCE_META = [
  { key: 'wood' as const,  label: 'Wood',  icon: '🪵', rateKey: 'woodPerHour' as const },
  { key: 'gold' as const,  label: 'Gold',  icon: '🪙', rateKey: 'goldPerHour' as const },
  { key: 'rum'  as const,  label: 'Rum',   icon: '🍺', rateKey: 'rumPerHour'  as const },
  { key: 'food' as const,  label: 'Food',  icon: '🐟', rateKey: 'foodPerHour' as const },
];

const BUILDING_META: Record<string, { icon: string; label: string }> = {
  Sawmill:     { icon: '🔨', label: 'Sawmill'      },
  GoldMine:    { icon: '⚡', label: 'Gold Mine'    },
  Distillery:  { icon: '🏭', label: 'Distillery'   },
  FishingDock: { icon: '🎣', label: 'Fishing Dock' },
  WoodStorage: { icon: '📦', label: 'Wood Storage' },
  GoldStorage: { icon: '💰', label: 'Gold Storage' },
  RumStorage:  { icon: '🛢', label: 'Rum Storage'  },
  FoodStorage: { icon: '🧱', label: 'Food Storage' },
};

function formatCountdown(seconds: number): string {
  if (seconds <= 0) return 'Done!';
  const m = Math.floor(seconds / 60);
  const s = Math.floor(seconds % 60);
  return m > 0 ? `${m}m ${s}s` : `${s}s`;
}

function fmt(n: number): string {
  return Math.floor(n).toLocaleString();
}

function UpgradeTimer({
  completesAt, buildingType, targetLevel, onComplete,
}: {
  completesAt: string; buildingType: string; targetLevel: number; onComplete: () => void;
}) {
  const [remaining, setRemaining] = useState(() =>
    Math.max(0, (new Date(completesAt).getTime() - Date.now()) / 1000)
  );

  useEffect(() => {
    if (remaining <= 0) { onComplete(); return; }
    const id = setInterval(() => {
      setRemaining(prev => {
        const next = Math.max(0, prev - 1);
        if (next === 0) { clearInterval(id); onComplete(); }
        return next;
      });
    }, 1000);
    return () => clearInterval(id);
  }, [completesAt, onComplete, remaining]);

  const elapsed = (new Date(completesAt).getTime() - Date.now()) / 1000 + remaining;
  const progress = Math.min(100, ((Math.max(1, elapsed) - remaining) / Math.max(1, elapsed)) * 100);
  const meta = BUILDING_META[buildingType] ?? { icon: '🔨', label: buildingType };

  return (
    <div className="bg-amber-950/40 border border-amber-700 rounded-xl p-4 mb-6">
      <div className="flex items-center justify-between mb-2">
        <span className="text-amber-300 font-semibold text-sm">
          {meta.icon} Upgrading {meta.label} to Level {targetLevel}
        </span>
        <span className="text-amber-400 font-mono text-sm font-bold">
          {formatCountdown(remaining)}
        </span>
      </div>
      <div className="h-2 bg-slate-700 rounded-full overflow-hidden">
        <div
          className="h-full bg-amber-500 transition-all duration-1000"
          style={{ width: `${progress}%` }}
        />
      </div>
    </div>
  );
}

function ResourcesPanel({ island }: { island: IslandDto }) {
  return (
    <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 mb-6">
      {RESOURCE_META.map(({ key, label, icon, rateKey }) => (
        <div key={key} className="bg-slate-800 border border-slate-700 rounded-xl p-4">
          <div className="text-2xl mb-1">{icon}</div>
          <div className="text-white font-bold text-lg leading-tight">{fmt(island.resources[key])}</div>
          <div className="text-slate-400 text-xs">{label}</div>
          {island[rateKey] > 0 && (
            <div className="text-amber-400 text-xs mt-1">+{fmt(island[rateKey])}/hr</div>
          )}
        </div>
      ))}
    </div>
  );
}

function BuildingsPanel({
  island, onUpgradeStarted,
}: {
  island: IslandDto; onUpgradeStarted: (updated: IslandDto) => void;
}) {
  const [loading, setLoading] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const startUpgrade = async (buildingType: string) => {
    setLoading(buildingType);
    setError(null);
    try {
      const updated = await api.startUpgrade(buildingType);
      onUpgradeStarted(updated);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to start upgrade');
    } finally {
      setLoading(null);
    }
  };

  const hasActiveUpgrade = island.activeUpgrade !== null;

  return (
    <div>
      <h2 className="text-lg font-semibold text-slate-300 mb-3">Buildings</h2>
      {error && (
        <div className="text-red-400 text-sm bg-red-950/40 border border-red-800 rounded-lg px-4 py-2.5 mb-3">
          {error}
        </div>
      )}
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
        {Object.entries(island.buildings).map(([type, level]) => {
          const meta = BUILDING_META[type] ?? { icon: '🏛', label: type };
          const isUpgrading = island.activeUpgrade?.buildingType === type;
          const isDisabled = hasActiveUpgrade || loading !== null;
          return (
            <div
              key={type}
              className={`bg-slate-800 border rounded-xl p-4 flex items-center justify-between
                ${isUpgrading ? 'border-amber-600' : 'border-slate-700'}`}
            >
              <div className="flex items-center gap-3">
                <span className="text-2xl">{meta.icon}</span>
                <div>
                  <div className="text-white font-medium text-sm">{meta.label}</div>
                  <div className="text-slate-400 text-xs">Level {level}</div>
                </div>
              </div>
              {isUpgrading ? (
                <span className="text-amber-400 text-xs font-medium">Upgrading...</span>
              ) : (
                <button
                  onClick={() => startUpgrade(type)}
                  disabled={isDisabled}
                  className="bg-amber-600 hover:bg-amber-500 disabled:bg-slate-700
                             text-slate-900 disabled:text-slate-500 text-xs font-bold
                             px-3 py-1.5 rounded-lg transition"
                >
                  {loading === type ? '...' : `Lv ${level + 1}`}
                </button>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
}

export function IslandDashboard({
  username, onLogout,
}: {
  username: string; onLogout: () => void;
}) {
  const [island, setIsland] = useState<IslandDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchIsland = useCallback(async () => {
    try {
      const data = await api.getIsland();
      setIsland(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load island');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { fetchIsland(); }, [fetchIsland]);
  useEffect(() => {
    const id = setInterval(fetchIsland, 30_000);
    return () => clearInterval(id);
  }, [fetchIsland]);

  return (
    <div className="min-h-screen bg-slate-900 text-white">
      <div
        className="fixed inset-0 bg-gradient-to-b from-slate-900 via-blue-950 to-slate-900"
        style={{ zIndex: -1 }}
      />
      <header className="border-b border-slate-800 px-4 py-3 flex items-center justify-between">
        <span className="font-bold text-amber-400">Corsair Tide</span>
        <div className="flex items-center gap-3">
          <span className="text-slate-400 text-sm">{username}</span>
          <button onClick={onLogout} className="text-slate-500 hover:text-slate-300 text-xs transition">
            Log out
          </button>
        </div>
      </header>

      <main className="max-w-2xl mx-auto px-4 py-6">
        {loading && <div className="text-slate-400 text-sm animate-pulse">Loading your island...</div>}
        {error && (
          <div className="text-red-400 text-sm bg-red-950/40 border border-red-800 rounded-lg px-4 py-3">
            {error}
          </div>
        )}
        {island && (
          <div>
            <h1 className="text-2xl font-bold text-amber-400 mb-1">{island.name}</h1>
            <p className="text-slate-500 text-xs mb-6 font-mono">{island.islandId}</p>
            {island.activeUpgrade && (
              <UpgradeTimer
                completesAt={island.activeUpgrade.completesAt}
                buildingType={island.activeUpgrade.buildingType}
                targetLevel={island.activeUpgrade.targetLevel}
                onComplete={fetchIsland}
              />
            )}
            <ResourcesPanel island={island} />
            <BuildingsPanel island={island} onUpgradeStarted={setIsland} />
          </div>
        )}
      </main>
    </div>
  );
}
