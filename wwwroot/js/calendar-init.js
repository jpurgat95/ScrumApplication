function initCalendar(apiUrl) {
    var calendarEl = document.getElementById('calendar');

    if (!calendarEl) {
        console.warn("Brak elementu #calendar na stronie – kalendarz nie zostanie załadowany");
        return;
    }

    var calendar = new FullCalendar.Calendar(calendarEl, {
        initialView: 'dayGridMonth',
        headerToolbar: {
            left: 'prev,next today',
            center: 'title',
            right: 'dayGridMonth,timeGridWeek,timeGridDay'
        },
        locale: 'pl',
        buttonText: {
            today: 'Dzisiaj',
            month: 'Miesiąc',
            week: 'Tydzień',
            day: 'Dzień',
            list: 'Lista'
        },
        buttonHints: {
            today: 'Przejdź do dzisiejszej daty',
            prev: 'Poprzedni miesiąc',
            next: 'Następny miesiąc'
        },
        allDayText: 'Dzień',

        events: apiUrl, // ← tutaj dynamiczny URL z parametru

        eventClassNames: function (info) {
            if (info.event.extendedProps.isDone) {
                return ['event-done'];
            } else {
                return ['event-pending'];
            }
        },

        eventDidMount: function (info) {
            var desc = info.event.extendedProps?.description || '';
            var timeText = info.timeText ? `<div><small>${info.timeText}</small></div>` : '';
            var content = `
                <div><strong>${info.event.title}</strong></div>
                ${timeText}
                ${desc ? `<div>${desc}</div>` : ''}
            `.trim();

            info.el.setAttribute('data-bs-toggle', 'tooltip');
            info.el.setAttribute('data-bs-html', 'true');
            info.el.setAttribute('data-bs-title', content);

            var tip = new bootstrap.Tooltip(info.el, {
                container: 'body',
                trigger: 'hover focus'
            });

            info.el._fcTooltip = tip;
        },
        eventWillUnmount: function (info) {
            if (info.el._fcTooltip) {
                info.el._fcTooltip.dispose();
                delete info.el._fcTooltip;
            }
        }
    });

    calendar.render();
}