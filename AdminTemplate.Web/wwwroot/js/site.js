document.addEventListener("DOMContentLoaded", function () {
  var desktopToggleButton = document.querySelector(".sidebar-desktop-toggle");

  if (!desktopToggleButton) {
    return;
  }

  desktopToggleButton.addEventListener("click", function () {
    if (window.innerWidth >= 768) {
      document.body.classList.toggle("sidebar-collapsed");
    }
  });

  window.addEventListener("resize", function () {
    if (window.innerWidth < 768) {
      document.body.classList.remove("sidebar-collapsed");
    }
  });
});
