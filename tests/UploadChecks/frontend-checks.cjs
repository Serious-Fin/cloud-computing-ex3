const fs = require('node:fs');
const vm = require('node:vm');
const assert = require('node:assert/strict');
const path = require('node:path');

const elements = new Map();
const document = {
  getElementById(id) {
    if (!elements.has(id)) elements.set(id, {
      value: '', files: [], textContent: '', addEventListener() {}, reset() {},
    });
    return elements.get(id);
  },
};
const requests = [];
const context = vm.createContext({
  document, location: { protocol: 'file:', hostname: '' }, FormData,
  fetch: async (url, options) => {
    if (options) requests.push({ url, ...options });
    return { ok: true, json: async () => [] };
  },
});
// Suppress table rendering; the checks focus on the actual browser/API upload contract.
const code = fs.readFileSync(path.join(__dirname, '../../frontend/app.js'), 'utf8');
vm.runInContext(code.replace('loadTires();', ''), context);
vm.runInContext('loadTires = async () => {};', context);

(async () => {
  document.getElementById('brand').value = 'Michelin';
  document.getElementById('type').value = 'Winter';
  document.getElementById('rimDiameter').value = '17';
  document.getElementById('price').value = '129.99';
  const image = new Blob(['image bytes'], { type: 'image/png' });
  document.getElementById('image').files = [image];
  await context.onSubmitForm({ preventDefault() {} });
  assert.equal(requests[0].method, 'POST');
  assert.equal(requests[0].body.get('brand'), 'Michelin');
  assert.equal(requests[0].body.get('price'), '129.99');
  assert.equal(requests[0].body.get('image').size, image.size);
  assert.equal(requests[0].headers, undefined, 'Browser must set multipart boundary');

  vm.runInContext('editingId = 42;', context);
  document.getElementById('image').files = [];
  await context.onSubmitForm({ preventDefault() {} });
  assert.equal(requests[1].method, 'PUT');
  assert.ok(requests[1].url.endsWith('/tires/42'));
  assert.equal(requests[1].body.has('image'), false, 'Editing without a file preserves the image');
  assert.equal(document.getElementById('submit-button').disabled, false);
  await context.showApiErrors({
    status: 503,
    text: async () => JSON.stringify({ title: 'Service Unavailable', detail: 'Image storage is not configured.' }),
  });
  assert.equal(document.getElementById('message').textContent, 'Image storage is not configured.');
  await context.showApiErrors({
    status: 503,
    text: async () => JSON.stringify({ title: 'Service Unavailable' }),
  });
  assert.equal(document.getElementById('message').textContent, 'Service Unavailable');
  console.log('Frontend multipart upload checks passed.');
})().catch(error => { console.error(error); process.exitCode = 1; });
