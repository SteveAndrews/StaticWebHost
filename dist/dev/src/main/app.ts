import { strings } from "./strings.js";

export function init() {
    const btn = document.getElementById('the-button') as HTMLButtonElement;
    const results = document.getElementById('the-results') as HTMLDivElement;

    if (!btn || !results) {
        throw new Error('Missing required DOM elements.');
    }

    btn.addEventListener('click', (e) => {
        results.textContent = strings().mainText;
    });
}

init();