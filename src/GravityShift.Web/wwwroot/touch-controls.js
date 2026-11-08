// On-screen controls for touch screens. Each button sends the same keydown/keyup a keyboard
// would; Yaeger's browser runtime reads KeyboardEvent.code from window listeners, so the game
// can't tell the difference. Buttons capture their own pointers, so several fingers work at
// once (hold walk while tapping a shift), unlike Yaeger's single-pointer canvas input.
(() => {
    const controls = document.getElementById('touch-controls');
    if (!controls) return;

    // ?touch=1 shows the controls on any device (handy for testing with a mouse).
    if (new URLSearchParams(location.search).get('touch') === '1') {
        document.documentElement.classList.add('force-touch');
    }

    const send = (type, code) =>
        window.dispatchEvent(new KeyboardEvent(type, { code, key: code, bubbles: true }));

    const releases = [];

    for (const button of controls.querySelectorAll('[data-key]')) {
        const code = button.dataset.key;
        const pointers = new Set();

        const release = (pointerId) => {
            if (!pointers.delete(pointerId) || pointers.size > 0) return;
            send('keyup', code);
            button.classList.remove('active');
        };

        button.addEventListener('pointerdown', (e) => {
            e.preventDefault();
            try {
                button.setPointerCapture(e.pointerId);
            } catch {
                // Synthetic or already-ended pointers can't be captured; the press still counts.
            }
            if (pointers.size === 0) {
                send('keydown', code);
                button.classList.add('active');
            }
            pointers.add(e.pointerId);
        });
        for (const type of ['pointerup', 'pointercancel', 'lostpointercapture']) {
            button.addEventListener(type, (e) => release(e.pointerId));
        }
        button.addEventListener('contextmenu', (e) => e.preventDefault());
        releases.push(() => [...pointers].forEach(release));
    }

    // Never leave a key held down when the page is hidden (app switch, notification shade).
    document.addEventListener('visibilitychange', () => {
        if (document.hidden) releases.forEach((r) => r());
    });

    const fullscreen = document.getElementById('fullscreen');
    if (fullscreen) {
        if (!document.fullscreenEnabled) {
            fullscreen.remove(); // e.g. iPhone Safari; itch.io's own fullscreen button still works
        } else {
            fullscreen.addEventListener('click', () => {
                if (document.fullscreenElement) document.exitFullscreen();
                else document.documentElement.requestFullscreen().catch(() => {});
            });
        }
    }
})();
