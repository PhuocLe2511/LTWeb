(function () {
    var productFetchController = null;
    var productFetchGeneration = 0;

    window.showProductSkeleton = function (container) {
        var cols = "";
        for (var i = 0; i < 8; i++) {
            cols +=
                '<div class="col-6 col-md-4 col-lg-3 mb-3">' +
                '<div class="product-skeleton card h-100 border-0">' +
                '<div class="skeleton-img"></div>' +
                '<div class="card-body pt-3">' +
                '<div class="skeleton-line"></div>' +
                '<div class="skeleton-line skeleton-line--short"></div>' +
                "</div></div></div>";
        }
        container.innerHTML = '<div class="row g-3">' + cols + "</div>";
    };

    /**
     * @param {Event|null} event
     * @param {HTMLFormElement} form
     * @param {number} page
     * @param {{ initial?: boolean }} [opts] initial=true: không dùng skeleton (tránh nhấp nháy 2 lần), chỉ dòng "Đang tải…"
     */
    window.paginationSearch = function (event, form, page, opts) {
        if (event) event.preventDefault();
        opts = opts || {};

        var target = document.getElementById(form.dataset.target);
        if (!target) return;

        if (productFetchController) {
            try {
                productFetchController.abort();
            } catch (e) { /* ignore */ }
        }
        productFetchController = new AbortController();
        var myGen = ++productFetchGeneration;

        if (opts.initial) {
            target.innerHTML =
                '<p class="text-muted small py-4 text-center mb-0">Đang tải danh sách…</p>';
        } else {
            showProductSkeleton(target);
        }

        var data = new FormData(form);
        data.set("Page", page);

        fetch(form.action + "?" + new URLSearchParams(data), {
            signal: productFetchController.signal,
            cache: "no-store",
            headers: { Accept: "text/html" }
        })
            .then(function (res) {
                if (!res.ok) throw new Error("network");
                return res.text();
            })
            .then(function (html) {
                if (myGen !== productFetchGeneration) return;
                target.innerHTML = html;
            })
            .catch(function (err) {
                if (err && err.name === "AbortError") return;
                if (myGen !== productFetchGeneration) return;
                target.innerHTML =
                    '<div class="alert alert-danger border-0 shadow-sm">Không tải được danh sách. Thử lại.</div>';
            });
    };
})();
