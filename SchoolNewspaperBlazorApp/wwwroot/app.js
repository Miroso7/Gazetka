// pomocnik formularza artykułu: pozycja kursora w polu treści (do wstawiania zdjęć w środek tekstu)
window.articleForm = {
    caret: function (element) {
        return element && typeof element.selectionStart === "number" ? element.selectionStart : -1;
    }
};
