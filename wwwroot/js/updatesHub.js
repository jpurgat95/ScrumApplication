const connection = new signalR.HubConnectionBuilder()
    .withUrl("/updatesHub")
    .build();
// ===== WYDARZENIA =====

// Nowe wydarzenie
connection.on("EventAdded", ev => {
    const table = $('#eventsTable').DataTable();

    // Przygotowanie danych w formie tablicy lub obiektu
    const rowData = [
        ev.title,
        ev.description,
        ev.startDate,
        ev.endDate,
        `<span class="badge ${ev.isDone ? 'bg-success' : 'bg-warning text-dark'}">
            ${ev.isDone ? 'Wykonane' : 'W trakcie'}
        </span>`,
        ev.userName || '',
        `<div>
            ${ev.canEdit ? `<a href="/Events/Edit/${ev.id}" class="btn btn-sm btn-outline-secondary me-1" title="Edytuj"><i class="bi bi-pencil"></i></a>` : ''}
            <form method="post" action="/Events/ToggleDone/${ev.id}" class="d-inline me-1">
                <button type="submit" class="btn btn-sm btn-outline-primary" title="Zmień status">
                    ${ev.isDone ? '<i class="bi bi-arrow-counterclockwise"></i>' : '<i class="bi bi-check2"></i>'}
                </button>
            </form>
            ${ev.canDelete ? `<button type="button" class="btn btn-sm btn-outline-danger" data-bs-toggle="modal" data-bs-target="#deleteModal" data-id="${ev.id}" title="Usuń"><i class="bi bi-trash"></i></button>` : ''}
        </div>`
    ];

    const newRow = table.row.add(rowData).draw(false).node();
    $(newRow).attr('id', `event-${ev.id}`);

    // --- Toast ---
    var toastEl = document.getElementById('liveToast');
    var toastBody = toastEl.querySelector('.toast-body');
    toastBody.textContent = `Użytkownik ${ev.userName} dodał nowe wydarzenie!`;
    toastEl.classList.remove('bg-success', 'bg-danger', 'bg-warning', 'bg-info', 'text-dark', 'text-white');
    toastEl.classList.add('bg-success', 'text-white');
    new bootstrap.Toast(toastEl).show();
});
// Usunięcie wydarzenia
connection.on("EventDeleted", eventId => {
    const table = $('#eventsTable').DataTable();
    const row = $(`#event-${eventId}`);
    if (row.length) {
        table.row(row).remove().draw(false);
    }

    // Toast
    var toastEl = document.getElementById('liveToast');
    var toastBody = toastEl.querySelector('.toast-body');
    toastBody.textContent = `Wydarzenie zostało usunięte`;
    toastEl.classList.remove('bg-success', 'bg-danger', 'bg-warning', 'bg-info', 'text-dark', 'text-white');
    toastEl.classList.add('bg-danger', 'text-white');
    new bootstrap.Toast(toastEl).show();
});
// Aktualizacja wydarzenia
connection.on("EventUpdated", ev => {
    console.log("📩 EventUpdated odebrany:", ev);

    const table = $('#eventsTable').DataTable();
    const row = $(`#event-${ev.id}`);

    if (!row.length) {
        console.warn("⚠️ Nie znaleziono wiersza w tabeli dla ev.id:", ev.id);
        return;
    }

    const rowIndex = table.row(row).index();
    let rowData;

    if (ev.userName !== undefined) {
        // ADMIN - wiersz z kolumną "Użytkownik"
        console.log("🟢 ADMIN wersja — będzie 7 kolumn");

        rowData = [
            ev.title || '',
            ev.description || '',
            ev.startDate || '',
            ev.endDate || '',
            `<span class="badge ${ev.isDone ? 'bg-success' : 'bg-warning text-dark'}">
                ${ev.isDone ? 'Wykonane' : 'W trakcie'}
            </span>`,
            ev.userName || '',
            `<div class="d-flex gap-1">
                ${ev.canEdit ? `<a href="/Events/Edit/${ev.id}" class="btn btn-sm btn-outline-secondary" title="Edytuj"><i class="bi bi-pencil"></i></a>` : ''}
                <button class="btn btn-sm btn-outline-primary toggle-done" data-id="${ev.id}" title="Zmień status">
                    ${ev.isDone ? '<i class="bi bi-arrow-counterclockwise"></i>' : '<i class="bi bi-check2"></i>'}
                </button>
                ${ev.canDelete ? `<button type="button" class="btn btn-sm btn-outline-danger" data-bs-toggle="modal" data-bs-target="#deleteModal" data-id="${ev.id}" title="Usuń"><i class="bi bi-trash"></i></button>` : ''}
            </div>`
        ];
    } else {
        // USER - wiersz bez kolumny "Użytkownik"
        console.log("🔵 USER wersja — będzie 6 kolumn");

        rowData = [
            ev.title || '',
            ev.description || '',
            ev.startDate || '',
            ev.endDate || '',
            `<span class="badge ${ev.isDone ? 'bg-success' : 'bg-warning text-dark'}">
                ${ev.isDone ? 'Wykonane' : 'W trakcie'}
            </span>`,
            `<div class="d-flex gap-1">
                ${ev.canEdit ? `<a href="/Events/Edit/${ev.id}" class="btn btn-sm btn-outline-secondary" title="Edytuj"><i class="bi bi-pencil"></i></a>` : ''}
                <button class="btn btn-sm btn-outline-primary toggle-done" data-id="${ev.id}" title="Zmień status">
                    ${ev.isDone ? '<i class="bi bi-arrow-counterclockwise"></i>' : '<i class="bi bi-check2"></i>'}
                </button>
                ${ev.canDelete ? `<button type="button" class="btn btn-sm btn-outline-danger" data-bs-toggle="modal" data-bs-target="#deleteModal" data-id="${ev.id}" title="Usuń"><i class="bi bi-trash"></i></button>` : ''}
            </div>`
        ];
    }

    console.log("📊 rowData przygotowane:", rowData);

    // 🔄 Odśwież wiersz w DataTables
    table.row(rowIndex).data(rowData).invalidate().draw(false);
    console.log(`✅ Wiersz #${rowIndex} zaktualizowany w DataTables`);

    // 🔔 Toast
    const toastEl = document.getElementById('liveToast');
    const toastBody = toastEl.querySelector('.toast-body');
    toastBody.textContent = `Wydarzenie "${ev.title || ''}" zostało zaktualizowane`;

    toastEl.classList.remove('bg-success', 'bg-danger', 'bg-warning', 'bg-info', 'text-dark', 'text-white');
    toastEl.classList.add('bg-success', 'text-white');

    new bootstrap.Toast(toastEl).show();
});

