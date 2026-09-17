// Reusable grid: fetches paged/sorted rows from GET /api/applications and renders them. All
// paging/sorting/filtering logic lives server-side (in the database query); this only tracks
// UI state and re-fetches when it changes.
(function () {
    document.querySelectorAll('.applications-grid').forEach(function (grid) {
        if (grid.dataset.gridInitialized) {
            return;
        }
        grid.dataset.gridInitialized = 'true';

        var state = { status: '', page: 1, pageSize: 10, sortBy: 'createdAt', sortDir: 'desc' };
        var body = grid.querySelector('[data-grid-body]');
        var pagination = grid.querySelector('[data-grid-pagination]');
        var summary = grid.querySelector('[data-grid-summary]');

        function statusBadgeClass(status) {
            return {
                Draft: 'bg-secondary', Submitted: 'bg-info', UnderReview: 'bg-primary', Returned: 'bg-warning text-dark',
                Approved: 'bg-success', Denied: 'bg-danger', Withdrawn: 'bg-secondary text-decoration-line-through'
            }[status] || 'bg-secondary';
        }

        function render(data) {
            if (data.rows.length === 0) {
                body.innerHTML = '<tr><td colspan="5" class="text-muted">No applications found.</td></tr>';
            } else {
                body.innerHTML = data.rows.map(function (row) {
                    return '<tr>' +
                        '<td>' + new Date(row.createdAtUtc).toLocaleDateString() + '</td>' +
                        '<td>' + row.propertyName + ' — ' + row.unitNumber + '</td>' +
                        '<td>' + row.applicantNames + '</td>' +
                        '<td><span class="badge ' + statusBadgeClass(row.status) + '">' + row.status + '</span></td>' +
                        '<td><a class="btn btn-sm btn-outline-primary" href="/Applications/Details/' + row.id + '">View</a></td>' +
                        '</tr>';
                }).join('');
            }

            var totalPages = Math.max(1, Math.ceil(data.totalCount / data.pageSize));
            summary.textContent = 'Page ' + data.page + ' of ' + totalPages + ' (' + data.totalCount + ' total)';

            pagination.innerHTML = '';
            for (var p = 1; p <= totalPages; p++) {
                var li = document.createElement('li');
                li.className = 'page-item' + (p === data.page ? ' active' : '');
                var a = document.createElement('a');
                a.className = 'page-link';
                a.href = '#';
                a.textContent = p;
                a.addEventListener('click', (function (targetPage) {
                    return function (e) { e.preventDefault(); state.page = targetPage; load(); };
                })(p));
                li.appendChild(a);
                pagination.appendChild(li);
            }

            grid.querySelectorAll('[data-sort-indicator]').forEach(function (el) {
                el.textContent = el.getAttribute('data-sort-indicator') === state.sortBy ? (state.sortDir === 'asc' ? '▲' : '▼') : '';
            });
        }

        function load() {
            var url = new URL(grid.getAttribute('data-api-url'), window.location.origin);
            if (state.status) url.searchParams.set('status', state.status);
            url.searchParams.set('page', state.page);
            url.searchParams.set('pageSize', state.pageSize);
            url.searchParams.set('sortBy', state.sortBy);
            url.searchParams.set('sortDir', state.sortDir);

            fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
                .then(function (res) { return res.json(); })
                .then(render);
        }

        var statusSelect = grid.querySelector('[data-grid-status]');
        if (statusSelect) {
            statusSelect.addEventListener('change', function () { state.status = this.value; state.page = 1; load(); });
        }

        var pageSizeSelect = grid.querySelector('[data-grid-page-size]');
        if (pageSizeSelect) {
            pageSizeSelect.addEventListener('change', function () { state.pageSize = parseInt(this.value, 10); state.page = 1; load(); });
        }

        grid.querySelectorAll('[data-grid-sort]').forEach(function (header) {
            header.addEventListener('click', function () {
                var column = this.getAttribute('data-grid-sort');
                if (state.sortBy === column) {
                    state.sortDir = state.sortDir === 'asc' ? 'desc' : 'asc';
                } else {
                    state.sortBy = column;
                    state.sortDir = 'asc';
                }
                load();
            });
        });

        load();
    });
})();
