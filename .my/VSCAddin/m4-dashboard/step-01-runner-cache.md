# Step 01 — Runner + cache

## Files

`vscode-extension/src/panel/runner.ts`:

```typescript
import { execFile } from 'child_process';

export function run(binary: string, args: string[], timeoutMs = 10_000): Promise<string> {
  return new Promise((resolve, reject) => {
    execFile(binary, args, { timeout: timeoutMs, windowsHide: true }, (err, stdout, stderr) => {
      if (err) reject(new Error(`${err.message}\n${stderr}`));
      else resolve(stdout);
    });
  });
}
```

`vscode-extension/src/panel/cache.ts`:

```typescript
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
```

## Done when
- [ ] `run` rejects on non-zero exit; resolves with stdout otherwise
- [ ] `TtlCache` returns cached value within TTL; refetches after expiry
- [ ] `invalidate('health')` clears matching keys only
