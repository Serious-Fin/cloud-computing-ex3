const fs = require('node:fs');
const vm = require('node:vm');
const assert = require('node:assert/strict');
const path = require('node:path');

const html = fs.readFileSync(path.join(__dirname, '../../frontend/index.html'), 'utf8');
function element() {
  return {
    textContent: '', children: [],
    addEventListener() {},
    appendChild(child) { this.children.push(child); },
    insertRow() { const row = element(); this.children.push(row); return row; },
    insertCell() { const cell = element(); this.children.push(cell); return cell; },
  };
}
// Match the real HTML: missing elements must return null, as in a browser.
const elements = new Map([...html.matchAll(/\bid="([^"]+)"/g)]
  .map((match) => [match[1], element()]));
const requests = [];
const context = vm.createContext({
  document: {
    getElementById: (id) => elements.get(id) ?? null,
    createElement: () => element(),
  },
  location: { protocol: 'https:', hostname: 'cloud-computing-ex3-web.onrender.com' },
  fetch: async (url) => {
    requests.push(url);
    return { ok: true, json: async () => [{
      id: 42, brand: 'Michelin', type: 'Winter', rimDiameter: 17,
      price: 129.99, imageUrl: 'https://example.com/tire.png',
      viewsLastHour: 12, viewsUpdatedAt: '2026-10-04T12:00:00Z',
    }] };
  },
});
vm.runInContext(fs.readFileSync(path.join(__dirname, '../../frontend/app.js'), 'utf8'), context);
setImmediate(() => {
  assert.deepEqual(requests, ['https://cloud-computing-ex3-api.onrender.com/tires']);
  assert.equal(elements.get('message').textContent, '');
  const rows = elements.get('tire-table-body').children;
  assert.equal(rows.length, 1);
  assert.equal(rows[0].children.length, 8);
  assert.equal(rows[0].children[1].textContent, 'Michelin');
  assert.equal(rows[0].children[5].children[0].children[0].src, 'https://example.com/tire.png');
  assert.equal(rows[0].children[6].textContent, 12);
  assert.equal(rows[0].children[7].children.length, 2);
  console.log('Frontend startup and tire table checks passed.');
});
