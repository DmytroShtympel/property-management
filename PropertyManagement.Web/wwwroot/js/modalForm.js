// Shared convention for every "modal populated from a partial view returned by a controller
// action" in this app (Property, Unit, UnitType, Residence History, Review). One script drives
// all of them: 422 + form partial re-renders the modal in place with validation errors; a 2xx
// response closes the modal and swaps the page's list/summary container via data-modal-refresh.
(function () {
    var modalEl = document.getElementById('sharedModal');
    if (!modalEl) {
        return;
    }

    var modal = new bootstrap.Modal(modalEl);
    var contentEl = document.getElementById('sharedModalContent');
    var refreshTarget = null;

    function loadModal(url, refresh) {
        refreshTarget = refresh;
        fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
            .then(function (res) { return res.text(); })
            .then(function (html) {
                contentEl.innerHTML = html;
                modal.show();
            });
    }

    function swapRefreshTarget(html) {
        if (!refreshTarget) {
            return;
        }
        var target = document.querySelector(refreshTarget);
        if (target) {
            target.outerHTML = html;
        }
    }

    document.addEventListener('click', function (e) {
        var trigger = e.target.closest('[data-modal-trigger]');
        if (!trigger) {
            return;
        }
        e.preventDefault();
        loadModal(trigger.getAttribute('data-modal-url'), trigger.getAttribute('data-modal-refresh'));
    });

    document.addEventListener('submit', function (e) {
        var form = e.target;
        if (!contentEl.contains(form)) {
            return;
        }
        e.preventDefault();

        fetch(form.getAttribute('action'), {
            method: 'POST',
            body: new FormData(form),
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        }).then(function (res) {
            return res.text().then(function (html) {
                if (res.status === 422 || !res.ok) {
                    contentEl.innerHTML = html;
                } else {
                    modal.hide();
                    swapRefreshTarget(html);
                }
            });
        });
    });

    modalEl.addEventListener('hidden.bs.modal', function () {
        contentEl.innerHTML = '';
        refreshTarget = null;
    });

    // Generic "POST, optionally confirm, swap a target container" trigger, for actions like
    // deleting a residence history row that can't be a nested <form> (they live inside the
    // wizard's own outer form).
    document.addEventListener('click', function (e) {
        var btn = e.target.closest('[data-post-trigger]');
        if (!btn) {
            return;
        }
        e.preventDefault();

        var confirmMessage = btn.getAttribute('data-confirm');
        if (confirmMessage && !confirm(confirmMessage)) {
            return;
        }

        var token = document.querySelector('input[name="__RequestVerificationToken"]');
        var body = new URLSearchParams();
        if (token) {
            body.append('__RequestVerificationToken', token.value);
        }

        fetch(btn.getAttribute('data-post-url'), {
            method: 'POST',
            body: body,
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        }).then(function (res) {
            return res.text().then(function (html) {
                var target = document.querySelector(btn.getAttribute('data-post-refresh'));
                if (target) {
                    target.outerHTML = html;
                }
            });
        });
    });
})();
