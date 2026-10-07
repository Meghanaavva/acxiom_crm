// Custom client-side rules for jQuery unobtrusive validation.
// The same rules are checked again on the server (services), this is only for quick feedback.
(function ($) {
  if (!$ || !$.validator) return;

  function today() {
    var d = new Date();
    var m = String(d.getMonth() + 1).padStart(2, '0');
    var day = String(d.getDate()).padStart(2, '0');
    return d.getFullYear() + '-' + m + '-' + day;
  }

  var OPEN_STAGES = ['Qualification', 'Proposal', 'Negotiation'];

  function stageIsOpen(element, stageField) {
    var stage = $(element.form).find('[name="' + stageField + '"]').val();
    return OPEN_STAGES.indexOf(stage) >= 0;
  }

  // data-val-notpast="message" -> date must be today or later
  $.validator.addMethod('notpast', function (value, element) {
    return this.optional(element) || value >= today();
  });
  $.validator.unobtrusive.adapters.addBool('notpast');

  // data-val-notpastifopen="message" data-val-notpastifopen-stagefield="Stage"
  $.validator.addMethod('notpastifopen', function (value, element, stageField) {
    if (this.optional(element) || !stageIsOpen(element, stageField)) return true;
    return value >= today();
  });
  $.validator.unobtrusive.adapters.addSingleVal('notpastifopen', 'stagefield');

  // data-val-positiveifopen="message" data-val-positiveifopen-stagefield="Stage"
  $.validator.addMethod('positiveifopen', function (value, element, stageField) {
    if (this.optional(element)) return true;
    var amount = parseFloat(value);
    if (amount < 0) return false;
    return !stageIsOpen(element, stageField) || amount > 0;
  });
  $.validator.unobtrusive.adapters.addSingleVal('positiveifopen', 'stagefield');

  // data-val-atleastone="message" data-val-atleastone-fields="CustomerId,LeadId"
  $.validator.addMethod('atleastone', function (value, element, fields) {
    var form = $(element.form);
    return fields.split(',').some(function (name) {
      var v = form.find('[name="' + name + '"]').val();
      return v && String(v).trim() !== '';
    });
  });
  $.validator.unobtrusive.adapters.addSingleVal('atleastone', 'fields');

  // data-val-passwordpolicy="Password" -> same rules as the ASP.NET Core Identity password options
  $.validator.addMethod('passwordpolicy', function (value, element) {
    if (this.optional(element)) return true;
    var message = '';
    if (value.length < 8) message = 'Password must be at least 8 characters.';
    else if (!/[A-Z]/.test(value)) message = 'Password must contain at least one uppercase letter.';
    else if (!/[a-z]/.test(value)) message = 'Password must contain at least one lowercase letter.';
    else if (!/\d/.test(value)) message = 'Password must contain at least one number.';
    else if (!/[^A-Za-z0-9]/.test(value)) message = 'Password must contain at least one special character.';
    $.validator.messages.passwordpolicy = message;
    $(element).data('passwordpolicy-message', message);
    return message === '';
  }, function (params, element) {
    return $(element).data('passwordpolicy-message');
  });
  $.validator.unobtrusive.adapters.add('passwordpolicy', function (options) {
    options.rules.passwordpolicy = true;
  });

  // Re-check dependent fields when the stage changes
  $(document).on('change', 'select[name="Stage"]', function () {
    var form = $(this.form);
    form.find('[data-val-positiveifopen], [data-val-notpastifopen]').each(function () {
      if (form.data('validator')) form.validate().element(this);
    });
  });
})(window.jQuery);

// Same wording as the server messages
if (window.jQuery && jQuery.validator) {
  jQuery.validator.messages.email = 'Enter a valid email address.';
  jQuery.validator.messages.number = 'Enter a valid number.';
}

// HTML min / max attributes (date pickers, probability) are also checked by jQuery Validate.
// Use our business messages for them instead of the generic "Please enter a value ..." text.
(function () {
  document.querySelectorAll('[data-val-notpast], [data-val-notpastifopen]').forEach(function (el) {
    el.setAttribute('data-msg-min', el.getAttribute('data-val-notpast') || el.getAttribute('data-val-notpastifopen'));
  });
  document.querySelectorAll('[data-val-range]').forEach(function (el) {
    el.setAttribute('data-msg-min', el.getAttribute('data-val-range'));
    el.setAttribute('data-msg-max', el.getAttribute('data-val-range'));
  });
})();

// Validate when a value changes instead of on blur. With blur validation the error text under a
// field disappears when the user clicks "Save", the button moves up and the first click is lost.
if (window.jQuery && jQuery.validator) {
  jQuery.validator.setDefaults({ onfocusout: false });
  jQuery(document).on('change', 'form input, form select, form textarea', function () {
    var validator = jQuery(this.form).data('validator');
    if (validator && this.name && validator.settings.rules) validator.element(this);
  });
}