// Delegowanie kliknięć dla przycisków toggle-done (dynamiczne wiersze)
$(document).on('click', '.toggle-done', function (e) {
    e.preventDefault();

    const id = $(this).data('id');
    const token = $('input[name="__RequestVerificationToken"]').val();

    console.log(`🟡 Toggle status event ${id}`);

    $.ajax({
        type: "POST",
        url: `/Events?handler=ToggleDone&id=${id}`,
        headers: {
            "RequestVerificationToken": token
        }
    }).done(() => console.log(`✅ Wysłano toggle dla event ${id}`))
        .fail(err => console.error("❌ Błąd toggle:", err));
});

// ===== ZADANIA =====
// Nowe zadanie
connection.on("TaskAdded", task => {
    console.log("Nowe zadanie:", task);
    const tbody = document.querySelector("#tasksTable tbody");
    if (!tbody) return;

    const row = document.createElement("tr");
    row.id = `task-${task.id}`;
    row.innerHTML = `
        <td class="task-title">${task.title}</td>
        <td class="task-desc">${task.description}</td>
        <td class="task-status">${task.isDone ? "Wykonane" : "W trakcie"}</td>
    `;
    tbody.appendChild(row);
});

// Usunięcie zadania
connection.on("TaskDeleted", taskId => {
    console.log("Usunięto zadanie o ID:", taskId);
    const row = document.getElementById(`task-${taskId}`);
    if (row) row.remove();
});

// Aktualizacja zadania
connection.on("TaskUpdated", task => {
    console.log("Zaktualizowano zadanie:", task);
    const row = document.getElementById(`task-${task.id}`);
    if (row) {
        row.querySelector(".task-title").textContent = task.title;
        row.querySelector(".task-desc").textContent = task.description;
        row.querySelector(".task-status").textContent = task.isDone ? "Wykonane" : "W trakcie";
    }
});


// Start połączenia
connection.start().catch(err => console.error(err.toString()));
