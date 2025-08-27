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
                <button class="btn btn-sm btn-outline-primary toggle-done" data-id="${ev.id}" title="Zmień status">
                    ${ev.isDone ? '<i class="bi bi-arrow-counterclockwise"></i>' : '<i class="bi bi-check2"></i>'}
                </button>
            ${ev.canDelete ? `<button type="button" class="btn btn-sm btn-outline-danger" data-bs-toggle="modal" data-bs-target="#deleteModal" data-id="${ev.id}" title="Usuń"><i class="bi bi-trash"></i></button>` : ''}
        </div>`
    ];

    const newRow = table.row.add(rowData).draw(false).node();
    $(newRow).attr('id', `event-${ev.id}`);

    // Odświeżenie FullCalendar, jeśli jest zainicjalizowany
    if (window.calendar) {
        calendar.refetchEvents();
        console.log(`🔄 FullCalendar odświeżony po dodaniu eventu #${ev.id}`);
    }

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

    // Odświeżenie FullCalendar, jeśli jest zainicjalizowany
    if (window.calendar) {
        calendar.refetchEvents();
        console.log(`🔄 FullCalendar odświeżony po aktualizacji event #${ev.id}`);
    }
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

    // Odświeżenie FullCalendar, jeśli jest zainicjalizowany
    if (window.calendar) {
        calendar.refetchEvents();
        console.log(`🔄 FullCalendar odświeżony po aktualizacji event #${ev.id}`);
    }

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
// Wydarzenie aktualizujące zadania
connection.on("EventUpdatesTask", task => {
    console.log("📩 EventUpdatesTask odebrany:", task);

    const table = $('#tasksTable').DataTable();
    const row = $(`#task-${task.id}`);

    if (!row.length) {
        console.warn("⚠️ Nie znaleziono wiersza w tabeli dla task.id:", task.id);
        return;
    }

    const rowIndex = table.row(row).index();
    let rowData;

    if (task.userName !== undefined) {
        // ADMIN - wiersz z kolumną "Użytkownik"
        rowData = [
            task.title || '',
            task.description || '',
            task.startDate || '',
            task.endDate || '',
            `<span class="badge ${task.isDone ? 'bg-success' : 'bg-warning text-dark'}">
            ${task.isDone ? 'Wykonane' : 'W trakcie'}
        </span>`,
            task.userName || '',
            `<div class="d-flex gap-1">
            ${task.canEdit
                ? `<a href="/Tasks/Edit/${task.id}" class="btn btn-sm btn-outline-secondary" title="Edytuj">
                      <i class="bi bi-pencil"></i>
                   </a>`
                : `<button type="button" class="btn btn-sm btn-secondary me-1" title="Nie można edytować" disabled>
                      <i class="bi bi-lock"></i>
                   </button>`
            }
            ${task.scrumEventDone
                ? `<button class="btn btn-sm btn-secondary" title="Nie można zmienić statusu" disabled>
                      ${task.isDone ? '<i class="bi bi-arrow-counterclockwise"></i>' : '<i class="bi bi-check2"></i>'}
                   </button>`
                : `<button class="btn btn-sm btn-outline-primary toggle-done-task" data-id="${task.id}" title="Zmień status" ${!task.canEdit ? 'disabled' : ''}>
                      ${task.isDone ? '<i class="bi bi-arrow-counterclockwise"></i>' : '<i class="bi bi-check2"></i>'}
                   </button>`
            }
            ${task.canDelete
                ? `<button type="button" class="btn btn-sm btn-outline-danger" data-bs-toggle="modal" data-bs-target="#deleteModal" data-id="${task.id}" title="Usuń">
                      <i class="bi bi-trash"></i>
                   </button>`
                : ''
            }
        </div>`
        ];
    } else {
        // USER - wiersz bez kolumny "Użytkownik"
        rowData = [
            task.title || '',
            task.description || '',
            task.startDate || '',
            task.endDate || '',
            `<span class="badge ${task.isDone ? 'bg-success' : 'bg-warning text-dark'}">
            ${task.isDone ? 'Wykonane' : 'W trakcie'}
        </span>`,
            `<div class="d-flex gap-1">
            ${task.canEdit
                ? `<a href="/Tasks/Edit/${task.id}" class="btn btn-sm btn-outline-secondary" title="Edytuj">
                      <i class="bi bi-pencil"></i>
                   </a>`
                : `<button type="button" class="btn btn-sm btn-secondary me-1" title="Nie można edytować" disabled>
                      <i class="bi bi-lock"></i>
                   </button>`
            }
            ${task.scrumEventDone
                ? `<button class="btn btn-sm btn-secondary" title="Nie można zmienić statusu" disabled>
                      ${task.isDone ? '<i class="bi bi-arrow-counterclockwise"></i>' : '<i class="bi bi-check2"></i>'}
                   </button>`
                : `<button class="btn btn-sm btn-outline-primary toggle-done-task" data-id="${task.id}" title="Zmień status" ${!task.canEdit ? 'disabled' : ''}>
                      ${task.isDone ? '<i class="bi bi-arrow-counterclockwise"></i>' : '<i class="bi bi-check2"></i>'}
                   </button>`
            }
            ${task.canDelete
                ? `<button type="button" class="btn btn-sm btn-outline-danger" data-bs-toggle="modal" data-bs-target="#deleteModal" data-id="${task.id}" title="Usuń">
                      <i class="bi bi-trash"></i>
                   </button>`
                : ''
            }
        </div>`
        ];
    }
    // Odśwież wiersz w DataTables
    table.row(rowIndex).data(rowData).invalidate().draw(false);

    // Odśwież kalendarz, jeśli istnieje
    if (window.calendar) {
        calendar.refetchEvents();
        console.log(`🔄 FullCalendar odświeżony po aktualizacji zadania #${task.id}`);
    }

    // Toast
    const toastEl = document.getElementById('liveToast');
    const toastBody = toastEl.querySelector('.toast-body');
    toastEl.classList.remove('bg-success', 'bg-danger', 'bg-warning', 'bg-info', 'text-dark', 'text-white');
    if (task.scrumEventDone) {
        toastBody.textContent = `Edycja zadania "${task.title || ''}" została zablokowana - powiązane wydarzenie zostało wykonane!`;
        toastEl.classList.add('bg-warning', 'text-white');
    } else {
        toastBody.textContent = `Edycja zadania "${task.title || ''}" jest teraz możliwa - powiązane wydarzenie jest niewykonane.`;
            toastEl.classList.add('bg-success', 'text-white');
    }
    new bootstrap.Toast(toastEl).show();
});


