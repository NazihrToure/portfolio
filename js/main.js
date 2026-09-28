window.addEventListener("resize", function() {
  var windowWidth = window.innerWidth;
  var windowHeight = window.innerHeight;

  console.log("Window width: " + windowWidth);
  console.log("Window height: " + windowHeight);

  if (windowWidth >= 900) {
    var button = document.getElementById("mobile-menu");
    var navigation = document.getElementById("nav-buttons");
    button.setAttribute("aria-expanded", "false");
    navigation.className = "nav-buttons";
  }
});

function toggleMobileMenu() {
  var button = document.getElementById("mobile-menu");
  var navigation = document.getElementById("nav-buttons");
  var isOpen = button.getAttribute("aria-expanded") === "true";

  if (isOpen) {
    button.setAttribute("aria-expanded", "false");
    navigation.className = "nav-buttons";
  } else {
    button.setAttribute("aria-expanded", "true");
    navigation.className = "mobile-nav-buttons";
  }
}

function changeIconColor(iconId, imageUrlPattern) {
  const icon = document.getElementById(iconId);
  icon.src = imageUrlPattern;
}
