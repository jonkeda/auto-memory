interface Entry { value: unknown; expires: number; }

export class TtlCache {
  private map = new Map<string, Entry>();
  constructor(private readonly ttlMs = 60_000) {}

  async get<T>(key: string, fetch: () => Promise<T>): Promise<T> {
    const now = Date.now();
    const hit = this.map.get(key);
    if (hit && hit.expires > now) return hit.value as T;
    const value = await fetch();
    this.map.set(key, { value, expires: now + this.ttlMs });
    return value;
  }

  invalidate(prefix?: string): void {
    if (!prefix) { this.map.clear(); return; }
    for (const k of this.map.keys()) if (k.startsWith(prefix)) this.map.delete(k);
  }
}
