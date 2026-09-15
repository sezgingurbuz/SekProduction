// Turne Takvimi / Etkinlik Günleri: seans olan illeri Türkiye haritasında boyar,
// ilin üzerine gelince o ildeki seansları küçük bir kartta gösterir.
(function () {
    var root = document.querySelector('[data-tour-map]');
    if (!root) {
        return;
    }

    var canvas = root.querySelector('[data-map-canvas]');
    var summary = root.querySelector('[data-map-summary]');
    var unmatchedBox = root.querySelector('[data-map-unmatched]');
    var bodies = root.querySelectorAll('[data-map-body]');
    var toggle = root.querySelector('[data-map-toggle]');
    var scopeButtons = root.querySelectorAll('[data-map-scope]');
    var data = JSON.parse(root.querySelector('[data-map-data]').textContent);
    var scope = 'month';
    var byPlate = {};
    var pinned = null;
    var tooltip = document.createElement('div');
    tooltip.className = 'tour-map-tooltip';
    tooltip.hidden = true;

    // Harita gizle/göster tercihi tarayıcıda hatırlanır.
    var hiddenKey = 'tourMapHidden';
    var setHidden = function (hidden) {
        bodies.forEach(function (el) { el.hidden = hidden; });
        toggle.textContent = hidden ? 'Haritayı göster' : 'Haritayı gizle';
        try { localStorage.setItem(hiddenKey, hidden ? '1' : '0'); } catch (e) { /* yok say */ }
    };
    toggle.addEventListener('click', function () { setHidden(!bodies[0].hidden); });
    try { if (localStorage.getItem(hiddenKey) === '1') { setHidden(true); } } catch (e) { /* yok say */ }

    // "İzmir", "izmir", "IZMIR", "Izmir" aynı ile eşleşsin diye Türkçe harfler sadeleştirilir.
    var normalize = function (text) {
        return (text || '').toLocaleLowerCase('tr-TR')
            .replace(/ı/g, 'i').replace(/ş/g, 's').replace(/ğ/g, 'g').replace(/ü/g, 'u')
            .replace(/ö/g, 'o').replace(/ç/g, 'c').replace(/[âà]/g, 'a').replace(/[îì]/g, 'i').replace(/[ûù]/g, 'u')
            .replace(/[^a-z0-9]/g, '');
    };

    // Sık kullanılan eski/kısa adlar ve KKTC şehirleri
    var aliases = {
        icel: '33', afyon: '03', maras: '46', urfa: '63', antep: '27', izmit: '41', adapazari: '54',
        antakya: '31', kibris: '00', kktc: '00', lefkosa: '00', girne: '00', gazimagusa: '00', magusa: '00',
        guzelyurt: '00', iskele: '00'
    };
    var plateByName = {};

    var findPlate = function (city) {
        var whole = normalize(city);
        if (plateByName[whole] || aliases[whole]) {
            return plateByName[whole] || aliases[whole];
        }
        // "Kadıköy / İstanbul", "İstanbul - Kadıköy", "Lefkoşa (KKTC)" gibi yazımlar
        var parts = (city || '').split(/[\/,\-–·()|]+/);
        for (var i = 0; i < parts.length; i++) {
            var key = normalize(parts[i]);
            if (plateByName[key] || aliases[key]) {
                return plateByName[key] || aliases[key];
            }
        }
        return null;
    };

    var escapeHtml = function (text) {
        var div = document.createElement('div');
        div.textContent = text == null ? '' : String(text);
        return div.innerHTML;
    };

    var provinces = [];
    var provinceName = function (group) {
        return group.dataset.iladi.replace(/\s*\(.*\)\s*/, '');
    };

    var render = function () {
        var sessions = data[scope] || [];
        byPlate = {};
        var unmatched = {};

        sessions.forEach(function (s) {
            var plate = findPlate(s.city);
            if (plate) {
                (byPlate[plate] = byPlate[plate] || []).push(s);
            } else {
                unmatched[s.city] = (unmatched[s.city] || 0) + 1;
            }
        });

        provinces.forEach(function (group) {
            var list = byPlate[group.dataset.plakakodu];
            var count = list ? list.length : 0;
            var level = count === 0 ? 0 : count === 1 ? 1 : count <= 3 ? 2 : 3;
            group.setAttribute('data-level', level);
            if (count) {
                group.setAttribute('tabindex', '0');
                group.setAttribute('aria-label', provinceName(group) + ': ' + count + ' seans');
            } else {
                group.removeAttribute('tabindex');
                group.removeAttribute('aria-label');
            }
        });

        var cityCount = Object.keys(byPlate).length;
        var matchedCount = sessions.length - Object.keys(unmatched).reduce(function (sum, k) { return sum + unmatched[k]; }, 0);
        summary.textContent = sessions.length ? '· ' + cityCount + ' ilde ' + matchedCount + ' seans' : '· seans yok';

        var names = Object.keys(unmatched);
        unmatchedBox.hidden = names.length === 0;
        unmatchedBox.innerHTML = names.length
            ? '<strong>Haritada bulunamayan şehirler:</strong> '
                + names.map(function (n) { return escapeHtml(n) + ' (' + unmatched[n] + ')'; }).join(', ')
                + '. Şehri il adıyla yazarsanız (örn. "Kadıköy / İstanbul") haritada görünür.'
            : '';

        hideTooltip(true);
    };

    var tooltipHtml = function (group) {
        var list = byPlate[group.dataset.plakakodu] || [];
        var name = provinceName(group);
        if (!list.length) {
            return '<div class="tour-map-tooltip__title">' + escapeHtml(name) + '</div><div class="text-muted small">Seans yok</div>';
        }
        var max = 6;
        var rows = list.slice(0, max).map(function (s) {
            return '<li>'
                + '<span class="tour-dot" style="--c:' + escapeHtml(s.color) + '"></span>'
                + '<strong>' + escapeHtml(s.day) + ' ' + escapeHtml(s.time) + '</strong> '
                + escapeHtml(s.production)
                + (s.venue ? '<div class="text-muted">' + escapeHtml(s.venue) + '</div>' : '')
                + (s.hasTicket ? '' : '<div class="tour-chip__warn">bilet linki yok</div>')
                + '</li>';
        }).join('');
        return '<div class="tour-map-tooltip__title">' + escapeHtml(name)
            + ' <span class="badge rounded-pill bg-light text-dark border">' + list.length + ' seans</span></div>'
            + '<ul>' + rows + '</ul>'
            + (list.length > max ? '<div class="text-muted small">+' + (list.length - max) + ' seans daha</div>' : '');
    };

    var placeTooltip = function (clientX, clientY) {
        var box = canvas.getBoundingClientRect();
        var x = clientX - box.left + 14;
        var y = clientY - box.top + 14;
        var w = tooltip.offsetWidth;
        var h = tooltip.offsetHeight;
        if (x + w > box.width) { x = Math.max(4, clientX - box.left - w - 14); }
        if (y + h > box.height) { y = Math.max(4, clientY - box.top - h - 14); }
        tooltip.style.left = x + 'px';
        tooltip.style.top = y + 'px';
    };

    var showTooltip = function (group, clientX, clientY) {
        tooltip.innerHTML = tooltipHtml(group);
        tooltip.hidden = false;
        tooltip.classList.toggle('is-pinned', pinned === group);
        provinces.forEach(function (g) { g.classList.toggle('is-active', g.dataset.plakakodu === group.dataset.plakakodu); });
        placeTooltip(clientX, clientY);
    };

    var hideTooltip = function (force) {
        if (pinned && !force) {
            return;
        }
        pinned = null;
        tooltip.hidden = true;
        provinces.forEach(function (g) { g.classList.remove('is-active'); });
    };

    var centerOf = function (group) {
        var rect = group.getBoundingClientRect();
        return { x: rect.left + rect.width / 2, y: rect.top + rect.height / 2 };
    };

    scopeButtons.forEach(function (button) {
        button.addEventListener('click', function () {
            scope = button.dataset.mapScope;
            scopeButtons.forEach(function (b) {
                b.classList.toggle('btn-secondary', b === button);
                b.classList.toggle('btn-outline-secondary', b !== button);
            });
            render();
        });
    });

    fetch(root.dataset.src)
        .then(function (response) {
            if (!response.ok) {
                throw new Error(response.status);
            }
            return response.text();
        })
        .then(function (svgText) {
            canvas.innerHTML = svgText;
            canvas.appendChild(tooltip);
            var svg = canvas.querySelector('svg');
            svg.removeAttribute('id');
            svg.setAttribute('role', 'img');
            svg.setAttribute('aria-label', 'Türkiye il haritası');
            provinces = Array.prototype.slice.call(svg.querySelectorAll('g[data-plakakodu]'));
            provinces.forEach(function (group) {
                plateByName[normalize(provinceName(group))] = group.dataset.plakakodu;

                group.addEventListener('mouseenter', function (e) {
                    if (!pinned) { showTooltip(group, e.clientX, e.clientY); }
                });
                group.addEventListener('mousemove', function (e) {
                    if (!pinned) { placeTooltip(e.clientX, e.clientY); }
                });
                group.addEventListener('mouseleave', function () { hideTooltip(false); });
                group.addEventListener('click', function (e) {
                    e.stopPropagation();
                    if (pinned === group) {
                        hideTooltip(true);
                        return;
                    }
                    pinned = group;
                    showTooltip(group, e.clientX, e.clientY);
                });
                group.addEventListener('focus', function () {
                    var c = centerOf(group);
                    showTooltip(group, c.x, c.y);
                });
                group.addEventListener('blur', function () { hideTooltip(false); });
            });
            document.addEventListener('click', function (e) {
                if (pinned && !tooltip.contains(e.target)) {
                    hideTooltip(true);
                }
            });
            render();
        })
        .catch(function () {
            canvas.innerHTML = '<div class="text-muted small p-4 text-center">Harita yüklenemedi.</div>';
        });
})();
