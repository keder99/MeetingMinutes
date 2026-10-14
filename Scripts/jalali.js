/* تقویم شمسی سبک برای فیلدهای تاریخ.
   استفاده: روی TextBox صفت data-jpicker="1" و readonly بگذارید؛ با کلیک، تقویم باز می‌شود
   و مقدار به‌صورت yyyy/mm/dd با ارقام فارسی در فیلد قرار می‌گیرد (سمت سرور با PersianDate.TryParse خوانده می‌شود). */
(function (root) {
    'use strict';

    var MONTHS = ['', 'فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور', 'مهر', 'آبان', 'آذر', 'دی', 'بهمن', 'اسفند'];
    var WEEK = ['ش', 'ی', 'د', 'س', 'چ', 'پ', 'ج'];
    var FA = '۰۱۲۳۴۵۶۷۸۹';
    var AR = '٠١٢٣٤٥٦٧٨٩';

    /* ---- تبدیل تاریخ (الگوریتم jalaali-js) ---- */
    function jalaaliToGregorian(jy, jm, jd) {
        var gy = (jy <= 979) ? 621 : 1600;
        jy -= (jy <= 979) ? 0 : 979;
        var days = (365 * jy) + (~~(jy / 33) * 8) + ~~(((jy % 33) + 3) / 4) + 78 + jd + ((jm < 7) ? (jm - 1) * 31 : ((jm - 7) * 30) + 186);
        gy += 400 * ~~(days / 146097); days %= 146097;
        if (days > 36524) { gy += 100 * ~~(--days / 36524); days %= 36524; if (days >= 365) days++; }
        gy += 4 * ~~(days / 1461); days %= 1461;
        if (days > 365) { gy += ~~((days - 1) / 365); days = (days - 1) % 365; }
        var gd = days + 1;
        var sal = [0, 31, ((gy % 4 === 0 && gy % 100 !== 0) || (gy % 400 === 0)) ? 29 : 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];
        var gm;
        for (gm = 1; gm <= 12; gm++) { if (gd <= sal[gm]) break; gd -= sal[gm]; }
        return { gy: gy, gm: gm, gd: gd };
    }

    function gregorianToJalali(gy, gm, gd) {
        var gdm = [0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334];
        var jy = (gy <= 1600) ? 0 : 979;
        gy -= (gy <= 1600) ? 621 : 1600;
        var gy2 = (gm > 2) ? (gy + 1) : gy;
        var days = (365 * gy) + (~~((gy2 + 3) / 4)) - (~~((gy2 + 99) / 100)) + (~~((gy2 + 399) / 400)) - 80 + gd + gdm[gm - 1];
        jy += 33 * ~~(days / 12053); days %= 12053;
        jy += 4 * ~~(days / 1461); days %= 1461;
        if (days > 365) { jy += ~~((days - 1) / 365); days = (days - 1) % 365; }
        var jm = (days < 186) ? 1 + ~~(days / 31) : 7 + ~~((days - 186) / 30);
        var jd = 1 + ((days < 186) ? (days % 31) : ((days - 186) % 30));
        return { jy: jy, jm: jm, jd: jd };
    }

    function daysInMonth(jy, jm) {
        var g1 = jalaaliToGregorian(jy, jm, 1);
        var ny = jy, nm = jm + 1;
        if (nm > 12) { nm = 1; ny = jy + 1; }
        var g2 = jalaaliToGregorian(ny, nm, 1);
        var d1 = Date.UTC(g1.gy, g1.gm - 1, g1.gd), d2 = Date.UTC(g2.gy, g2.gm - 1, g2.gd);
        return Math.round((d2 - d1) / 86400000);
    }

    /* ---- کمکی‌ها ---- */
    function toEn(s) {
        return String(s || '').replace(/[۰-۹]/g, function (d) { return FA.indexOf(d); })
                              .replace(/[٠-٩]/g, function (d) { return AR.indexOf(d); });
    }
    function toFa(s) { return String(s).replace(/\d/g, function (d) { return FA.charAt(+d); }); }
    function pad(n) { return (n < 10 ? '0' : '') + n; }
    function fmt(jy, jm, jd) { return toFa(jy + '/' + pad(jm) + '/' + pad(jd)); }
    function parse(str) {
        var m = /^(\d{4})[\/\-](\d{1,2})[\/\-](\d{1,2})$/.exec(toEn(str).trim());
        return m ? { jy: +m[1], jm: +m[2], jd: +m[3] } : null;
    }
    function today() {
        var d = new Date();
        return gregorianToJalali(d.getFullYear(), d.getMonth() + 1, d.getDate());
    }
    /* شنبه = 0 ... جمعه = 6 */
    function weekdayOfFirst(jy, jm) {
        var g = jalaaliToGregorian(jy, jm, 1);
        return (new Date(g.gy, g.gm - 1, g.gd).getDay() + 1) % 7;
    }

    var api = {
        jalaaliToGregorian: jalaaliToGregorian, gregorianToJalali: gregorianToJalali,
        daysInMonth: daysInMonth, parse: parse, fmt: fmt, toEn: toEn, toFa: toFa, weekdayOfFirst: weekdayOfFirst
    };
    if (typeof module !== 'undefined' && module.exports) { module.exports = api; }
    if (typeof document === 'undefined') { return; }

    /* ---- رابط کاربری تقویم ---- */
    var panel = null, state = { input: null, jy: 0, jm: 0 };

    function injectStyle() {
        if (document.getElementById('jalali-cal-style')) { return; }
        var st = document.createElement('style');
        st.id = 'jalali-cal-style';
        st.appendChild(document.createTextNode('.jalali-cal-head .nav-grp{display:flex;gap:4px}'));
        document.head.appendChild(st);
    }

    function ensurePanel() {
        if (!panel) {
            injectStyle();
            panel = document.createElement('div');
            panel.className = 'jalali-cal';
            document.body.appendChild(panel);
            panel.addEventListener('click', onPanelClick);
        }
        return panel;
    }

    function render() {
        var jy = state.jy, jm = state.jm;
        var offset = weekdayOfFirst(jy, jm), n = daysInMonth(jy, jm), t = today();
        var sel = parse(state.input.value), i, html;

        html = '<div class="jalali-cal-head">' +
               '<span class="nav-grp">' +
               '<button type="button" data-act="prevY" aria-label="سال قبل" title="سال قبل"><i class="ti ti-chevrons-right"></i></button>' +
               '<button type="button" data-act="prev" aria-label="ماه قبل" title="ماه قبل"><i class="ti ti-chevron-right"></i></button></span>' +
               '<b>' + MONTHS[jm] + ' ' + toFa(jy) + '</b>' +
               '<span class="nav-grp">' +
               '<button type="button" data-act="next" aria-label="ماه بعد" title="ماه بعد"><i class="ti ti-chevron-left"></i></button>' +
               '<button type="button" data-act="nextY" aria-label="سال بعد" title="سال بعد"><i class="ti ti-chevrons-left"></i></button></span></div>';
        html += '<div class="jalali-cal-week">';
        for (i = 0; i < 7; i++) { html += '<span>' + WEEK[i] + '</span>'; }
        html += '</div><div class="jalali-cal-days">';
        for (i = 0; i < offset; i++) { html += '<span></span>'; }
        for (i = 1; i <= n; i++) {
            var cls = '';
            if (t.jy === jy && t.jm === jm && t.jd === i) { cls += ' today'; }
            if (sel && sel.jy === jy && sel.jm === jm && sel.jd === i) { cls += ' selected'; }
            html += '<button type="button" data-d="' + i + '" class="' + cls.trim() + '">' + toFa(i) + '</button>';
        }
        html += '</div><div class="jalali-cal-foot">' +
                '<button type="button" data-act="today">امروز</button>' +
                '<button type="button" data-act="clear">پاک کردن</button></div>';
        panel.innerHTML = html;
    }

    function place(input) {
        var r = input.getBoundingClientRect(), w = 264;
        var left = Math.max(8, Math.min(r.left, window.innerWidth - w - 8));
        panel.style.top = (r.bottom + 6) + 'px';
        panel.style.left = left + 'px';
        panel.style.right = 'auto';
    }

    function open(input) {
        ensurePanel();
        var cur = parse(input.value) || today();
        state.input = input; state.jy = cur.jy; state.jm = cur.jm;
        render(); place(input);
        panel.className = 'jalali-cal open';
    }

    function close() { if (panel) { panel.className = 'jalali-cal'; } }

    function setValue(text) {
        state.input.value = text;
        var ev;
        if (typeof Event === 'function') { ev = new Event('change', { bubbles: true }); }
        else { ev = document.createEvent('Event'); ev.initEvent('change', true, true); }
        state.input.dispatchEvent(ev);
    }

    function onPanelClick(e) {
        // رفع اشکال: render() دکمه‌ی کلیک‌شده را از DOM حذف می‌کند و اگر رویداد به document برسد،
        // «کلیک بیرون از تقویم» تشخیص داده می‌شد و تقویم بعد از هر جابه‌جایی ماه بسته می‌شد.
        e.stopPropagation();
        var el = e.target;
        while (el && el !== panel && !(el.getAttribute && (el.getAttribute('data-act') || el.getAttribute('data-d')))) { el = el.parentNode; }
        if (!el || el === panel) { return; }
        var act = el.getAttribute('data-act'), d = el.getAttribute('data-d');
        if (d) { setValue(fmt(state.jy, state.jm, +d)); close(); return; }
        if (act === 'prev') { state.jm--; if (state.jm < 1) { state.jm = 12; state.jy--; } render(); }
        else if (act === 'next') { state.jm++; if (state.jm > 12) { state.jm = 1; state.jy++; } render(); }
        else if (act === 'prevY') { state.jy--; render(); }
        else if (act === 'nextY') { state.jy++; render(); }
        else if (act === 'today') { var t = today(); setValue(fmt(t.jy, t.jm, t.jd)); close(); }
        else if (act === 'clear') { setValue(''); close(); }
    }

    document.addEventListener('click', function (e) {
        var t = e.target;
        if (t && t.getAttribute && t.getAttribute('data-jpicker') !== null) { open(t); return; }
        if (panel && !panel.contains(t)) { close(); }
    });
    document.addEventListener('keydown', function (e) { if (e.key === 'Escape') { close(); } });
    window.addEventListener('scroll', function () { close(); }, true);
    window.addEventListener('resize', close);

})(typeof window !== 'undefined' ? window : this);
