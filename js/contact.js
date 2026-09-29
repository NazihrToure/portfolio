function validateContactField(field) {
  const value = field.value.trim();

  if (field.name === "name") {
  if (!value) return "Enter your name.";

  if (!/^[\p{L}\p{M}' -]+$/u.test(value)) {
    return "Enter a valid name.";
  }
}
  if (field.name === "email") {
    if (!value) return "Enter your email address.";
    if (field.validity.typeMismatch) {
      return "Enter a valid email address, such as name@example.com.";
    }
  }
  if (field.name === "message" && value.length < 20) {
    return "Write a message of at least 20 characters, excluding spaces at the beginning and end.";
  }
  if (value.length > field.maxLength) {
    return "Use no more than " + field.maxLength + " characters.";
  }
  return "";
}

function showFieldError(field, message) {
  document.getElementById(field.name + "-error").textContent = message;
  field.setAttribute("aria-invalid", String(Boolean(message)));
}

function validateContactForm(form) {
  const fields = Array.from(form.querySelectorAll("input, textarea"));
  return fields.filter(function(field) {
    const message = validateContactField(field);
    showFieldError(field, message);
    return Boolean(message);
  });
}

function handleContactSubmit(event) {
  event.preventDefault();
  const invalidFields = validateContactForm(event.currentTarget);
  const status = document.getElementById("form-status");

  if (invalidFields.length) {
    status.dataset.state = "error";
    status.textContent = "Please correct the highlighted fields before continuing.";
    invalidFields[0].focus();
    return;
  }

  status.dataset.state = "success";
  status.textContent = "Your message passes all checks. It has not been sent: sending is not available yet. You can contact me on LinkedIn using the link above.";
}

function handleContactInput(event) {
  const field = event.target;
  if (!field.matches("input, textarea")) return;

  const status = document.getElementById("form-status");
  status.textContent = "";
  delete status.dataset.state;
  if (field.hasAttribute("aria-invalid")) {
    showFieldError(field, validateContactField(field));
  }
}

function initializeContactForm() {
  const form = document.getElementById("contact-form");
  if (!form) return;

  form.noValidate = true;
  form.addEventListener("submit", handleContactSubmit);
  form.addEventListener("input", handleContactInput);
  form.querySelector('button[type="submit"]').disabled = false;
}

initializeContactForm();
