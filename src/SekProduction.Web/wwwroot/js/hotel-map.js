// Oteller sayfası: Türkiye haritasından ya da arama kutusundan şehir seçilince
// o şehrin otel kartları gösterilir. Filtreleme tarayıcıda yapılır, sayfa yenilenmez.
(function () {
    var root = document.querySelector('[data-hotel-map]');
    if (!root) {
        return;
    }

    var canvas = root.querySelector('[data-map-canvas]');
    var search = root.querySelector('[data-city-search]');
    var picker = root.querySelector('[data-city-picker]');
    var resetButton = root.querySelector('[data-city-reset]');
    var heading = root.querySelector('[data-city-heading]');
    var emptyBox = root.querySelector('[data-city-empty]');
    var groups = Array.prototype.slice.call(document.querySelectorAll('[data-hotel-city]'));
    var cityItems = Array.prototype.slice.call(picker.querySelectorAll('[data-city-item]'));

    var provinces = [];
    var selectedCity = root.dataset.selectedCity || '';

    // "İzmir", "izmir", "IZMIR" aynı ile eşleşsin diye Türkçe harfler sadeleştirilir.
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
        // "Kadıköy / İstanbul", "Lefkoşa (KKTC)" gibi yazımlar
        var parts = (city || '').split(/[\/,\-–·()|]+/);
        for (var i = 0; i < parts.length; i++) {
            var key = normalize(parts[i]);
            if (plateByName[key] || aliases[key]) {
                return plateByName[key] || aliases[key];
            }
        }
        return null;
    };

    var provinceName = function (group) {
        return group.dataset.iladi.replace(/\s*\(.*\)\s*/, '');
    };

    // Seçim bir şehir adı (otel kaydındaki yazım) ya da bir il plaka kodu olabilir.
    var matches = function (group) {
        if (!selectedCity) {
            return true;
        }

        var city = group.dataset.hotelCity;
        if (normalize(city) === normalize(selectedCity)) {
            return true;
        }

        var selectedPlate = findPlate(selectedCity);
        return selectedPlate !== null && findPlate(city) === selectedPlate;
    };

    var apply = function () {
        var shown = 0;
        groups.forEach(function (group) {
            var visible = matches(group);
            group.hidden = !visible;
            if (visible) {
                shown++;
            }
        });

        cityItems.forEach(function (item) {
            item.classList.toggle('is-selected', !!selectedCity && normalize(item.dataset.cityItem) === normalize(selectedCity));
        });

        var selectedPlate = selectedCity ? findPlate(selectedCity) : null;
        provinces.forEach(function (group) {
            group.classList.toggle('is-selected', selectedPlate !== null && group.dataset.plakakodu === selectedPlate);
        });

        heading.textContent = selectedCity ? selectedCity : 'Bütün şehirler';
        resetButton.hidden = !selectedCity;
        emptyBox.hidden = shown > 0;

        // Adres çubuğu paylaşılabilir kalsın; sayfa yenilenmez.
        var url = selectedCity ? '?city=' + encodeURIComponent(selectedCity) : location.pathname;
        history.replaceState(null, '', url);
    };

    var select = function (city) {
        selectedCity = city || '';
        apply();
    };

    cityItems.forEach(function (item) {
        item.addEventListener('click', function () {
            select(item.dataset.cityItem === selectedCity ? '' : item.dataset.cityItem);
        });
    });

    resetButton.addEventListener('click', function () { select(''); });

    search.addEventListener('input', function () {
        var term = normalize(search.value);
        cityItems.forEach(function (item) {
            item.hidden = term.length > 0 && normalize(item.dataset.cityItem).indexOf(term) === -1;
        });
    });

    // Enter: aramada görünen ilk şehri seçer.
    search.addEventListener('keydown', function (event) {
        if (event.key !== 'Enter') {
            return;
        }
        event.preventDefault();
        var first = cityItems.filter(function (item) { return !item.hidden; })[0];
        if (first) {
            select(first.dataset.cityItem);
        }
    });

    var hotelCountByPlate = function () {
        var counts = {};
        groups.forEach(function (group) {
            var plate = findPlate(group.dataset.hotelCity);
            if (plate) {
                counts[plate] = (counts[plate] || 0) + Number(group.dataset.hotelCount || 0);
            }
        });
        return counts;
    };

    fetch(root.dataset.src)
        .then(function (response) {
            if (!response.ok) {
                throw new Error(response.status);
            }
            return response.text();
        })
        .then(function (svgText) {
            canvas.innerHTML = svgText;
            var svg = canvas.querySelector('svg');
            svg.removeAttribute('id');
            svg.setAttribute('role', 'img');
            svg.setAttribute('aria-label', 'Türkiye il haritası');

            provinces = Array.prototype.slice.call(svg.querySelectorAll('g[data-plakakodu]'));
            provinces.forEach(function (group) {
                plateByName[normalize(provinceName(group))] = group.dataset.plakakodu;
            });

            var counts = hotelCountByPlate();
            provinces.forEach(function (group) {
                var count = counts[group.dataset.plakakodu] || 0;
                var level = count === 0 ? 0 : count === 1 ? 1 : count <= 3 ? 2 : 3;
                group.setAttribute('data-level', level);

                if (count > 0) {
                    group.setAttribute('tabindex', '0');
                    group.setAttribute('aria-label', provinceName(group) + ': ' + count + ' otel');
                    var choose = function () { select(provinceName(group)); };
                    group.addEventListener('click', choose);
                    group.addEventListener('keydown', function (event) {
                        if (event.key === 'Enter' || event.key === ' ') {
                            event.preventDefault();
                            choose();
                        }
                    });
                }
            });

            apply();
        })
        .catch(function () {
            canvas.innerHTML = '<div class="text-muted small p-4 text-center">Harita yüklenemedi; şehri listeden veya aramadan seçebilirsiniz.</div>';
            apply();
        });
})();
