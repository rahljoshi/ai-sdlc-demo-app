const fs = require('fs');
const path = require('path');

const event = process.argv[2] || 'unknown';
const repoRoot = path.resolve(__dirname, '..', '..');
const logPath = path.join(repoRoot, 'ai-interaction-log.jsonl');

let raw = '';
process.stdin.setEncoding('utf8');
process.stdin.on('data', (chunk) => {
  raw += chunk;
});
process.stdin.on('end', () => {
  try {
    const entry = {
      timestamp: new Date().toISOString(),
      event,
      actor: process.env.CURSOR_AGENT_NAME || 'cursor-agent',
      summary: summarize(raw)
    };
    fs.appendFileSync(logPath, JSON.stringify(entry) + '\n');
  } catch {
    // Logging must not block the agent.
  }
  process.stdout.write('{}\n');
});

function summarize(input) {
  const trimmed = (input || '').trim();
  if (!trimmed) return 'no payload';
  try {
    const parsed = JSON.parse(trimmed);
    const text = firstString(parsed, [
      'summary',
      'text',
      'message',
      'prompt',
      'agent_message',
      'last_assistant_message',
      'user_message'
    ]);
    if (text) return squash(text);
    return squash(JSON.stringify(parsed));
  } catch {
    return squash(trimmed);
  }
}

function firstString(value, keys) {
  if (!value || typeof value !== 'object') return '';
  for (const key of keys) {
    if (typeof value[key] === 'string' && value[key].trim()) return value[key];
  }
  return '';
}

function squash(text) {
  return text.replace(/\s+/g, ' ').slice(0, 500);
}
