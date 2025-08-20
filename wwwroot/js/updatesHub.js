const connection = new signalR.HubConnectionBuilder()
    .withUrl("/updatesHub")
    .build();

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

// ===== WYDARZENIA =====

// Nowe wydarzenie
connection.on("EventAdded", ev => {
    console.log("Nowe wydarzenie:", ev);

    const row = document.createElement("tr");
    row.id = `event-${ev.id}`;
    row.innerHTML = `
        <td class="event-title">${ev.title}</td>
        <td class="event-desc">${ev.description}</td>
        <td class="event-start">${ev.startDate}</td>
        <td class="event-end">${ev.endDate}</td>
        <td class="event-status">
            <span class="badge ${ev.isDone ? 'bg-success' : 'bg-warning text-dark'}">
                ${ev.isDone ? 'Wykonane' : 'W trakcie'}
            </span>
        </td>
        ${ev.userName ? `<td>${ev.userName}</td>` : ''}
        <td>
            ${ev.canEdit ? `<a href="/Events/Edit/${ev.id}" class="btn btn-sm btn-outline-secondary me-1" title="Edytuj"><i class="bi bi-pencil"></i></a>` : ''}
            <form method="post" action="/Events/ToggleDone/${ev.id}" class="d-inline me-1">
                <button type="submit" class="btn btn-sm btn-outline-primary" title="Zmień status">
                    ${ev.isDone ? '<i class="bi bi-arrow-counterclockwise"></i>' : '<i class="bi bi-check2"></i>'}
                </button>
            </form>
            ${ev.canDelete ? `<button type="button" class="btn btn-sm btn-outline-danger" data-bs-toggle="modal" data-bs-target="#deleteModal" data-id="${ev.id}" title="Usuń"><i class="bi bi-trash"></i></button>` : ''}
        </td>
    `;

    document.querySelector("#tasksTable tbody").appendChild(row);
});
// Usunięcie wydarzenia
connection.on("EventDeleted", eventId => {
    console.log("Usunięto wydarzenie o ID:", eventId);
    const row = document.getElementById(`event-${eventId}`);
    if (row) row.remove();
});

// Aktualizacja wydarzenia
connection.on("EventUpdated", ev => {
    console.log("Zaktualizowano wydarzenie:", ev);
    const row = document.getElementById(`event-${ev.id}`);
    if (row) {
        row.querySelector(".event-title").textContent = ev.title;
        row.querySelector(".event-desc").textContent = ev.description;
        row.querySelector(".event-start").textContent = ev.startDate;
        row.querySelector(".event-end").textContent = ev.endDate;
        row.querySelector(".event-status").textContent = ev.isDone ? "Wykonane" : "W trakcie";
        row.querySelector(".event-user").textContent = ev.userName;
    }
});

// Start połączenia
connection.start().catch(err => console.error(err.toString()));
