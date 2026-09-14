(function () {
    var input = document.querySelector('[data-image-input]');
    var drop = document.querySelector('[data-image-drop]');
    var preview = document.querySelector('[data-image-preview]');
    var placeholder = document.querySelector('[data-image-placeholder]');
    var remove = document.querySelector('[data-image-remove]');
    var originalSrc = preview ? preview.getAttribute('src') : '';

    function showPreview(src) {
        preview.src = src;
        preview.hidden = false;
        placeholder.hidden = true;
    }

    function showPlaceholder() {
        preview.hidden = true;
        placeholder.hidden = false;
    }

    if (input && drop) {
        input.addEventListener('change', function () {
            var file = input.files && input.files[0];
            if (!file) {
                return;
            }
            if (remove) {
                remove.checked = false;
            }
            var reader = new FileReader();
            reader.onload = function (e) { showPreview(e.target.result); };
            reader.readAsDataURL(file);
        });

        ['dragenter', 'dragover'].forEach(function (name) {
            drop.addEventListener(name, function (e) {
                e.preventDefault();
                drop.classList.add('is-dragover');
            });
        });
        ['dragleave', 'drop'].forEach(function (name) {
            drop.addEventListener(name, function (e) {
                e.preventDefault();
                drop.classList.remove('is-dragover');
            });
        });
        drop.addEventListener('drop', function (e) {
            if (e.dataTransfer && e.dataTransfer.files.length) {
                input.files = e.dataTransfer.files;
                input.dispatchEvent(new Event('change'));
            }
        });
    }

    if (remove) {
        remove.addEventListener('change', function () {
            if (remove.checked) {
                input.value = '';
                showPlaceholder();
            } else if (originalSrc) {
                showPreview(originalSrc);
            }
        });
    }

    var slugSource = document.querySelector('[data-slug-source]');
    var slugTarget = document.querySelector('[data-slug-target]');
    if (slugSource && slugTarget && slugTarget.dataset.autoslug === 'true') {
        var map = { 'ç': 'c', 'ğ': 'g', 'ı': 'i', 'ö': 'o', 'ş': 's', 'ü': 'u', 'â': 'a', 'î': 'i', 'û': 'u' };
        var manuallyEdited = slugTarget.value.length > 0;

        slugTarget.addEventListener('input', function () {
            manuallyEdited = slugTarget.value.length > 0;
        });

        slugSource.addEventListener('input', function () {
            if (manuallyEdited) {
                return;
            }
            slugTarget.value = slugSource.value
                .toLocaleLowerCase('tr')
                .replace(/[çğıöşüâîû]/g, function (ch) { return map[ch]; })
                .replace(/[^a-z0-9]+/g, '-')
                .replace(/^-+|-+$/g, '');
        });
    }
})();
