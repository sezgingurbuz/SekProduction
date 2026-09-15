(function () {
    var modalEl = document.getElementById('tourModal');
    if (!modalEl) {
        return;
    }

    var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    var form = modalEl.querySelector('[data-tour-form]');
    var title = modalEl.querySelector('.modal-title');
    var deleteButton = modalEl.querySelector('[data-tour-delete]');
    var deleteForm = document.getElementById('tourDeleteForm');

    function field(name) {
        return form.querySelector('[data-field="' + name + '"]');
    }

    function fill(values) {
        ['id', 'productionId', 'date', 'time', 'city', 'venue', 'ticketUrl'].forEach(function (name) {
            var input = field(name);
            var value = values[name];
            if (value === undefined || value === null || value === '') {
                value = input.dataset.default || '';
            }
            input.value = value;
        });
    }

    function today() {
        var d = new Date();
        return d.getFullYear() + '-' + String(d.getMonth() + 1).padStart(2, '0') + '-' + String(d.getDate()).padStart(2, '0');
    }

    // Yeni seans: gün hücresindeki + ile açılırsa o günün tarihi gelir.
    document.querySelectorAll('[data-tour-add]').forEach(function (button) {
        button.addEventListener('click', function (e) {
            e.stopPropagation();
            fill({ date: button.dataset.date || today() });
            title.textContent = 'Seans Ekle';
            deleteButton.hidden = true;
            modal.show();
        });
    });

    // Var olan seans: takvimdeki kutucuk ya da listedeki Düzenle düğmesi.
    document.querySelectorAll('[data-tour-edit]').forEach(function (button) {
        button.addEventListener('click', function () {
            fill(button.dataset);
            deleteForm.querySelector('[data-field="id"]').value = button.dataset.id;
            title.textContent = 'Seansı Düzenle';
            deleteButton.hidden = false;
            modal.show();
        });
    });

    modalEl.addEventListener('shown.bs.modal', function () {
        field('productionId').focus();
    });

    deleteForm.addEventListener('submit', function (e) {
        if (!confirm(deleteForm.dataset.confirm)) {
            e.preventDefault();
        }
    });

    // Filtreler seçildiği anda uygulanır.
    var filterForm = document.querySelector('[data-tour-filter]');
    if (filterForm) {
        filterForm.querySelectorAll('select').forEach(function (select) {
            select.addEventListener('change', function () { filterForm.submit(); });
        });
    }

    // Gün seçimi: boş alana tıklamak günü seçer/bırakır, Shift ile aralık seçilir.
    // Seçili günler alttaki çubuktan tek seferde eklenir.
    var calendar = document.querySelector('[data-tour-calendar]');
    var selectionForm = document.querySelector('[data-tour-selection]');
    if (calendar && selectionForm) {
        var dayCells = Array.prototype.slice.call(calendar.querySelectorAll('[data-drop-date]'));
        var labels = {};
        dayCells.forEach(function (cell) { labels[cell.dataset.dropDate] = cell.dataset.label; });
        var selected = {};
        var lastIndex = null;
        var count = selectionForm.querySelector('[data-selection-count]');
        var list = selectionForm.querySelector('[data-selection-list]');
        var inputs = selectionForm.querySelector('[data-selection-inputs]');
        var bulkDeleteButton = selectionForm.querySelector('[data-selection-delete]');
        var bulkDeleteCount = selectionForm.querySelector('[data-selection-session-count]');
        var bulkDeleteForm = document.getElementById('tourBulkDeleteForm');
        var selectedSessionIds = [];

        var render = function () {
            var dates = Object.keys(selected).sort();
            selectedSessionIds = [];
            dayCells.forEach(function (cell) {
                var isSelected = !!selected[cell.dataset.dropDate];
                cell.classList.toggle('is-selected', isSelected);
                if (isSelected) {
                    cell.querySelectorAll('[data-tour-edit]').forEach(function (chip) {
                        selectedSessionIds.push(chip.dataset.id);
                    });
                }
            });
            bulkDeleteButton.hidden = selectedSessionIds.length === 0;
            bulkDeleteCount.textContent = selectedSessionIds.length;
            selectionForm.hidden = dates.length === 0;
            document.body.classList.toggle('has-tour-selection', dates.length > 0);
            count.textContent = dates.length;
            list.textContent = dates.map(function (d) { return labels[d]; }).join(', ');
            inputs.innerHTML = '';
            dates.forEach(function (d) {
                var input = document.createElement('input');
                input.type = 'hidden';
                input.name = 'dates';
                input.value = d;
                inputs.appendChild(input);
            });
        };

        var clearSelection = function () {
            selected = {};
            lastIndex = null;
            render();
        };

        dayCells.forEach(function (cell, index) {
            cell.addEventListener('click', function (e) {
                if (e.target.closest('button, a')) {
                    return;
                }
                var date = cell.dataset.dropDate;
                if (e.shiftKey && lastIndex !== null) {
                    var from = Math.min(lastIndex, index);
                    var to = Math.max(lastIndex, index);
                    for (var i = from; i <= to; i++) {
                        selected[dayCells[i].dataset.dropDate] = true;
                    }
                } else if (selected[date]) {
                    delete selected[date];
                } else {
                    selected[date] = true;
                }
                lastIndex = index;
                render();
            });
        });

        selectionForm.querySelector('[data-selection-clear]').addEventListener('click', clearSelection);

        bulkDeleteButton.addEventListener('click', function () {
            var dayCount = Object.keys(selected).length;
            if (!selectedSessionIds.length || !confirm('Seçili ' + dayCount + ' gündeki ' + selectedSessionIds.length + ' seans silinecek. Bu işlem geri alınamaz. Devam edilsin mi?')) {
                return;
            }
            var container = bulkDeleteForm.querySelector('[data-bulk-delete-inputs]');
            container.innerHTML = '';
            selectedSessionIds.forEach(function (id) {
                var input = document.createElement('input');
                input.type = 'hidden';
                input.name = 'ids';
                input.value = id;
                container.appendChild(input);
            });
            bulkDeleteForm.submit();
        });
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && !modalEl.classList.contains('show')) {
                clearSelection();
            }
        });
    }

    // Sürükle-bırak: seansı başka bir güne taşır, saati korunur.
    var token = document.querySelector('#tourMoveForm input[name="__RequestVerificationToken"]');
    var moveUrl = document.getElementById('tourMoveForm').getAttribute('action');
    var dragged = null;

    document.querySelectorAll('.tour-chip[draggable="true"]').forEach(function (chip) {
        chip.addEventListener('dragstart', function (e) {
            dragged = chip;
            chip.classList.add('is-dragging');
            e.dataTransfer.effectAllowed = 'move';
            e.dataTransfer.setData('text/plain', chip.dataset.id);
        });
        chip.addEventListener('dragend', function () {
            chip.classList.remove('is-dragging');
            dragged = null;
        });
    });

    document.querySelectorAll('[data-drop-date]').forEach(function (day) {
        day.addEventListener('dragover', function (e) {
            if (!dragged) {
                return;
            }
            e.preventDefault();
            day.classList.add('is-drop-target');
        });
        day.addEventListener('dragleave', function (e) {
            if (!day.contains(e.relatedTarget)) {
                day.classList.remove('is-drop-target');
            }
        });
        day.addEventListener('drop', function (e) {
            e.preventDefault();
            day.classList.remove('is-drop-target');
            if (!dragged || dragged.dataset.date === day.dataset.dropDate) {
                return;
            }

            var body = new FormData();
            body.append('id', dragged.dataset.id);
            body.append('date', day.dataset.dropDate);
            body.append('__RequestVerificationToken', token.value);

            day.appendChild(dragged);
            fetch(moveUrl, { method: 'POST', body: body, credentials: 'same-origin' })
                .then(function (response) {
                    if (!response.ok) {
                        throw new Error(response.status);
                    }
                    location.reload();
                })
                .catch(function () {
                    alert('Seans taşınamadı, sayfa yenileniyor.');
                    location.reload();
                });
        });
    });
})();
