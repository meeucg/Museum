import { SlowAppear, Show, ClassListContains } from '/utilityFunctions.js';
class headerFrame {
    constructor(document, url, onExitCallback = () => { }, onLoadCallback = () => { }) {
        this.frame = document.createElement("iframe");
        this.frame.className = "sign-in-frame";
        this.frame.src = url;

        this.document = document;
        this.header = document.querySelector("header");

        this.onExitCallback = onExitCallback;
        this.onLoadCallback = onLoadCallback;
    }

    show() {
        let frame = this.frame;
        let document = this.document;
        let header = this.header;

        header.insertBefore(frame, header.firstChild);

        let wait = setTimeout(() => {
            alert("Failed to load page, timeout expired.");
            frame.remove();
        }, 15000);

        frame.addEventListener('load', (ev) => {
            var iframeDocument = frame.contentDocument || frame.contentWindow.document;

            this.onLoadCallback(iframeDocument);
            clearTimeout(wait);

            let handleKey = (ev) => {
                if (ev.key == "Escape") {
                    this.hide();
                }
            };

            let handleClick = (ev) => {
                if (ClassListContains(ev.target.classList, "escape-on-click")) {
                    this.hide();
                }
            };

            iframeDocument.addEventListener("click", handleClick);
            iframeDocument.addEventListener("keydown", handleKey);
            document.addEventListener("keydown", handleKey);

            SlowAppear(frame);
            setTimeout(() => {
                Show(frame);
            }, 150);
        });
    }

    hide() {
        this.frame.remove();
        this.onExitCallback();
    }
}


export {headerFrame};