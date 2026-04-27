import { execFile } from 'child_process';

export function run(binary: string, args: string[], timeoutMs = 10_000): Promise<string> {
  return new Promise((resolve, reject) => {
    execFile(binary, args, { timeout: timeoutMs, windowsHide: true }, (err, stdout, stderr) => {
      if (err) {
        // err.message already contains stderr in most Node versions; avoid doubling it
        const extra = stderr && !err.message.includes(stderr.trim()) ? `\n${stderr}` : '';
        reject(new Error(`${err.message}${extra}`));
      } else {
        resolve(stdout);
      }
    });
  });
}
