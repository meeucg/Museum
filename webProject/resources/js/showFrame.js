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
        let loginFrame = this.frame;
        let document = this.document;
        let header = this.header;

        header.insertBefore(loginFrame, header.firstChild);

        let wait = setTimeout(() => {
            alert("Failed to load page, timeout expired.");
            loginFrame.remove();
        }, 15000);

        loginFrame.addEventListener('load', (ev) => {
            this.onLoadCallback();
            clearTimeout(wait);
            var iframeDocument = loginFrame.contentDocument || loginFrame.contentWindow.document;

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

            SlowAppear(loginFrame);
            setTimeout(() => {
                Show(loginFrame);
            }, 150);
        });
    }

    hide() {
        this.frame.remove();
        this.onExitCallback();
    }
}


export {headerFrame};