(function () {
    var modalEl = document.getElementById('tourModal');
    if (!modalEl) {
        return;
    }

    // Kaydırma konumunu koru: takvimden ay değiştirince veya kaydet/ekle/sil sonrası
    // sayfa yeniden yüklendiğinde kullanıcı en üste atılmaz, kaldığı yerde devam eder.
    var scrollKey = 'tourScroll:' + location.pathname.toLowerCase();

    var rememberScroll = function () {
        try {
            sessionStorage.setItem(scrollKey, JSON.stringify({ y: window.scrollY, t: Date.now() }));
        } catch (e) {
            // Depolama kapalıysa sayfa her zamanki gibi en üstten açılır.
        }
    };

    // Konum geri yüklenince en üstteki durum mesajı ekran dışında kalır; sağ üstte bildirim olarak da gösterilir.
    var showToasts = function () {
        var alerts = document.querySelectorAll('body > .container-fluid > .alert, body > .container > .alert');
        if (!alerts.length) {
            return;
        }
        var box = document.createElement('div');
        box.className = 'tour-toasts';
        alerts.forEach(function (alert) {
            var toast = alert.cloneNode(true);
            toast.classList.add('alert-dismissible', 'fade', 'show');
            toast.setAttribute('role', 'status');
            var close = document.createElement('button');
            close.type = 'button';
            close.className = 'btn-close';
            close.setAttribute('aria-label', 'Kapat');
            close.addEventListener('click', function () { toast.remove(); });
            toast.appendChild(close);
            box.appendChild(toast);
            if (!alert.classList.contains('alert-danger')) {
                setTimeout(function () {
                    toast.style.opacity = '0';
                    setTimeout(function () { toast.remove(); }, 400);
                }, 5000);
            }
        });
        document.body.appendChild(box);
    };

    var restoreScroll = function () {
        var saved = null;
        try {
            saved = JSON.parse(sessionStorage.getItem(scrollKey));
            sessionStorage.removeItem(scrollKey);
        } catch (e) {
            return;
        }
        // Yalnızca az önce bu sayfadan çıkılmışsa geri yükle (menüden yeniden açılışta en üstten başlar).
        if (!saved || Date.now() - saved.t > 30000 || saved.y < 50) {
            return;
        }
        // Bootstrap yumuşak kaydırma açık; açılışta aşağı kayan animasyon görünmesin.
        window.scrollTo({ top: saved.y, behavior: 'instant' });
        showToasts();
    };

    document.querySelectorAll('[data-keep-scroll]').forEach(function (link) {
        link.addEventListener('click', rememberScroll);
    });
    document.querySelectorAll('#tourModal form, #tourDeleteForm, [data-tour-selection]').forEach(function (f) {
        f.addEventListener('submit', rememberScroll);
    });
    // Harita gizleme tercihi (tour-map.js) uygulandıktan sonra konum geri yüklenir.
    document.addEventListener('DOMContentLoaded', restoreScroll);

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
    // Seçili günler alttaki çubuktan tek seferde eklenir veya o günlerdeki seanslar silinir.
    // Ay değiştirmek sayfayı yenilediği için seçim sekme oturumunda saklanır; böylece
    // birden fazla ayda gün seçilebilir. Ekleme/silme sonrası ya da "Seçimi temizle" ile sıfırlanır.
    var calendar = document.querySelector('[data-tour-calendar]');
    var selectionForm = document.querySelector('[data-tour-selection]');
    if (calendar && selectionForm) {
        var dayCells = Array.prototype.slice.call(calendar.querySelectorAll('[data-drop-date]'));

        // Seçim sayfaya (Turne Takvimi + filtreler ya da yapımın Etkinlik Günleri) özeldir; ay ve görünüm hariç.
        var storageKey = (function () {
            var params = new URLSearchParams(location.search);
            ['year', 'month', 'view'].forEach(function (name) { params.delete(name); });
            params.sort();
            return 'tourSelection:' + location.pathname.toLowerCase() + '?' + params.toString();
        })();

        var loadSelection = function () {
            try {
                return JSON.parse(sessionStorage.getItem(storageKey)) || {};
            } catch (e) {
                return {};
            }
        };

        var saveSelection = function () {
            try {
                if (Object.keys(selected).length) {
                    sessionStorage.setItem(storageKey, JSON.stringify(selected));
                } else {
                    sessionStorage.removeItem(storageKey);
                }
            } catch (e) {
                // Depolama kapalıysa seçim yalnızca bu sayfada kalır.
            }
        };

        var forgetSelection = function () {
            try {
                sessionStorage.removeItem(storageKey);
            } catch (e) {
                // yok say
            }
        };

        // tarih -> { label: '5 Eki', ids: ['12', '13'] }; ids o gündeki seanslardır (toplu silme için).
        var selected = loadSelection();
        var lastIndex = null;
        var count = selectionForm.querySelector('[data-selection-count]');
        var list = selectionForm.querySelector('[data-selection-list]');
        var inputs = selectionForm.querySelector('[data-selection-inputs]');
        var bulkDeleteButton = selectionForm.querySelector('[data-selection-delete]');
        var bulkDeleteCount = selectionForm.querySelector('[data-selection-session-count]');
        var bulkDeleteForm = document.getElementById('tourBulkDeleteForm');
        var selectedSessionIds = [];

        var otherMonths = selectionForm.querySelector('[data-selection-other-months]');

        var render = function () {
            // Ekrandaki günlerin seans listesi sayfadan tazelenir; diğer aylardakiler saklanandan gelir.
            var visible = {};
            dayCells.forEach(function (cell) {
                var date = cell.dataset.dropDate;
                var entry = selected[date];
                visible[date] = true;
                cell.classList.toggle('is-selected', !!entry);
                if (entry) {
                    entry.label = cell.dataset.label;
                    entry.ids = Array.prototype.map.call(cell.querySelectorAll('[data-tour-edit]'), function (chip) {
                        return chip.dataset.id;
                    });
                }
            });

            var dates = Object.keys(selected).sort();
            var offscreen = dates.filter(function (d) { return !visible[d]; }).length;
            selectedSessionIds = [];
            dates.forEach(function (d) {
                selectedSessionIds = selectedSessionIds.concat(selected[d].ids || []);
            });
            saveSelection();

            bulkDeleteButton.hidden = selectedSessionIds.length === 0;
            bulkDeleteCount.textContent = selectedSessionIds.length;
            selectionForm.hidden = dates.length === 0;
            document.body.classList.toggle('has-tour-selection', dates.length > 0);
            count.textContent = dates.length;
            otherMonths.textContent = offscreen ? '(' + offscreen + ' gün diğer aylarda)' : '';
            list.textContent = dates.map(function (d) { return selected[d].label; }).join(', ');
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
                        selected[dayCells[i].dataset.dropDate] = selected[dayCells[i].dataset.dropDate] || {};
                    }
                } else if (selected[date]) {
                    delete selected[date];
                } else {
                    selected[date] = {};
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
            forgetSelection();
            rememberScroll();
            bulkDeleteForm.submit();
        });

        // Günler eklenince seçim tamamlanmış sayılır.
        selectionForm.addEventListener('submit', forgetSelection);

        render();
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && !modalEl.classList.contains('show')) {
                clearSelection();
            }
        });
    }

    // Sürükle-bırak: seansı başka bir güne taşır, saati korunur.
    // Bırakma anında KAYDEDİLMEZ; yanlışlıkla taşımayı önlemek için değişiklik alttaki
    // çubukta bekler, kullanıcı "Değişiklikleri kaydet" deyince sunucuya gider.
    var movesForm = document.querySelector('[data-tour-moves]');
    if (movesForm) {
        var movesCount = movesForm.querySelector('[data-moves-count]');
        var movesList = movesForm.querySelector('[data-moves-list]');
        var movesInputs = movesForm.querySelector('[data-moves-inputs]');
        var movesCancel = movesForm.querySelector('[data-moves-cancel]');

        // id -> { chip, fromCell, fromDate, fromLabel, toLabel }
        var pending = {};
        var saving = false;
        var dragged = null;

        var labelOf = function (cell) {
            return cell.dataset.fullLabel || cell.dataset.label || cell.dataset.dropDate;
        };

        // Seansı hedef günde saat sırasına göre yerleştirir.
        var placeChip = function (cell, chip) {
            var siblings = cell.querySelectorAll('.tour-chip');
            for (var i = 0; i < siblings.length; i++) {
                if (siblings[i] !== chip && (siblings[i].dataset.time || '') > (chip.dataset.time || '')) {
                    cell.insertBefore(chip, siblings[i]);
                    return;
                }
            }
            cell.appendChild(chip);
        };

        var renderMoves = function () {
            var ids = Object.keys(pending);
            movesForm.hidden = ids.length === 0;
            document.body.classList.toggle('has-tour-moves', ids.length > 0);
            movesCount.textContent = ids.length;

            movesList.textContent = '';
            ids.forEach(function (id) {
                var entry = pending[id];
                var name = entry.chip.dataset.production || 'Seans';
                var city = entry.chip.dataset.city;
                var line = document.createElement('div');
                line.textContent = name + (city ? ' · ' + city : '')
                    + ': ' + entry.fromLabel + ' → ' + entry.toLabel + ' gününe taşındı';
                movesList.appendChild(line);
            });

            movesInputs.innerHTML = '';
            ids.forEach(function (id, index) {
                [['Id', id], ['Date', pending[id].toDate]].forEach(function (pair) {
                    var input = document.createElement('input');
                    input.type = 'hidden';
                    input.name = 'moves[' + index + '].' + pair[0];
                    input.value = pair[1];
                    movesInputs.appendChild(input);
                });
            });
        };

        var cancelMoves = function () {
            Object.keys(pending).forEach(function (id) {
                var entry = pending[id];
                entry.chip.dataset.date = entry.fromDate;
                placeChip(entry.fromCell, entry.chip);
            });
            pending = {};
            renderMoves();
        };

        movesCancel.addEventListener('click', cancelMoves);
        movesForm.addEventListener('submit', function () {
            saving = true;
            rememberScroll();
        });

        // Kaydedilmemiş taşıma varken sayfadan ayrılmak değişikliği düşürür; kullanıcı uyarılır.
        window.addEventListener('beforeunload', function (e) {
            if (saving || !Object.keys(pending).length) {
                return;
            }
            e.preventDefault();
            e.returnValue = '';
        });

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

                var chip = dragged;
                var id = chip.dataset.id;
                // Aynı seans yeniden sürüklendiyse ilk konumu korunur, hedef güncellenir.
                var entry = pending[id] || {
                    chip: chip,
                    fromCell: chip.closest('[data-drop-date]'),
                    fromDate: chip.dataset.date,
                    fromLabel: labelOf(chip.closest('[data-drop-date]'))
                };

                chip.dataset.date = day.dataset.dropDate;
                placeChip(day, chip);

                if (entry.fromDate === day.dataset.dropDate) {
                    // Kendi gününe geri bırakıldı; bekleyen değişiklik kalmadı.
                    delete pending[id];
                } else {
                    entry.toDate = day.dataset.dropDate;
                    entry.toLabel = labelOf(day);
                    pending[id] = entry;
                }

                renderMoves();
            });
        });

        renderMoves();
    }
})();
