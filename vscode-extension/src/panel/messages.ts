import { AutoMemoryPanel } from './AutoMemoryPanel';
import { Strategy } from '../instructions/InstructionsManager';

interface Msg { type: string; payload: unknown; }

export async function handleMessage(panel: AutoMemoryPanel, msg: Msg): Promise<void> {
  try {
    switch (msg.type) {
      case 'refresh':            return refresh(panel, msg.payload?.section ?? 'all');
      case 'search':             return search(panel, msg.payload.query);
      case 'showSession':        return showSession(panel, msg.payload.id);
      case 'addInstructions':    return addIns(panel, msg.payload.strategy);
      case 'removeInstructions': return removeIns(panel, msg.payload.strategy);
    }
  } catch (e) {
    panel.post('error', { message: (e as Error).message });
  }
}

async function refresh(p: AutoMemoryPanel, section: string): Promise<void> {
  if (section === 'all' || section === 'install')      void postInstall(p).catch(() => {});
  if (section === 'all' || section === 'health')       void postHealth(p).catch(e => p.post('health', { error: (e as Error).message }));
  if (section === 'all' || section === 'sessions')     void postSessions(p).catch(e => p.post('sessions', { error: (e as Error).message }));
  if (section === 'all' || section === 'instructions') void postInstructions(p).catch(() => {});
}

async function postInstall(p: AutoMemoryPanel): Promise<void> {
  const v = await p.binary().installedVersion();
  const healthData = p.getLastHealthData();
  const vsCodeCount = healthData?.session_state_count ?? 0;
  const chatCount = healthData?.vscode_chat_count ?? 0;
  p.post('installStatus', {
    version: v,
    installDir: p.binary().installDir(),
    pathStrategy: 'userPath',
    vscode_session_count: vsCodeCount,
    vscode_chat_count: chatCount,
  });
}

async function postHealth(p: AutoMemoryPanel): Promise<void> {
  const json = await p.cli().get('health', () => p.runCli(['health', '--json']));
  const data = JSON.parse(json as string);
  p.setLastHealthData(data);
  p.post('health', data);
}

async function postSessions(p: AutoMemoryPanel): Promise<void> {
  const json = await p.cli().get('sessions', () => p.runCli(['list', '--json', '--limit', '10']));
  p.post('sessions', JSON.parse(json as string));
}

async function postInstructions(p: AutoMemoryPanel): Promise<void> {
  const out: Record<string, unknown> = {};
  for (const s of ['A','B','C'] as Strategy[]) {
    try { out[s] = await p.instructions().status(s); }
    catch { out[s] = { installed: false, path: '(no workspace)' }; }
  }
  p.post('instructionsStatus', out);
}

async function search(p: AutoMemoryPanel, query: string): Promise<void> {
  const json = await p.runCli(['search', query, '--json']);
  p.post('searchResult', JSON.parse(json));
}

async function showSession(p: AutoMemoryPanel, id: string): Promise<void> {
  const json = await p.runCli(['show', id, '--json']);
  p.post('sessionDetail', JSON.parse(json));
}

async function addIns(p: AutoMemoryPanel, s: Strategy): Promise<void> {
  await p.instructions().add(s);
  await postInstructions(p);
}

async function removeIns(p: AutoMemoryPanel, s: Strategy): Promise<void> {
  await p.instructions().remove(s);
  await postInstructions(p);
}
