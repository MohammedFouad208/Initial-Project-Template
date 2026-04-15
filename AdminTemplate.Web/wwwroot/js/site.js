document.addEventListener("DOMContentLoaded", function () {
  initSidebar();
  initRtlToggle();
  initFormLoadingStates();
  initMicroInteractions();
  applyStoredDirection();
});

/* ============================================================
   Sidebar
   ============================================================ */
function initSidebar() {
  var toggleBtn = document.querySelector(".sidebar-desktop-toggle");
  var sidebar   = document.getElementById("adminSidebar");
  var backdrop  = document.querySelector(".sidebar-backdrop");
  var closeBtn  = document.querySelector(".sidebar-close-btn");

  if (!toggleBtn) return;

  toggleBtn.addEventListener("click", function () {
    if (window.innerWidth >= 768) {
      document.body.classList.toggle("sidebar-collapsed");
      localStorage.setItem("sidebarCollapsed",
        document.body.classList.contains("sidebar-collapsed") ? "1" : "0");
    } else {
      toggleMobileSidebar(sidebar, backdrop);
    }
  });

  if (closeBtn) {
    closeBtn.addEventListener("click", function () {
      closeMobileSidebar(sidebar, backdrop);
    });
  }

  if (backdrop) {
    backdrop.addEventListener("click", function () {
      closeMobileSidebar(sidebar, backdrop);
    });
  }

  window.addEventListener("resize", function () {
    if (window.innerWidth >= 768) {
      closeMobileSidebar(sidebar, backdrop);
    }
  });

  if (localStorage.getItem("sidebarCollapsed") === "1" && window.innerWidth >= 768) {
    document.body.classList.add("sidebar-collapsed");
  }
}

function toggleMobileSidebar(sidebar, backdrop) {
  if (!sidebar) return;
  var isOpen = sidebar.classList.contains("sidebar-open");
  isOpen ? closeMobileSidebar(sidebar, backdrop) : openMobileSidebar(sidebar, backdrop);
}

function openMobileSidebar(sidebar, backdrop) {
  if (sidebar) sidebar.classList.add("sidebar-open");
  if (backdrop) backdrop.classList.add("active");
  document.body.style.overflow = "hidden";
}

function closeMobileSidebar(sidebar, backdrop) {
  if (sidebar) sidebar.classList.remove("sidebar-open");
  if (backdrop) backdrop.classList.remove("active");
  document.body.style.overflow = "";
}

/* ============================================================
   RTL / LTR Toggle
   ============================================================ */
function applyStoredDirection() {
  var dir = localStorage.getItem("textDirection") || "ltr";
  applyDirection(dir);
}

function applyDirection(dir) {
  var html = document.documentElement;
  html.setAttribute("dir", dir);
  html.setAttribute("lang", dir === "rtl" ? "ar" : "en");

  document.querySelectorAll(".rtl-toggle-btn").forEach(function (btn) {
    btn.textContent = dir === "rtl" ? "LTR" : "RTL";
    btn.title = dir === "rtl" ? "Switch to Left-to-Right" : "Switch to Right-to-Left";
  });
}

function initRtlToggle() {
  var btn = document.querySelector(".rtl-toggle-btn");
  if (!btn) return;

  btn.addEventListener("click", function () {
    var current = document.documentElement.getAttribute("dir") || "ltr";
    var next = current === "rtl" ? "ltr" : "rtl";
    applyDirection(next);
    localStorage.setItem("textDirection", next);
  });
}

/* ============================================================
   Form Loading States
   ============================================================ */
function initFormLoadingStates() {
  document.querySelectorAll("form").forEach(function (form) {
    form.addEventListener("submit", function () {
      var submitBtn = form.querySelector('[type="submit"]');
      if (submitBtn && !submitBtn.dataset.noLoading) {
        submitBtn.classList.add("btn-loading");
        submitBtn.disabled = true;
      }
    });
  });
}

/* ============================================================
   Micro-Interactions
   ============================================================ */
function initMicroInteractions() {
  animateCounters();
  addRippleEffect();
}

function animateCounters() {
  document.querySelectorAll("[data-count]").forEach(function (el) {
    var target = parseInt(el.getAttribute("data-count"), 10);
    if (isNaN(target) || target === 0) return;
    countUp(el, 0, target, 1100);
  });
}

function countUp(el, from, to, duration) {
  var startTime = null;
  function step(timestamp) {
    if (!startTime) startTime = timestamp;
    var elapsed  = timestamp - startTime;
    var progress = Math.min(elapsed / duration, 1);
    var eased    = 1 - Math.pow(1 - progress, 3);
    el.textContent = Math.round(from + (to - from) * eased).toLocaleString();
    if (progress < 1) requestAnimationFrame(step);
  }
  requestAnimationFrame(step);
}

function addRippleEffect() {
  document.querySelectorAll(".btn-primary").forEach(function (btn) {
    btn.addEventListener("click", function (e) {
      var rect   = btn.getBoundingClientRect();
      var ripple = document.createElement("span");
      ripple.className = "btn-ripple";
      ripple.style.left = (e.clientX - rect.left) + "px";
      ripple.style.top  = (e.clientY - rect.top)  + "px";
      btn.appendChild(ripple);
      setTimeout(function () { ripple.remove(); }, 600);
    });
  });
}

/* ============================================================
   Toast Notifications
   ============================================================ */
var AppToast = (function () {
  function show(message, type) {
    type = type || "info";
    var colors = {
      success: "var(--success)",
      danger:  "var(--danger)",
      warning: "var(--warning)",
      info:    "var(--info)"
    };
    var accent = colors[type] || colors.info;

    var el = document.createElement("div");
    el.className = "toast align-items-center border-0 shadow";
    el.setAttribute("role", "alert");
    el.setAttribute("aria-live", "assertive");
    el.setAttribute("aria-atomic", "true");
    el.style.borderInlineStart = "4px solid " + accent;

    el.innerHTML =
      '<div class="d-flex">' +
      '  <div class="toast-body">' + message + '</div>' +
      '  <button type="button" class="btn-close me-2 m-auto"' +
      '    data-bs-dismiss="toast" aria-label="Close"></button>' +
      '</div>';

    var container = document.getElementById("toast-container");
    if (container) { container.appendChild(el); }

    var toast = bootstrap.Toast.getOrCreateInstance(el, { delay: 4000 });
    el.addEventListener("hidden.bs.toast", function () { el.remove(); });
    toast.show();
  }

  return { show: show };
}());
