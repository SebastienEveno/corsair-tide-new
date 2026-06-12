// ── Token storage ─────────────────────────────────────────────────────────────

const TOKEN_KEY = 'corsair_jwt';

export const auth = {
  getToken: (): string | null => localStorage.getItem(TOKEN_KEY),
  setToken: (token: string) => localStorage.setItem(TOKEN_KEY, token),
  clear: () => localStorage.removeItem(TOKEN_KEY),
  isLoggedIn: () => !!localStorage.getItem(TOKEN_KEY),
};

// ── Types ──────────────────────────────────────────────────────────────────────

export interface PlayerDto {
  playerId: string;
  username: string;
  email: string;
}

export interface LoginResponse {
  token: string;
  username: string;
  playerId: string;
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
  const token = auth.getToken();
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
  };

  const res = await fetch(url, { headers, ...init });

  if (res.status === 401) {
    auth.clear();
    window.location.reload();
    throw new Error('Session expired. Please log in again.');
  }

  if (!res.ok) {
    let message = `HTTP ${res.status}`;
    try {
      const body = await res.json();
      message = body.error ?? body.title ?? message;
    } catch { /* ignore */ }
    throw new Error(message);
  }

  return res.json() as Promise<T>;
}

export const api = {
  login: (username: string, password: string) =>
    request<LoginResponse>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ username, password }),
    }),

  registerPlayer: (username: string, email: string, password: string) =>
    request<PlayerDto>('/api/players/register', {
      method: 'POST',
      body: JSON.stringify({ username, email, password }),
    }),

  getIsland: () =>
    request<IslandDto>('/api/islands/me'),

  startUpgrade: (buildingType: string) =>
    request<IslandDto>('/api/islands/me/upgrades', {
      method: 'POST',
      body: JSON.stringify({ buildingType }),
    }),
};
