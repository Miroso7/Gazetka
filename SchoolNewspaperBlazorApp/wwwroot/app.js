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

// kadrowanie zdjęcia profilowego: przeciąganie i powiększanie zdjęcia w okrągłym oknie, wynik to kwadratowy JPEG
window.avatarCropper = (function() {
    // base64 wysyłamy do serwera we fragmentach, bo jedna wiadomość od przeglądarki może mieć najwyżej 32 KB
    var CHUNK = 16000;
    var states = new WeakMap();

    function clamp(value, min, max) {
        return Math.min(max, Math.max(min, value));
    }

    // ustawia zdjęcie w oknie tak, żeby zawsze wypełniało całe okno (bez pustych brzegów)
    function draw(state) {
        var scale = state.base * state.zoom;
        var width = state.image.naturalWidth * scale;
        var height = state.image.naturalHeight * scale;
        state.x = clamp(state.x, state.size - width, 0);
        state.y = clamp(state.y, state.size - height, 0);
        state.image.style.width = width + "px";
        state.image.style.height = height + "px";
        state.image.style.transform = "translate(" + state.x + "px," + state.y + "px)";
    }

    // powiększenie względem środka okna
    function setZoom(state, zoom) {
        var oldScale = state.base * state.zoom;
        var centerX = (state.size / 2 - state.x) / oldScale;
        var centerY = (state.size / 2 - state.y) / oldScale;
        state.zoom = clamp(zoom, 1, 3);
        var newScale = state.base * state.zoom;
        state.x = state.size / 2 - centerX * newScale;
        state.y = state.size / 2 - centerY * newScale;
        state.slider.value = state.zoom;
        draw(state);
    }

    function dispose(viewport) {
        var state = states.get(viewport);
        if (!state) return;
        viewport.removeEventListener("pointerdown", state.onDown);
        viewport.removeEventListener("pointermove", state.onMove);
        viewport.removeEventListener("pointerup", state.onUp);
        viewport.removeEventListener("pointercancel", state.onUp);
        viewport.removeEventListener("wheel", state.onWheel);
        viewport.removeEventListener("keydown", state.onKey);
        state.slider.removeEventListener("input", state.onSlider);
        state.observer.disconnect();
        URL.revokeObjectURL(state.url);
        if (state.image.parentNode) state.image.parentNode.removeChild(state.image);
        states.delete(viewport);
    }

    // wczytuje wybrane zdjęcie z pola pliku (bez wysyłania go na serwer); zwraca false, gdy plik nie jest zdjęciem
    function init(viewport, slider, input) {
        if (!viewport || !slider) return Promise.resolve(false);
        dispose(viewport);

        var file = input && input.files && input.files[0];
        if (!file) return Promise.resolve(false);

        var url = URL.createObjectURL(file);
        var image = new Image();
        image.alt = "";
        image.draggable = false;
        image.className = "avatar-crop__img";

        return new Promise(function(resolve) {
            image.onerror = function() {
                URL.revokeObjectURL(url);
                resolve(false);
            };
            image.onload = function() {
                var size = viewport.clientWidth;
                if (!size || !image.naturalWidth || !image.naturalHeight) {
                    URL.revokeObjectURL(url);
                    resolve(false);
                    return;
                }

                viewport.appendChild(image);
                var state = {
                    image: image,
                    url: url,
                    slider: slider,
                    size: size,
                    base: size / Math.min(image.naturalWidth, image.naturalHeight),
                    zoom: 1,
                    x: 0,
                    y: 0,
                    drag: null
                };

                // start: środek zdjęcia, a na zdjęciach pionowych kadr bliżej góry (tam zwykle jest twarz)
                var width = image.naturalWidth * state.base;
                var height = image.naturalHeight * state.base;
                state.x = (size - width) / 2;
                state.y = (size - height) * (height > width ? 0.2 : 0.5);
                slider.value = 1;
                draw(state);

                state.onDown = function(event) {
                    if (event.pointerType === "mouse" && event.button !== 0) return;
                    state.drag = { pointerX: event.clientX, pointerY: event.clientY, x: state.x, y: state.y };
                    viewport.setPointerCapture(event.pointerId);
                    viewport.classList.add("is-dragging");
                };
                state.onMove = function(event) {
                    if (!state.drag) return;
                    state.x = state.drag.x + event.clientX - state.drag.pointerX;
                    state.y = state.drag.y + event.clientY - state.drag.pointerY;
                    draw(state);
                };
                state.onUp = function() {
                    state.drag = null;
                    viewport.classList.remove("is-dragging");
                };
                state.onWheel = function(event) {
                    event.preventDefault();
                    setZoom(state, state.zoom - event.deltaY * 0.002);
                };
                state.onKey = function(event) {
                    var step = 8;
                    var moves = { ArrowLeft: [step, 0], ArrowRight: [-step, 0], ArrowUp: [0, step], ArrowDown: [0, -step] };
                    var move = moves[event.key];
                    if (!move) return;
                    event.preventDefault();
                    state.x += move[0];
                    state.y += move[1];
                    draw(state);
                };
                state.onSlider = function() {
                    setZoom(state, parseFloat(slider.value));
                };
                // gdy okno zmieni rozmiar (np. obrót telefonu), kadr zostaje ten sam
                state.observer = new ResizeObserver(function() {
                    var newSize = viewport.clientWidth;
                    if (!newSize || newSize === state.size) return;
                    var ratio = newSize / state.size;
                    state.x *= ratio;
                    state.y *= ratio;
                    state.base *= ratio;
                    state.size = newSize;
                    draw(state);
                });

                viewport.addEventListener("pointerdown", state.onDown);
                viewport.addEventListener("pointermove", state.onMove);
                viewport.addEventListener("pointerup", state.onUp);
                viewport.addEventListener("pointercancel", state.onUp);
                viewport.addEventListener("wheel", state.onWheel, { passive: false });
                viewport.addEventListener("keydown", state.onKey);
                slider.addEventListener("input", state.onSlider);
                state.observer.observe(viewport);

                states.set(viewport, state);
                resolve(true);
            };
            image.src = url;
        });
    }

    // wycina wybrany kadr do kwadratowego JPEG-a; zwraca liczbę fragmentów base64 do pobrania przez chunk()
    function apply(viewport, outputSize) {
        var state = states.get(viewport);
        if (!state) return Promise.resolve(0);

        var scale = state.base * state.zoom;
        var side = state.size / scale;
        var canvas = document.createElement("canvas");
        canvas.width = outputSize;
        canvas.height = outputSize;

        var context = canvas.getContext("2d");
        context.fillStyle = "#ffffff";
        context.fillRect(0, 0, outputSize, outputSize);
        context.imageSmoothingQuality = "high";
        context.drawImage(state.image, -state.x / scale, -state.y / scale, side, side, 0, 0, outputSize, outputSize);

        return new Promise(function(resolve) {
            canvas.toBlob(function(blob) {
                if (!blob) {
                    resolve(0);
                    return;
                }
                var reader = new FileReader();
                reader.onload = function() {
                    var data = String(reader.result);
                    state.encoded = data.substring(data.indexOf(",") + 1);
                    resolve(Math.ceil(state.encoded.length / CHUNK));
                };
                reader.onerror = function() {
                    resolve(0);
                };
                reader.readAsDataURL(blob);
            }, "image/jpeg", 0.9);
        });
    }

    function chunk(viewport, index) {
        var state = states.get(viewport);
        return state && state.encoded ? state.encoded.substr(index * CHUNK, CHUNK) : "";
    }

    return { init: init, apply: apply, chunk: chunk, dispose: dispose };
})();

// zegar i data w nagłówku (widoczne od 1200 px): odświeżane co sekundę w przeglądarce, bez udziału serwera
(function() {
    var timeFormat = new Intl.DateTimeFormat("pl-PL", { hour: "2-digit", minute: "2-digit", second: "2-digit" });
    var dateFormat = new Intl.DateTimeFormat("pl-PL", { weekday: "short", day: "numeric", month: "short" });

    function tick() {
        var now = new Date();
        var time = document.querySelector("[data-site-clock='time']");
        var date = document.querySelector("[data-site-clock='date']");
        if (time) time.textContent = timeFormat.format(now);
        if (date) date.textContent = dateFormat.format(now);
    }

    tick();
    setInterval(tick, 1000);
    document.addEventListener("DOMContentLoaded", tick);
})();
