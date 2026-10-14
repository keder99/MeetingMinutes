/* جست‌وجوی سریع مسئول در فهرست افراد (شامل نام‌های مستعار).
   mmInitPeople(searchBoxId, selectId, people)  ←  people = [{v:"12", t:"دکتر خیری", s:"خیری قبایی ..."}] */
(function (root) {
    'use strict';

    function fix(s) {
        return String(s || '').replace(/ي/g, 'ی').replace(/ك/g, 'ک')
                              .replace(/[\u200c\u200d\u0640\u064b-\u065f]/g, '').replace(/\s+/g, '').toLowerCase();
    }

    function filterPeople(query, people) {
        var q = fix(query), out = [], i;
        for (i = 0; i < people.length; i++) {
            if (!q || fix(people[i].s).indexOf(q) >= 0 || fix(people[i].t).indexOf(q) >= 0) { out.push(people[i]); }
        }
        return out;
    }

    var api = { fix: fix, filterPeople: filterPeople };
    if (typeof module !== 'undefined' && module.exports) { module.exports = api; }
    if (typeof document === 'undefined') { return; }

    root.mmInitPeople = function (searchId, selectId, people) {
        var box = document.getElementById(searchId), sel = document.getElementById(selectId);
        if (!box || !sel || box.getAttribute('data-bound')) { return; }
        box.setAttribute('data-bound', '1');
        var emptyText = sel.options.length ? sel.options[0].text : '— بدون فرد —';

        function rebuild() {
            var keep = sel.value, list = filterPeople(box.value, people), i, found = false;
            sel.options.length = 0;
            sel.options.add(new Option(emptyText, ''));
            for (i = 0; i < list.length; i++) { sel.options.add(new Option(list[i].t, list[i].v)); }
            for (i = 0; i < sel.options.length; i++) {
                if (sel.options[i].value === keep) { sel.selectedIndex = i; found = true; break; }
            }
            // اگر فقط یک نفر مطابقت دارد و چیزی انتخاب نشده، همان انتخاب شود
            if (!found && box.value && list.length === 1) { sel.selectedIndex = 1; }
        }

        box.addEventListener('input', rebuild);
        box.addEventListener('keydown', function (e) { if (e.key === 'Enter') { e.preventDefault(); } });
    };
})(typeof window !== 'undefined' ? window : this);
