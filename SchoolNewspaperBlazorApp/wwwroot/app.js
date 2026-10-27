// pomocnik formularza artykułu: pozycja kursora w polu treści (do wstawiania zdjęć w środek tekstu)
window.articleForm = {
    caret: function(element) {
        return element && typeof element.selectionStart === "number" ? element.selectionStart : -1;
    }
};

// menu na telefonie zamyka się po wybraniu linku lub wysłaniu wyszukiwania (nawigacja bez przeładowania strony zostawiłaby je otwarte)
(function() {
    function closeNav() {
        var toggle = document.getElementById("nav-toggle");
        if (toggle) toggle.checked = false;
    }

    document.addEventListener("click", function(event) {
        if (event.target.closest && event.target.closest(".site-nav__panel a")) closeNav();
    });

    document.addEventListener("submit", function(event) {
        if (event.target.closest && event.target.closest(".site-nav__search")) closeNav();
    });
})();
