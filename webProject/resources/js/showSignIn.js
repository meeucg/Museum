import { SlowAppear, Show, ClassListContains } from '/utilityFunctions.js';

class loginPage {

    constructor(document, onExitCallback = () => { }) {
        this.loginFrame = document.createElement("iframe");
        this.loginFrame.className = "sign-in-frame";
        this.loginFrame.src = "/loginPage.html";

        this.document = document;
        this.body = document.querySelector("body");

        this.onExitCallback = onExitCallback;
    }

    show() {
        let loginFrame = this.loginFrame;
        let document = this.document;
        let body = this.body;

        body.insertBefore(loginFrame, body.firstChild);

        let wait = setTimeout(() => {
            alert("Failed to load Sign-in page, timeout expired.");
            loginFrame.remove();
        }, 15000);

        loginFrame.addEventListener('load', (ev) => {
            clearTimeout(wait);
            var iframeDocument = loginFrame.contentDocument || loginFrame.contentWindow.document;
            
            let handleKey = (parent) => { 
                return (ev) => {
                    if(ev.key == "Escape"){
                        this.hide();
                        parent.removeEventListener("keydown", handleKey(parent));
                    }
                }; 
            };

            let handleClick = (parent) => { 
                return (ev) => {
                    if(ClassListContains(ev.target.classList, "center-container")){
                        this.hide();
                        parent.removeEventListener("click", handleKey(parent));
                    }
                }; 
            };

            iframeDocument.addEventListener("click", handleClick(iframeDocument));
            iframeDocument.addEventListener("keydown", handleKey(iframeDocument));

            document.addEventListener("keydown", handleKey(document));

            SlowAppear(loginFrame);
            setTimeout(() => {
                Show(loginFrame);
            }, 150);
        });
    }

    hide(){
        this.loginFrame.remove();
        this.onExitCallback();
    }
}

class registerPage {

    constructor(document, onExitCallback = () => { }) {
        this.registerFrame = document.createElement("iframe");
        this.registerFrame.className = "sign-in-frame";
        this.registerFrame.src = "/registerPage.html";

        this.document = document;
        this.body = document.querySelector("body");

        this.onExitCallback = onExitCallback;
    }

    show() {
        let registerFrame = this.registerFrame;
        let document = this.document;
        let body = this.body;
        
        body.insertBefore(registerFrame, body.firstChild);

        let wait = setTimeout(() => {
            alert("Failed to load Sign-in page, timeout expired.");
            registerFrame.remove();
        }, 15000);

        registerFrame.addEventListener('load', (ev) => {
            clearTimeout(wait);
            var iframeDocument = registerFrame.contentDocument || registerFrame.contentWindow.document;

            let handleKey = (parent) => { 
                return (ev) => {
                    if(ev.key == "Escape"){
                        this.hide();
                        parent.removeEventListener("keydown", handleKey(parent));
                    }
                }; 
            };

            let handleClick = (parent) => { 
                return (ev) => {
                    if(ClassListContains(ev.target.classList, "center-container")){
                        this.hide();
                        parent.removeEventListener("click", handleKey(parent));
                    }
                }; 
            };

            iframeDocument.addEventListener("click", handleClick(iframeDocument));
            iframeDocument.addEventListener("keydown", handleKey(iframeDocument));

            document.addEventListener("keydown", handleKey(document));

            SlowAppear(registerFrame);
            setTimeout(() => {
                Show(registerFrame);
            }, 150);
        });
    }

    hide(){
        this.registerFrame.remove();
        this.onExitCallback();
    }
}

export {loginPage, registerPage};