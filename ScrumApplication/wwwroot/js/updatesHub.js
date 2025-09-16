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
    toastBody.textContent = `Wydarzenie i powiązanie z nim zadania zostały usunięte`;
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
        console.log(`🔄 FullCalendar odświeżony po aktualizacji zadania #${task.id}`);
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
//MIESZANE
// Wydarzenie aktualizujące zadania
connection.on("EventUpdatesTask", tasks => {
    console.log("📩 EventUpdatesTask odebrany:", tasks);
    const table = $('#tasksTable').DataTable();

    tasks.forEach(task => {
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

        table.row(rowIndex).data(rowData).invalidate().draw(false);

        if (window.calendar) {
            calendar.refetchEvents();
            console.log(`🔄 FullCalendar odświeżony po aktualizacji zadania #${task.id}`);
        }

        const toastEl = document.getElementById('liveToast');
        const toastBody = toastEl.querySelector('.toast-body');
        toastEl.classList.remove('bg-success', 'bg-danger', 'bg-warning', 'bg-info', 'text-dark', 'text-white');
        if (task.scrumEventDone) {
            toastBody.textContent = `Edycja zadania "${task.title || ''}" została zablokowana - powiązane wydarzenie zostało wykonane!`;
            toastEl.classList.add('bg-warning', 'text-white');
        } else {
            toastBody.textContent = `Edycja zadania "${task.title || ''}" jest teraz możliwa - powiązane wydarzenie oznaczono jako niewykonane.`;
            toastEl.classList.add('bg-success', 'text-white');
        }
        new bootstrap.Toast(toastEl).show();
    });
});
function showToast(message, bgClass = 'bg-info', textClass = 'text-white') {
    const toastEl = document.getElementById('liveToast');
    const toastBody = toastEl.querySelector('.toast-body');
    toastEl.className = 'toast'; // reset klas
    toastBody.textContent = message;
    toastEl.classList.add(bgClass, textClass);

    const bsToast = new bootstrap.Toast(toastEl);
    bsToast.show();
}

