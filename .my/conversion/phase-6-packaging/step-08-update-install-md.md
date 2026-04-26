# Step 08 — Update install.md

## Goal
Add a ".NET binary" tab/section to `deploy/install.md`.

## Snippet
```bash
curl -L -o /usr/local/bin/session-recall \
  https://github.com/dezgit2025/auto-memory/releases/latest/download/session-recall-linux-x64
chmod +x /usr/local/bin/session-recall
session-recall health
```

## Done when
- [ ] `deploy/install.md` has parallel sections: "pip install" (default) and ".NET binary".
- [ ] Both flows verified end-to-end on a clean Ubuntu container.
