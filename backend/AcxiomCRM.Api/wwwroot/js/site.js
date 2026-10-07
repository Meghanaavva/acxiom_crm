// General page behaviour for the Razor views

document.addEventListener('DOMContentLoaded', function () {
  // Show toast messages (TempData) and hide them after 4 seconds
  document.querySelectorAll('.toast').forEach(function (el) {
    new bootstrap.Toast(el, { delay: 4000 }).show();
  });

  // Sidebar drawer on tablets / mobile
  var sidebar = document.getElementById('sidebar');
  var toggle = document.getElementById('sidebarToggle');
  var backdrop = document.getElementById('sidebarBackdrop');
  if (sidebar && toggle) {
    toggle.addEventListener('click', function () { sidebar.classList.toggle('open'); });
    backdrop.addEventListener('click', function () { sidebar.classList.remove('open'); });
  }

  // Rows-per-page dropdown in the pagination partial
  document.querySelectorAll('.page-size-select').forEach(function (select) {
    select.addEventListener('change', function () { window.location.href = select.value; });
  });

  // Confirmation dialog: <form data-confirm="Delete this customer?"> (optional data-confirm-button="Delete")
  var modalEl = document.getElementById('confirmModal');
  if (modalEl) {
    var modal = new bootstrap.Modal(modalEl);
    var pendingForm = null;
    document.querySelectorAll('form[data-confirm]').forEach(function (form) {
      form.addEventListener('submit', function (e) {
        if (form.dataset.confirmed === 'true') return;
        e.preventDefault();
        pendingForm = form;
        document.getElementById('confirmModalMessage').textContent = form.dataset.confirm;
        var ok = document.getElementById('confirmModalOk');
        ok.textContent = form.dataset.confirmButton || 'Confirm';
        ok.className = 'btn ' + (form.dataset.confirmVariant || 'btn-danger');
        modal.show();
      });
    });
    document.getElementById('confirmModalOk').addEventListener('click', function () {
      if (!pendingForm) return;
      pendingForm.dataset.confirmed = 'true';
      modal.hide();
      pendingForm.requestSubmit ? pendingForm.requestSubmit() : pendingForm.submit();
    });
  }

  // Filter forms: submit when a dropdown / date changes
  document.querySelectorAll('form.auto-submit select, form.auto-submit input[type=date]').forEach(function (input) {
    input.addEventListener('change', function () { input.form.submit(); });
  });
});
