// jQuery doğrulaması sayıları nokta ondalıklı bekler; tr-TR'de kullanıcı virgülle yazar.
// Bu dosya number ve range kurallarını her iki yazımı kabul edecek şekilde değiştirir.
(function ($) {
    if (!$ || !$.validator) {
        return;
    }

    function toNumber(value) {
        if (typeof value !== "string") {
            return Number(value);
        }

        var text = value.trim();
        if (text.indexOf(",") > -1) {
            // 1.850,00 -> 1850.00
            text = text.replace(/\./g, "").replace(",", ".");
        }

        return Number(text);
    }

    $.validator.methods.number = function (value, element) {
        return this.optional(element) || !isNaN(toNumber(value));
    };

    $.validator.methods.range = function (value, element, param) {
        var number = toNumber(value);
        return this.optional(element) || (!isNaN(number) && number >= param[0] && number <= param[1]);
    };
})(window.jQuery);
