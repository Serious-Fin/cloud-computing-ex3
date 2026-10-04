// Update the deployed URL if Render assigns a different API hostname.
const API_BASE =
  location.protocol === "file:" ||
  location.hostname === "localhost" ||
  location.hostname === "127.0.0.1"
    ? "http://localhost:5016"
    : "https://cloud-computing-ex3-api.onrender.com";

const apiBaseSpan = document.getElementById("api-base");
const messageEl = document.getElementById("message");
const errorsEl = document.getElementById("errors");
const formEl = document.getElementById("tire-form");
const formTitleEl = document.getElementById("form-title");
const submitButton = document.getElementById("submit-button");
const cancelButton = document.getElementById("cancel-button");
const refreshButton = document.getElementById("refresh-button");
const tableBody = document.getElementById("tire-table-body");

let tires = [];
let editingId = null;

apiBaseSpan.textContent = API_BASE;
formEl.addEventListener("submit", onSubmitForm);
cancelButton.addEventListener("click", cancelEdit);
refreshButton.addEventListener("click", loadTires);

loadTires();

async function loadTires() {
  clearMessage();
  try {
    const response = await fetch(`${API_BASE}/tires`);
    if (!response.ok) {
      showMessage("Could not load tires (HTTP " + response.status + ").");
      return;
    }

    tires = await response.json();
    renderTires();
  } catch {
    showMessage("Could not reach the API. Is it running on " + API_BASE + "?");
  }
}

async function onSubmitForm(event) {
  event.preventDefault();
  clearMessage();

  const imageInput = document.getElementById("image");
  const image = imageInput.files[0];
  if (image && image.size > 5 * 1024 * 1024) {
    showMessage("Choose an image no larger than 5 MB.");
    return;
  }
  const tire = new FormData();
  tire.append("brand", document.getElementById("brand").value.trim());
  tire.append("type", document.getElementById("type").value);
  tire.append("rimDiameter", document.getElementById("rimDiameter").value);
  tire.append("price", document.getElementById("price").value);
  if (image) tire.append("image", image);
  const isEdit = editingId !== null;
  const url = isEdit ? `${API_BASE}/tires/${editingId}` : `${API_BASE}/tires`;
  const method = isEdit ? "PUT" : "POST";

  submitButton.disabled = true;
  try {
    const response = await fetch(url, {
      method,
      body: tire,
    });

    if (!response.ok) {
      await showApiErrors(response);
      return;
    }

    cancelEdit();
    await loadTires();
    showMessage(isEdit ? "Tire updated." : "Tire created.");
  } catch {
    showMessage("Request failed. Is the API running?");
  } finally {
    submitButton.disabled = false;
  }
}

async function deleteTire(id) {
  clearMessage();
  try {
    const response = await fetch(`${API_BASE}/tires/${id}`, { method: "DELETE" });
    if (!response.ok) {
      showMessage("Delete failed (HTTP " + response.status + ").");
      return;
    }

    if (editingId === id) {
      cancelEdit();
    }
    await loadTires();
    showMessage("Tire deleted.");
  } catch {
    showMessage("Request failed. Is the API running?");
  }
}

function startEdit(id) {
  const tire = tires.find((t) => t.id === id);
  if (!tire) return;

  editingId = tire.id;

  document.getElementById("brand").value = tire.brand ?? "";
  document.getElementById("type").value = tire.type ?? "";
  document.getElementById("rimDiameter").value = tire.rimDiameter;
  document.getElementById("price").value = tire.price;
  document.getElementById("image").value = "";
  document.getElementById("image").required = false;
  document.getElementById("image-help").textContent = "Leave empty to keep the current image, or choose a replacement (max 5 MB).";

  formTitleEl.textContent = "Edit tire #" + tire.id;
  submitButton.textContent = "Save";
  cancelButton.hidden = false;
  clearMessage();
}

function cancelEdit() {
  editingId = null;
  formEl.reset();
  document.getElementById("image").required = true;
  document.getElementById("image-help").textContent = "JPEG, PNG, or WebP; max 5 MB.";
  formTitleEl.textContent = "Add a tire";
  submitButton.textContent = "Add";
  cancelButton.hidden = true;
  clearMessage();
}

function renderTires() {
  tableBody.innerHTML = "";

  if (tires.length === 0) {
    const row = tableBody.insertRow();
    const cell = row.insertCell();
    cell.colSpan = 8;
    cell.textContent = "No tires yet.";
    return;
  }

  for (const tire of tires) {
    const row = tableBody.insertRow();

    row.insertCell().textContent = tire.id;
    row.insertCell().textContent = tire.brand;
    row.insertCell().textContent = tire.type;
    row.insertCell().textContent = tire.rimDiameter;
    row.insertCell().textContent = tire.price;

    const imageCell = row.insertCell();
    const link = document.createElement("a");
    link.href = tire.imageUrl;
    link.target = "_blank";
    link.rel = "noopener";
    link.textContent = "image";
    imageCell.appendChild(link);
    imageCell.appendChild(document.createElement("br"));
    const image = document.createElement("img");
    image.src = tire.imageUrl;
    image.alt = tire.brand ?? "tire";
    imageCell.appendChild(image);

    const viewsCell = row.insertCell();
    viewsCell.textContent = tire.viewsUpdatedAt ? tire.viewsLastHour : "Pending update";
    if (tire.viewsUpdatedAt) {
      viewsCell.title = "Last updated: " + new Date(tire.viewsUpdatedAt).toLocaleString();
    }

    const actionsCell = row.insertCell();

    const editButton = document.createElement("button");
    editButton.textContent = "Edit";
    editButton.addEventListener("click", () => startEdit(tire.id));
    actionsCell.appendChild(editButton);

    const deleteButton = document.createElement("button");
    deleteButton.textContent = "Delete";
    deleteButton.addEventListener("click", () => deleteTire(tire.id));
    actionsCell.appendChild(deleteButton);
  }
}

async function showApiErrors(response) {
  errorsEl.textContent = "";

  const text = await response.text();

  let problem = null;
  try {
    problem = JSON.parse(text);
  } catch {
    // The body was not JSON (for example a plain 400 from model binding).
  }

  if (problem && problem.errors) {
    const lines = [];
    for (const field of Object.keys(problem.errors)) {
      lines.push(field + ": " + problem.errors[field].join(" "));
    }
    errorsEl.textContent = lines.join("\n");
    showMessage("The API rejected the input.");
  } else if (problem && problem.title) {
    showMessage(problem.title);
  } else if (text) {
    // Keep only the first line so a server stack trace is not dumped on the page.
    showMessage("Request failed (HTTP " + response.status + "): " + text.split("\n")[0].slice(0, 200));
  } else {
    showMessage("Request failed (HTTP " + response.status + ").");
  }
}

function showMessage(text) {
  messageEl.textContent = text;
}

function clearMessage() {
  messageEl.textContent = "";
  errorsEl.textContent = "";
}