// Blokowanie edycji
connection.on("BlockTaskEdit", function (taskId) {
    console.log(`📩 Event BlockTaskEdit odebrany dla taskId: ${taskId} na stronie edycji`);
    if (taskId === currentTaskId) {
        console.log(`🔒 Blokowanie formularza dla zadania o ID: ${taskId}`);
        document.querySelectorAll('input, select, textarea, button[type="submit"]').forEach(el => el.disabled = true);
        // 🔔 Toast
        const toastEl = document.getElementById('liveToast');
        const toastBody = toastEl.querySelector('.toast-body');
        toastBody.textContent = "Edycja tego zadania została zablokowana, ponieważ powiązane wydarzenie zostało wykonane.";
        toastEl.classList.remove('bg-success', 'bg-danger', 'bg-warning', 'bg-info', 'text-dark', 'text-white');
        toastEl.classList.add('bg-warning', 'text-white');
        new bootstrap.Toast(toastEl).show();
    }
});
// Odblokowywanie edycji
connection.on("UnblockTaskEdit", function (taskId) {
    console.log(`📩 Event UnblockTaskEdit odebrany dla taskId: ${taskId} na stronie edycji`);
    if (taskId === currentTaskId) {
        console.log(`🔓 Odblokowanie formularza dla zadania o ID: ${taskId}`);
        document.querySelectorAll('input, select, textarea, button[type="submit"]').forEach(el => el.disabled = false);
        // 🔔 Toast
        const toastEl = document.getElementById('liveToast');
        const toastBody = toastEl.querySelector('.toast-body');
        toastBody.textContent = "Edycja tego zadania została odblokowana.";
        toastEl.classList.remove('bg-success', 'bg-danger', 'bg-warning', 'bg-info', 'text-dark', 'text-white');
        toastEl.classList.add('bg-success', 'text-white');
        new bootstrap.Toast(toastEl).show();
    }
});
//Wylogowanie po usunięciu użytkownika
connection.on("ForceLogoutWithToast", function () {
    console.log("Otrzymano sygnał wylogowania. Wyświetlam toast i przekierowuję na /Logout");

    const toastEl = document.getElementById('liveToast');
    const toastBody = toastEl.querySelector('.toast-body');
    toastBody.textContent = "Użytkownik został usunięty";

    toastEl.classList.remove('bg-success', 'bg-danger', 'bg-warning', 'bg-info', 'text-dark', 'text-white');
    toastEl.classList.add('bg-info', 'text-white');

    const bsToast = new bootstrap.Toast(toastEl);
    bsToast.show();

    // Po wyświetleniu toastu, po 2 sekundach przekieruj:
    setTimeout(() => {
        window.location.href = '/Logout';
    }, 2000);
});
// Wymuszenie resetu hasła
connection.on("ForcePasswordReset", (resetUrl) => {
    var toastEl = document.getElementById('liveToast');
    var toast = new bootstrap.Toast(toastEl);
    toast.show();

    // Po pokazaniu toastu, po 4s, przekieruj:
    setTimeout(() => {
        window.location.href = resetUrl;
    }, 3000);
});
// Powiadomienie o nowej rejestracji użytkownika (tylko dla adminów)
connection.on("UserRegistered", (userName, userId) => {
    const toastEl = document.getElementById('liveToast');
    const toastBody = toastEl.querySelector('.toast-body');

    toastBody.textContent = `Nowy użytkownik: ${userName} został zarejestrowany.`;
    toastEl.classList.remove('bg-success', 'bg-danger', 'bg-warning', 'bg-info', 'text-dark', 'text-white');
    toastEl.classList.add('bg-info', 'text-white');
    const bsToast = new bootstrap.Toast(toastEl);
    bsToast.show();

    const tbody = document.querySelector("table tbody");
    if (!tbody) {
        console.warn("Nie znaleziono elementu tbody w tabeli użytkowników!");
        return;
    }

    // Token CSRF
    const csrfTokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    const csrfToken = csrfTokenInput ? csrfTokenInput.value : null;
    if (!csrfToken) {
        console.warn("Nie znaleziono tokena __RequestVerificationToken na stronie!");
    } else {
        console.log("CSRF token znaleziony:", csrfToken);
    }

    // Utwórz nowy wiersz
    const tr = document.createElement("tr");
    tr.innerHTML = `
        <td>${userName}</td>
        <td>Użytkownik</td>
        <td>
            <form method="post" class="d-flex gap-2" action="/Admin/AdminPanel?handler=ForcePasswordReset">
                <input type="hidden" name="userId" value="${userId}" />
                <button type="submit" class="btn btn-warning btn-sm">Wymuś reset</button>
                <input name="__RequestVerificationToken" type="hidden" value="${csrfToken || ''}" />
            </form>
        </td>
        <td>
            <form method="post" id="deleteForm" style="display:none;" action="/Admin/AdminPanel?handler=DeleteUser">
                <input name="__RequestVerificationToken" type="hidden" value="${csrfToken || ''}" />
            </form>
            <button type="button" class="btn btn-danger btn-sm" data-bs-toggle="modal" data-bs-target="#confirmDeleteModal" data-user-id="${userId}">
                Usuń
            </button>
        </td>
    `;

    // Wstaw tr na początek tbody
    tbody.prepend(tr);
    console.log('Nowy użytkownik został dodany do tabeli:', userName);
});
//Usuwanie powiązanych zadań po usunięciu wydarzenia
connection.on("RelatedTasksDeleted", (eventId, taskIds) => {
    const table = $('#tasksTable').DataTable();
    taskIds.forEach(taskId => {
        const row = $(`#task-${taskId}`);
        if (row.length) {
            table.row(row).remove().draw(false);
        }
    });

    // Toast
    var toastEl = document.getElementById('liveToast');
    var toastBody = toastEl.querySelector('.toast-body');
    toastBody.textContent = `Wydarzenie ${taskId} oraz zadania z nim powiązane usunięto.`;
    toastEl.classList.remove('bg-success', 'bg-danger', 'bg-warning', 'bg-info', 'text-dark', 'text-white');
    toastEl.classList.add('bg-danger', 'text-white');
    new bootstrap.Toast(toastEl).show();

    // Odświeżenie kalendarza JS (jeśli wymagane)
    if (window.calendar) {
        calendar.refetchEvents();
        console.log(`🔄 FullCalendar odświeżony po usunięciu powiązanych zadań wydarzenia #${eventId}`);
    }
});
// Aktualizacja listy wydarzeń w formularzu dodawania/edycji zadania
connection.on("EventsListUpdated", (events) => {
    const select = document.querySelector('select[name="EventId"]');
    if (!select) return;

    // Usuń wszystkie istniejące opcje oprócz domyślnej (value="")
    select.querySelectorAll('option:not([value=""])').forEach(option => option.remove());

    // Dodaj nowe opcje z aktualnej listy wydarzeń
    events.forEach(ev => {
        const option = document.createElement('option');
        option.value = ev.id;
        option.textContent = `${ev.title} (${new Date(ev.startDate).toLocaleString()} : ${new Date(ev.endDate).toLocaleString()})`;
        select.appendChild(option);
    });
});
// Blokowanie edycji po usunięciu zadania
connection.on("BlockEditWhenTaskDeleted", function (taskId) {
    console.log(`📩 Event BlockEditWhenDeleted odebrany dla taskId: ${taskId} na stronie edycji`);
    if (taskId === currentTaskId) {
        console.log(`🔒 Blokowanie formularza dla usuniętego zadania o ID: ${taskId}`);
        document.querySelectorAll('input, select, textarea, button[type="submit"]').forEach(el => el.disabled = true);
        // 🔔 Toast
        const toastEl = document.getElementById('liveToast');
        const toastBody = toastEl.querySelector('.toast-body');
        toastBody.textContent = "Edycja zablokowana - zadanie zostało usunięte";
        toastEl.classList.remove('bg-success', 'bg-danger', 'bg-warning', 'bg-info', 'text-dark', 'text-white');
        toastEl.classList.add('bg-warning', 'text-white');
        new bootstrap.Toast(toastEl).show();

        // Przekierowanie po 3 sekundach
        setTimeout(() => {
            window.location.href = "https://localhost:7264/Tasks";
        }, 3000);
    }
});
// Blokowanie edycji wydarzenia po jego usunięciu
connection.on("BlockEditWhenEventDeleted", function (eventId) {
    console.log(`📩 Event BlockEditWhenEventDeleted odebrany dla eventId: ${eventId} na stronie edycji`);
    if (eventId === currentEventId) {
        console.log(`🔒 Blokowanie formularza dla usuniętego wydarzenia o ID: ${eventId}`);
        document.querySelectorAll('input, select, textarea, button[type="submit"]').forEach(el => el.disabled = true);

        // Wyświetlenie toastu z komunikatem
        const toastEl = document.getElementById('liveToast');
        const toastBody = toastEl.querySelector('.toast-body');
        toastBody.textContent = "Edycja zablokowana - wydarzenie zostało usunięte";
        toastEl.classList.remove('bg-success', 'bg-danger', 'bg-warning', 'bg-info', 'text-dark', 'text-white');
        toastEl.classList.add('bg-warning', 'text-white');
        new bootstrap.Toast(toastEl).show();

        // Opcjonalnie przekierowanie po 3 sekundach
        setTimeout(() => {
            window.location.href = "https://localhost:7264/Events"; // adres strony z wydarzeniami
        }, 3000);
    }
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