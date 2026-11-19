// ==========================================
// PAWHAVEN CUSTOM UI DIALOG & NOTIFICATION
// Thay thế hoàn toàn alert() và confirm() mặc định của trình duyệt
// ==========================================

let pawConfirmCallback = null;

function showPawConfirm(options) {
    const title = options.title || 'Xác nhận thao tác';
    const message = options.message || 'Bạn có chắc chắn muốn thực hiện hành động này?';
    const type = options.type || 'success'; // 'success', 'danger', 'warning', 'info'
    const okText = options.okText || 'Xác nhận';
    const cancelText = options.cancelText || 'Hủy bỏ';
    
    pawConfirmCallback = options.onConfirm || null;

    const modalEl = document.getElementById('pawConfirmModal');
    if (!modalEl) {
        if (confirm(message)) {
            if (options.onConfirm) options.onConfirm();
            if (options.form) options.form.submit();
        }
        return;
    }

    document.getElementById('pawConfirmTitle').textContent = title;
    document.getElementById('pawConfirmMessage').textContent = message;

    const okBtn = document.getElementById('pawConfirmOkBtn');
    okBtn.textContent = okText;

    const cancelBtn = document.getElementById('pawConfirmCancelBtn');
    cancelBtn.textContent = cancelText;
    cancelBtn.style.display = 'block';

    const iconBox = document.getElementById('pawConfirmIconBox');
    const icon = document.getElementById('pawConfirmIcon');

    if (type === 'danger') {
        iconBox.className = 'rounded-circle d-inline-flex align-items-center justify-content-center mb-3 bg-danger bg-opacity-10 text-danger shadow-sm';
        icon.className = 'bi bi-exclamation-triangle-fill fs-2';
        okBtn.className = 'btn btn-danger rounded-pill px-4 py-2 fw-bold flex-grow-1 shadow-sm';
    } else if (type === 'warning') {
        iconBox.className = 'rounded-circle d-inline-flex align-items-center justify-content-center mb-3 bg-warning bg-opacity-10 text-warning shadow-sm';
        icon.className = 'bi bi-exclamation-circle-fill fs-2';
        okBtn.className = 'btn btn-warning rounded-pill px-4 py-2 fw-bold flex-grow-1 shadow-sm text-dark';
    } else if (type === 'info') {
        iconBox.className = 'rounded-circle d-inline-flex align-items-center justify-content-center mb-3 bg-info bg-opacity-10 text-info shadow-sm';
        icon.className = 'bi bi-info-circle-fill fs-2';
        okBtn.className = 'btn btn-info rounded-pill px-4 py-2 fw-bold flex-grow-1 shadow-sm text-white';
    } else {
        iconBox.className = 'rounded-circle d-inline-flex align-items-center justify-content-center mb-3 bg-success bg-opacity-10 text-success shadow-sm';
        icon.className = 'bi bi-check-circle-fill fs-2';
        okBtn.className = 'btn btn-success rounded-pill px-4 py-2 fw-bold flex-grow-1 shadow-sm';
    }

    okBtn.onclick = function() {
        const modalInstance = bootstrap.Modal.getInstance(modalEl);
        if (modalInstance) modalInstance.hide();
        if (pawConfirmCallback) pawConfirmCallback();
        if (options.form) options.form.submit();
    };

    const modalInstance = bootstrap.Modal.getOrCreateInstance(modalEl);
    modalInstance.show();
}

function showPawAlert(options) {
    if (typeof options === 'string') {
        options = { message: options };
    }
    const title = options.title || 'Thông báo';
    const message = options.message || '';
    const type = options.type || 'info';
    const okText = options.okText || 'Đã hiểu';

    const modalEl = document.getElementById('pawConfirmModal');
    if (!modalEl) {
        alert(message);
        return;
    }

    document.getElementById('pawConfirmTitle').textContent = title;
    document.getElementById('pawConfirmMessage').textContent = message;

    const okBtn = document.getElementById('pawConfirmOkBtn');
    okBtn.textContent = okText;

    const cancelBtn = document.getElementById('pawConfirmCancelBtn');
    cancelBtn.style.display = 'none'; // Ẩn nút hủy, chỉ để lại nút OK

    const iconBox = document.getElementById('pawConfirmIconBox');
    const icon = document.getElementById('pawConfirmIcon');

    if (type === 'success') {
        iconBox.className = 'rounded-circle d-inline-flex align-items-center justify-content-center mb-3 bg-success bg-opacity-10 text-success shadow-sm';
        icon.className = 'bi bi-check-circle-fill fs-2';
        okBtn.className = 'btn btn-success rounded-pill px-4 py-2 fw-bold flex-grow-1 shadow-sm';
    } else if (type === 'danger') {
        iconBox.className = 'rounded-circle d-inline-flex align-items-center justify-content-center mb-3 bg-danger bg-opacity-10 text-danger shadow-sm';
        icon.className = 'bi bi-x-circle-fill fs-2';
        okBtn.className = 'btn btn-danger rounded-pill px-4 py-2 fw-bold flex-grow-1 shadow-sm';
    } else {
        iconBox.className = 'rounded-circle d-inline-flex align-items-center justify-content-center mb-3 bg-primary bg-opacity-10 text-primary shadow-sm';
        icon.className = 'bi bi-info-circle-fill fs-2';
        okBtn.className = 'btn btn-primary rounded-pill px-4 py-2 fw-bold flex-grow-1 shadow-sm';
    }

    okBtn.onclick = function() {
        const modalInstance = bootstrap.Modal.getInstance(modalEl);
        if (modalInstance) modalInstance.hide();
        if (options.onClose) options.onClose();
    };

    const modalInstance = bootstrap.Modal.getOrCreateInstance(modalEl);
    modalInstance.show();
}

// Bắt sự kiện tự động cho các nút có thuộc tính data-paw-confirm
document.addEventListener('click', function(e) {
    const trigger = e.target.closest('[data-paw-confirm]');
    if (trigger) {
        e.preventDefault();
        e.stopPropagation();

        const msg = trigger.getAttribute('data-paw-confirm');
        const title = trigger.getAttribute('data-paw-title') || 'Xác nhận thao tác';
        const type = trigger.getAttribute('data-paw-type') || 'success';
        const okText = trigger.getAttribute('data-paw-ok') || 'Xác nhận';
        const form = trigger.closest('form');

        showPawConfirm({
            title: title,
            message: msg,
            type: type,
            okText: okText,
            form: form,
            onConfirm: function() {
                if (!form && trigger.tagName === 'A' && trigger.href) {
                    window.location.href = trigger.href;
                }
            }
        });
    }
});
