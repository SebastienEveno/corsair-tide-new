// ── Types ──────────────────────────────────────────────────────────────────────

export interface PlayerDto {
  playerId: string;
  username: string;
  email: string;
}

export interface ResourcesDto {
  wood: number;
  gold: number;
  rum: number;
  food: number;
}

export interface ActiveUpgradeDto {
  buildingType: string;
  targetLevel: number;
  completesAt: string;
  secondsRemaining: number;
}

export interface IslandDto {
  islandId: string;
  name: string;
  resources: ResourcesDto;
  buildings: Record<string, number>;
  activeUpgrade: ActiveUpgradeDto | null;
  woodPerHour: number;
  goldPerHour: number;
  rumPerHour: number;
  foodPerHour: number;
}

// ── API calls ─────────────────────────────────────────────────────────────────

async function request<T>(url: string, init?: RequestInit): Promise<T> {
  const res = await fetch(url, {
    headers: { 'Content-Type': 'application/json' },
    ...init,
  });
  if (!res.ok) {
    let message = `HTTP ${res.status}`;
    try {
      const body = await res.json();
      message = body.error ?? body.title ?? message;
    } catch {
      /* ignore */
    }
    throw new Error(message);
  }
  return res.json() as Promise<T>;
}

export const api = {
  registerPlayer: (username: string, email: string) =>
    request<PlayerDto>('/api/players/register', {
      method: 'POST',
      body: JSON.stringify({ username, email }),
    }),

  getPlayer: (username: string) =>
    request<PlayerDto>(`/api/players/${encodeURIComponent(username)}`),

  getIsland: (playerId: string) =>
    request<IslandDto>(`/api/islands/${playerId}`),

  startUpgrade: (playerId: string, buildingType: string) =>
    request<IslandDto>(`/api/islands/${playerId}/upgrades`, {
      method: 'POST',
      body: JSON.stringify({ buildingType }),
    }),
};
