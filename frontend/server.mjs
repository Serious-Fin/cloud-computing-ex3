import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';

const port = Number(process.env.PORT || 8080);
if (!Number.isInteger(port) || port < 1 || port > 65535) {
  throw new Error('PORT must be an integer between 1 and 65535.');
}

const apiBaseUrl = (process.env.API_BASE_URL || '').trim().replace(/\/+$/, '');
let apiUrl;
try {
  apiUrl = new URL(apiBaseUrl);
} catch {
  throw new Error('Set API_BASE_URL to the public API URL.');
}
if (!['http:', 'https:'].includes(apiUrl.protocol) || apiUrl.username ||
    apiUrl.password || apiUrl.search || apiUrl.hash) {
  throw new Error('API_BASE_URL must be an HTTP(S) URL without credentials, query, or fragment.');
}

// An explicit file list keeps server source and other repository files private.
const files = new Map();
for (const [name, contentType] of [
  ['index.html', 'text/html; charset=utf-8'],
  ['styles.css', 'text/css; charset=utf-8'],
  ['app.js', 'text/javascript; charset=utf-8'],
]) {
  files.set(`/${name}`, { contentType, body: await readFile(new URL(name, import.meta.url)) });
}
files.set('/', files.get('/index.html'));
files.set('/config.js', {
  contentType: 'text/javascript; charset=utf-8',
  body: `globalThis.APP_CONFIG = ${JSON.stringify({ apiBaseUrl })};\n`,
});
files.set('/healthz', { contentType: 'text/plain; charset=utf-8', body: 'ok\n' });

createServer((request, response) => {
  if (request.method !== 'GET' && request.method !== 'HEAD') {
    response.writeHead(405, { Allow: 'GET, HEAD' });
    response.end();
    return;
  }
  const pathname = (request.url || '/').split('?')[0];
  const file = files.get(pathname);
  if (!file) {
    response.writeHead(404);
    response.end();
    return;
  }
  response.writeHead(200, {
    'Content-Type': file.contentType,
    'Cache-Control': 'no-store',
    'X-Content-Type-Options': 'nosniff',
  });
  response.end(request.method === 'HEAD' ? undefined : file.body);
}).listen(port, '0.0.0.0', () => {
  console.log(`Frontend listening on port ${port}`);
});
