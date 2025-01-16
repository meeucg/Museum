import { headerFrame } from '/showFrame.js';

const backButton = document.getElementById("back-button");
const changeUser = document.getElementById("register");
const edit = document.getElementById("edit");
const profileContainer = document.getElementById("profile-container");
const banner = document.querySelector(".profile-banner");
const body = document.querySelector("body");

let style = getComputedStyle(profileContainer);
let height = +style.height.slice(0, -2);
let paddingTop = +style.paddingTop.slice(0, -2);
let paddingBottom = +style.paddingBottom.slice(0, -2);

banner.style.height = `${height + paddingTop + paddingBottom}px`;

backButton.addEventListener("click", (ev) => {
    window.history.back();
});

changeUser.addEventListener('click', () => {
    let onExit = (body) => {
        return () => {
            body.style.overflowY = "auto";
        }
    }

    let onLoad = (body) => {
        return () => {
            body.style.overflowY = "hidden";
        }
    }

    let lPage = new headerFrame(document, "/loginPage.html", onExit(body), onLoad(body));
    lPage.show();
});

edit.addEventListener("click", () => {
    let onExit = (body) => {
        return () => {
            body.style.overflowY = "auto";
        }
    }

    let onLoad = (body) => {
        return () => {
            body.style.overflowY = "hidden";
        }
    }

    let editPage = new headerFrame(document, "/editProfile.html", onExit(body), onLoad(body));
    editPage.show();
});