// ===== ZADANIA =====
// Nowe zadanie
connection.on("TaskAdded", task => {
    const table = $('#tasksTable').DataTable();

    // Przygotowanie danych w formie tablicy lub obiektu
    const rowData = [
        task.title,
        task.description,
        task.startDate,
        task.endDate,
        `<span class="badge ${task.isDone ? 'bg-success' : 'bg-warning text-dark'}">
            ${task.isDone ? 'Wykonane' : 'W trakcie'}
        </span>`,
        task.userName || '',
        `<div class="d-flex gap-1">
                ${task.canEdit ? `<a href="/Tasks/Edit/${task.id}" class="btn btn-sm btn-outline-secondary" title="Edytuj"><i class="bi bi-pencil"></i></a>` : ''}
                <button class="btn btn-sm btn-outline-primary toggle-done-task" data-id="${task.id}" title="Zmień status">
                    ${task.isDone ? '<i class="bi bi-arrow-counterclockwise"></i>' : '<i class="bi bi-check2"></i>'}
                </button>
                ${task.canDelete ? `<button type="button" class="btn btn-sm btn-outline-danger" data-bs-toggle="modal" data-bs-target="#deleteModal" data-id="${task.id}" title="Usuń"><i class="bi bi-trash"></i></button>` : ''}
         </div>`
    ];

    const newRow = table.row.add(rowData).draw(false).node();
    $(newRow).attr('id', `task-${task.id}`);

    // Odświeżenie FullCalendar, jeśli jest zainicjalizowany
    if (window.calendar) {
        calendar.refetchEvents();
        console.log(`🔄 FullCalendar odświeżony po dodaniu zadania #${task.id}`);
    }

    // --- Toast ---
    var toastEl = document.getElementById('liveToast');
    var toastBody = toastEl.querySelector('.toast-body');
    toastBody.textContent = `Użytkownik ${task.userName} dodał nowe zadanie!`;
    toastEl.classList.remove('bg-success', 'bg-danger', 'bg-warning', 'bg-info', 'text-dark', 'text-white');
    toastEl.classList.add('bg-success', 'text-white');
    new bootstrap.Toast(toastEl).show();
});
// Usunięcie zadania
connection.on("TaskDeleted", taskId => {
    const table = $('#tasksTable').DataTable();
    const row = $(`#task-${taskId}`);
    if (row.length) {
        table.row(row).remove().draw(false);
    }

    // Toast
    var toastEl = document.getElementById('liveToast');
    var toastBody = toastEl.querySelector('.toast-body');
    toastBody.textContent = `Zadanie zostało usunięte`;
    toastEl.classList.remove('bg-success', 'bg-danger', 'bg-warning', 'bg-info', 'text-dark', 'text-white');
    toastEl.classList.add('bg-danger', 'text-white');
    new bootstrap.Toast(toastEl).show();

    // Odświeżenie FullCalendar, jeśli jest zainicjalizowany
    if (window.calendar) {
        calendar.refetchEvents();
        console.log(`🔄 FullCalendar odświeżony po aktualizacji event #${ev.id}`);
    }
});
// Aktualizacja zadania
connection.on("TaskUpdated", task => {
    console.log("📩 TaskUpdated odebrany:", task);

    const table = $('#tasksTable').DataTable();
    const row = $(`#task-${task.id}`);

    if (!row.length) {
        console.warn("⚠️ Nie znaleziono wiersza w tabeli dla task.id:", task.id);
        return;
    }

    const rowIndex = table.row(row).index();
    let rowData;

    if (task.userName !== undefined) {
        // ADMIN - wiersz z kolumną "Użytkownik"
        console.log("🟢 ADMIN wersja — będzie 7 kolumn");

        rowData = [
            task.title || '',
            task.description || '',
            task.startDate || '',
            task.endDate || '',
            `<span class="badge ${task.isDone ? 'bg-success' : 'bg-warning text-dark'}">
                ${task.isDone ? 'Wykonane' : 'W trakcie'}
            </span>`,
            task.userName || '',
            `<div class="d-flex gap-1">
                ${task.canEdit ? `<a href="/Tasks/Edit/${task.id}" class="btn btn-sm btn-outline-secondary" title="Edytuj"><i class="bi bi-pencil"></i></a>` : ''}
                <button class="btn btn-sm btn-outline-primary toggle-done-task" data-id="${task.id}" title="Zmień status">
                    ${task.isDone ? '<i class="bi bi-arrow-counterclockwise"></i>' : '<i class="bi bi-check2"></i>'}
                </button>
                ${task.canDelete ? `<button type="button" class="btn btn-sm btn-outline-danger" data-bs-toggle="modal" data-bs-target="#deleteModal" data-id="${task.id}" title="Usuń"><i class="bi bi-trash"></i></button>` : ''}
            </div>`
        ];
    } else {
        // USER - wiersz bez kolumny "Użytkownik"
        console.log("🔵 USER wersja — będzie 6 kolumn");

        rowData = [
            task.title || '',
            task.description || '',
            task.startDate || '',
            task.endDate || '',
            `<span class="badge ${task.isDone ? 'bg-success' : 'bg-warning text-dark'}">
                ${task.isDone ? 'Wykonane' : 'W trakcie'}
            </span>`,
            `<div class="d-flex gap-1">
                ${task.canEdit ? `<a href="/Tasks/Edit/${task.id}" class="btn btn-sm btn-outline-secondary" title="Edytuj"><i class="bi bi-pencil"></i></a>` : ''}
                <button class="btn btn-sm btn-outline-primary toggle-done-task" data-id="${task.id}" title="Zmień status">
                    ${task.isDone ? '<i class="bi bi-arrow-counterclockwise"></i>' : '<i class="bi bi-check2"></i>'}
                </button>
                ${task.canDelete ? `<button type="button" class="btn btn-sm btn-outline-danger" data-bs-toggle="modal" data-bs-target="#deleteModal" data-id="${task.id}" title="Usuń"><i class="bi bi-trash"></i></button>` : ''}
            </div>`
        ];
    }

    console.log("📊 rowData przygotowane:", rowData);

    // 🔄 Odśwież wiersz w DataTables
    table.row(rowIndex).data(rowData).invalidate().draw(false);
    console.log(`✅ Wiersz #${rowIndex} zaktualizowany w DataTables`);

    // Odświeżenie FullCalendar, jeśli jest zainicjalizowany
    if (window.calendar) {
        calendar.refetchEvents();
        console.log(`🔄 FullCalendar odświeżony po aktualizacji zadania #${task.id}`);
    }

    // 🔔 Toast
    const toastEl = document.getElementById('liveToast');
    const toastBody = toastEl.querySelector('.toast-body');
    toastBody.textContent = `Zadanie "${task.title || ''}" zostało zaktualizowane`;

    toastEl.classList.remove('bg-success', 'bg-danger', 'bg-warning', 'bg-info', 'text-dark', 'text-white');
    toastEl.classList.add('bg-success', 'text-white');

    new bootstrap.Toast(toastEl).show();
});


// Delegowanie kliknięć toggle-done dla zadań
$(document).on('click', '.toggle-done-task', function (e) {
    e.preventDefault();

    const id = $(this).data('id');
    const token = $('input[name="__RequestVerificationToken"]').val();
    console.log(`🟡 Toggle status task ${id}`);

    $.ajax({
        type: "POST",
        url: `/Tasks?handler=ToggleDone&id=${id}`,
        headers: {
            "RequestVerificationToken": token
        }
    });
});

// Start połączenia
connection.start().catch(err => console.error(err.toString()));
